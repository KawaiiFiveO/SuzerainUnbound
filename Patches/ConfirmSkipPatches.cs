using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Events;

namespace SuzerainUnbound
{
    // Auto-confirms (and skips) Suzerain's "Are you sure?" confirmation popups.
    //
    // Each popup is shown by a method that builds a ConfirmationPanelData and calls
    // ConfirmationPanel.Setup. We intercept that method, invoke its confirm ("Yes") lambda,
    // and return false so the dialog is never built.
    //
    // ConfirmationPanelData layout (from decompilation):
    //   +0x38 = "No"  button label,  +0x40 = No action  (b__N_0, present only on two-button panels)
    //   +0x48 = "Yes" button label,  +0x50 = Yes action (the confirm we invoke)
    // The YES action is ALWAYS the one at +0x50: that is _..._b__N_1 on the two-button panels
    // (Decisions, WarProduction) and _..._b__N_0 on the single-action panels (everything else --
    // those still render a No button, but only wire a callback for Yes).
    //
    // System/destructive confirmations (exit, quit, back-to-menu, load-checkpoint) are only
    // skipped when the user opts in via the skipAll config toggle, since they guard against
    // losing unsaved progress on a misclick.
    //
    // The decree sign popup does NOT use this mechanism -- see DecreeSignSkip below.

    public enum ConfirmSkipMode
    {
        disabled,
        skipIngame,
        skipAll
    }

    internal static class ConfirmSkipPatches
    {
        private static bool SkipSystemEnabled => Plugin.EnableConfirmSkip.Value == ConfirmSkipMode.skipAll;

        private static readonly Target[] Targets =
        {
            //                declaring type                method                          confirm lambda                          system? label
            new Target(typeof(PagedDecisionPanel),          "OnFinish",                     "_OnFinish_b__48_1",                     false, "Decisions"),
            new Target(typeof(WarProductionPanel),          "OnTrainButtonClick",           "_OnTrainButtonClick_b__93_1",           false, "WarProduction"),
            new Target(typeof(SkipProloguePanel),           "OnConfirmClick",               "_OnConfirmClick_b__24_0",               false, "SkipPrologue"),
            new Target(typeof(TemplateArchetypeSlot),       "OnSelectArchetypeButtonClick", "_OnSelectArchetypeButtonClick_b__17_0", false, "Archetype"),
            new Target(typeof(CharacterCustomizationPanel), "OnFinish",                     "_OnFinish_b__33_0",                     false, "Customization"),
            new Target(typeof(LoadArchetypePanel),          "OnConfirmSaveFileSelection",   "_OnConfirmSaveFileSelection_b__21_0",   false, "LoadArchetype"),
            new Target(typeof(OneTimeDecreesPanel),         "OnFinishButtonClick",          "_OnFinishButtonClick_b__37_0",          false, "OneTimeDecrees"),

            // --- system / destructive: only skipped when SkipSystem is enabled ---
            new Target(typeof(OptionsPage),                 "OnApplyClick",                 "_OnApplyClick_b__41_1",                 true,  "ApplySettings"),
            new Target(typeof(EscapeMenuPanel),             "OnMainMenuClick",              "_OnMainMenuClick_b__72_0",              true,  "BackToMainMenu"),
            new Target(typeof(MainMenuPanel),               "OnExitClick",                  "_OnExitClick_b__87_0",                  true,  "ExitGame (MainMenu)"),   // lambda on <>c
            new Target(typeof(EscapeMenuPanel),             "OnExitClick",                  "_OnExitClick_b__75_0",                  true,  "ExitGame (EscapeMenu)"), // lambda on <>c
            new Target(typeof(EscapeMenuPanel),             "OnLoadLastCheckpointClick",    "_OnLoadLastCheckpointClick_b__62_0",    true,  "LoadLastCheckpoint"),
        };

        private static readonly Dictionary<MethodBase, Target> _byMethod = new Dictionary<MethodBase, Target>();

        public static void ApplyAll(Harmony harmony)
        {
            MethodInfo prefix = AccessTools.Method(typeof(ConfirmSkipPatches), nameof(SkipPrefix));

            foreach (Target t in Targets)
            {
                MethodInfo target = AccessTools.Method(t.Type, t.Method);
                if (target == null)
                {
                    Plugin.Log.LogError($"[YesImSure] Target not found: {t.Type.Name}.{t.Method} ({t.Label}) -- skipped");
                    continue;
                }

                _byMethod[target] = t;
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Plugin.Log.LogInfo($"[YesImSure] Patched {t.Type.Name}.{t.Method} ({t.Label})");
            }

            DecreeSignSkip.Apply(harmony);
        }

