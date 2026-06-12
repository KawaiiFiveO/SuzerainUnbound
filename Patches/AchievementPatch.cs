using HarmonyLib;
using Steamworks.Data;
using PixelCrushers.DialogueSystem.SequencerCommands;

namespace SuzerainUnbound;

[HarmonyPatch(typeof(SequencerCommandUnlockSteamAchievement), nameof(SequencerCommandUnlockSteamAchievement.Start))]
public class ForceDialogueAchievementPatch
{
    // We use __instance to get the running sequencer command
    static void Prefix(SequencerCommandUnlockSteamAchievement __instance)
    {
        // Traverse looks inside the object, ignores 'protected' limits, climbs up to the base class, 
        // finds the GetParameter method, passes 0 and an empty string, and gives us the result!
        string achievementID = string.Empty;
        try
        {
            achievementID = Traverse.Create(__instance)
            .Method("GetParameter", new object[] { 0, string.Empty })
            .GetValue<string>();
        }
        catch
        {
            Plugin.Log.LogWarning("[ForceAchievements] Failed to get achievement ID!");
        }

        if (!string.IsNullOrEmpty(achievementID))
        {
            Plugin.Log.LogInfo($"[ForceAchievements] Bypassing Torpor Mode! Unlocking achievement: {achievementID}");

            try
            {
                new Achievement(achievementID).Trigger();
            }
            catch
            {
                Plugin.Log.LogWarning($"[ForceAchievements] Failed to unlock achievement: {achievementID}");
            }
        }
        else
        {
            Plugin.Log.LogWarning("[ForceAchievements] Achievement ID was not found by Traverse!");
        }
    }
}