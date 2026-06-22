using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace SuzerainUnbound;

internal static class TextHookController
{
    internal static bool _autoAdvancing = false;

    private static readonly HttpClient _httpClient = new();
    private static string _lastText = string.Empty;

    private static bool _lunaConnected = false;
    private static bool _lunaErrorLogged = false;
    private static DateTime _lunaRetryAfter = DateTime.MinValue;

    internal static void SendLine(string rawText)
    {
        string text = StripTags(rawText);
        if (string.IsNullOrEmpty(text) || text == _lastText) { return; }
        _lastText = text;

        Plugin.Log.LogDebug($"[TextHook] {text}");

        var mode = Plugin.EnableTextHook.Value;
        if (mode == TextHookMode.lunaTranslator || mode == TextHookMode.both) { SendToLuna(text); }
        if (mode == TextHookMode.clipboard       || mode == TextHookMode.both) { CopyToClipboard(text); }
    }

    private static string StripTags(string text)
    {
        text = Regex.Replace(text, @"\{[^}]*\}", "");
        text = Regex.Replace(text, @"<[^>]*>", "");
        return text.Trim();
    }

    private static void SendToLuna(string text)
    {
        if (_lunaErrorLogged && DateTime.UtcNow < _lunaRetryAfter) { return; }

        string baseUrl = Plugin.LunaTranslatorUrl.Value;
        string url = $"{baseUrl}/api/textinput?text={Uri.EscapeDataString(text)}";
        _ = Task.Run(async () =>
        {
            try
            {
                await _httpClient.GetAsync(url);
                if (!_lunaConnected)
                {
                    Plugin.Log.LogInfo($"[TextHook] Connected to LunaTranslator at '{baseUrl}'.");
                    _lunaConnected = true;
                }
                else if (_lunaErrorLogged)
                {
                    Plugin.Log.LogInfo("[TextHook] LunaTranslator connection restored.");
                }
                _lunaErrorLogged = false;
                _lunaRetryAfter = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                if (!_lunaErrorLogged)
                {
                    Plugin.Log.LogWarning($"[TextHook] Could not reach LunaTranslator at '{baseUrl}'. Is it running with Network Service enabled? ({ex.Message}) Will retry in 30s.");
                }
                _lunaErrorLogged = true;
                _lunaRetryAfter = DateTime.UtcNow.AddSeconds(30);
            }
        });
    }

    private static void CopyToClipboard(string text)
    {
        try { GUIUtility.systemCopyBuffer = text; }
        catch (Exception ex) { Plugin.Log.LogWarning($"[TextHook] Clipboard write failed: {ex.Message}"); }
    }
}
