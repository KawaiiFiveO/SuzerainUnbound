using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public static ConfigEntry<Key> SkipKey;
    public static ConfigEntry<bool> EnableBackgroundRun;
    public static ConfigEntry<bool> MuteInBackground;

    public override void Load()
    {
        Log = base.Log;

        Log.LogInfo("==========================================================================");
        Log.LogInfo($"  Suzerain Unbound v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        Log.LogInfo("  Created by: OneHalf");
        Log.LogInfo("  Website: https://onehalf.dev/SuzerainUnbound");
        Log.LogInfo("  Report Issues: https://github.com/KawaiiFiveO/SuzerainUnbound/issues");
        Log.LogInfo("  A Morgna wes core! Vectern sis da!");
        Log.LogInfo("==========================================================================");
        Log.LogInfo("  [REQUIREMENT] Requires BepInEx 6.0 (IL2CPP) Bleeding Edge!");
        Log.LogInfo("  [INFO] Developed on BepInExbuild: 6.0.0-be.764");
        Log.LogInfo("  [INFO] Tested on Suzerain version: 3.1.0 (Windows) Build: 175");
        Log.LogInfo("==========================================================================");

        // 2. Bind the configs. (Category, Name, Default Value, Description)
        // This generates a text file in BepInEx/config/com.onehalf.suzerainunbound.cfg
        SkipSplashScreen = Config.Bind("Features", "SkipSplash", true, "Skips the Torpor Games intro logos.");
        EnableInstantText = Config.Bind("Features", "FastDialogue", true, "Instantly shows text on screen and enables Dialogue Skip.");
        // Bind the skip key, defaulting to LeftCtrl
        SkipKey = Config.Bind("Features", "DialogueSkipKey", Key.LeftCtrl, "The key to hold down for fast-forwarding dialogue. (WARNING: Space, Enter, Esc, and Number keys will be ignored to prevent gameplay conflicts).");
        EnableBackgroundRun = Config.Bind("Features", "RunInBackground", true, "Prevents the game from pausing or throttling framerate when Alt-Tabbed.");
        MuteInBackground = Config.Bind("Features", "MuteInBackground", true, "Mutes the game audio when you Alt-Tab out. Only works if RunInBackground is enabled.");
        // Cheats are off by default
        EnableTorporUnlock = Config.Bind("Features", "TorporModeUnlock", false, "Allows disabling Torpor mode on fresh saves.");
        EnableAchievements = Config.Bind("Features", "ForceAchievements", false, "Forces Steam achievements to unlock even if Torpor Mode is off.");

        // 3. Create a single Harmony instance for our mod
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        // 4. Conditionally apply the patches based on the user's config file
        if (SkipSplashScreen.Value)
        {
            harmony.PatchAll(typeof(SkipSplashPatch));
            Log.LogInfo("[CONFIG] Skip Splash patch applied.");
        }

        if (EnableInstantText.Value)
        {
            harmony.PatchAll(typeof(InstantTextPatch));
            Log.LogInfo("[CONFIG] Fast Dialogue patch applied.");

            ClassInjector.RegisterTypeInIl2Cpp<DialogueSkipper>();
            var skipperObject = new GameObject("SuzerainDialogueSkipper");
            skipperObject.AddComponent<DialogueSkipper>();
            GameObject.DontDestroyOnLoad(skipperObject);
            Log.LogInfo($"[CONFIG] Dialogue Skipper injected! Hold '{SkipKey.Value}' in-game to fast-forward.");
        }

        if (EnableBackgroundRun.Value)
        {
            harmony.PatchAll(typeof(BackgroundExecutionPatch));
            harmony.PatchAll(typeof(AlwaysFocusedPatch));
            Log.LogInfo("[CONFIG] Run In Background patches applied.");

            Il2CppInterop.Runtime.Injection.ClassInjector.RegisterTypeInIl2Cpp<BackgroundMuteController>();
            var muteObject = new GameObject("SuzerainBackgroundMuteController");
            muteObject.AddComponent<BackgroundMuteController>();
            GameObject.DontDestroyOnLoad(muteObject);
            Log.LogInfo($"[CONFIG] Background Mute Controller injected. MuteInBackground: {MuteInBackground.Value}");
        }

        if (EnableTorporUnlock.Value)
        {
            harmony.PatchAll(typeof(FakeCampaignCompletionPatch));
            //harmony.PatchAll(typeof(ForceTorporToggleInteractablePatch)); // this patch is likely redundant
            Log.LogInfo("[CONFIG] Torpor Mode Unlock patches applied.");
        }

        if (EnableAchievements.Value)
        {
            harmony.PatchAll(typeof(ForceDialogueAchievementPatch));
            Log.LogInfo("[CONFIG] Force Achievements patch applied.");
        }

        Log.LogInfo("All selected patches applied!");
    }
}