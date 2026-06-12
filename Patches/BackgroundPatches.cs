using HarmonyLib;
using UnityEngine;

namespace SuzerainUnbound;

// 1. We use PersistenceManager.Awake just as a convenient "Game Startup" trigger
[HarmonyPatch(typeof(PersistenceManager), "Awake")]
public class BackgroundExecutionPatch
{
    static void Postfix()
    {
        if (Plugin.EnableBackgroundRun.Value)
        {
            // Tells the core Unity C++ engine to never suspend the main thread
            Application.runInBackground = true;
            Plugin.Log.LogInfo("[RunInBackground] Forced Unity Application.runInBackground to true!");
        }
    }
}

// 2. We intercept the global property that scripts check to see if you Alt-Tabbed
[HarmonyPatch(typeof(Application), nameof(Application.isFocused), MethodType.Getter)]
public class AlwaysFocusedPatch
{
    static void Postfix(ref bool __result)
    {
        // If our setting is enabled, overwrite whatever Unity returned with 'true'
        if (Plugin.EnableBackgroundRun.Value)
        {
            __result = true;
        }
    }
}
