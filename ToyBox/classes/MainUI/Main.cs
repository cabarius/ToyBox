// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games

using HarmonyLib;
using Kingmaker;
using Kingmaker.Utility;
using ModKit;
using ModKit.Utility;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Reflection;
using ToyBox.Multiclass;
using UnityEngine;
using UnityModManagerNet;
using GL = UnityEngine.GUILayout;
using Logger = ModKit.Logger;

namespace ToyBox
{
#if DEBUG
    [EnableReloading]
#endif
    static class Main
    {
        static Harmony HarmonyInstance;

        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");

        static string modId;

        public static UnityModManager.ModEntry modEntry;

        public static Settings settings;

        public static Mod multiclassMod;

        public static bool Enabled;

        public static bool freshlyLaunched = true;

        public static bool IsInGame => Game.Instance.Player?.Party.Any() ?? false;

        static Exception caughtException;

        public static void Log(string s)
        {
            if (modEntry != null)
            {
                Logger.Log(s);
            }
        }

        public static void Log(int indent, string s)
        {
            Log("    ".Repeat(indent) + s);
        }

        public static void Debug(string s)
        {
            if (modEntry != null)
            {
                Logger.ModLoggerDebug(s);
            }
        }

        public static void Error(Exception e)
        {
            if (modEntry != null)
            {
                Logger.Log(e);
            }
        }

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
#if DEBUG
                modEntry.OnUnload = Unload;
#endif
                modId = modEntry.Info.Id;
                Logger.modLogger = modEntry.Logger;
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                Logger.modEntryPath = modEntry.Path;

                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                multiclassMod = new Mod();
            }
            catch (Exception e)
            {
                Error(e);

                throw e;
            }

            return true;
        }
#if DEBUG
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            HarmonyInstance.UnpatchAll(modId);

            return true;
        }
#endif
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;

            return true;
        }

        static void ResetSearch()
        {
            BlueprintBrowser.ResetSearch();
        }

        static void ResetGUI(UnityModManager.ModEntry modEntry)
        {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.searchText = "";
            settings.searchLimit = 100;
            CheapTricks.ResetGUI();
            LevelUp.ResetGUI();
            PartyEditor.ResetGUI();
            CharacterPicker.ResetGUI();
            BlueprintBrowser.ResetGUI();
            QuestEditor.ResetGUI();
            caughtException = null;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Main.modEntry = modEntry;

            if (!Enabled)
            {
                return;
            }

            if (!IsInGame)
            {
                UI.Label("ToyBox has limited functionality from the main menu".yellow().bold());
            }

            try
            {
                Event e = Event.current;
                UI.userHasHitReturn = (e.keyCode == KeyCode.Return);
                UI.focusedControlName = GUI.GetNameOfFocusedControl();

                if (caughtException != null)
                {
                    UI.Label("ERROR".red().bold() + $": caught exception {caughtException}");
                    UI.ActionButton("Reset".orange().bold(), () => ResetGUI(modEntry), UI.AutoWidth());

                    return;
                }

                UI.TabBar(ref settings.selectedTab,
                          () =>
                          {
                              if (BlueprintLoader.Shared.IsLoading)
                              {
                                  UI.Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                              }
                              else { UI.Space(25); }
                          },
                          new NamedAction("Cheap Tricks", CheapTricks.OnGUI),
#if DEBUG
                          new NamedAction("Level Up & Multiclass", LevelUp.OnGUI),
#else
                          new NamedAction("Level Up", () => { LevelUp.OnGUI(); }),
#endif
                          new NamedAction("Party Editor", PartyEditor.OnGUI),
                          new NamedAction("Search 'n Pick", () => BlueprintBrowser.OnGUI()),
                          new NamedAction("Quest Editor", QuestEditor.OnGUI)
                );
            }
            catch (Exception e)
            {
                Console.Write($"{e}");
                caughtException = e;
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }
}