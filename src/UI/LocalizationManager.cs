using UnityExplorer.Config;

namespace UnityExplorer.UI
{
    public static class LocalizationManager
    {
        public static MenuLanguage CurrentLanguage => ConfigManager.Menu_Language?.Value ?? MenuLanguage.English;

        public static string GetPanelLabel(UIManager.Panels panel, string fallbackEnglish)
        {
            if (CurrentLanguage != MenuLanguage.German)
                return fallbackEnglish;

            return panel switch
            {
                UIManager.Panels.ObjectExplorer => "Objekt-Explorer",
                UIManager.Panels.Inspector => "Inspektor",
                UIManager.Panels.CSConsole => "C#-Konsole",
                UIManager.Panels.Options => "Optionen",
                UIManager.Panels.ConsoleLog => "Protokoll",
                UIManager.Panels.AutoCompleter => "Autovervollständigung",
                UIManager.Panels.UIInspectorResults => "UI-Inspektor-Ergebnisse",
                UIManager.Panels.HookManager => "Hooks",
                UIManager.Panels.Clipboard => "Zwischenablage",
                UIManager.Panels.Freecam => "Freikamera",
                _ => fallbackEnglish,
            };
        }

        public static string GetText(string key, string fallbackEnglish)
        {
            if (CurrentLanguage != MenuLanguage.German)
                return fallbackEnglish;

            return key switch
            {
                "save_options" => "Optionen speichern",
                "current_paste" => "Aktueller Inhalt:",
                "generic_arguments" => "Generische Argumente",
                "arguments" => "Argumente",
                "scene_loader" => "Szenen-Lader",
                "none_asset_resource" => "Keiner (Asset/Ressource)",
                _ => fallbackEnglish,
            };
        }
    }
}
