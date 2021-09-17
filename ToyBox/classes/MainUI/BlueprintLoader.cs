// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.BundlesLoading;
using System.IO;

namespace ToyBox {
    public class BlueprintLoader : MonoBehaviour {
        public delegate void LoadBlueprintsCallback(IEnumerable<SimpleBlueprint> blueprints);
        LoadBlueprintsCallback callback;
        List<SimpleBlueprint> blueprints;
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
            int loaded = 0;
            int total = 1;
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
#if false    // TODO - Truinto for evaluation; my result improved from 2689 to 17 milliseconds
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
                    Main.Log($"cannot load GUID: {guid}");
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

            Main.Log($"loaded {blueprints.Count} blueprints in {watch.ElapsedMilliseconds} milliseconds");
            this.callback(blueprints);
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

        static AssetBundleRequest LoadRequest;
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
            Main.Log($"created request {LoadRequest}");
            LoadRequest.completed += (asyncOperation) => {
                Main.Log($"completed request and calling completion - {LoadRequest.allAssets.Length} Assets ");
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
