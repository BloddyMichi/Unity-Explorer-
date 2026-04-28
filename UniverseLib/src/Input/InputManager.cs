using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;

namespace UniverseLib.Input
{
    /// <summary>
    /// A universal Input handler which works with both legacy Input and the new InputSystem.
    /// </summary>
    public static class InputManager
    {
        /// <summary>
        /// The current Input package which is being used by the game.
        /// </summary>
        public static InputType CurrentType { get; private set; }

        internal static IHandleInput inputHandler;
        private static bool inputFailureLogged;
        private static bool attemptedHotSwapToInputSystem;

        private static bool TryPromoteToInputSystem(bool logSuccess = true)
        {
            if (CurrentType == InputType.InputSystem)
                return true;

            if (attemptedHotSwapToInputSystem)
                return false;

            attemptedHotSwapToInputSystem = true;

            if (InputSystem.TKeyboard == null && InputSystem.TMouse == null)
                return false;

            try
            {
                inputHandler = new InputSystem();
                CurrentType = InputType.InputSystem;
                _ = inputHandler.MousePosition;
                _ = inputHandler.MouseScrollDelta;
                if (logSuccess)
                    Universe.Log("Promoted input backend to new InputSystem support.");
                return true;
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"InputSystem promotion failed: {ex.Message}");
                return false;
            }
        }

        private static T SafeInput<T>(Func<T> action, T fallback = default)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                if (CurrentType == InputType.Legacy && (LegacyInput.IsBackendUnavailable || ex is TargetInvocationException))
                {
                    if (TryPromoteToInputSystem())
                    {
                        try
                        {
                            return action();
                        }
                        catch (Exception retryEx)
                        {
                            if (!inputFailureLogged)
                            {
                                inputFailureLogged = true;
                                Universe.LogWarning($"Input backend failure detected after InputSystem promotion: {retryEx.Message}");
                            }
                            return fallback;
                        }
                    }
                }

