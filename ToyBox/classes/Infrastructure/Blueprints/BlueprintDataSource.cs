using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.BundlesLoading;
using ModKit;
using Kingmaker.Blueprints.Facts;
using System.Web.Caching;

namespace ToyBox {
    using BlueprintGuid = String;
    internal class BlueprintDataSource : DataSource<SimpleBlueprint> {
        private HashSet<BlueprintGuid> _allGUIDS;
        internal readonly HashSet<string> badBlueprints = new() { "ce0842546b73aa34b8fcf40a970ede68", "2e3280bf21ec832418f51bee5136ec7a", "b60252a8ae028ba498340199f48ead67", "fb379e61500421143b52c739823b4082" };
        protected override void LoadData() {
            var toc = ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints;
            if (toc == null) {
                Stop();
                return;
            }
            var bpCache = ResourcesLibrary.BlueprintsCache;
            var blueprintsInProcess = new List<Entry> { };
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var loaded = 0;
            var total = 1;
            var allGUIDs = toc.AsEnumerable().OrderBy(e => e.Value.Offset);
            total = allGUIDs.Count();
            Mod.Log($"Loading {total} Blueprints");
            UpdateProgress(blueprintsInProcess, total);
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
                blueprintsInProcess.Add(Transformer(bp));
                loaded += 1;
                if (loaded % 1000 == 0) {
                    UpdateProgress(blueprintsInProcess, total);
                    blueprintsInProcess.Clear();
                }
            }
            UpdateProgress(blueprintsInProcess, total, true);
            blueprintsInProcess.Clear();
        }
    }
}
