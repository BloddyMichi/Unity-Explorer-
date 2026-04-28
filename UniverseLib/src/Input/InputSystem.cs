using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UniverseLib.UI;
using UniverseLib.Utility;

#if INTEROP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif
#if UNHOLLOWER
using UnhollowerBaseLib;
#endif

#nullable enable

namespace UniverseLib.Input;

public class InputSystem : IHandleInput
{
#region Reflection cache

    // typeof(InputSystem.Keyboard)
    public static Type TKeyboard => t_Keyboard ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard");
    static Type? t_Keyboard;

    // typeof(InputSystem.Mouse)
    public static Type TMouse => t_Mouse ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse");
    static Type? t_Mouse;

    // typeof (InputSystem.Key)
    public static Type TKey => t_Key ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key");
    static Type? t_Key;

    // InputSystem.Controls.ButtonControl.isPressed
    static PropertyInfo? p_btnIsPressed;
    // InputSystem.Controls.ButtonControl.wasPressedThisFrame
    static PropertyInfo? p_btnWasPressed;
    // InputSystem.Controls.ButtonControl.wasReleasedThisFrame
    static PropertyInfo? p_btnWasReleased;

    // Keyboard.current
    static object? CurrentKeyboard => p_kbCurrent?.GetValue(null, null);
    static PropertyInfo? p_kbCurrent;
    // Keyboard.this[Key]
    static PropertyInfo? p_kbIndexer;

    // Mouse.current
    static object? CurrentMouse => p_mouseCurrent?.GetValue(null, null);
    static PropertyInfo? p_mouseCurrent;

    // Mouse.current.leftButton
    static object? LeftMouseButton => p_leftButton?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_leftButton;

    // Mouse.current.rightButton
    static object? RightMouseButton => p_rightButton?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_rightButton;

    // Mouse.current.middleButton
    static object? MiddleMouseButton => p_middleButton?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_middleButton;

    // Mouse.current.forwardButton
    static object? ForwardMouseButton => p_forwardButton?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_forwardButton;

    // Mouse.current.backButton
    static object? BackMouseButton => p_backButton?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_backButton;

    // InputSystem.InputControl<Vector2>.ReadValue()
    static MethodInfo? m_ReadV2Control;

    // Mouse.current.position
    static object? MousePositionInfo => p_position?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_position;

    // Mouse.current.scroll
    static object? MouseScrollInfo => p_scrollDelta?.GetValue(CurrentMouse, null);
    static PropertyInfo? p_scrollDelta;

    // typeof(InputSystem.UI.InputSystemUIInputModule)
    public Type TInputSystemUIInputModule => t_UIInputModule
                                          ??= ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
    internal Type? t_UIInputModule;

    // Our UI input module
    public BaseInputModule? UIInputModule => newInputModule;
    internal BaseInputModule? newInputModule;

    // UI input action maps
    Type? t_InputExtensions;
    object? UIActionMap;
    MethodInfo? m_UI_Enable;
    PropertyInfo? p_actionsAsset;

#endregion

    public InputSystem()
    {
        SetupSupportedDevices();

        p_kbCurrent = TKeyboard.GetProperty("current");
        p_kbIndexer = TKeyboard.GetProperty("Item", [ TKey ]);

        Type t_btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
        p_btnIsPressed = t_btnControl.GetProperty("isPressed");
        p_btnWasPressed = t_btnControl.GetProperty("wasPressedThisFrame");
        p_btnWasReleased = t_btnControl.GetProperty("wasReleasedThisFrame");

        p_mouseCurrent = TMouse.GetProperty("current");
        p_leftButton = TMouse.GetProperty("leftButton");
        p_rightButton = TMouse.GetProperty("rightButton");
        p_middleButton = TMouse.GetProperty("middleButton");
        p_backButton = TMouse.GetProperty("backButton");
        p_forwardButton = TMouse.GetProperty("forwardButton");
        p_scrollDelta = TMouse.GetProperty("scroll");

        p_position = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer")
                       .GetProperty("position");

        m_ReadV2Control = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                  .MakeGenericType(typeof(Vector2))
                                  .GetMethod("ReadValue");
    }

