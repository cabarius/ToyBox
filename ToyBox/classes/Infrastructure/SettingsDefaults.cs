using System.Collections.Generic;

namespace ToyBox {
    internal static class SettingsDefaults {
        public static readonly HashSet<string> DefaultBuffsToIgnoreForDurationMultiplier = new() {
            "24cf3deb078d3df4d92ba24b176bda97", //Prone
            "e6f2fc5d73d88064583cb828801212f4", //Fatigued
            "bb1b849f30e6464284c1efd0e812d626", //Army Nauseated
            "f59aa0658cda4c7b82bf73c632a39650", //Army Stinking Cloud 
            "6179bbe7a7b4b674c813dedbca121799", //Summoned Unit Appear Buff (causes inaction for summoned units)
            "12f2f2cf326dfd743b2cce5b14e99b3c", //Resurrection Buff
        };
    }
}
