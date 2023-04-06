using System;
using UnityEngine;
using ModKit;
using System.Collections;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker;
using Kingmaker.Kingdom;
using System.Linq;
using Kingmaker.Armies;

namespace ModKit {
    public static partial class ui {
        public static class Size {
            public const float Automatic = -1;
            public const float Expands = float.PositiveInfinity;

        }
        public class View : IDisposable {
            public View background { get; set; } = null;
            public Color color { get; set; } = Color.clear;
            public float height { get; set; } = Size.Automatic;
            public float width { get; set; } = Size.Automatic;
            public RectOffset padding { get; set; } = new RectOffset();
            public void Dispose() => throw new NotImplementedException();
            public static explicit operator View(Color c) => new() { color = c };
        }
        public class Stack : View {
            public float spacing { get; set; } = 25;
        }

        public class HStack : Stack {

        }
        public class VStack : Stack {

        }
        public class List<ItemType> : Stack {
            public IEnumerable<ItemType> items;
            public void ForEach(Action<ItemType> action) { }
            public void ForEach(Action<ItemType, int> action) { }
        }

        public class Label : View {
            public string text { get; set; } = "";
            public Label(string t) { this.text = t; }
        }
    }
}

namespace ToyBox {
    // A place to play...
    public static class Playground {
        public static bool hasStartUp = false;
        public static Dictionary<int, bool> isInMercenaryPool = new();
        public static Dictionary<int, bool> isInRecruitPool = new();
        public static IEnumerable<BlueprintUnit> bps;

        public static void startUp() {
            bps = from unit in BlueprintExtensions.GetBlueprints<BlueprintUnit>()
                  where unit.NameSafe().StartsWith("Army")
                  select unit;
            IEnumerable<BlueprintUnit> recruitPool = from recruitable in KingdomState.Instance.RecruitsManager.Pool
                                                     select recruitable.Unit;
            foreach (var entry in bps) {
                isInRecruitPool[entry.GetHashCode()] = recruitPool.Contains(entry);
                isInMercenaryPool[entry.GetHashCode()] = KingdomState.Instance.MercenariesManager.HasUnitInPool(entry);
            }
        }
        public static void addAllCurrentUnits() {
            var playerArmies = from army in classes.MainUI.ArmiesEditor.ArmiesByDistanceFromPlayer()
                               where army.Item1.Data.Faction == ArmyFaction.Crusaders
                               select army;
            foreach (var army in playerArmies) {
                foreach (var squad in army.Item1.Data.Squads) {
                    var unit = squad.Unit.GetHashCode();
                    if (!isInMercenaryPool[unit] && !isInRecruitPool[unit]) {
                        KingdomState.Instance.MercenariesManager.AddMercenary(squad.Unit, 1);
                    }
                }
            }
        }
        public static bool discloseArmies = false;
        public static void OnGUI() {
            if (!hasStartUp) {
                Playground.startUp();
            }
            using (UI.VerticalScope()) {
                UI.Toggle("Should add Units from new armies to Mercenary units if not recruitable?", ref Main.settings.toggleAddNewUnitsAsMercenaries, UI.AutoWidth());
                UI.ActionButton("Add all current units that are neither recruitable nor Mercanries to Mercenary units", () => addAllCurrentUnits(), UI.AutoWidth());
                UI.DisclosureToggle("Show All Army Units", ref discloseArmies);
                if (discloseArmies) {
                    using (UI.HorizontalScope()) {
                        UI.Label("Unit Name", UI.Width(400));
                        UI.Label("Add/Remove to/from Mercenary Pool", UI.Width(250));
                        UI.Label("Is recruitable", UI.Width(100));
                        UI.Label("Mercenary Weight", UI.AutoWidth());
                    }
                    foreach (var entry in bps) {
                        using (UI.HorizontalScope()) {
                            UI.Label(entry.NameSafe().cyan(), UI.Width(400));
                            bool isPart = isInMercenaryPool[entry.GetHashCode()];
                            using (UI.HorizontalScope(UI.Width(250))) {
                                if (UI.Toggle("", ref isPart, "Remove".orange(), "Add".green(), 0, UI.textBoxStyle, GUI.skin.box, UI.AutoWidth())) {
                                    isInMercenaryPool[entry.GetHashCode()] = isPart;
                                    if (isPart) {
                                        KingdomState.Instance.MercenariesManager.AddMercenary(entry, 1);
                                    }
                                    else {
                                        KingdomState.Instance.MercenariesManager.RemoveMercenary(entry);
                                    }
                                }
                            }
                            UI.Label(isInRecruitPool[entry.GetHashCode()].ToString(), UI.Width(100));
                            if (!isInMercenaryPool[entry.GetHashCode()]) {
                                UI.Label("N/A", UI.AutoWidth());
                            }
                            else {
                                var res = KingdomState.Instance.MercenariesManager.Pool.FirstOrDefault(unit => unit.Unit == entry);
                                if (res != null) {
                                    var tmp = res.Weight;
                                    if (UI.LogSliderCustomLabelWidth("Weight", ref tmp, 0.01f, 1000, 1, 2, "", 50, UI.AutoWidth())) {
                                        res.UpdateWeight(tmp);
                                    }
                                }
                                else {
                                    UI.Label("Weird", UI.AutoWidth());
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


//                using (var stack = new UI.HStack { spacing = 25, color = RGBA.darkgrey.color() }) {
