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
    static bool Prefix(ref bool __result)
    {
        if (Plugin.EnableBackgroundRun.Value)
        {
            // Force the answer to always be TRUE
            __result = true;

            // Skip the real Unity check
            return false;
        }
        return true;
    }
}