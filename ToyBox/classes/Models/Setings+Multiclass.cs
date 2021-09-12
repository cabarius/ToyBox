// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using ModKit.Utility;
using System.Collections.Generic;
using UnityModManagerNet;
using ToyBox.Multiclass;
using Kingmaker.UnitLogic;
using ModKit;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.Classes;

namespace ToyBox {
    public class ArchetypeOptions : HashSet<string> {
        public const string NoArchetype = "$NoArchetype";

        public bool Contains(BlueprintArchetype arch) => Contains(arch == null ? NoArchetype : arch.HashKey());

        public void Add(BlueprintArchetype arch) => Add(arch == null ? NoArchetype : arch.HashKey());
        public void AddExclusive(BlueprintArchetype arch) { Clear(); Add(arch); }
        public void Remove(BlueprintArchetype arch) => Remove(arch == null ? NoArchetype : arch.HashKey());
        public override string ToString() {
            var result = "{";
            foreach (var arch in this) result += " " + arch + ",";
            return result;
        }

    }
    public class MulticlassOptions : SerializableDictionary<string, ArchetypeOptions> {
        public const string CharGenKey = @"$CharacterGeneration";
        public static MulticlassOptions Get(UnitDescriptor? ch) {
            //Main.Log($"stack: {System.Environment.StackTrace}");
            MulticlassOptions options;
            if (ch == null) {
                options = Main.settings.multiclassSettings.GetValueOrDefault(CharGenKey, new MulticlassOptions());
                Main.Debug($"MulticlassOptions.Get - chargen - options: {options}");
            }
            else {
                if (ch.HashKey() == null) return null;
                options = Main.settings.multiclassSettings.GetValueOrDefault(ch.HashKey(), new MulticlassOptions());
                Main.Debug($"MulticlassOptions.Get - {ch.CharacterName} - set: {options}");
            }
            return options;
        }
        public static void Set(UnitDescriptor? ch, MulticlassOptions options) {
            //modLogger.Log($"stack: {System.Environment.StackTrace}");
            if (ch == null) Main.settings.multiclassSettings[CharGenKey] = options;
            else {
                if (ch.HashKey() == null) return;
                Main.settings.multiclassSettings[ch.HashKey()] = options;
            }
        }

        public ArchetypeOptions ArchetypeOptions(BlueprintCharacterClass cl) {
            return this.GetValueOrDefault(cl.HashKey(), new ArchetypeOptions());
        }
        public void SetArchetypeOptions(BlueprintCharacterClass cl, ArchetypeOptions archOptions) {
            this[cl.HashKey()] = archOptions;
        }
        public bool Contains(BlueprintCharacterClass cl) => base.ContainsKey(cl.HashKey());
        public void Add(BlueprintCharacterClass cl) => this.Add(cl.HashKey(), new ArchetypeOptions());
        public void Remove(BlueprintCharacterClass cl) => this.Remove(cl.HashKey());
        public override string ToString() {
            string result = base.ToString() + " {\n";
            foreach (var classEntry in this) {
                result += $"    {classEntry.Key} : {classEntry.Value}";
            }
            result += "}";
            return result;
        }
    }
}
