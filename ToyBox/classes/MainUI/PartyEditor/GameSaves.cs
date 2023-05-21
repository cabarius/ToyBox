using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Kingmaker.EntitySystem.Persistence;
using ModKit.Utility;
using UnityEngine;
using UnityModManagerNet;
using static ModKit.UI;

namespace ToyBox {
    // To be clear this is an editor of your list of saves
    // ToyBox already takes care of the role of the actual save editor
    public static class GameSavesBrowser {
        public static Settings Settings => Main.Settings;
        private static Browser<SaveInfo, SaveInfo> savesBrowser = new(true, true);
        private static IEnumerable<SaveInfo> _allSaves = null;
        private static IEnumerable<SaveInfo> _currentSaves = null;
        public static string SearchKey(this SaveInfo info) =>
#if Wrath
            $"{info.Name
            }{info.Area.AreaName.ToString()
            }{info.Campaign.Title
            }{info.DlcCampaign.Campaign.Title
            }{info.Description
            }{info.FileName
            }";
#elif RT
            $"{info.Name
            }{info.Area.AreaName.ToString()
            }{info.Description
            }{info.FileName
            }";
#endif
        public static IComparable[] SortKey(this SaveInfo info) => new IComparable[] {
            info.PlayerCharacterName,
            info.GameSaveTime
        };

        public static void OnGUI() {
            var currentGameID = Game.Instance.Player.GameId;
            var saveManager = Game.Instance.SaveManager;

            Div(0, 25);
            HStack("Saves".localize(),
                   1,
                   () => {
                       Toggle("Auto load Last Save on launch".localize(), ref Settings.toggleAutomaticallyLoadLastSave, 500.width());
                       HelpLabel("Hold down shift during launch to bypass".localize());
                   },
                   () => Label($"Save ID: {currentGameID}"),
            () => { }
                );
            if (Main.IsInGame) {
                Div(50, 25);
                //var currentSave = Game.Instance.SaveManager.GetLatestSave();
                // TODO: add refresh
                if (_currentSaves == null || _allSaves == null) {
                    saveManager.UpdateSaveListIfNeeded(true);
                    _currentSaves = saveManager.Where(info => info?.GameId == currentGameID);
                    _allSaves = saveManager.Where(info => info != null);
                }
                using (VerticalScope()) {
                    savesBrowser.OnGUI(_currentSaves,
                                       () => _allSaves,
                                       info => info,
                                       info => info.SearchKey(),
                                       info => info.SortKey(),
                                       () => {
                                           Toggle("Show GameID", ref Settings.toggleShowGameIDs);
                                       },
                                       (info, _) => {
                                           var isCurrent = _currentSaves.Contains(info);
                                           var characterName = isCurrent ? info.PlayerCharacterName.orange() : info.PlayerCharacterName;
                                           Label(characterName, 400.width());
#if RT
                                           25.space();
                                           Label($"Level: {info.PlayerCharacterRank}");
#endif
                                           25.space();
                                           Label($"{info.Area.AreaName.StringValue()}".cyan(), 400.width());
                                           if (Settings.toggleShowGameIDs) {
                                               25.space();
                                               ClipboardLabel(info.GameId, 400.width());
                                           }
                                           25.space();
                                           HelpLabel(info.Name.ToString());
                                       },
                                       null,
                                       50,
                                       true,
                                       true,
                                       100,
                                       400,
                                       "",
                                       false
                        );
                }
            }

        }
    }
}
