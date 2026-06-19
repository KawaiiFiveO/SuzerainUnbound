using UnityEngine.InputSystem;

namespace SuzerainUnbound;

public enum CustomKey
{
    LeftCtrl, RightCtrl,
    LeftShift, RightShift,
    LeftAlt, RightAlt,
    Tab, CapsLock,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Insert, Delete, Home, End, PageUp, PageDown
}

internal static class InputHelper
{
    internal static Key ToInputKey(CustomKey k) => k switch
    {
        CustomKey.LeftCtrl => Key.LeftCtrl,
        CustomKey.RightCtrl => Key.RightCtrl,
        CustomKey.LeftShift => Key.LeftShift,
        CustomKey.RightShift => Key.RightShift,
        CustomKey.LeftAlt => Key.LeftAlt,
        CustomKey.RightAlt => Key.RightAlt,
        CustomKey.Tab => Key.Tab,
        CustomKey.CapsLock => Key.CapsLock,
        CustomKey.F1 => Key.F1,
        CustomKey.F2 => Key.F2,
        CustomKey.F3 => Key.F3,
        CustomKey.F4 => Key.F4,
        CustomKey.F5 => Key.F5,
        CustomKey.F6 => Key.F6,
        CustomKey.F7 => Key.F7,
        CustomKey.F8 => Key.F8,
        CustomKey.F9 => Key.F9,
        CustomKey.F10 => Key.F10,
        CustomKey.F11 => Key.F11,
        CustomKey.F12 => Key.F12,
        CustomKey.Insert => Key.Insert,
        CustomKey.Delete => Key.Delete,
        CustomKey.Home => Key.Home,
        CustomKey.End => Key.End,
        CustomKey.PageUp => Key.PageUp,
        CustomKey.PageDown => Key.PageDown,
        _ => Key.LeftCtrl
    };
}
