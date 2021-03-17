using UnityModManagerNet;

namespace ToyBox
{
    public class Settings : UnityModManager.ModSettings
    {
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
