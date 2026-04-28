using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib.Input;

namespace UniverseLib.UI
{
    internal static class ValueChangedRelay
    {
        internal static void UpdateInstances()
        {
            ButtonClickRelay.UpdateInstances();
            DropdownValueChangedRelay.UpdateInstances();
            SliderValueChangedRelay.UpdateInstances();
            ScrollbarValueChangedRelay.UpdateInstances();
            ScrollRectValueChangedRelay.UpdateInstances();
            ToggleValueChangedRelay.UpdateInstances();
            InputFieldEndEditRelay.UpdateInstances();
            InputFieldValueChangedRelay.UpdateInstances();
        }
    }

    internal static class ButtonClickRelay
    {
        private sealed class State
        {
            internal readonly Button Button;
            internal RectTransform ButtonRect;
            internal Action OnClick;
            internal bool WasPointerDown;
            internal int LastInvokeFrame = -1;

            internal State(Button button)
            {
                Button = button;
                ButtonRect = button != null ? button.transform.TryCast<RectTransform>() : null;
            }

            internal bool Update()
            {
                if (!Button)
                    return false;

                if (!ButtonRect && Button.transform)
                    ButtonRect = Button.transform.TryCast<RectTransform>();

                if (!ButtonRect || !Button.enabled || !Button.gameObject.activeInHierarchy || !Button.IsInteractable())
                {
                    WasPointerDown = false;
                    return true;
                }

                // Data Center / Unity 6000 safe-click mode:
                // Trigger ButtonRef actions immediately on mouse-down instead of waiting for mouse-up.
                // In this game the first click can otherwise only focus/capture the UI and the real
                // button action may require a second click. Invoking on mouse-down makes one click enough.
                if (InputManager.GetMouseButtonDown(0) && IsPointerInside())
                {
                    WasPointerDown = false;

                    if (LastInvokeFrame == Time.frameCount)
                        return true;

                    LastInvokeFrame = Time.frameCount;
                    OnClick?.Invoke();
                    return true;
                }

                // Keep the old release path disabled in safe-click mode to avoid double-invoking.
                if (!WasPointerDown || InputManager.GetMouseButton(0))
                    return true;

                bool shouldInvoke = IsPointerInside();
                WasPointerDown = false;

                if (!shouldInvoke || LastInvokeFrame == Time.frameCount)
                    return true;

                LastInvokeFrame = Time.frameCount;
                OnClick?.Invoke();
                return true;
            }