        // Shared prefix. Uses __originalMethod to find which popup this is.
        private static bool SkipPrefix(object __instance, MethodBase __originalMethod)
        {
            if (!_byMethod.TryGetValue(__originalMethod, out Target t))
                return true; // not one of ours -> run original

            if (t.System && !SkipSystemEnabled)
                return true; // user hasn't opted into skipping system confirmations -> show it

            if (TryInvokeConfirm(__instance, t.Type, t.ConfirmLambda, t.Label))
            {
                Plugin.Log.LogInfo($"[YesImSure] Auto-confirmed and skipped popup: {t.Label}");

                StrangeRanks.IncrementStrangeStat(Plugin.SkippedPopupsCount, "Popup Skipper");

                return false; // suppress -> no dialog
            }

            Plugin.Log.LogWarning($"[YesImSure] '{t.Label}' confirm could not be invoked -- showing popup instead");
            return true; // fall back to original so the player is never stuck
        }

        // Invokes a confirm lambda whether it's an instance method on the panel or a cached
        // lambda on a nested <>c display class (non-capturing lambdas like "quit").
        private static bool TryInvokeConfirm(object instance, Type panelType, string lambdaName, string label)
        {
            try
            {
                // 1) instance method directly on the panel
                MethodInfo m = FindMethod(panelType, lambdaName);
                if (m != null)
                {
                    m.Invoke(instance, null);
                    return true;
                }

                // 2) cached lambda on a nested display class (e.g. <>c)
                foreach (Type nt in panelType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    MethodInfo nm = FindMethod(nt, lambdaName);
                    if (nm == null) continue;

                    // Prefer the compiler's cached singleton (non-capturing <>c lambdas); fall back
                    // to a fresh instance (instance-capturing display classes like c__DisplayClass14_0).
                    object singleton = GetDisplayClassSingleton(nt);
                    object target = singleton ?? CreateInstance(nt);
                    if (target == null)
                    {
                        Plugin.Log.LogError($"[YesImSure] Found {nt.Name}.{lambdaName} for '{label}' but could not obtain an instance to invoke it on");
                        return false;
                    }

                    // For instance-capturing display classes (no static singleton): set the outer
                    // 'this' (the panel instance) at offset 0x10 — the first field after the Il2Cpp
                    // object header, which is where the compiler always places <>4__this.
                    // Il2CppInterop reflection cannot find this field by name or type; a native write
                    // is the only reliable approach.
                    if (singleton == null && instance != null)
                    {
                        try
                        {
                            var tBase = target as Il2CppObjectBase;
                            var iBase = instance as Il2CppObjectBase;
                            if (tBase != null && iBase != null)
                            {
                                IntPtr tPtr = IL2CPP.Il2CppObjectBaseToPtr(tBase);
                                IntPtr iPtr = IL2CPP.Il2CppObjectBaseToPtr(iBase);
                                if (tPtr != IntPtr.Zero && iPtr != IntPtr.Zero)
                                    Marshal.WriteIntPtr(IntPtr.Add(tPtr, 0x10), iPtr);
                            }
                        }
                        catch { }
                    }

                    nm.Invoke(target, null);
                    return true;
                }

                Plugin.Log.LogError($"[YesImSure] Could not resolve confirm lambda '{lambdaName}' on {panelType.Name} for '{label}'");
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[YesImSure] Invoking confirm for '{label}' threw: {e}");
                return false;
            }
        }

        // Quiet method lookup: AccessTools.Method logs a warning on every miss, and we probe
        // several types (the panel plus its nested types) expecting most to miss. Plain reflection
        // returns null silently. Lambda names are unique, so no ambiguity to worry about.
        private static MethodInfo FindMethod(Type type, string name)
        {
            return type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        }

        // A non-capturing lambda's display class caches a singleton of itself (<>9). Il2CppInterop
        // may expose that as a static property or a static field, so check both.
        private static object GetDisplayClassSingleton(Type displayClass)
        {
            const BindingFlags F = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (PropertyInfo p in displayClass.GetProperties(F))
            {
                if (p.PropertyType != displayClass || p.GetMethod == null) continue;
                try { object v = p.GetValue(null); if (v != null) return v; }
                catch { /* getter may fault if uninitialised; keep looking */ }
            }
            foreach (FieldInfo f in displayClass.GetFields(F))
            {
                if (f.FieldType != displayClass) continue;
                try { object v = f.GetValue(null); if (v != null) return v; }
                catch { /* field init may not have run; keep looking */ }
            }
            return null;
        }

        // Constructs a fresh instance of a display class (its parameterless ctor exists in IL2CPP).
        private static object CreateInstance(Type t)
        {
            try { return Activator.CreateInstance(t); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"[YesImSure] Could not instantiate {t.Name}: {e.Message}");
                return null;
            }
        }

