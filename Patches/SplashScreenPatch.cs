using HarmonyLib;

namespace SuzerainUnbound;

[HarmonyPatch(typeof(PersistenceManager), "Awake")]
public class SkipSplashPatch
{
    private static bool _hasSkipped = false;

    static void Postfix(PersistenceManager __instance)
    {
        if (!_hasSkipped)
        {
            __instance.skipSplashScreen = true;
            Plugin.Log.LogInfo("[SkipSplash] Startup detected, forced skipSplashScreen to true!");
            _hasSkipped = true;
        }
    }
}