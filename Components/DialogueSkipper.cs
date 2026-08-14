using PixelCrushers.DialogueSystem;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuzerainUnbound;

public class DialogueSkipper : MonoBehaviour
{
    public DialogueSkipper(IntPtr ptr) : base(ptr) { }

    private float _nextAdvanceTime = 0f;

    void Update()
    {
        if (DialogueManager.isConversationActive && DialogueManager.standardDialogueUI != null)
        {
            // Grab the configured key
            Key configuredKey = InputHelper.ToInputKey(Plugin.SkipKey.Value);

            // Use the indexer to read the live physical state of the configured key
            if (Keyboard.current != null && Keyboard.current[configuredKey].isPressed)
            {
                if (Time.time >= _nextAdvanceTime)
                {
                    // Any panel that takes over a conversation (decrees, decisions, budget
                    // meetings, war production, bills) routes through
                    // ConversationPanel.PauseConversation() -> ConversationHandler.Pause() ->
                    // DialogueSystemController.Pause(), which sets DialogueTime.isPaused.
                    // PixelCrushers does not gate OnContinueConversation() on that flag, so
                    // advancing here walks the conversation past its decision branch before the
                    // panel has written the result -- characters then react to the choice you
                    // didn't make.
                    //
                    // The flag stays set until the panel's finish coroutine has run every
                    // DialogueLua.SetVariable / ArticyLuaHelper.Run and called
                    // UnpauseConversation(), so it also covers the window after the panel stops
                    // showing but before its decisions have been applied. A panel IsShowing()
                    // check does not: PagedDecisionPanel clears isShowing on the first line of
                    // its finish coroutine, one hide animation and a frame before any Lua runs.
                    if (DialogueTime.isPaused)
                    {
                        return;
                    }

                    DialogueManager.standardDialogueUI.OnContinueConversation();
                    _nextAdvanceTime = Time.time + 0.15f;

                    StrangeRanks.IncrementStrangeStat(Plugin.SkippedDialogueCount, "Dialogue Skipper");
                }
            }
        }
    }
}
