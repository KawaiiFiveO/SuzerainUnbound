using HarmonyLib;
using Steamworks.Data;
using PixelCrushers.DialogueSystem.SequencerCommands;
using System;

namespace SuzerainUnbound;

[HarmonyPatch(typeof(SequencerCommandUnlockSteamAchievement), nameof(SequencerCommandUnlockSteamAchievement.Start))]
public class ForceDialogueAchievementPatch
{
    static void Prefix(SequencerCommandUnlockSteamAchievement __instance)
    {
        // SequencerCommand.parameters is protected, so we read it via Traverse.
        // Accessing the property directly is more robust than invoking GetParameter by name.
        string achievementID = string.Empty;
        try
        {
            string[] parameters = Traverse.Create(__instance).Property("parameters").GetValue<string[]>();
            achievementID = (parameters != null && parameters.Length > 0) ? parameters[0] : string.Empty;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ForceAchievements] Failed to get achievement ID: {ex.Message}");
        }

        if (!string.IsNullOrEmpty(achievementID))
        {
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
        else
        {
            Plugin.Log.LogWarning("[ForceAchievements] Achievement ID was not found!");
        }
    }
}