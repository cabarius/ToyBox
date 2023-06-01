﻿using Kingmaker.Blueprints.Classes;
#if Wrath
using Kingmaker.UnitLogic.Class.LevelUp;
#endif

namespace ToyBox {
    // https://github.com/paxchristos/pk_multiple_classes_per_level_fork
    public class StateReplacer {
        public readonly LevelUpState State;
        public readonly BlueprintCharacterClass? SelectedClass;
        public readonly int NextClassLevel;
        
        public StateReplacer(LevelUpState state) {
            State = state;
            SelectedClass = state.SelectedClass;
            NextClassLevel = state.NextClassLevel;
        }

        public void Replace(BlueprintCharacterClass selectedClass) => State.SelectedClass = selectedClass;

        public void Replace(BlueprintCharacterClass selectedClass, int nextClassLevel) {
            State.SelectedClass = selectedClass;
            State.NextClassLevel = nextClassLevel;
        }

        public void Restore() {
            State.SelectedClass = SelectedClass;
            State.NextClassLevel = NextClassLevel;
        }
    }
}
