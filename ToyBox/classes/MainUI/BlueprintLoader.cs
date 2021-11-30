// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using ModKit;
using System;

namespace ToyBox {

    public class BlueprintLoader : MonoBehaviour {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private LoadBlueprintsCallback callback;
        private List<SimpleBlueprint> _blueprintsInProcess;
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
        private IEnumerator coroutine;
        private void UpdateProgress(int loaded, int total) {
            if (total <= 0) {
                progress = 0.0f;
                return;
            }
            progress = (float)loaded / (float)total;
        }

        internal readonly HashSet<string> badBlueprints = new() { "ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a", "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082" };

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
                }
                catch {
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
                if (Shared.IsLoading) { return null; }
                else {
                    Mod.Debug($"calling BlueprintLoader.Load");
                    Shared.Load((bps) => {
                        _blueprintsInProcess = bps.ToList();
                        blueprints = _blueprintsInProcess;
                        Mod.Debug($"success got {bps.Count()} bluerints");
                    });
                    return null;
                }
            }
            return blueprints;
        }
        public List<BPType> GetBlueprints<BPType>() {
            var bps = GetBlueprints();
            return bps?.OfType<BPType>().ToList() ?? null;
        }
    }

    public static class BlueprintLoader<BPType> {
        public static IEnumerable<BPType> blueprints = null;
    }

    public static class BlueprintLoaderOld {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private static AssetBundleRequest LoadRequest;
        public static float progress = 0;
        public static void Load(LoadBlueprintsCallback callback) {
#if false
            var bundle = (AssetBundle)AccessTools.Field(typeof(ResourcesLibrary), "s_BlueprintsBundle").GetValue(null);
            Main.Log($"got bundle {bundle}");
            LoadRequest = bundle.LoadAllAssetsAsync<BlueprintScriptableObject>();
#endif
            var bundle = BundlesLoadService.Instance.RequestBundle(AssetBundleNames.BlueprintAssets);
            BundlesLoadService.Instance.LoadDependencies(AssetBundleNames.BlueprintAssets);
            LoadRequest = bundle.LoadAllAssetsAsync<object>();
            Mod.Trace($"created request {LoadRequest}");
            LoadRequest.completed += (asyncOperation) => {
                Mod.Trace($"completed request and calling completion - {LoadRequest.allAssets.Length} Assets ");
                callback(LoadRequest.allAssets.Cast<SimpleBlueprint>());
                LoadRequest = null;
            };
        }
        public static bool LoadInProgress() {
            if (LoadRequest != null) {
                progress = LoadRequest.progress;
                return true;
            }
            return false;
        }
    }
}
