using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityModManagerNet;

namespace ModBase
{
    public interface IModEventHandler
    {
        void HandleModEnable();

        void HandleModDisable();
    }

    public interface IMod {
        void Patch();
        void UnPatch();
    }
    public class Core<TMod, TSettings>
        where TMod : IMod, new()
        where TSettings : UnityModManager.ModSettings, new()
    {
        #region Fields & Properties

        private UnityModManager.ModEntry.ModLogger _logger;
        private Assembly _assembly;

        private List<IModEventHandler> _eventHandler;

        public TMod Mod { get; private set; }

        public TSettings Settings { get; private set; }

        public bool Enabled { get; private set; }

        public bool Patched { get; private set; }

        #endregion

        public Core(UnityModManager.ModEntry modEntry, Assembly assembly)
        {
            _logger = modEntry.Logger;
            _assembly = assembly;
        }

        #region Toggle

        public void Enable(UnityModManager.ModEntry modEntry)
        {
            DateTime startTime = DateTime.Now;
            Debug($"[{DateTime.Now - startTime:ss':'ff}] Enabling.");

            //try
            //{
                Debug($"[{DateTime.Now - startTime:ss':'ff}] Loading settings.");
                Settings = UnityModManager.ModSettings.Load<TSettings>(modEntry);
                Mod = new TMod();

                modEntry.OnSaveGUI += HandleSaveGUI;

                // patchcing
                if (!Patched)
                {
                    Mod.Patch();
                }
                Patched = true;

                // register events
                Debug($"[{DateTime.Now - startTime:ss':'ff}] Registering events.");
                _eventHandler = _assembly.GetTypes()
                    .Where(type => !type.IsInterface && !type.IsAbstract && typeof(IModEventHandler).IsAssignableFrom(type))
                    .Select(handler => Activator.CreateInstance(handler, true) as IModEventHandler).ToList();

                Enabled = true;

                Debug($"[{DateTime.Now - startTime:ss':'ff}] Raising events: 'OnEnable'");
                foreach (IModEventHandler handler in _eventHandler)
                    handler.HandleModEnable();
            //}
            //catch (Exception e)
            //{
                //Disable(modEntry, true);
            //    throw e;
            //}

            Debug($"[{DateTime.Now - startTime:ss':'ff}] Enabled.");
        }

        public void Disable(UnityModManager.ModEntry modEntry, bool unpatch = false)
        {
            DateTime startTime = DateTime.Now;
            Debug($"[{DateTime.Now - startTime:ss':'ff}] Disabling.");

            // using try-catch to prevent the progression being disrupt by exceptions
            try
            {
                if (Enabled && _eventHandler != null)
                {
                    Debug($"[{DateTime.Now - startTime:ss':'ff}] Raising events: 'OnDisable'");
                    foreach (IModEventHandler handler in _eventHandler)
                        handler.HandleModDisable();
                }
            }
            catch (Exception e)
            {
                Error(e.ToString());
            }

            _eventHandler = null;

            if (unpatch)
            {
                /*
                HarmonyInstance harmonyInstance = HarmonyInstance.Create(modEntry.Info.Id);
                foreach (MethodBase method in harmonyInstance.GetPatchedMethods().ToList())
                {
                    Patches patchInfo = harmonyInstance.GetPatchInfo(method);
                    List<Patch> patches = patchInfo.Transpilers.Concat(patchInfo.Postfixes).Concat(patchInfo.Prefixes)
                        .Where(patch => patch.owner == modEntry.Info.Id).ToList();
                    if (patches.Any())
                    {
                        Debug($"[{DateTime.Now - startTime:ss':'ff}] Unpatching: {patches.First().patch.DeclaringType.DeclaringType?.Name}.{method.DeclaringType.Name}.{method.Name}");
                        foreach (Patch patch in patches)
                        {
                            try
                            {
                                harmonyInstance.Unpatch(method, patch.patch);
                            }
                            catch (Exception e)
                            {
                                Error(e.ToString());
                            }
                        }
                    }
                }
                */
                Mod.UnPatch();
                Patched = false;
            }

            modEntry.OnSaveGUI -= HandleSaveGUI;

            Mod = default;
            Settings = null;

            Enabled = false;

            Debug($"[{DateTime.Now - startTime:ss':'ff}] Disabled.");
        }

        #endregion

        #region Event Handlers

        private void HandleSaveGUI(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save(Settings, modEntry);
        }

        #endregion

        #region Loggers

        public void Critical(string str)
        {
            _logger.Critical(str);
        }

        public void Critical(object obj)
        {
            _logger.Critical(obj?.ToString() ?? "null");
        }

        public void Error(string str)
        {
            _logger.Error(str);
        }

        public void Error(object obj)
        {
            _logger.Error(obj?.ToString() ?? "null");
        }

        public void Log(string str)
        {
            _logger.Log(str);
        }

        public void Log(object obj)
        {
            _logger.Log(obj?.ToString() ?? "null");
        }

        public void Warning(string str)
        {
            _logger.Warning(str);
        }

        public void Warning(object obj)
        {
            _logger.Warning(obj?.ToString() ?? "null");
        }

        [Conditional("DEBUG")]
        public void Debug(string str)
        {
            _logger.Log(str);
        }

        [Conditional("DEBUG")]
        public void Debug(object obj)
        {
            _logger.Log(obj?.ToString() ?? "null");
        }

        #endregion
    }
}
