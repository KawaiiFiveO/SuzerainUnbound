using HarmonyLib;
using PixelCrushers.DialogueSystem;
using UnityEngine.InputSystem;

namespace SuzerainUnbound;

public enum TextHookMode { disabled, lunaTranslator, clipboard, both }

[HarmonyPatch(typeof(ConversationHandler), "OnContinue")]
public class TextHookAutoAdvancePatch
{
    static void Prefix() => TextHookController._autoAdvancing = true;
}

[HarmonyPatch(typeof(ConversationHandler), "OnConversationLine")]
public class TextHookPatch
{
    static void Postfix(Subtitle subtitle)
    {
        // Guard: skip key held (Dialogue Skip rapid-fire)
        if (Plugin.EnableInstantText.Value && Keyboard.current != null)
        {
            Key key = InputHelper.ToInputKey(Plugin.SkipKey.Value);
            if (Keyboard.current[key].isPressed) { return; }
        }

        // Guard: game auto-advanced this line via Skip Read Text
        if (TextHookController._autoAdvancing)
        {
            TextHookController._autoAdvancing = false;
            return;
        }

        if (!Plugin.TextHookIncludePlayerLines.Value && subtitle.speakerInfo.isPlayer) { return; }
        TextHookController.SendLine(subtitle.formattedText.text);
    }
}
