using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityModManagerNet;

namespace ModMaker {
    public class BlueprintLibrary {
        //维护一个独立于游戏本体ResourcesLibrary的全体blueprint的集合
        //Mod可以在此处进行游戏本体中已存在的blueprint的修改，从而避免被游戏本体的垃圾回收机制覆盖了修改
        public Dictionary<string, BlueprintScriptableObject> BlueprintByAssetId;
        public List<BlueprintScriptableObject> AllBlueprints;
        List<string> ModifiedBlueprintIds;
        List<string> NewBlueprintIds;
        public List<string> OldBlueprintIds;

        public BlueprintLibrary() {
            this.BlueprintByAssetId = new Dictionary<string, BlueprintScriptableObject>();
            this.AllBlueprints = new List<BlueprintScriptableObject>();
            this.ModifiedBlueprintIds = new List<string>();
            this.NewBlueprintIds = new List<string>();
            this.OldBlueprintIds = new List<string>();
        }
        public void LoadBlueprints() {
            AssetBundle bundle = typeof(ResourcesLibrary).GetField("s_BlueprintsBundle", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as AssetBundle;
            this.AllBlueprints = new List<BlueprintScriptableObject>(bundle.LoadAllAssets<BlueprintScriptableObject>());
            foreach (BlueprintScriptableObject blueprint in this.AllBlueprints) {
                this.BlueprintByAssetId[blueprint.AssetGuid] = blueprint;
                this.OldBlueprintIds.Add(blueprint.AssetGuid);
            }
        }
        public bool IsModifiedBlueprint(BlueprintScriptableObject blueprint) {
            return this.IsModifiedBlueprint(blueprint.AssetGuid);
        }
       
        public bool IsModifiedBlueprint(string assetId) {
            /*
            UnityModManagerNet.UnityModManager.Logger.Log("[[Modified]]");
            foreach (var guid in this.ModifiedBlueprintIds) {
                UnityModManagerNet.UnityModManager.Logger.Log($"{guid}: {this.BlueprintByAssetId[guid]}");
            }
            */
            //UnityModManagerNet.UnityModManager.Logger.Log($"{assetId} modified = {this.ModifiedBlueprintIds.Contains(assetId)}");
            return this.ModifiedBlueprintIds.Contains(assetId);
        }

        public bool IsNewBlueprint(string assetId) {
            return this.NewBlueprintIds.Contains(assetId);
        }

        public bool IsNewBlueprint(BlueprintScriptableObject blueprint) {
            return this.IsNewBlueprint(blueprint.AssetGuid);
        }

        internal bool isValidGuid(string s) {
            return s == null ? false : Regex.Match(s, "[0-9a-fA-F]{16}").Success;
        }
        public T GetAsset<T>(string guid) where T : BlueprintScriptableObject {
            BlueprintScriptableObject ret = null;
            this.BlueprintByAssetId.TryGetValue(guid, out ret);
            if(ret != null && !IsNewBlueprint(ret) && !IsModifiedBlueprint(ret)) {
                this.ModifiedBlueprintIds.Add(guid);
            }
            return ret as T;
        }

        public T Get<T>(string guid) where T : BlueprintScriptableObject {
            return this.GetAsset<T>(guid);
        }
        public void AddAsset<T>(T obj, string guid = null) where T : BlueprintScriptableObject {
            if (!this.isValidGuid(obj.AssetGuid) && this.isValidGuid(guid)) { 
                AccessTools.Field(obj.GetType(), "m_AssetGuid").SetValue(obj, guid);
            }
            else {
                if(guid != null && obj.AssetGuid != guid) {
                    throw new NotSupportedException($"Guid mismatch in Addasset (asset name = {obj.name} type = {obj.GetType()}");
                }
            }
            if (!this.isValidGuid(obj.AssetGuid)) {
                throw new NotSupportedException($"Invalid guid in AddAsset (asset name = {obj.name}, type = {obj.GetType()})!");
            }
            else {
                UnityModManager.Logger.Log($"BlueprintLibrary添加Blueprint {obj.AssetGuid}");
                BlueprintScriptableObject existing;
                if(this.BlueprintByAssetId.TryGetValue(obj.AssetGuid, out existing)) {
                    throw new NotSupportedException($"Asset w/ guid {obj.AssetGuid} is existed!");
                }
                this.BlueprintByAssetId[obj.AssetGuid] = obj;
                this.AllBlueprints.Add(obj);
                this.NewBlueprintIds.Add(obj.AssetGuid);
            }
        }
    }
}
