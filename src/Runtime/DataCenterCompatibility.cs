using UnityExplorer.Config;

namespace UnityExplorer.Runtime;

internal static class DataCenterCompatibility
{
    public static bool SafeModeActive =>
        ExplorerCore.IsUnity6000OrNewer &&
        (ConfigManager.Unity6000_EnableSafeMode?.Value ?? true);

    public static bool DeferPanelBootstrap =>
        SafeModeActive;

    public static bool DisableSceneExplorer =>
        SafeModeActive &&
        (ConfigManager.Unity6000_DisableSceneExplorer?.Value ?? true);

    public static bool UseSafeInputFallbacks =>
        SafeModeActive &&
        (ConfigManager.Unity6000_UseSafeInputFallbacks?.Value ?? true);

    public static bool UseSafeScrollFallbacks =>
        SafeModeActive &&
        (ConfigManager.Unity6000_UseSafeScrollFallbacks?.Value ?? true);

    public static bool DisableTimeScaleWidget =>
        SafeModeActive &&
        (ConfigManager.Unity6000_DisableTimeScaleWidget?.Value ?? true);

    public static string GetSummary()
    {
        if (!ExplorerCore.IsUnity6000OrNewer)
            return "Unity 6000 safe mode inactive: current Unity version does not require it.";

        return "Unity 6000 safe mode " + (SafeModeActive ? "enabled" : "disabled") +
            $"; SceneExplorer disabled={DisableSceneExplorer}" +
            $"; SafeInput={UseSafeInputFallbacks}" +
            $"; SafeScroll={UseSafeScrollFallbacks}" +
            $"; TimeScale disabled={DisableTimeScaleWidget}" +
            $"; Deferred panels={DeferPanelBootstrap}";
    }
}
