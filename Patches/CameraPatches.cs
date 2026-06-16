using System;
using HarmonyLib;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SuzerainUnbound;

public enum MapPanMode
{
    vanilla,
    stopAtEdge,
    disabled
}

[HarmonyPatch(typeof(MapScroller), "LateUpdate")]
public class CameraEdgePanPatch
{
    // ==========================================
    // WIN32 NATIVE API
    // ==========================================
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // Asks Windows what application the user is currently using
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    // ==========================================

    private static float _originalBorderPercentage = -1f;

    static void Prefix(MapScroller __instance)
    {
        if (_originalBorderPercentage < 0f)
        {
            _originalBorderPercentage = __instance.screenBorderPercentage;
        }

        MapPanMode currentMode = Plugin.EdgePanMode.Value;

        if (currentMode == MapPanMode.disabled)
        {
            __instance.screenBorderPercentage = -1f;
            return;
        }

        if (currentMode == MapPanMode.stopAtEdge)
        {
            bool isOutOfBounds = false;

            IntPtr gameWindow = GetActiveWindow();
            IntPtr foregroundWindow = GetForegroundWindow();

            // 1. Did the user click on another monitor or Alt-Tab?
            // If the game window isn't the active foreground window, we lost focus!
            if (gameWindow == IntPtr.Zero || gameWindow != foregroundWindow)
            {
                isOutOfBounds = true;
            }
            else
            {
                // 2. Is the mouse hovering on the second monitor without clicking?
                POINT p;
                if (GetCursorPos(out p))
                {
                    ScreenToClient(gameWindow, ref p);

                    if (p.X < 0 || p.Y < 0 || p.X > Screen.width || p.Y > Screen.height)
                    {
                        isOutOfBounds = true;
                    }
                }
            }

            if (isOutOfBounds)
            {
                __instance.screenBorderPercentage = -1f;
            }
            else
            {
                __instance.screenBorderPercentage = _originalBorderPercentage;
            }

            return;
        }

        __instance.screenBorderPercentage = _originalBorderPercentage;
    }
}

// We patch the core Unity Engine button check to globally spoof the Right Mouse Button
[HarmonyPatch(typeof(ButtonControl), "get_isPressed")]
public class DotaCameraMovePatch
{
    static bool Prefix(ButtonControl __instance, ref bool __result)
    {
        if (Plugin.EnableDotaCamera.Value && Mouse.current != null)
        {
            // If the game asks: "Is the Right Mouse Button pressed?"
            if (__instance == Mouse.current.rightButton)
            {
                // We secretly check the Middle Mouse Button instead!
                if (Mouse.current.middleButton.isPressed)
                {
                    __result = true; // Tell the game YES!
                    return false;    // Skip the real hardware check
                }
            }

            // BONUS: If we swapped Right to Middle, we should probably also check if they actually
            // want to use the Right button for something else (though Suzerain rarely does).
            // But this keeps the spoofing clean.
        }
        return true;
    }
}

// We must also spoof the "wasPressedThisFrame" check, because the developers used both!
[HarmonyPatch(typeof(ButtonControl), "get_wasPressedThisFrame")]
public class DotaCameraMoveFramePatch
{
    static bool Prefix(ButtonControl __instance, ref bool __result)
    {
        if (Plugin.EnableDotaCamera.Value && Mouse.current != null)
        {
            if (__instance == Mouse.current.rightButton)
            {
                if (Mouse.current.middleButton.wasPressedThisFrame)
                {
                    __result = true;
                    return false;
                }
            }
        }
        return true;
    }
}
