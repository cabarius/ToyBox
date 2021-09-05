// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Utility;
using ModKit;
using System.Linq;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches
{
    static class Appearance
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static Player player = Game.Instance.Player;

        public static CustomizationOptions Concat(this CustomizationOptions a, CustomizationOptions b)
        {
            var c = new CustomizationOptions
            {
                Heads = a.Heads.Concat(b.Heads).ToArray(),
                Eyebrows = a.Eyebrows.Concat(b.Eyebrows).ToArray(),
                Hair = a.Hair.Concat(b.Hair).ToArray(),
                Beards = a.Beards.Concat(b.Beards).ToArray(),
                Horns = a.Horns.Concat(b.Horns).ToArray(),
                TailSkinColors = a.TailSkinColors.Concat(b.TailSkinColors).ToArray()
            };

            return c;
        }

        public class BlueprintAllRacesWrapper : BlueprintRace
        {
            BlueprintRace race;

            public static CustomizationOptions AllFemaleOptions = new CustomizationOptions();

            public static CustomizationOptions AllMaleOptions = new CustomizationOptions();

            public static CustomizationOptions AllOptions = new CustomizationOptions();

            static bool _IsInitialized;

            static void InitAllOptionsIfNeeded()
            {
                if (_IsInitialized)
                {
                    return;
                }

                var allRaces = ResourcesLibrary.GetRoot().Progression.CharacterRaces;

                foreach (var race in allRaces)
                {
                    AllFemaleOptions = AllFemaleOptions.Concat(race.FemaleOptions);
                    AllMaleOptions = AllMaleOptions.Concat(race.MaleOptions);
                    AllOptions = AllOptions.Concat(race.FemaleOptions).Concat(race.MaleOptions);
                }

                _IsInitialized = true;
            }

            public BlueprintAllRacesWrapper(BlueprintRace race)
            {
                this.race = race;
                InitAllOptionsIfNeeded();

                if (settings.toggleIgnoreGenderRestrictions)
                {
                    this.FemaleOptions = AllOptions;
                    this.MaleOptions = AllOptions;
                }
                else
                {
                    this.FemaleOptions = AllFemaleOptions;
                    this.MaleOptions = AllMaleOptions;
                }
            }
        }

        [HarmonyPatch(typeof(DollState), "Race", MethodType.Getter)]
        public static class DollState_Race_Patch
        {
            public static bool Prefix(DollState __instance, BlueprintRace __result)
            {
                if (!settings.toggleAllRaceCustomizations && !settings.toggleIgnoreGenderRestrictions)
                {
                    return true;
                }

                __result = new BlueprintAllRacesWrapper(__result);

                return false;
            }
        }
    }
}