using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;
using UniverseLib.Utility;

namespace UniverseLib.Input
{
    public class LegacyInput : IHandleInput
    {
        public LegacyInput()
        {
            p_mousePosition = TInput.GetProperty("mousePosition");
            p_mouseDelta = TInput.GetProperty("mouseScrollDelta");
            m_getKey = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            m_getKeyDown = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            m_getKeyUp = TInput.GetMethod("GetKeyUp", new Type[] { typeof(KeyCode) });
            m_getMouseButton = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            m_getMouseButtonDown = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
            m_getMouseButtonUp = TInput.GetMethod("GetMouseButtonUp", new Type[] { typeof(int) });
            m_resetInputAxes = TInput.GetMethod("ResetInputAxes", ArgumentUtility.EmptyTypes);
        }

        public static Type TInput => t_Input ??= ReflectionUtility.GetTypeByName("UnityEngine.Input");
        private static Type t_Input;

        private static PropertyInfo p_mousePosition;
        private static PropertyInfo p_mouseDelta;
        private static MethodInfo m_getKey;
        private static MethodInfo m_getKeyDown;
        private static MethodInfo m_getKeyUp;
        private static MethodInfo m_getMouseButton;
        private static MethodInfo m_getMouseButtonDown;
        private static MethodInfo m_getMouseButtonUp;
        private static MethodInfo m_resetInputAxes;

        private static bool backendMismatchLogged;
        internal static bool backendDisabled;

        internal static bool IsBackendUnavailable => backendDisabled;

        private static T SafeInvoke<T>(Func<T> action, T fallback = default)
        {
            if (backendDisabled)
                return fallback;

            try
            {
                return action();
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogBackendMismatchOnce(tie.InnerException);
                return fallback;
            }
            catch (Exception ex)
            {
                LogBackendMismatchOnce(ex);
                return fallback;
            }
        }

        private static void SafeInvoke(Action action)
        {
            if (backendDisabled)
                return;

            try
            {
                action();
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogBackendMismatchOnce(tie.InnerException);
            }
            catch (Exception ex)
            {
                LogBackendMismatchOnce(ex);
            }
        }

        private static void LogBackendMismatchOnce(Exception ex)
        {
            if (backendMismatchLogged)
                return;

            backendMismatchLogged = true;
            backendDisabled = true;
            Universe.LogWarning($"LegacyInput disabled due to backend mismatch: {ex.Message}");
        }

        public Vector2 MousePosition => SafeInvoke(() => (Vector2)(Vector3)p_mousePosition.GetValue(null, null), Vector2.zero);
        public Vector2 MouseScrollDelta => SafeInvoke(() => (Vector2)p_mouseDelta.GetValue(null, null), Vector2.zero);

        public bool GetKey(KeyCode key) => SafeInvoke(() => (bool)m_getKey.Invoke(null, new object[] { key }), false);
        public bool GetKeyDown(KeyCode key) => SafeInvoke(() => (bool)m_getKeyDown.Invoke(null, new object[] { key }), false);
        public bool GetKeyUp(KeyCode key) => SafeInvoke(() => (bool)m_getKeyUp.Invoke(null, new object[] { key }), false);

        public bool GetMouseButton(int btn) => SafeInvoke(() => (bool)m_getMouseButton.Invoke(null, new object[] { btn }), false);
        public bool GetMouseButtonDown(int btn) => SafeInvoke(() => (bool)m_getMouseButtonDown.Invoke(null, new object[] { btn }), false);
        public bool GetMouseButtonUp(int btn) => SafeInvoke(() => (bool)m_getMouseButtonUp.Invoke(null, new object[] { btn }), false);

        public void ResetInputAxes() => SafeInvoke(() => m_resetInputAxes.Invoke(null, ArgumentUtility.EmptyArgs));

        // UI Input module

        public BaseInputModule UIInputModule => inputModule;
        internal StandaloneInputModule inputModule;

        public void AddUIInputModule()
        {
            inputModule = UniversalUI.CanvasRoot.gameObject.AddComponent<StandaloneInputModule>();
            inputModule.m_EventSystem = UniversalUI.EventSys;
        }

        public void ActivateModule()
        {
            try
            {
                inputModule.ActivateModule();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception enabling StandaloneInputModule: {ex}");
            }
        }
    }
}