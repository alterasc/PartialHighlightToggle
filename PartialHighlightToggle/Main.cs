using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using System.Reflection;
using UnityModManagerNet;

namespace PartialHighlightToggle;

public static class Main
{
    internal static Harmony HarmonyInstance;
    internal static UnityModManager.ModEntry.ModLogger log;
    internal static SettingsModMenu Settings;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        log = modEntry.Logger;
        HarmonyInstance = new Harmony(modEntry.Info.Id);
        HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        return true;
    }

    [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
    internal static class BlueprintInitPatch
    {
        private static bool _initiailized;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_initiailized) return;
            _initiailized = true;
            Settings = new SettingsModMenu();
            Settings.Initialize();
            HighlightManager.isBasicHighlightToggledOn = Settings.DefaultHighlightState;
        }
    }
}

