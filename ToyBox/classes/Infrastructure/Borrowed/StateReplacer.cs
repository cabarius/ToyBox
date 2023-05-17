using Kingmaker.Blueprints.Classes;
#if Wrath
using Kingmaker.UnitLogic.Class.LevelUp;
#elif RT
using Kingmaker.UnitLogic.Levelup.Obsolete;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
#endif

namespace ToyBox {
    // https://github.com/paxchristos/pk_multiple_classes_per_level_fork
    public class StateReplacer {
        public readonly LevelUpState State;
        public readonly BlueprintCharacterClass SelectedClass;
        public readonly int NextClassLevel;

        private StateReplacer() { }

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
