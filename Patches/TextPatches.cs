using HarmonyLib;
using Febucci.UI;

namespace SuzerainUnbound;

[HarmonyPatch(typeof(TypewriterByCharacter), "GetWaitAppearanceTimeOf")]
public class InstantTextPatch
{
    // A flag so we only print our success log once, instead of spamming the console
    private static bool _hasLogged = false;

    // Prefix to intercept the character delay calculation
    static bool Prefix(ref float __result)
    {
        // 1. Force the typing delay to 0 seconds!
        __result = 0f;

        // 2. Reach into Febucci's global settings and forcefully disable fade-ins!
        // Doing this here ensures it is applied right as the text is rendering.
        TextAnimatorSettings.SetAppearancesActive(false);

        if (!_hasLogged)
        {
            Plugin.Log.LogInfo("[FastDialogue] Globally disabled TextAnimator fade-in appearances and set text speed to instant!");
            _hasLogged = true;
        }

        // 3. Return false to skip Febucci's actual timing calculation
        return false;
    }
}