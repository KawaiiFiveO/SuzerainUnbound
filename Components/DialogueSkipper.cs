using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using PixelCrushers.DialogueSystem;

namespace SuzerainUnbound;

public class DialogueSkipper : MonoBehaviour
{
    public DialogueSkipper(IntPtr ptr) : base(ptr) { }

    private float _nextAdvanceTime = 0f;

    private readonly Key[] _blacklistedKeys = new Key[]
{
        Key.None, Key.Space, Key.Enter, Key.NumpadEnter,
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Escape
};

    void Update()
    {
        if (DialogueManager.isConversationActive && DialogueManager.standardDialogueUI != null)
        {
            // Grab the configured key
            Key configuredKey = Plugin.SkipKey.Value;

            // If the user chose a banned key, forcefully revert to LeftCtrl
            if (_blacklistedKeys.Contains(configuredKey))
            {
                configuredKey = Key.LeftCtrl;
                Plugin.Log.LogWarning($"[DialogueSkipper] Configured key {configuredKey} conflicts with default key bindings! Defaulting to LeftCtrl.");
            }


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