            private bool IsPointerInside()
            {
                return ButtonRect &&
                    RectTransformUtility.RectangleContainsScreenPoint(ButtonRect, InputManager.MousePosition, null);
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();
        private static readonly List<int> updateKeys = new();

        internal static void Register(Button button, Action onClick)
        {
            if (!button || onClick == null)
                return;

            int key = button.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.Button)
                states[key] = state = new State(button);

            state.OnClick += onClick;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();
            updateKeys.Clear();

            foreach (int key in states.Keys)
                updateKeys.Add(key);

            for (int i = 0; i < updateKeys.Count; i++)
            {
                int key = updateKeys[i];
                if (states.TryGetValue(key, out State state) && !state.Update())
                    deadKeys.Add(key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class DropdownValueChangedRelay
    {
        private sealed class State
        {
            internal readonly Dropdown Dropdown;
            internal Action<int> OnValueChanged;
            internal int LastValue;

            internal State(Dropdown dropdown)
            {
                Dropdown = dropdown;
                LastValue = dropdown != null ? dropdown.value : 0;
            }

            internal bool Update()
            {
                if (!Dropdown)
                    return false;

                int currentValue = Dropdown.value;
                if (currentValue == LastValue)
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(Dropdown dropdown, Action<int> onValueChanged)
        {
            if (!dropdown || onValueChanged == null)
                return;

            int key = dropdown.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.Dropdown)
                states[key] = state = new State(dropdown);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class SliderValueChangedRelay
    {
        private sealed class State
        {
            internal readonly Slider Slider;
            internal Action<float> OnValueChanged;
            internal float LastValue;

            internal State(Slider slider)
            {
                Slider = slider;
                LastValue = slider != null ? slider.value : 0f;
            }

            internal bool Update()
            {
                if (!Slider)
                    return false;

                float currentValue = Slider.value;
                if (Mathf.Approximately(currentValue, LastValue))
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(Slider slider, Action<float> onValueChanged)
        {
            if (!slider || onValueChanged == null)
                return;

            int key = slider.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.Slider)
                states[key] = state = new State(slider);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class ScrollbarValueChangedRelay
    {
        private sealed class State
        {
            internal readonly Scrollbar Scrollbar;
            internal Action<float> OnValueChanged;
            internal float LastValue;

            internal State(Scrollbar scrollbar)
            {
                Scrollbar = scrollbar;
                LastValue = scrollbar != null ? scrollbar.value : 0f;
            }

            internal bool Update()
            {
                if (!Scrollbar)
                    return false;

                float currentValue = Scrollbar.value;
                if (Mathf.Approximately(currentValue, LastValue))
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(Scrollbar scrollbar, Action<float> onValueChanged)
        {
            if (!scrollbar || onValueChanged == null)
                return;

            int key = scrollbar.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.Scrollbar)
                states[key] = state = new State(scrollbar);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class ScrollRectValueChangedRelay
    {
        private sealed class State
        {
            internal readonly ScrollRect ScrollRect;
            internal Action<Vector2> OnValueChanged;
            internal Vector2 LastValue;

            internal State(ScrollRect scrollRect)
            {
                ScrollRect = scrollRect;
                LastValue = scrollRect != null ? scrollRect.normalizedPosition : Vector2.zero;
            }

            internal bool Update()
            {
                if (!ScrollRect)
                    return false;

                Vector2 currentValue = ScrollRect.normalizedPosition;
                if ((currentValue - LastValue).sqrMagnitude <= 0.000001f)
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(ScrollRect scrollRect, Action<Vector2> onValueChanged)
        {
            if (!scrollRect || onValueChanged == null)
                return;

            int key = scrollRect.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.ScrollRect)
                states[key] = state = new State(scrollRect);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class ToggleValueChangedRelay
    {
        private sealed class State
        {
            internal readonly Toggle Toggle;
            internal Action<bool> OnValueChanged;
            internal bool LastValue;

            internal State(Toggle toggle)
            {
                Toggle = toggle;
                LastValue = toggle != null && toggle.isOn;
            }

            internal bool Update()
            {
                if (!Toggle)
                    return false;

                bool currentValue = Toggle.isOn;
                if (currentValue == LastValue)
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(Toggle toggle, Action<bool> onValueChanged)
        {
            if (!toggle || onValueChanged == null)
                return;

            int key = toggle.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.Toggle)
                states[key] = state = new State(toggle);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class InputFieldEndEditRelay
    {
        private sealed class State
        {
            internal readonly InputField InputField;
            internal Action<string> OnEndEdit;
            internal bool WasFocused;
            internal int LastInvokeFrame = -1;

            internal State(InputField inputField)
            {
                InputField = inputField;
            }

            internal bool Update()
            {
                if (!InputField)
                    return false;

                if (!InputField.enabled || !InputField.gameObject.activeInHierarchy)
                {
                    WasFocused = false;
                    return true;
                }

                if (InputField.isFocused)
                {
                    WasFocused = true;
                    return true;
                }

                if (!WasFocused || LastInvokeFrame == Time.frameCount)
                    return true;

                WasFocused = false;
                LastInvokeFrame = Time.frameCount;
                OnEndEdit?.Invoke(InputField.text ?? string.Empty);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(InputField inputField, Action<string> onEndEdit)
        {
            if (!inputField || onEndEdit == null)
                return;

            int key = inputField.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.InputField)
                states[key] = state = new State(inputField);

            state.OnEndEdit += onEndEdit;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }

    internal static class InputFieldValueChangedRelay
    {
        private sealed class State
        {
            internal readonly InputField InputField;
            internal Action<string> OnValueChanged;
            internal string LastValue;

            internal State(InputField inputField)
            {
                InputField = inputField;
                LastValue = inputField != null ? inputField.text ?? string.Empty : string.Empty;
            }

            internal bool Update()
            {
                if (!InputField)
                    return false;

                string currentValue = InputField.text ?? string.Empty;
                if (currentValue == LastValue)
                    return true;

                LastValue = currentValue;
                OnValueChanged?.Invoke(currentValue);
                return true;
            }
        }

        private static readonly Dictionary<int, State> states = new();
        private static readonly List<int> deadKeys = new();

        internal static void Register(InputField inputField, Action<string> onValueChanged)
        {
            if (!inputField || onValueChanged == null)
                return;

            int key = inputField.GetInstanceID();
            if (!states.TryGetValue(key, out State state) || !state.InputField)
                states[key] = state = new State(inputField);

            state.OnValueChanged += onValueChanged;
        }

        internal static void UpdateInstances()
        {
            if (states.Count == 0)
                return;

            deadKeys.Clear();

            foreach (KeyValuePair<int, State> pair in states)
            {
                if (!pair.Value.Update())
                    deadKeys.Add(pair.Key);
            }

            for (int i = 0; i < deadKeys.Count; i++)
                states.Remove(deadKeys[i]);
        }
    }
}
