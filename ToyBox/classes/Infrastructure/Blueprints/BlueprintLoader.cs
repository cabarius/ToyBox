// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.BundlesLoading;
using ModKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public class BlueprintLoader : MonoBehaviour {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private LoadBlueprintsCallback callback;
        private List<SimpleBlueprint> _blueprintsInProcess;
        private List<SimpleBlueprint> blueprints;
        private HashSet<SimpleBlueprint> bpsToAdd = new();
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
        private IEnumerator coroutine;
        private void UpdateProgress(int loaded, int total) {
            if (total <= 0) {
                progress = 0.0f;
                return;
            }
            progress = (float)loaded / (float)total;
        }

        internal readonly HashSet<string> badBlueprints = new() { "ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a",
            "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082", "5d2b9742ce82457a9ae7209dce770071" };

        private IEnumerator LoadBlueprints() {
            yield return null;
            var bpCache = ResourcesLibrary.BlueprintsCache;
            while (bpCache == null) {
                yield return null;
                bpCache = ResourcesLibrary.BlueprintsCache;
            }
            _blueprintsInProcess = new List<SimpleBlueprint> { };
            var toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            while (toc == null) {
                yield return null;
                toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
#if true    // TODO - Truinto for evaluation; my result improved from 2689 to 17 milliseconds
            var loaded = 0;
            var total = 1;
            var allGUIDs = toc.AsEnumerable().OrderBy(e => e.Value.Offset);
            total = allGUIDs.Count();
            Mod.Log($"Loading {total} Blueprints");
            UpdateProgress(loaded, total);
            foreach (var entry in allGUIDs) {
                if (badBlueprints.Contains(entry.Key.ToString())) continue;
                SimpleBlueprint bp;
                try {
                    bp = bpCache.Load(entry.Key);
                } catch {
                    Mod.Warn($"cannot load GUID: {entry.Key}");
                    continue;
                }
                _blueprintsInProcess.Add(bp);
                loaded += 1;
                UpdateProgress(loaded, total);
                if (loaded % 1000 == 0) {
                    yield return null;
                }
            }
#else
            blueprints = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.Values.Select(s => s.Blueprint).ToList();
#endif
            watch.Stop();
            Mod.Log($"loaded {_blueprintsInProcess.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            callback(_blueprintsInProcess);
            yield return null;
            StopCoroutine(coroutine);
            coroutine = null;
        }
        private void Load(LoadBlueprintsCallback callback) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
                coroutine = null;
            }
            this.callback = callback;
            coroutine = LoadBlueprints();
            StartCoroutine(coroutine);
        }
        public bool IsLoading {
            get {
                if (coroutine != null) {
                    return true;
                }
                return false;
            }
        }

        public List<SimpleBlueprint> GetBlueprints() {
            if (blueprints == null) {
                if (Shared.IsLoading) { return null; } else {
                    Mod.Debug($"calling BlueprintLoader.Load");
                    Shared.Load((bps) => {
                        _blueprintsInProcess = bps.Concat(bpsToAdd).ToList();
                        bpsToAdd.Clear();
                        blueprints = _blueprintsInProcess;
                        Mod.Debug($"success got {bps.Count()} bluerints");
                    });
                    return null;
                }
            }
            if (bpsToAdd.Count > 0) {
                blueprints.AddRange(bpsToAdd);
                bpsToAdd.Clear();
            }
            return blueprints;
        }
        public List<BPType> GetBlueprints<BPType>() {
            var bps = GetBlueprints();
            return bps?.OfType<BPType>().ToList() ?? null;
        }
        internal IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<BlueprintGuid> guids) where BPType : BlueprintFact {
            var bps = GetBlueprints<BPType>();
            return bps?.Where(bp => guids.Contains(bp.AssetGuid));
        }
        public IEnumerable<BPType> GetBlueprintsByGuids<BPType>(IEnumerable<string> guids) where BPType : BlueprintFact => GetBlueprintsByGuids<BPType>(guids.Select(g => BlueprintGuid.Parse(g)));
        [HarmonyPatch(typeof(BlueprintsCache))]
        internal static class BlueprintLoaderPatches {
            [HarmonyPatch(nameof(BlueprintsCache.AddCachedBlueprint))]
            [HarmonyPostfix]
            internal static void AddCachedBlueprint(BlueprintGuid guid, SimpleBlueprint bp) {
                if (Shared.IsLoading || Shared.blueprints != null) {
                    Shared.bpsToAdd.Add(bp);
                }
            }
            [HarmonyPatch(nameof(BlueprintsCache.RemoveCachedBlueprint))]
            [HarmonyPostfix]
            internal static void RemoveCachedBlueprint(BlueprintGuid guid) {
                Shared.bpsToAdd.RemoveWhere(bp => bp.AssetGuid == guid);
            }
        }
    }

    public static class BlueprintLoader<BPType> {
        public static IEnumerable<BPType> blueprints = null;
    }
}
