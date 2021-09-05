// Copyright < 2021 > Narria(github user Cabarius) - License: MIT

using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox
{
    public class BlueprintLoader : MonoBehaviour
    {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private LoadBlueprintsCallback callback;

        private List<SimpleBlueprint> blueprints;

        public float progress;

        private static BlueprintLoader _shared;

        public static BlueprintLoader Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new GameObject().AddComponent<BlueprintLoader>();
                    DontDestroyOnLoad(_shared.gameObject);
                }

                return _shared;
            }
        }

        private IEnumerator coroutine;

        private void UpdateProgress(int loaded, int total)
        {
            if (total <= 0)
            {
                progress = 0.0f;

                return;
            }

            progress = loaded / (float)total;
        }

        private IEnumerator LoadBlueprints()
        {
            int loaded = 0;
            int total = 1;

            yield return null;

            var bpCache = ResourcesLibrary.BlueprintsCache;

            while (bpCache == null)
            {
                yield return null;

                bpCache = ResourcesLibrary.BlueprintsCache;
            }

            blueprints = new List<SimpleBlueprint>();
            var toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;

            var allGUIDs = new List<BlueprintGuid>();

            foreach (var key in toc.Keys)
            {
                allGUIDs.Add(key);
            }

            total = allGUIDs.Count;
            UpdateProgress(loaded, total);

            foreach (var guid in allGUIDs)
            {
                var bp = bpCache.Load(guid);
                blueprints.Add(bp);
                loaded += 1;
                UpdateProgress(loaded, total);

                if (loaded % 1000 == 0)
                {
                    yield return null;
                }
            }

            Main.Log($"loaded {blueprints.Count} blueprints");
            this.callback(blueprints);

            yield return null;

            StopCoroutine(coroutine);
            coroutine = null;
        }

        public void Load(LoadBlueprintsCallback callback)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
            }

            this.callback = callback;
            coroutine = LoadBlueprints();
            StartCoroutine(coroutine);
        }

        public bool IsLoading => coroutine != null;
    }

    public static class BlueprintLoaderOld
    {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);

        private static AssetBundleRequest LoadRequest;

        public static float progress;

        public static void Load(LoadBlueprintsCallback callback)
        {
            var bundle = BundlesLoadService.Instance.RequestBundle(AssetBundleNames.BlueprintAssets);
            BundlesLoadService.Instance.LoadDependencies(AssetBundleNames.BlueprintAssets);
            LoadRequest = bundle.LoadAllAssetsAsync<object>();
            Main.Log($"created request {LoadRequest}");

            LoadRequest.completed += asyncOperation =>
                                     {
                                         Main.Log($"completed request and calling completion - {LoadRequest.allAssets.Length} Assets ");
                                         callback(LoadRequest.allAssets.Cast<SimpleBlueprint>());
                                         LoadRequest = null;
                                     };
        }

        public static bool LoadInProgress()
        {
            if (LoadRequest != null)
            {
                progress = LoadRequest.progress;

                return true;
            }

            return false;
        }
    }
}