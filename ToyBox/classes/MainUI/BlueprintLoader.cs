// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using ModKit;

namespace ToyBox {
    public class BlueprintLoader : MonoBehaviour {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private LoadBlueprintsCallback callback;
        private List<SimpleBlueprint> blueprints;
        public float progress = 0;
        private static BlueprintLoader _shared;
        public static BlueprintLoader Shared {
            get {
                if (_shared == null) {
                    _shared = new GameObject().AddComponent<BlueprintLoader>();
                    UnityEngine.Object.DontDestroyOnLoad(_shared.gameObject);
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
        private IEnumerator LoadBlueprints() {
            yield return null;
            var bpCache = ResourcesLibrary.BlueprintsCache;
            while (bpCache == null) {
                yield return null;
                bpCache = ResourcesLibrary.BlueprintsCache;
            }
            blueprints = new List<SimpleBlueprint> { };
            var toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            while (toc == null) {
                yield return null;
                toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
#if true    // TODO - Truinto for evaluation; my result improved from 2689 to 17 milliseconds
            var loaded = 0;
            var total = 1;
            var allGUIDs = new List<BlueprintGuid> { };
            foreach (var key in toc.Keys) {
                allGUIDs.Add(key);
            }
            total = allGUIDs.Count;
            UpdateProgress(loaded, total);
            foreach (var guid in allGUIDs) {
                SimpleBlueprint bp;
                try {
                    bp = bpCache.Load(guid);
                }
                catch {
                    Mod.Warning($"cannot load GUID: {guid}");
                    continue;
                }
                blueprints.Add(bp);
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

            Mod.Debug($"loaded {blueprints.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            callback(blueprints);
            yield return null;
            StopCoroutine(coroutine);
            coroutine = null;
        }
        public void Load(LoadBlueprintsCallback callback) {
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
