using HarmonyLib;

namespace SuzerainUnbound;

[HarmonyPatch(typeof(PersistenceManager), nameof(PersistenceManager.IsStoryPackFinishedOnceBefore))]
public class FakeCampaignCompletionPatch
{
    static void Postfix(StoryPackData storyPackData, ref bool __result)
    {
        if (storyPackData != null)
        {
            // The base game (Sordland) is always owned. DLCs are owned if IsPurchased or IsPlayable is true.
            bool isOwned = storyPackData.IsSordland || storyPackData.IsPurchased || storyPackData.IsPlayable;

            if (!isOwned)
            {
                // If they don't own it, we do nothing
                Plugin.Log.LogInfo($"[TorporModeUnlock] Skipping unowned Story Pack ({storyPackData.NameInDatabase}) to prevent crashes.");
                return;
            }

            // Because this is a Postfix, __result holds the REAL answer from the game!
            if (__result == false)
            {
                Plugin.Log.LogInfo($"[TorporModeUnlock] Intercepted owned Story Pack ({storyPackData.NameInDatabase}). Forcing completion to TRUE!");

                // Overwrite the game's answer before it gets sent back to the UI
                __result = true;
            }
            else
            {
                Plugin.Log.LogInfo($"[TorporModeUnlock] Intercepted owned Story Pack ({storyPackData.NameInDatabase}), but it was already completed.");
            }
        }
    }
}