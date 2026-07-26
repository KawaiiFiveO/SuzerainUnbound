using HarmonyLib;
using Steamworks.Data;
using System;

namespace SuzerainUnbound;

// StoreManager.UnlockAchievement is the single choke point for achievement unlocks:
// the [LuaFunction] attribute means the dialogue database calls it directly as Lua, and
// SequencerCommandUnlockSteamAchievement.Start() also routes into it.
// Vanilla gates the whole body behind isTorporModeOn, returning silently when it is off,
// so we trigger the Steam achievement ourselves before the original runs.
[HarmonyPatch(typeof(StoreManager), nameof(StoreManager.UnlockAchievement))]
public class ForceAchievementPatch
{
    static void Prefix(string achievementID)
    {
        if (string.IsNullOrEmpty(achievementID))
        {
            Plugin.Log.LogWarning("[ForceAchievements] UnlockAchievement called with an empty ID.");
            return;
        }

        Plugin.Log.LogInfo($"[ForceAchievements] Bypassing Torpor Mode! Unlocking achievement: {achievementID}");

        try
        {
            new Achievement(achievementID).Trigger();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ForceAchievements] Failed to unlock achievement '{achievementID}': {ex.Message}");
        }
    }
}
