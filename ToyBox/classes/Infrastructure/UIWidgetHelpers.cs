using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityModManagerNet;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.Localization;
using Kingmaker.Utility;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using static ToyBox.UIHelpers;

namespace ToyBox {
    public static partial class UIHelpers {
        public static WidgetPaths_1_0 WidgetPaths;
        public static Transform Settings => SceneManager.GetSceneByName("UI_LoadingScreen_Scene").GetRootGameObjects().FirstOrDefault(gameObject => gameObject.name.StartsWith("CommonPCView")).ChildTransform("Canvas/SettingsView");
        public static Transform SaveLoadScreen => SceneManager.GetSceneByName("UI_LoadingScreen_Scene").GetRootGameObjects().FirstOrDefault(gameObject => gameObject.name.StartsWith("CommonPCView")).ChildTransform("FadeCanvas/SaveLoadView");
        public static Transform UIRoot => throw new NotImplementedException(); // StaticCanvas.Instance.transform;
        public static Transform ServiceWindow => UIRoot.Find("ServiceWindowsPCView");
        // We deal with two different cases for finding our UI bits (thanks Owlcat!)
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView
        // GlobalMapPCView(Clone)/StaticCanvas/ServiceWindowsConfig
        public static Transform SearchViewPrototype => throw new NotImplementedException();
        // MainMenuPCView/Canvas/ChargenPCView/ContentWrapper/DetailedViewZone/ChargenFeaturesDetailedPCView/FeatureSelectorPlace/FeatureSelectorView/FeatureSearchView/ FieldPlace/SearchField
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/ParentThing/Equipment/RightBlock/FeatureSelectorView(Clone)/FeatureSearchView/ FieldPlace/SearchField


        public static Transform SpellbookScreen => ServiceWindow.Find(WidgetPaths.SpellScreen);
        public static Transform MythicInfoView => ServiceWindow.Find(WidgetPaths.MythicView);
        public static Transform EncyclopediaView => ServiceWindow.Find(WidgetPaths.EncyclopediaView);

        public static Transform CharacterScreen => ServiceWindow.Find(WidgetPaths.CharacterScreen);

        public static Transform InventoryScreen => ServiceWindow.Find(WidgetPaths.InventoryScreen);
        public static Transform LocalMapScreen => ServiceWindow.Find(WidgetPaths.LocalMapScreen);

        public class WidgetPaths_1_0 {
            public virtual string SpellScreen => "SpellbookView/SpellbookScreen";
            public virtual string MythicView => "MythicInfoView";
            public virtual string EncyclopediaView => "EncyclopediaView";

            public virtual string CharacterScreen => "CharacterInfoView/CharacterScreen";
            // If we ever need to support old stuff then put something for the following
            public virtual string InventoryScreen => throw new NotImplementedException();
            public virtual string LocalMapScreen => throw new NotImplementedException();
        }

        class WidgetPaths_1_1 : WidgetPaths_1_0 {
            public override string SpellScreen => "SpellbookPCView/SpellbookScreen";
            public override string MythicView => "MythicInfoPCView";
            public override string EncyclopediaView => "EncyclopediaPCView";
            public override string CharacterScreen => "CharacterInfoPCView/CharacterScreen";
        }

        class WidgetPaths_1_2 : WidgetPaths_1_1 { }

        class WidgetPaths_1_4 : WidgetPaths_1_2 {
            public override string SpellScreen => "Background/Windows/SpellbookPCView/SpellbookScreen";
            public override string MythicView => "Background/Windows/MythicInfoPCView";
            public override string EncyclopediaView => "Background/Windows/EncyclopediaPCView";
            public override string CharacterScreen => "Background/Windows/CharacterInfoPCView/CharacterScreen";
        }

        class WidgetPaths_2_0 : WidgetPaths_1_4 {
            public override string InventoryScreen => "Background/Windows/InventoryPCView";
            public override string LocalMapScreen => "Background/Windows/LocalMapPCView";
        }

        public static void OnLoad() {
            if (UnityModManager.gameVersion.Major == 2) {
                UIHelpers.WidgetPaths = new WidgetPaths_2_0();
            } else if (UnityModManager.gameVersion.Major == 1) {

                if (UnityModManager.gameVersion.Minor == 4)
                    UIHelpers.WidgetPaths = new WidgetPaths_1_4();
                else if (UnityModManager.gameVersion.Minor == 3)
                    UIHelpers.WidgetPaths = new WidgetPaths_1_2();
                else if (UnityModManager.gameVersion.Minor == 2)
                    UIHelpers.WidgetPaths = new WidgetPaths_1_2();
                else if (UnityModManager.gameVersion.Minor == 1)
                    UIHelpers.WidgetPaths = new WidgetPaths_1_1();
                else
                    UIHelpers.WidgetPaths = new WidgetPaths_1_0();
            }
        }
    }
}