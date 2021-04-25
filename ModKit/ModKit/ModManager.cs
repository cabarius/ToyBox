using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace ModKit {
    public interface IModEventHandler {
        int Priority { get; }

        void HandleModEnable();

        void HandleModDisable();
    }

    public class ModManager<TCore, TSettings>
        where TCore : class, new()
        where TSettings : UnityModManager.ModSettings, new() {
        #region Fields & Properties

        private UnityModManager.ModEntry.ModLogger _logger;
        private List<IModEventHandler> _eventHandlers;

        public TCore Core { get; private set; }

        public TSettings Settings { get; private set; }

        public Version Version { get; private set; }

        public bool Enabled { get; private set; }

        public bool Patched { get; private set; }

        #endregion

        #region Toggle

        public void Enable(UnityModManager.ModEntry modEntry, Assembly assembly) {
            _logger = modEntry.Logger;

            if (Enabled) {
                Debug("Already enabled.");
                return;
            }

            using (ProcessLogger process = new ProcessLogger(_logger)) {
                try {
                    process.Log("Enabling.");
                    var dict = Harmony.VersionInfo(out var myVersion);
                    process.Log($"Harmony version: {myVersion}");
                    foreach (var entry in dict) {
                        process.Log($"Mod {entry.Key} loaded with Harmony version {entry.Value}");
                    }

                    process.Log("Loading settings.");
                    modEntry.OnSaveGUI += HandleSaveGUI;
                    Version = modEntry.Version;
                    Settings = UnityModManager.ModSettings.Load<TSettings>(modEntry);
                    Core = new TCore();

                    Type[] types = assembly.GetTypes();

                    if (!Patched) {
                        Harmony harmonyInstance = new Harmony(modEntry.Info.Id);
                        foreach (Type type in types) {
                            List<HarmonyMethod> harmonyMethods = HarmonyMethodExtensions.GetFromType(type);
                            if (harmonyMethods != null && harmonyMethods.Count() > 0) {
                                process.Log($"Patching: {type.FullName}");
                                try {
                                    PatchClassProcessor patchProcessor = harmonyInstance.CreateClassProcessor(type);
                                    patchProcessor.Patch();
                                }
                                catch (Exception e) {
                                    Error(e);
                                }
                            }
                        }
                        Patched = true;
                    }

                    Enabled = true;

                    process.Log("Registering events.");
                    _eventHandlers = types.Where(type => type != typeof(TCore) &&
                        !type.IsInterface && !type.IsAbstract && typeof(IModEventHandler).IsAssignableFrom(type))
                        .Select(type => Activator.CreateInstance(type, true) as IModEventHandler).ToList();
                    if (Core is IModEventHandler core) {
                        _eventHandlers.Add(core);
                    }
                    _eventHandlers.Sort((x, y) => x.Priority - y.Priority);

                    process.Log("Raising events: OnEnable()");
                    for (int i = 0; i < _eventHandlers.Count; i++) {
                        _eventHandlers[i].HandleModEnable();
                    }
                }
                catch (Exception e) {
                    Error(e);
                    Disable(modEntry, true);
                    throw;
                }

                process.Log("Enabled.");
            }
        }

        public void Disable(UnityModManager.ModEntry modEntry, bool unpatch = false) {
            _logger = modEntry.Logger;

            using (ProcessLogger process = new ProcessLogger(_logger)) {
                process.Log("Disabling.");

                Enabled = false;

                // use try-catch to prevent the progression being disrupt by exceptions
                if (_eventHandlers != null) {
                    process.Log("Raising events: OnDisable()");
                    for (int i = _eventHandlers.Count - 1; i >= 0; i--) {
                        try { _eventHandlers[i].HandleModDisable(); }
                        catch (Exception e) { Error(e); }
                    }
                    _eventHandlers = null;
                }

                if (unpatch) {
                    Harmony harmonyInstance = new Harmony(modEntry.Info.Id);
                    foreach (MethodBase method in harmonyInstance.GetPatchedMethods().ToList()) {
                        Patches patchInfo = Harmony.GetPatchInfo(method);
                        IEnumerable<Patch> patches =
                            patchInfo.Transpilers.Concat(patchInfo.Postfixes).Concat(patchInfo.Prefixes)
                            .Where(patch => patch.owner == modEntry.Info.Id);
                        if (patches.Any()) {
                            process.Log($"Unpatching: {patches.First().PatchMethod.DeclaringType.FullName} from {method.DeclaringType.FullName}.{method.Name}");
                            foreach (Patch patch in patches) {
                                try { harmonyInstance.Unpatch(method, patch.PatchMethod); }
                                catch (Exception e) { Error(e); }
                            }
                        }
                    }
                    Patched = false;
                }

                modEntry.OnSaveGUI -= HandleSaveGUI;
                Core = null;
                Settings = null;
                Version = null;
                _logger = null;

                process.Log("Disabled.");
            }
        }

        #endregion

        #region Settings

        public void ResetSettings() {
            if (Enabled) {
                Settings = new TSettings();
            }
        }

        private void HandleSaveGUI(UnityModManager.ModEntry modEntry) {
            UnityModManager.ModSettings.Save(Settings, modEntry);
        }

        #endregion

        #region Loggers

        public void Critical(string str) {
            _logger.Critical(str);
        }

        public void Critical(object obj) {
            _logger.Critical(obj?.ToString() ?? "null");
        }

        public void Error(Exception e) {
            _logger.Error($"{e.Message}\n{e.StackTrace}");
            if (e.InnerException != null)
                Error(e.InnerException);
        }

        public void Error(string str) {
            _logger.Error(str);
        }

        public void Error(object obj) {
            _logger.Error(obj?.ToString() ?? "null");
        }

        public void Log(string str) {
            _logger.Log(str);
        }

        public void Log(object obj) {
            _logger.Log(obj?.ToString() ?? "null");
        }

        public void Warning(string str) {
            _logger.Warning(str);
        }

        public void Warning(object obj) {
            _logger.Warning(obj?.ToString() ?? "null");
        }

        [Conditional("DEBUG")]
        public void Debug(MethodBase method, params object[] parameters) {
            _logger.Log($"{method.DeclaringType.Name}.{method.Name}({string.Join(", ", parameters)})");
        }

        [Conditional("DEBUG")]
        public void Debug(string str) {
            _logger.Log(str);
        }

        [Conditional("DEBUG")]
        public void Debug(object obj) {
            _logger.Log(obj?.ToString() ?? "null");
        }

        #endregion

        private class ProcessLogger : IDisposable {
            Stopwatch _stopWatch = new Stopwatch();
            UnityModManager.ModEntry.ModLogger _logger;

            public ProcessLogger(UnityModManager.ModEntry.ModLogger logger) {
                _logger = logger;
                _stopWatch.Start();
            }

            public void Dispose() {
                _stopWatch.Stop();
            }

            [Conditional("DEBUG")]
            public void Log(string status) {
                _logger.Log($"[{_stopWatch.Elapsed:ss\\.ff}] {status}");
            }
        }
    }
}