    internal static void SetupSupportedDevices()
    {
        try
        {
            // typeof(InputSystem)
            Type t_InputSystem = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputSystem");
            // InputSystem.settings
            object? settings = t_InputSystem.GetProperty("settings", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
            if (settings is null)
            {
                throw new NullReferenceException("UnityEngine.InputSystem.InputSystem.setting is Null");
            }
            // typeof(InputSettings)
            Type t_Settings = settings.GetActualType();
            // InputSettings.supportedDevices
            PropertyInfo? supportedProp = t_Settings.GetProperty("supportedDevices", BindingFlags.Public | BindingFlags.Instance);
            if (supportedProp is null)
            {
                throw new NullReferenceException("UnityEngine.InputSystem.InputSystem.setting supportedDevices prop not found");
            }
            object? supportedDevices = supportedProp.GetValue(settings, null);
            if (supportedDevices is null)
            {
                throw new NullReferenceException("UnityEngine.InputSystem.InputSystem.setting supportedDevices is null");
            }

            // An empty supportedDevices list means all devices are supported.
            Type supportedDeviceType = supportedDevices.GetActualType();
#if CPP
            // weird hack for il2cpp, use the implicit operator and cast Il2CppStringArray to ReadOnlyArray<string>
            object[] emptyStringArray =[ new Il2CppStringArray(0) ];
            MethodInfo? op_implicit = supportedDeviceType.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public);
            if (op_implicit is null)
            {
                throw new NullReferenceException("UnityEngine.InputSystem.InputSystem.setting supportedDevicesop_implicit is null");
            }
            supportedProp.SetValue(settings, op_implicit.Invoke(null, emptyStringArray), null);
#else
            supportedProp.SetValue(settings, Activator.CreateInstance(supportedDeviceType, new string[0]), null);
#endif
        }
        catch (Exception ex)
        {
            Universe.LogWarning($"Exception setting up InputSystem.settings.supportedDevices list!");
            Universe.Log(ex);
        }
    }

    // Input API

    public Vector2 MousePosition
    {
        get
        {
            if (m_ReadV2Control is null)
            {
                return default;
            }
            object? mousePos = m_ReadV2Control.Invoke(MousePositionInfo, ArgumentUtility.EmptyArgs);
            return mousePos is Vector2 vector ? vector : default;
        }
    }

    public Vector2 MouseScrollDelta
    {
        get
        {
            if (m_ReadV2Control is null)
            {
                return default;
            }
            object? scrollDelta = m_ReadV2Control.Invoke(MouseScrollInfo, ArgumentUtility.EmptyArgs);
            return scrollDelta is Vector2 vector ? vector : default;
        }
    }

    static object? GetMouseButtonObject(int btn) => btn switch
    {
        0 => LeftMouseButton,
        1 => RightMouseButton,
        2 => MiddleMouseButton,
        3 => BackMouseButton,
        4 => ForwardMouseButton,
        _ => throw new NotImplementedException()
    };

    public bool GetMouseButtonDown(int btn)
    {
        if (p_btnWasPressed is null)
        {
            return false;
        }
        object? pressed = p_btnWasPressed.GetValue(GetMouseButtonObject(btn), null);
        return pressed is bool b && b;
    }

    public bool GetMouseButton(int btn)
    {
        if (p_btnIsPressed is null)
        {
            return false;
        }
        object? pressed = p_btnIsPressed.GetValue(GetMouseButtonObject(btn), null);
        return pressed is bool b && b;
    }

    public bool GetMouseButtonUp(int btn)
    {
        if (p_btnWasReleased is null)
        {
            return false;
        }
        object? released = p_btnWasReleased.GetValue(GetMouseButtonObject(btn), null);
        return released is bool b && b;
    }

#region KeyCode <-> Key Helpers

    public static Dictionary<KeyCode, object?> KeyCodeToKeyDict = new();
    public static Dictionary<KeyCode, object?> KeyCodeToKeyEnumDict = new();

    internal static Dictionary<string, string> keycodeToKeyFixes = new()
    {
        { "Control", "Ctrl" },
        { "Return", "Enter" },
        { "Alpha", "Digit" },
        { "Keypad", "Numpad" },
        { "Numlock", "NumLock" },
        { "Print", "PrintScreen" },
        { "BackQuote", "Backquote" }
    };

    public static object? KeyCodeToActualKey(KeyCode key)
    {
        if (!KeyCodeToKeyDict.ContainsKey(key))
        {
            object? parsed = KeyCodeToKeyEnum(key);
            object? actualKey = p_kbIndexer is null ? default : p_kbIndexer.GetValue(CurrentKeyboard, [parsed]);
            KeyCodeToKeyDict.Add(key, actualKey);
        }

        return KeyCodeToKeyDict[key];
    }

    public static object? KeyCodeToKeyEnum(KeyCode key)
    {
        if (!KeyCodeToKeyEnumDict.ContainsKey(key))
        {
            string s = key.ToString();
            try
            {
                if (keycodeToKeyFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                    s = s.Replace(entry.Key, entry.Value);
            }
            catch { /* suppressed */ }

            try
            {
                object parsed = Enum.Parse(TKey, s);
                KeyCodeToKeyEnumDict.Add(key, parsed);
            }
            catch (Exception ex)
            {
                Universe.Log(ex);
                KeyCodeToKeyEnumDict.Add(key, default);
            }
        }

        return KeyCodeToKeyEnumDict[key];
    }

#endregion

    public bool GetKeyDown(KeyCode key)
    {
        object? actual = KeyCodeToActualKey(key);
        if (p_btnWasPressed is null)
        {
            return false;
        }
        object? pressed = p_btnWasPressed.GetValue(actual, null);
        return pressed is bool b && b;
    }