        private readonly struct Target
        {
            public readonly Type Type;
            public readonly string Method;
            public readonly string ConfirmLambda;
            public readonly bool System;
            public readonly string Label;

            public Target(Type type, string method, string confirmLambda, bool system, string label)
            {
                Type = type;
                Method = method;
                ConfirmLambda = confirmLambda;
                System = system;
                Label = label;
            }
        }
    }

    // Auto-confirms the decree "are you sure you want to sign this?" popup, but leaves the
    // counsel variant ("The Grand Vizier wishes to speak to you before you sign...") alone.
    //
    // This one popup cannot use the ConfirmSkipPatches mechanism above. Its confirm lambda lives
    // in a *capturing* display class:
    //
    //   DecreeDetailsPage.<>c__DisplayClass14_0
    //     +0x10  <>4__this
    //     +0x18  hasDecreeCounselConversation   <-- the bool <OnSignClick>b__0 branches on
    //
    // b__0 disables the sign button, then either fires onSignConfirmed (sign now) or starts the
    // counsel conversation, depending on that bool. Suppressing OnSignClick and rebuilding the
    // display class by hand leaves the bool zeroed, so every decree took the sign-now branch and
    // the counsel conversation was silently discarded along with everything it sets.
    //
    // So we let OnSignClick run instead. It has no side effects of its own -- it builds a
    // ConfirmationPanelData and calls ConfirmationPanel.Setup synchronously -- which leaves the
    // game holding a correctly populated display class. We intercept Setup, read the bool off
    // the real confirm delegate, and either invoke that delegate (no conversation: sign now) or
    // fall through and let the prompt render (conversation: the player still opts in).
    internal static class DecreeSignSkip
    {
        private const int DelegateTargetOffset = 0x20; // System.Delegate.m_target
        private const int HasCounselOffset     = 0x18; // <>c__DisplayClass14_0.hasDecreeCounselConversation

        // Set for the duration of OnSignClick so the Setup prefix knows the popup being built is
        // ours. Setup is called directly from OnSignClick, so the window is a single call deep.
        [ThreadStatic] private static bool _inSignClick;

        public static void Apply(Harmony harmony)
        {
            MethodInfo signClick = AccessTools.Method(typeof(DecreeDetailsPage), "OnSignClick");
            MethodInfo setup = AccessTools.Method(typeof(ConfirmationPanel), "Setup",
                new[] { typeof(ConfirmationPanel.ConfirmationPanelData) });

            if (signClick == null || setup == null)
            {
                Plugin.Log.LogError("[YesImSure] Target not found: DecreeDetailsPage.OnSignClick or ConfirmationPanel.Setup (DecreeSign) -- skipped");
                return;
            }

            harmony.Patch(signClick,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(DecreeSignSkip), nameof(SignClickPrefix))),
                finalizer: new HarmonyMethod(AccessTools.Method(typeof(DecreeSignSkip), nameof(SignClickFinalizer))));

            harmony.Patch(setup,
                prefix: new HarmonyMethod(AccessTools.Method(typeof(DecreeSignSkip), nameof(SetupPrefix))));

            Plugin.Log.LogInfo("[YesImSure] Patched DecreeDetailsPage.OnSignClick (DecreeSign)");
        }

        private static void SignClickPrefix()
        {
            _inSignClick = true;
        }

        // Finalizer rather than a postfix so the flag is cleared even if OnSignClick throws.
        private static void SignClickFinalizer()
        {
            _inSignClick = false;
        }

        private static bool SetupPrefix(ConfirmationPanel.ConfirmationPanelData confirmationPanelData)
        {
            if (!_inSignClick)
            {
                return true; // some other popup -> leave it alone
            }

            UnityAction confirm = confirmationPanelData?.buttonAction_2;
            if (confirm == null)
            {
                Plugin.Log.LogWarning("[YesImSure] 'DecreeSign' popup had no confirm action -- showing popup instead");
                return true;
            }

            bool? hasCounsel = ReadHasCounselConversation(confirm);
            if (hasCounsel == null)
            {
                Plugin.Log.LogWarning("[YesImSure] 'DecreeSign' counsel state could not be read -- showing popup instead");
                return true;
            }

            if (hasCounsel.Value)
            {
                // Confirming here would drop the player straight into the counsel conversation
                // rather than signing, so this prompt is a real choice. Let it render.
                return true;
            }

            confirm.Invoke();
            Plugin.Log.LogInfo("[YesImSure] Auto-confirmed and skipped popup: DecreeSign");

            StrangeRanks.IncrementStrangeStat(Plugin.SkippedPopupsCount, "Popup Skipper");

            return false; // suppress -> no dialog
        }

        // Walks confirm -> its display class -> the captured bool. Il2CppInterop cannot reach
        // compiler-generated types by name, so both hops are raw reads; the offsets come from the
        // il2cpp dump and are confirmed by the decompiled OnSignClick.
        private static bool? ReadHasCounselConversation(UnityAction confirm)
        {
            try
            {
                IntPtr confirmPtr = IL2CPP.Il2CppObjectBaseToPtr(confirm);
                if (confirmPtr == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr displayClass = Marshal.ReadIntPtr(IntPtr.Add(confirmPtr, DelegateTargetOffset));
                if (displayClass == IntPtr.Zero)
                {
                    return null; // multicast delegate or unbound -- not something we can read
                }

                return Marshal.ReadByte(IntPtr.Add(displayClass, HasCounselOffset)) != 0;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"[YesImSure] Reading 'DecreeSign' counsel state threw: {e}");
                return null;
            }
        }
    }
}
