using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SuzerainUnbound;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    // 1. Declare variables to hold the user's settings
    public static ConfigEntry<bool> EnableTorporUnlock;
    public static ConfigEntry<bool> EnableAchievements;
    public static ConfigEntry<bool> SkipSplashScreen;
    public static ConfigEntry<bool> EnableInstantText;
    public static ConfigEntry<CustomKey> SkipKey;
    public static ConfigEntry<bool> ReadAllReports;
    public static ConfigEntry<CustomKey> ReadAllMapReportsKey;
    public static ConfigEntry<CustomKey> ReadAllArticlesKey;
    public static ConfigEntry<BackgroundRunMode> EnableBackgroundRun;
    public static ConfigEntry<ConfirmSkipMode> EnableConfirmSkip;
    public static ConfigEntry<MapPanMode> EdgePanMode;
    public static ConfigEntry<bool> EnableDotaCamera;
    public static ConfigEntry<int> SkippedPopupsCount;
    public static ConfigEntry<int> SkippedDialogueCount;
    public static ConfigEntry<int> ReportsReadCount;
    public static ConfigEntry<bool> EnableRichPresence;
    public static ConfigEntry<bool> ShowStrangeRanks;
    public static ConfigEntry<ColorblindPalette> ColorblindMode;
    public static ConfigEntry<TextHookMode> EnableTextHook;
    public static ConfigEntry<string> LunaTranslatorUrl;
    public static ConfigEntry<bool> TextHookIncludePlayerLines;

    public override void Load()
    {
        Log = base.Log;

        Log.LogMessage("==========================================================================");
        Log.LogMessage($"  Suzerain Unbound v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        Log.LogMessage("  Created by: OneHalf");
        Log.LogMessage("  Website: https://onehalf.dev/SuzerainUnbound");
        Log.LogMessage("  Report Issues: https://github.com/KawaiiFiveO/SuzerainUnbound/issues");
        Log.LogMessage("  A Morgna wes core! Vectern sis da!");
        Log.LogMessage("==========================================================================");
        Log.LogMessage("  [REQUIREMENT] Requires BepInEx 6.0 (IL2CPP) Bleeding Edge!");
        Log.LogMessage("  [INFO] Developed on BepInEx build: 6.0.0-be.783");
        Log.LogMessage("  [INFO] Tested on Suzerain version: 3.1.0 (Windows) Build: 175");
        Log.LogMessage("==========================================================================");

        // 2. Bind the configs. (Category, Name, Default Value, Description)
        // This generates a text file in BepInEx/config/com.onehalf.suzerainunbound.cfg
        SkipSplashScreen = Config.Bind("Features", "SkipSplash", true, "Skips the Torpor Games intro logos.");

        EnableInstantText = Config.Bind("Features", "FastDialogue", true, "Instantly shows text on screen and enables Dialogue Skip.");
        SkipKey = Config.Bind("Controls", "DialogueSkipKey", CustomKey.LeftCtrl, "(FastDialogue) The key to hold down for fast-forwarding dialogue.");

        ReadAllReports = Config.Bind("Features", "ReadAllReports", true, "Enables keybinds to instantly mark all pending map reports or news articles as read.");
        ReadAllMapReportsKey = Config.Bind("Controls", "ReadAllMapReportsKey", CustomKey.F1, "(ReadAllReports) Key to mark all pending map reports as read.");
        ReadAllArticlesKey = Config.Bind("Controls", "ReadAllArticlesKey", CustomKey.F2, "(ReadAllReports) Key to mark all pending news articles as read.");

        EnableBackgroundRun = Config.Bind("Features", "RunInBackground", BackgroundRunMode.muted, "Run In Background modes\ndisabled: Default game behavior.\nunmuted: Prevents the game from pausing or throttling framerate when Alt-Tabbed, but audio is not muted.\nmuted: Also mutes the game audio when you Alt-Tab out.");
        EnableConfirmSkip = Config.Bind("Features", "YesImSure", ConfirmSkipMode.skipIngame, "Yes I'm Sure modes\ndisabled: Never skip prompts.\nskipIngame: Automatically skip all in-game/setup 'Are you sure about your decisions?' prompts.\nskipAll: Also auto-skip all apply/quit/main-menu/load-checkpoint confirmations.");
        EdgePanMode = Config.Bind("Controls", "FixCameraEdgePan", MapPanMode.stopAtEdge, "Fix Camera Edge Pan modes\nvanilla: Default camera behavior.\nstopAtEdge: Prevents endless panning when the mouse is on a second monitor.\ndisabled: Turns off edge panning completely (use Right-Mouse or Middle-Mouse instead).");
        EnableDotaCamera = Config.Bind("Controls", "DotaCameraRebind", true, "Allow map dragging with Middle-Mouse Button. (Does not affect Right-Mouse.)");
        EnableRichPresence = Config.Bind("Features", "DiscordRPC", true, "Enable Discord Rich Presence for Suzerain.");

        // Accessibility patches
        ColorblindMode = Config.Bind("Accessibility", "ColorblindMode", ColorblindPalette.vanilla, "Colorblind-friendly color palette for Overview panel/Economy situation severity (Low/Medium/High).\nvanilla: Game default (green/yellow/red).\nblueOrange: Blue/amber/orange stoplight.\nokabeIto: Okabe-Ito universal design palette.\nibm: IBM colorblind-safe palette.\ntritanopia: Optimized for tritanopia (blue-yellow colorblindness).\ngraves: A fun pastel palette. Decorative, not intended for actual colorblind use.");
        EnableTextHook = Config.Bind("Accessibility", "TextHook", TextHookMode.disabled, "Live text hook for external translation/text-to-speech tools.\ndisabled: Off.\nlunaTranslator: POST each dialogue line to LunaTranslator's /api/textinput.\nclipboard: Copy each line to the system clipboard.\nboth: Do both simultaneously.");
        LunaTranslatorUrl = Config.Bind("Translation", "LunaTranslatorUrl", "http://localhost:2333", "(TextHook) Base URL of the LunaTranslator network service. Check LunaTranslator Settings -> Network Service for the port.");
        TextHookIncludePlayerLines = Config.Bind("Translation", "IncludePlayerLines", true, "(TextHook) Whether to send player's chosen dialogue lines. Disable for NPC-only output.");

        // Cheats are off by default
        EnableTorporUnlock = Config.Bind("Cheats", "TorporModeUnlock", false, "Allows disabling Torpor Mode on fresh saves.");
        EnableAchievements = Config.Bind("Cheats", "ForceAchievements", false, "Forces Steam achievements to unlock even if Torpor Mode is off.");

        // Track user stats
        SkippedPopupsCount = Config.Bind("Strange Counters", "Are You Sure Popups Skipped", 0, "The total number of times the Yes I'm Sure patch has saved you from having to click 'Yes' on a pointless confirmation popup.");
        SkippedDialogueCount = Config.Bind("Strange Counters", "Lines of Dialogue Skipped", 0, "The total number of times the Dialogue Skipper has fired (regardless of whether it actually skipped anything).");
        ReportsReadCount = Config.Bind("Strange Counters", "Reports and Articles Read", 0, "The total number of map reports and news articles marked as read via the Read All Reports patch.");
        ShowStrangeRanks = Config.Bind("Strange Counters", "ShowStrangeRanks", true, "Display a rank-up message in the console when you reach a new Strange rank.");

        // 3. Create a single Harmony instance for our mod
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        // 4. Conditionally apply the patches based on the user's config file
        if (SkipSplashScreen.Value)
        {
            try
            {
                harmony.PatchAll(typeof(SkipSplashPatch));
                Log.LogInfo("[CONFIG] Skip Splash patch applied.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Skip Splash patch failed to apply.");
            }
        }

        if (EnableInstantText.Value)
        {
            try
            {
                harmony.PatchAll(typeof(InstantTextPatch));
                Log.LogInfo("[CONFIG] Fast Dialogue patch applied.");

                ClassInjector.RegisterTypeInIl2Cpp<DialogueSkipper>();
                var skipperObject = new GameObject("SuzerainDialogueSkipper");
                skipperObject.AddComponent<DialogueSkipper>();
                GameObject.DontDestroyOnLoad(skipperObject);
                Log.LogInfo($"[CONFIG] Dialogue Skipper injected! Hold '{SkipKey.Value}' in-game to fast-forward.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Fast Dialogue patch failed to apply.");
            }
        }

        if (EnableBackgroundRun.Value != BackgroundRunMode.disabled)
        {
            try
            {
                harmony.PatchAll(typeof(BackgroundExecutionPatch));
                harmony.PatchAll(typeof(AlwaysFocusedPatch));
                Log.LogInfo($"[CONFIG] Run In Background patches applied. Mode: {EnableBackgroundRun.Value}");

                Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<BackgroundMuteController>();
                var muteObject = new GameObject("SuzerainBackgroundMuteController");
                muteObject.AddComponent<BackgroundMuteController>();
                GameObject.DontDestroyOnLoad(muteObject);
                Log.LogInfo($"[CONFIG] Background Mute Controller injected.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Run In Background patches failed to apply.");
            }
        }

        if (EnableConfirmSkip.Value != ConfirmSkipMode.disabled)
        {
            try
            {
                ConfirmSkipPatches.ApplyAll(harmony);
                Log.LogInfo($"[CONFIG] Yes I'm Sure (Auto Confirm) patches applied. Mode: {EnableConfirmSkip.Value}");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Yes I'm Sure (Auto Confirm) patches failed to apply.");
            }
        }

        if (EdgePanMode.Value != MapPanMode.vanilla)
        {
            try
            {
                bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (!isWindows && EdgePanMode.Value == MapPanMode.stopAtEdge)
                {
                    Log.LogWarning("[CONFIG] Fix Camera Edge Pan (stopAtEdge mode) is only available on Windows. Skipping patch.");
                }
                else
                {
                    harmony.PatchAll(typeof(CameraEdgePanPatch));
                    Log.LogInfo($"[CONFIG] Fix Camera Edge Pan patch applied. Mode: {EdgePanMode.Value}");
                }
            }
            catch
            {
                Log.LogWarning("[CONFIG] Fix Camera Edge Pan patch failed to apply.");
            }
        }

        if (EnableDotaCamera.Value)
        {
            try
            {
                harmony.PatchAll(typeof(DotaCameraMovePatch));
                harmony.PatchAll(typeof(DotaCameraMoveFramePatch));
                Log.LogInfo("[CONFIG] Dota Camera Rebind patch applied. Use Middle-Mouse to move the camera.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Dota Camera Rebind patch failed to apply.");
            }
        }

        if (EnableRichPresence.Value)
        {
            try
            {
                harmony.PatchAll(typeof(RichPresencePatches));
                DiscordRichPresence.Initialize(Log);
                Log.LogInfo("[CONFIG] Discord Rich Presence patches applied.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Discord Rich Presence patches failed to apply.");
            }
        }

        if (ReadAllReports.Value)
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<ReadAllController>();
                var readerObject = new GameObject("SuzerainReadAllController");
                readerObject.AddComponent<ReadAllController>();
                GameObject.DontDestroyOnLoad(readerObject);
                Log.LogInfo($"[CONFIG] Read All Reports injected! Press '{ReadAllMapReportsKey.Value}' for map reports, '{ReadAllArticlesKey.Value}' for news articles.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Read All Reports failed to inject.");
            }
        }

        if (EnableTextHook.Value != TextHookMode.disabled)
        {
            try
            {
                harmony.PatchAll(typeof(TextHookPatch));
                harmony.PatchAll(typeof(TextHookAutoAdvancePatch));
                Log.LogInfo($"[CONFIG] Text Hook patches applied. Mode: {EnableTextHook.Value}");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Text Hook patches failed to apply.");
            }
        }

        if (ColorblindMode.Value != ColorblindPalette.vanilla)
        {
            try
            {
                ColorblindPatches.ApplyAll(harmony);
                Log.LogInfo($"[CONFIG] Colorblind Mode patches applied. Palette: {ColorblindMode.Value}");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Colorblind Mode patches failed to apply.");
            }
        }

        if (EnableTorporUnlock.Value)
        {
            try
            {
                harmony.PatchAll(typeof(FakeCampaignCompletionPatch));
                Log.LogInfo("[CONFIG] Torpor Mode Unlock patches applied.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Torpor Mode Unlock patches failed to apply.");
            }
        }

        if (EnableAchievements.Value)
        {
            try
            {
                harmony.PatchAll(typeof(ForceDialogueAchievementPatch));
                Log.LogInfo("[CONFIG] Force Achievements patch applied.");
            }
            catch
            {
                Log.LogWarning("[CONFIG] Force Achievements patch failed to apply.");
            }
        }

        Log.LogInfo("All selected patches applied!");

        // Display user stats
        if (ShowStrangeRanks.Value)
        {
            StrangeRanks.Display();
        }
    }
}
