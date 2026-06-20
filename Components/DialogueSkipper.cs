using PixelCrushers.DialogueSystem;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuzerainUnbound;

public class DialogueSkipper : MonoBehaviour
{
    public DialogueSkipper(IntPtr ptr) : base(ptr) { }

    private float _nextAdvanceTime = 0f;
    private OneTimeDecreesPanel _decreePanel;

    void Update()
    {
        if (DialogueManager.isConversationActive && DialogueManager.standardDialogueUI != null)
        {
            // Grab the configured key
            Key configuredKey = InputHelper.ToInputKey(Plugin.SkipKey.Value);

            // Use the indexer to read the live physical state of the configured key
            if (Keyboard.current != null && Keyboard.current[configuredKey].isPressed && DialogueManager.standardDialogueUI != null && DialogueManager.isConversationActive)
            {
                if (Time.time >= _nextAdvanceTime)
                {
                    // ConversationPanel.PauseConversation() is a game-level pause that doesn't
                    // block OnContinueConversation() at the PixelCrushers level. Guard manually:
                    // don't advance dialogue while a game panel is overriding the conversation.
                    if (_decreePanel == null)
                        _decreePanel = FindObjectOfType<OneTimeDecreesPanel>();
                    if (_decreePanel != null && _decreePanel.IsShowing())
                        return;

                    DialogueManager.standardDialogueUI.OnContinueConversation();
                    _nextAdvanceTime = Time.time + 0.15f;

                    StrangeRanks.IncrementStrangeStat(Plugin.SkippedDialogueCount, "Dialogue Skipper");
                }
            }
        }
    }
}