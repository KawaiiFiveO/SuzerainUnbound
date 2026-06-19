using BepInEx.Configuration;

namespace SuzerainUnbound;

public class StrangeRanks
{
    public static void Display()
    {
        int totalPopupSkips = Plugin.SkippedPopupsCount.Value;
        string strangePopupRank = GetStrangeRank(totalPopupSkips);
        int totalDialogueSkips = Plugin.SkippedDialogueCount.Value;
        string strangeDialogueRank = GetStrangeRank(totalDialogueSkips);
        int totalReportsRead = Plugin.ReportsReadCount.Value;
        string strangeReportsRank = GetStrangeRank(totalReportsRead);

        Plugin.Log.LogMessage("[STRANGE] COUNTERS:");
        Plugin.Log.LogMessage($"[STRANGE] {strangePopupRank} Popup Skipper — Skips: {totalPopupSkips}");
        Plugin.Log.LogMessage($"[STRANGE] {strangeDialogueRank} Dialogue Skipper — Skips: {totalDialogueSkips}");
        Plugin.Log.LogMessage($"[STRANGE] {strangeReportsRank} Report Reader — Read: {totalReportsRead}");
    }

    // This will give us a string representing the user's Strange rank
    public static string GetStrangeRank(int count)
    {
        if (count >= 8500) return "Rayne's Own";
        if (count >= 7500) return "Assembly-Clearing";
        if (count >= 5000) return "Constitution-Melting";
        if (count >= 2500) return "Positively Petr's Own";
        if (count >= 1500) return "Wicked Malenyevist";
        if (count >= 1000) return "Veto-Spattered";
        if (count >= 999) return "Completely Civilian";
        if (count >= 750) return "Spectacularly Rizian";
        if (count >= 500) return "Truly Sollist";
        if (count >= 250) return "Sufficiently Sordish";
        if (count >= 175) return "Slightly Sovereign";
        if (count >= 100) return "Notably Reformist";
        if (count >= 50) return "Somewhat Dictatorial";
        if (count >= 25) return "Mildly Presidential";
        if (count >= 10) return "Scarcely Decisive";

        return "Unremarkable";
    }

    public static void IncrementStrangeStat(ConfigEntry<int> statConfig, string itemName)
    {
        int oldCount = statConfig.Value;
        string oldRank = GetStrangeRank(oldCount);

        statConfig.Value++;
        int newCount = statConfig.Value;
        string newRank = GetStrangeRank(newCount);

        if ((oldRank != newRank) && Plugin.ShowStrangeRanks.Value)
        {
            Plugin.Log.LogMessage($"[STRANGE] Your {itemName} has reached a new rank: {newRank}!");
        }
    }
}