using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SuzerainUnbound;

public enum ColorblindPalette
{
    vanilla,
    blueOrange,
    okabeIto,
    ibm,
    tritanopia,
    graves,
}

public static class ColorblindPatches
{
    public static void ApplyAll(Harmony harmony)
    {
        harmony.PatchAll(typeof(ColorblindSituationPatch));
        harmony.PatchAll(typeof(ColorblindHUDTextStatPatch));
        harmony.PatchAll(typeof(ColorblindHUDStatPatch));
    }
}

// ─── Overview panel: situation severity (Low / Medium / High) ────────────────

[HarmonyPatch(typeof(TemplateOverviewEntry), "Setup", new[] { typeof(SituationData), typeof(Color) })]
class ColorblindSituationPatch
{
    static void Prefix(SituationData situationData, ref Color textColor)
    {
        ColorblindPalette palette = Plugin.ColorblindMode.Value;
        if (palette == ColorblindPalette.vanilla) return;
        textColor = situationData.SituationSeverity switch
        {
            SituationData.SituationSeverityType.Low    => ColorblindColors.Get(ColorLevel.Good,    palette),
            SituationData.SituationSeverityType.Medium => ColorblindColors.Get(ColorLevel.Neutral, palette),
            _                                          => ColorblindColors.Get(ColorLevel.Bad,     palette),
        };
    }
}

// ─── HUD text stat (Unstable / Stable / Growing and similar text labels) ─────

[HarmonyPatch(typeof(TemplateHUDTextStat), "Setup", new[] { typeof(HUDTextStatData) })]
[HarmonyPatch(typeof(TemplateHUDTextStat), "Setup", new System.Type[0])]
class ColorblindHUDTextStatPatch
{
    static readonly PropertyInfo _statValueText = AccessTools.Property(typeof(TemplateHUDTextStat), "statValueText");
    static readonly PropertyInfo _red           = AccessTools.Property(typeof(TemplateHUDTextStat), "red");
    static readonly PropertyInfo _yellow        = AccessTools.Property(typeof(TemplateHUDTextStat), "yellow");
    static readonly PropertyInfo _green         = AccessTools.Property(typeof(TemplateHUDTextStat), "green");

    static void Postfix(TemplateHUDTextStat __instance)
    {
        ColorblindPalette palette = Plugin.ColorblindMode.Value;
        if (palette == ColorblindPalette.vanilla) return;
        if (_statValueText == null || _red == null || _yellow == null || _green == null) return;

        var statValueText = _statValueText.GetValue(__instance) as TextMeshProUGUI;
        if (statValueText == null) return;

        Color current = statValueText.color;
        Color red     = (Color)_red.GetValue(__instance);
        Color yellow  = (Color)_yellow.GetValue(__instance);
        Color green   = (Color)_green.GetValue(__instance);

        if      (current == red)    { statValueText.color = ColorblindColors.Get(ColorLevel.Bad,     palette); }
        else if (current == yellow) { statValueText.color = ColorblindColors.Get(ColorLevel.Neutral, palette); }
        else if (current == green)  { statValueText.color = ColorblindColors.Get(ColorLevel.Good,    palette); }
        // black: unchanged
    }
}

// ─── HUD numeric stat (positive / zero / negative) ───────────────────────────

[HarmonyPatch(typeof(TemplateHUDStat), "Setup", new[] { typeof(HUDStatData) })]
[HarmonyPatch(typeof(TemplateHUDStat), "Setup", new System.Type[0])]
class ColorblindHUDStatPatch
{
    static readonly PropertyInfo _statText = AccessTools.Property(typeof(TemplateHUDStat), "statText");

    static void Postfix(TemplateHUDStat __instance)
    {
        ColorblindPalette palette = Plugin.ColorblindMode.Value;
        if (palette == ColorblindPalette.vanilla || _statText == null) return;

        var statText = _statText.GetValue(__instance) as TextMeshProUGUI;
        if (statText == null) return;

        // Only touch stats that embed <color=#HEX> rich text tags — those are the
        // ones the game intentionally colors (e.g. Government Budget). Stats that
        // don't use color tags (e.g. Personal Wealth) are left entirely unchanged.
        string text = statText.text;
        if (!text.Contains("<color", StringComparison.OrdinalIgnoreCase)) return;

        int value = __instance.GetCurrentValue();
        ColorLevel level = value > 0 ? ColorLevel.Good : value < 0 ? ColorLevel.Bad : ColorLevel.Neutral;
        Color newColor = ColorblindColors.Get(level, palette);
        statText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(newColor)}>{StripColorTags(text)}</color>";
    }

    static string StripColorTags(string text)
    {
        while (text.Contains("<color", StringComparison.OrdinalIgnoreCase))
        {
            int start = text.IndexOf("<color", StringComparison.OrdinalIgnoreCase);
            int end   = text.IndexOf('>', start);
            if (end < 0) break;
            text = text.Remove(start, end - start + 1);
        }
        return text.Replace("</color>", "").Replace("</Color>", "");
    }
}

// ─── Shared palette lookup ────────────────────────────────────────────────────

enum ColorLevel { Good, Neutral, Bad }

static class ColorblindColors
{
    internal static Color Get(ColorLevel level, ColorblindPalette palette)
    {
        return palette switch
        {
            ColorblindPalette.blueOrange => level switch
            {
                ColorLevel.Good    => Hex(0x1A85FF), // blue
                ColorLevel.Neutral => Hex(0xFFC20A), // amber
                _                  => Hex(0xE66100), // deep orange
            },
            ColorblindPalette.okabeIto => level switch
            {
                ColorLevel.Good    => Hex(0x009E73), // bluish green
                ColorLevel.Neutral => Hex(0xF0E442), // yellow
                _                  => Hex(0xD55E00), // vermilion
            },
            ColorblindPalette.ibm => level switch
            {
                ColorLevel.Good    => Hex(0x648FFF), // blue
                ColorLevel.Neutral => Hex(0xFFB000), // amber
                _                  => Hex(0xDC267F), // magenta
            },
            ColorblindPalette.tritanopia => level switch
            {
                ColorLevel.Good    => Hex(0x009E73), // bluish green
                ColorLevel.Neutral => Hex(0x999999), // neutral grey
                _                  => Hex(0xD55E00), // vermilion
            },
            ColorblindPalette.graves => level switch
            {
                ColorLevel.Good    => Hex(0x93D089), // light green
                ColorLevel.Neutral => Hex(0xEDEA8E), // light yellow
                _                  => Hex(0xE38DBF), // light pink
            },
            _ => Color.white,
        };
    }

    static Color Hex(int rgb) => new Color(
        ((rgb >> 16) & 0xFF) / 255f,
        ((rgb >> 8) & 0xFF) / 255f,
        (rgb & 0xFF) / 255f
    );
}
