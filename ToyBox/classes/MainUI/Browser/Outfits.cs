using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Visual.CharacterSystem;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public class Outfits {
        public static Browser<KingmakerEquipmentEntity, KingmakerEquipmentEntity> OutfitsBrowser = new(true, true);
        public static List<KingmakerEquipmentEntity> availableOutfits;
        public static List<KingmakerEquipmentEntity> equippedOutfits;
        public static Settings settings => Main.Settings;
        public static bool isInit = false;
        public static bool showCharacterFilterCategories = false;
        public static BaseUnitEntity ch;
        public static bool needReInit = false;
        public static void OnGUI() {
            bool justInit = false;
            if (!Main.IsInGame) {
                UI.Label("Outfits not available until you load a save.".localize().yellow().bold());
                return;
            }
            if (needReInit && Event.current.type == EventType.Layout) {
                needReInit = false;
                isInit = false;
                OutfitsBrowser.ReloadData();
            }
            using (HorizontalScope()) {
                50.space();
                using (VerticalScope()) {
                    Toggle("Show Character filter choices".localize(), ref showCharacterFilterCategories);
                    if (showCharacterFilterCategories) {
                        CharacterPicker.OnFilterPickerGUI();
                    }
                    CharacterPicker.OnCharacterPickerGUI();
                    var tmp = CharacterPicker.GetSelectedCharacter();
                    if (tmp != ch) {
                        ch = tmp;
                        needReInit = true;
                    }
                }
            }
            if (ch == null) ch = Shodan.MainCharacter;
            if (!Main.Settings.perSave.doOverrideOutfit.TryGetValue(ch.HashKey(), out var valuePair)) {
                valuePair = new(false, new());
            }
            var tmpOverride = valuePair.Item1;
            if (Toggle($"Override outfit settings for character {ch}?", ref tmpOverride)) {
                Main.Settings.perSave.doOverrideOutfit[ch.HashKey()] = new(tmpOverride, valuePair.Item2);
                Settings.SavePerSaveSettings();
            }
            if (tmpOverride) {
                if (!isInit) {
                    availableOutfits = BlueprintLoader.Shared.GetBlueprints<KingmakerEquipmentEntity>();
                    if (availableOutfits != null && availableOutfits?.Count > 0) {
                        if (valuePair.Item2.Count == 0) {
                            equippedOutfits = availableOutfits.Where(bp => {
                                if (ch.IsInGame && ch.View != null && ch.View.CharacterAvatar != null) {
                                    IEnumerable<EquipmentEntity> enumerable = bp.Load(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                                    foreach (var ee in enumerable) {
                                        if (ch?.View?.CharacterAvatar?.EquipmentEntities?.Contains(ee) ?? false) {
                                            return true;
                                        }
                                    }
                                }
                                return false;
                            })?.ToList();
                            if (equippedOutfits == null) equippedOutfits = new();
                            Main.Settings.perSave.doOverrideOutfit[ch.HashKey()] = new(tmpOverride, equippedOutfits.Select(e => e.AssetGuid).ToList());
                            Settings.SavePerSaveSettings();
                        }
                        else {
                            ch.View.CharacterAvatar.RemoveAllEquipmentEntities();
                            BlueprintAction action = null;
                            BlueprintAction action2 = null;
                            foreach (var kee in availableOutfits) {
                                if (valuePair.Item2.Contains(kee.AssetGuid)) {
                                    if (action == null) {
                                        action = kee.GetActions().Where(a => a.name == "Dress".localize()).First();
                                    }
                                    if (action.canPerform(kee, ch)) {
                                        action.action(kee, ch);
                                    }
                                }
                                else {
                                    if (action2 == null) {
                                        action2 = kee.GetActions().Where(a => a.name == "Undress".localize()).First();
                                    }
                                    if (action2.canPerform(kee, ch)) {
                                        action2.action(kee, ch);
                                    }
                                }
                            }
                        }
                        justInit = true;
                    }
                }
                if (justInit) {
                    if (Event.current.type == EventType.Repaint) {
                        justInit = false;
                        isInit = true;
                    }
                    return;
                }
                if (isInit) {
                    OutfitsBrowser.OnGUI(equippedOutfits,
                    () => availableOutfits,
                    current => current,
                    kee => $"{kee.name} {kee.Comment}",
                    kee => new[] { kee.name },
                    () => {
                        using (VerticalScope()) {
                            Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                            Div(0, 25);
                        }
                    },
                    (kee, maybeKee) => {
                        var remainingWidth = ummWidth;
                        // Indent
                        remainingWidth -= 50;
                        var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
                        remainingWidth -= titleWidth;

                        var text = kee.name.MarkedSubstring(OutfitsBrowser.SearchText);
                        if (maybeKee != null) {
                            text = text.Cyan().Bold();
                        }
                        Label(text, Width((int)titleWidth));
                        var actions = kee.GetActions()
                            .Where(action => action.canPerform(kee, ch));
                        var actionsCount = actions.Count();
                        for (var ii = 0; ii < 4; ii++) {
                            if (ii < actionsCount) {
                                var action = actions.ElementAt(ii);
                                var actionName = action.name;
                                ActionButton(actionName, () => action.action(kee, ch, 1), Width(160));
                                Space(10);
                                remainingWidth -= 174.0f;

                            }
                            else {
                                Space(174);
                            }
                        }
                        remainingWidth -= 190;
                        Space(20); remainingWidth -= 20;
                        ReflectionTreeView.DetailToggle("", kee, kee, 0);
                        using (VerticalScope(Width(remainingWidth))) {
                            using (HorizontalScope(Width(remainingWidth))) {
                                if (settings.showAssetIDs) ClipboardLabel(kee.AssetGuid.ToString(), ExpandWidth(false));
                                if (!kee.Comment.IsNullOrEmpty()) {
                                    Label(kee.Comment.Green());
                                }
                            }
                        }
                    },
                    (kee, maybeKee) => {
                        ReflectionTreeView.OnDetailGUI(kee);
                    }, 50, false);
                }
            }
        }
    }
}