    public bool GetKey(KeyCode key)
    {
        object? actual = KeyCodeToActualKey(key);
        if (p_btnIsPressed is null)
        {
            return false;
        }
        object? pressed = p_btnIsPressed.GetValue(actual, null);
        return pressed is bool b && b;
    }

    public bool GetKeyUp(KeyCode key)
    {
        object? actual = KeyCodeToActualKey(key);
        if (p_btnWasReleased is null)
        {
            return false;
        }
        object? released = p_btnWasReleased.GetValue(actual, null);
        return released is bool b && b;
    }

    // InputSystem has no equivalent API for "ResetInputAxes".

    public void ResetInputAxes()
    {
    }

    // UI Input

    public void AddUIInputModule()
    {
        if (TInputSystemUIInputModule == null)
        {
            Universe.LogWarning("Unable to find UI Input Module Type, Input will not work!");
            return;
        }

        Type assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
        newInputModule = RuntimeHelper.AddComponent<BaseInputModule>(UniversalUI.CanvasRoot, TInputSystemUIInputModule);
        object asset = RuntimeHelper.CreateScriptable(assetType).TryCast(assetType);

        t_InputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

        MethodInfo? addMap = t_InputExtensions.GetMethod("AddActionMap", [ assetType, typeof(string) ]);
        if (addMap is null)
        {
            Universe.LogWarning("Unable to add ActionMap, Input will not work!");
            return;
        }
        object map = addMap.Invoke(null, [ asset, "UI" ])
            .TryCast(ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionMap"));

        CreateAction(map, "point", [ "<Mouse>/position" ], "point");
        CreateAction(map, "click", [ "<Mouse>/leftButton" ], "leftClick");
        CreateAction(map, "rightClick", [ "<Mouse>/rightButton" ], "rightClick");
        CreateAction(map, "scrollWheel", [ "<Mouse>/scroll" ], "scrollWheel");

        m_UI_Enable = map.GetType().GetMethod("Enable");
        if (m_UI_Enable is null)
        {
            Universe.LogWarning("Unable to Enable ActionMap, Input will not work!");
            return;
        }
        m_UI_Enable.Invoke(map, ArgumentUtility.EmptyArgs);
        UIActionMap = map;

        p_actionsAsset = TInputSystemUIInputModule.GetProperty("actionsAsset");
    }

    private void CreateAction(object map, string actionName, string[] bindings, string propertyName)
    {
        if (TInputSystemUIInputModule == null)
        {
            Universe.LogWarning($"Unable to find UI Input Module Type, Input Action:{actionName} will not work!");
            return;
        }

        MethodInfo? disable = map.GetType().GetMethod("Disable");
        if (disable is null)
        {
            Universe.LogWarning($"Unable to Disable ActionMap, Input Action:{actionName} will not work!");
            return;
        }
        disable.Invoke(map, ArgumentUtility.EmptyArgs);

        Type inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction");
        MethodInfo? addAction = t_InputExtensions?.GetMethod("AddAction");
        if (addAction is null)
        {
            Universe.LogWarning($"Unable to AddAction, Input Action:{actionName} will not work!");
            return;
        }
        object action = addAction.Invoke(null, [ map, actionName, default, null, null, null, null, null ])
            .TryCast(inputActionType);

        MethodInfo? addBinding = t_InputExtensions?.GetMethod(
            "AddBinding", [ inputActionType, typeof(string), typeof(string), typeof(string), typeof(string) ]);
        if (addBinding is null)
        {
            Universe.LogWarning($"Unable to AddBinding, Input Action:{actionName} will not work!");
            return;
        }

        foreach (string binding in bindings)
            addBinding.Invoke(null, [ action.TryCast(inputActionType), binding, null, null, null ]);

        Type refType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference");
        object? inputRef = refType.GetMethod("Create")?.Invoke(null, [ action ]).TryCast(refType);

        TInputSystemUIInputModule
            .GetProperty(propertyName)?
            .SetValue(newInputModule.TryCast(TInputSystemUIInputModule), inputRef, null);
    }

    public void ActivateModule()
    {
        try
        {
            BaseInputModule newInput = (BaseInputModule)newInputModule.TryCast(TInputSystemUIInputModule);
            newInput.m_EventSystem = UniversalUI.EventSys;
            newInput.ActivateModule();
            m_UI_Enable?.Invoke(UIActionMap, ArgumentUtility.EmptyArgs);

            // if the actionsAsset is null, call the AssignDefaultActions method.
            if (p_actionsAsset?.GetValue(newInput.TryCast(p_actionsAsset.DeclaringType), null) == null)
            {
                MethodInfo? assignDefaultMethod = newInput.GetType()
                    .GetMethod("AssignDefaultActions");
                if (assignDefaultMethod != null)
                    assignDefaultMethod.Invoke(newInput.TryCast(assignDefaultMethod.DeclaringType), new object[0]);
                else
                    Universe.Log("AssignDefaultActions method is null!");
            }
        }
        catch (Exception ex)
        {
            Universe.LogWarning("Exception enabling InputSystem UI Input Module: " + ex);
        }
    }
}