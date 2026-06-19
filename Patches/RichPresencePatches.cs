using BepInEx.Logging;
using DiscordRPC;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace SuzerainUnbound
{
    public static class DiscordRichPresence
    {
        public const string ClientId = "1516272806327881778";

        internal static ManualLogSource Log;
        internal static DiscordRpcClient Discord;

        // Session start, used for Discord's "elapsed" timer.
        private static readonly DateTime SessionStart = DateTime.UtcNow;

        // Last fields the game gave us, so we can re-push once Discord connects.
        private static string _a, _b, _c;
        private static bool _initialized;

        /// <summary>Call once from your plugin's Load(). Safe to call again (no-ops).</summary>
        public static void Initialize(ManualLogSource log)
        {
            if (_initialized) return;
            _initialized = true;

            Log = log;

            Discord = new DiscordRpcClient(ClientId);
            // DiscordRPC.NET runs its own pipe IO thread and dispatches events itself
            // (autoEvents defaults true), so no Update() pump is needed.
            Discord.OnReady += (s, e) =>
            {
                Log.LogInfo($"[DiscordRPC] Discord connected as {e.User.Username}");
                PushCurrent(); // re-push in case a story beat fired before we connected
            };
            Discord.OnError += (s, e) => Log.LogWarning($"[DiscordRPC] Discord error {e.Code}: {e.Message}");
            Discord.Initialize();

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                try { Discord?.ClearPresence(); Discord?.Dispose(); } catch { }
            };

            Log.LogInfo("[DiscordRPC] Suzerain Discord RP module initialized.");
            PushCurrent(); // neutral baseline until the first story beat fires
        }

        /// <summary>
        /// Called from the StoreManager hook with the three raw sequencer-command
        /// fields (before the game joins them into the Steam "action" string).
        /// </summary>
        internal static void UpdateFromGame(string a, string b, string c)
        {
            _a = a; _b = b; _c = c;
            PushCurrent();
        }

        internal static void PushCurrent()
        {
            if (Discord == null) return;

            // first field -> Details line, the rest -> State line.
            string details = string.IsNullOrEmpty(_a) ? null : _a;

            var extras = new List<string>();
            if (!string.IsNullOrEmpty(_b)) extras.Add(_b);
            if (!string.IsNullOrEmpty(_c)) extras.Add(_c);
            string state = extras.Count > 0 ? string.Join(" \u2022 ", extras) : null;

            // Never blank out, or Discord reverts to its auto-detected "Playing Suzerain".
            if (string.IsNullOrEmpty(details) && string.IsNullOrEmpty(state))
                details = "Playing Suzerain";

            Discord.SetPresence(new RichPresence
            {
                Details = Clamp(details),
                State = Clamp(state),
                Timestamps = new Timestamps(SessionStart),
                Assets = new Assets
                {
                    LargeImageKey = "suzerain",   // asset name from the Dev Portal
                    LargeImageText = "Suzerain"
                }
            });
        }

        // Discord caps Details/State at 128 bytes; clamp conservatively.
        private static string Clamp(string s)
            => string.IsNullOrEmpty(s) ? null : (s.Length > 120 ? s.Substring(0, 120) : s);
    }

    [HarmonyPatch]
    internal static class RichPresencePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamRichPresenceManager), nameof(SteamRichPresenceManager.SetRichPresence), new[] { typeof(string) })]
        public static void OnSetRichPresence(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            DiscordRichPresence.Log.LogDebug($"[DiscordRPC] SetRichPresence called with action=\"{action}\"");

            // Split the game's string into a maximum of 3 pieces wherever there is a comma
            string[] parts = action.Split(new[] { ", " }, 3, StringSplitOptions.None);

            string a = parts.Length > 0 ? parts[0] : ""; // e.g., "Reviewing the current economic situation of Sordland"
            string b = parts.Length > 1 ? parts[1] : ""; // e.g., "White Room"
            string c = parts.Length > 2 ? parts[2] : ""; // e.g., "Maroon Palace"

            // Now send them as separate strings!
            DiscordRichPresence.UpdateFromGame(a, b, c); 
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SteamRichPresenceManager), nameof(SteamRichPresenceManager.ClearRichPresence))]
        public static void OnClearRichPresence()
        {
            DiscordRichPresence.Log.LogDebug("[DiscordRPC] ClearRichPresence called.");
            DiscordRichPresence.UpdateFromGame("Playing Suzerain", "", "");
        }
    }
}
