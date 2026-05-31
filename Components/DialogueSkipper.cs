using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PixelCrushers.DialogueSystem;

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
            Key configuredKey = Plugin.SkipKey.Value;

            // Use the indexer to read the live physical state of the configured key
            if (Keyboard.current != null && Keyboard.current[configuredKey].isPressed)
            {
                if (Time.time >= _nextAdvanceTime)
                {
                    DialogueManager.standardDialogueUI.OnContinueConversation();
                    _nextAdvanceTime = Time.time + 0.15f;
                }
            }
        }
    }
}