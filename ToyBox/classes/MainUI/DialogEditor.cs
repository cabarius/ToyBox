// Copyright < 2023 >  - Narria (github user Cabarius) - License: MIT
using UnityEngine;
using HarmonyLib;
using System;
using System.Linq;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using static ModKit.UI;
using ModKit.DataViewer;
using System.Collections.Generic;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.UnitLogic.Parts;
using ModKit.Utility;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;
using static ToyBox.BlueprintExtensions;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using System.Security.AccessControl;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.UI;

namespace ToyBox {
    public static class DialogEditor {
        public static Settings Settings => Main.Settings;
        public static Player player => Game.Instance.Player;

        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            if (Game.Instance?.DialogController is { } dialogController) {
                ReflectionTreeView.DetailToggle("Inspect Dialog Controller", dialogController);
                ReflectionTreeView.OnDetailGUI(dialogController);
                dialogController.CurrentCue.OnGUI();
            }
        }

        public static void OnGUI(this Dialog dialog) {

        }

        public static void OnGUI(this BlueprintCueBase cue) {
            
        }

        public static void OnGUI(this BlueprintAnswer answer) {
            
        }

        public static void OnGUI(this BlueprintAnswersList answersList) {
            
        }
    }
}
