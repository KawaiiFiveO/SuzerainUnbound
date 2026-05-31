using HarmonyLib;

namespace SuzerainUnbound;

// this patch is likely redundant after FakeCampaignCompletionPatch
// but it's here just in case
[HarmonyPatch(typeof(StorySelectionPanel), nameof(StorySelectionPanel.Setup))]
public class ForceTorporToggleInteractablePatch
{
    static void Postfix(StorySelectionPanel __instance)
    {
        // We check if the toggle exists AND if it is currently locked (false)
        if (__instance.torporModeToggle != null && !__instance.torporModeToggle.interactable)
        {
            __instance.torporModeToggle.interactable = true;
            Plugin.Log.LogInfo("[TorporModeUnlock] Torpor Mode was locked. Forced it to be interactable!");

            if (__instance.torporModeCanvasGroup != null)
            {
                __instance.torporModeCanvasGroup.alpha = 1f;
                __instance.torporModeCanvasGroup.interactable = true;
            }

            if (__instance.torporModeNotInteractableTooltip != null)
            {
                __instance.torporModeNotInteractableTooltip.gameObject.SetActive(false);
            }
        }
        else
        {
            // We should always end up here anyway due to FakeCampaignCompletionPatch
            //Plugin.Log.LogInfo("[TorporModeUnlock] Torpor Mode was not locked. Leaving it alone.");
        }
    }
}

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