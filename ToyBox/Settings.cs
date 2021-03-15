using UnityModManagerNet;

namespace ToyBox
{
    public class Settings : UnityModManager.ModSettings
    {
//        public float MyFloatOption = 2f;
//        public bool MyBoolOption = true;
//        public string parameterOption = "";
        public int searchLimit = 100;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
