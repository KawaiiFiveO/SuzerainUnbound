using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using PixelCrushers.DialogueSystem;

namespace SuzerainUnbound;

public enum DialogueSkipKey
{
    LeftCtrl, RightCtrl,
    LeftShift, RightShift,
    LeftAlt, RightAlt,
    Tab, CapsLock,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Insert, Delete, Home, End, PageUp, PageDown
}

public class DialogueSkipper : MonoBehaviour
{
    public DialogueSkipper(IntPtr ptr) : base(ptr) { }

    private float _nextAdvanceTime = 0f;

    private static Key ToInputKey(DialogueSkipKey k) => k switch
    {
        DialogueSkipKey.LeftCtrl => Key.LeftCtrl,
        DialogueSkipKey.RightCtrl => Key.RightCtrl,
        DialogueSkipKey.LeftShift => Key.LeftShift,
        DialogueSkipKey.RightShift => Key.RightShift,
        DialogueSkipKey.LeftAlt => Key.LeftAlt,
        DialogueSkipKey.RightAlt => Key.RightAlt,
        DialogueSkipKey.Tab => Key.Tab,
        DialogueSkipKey.CapsLock => Key.CapsLock,
        DialogueSkipKey.F1 => Key.F1,
        DialogueSkipKey.F2 => Key.F2,
        DialogueSkipKey.F3 => Key.F3,
        DialogueSkipKey.F4 => Key.F4,
        DialogueSkipKey.F5 => Key.F5,
        DialogueSkipKey.F6 => Key.F6,
        DialogueSkipKey.F7 => Key.F7,
        DialogueSkipKey.F8 => Key.F8,
        DialogueSkipKey.F9 => Key.F9,
        DialogueSkipKey.F10 => Key.F10,
        DialogueSkipKey.F11 => Key.F11,
        DialogueSkipKey.F12 => Key.F12,
        DialogueSkipKey.Insert => Key.Insert,
        DialogueSkipKey.Delete => Key.Delete,
        DialogueSkipKey.Home => Key.Home,
        DialogueSkipKey.End => Key.End,
        DialogueSkipKey.PageUp => Key.PageUp,
        DialogueSkipKey.PageDown => Key.PageDown,
        _ => Key.LeftCtrl
    };

    void Update()
    {
        if (DialogueManager.isConversationActive && DialogueManager.standardDialogueUI != null)
        {
            // Grab the configured key
            Key configuredKey = ToInputKey(Plugin.SkipKey.Value);

            // Use the indexer to read the live physical state of the configured key
            if (Keyboard.current != null && Keyboard.current[configuredKey].isPressed && DialogueManager.standardDialogueUI != null && DialogueManager.isConversationActive)
            {
                if (Time.time >= _nextAdvanceTime)
                {
                    DialogueManager.standardDialogueUI.OnContinueConversation();
                    _nextAdvanceTime = Time.time + 0.15f;

                    Plugin.IncrementStrangeStat(Plugin.SkippedDialogueCount, "Dialogue Skipper");
                }
            }
        }
    }
}