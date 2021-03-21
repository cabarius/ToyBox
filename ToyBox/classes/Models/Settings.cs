using UnityModManagerNet;

namespace ToyBox {
    public class Settings : UnityModManager.ModSettings {
        public int selectedPartyFilter = 0;
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";

        public bool highlightObjectsToggle = false;
        public bool settingShowDebugInfo = true;
        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}
