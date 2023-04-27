// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using ModKit;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyBox {

    public class BlueprintLoader : MonoBehaviour {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private ConcurrentQueue<SimpleBlueprint> _blueprintsInProcess;
        private List<SimpleBlueprint> blueprints;
        //private List<SimpleBlueprint> blueprints;
        public float progress = 0;
        private static BlueprintLoader _shared;
        public static BlueprintLoader Shared {
            get {
                if (_shared == null) {
                    _shared = new GameObject().AddComponent<BlueprintLoader>();
                    DontDestroyOnLoad(_shared.gameObject);
                }
                return _shared;
            }
        }
        private CancellationTokenSource cancellationTokenSource;
        private void UpdateProgress(int loaded, int total) {
            if (total <= 0) {
                progress = 0.0f;
                return;
            }
            progress = (float)loaded / (float)total;
        }

        internal readonly HashSet<string> badBlueprints = new() { "ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a", "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082" };

        private void LoadBlueprints() {
            var bpCache = ResourcesLibrary.BlueprintsCache;
            while (bpCache == null) {
                bpCache = ResourcesLibrary.BlueprintsCache;
            }
            _blueprintsInProcess = new();
            var toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            while (toc == null) {
                toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var loaded = 0;
            var total = 1;
            var allGUIDs = toc.AsEnumerable().OrderBy(e => e.Value.Offset);
            total = allGUIDs.Count();
            Mod.Log($"Loading {total} Blueprints");
            UpdateProgress(loaded, total);
            foreach (var entry in allGUIDs) {
                if (cancellationTokenSource.IsCancellationRequested) {
                    return;
                }
                if (badBlueprints.Contains(entry.Key.ToString())) continue;
                SimpleBlueprint bp;
                try {
                    bp = bpCache.Load(entry.Key);
                } catch {
                    Mod.Warn($"cannot load GUID: {entry.Key}");
                    continue;
                }
                lock (_blueprintsInProcess) {
                    _blueprintsInProcess.Enqueue(bp);
                }
                loaded += 1;
                UpdateProgress(loaded, total);
            }
            watch.Stop();
            Mod.Log($"loaded {_blueprintsInProcess.Count + blueprints.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            cancellationTokenSource = null;
            Mod.OnShowGUI();
        }
        private void Load() {
            if (cancellationTokenSource != null) {
                cancellationTokenSource.Cancel();
            }
            else {
                do {
                    cancellationTokenSource = new();
                    Task.Run(() => LoadBlueprints());
                } while (cancellationTokenSource?.IsCancellationRequested ?? false);
            }
        }
        public bool IsLoading {
            get {
                if (cancellationTokenSource != null) {
                    return true;
                }
                return false;
            }
        }

        public List<SimpleBlueprint> GetBlueprints() {
            if (Shared.IsLoading) {
                blueprints ??= new();
                lock (_blueprintsInProcess) {
                    while (_blueprintsInProcess.Count > 0) {
                        SimpleBlueprint toAdd;
                        if (_blueprintsInProcess.TryDequeue(out toAdd)) {
                            blueprints.Add(toAdd);
                        }
                    }
                }
            }
            else {
                if (blueprints == null) {
                    Mod.Debug($"calling BlueprintLoader.Load");
                    Shared.Load();
                    return null;
                }
                else if (_blueprintsInProcess.Count > 0) {
                    while (_blueprintsInProcess.Count > 0) {
                        SimpleBlueprint toAdd;
                        if (_blueprintsInProcess.TryDequeue(out toAdd)) {
                            blueprints.Add(toAdd);
                        }
                    }
                    Mod.Debug($"success got {blueprints.Count()} bluerints");
                }
            }
            return blueprints;
        }
        public List<BPType> GetBlueprints<BPType>() {
            var bps = GetBlueprints();
            return bps?.OfType<BPType>().ToList() ?? null;
        }

        private IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<BlueprintGuid> guids) where BPType : BlueprintFact {
            var bps = GetBlueprints<BPType>();
            return bps?.Where(bp => guids.Contains(bp.AssetGuid));
        }

        public IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<string> guids) where BPType : BlueprintFact => GetBlueprintsByGuids<BPType>(guids.Select(g => BlueprintGuid.Parse(g)));
    }
}
