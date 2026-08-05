global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using UnityEngine;
global using UnityEngine.UI;
global using UniverseLib;
global using UniverseLib.Utility;

using UnityExplorer.Config;
using UnityExplorer.ObjectExplorer;
using UnityExplorer.Runtime;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

using HarmonyPatch = HarmonyLib.Harmony;

namespace UnityExplorer;

public static class ExplorerCore
{
    public const string NAME = "UnityExplorer";
    // Upstream UnityExplorer base version this fork is built on. This value is
    // logged at runtime and drives the assembly version. The Data Center fork's
    // own release series (v1.x) is tracked separately in CHANGELOG.md.
    public const string VERSION = "4.13.5";
    public const string AUTHOR = "Sinai, yukieiji";
    public const string GUID = "com.sinai.unityexplorer";

    // The Unity version never changes at runtime, so evaluate the (native,
    // IL2CPP-marshalled) Application.unityVersion access once and cache it.
    // This is the single source of truth for the Unity 6000 safe-mode gate.
    public static bool IsUnity6000OrNewer { get; } =
        (Application.unityVersion ?? string.Empty).StartsWith("6000.", StringComparison.Ordinal);

    /// <summary>
    /// True when the Unity 6000 safe mode should restrict the paths that can
    /// hard-crash (AccessViolation / native wrapper invalidation) on IL2CPP:
    /// Scene enumeration, child-transform traversal, the pooled inspector
    /// ScrollView and the TimeScale widget. These are managed by native SEH
    /// crashes that try/catch cannot handle, so they stay off unless the user
    /// opts in via the "Unity 6000 Experimental Native Paths" config. The
    /// null-conditional keeps this safe if evaluated before config init.
    /// </summary>
    public static bool Unity6000RestrictNativePaths =>
        IsUnity6000OrNewer && !(ConfigManager.Unity6000_Experimental_Native_Paths?.Value ?? false);

    public static IExplorerLoader Loader { get; private set; }
    public static string ExplorerFolder => Path.Combine(Loader.ExplorerFolderDestination, Loader.ExplorerFolderName);
    public const string DEFAULT_EXPLORER_FOLDER_NAME = "sinai-dev-UnityExplorer";

    public static HarmonyPatch Harmony { get; } = new HarmonyPatch(GUID);

    /// <summary>
    /// Initialize UnityExplorer with the provided Loader implementation.
    /// </summary>
    public static void Init(IExplorerLoader loader)
    {
        if (Loader != null)
            throw new Exception("UnityExplorer is already loaded.");

        Loader = loader;

        Log($"{NAME} {VERSION} initializing...");
        LogRuntimeCompatibility();

        CheckLegacyExplorerFolder();
        Directory.CreateDirectory(ExplorerFolder);
        ConfigManager.Init(Loader.ConfigHandler);

        Universe.Init(ConfigManager.Startup_Delay_Time.Value, LateInit, Log, new()
        {
            Disable_EventSystem_Override = ConfigManager.Disable_EventSystem_Override.Value,
            Force_Unlock_Mouse = ConfigManager.Force_Unlock_Mouse.Value,
            Disable_Setup_Force_ReLoad_ManagedAssemblies = ConfigManager.Disable_Setup_Force_ReLoad_ManagedAssemblies.Value,
            Bypass_UniverseLib_ICall = ConfigManager.Bypass_UniverseLib_ICall.Value,
            Unhollowed_Modules_Folder = loader.UnhollowedModulesFolder
        });

        UERuntimeHelper.Init();
        ExplorerBehaviour.Setup();
        UnityCrashPrevention.Init();
    }

    // Do a delayed setup so that objects aren't destroyed instantly.
    // This can happen for a multitude of reasons.
    // Default delay is 1 second which is usually enough.
    static void LateInit()
    {
        if (Unity6000RestrictNativePaths)
        {
            Log($"Skipping SceneHandler.Init on Unity {Application.unityVersion} due to Unity 6 IL2CPP/CoreCLR stability issues.");
        }
        else
        {
            try
            {
                SceneHandler.Init();
            }
            catch (Exception ex)
            {
                LogWarning($"SceneHandler.Init failed. UnityExplorer will continue in limited mode: {ex}");
            }
        }

        try
        {
            Log("Creating UI...");
            UIManager.InitUI();
            Log($"{NAME} {VERSION} ({Universe.Context}) initialized.");
        }
        catch (Exception ex)
        {
            LogError($"UI initialization failed: {ex}");
        }

        // InspectorManager.Inspect(typeof(Tests.TestClass));
    }

    private static void LogRuntimeCompatibility()
    {
        string unityVersion = Application.unityVersion ?? "unknown";
        string safeMode = IsUnity6000OrNewer ? "enabled" : "disabled";

        Log($"Runtime compatibility: Unity {unityVersion}, Unity 6000 safe mode {safeMode}.");

        if (IsUnity6000OrNewer)
        {
            Log("Unity 6000 safe mode will disable or replace unstable scene, scroll-view and input-field paths.");
        }
    }

    internal static void Update()
    {
        ExplorerKeybind.Update();
    }


    #region LOGGING

    public static void Log(object message)
        => Log(message, LogType.Log);

    public static void LogWarning(object message)
        => Log(message, LogType.Warning);

    public static void LogError(object message)
        => Log(message, LogType.Error);

    public static void LogUnity(object message, LogType logType)
    {
        if (!ConfigManager.Log_Unity_Debug.Value)
            return;

        Log($"[Unity] {message}", logType);
    }

    private static void Log(object message, LogType logType)
    {
        string log = message?.ToString() ?? "";

        LogPanel.Log(log, logType);

        switch (logType)
        {
            case LogType.Assert:
            case LogType.Log:
                Loader.OnLogMessage(log);
                break;

            case LogType.Warning:
                Loader.OnLogWarning(log);
                break;

            case LogType.Error:
            case LogType.Exception:
                Loader.OnLogError(log);
                break;
        }
    }

    #endregion


    #region LEGACY FOLDER MIGRATION

    // Can be removed eventually. For migration from <4.7.0
    static void CheckLegacyExplorerFolder()
    {
        string legacyPath = Path.Combine(Loader.ExplorerFolderDestination, "UnityExplorer");
        if (Directory.Exists(legacyPath))
        {
            LogWarning($"Attempting to migrate old 'UnityExplorer/' folder to 'sinai-dev-UnityExplorer/'...");

            // If new folder doesn't exist yet, let's just use Move().
            if (!Directory.Exists(ExplorerFolder))
            {
                try
                {
                    Directory.Move(legacyPath, ExplorerFolder);
                    Log("Migrated successfully.");
                }
                catch (Exception ex)
                {
                    LogWarning($"Exception migrating folder: {ex}");
                }
            }
            else // We have to merge
            {
                try
                {
                    CopyAll(new(legacyPath), new(ExplorerFolder));
                    Directory.Delete(legacyPath, true);
                    Log("Migrated successfully.");
                }
                catch (Exception ex)
                {
                    LogWarning($"Exception migrating folder: {ex}");
                }
            }
        }
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into it's new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.MoveTo(Path.Combine(target.ToString(), fi.Name));
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    #endregion
}