                if (!inputFailureLogged)
                {
                    inputFailureLogged = true;
                    Universe.LogWarning($"Input backend failure detected, using safe defaults: {ex.Message}");
                }
                return fallback;
            }
        }

        #region Internal Init

        internal static void Init()
        {
            InitHandler();
            InitKeycodes();
            CursorUnlocker.Init();
            EventSystemHelper.Init();
        }

        private static void InitHandler()
        {
            // Prefer the new Input System when it is available.
            // On Unity 6000 games the legacy UnityEngine.Input API may still be present,
            // but calling into it can throw InvalidOperationException when the game is configured
            // to use only the Input System package.

            if (InputSystem.TKeyboard != null || InputSystem.TMouse != null)
            {
                try
                {
                    inputHandler = new InputSystem();
                    CurrentType = InputType.InputSystem;

                    // Probe a few safe members to make sure the package is actually usable.
                    _ = inputHandler.MousePosition;
                    _ = inputHandler.MouseScrollDelta;

                    attemptedHotSwapToInputSystem = false;
                    Universe.Log("Initialized new InputSystem support.");
                    return;
                }
                catch (Exception ex)
                {
                    Universe.LogWarning($"InputSystem init failed, falling back to LegacyInput: {ex}");
                }
            }

            // With BepInEx Il2CppInterop, for some reason InputLegacyModule may be loaded but our ReflectionUtility doesn't cache it.
            if (ReflectionUtility.GetTypeByName("UnityEngine.Input") == null)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.FullName.Contains("UnityEngine.InputLegacyModule"))
                    {
                        ReflectionUtility.CacheTypes(asm);
                        break;
                    }
                }
            }

            if (LegacyInput.TInput != null)
            {
                try
                {
                    inputHandler = new LegacyInput();
                    CurrentType = InputType.Legacy;

                    // Probe the legacy backend. LegacyInput itself now suppresses runtime backend mismatch exceptions.
                    _ = inputHandler.MousePosition;
                    _ = inputHandler.MouseScrollDelta;
                    inputHandler.GetKeyDown(KeyCode.F5);

                    attemptedHotSwapToInputSystem = false;
                    Universe.Log("Initialized Legacy Input support");
                    return;
                }
                catch (Exception ex)
                {
                    Universe.LogWarning($"LegacyInput init failed: {ex}");
                }
            }

            Universe.LogWarning("Could not find any usable Input Module Type!");
            inputHandler = new NoInput();
            CurrentType = InputType.None;
        }

        private static void InitKeycodes()
        {
            // Cache keycodes for rebinding

            Array keycodes = Enum.GetValues(typeof(KeyCode));
            List<KeyCode> list = new();
            foreach (KeyCode kc in keycodes)
            {
                string s = kc.ToString();
                if (!s.Contains("Mouse") && !s.Contains("Joystick"))
                    list.Add(kc);
            }
            allKeycodes = list.ToArray();
        }

        #endregion

        // ~~~~~~ Main Input API ~~~~~~

        /// <summary>
        /// The current user Cursor position, with (0,0) being the bottom-left of the game display window.
        /// </summary>
        public static Vector3 MousePosition
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return SafeInput(() => inputHandler.MousePosition, Vector2.zero);
            }
        }

        /// <summary>
        /// The current mouse scroll delta from this frame. x is horizontal, y is vertical.
        /// </summary>
        public static Vector2 MouseScrollDelta
        {
            get
            {
                if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                    return Vector2.zero;

                return SafeInput(() => inputHandler.MouseScrollDelta, Vector2.zero);
            }
        }

        /// <summary>
        /// Returns true if the provided KeyCode was pressed this frame. 
        /// Translates KeyCodes into Key if InputSystem is being used.
        /// </summary>
        public static bool GetKeyDown(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return SafeInput(() => inputHandler.GetKeyDown(key), false);
        }

        /// <summary>
        /// Returns true if the provided KeyCode is being held down (not necessarily just pressed). 
        /// Translates KeyCodes into Key if InputSystem is being used.
        /// </summary>
        public static bool GetKey(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return SafeInput(() => inputHandler.GetKey(key), false);
        }

        /// <summary>
        /// Returns true if the provided KeyCode was released this frame.
        /// Translates KeyCodes into Key if InputSystem is being used.
        /// </summary>
        public static bool GetKeyUp(KeyCode key)
        {
            if (Rebinding || Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            if (key == KeyCode.None)
                return false;
            return SafeInput(() => inputHandler.GetKeyUp(key), false);
        }

        /// <summary>
        /// Returns true if the provided mouse button was pressed this frame.
        /// <br/>0 = left, 1 = right, 2 = middle, 3 = back, 4 = forward.
        /// </summary>
        public static bool GetMouseButtonDown(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return SafeInput(() => inputHandler.GetMouseButtonDown(btn), false);
        }

        /// <summary>
        /// Returns true if the provided mouse button is being held down (not necessarily just pressed).
        /// <br/>0 = left, 1 = right, 2 = middle, 3 = back, 4 = forward.
        /// </summary>
        public static bool GetMouseButton(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return SafeInput(() => inputHandler.GetMouseButton(btn), false);
        }

        /// <summary>
        /// Returns true if the provided mouse button was released this frame. 
        /// <br/>0 = left, 1 = right, 2 = middle, 3 = back, 4 = forward.
        /// </summary>
        public static bool GetMouseButtonUp(int btn)
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return false;

            return SafeInput(() => inputHandler.GetMouseButtonUp(btn), false);
        }

        /// <summary>
        /// Calls the equivalent method for the current <see cref="InputType"/> to reset the Input axes.
        /// </summary>
        public static void ResetInputAxes()
        {
            if (Universe.CurrentGlobalState != Universe.GlobalState.SetupCompleted)
                return;

            SafeInput(() => { inputHandler.ResetInputAxes(); return true; }, false);
        }

        // ~~~~~~ Rebinding ~~~~~~

        /// <summary>
        /// Whether anything is currently using the Rebinding feature. If no UI is showing, this will return false.
        /// </summary>
        public static bool Rebinding 
        {
            get => isRebinding && UniversalUI.AnyUIShowing;
            set => isRebinding = value;
        }
        static bool isRebinding;

        /// <summary>
        /// The last pressed Key during rebinding.
        /// </summary>
        public static KeyCode? LastRebindKey { get; set; }

        internal static IEnumerable<KeyCode> allKeycodes;
        internal static Action<KeyCode> onRebindPressed;
        internal static Action<KeyCode?> onRebindFinished;

        internal static void Update()
        {
            if (Rebinding)
            {
                KeyCode? kc = GetCurrentKeyDown();
                if (kc != null)
                {
                    LastRebindKey = kc;
                    onRebindPressed?.Invoke((KeyCode)kc);
                }
            }
        }

        internal static KeyCode? GetCurrentKeyDown()
        {
            foreach (KeyCode kc in allKeycodes)
            {
                if (inputHandler.GetKeyDown(kc))
                    return kc;
            }

            return null;
        }

        /// <summary>
        /// Begins the Rebinding process, keys pressed will be recorded. Call <see cref="EndRebind"/> to finish rebinding.
        /// </summary>
        /// <param name="onSelection">Will be invoked whenever any key is pressed, even if rebinding has not finished yet.</param>
        /// <param name="onFinished">Invoked when EndRebind is called.</param>
        public static void BeginRebind(Action<KeyCode> onSelection, Action<KeyCode?> onFinished)
        {
            if (Rebinding)
                return;

            onRebindPressed = onSelection;
            onRebindFinished = onFinished;

            Rebinding = true;
            LastRebindKey = null;
        }

        /// <summary>
        /// Call this to finish Rebinding. The onFinished Action supplied to <see cref="BeginRebind"/> will be invoked if we are currently Rebinding.
        /// </summary>
        public static void EndRebind()
        {
            if (!Rebinding)
                return;

            Rebinding = false;
            onRebindFinished?.Invoke(LastRebindKey);

            onRebindFinished = null;
            onRebindPressed = null;
        }
    }
}