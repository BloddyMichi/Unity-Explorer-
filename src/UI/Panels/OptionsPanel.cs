using UnityExplorer.CacheObject;
using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;
using UniverseLib.UI;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class OptionsPanel : UEPanel, ICacheObjectController, ICellPoolDataSource<ConfigEntryCell>
    {
        public override string Name => "Options";
        public override UIManager.Panels PanelType => UIManager.Panels.Options;

        public override int MinWidth => 600;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.5f, 0.1f);
        public override Vector2 DefaultAnchorMax => new(0.5f, 0.85f);

        public override bool ShouldSaveActiveState => false;
        public override bool ShowByDefault => false;

        private static readonly KeyCode[] MouseInspectKeybindChoices =
        {
            KeyCode.None,
            KeyCode.F8,
            KeyCode.F9,
            KeyCode.F10,
            KeyCode.F11,
            KeyCode.Insert,
            KeyCode.Home,
            KeyCode.End,
            KeyCode.PageUp,
            KeyCode.PageDown,
            KeyCode.Mouse2,
            KeyCode.Mouse3,
            KeyCode.Mouse4,
            KeyCode.Mouse5
        };

        // Entry holders
        private readonly List<CacheConfigEntry> configEntries = new();

        // ICacheObjectController
        public CacheObjectBase ParentCacheObject => null;
        public object Target => null;
        public Type TargetType => null;
        public bool CanWrite => true;

        // ICellPoolDataSource
        public int ItemCount => configEntries.Count;

        public OptionsPanel(UIBase owner) : base(owner)
        {
            foreach (KeyValuePair<string, IConfigElement> entry in ConfigManager.ConfigElements)
            {
                CacheConfigEntry cache = new(entry.Value)
                {
                    Owner = this
                };
                configEntries.Add(cache);
            }

            foreach (CacheConfigEntry config in configEntries)
                config.UpdateValueFromSource();
        }

        public void OnCellBorrowed(ConfigEntryCell cell)
        {
        }

        public void SetCell(ConfigEntryCell cell, int index)
        {
            CacheObjectControllerHelper.SetCell(cell, index, this.configEntries, null);
        }

        // UI Construction

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600f);
        }

        protected override void ConstructPanelContent()
        {
            // Save button

            UniverseLib.UI.Models.ButtonRef saveBtn = UIFactory.CreateButton(this.ContentRoot, "Save", LocalizationManager.GetText("save_options", "Save Options"), new Color(0.2f, 0.3f, 0.2f));
            ConfigManager.Menu_Language.OnValueChanged += _ =>
            {
                if (saveBtn?.ButtonText != null)
                    saveBtn.ButtonText.text = LocalizationManager.GetText("save_options", "Save Options");
            };
            UIFactory.SetLayoutElement(saveBtn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);
            saveBtn.OnClick += ConfigManager.Handler.SaveConfig;

            CreateMouseInspectKeybindDropdowns();

            // Config entries

            ScrollPool<ConfigEntryCell> scrollPool = UIFactory.CreateScrollPool<ConfigEntryCell>(
                this.ContentRoot, 
                "ConfigEntries", 
                out GameObject scrollObj,
                out GameObject scrollContent);

            scrollPool.Initialize(this);
        }

        private void CreateMouseInspectKeybindDropdowns()
        {
            GameObject block = UIFactory.CreateVerticalGroup(
                ContentRoot,
                "DataCenterMouseInspectKeybindBlock",
                true,
                false,
                true,
                true,
                4,
                new Vector4(6, 6, 6, 6),
                new Color(0.08f, 0.08f, 0.08f));

            UIFactory.SetLayoutElement(block, minHeight: 110, flexibleHeight: 0, flexibleWidth: 9999);

            Text header = UIFactory.CreateLabel(
                block,
                "DataCenterMouseInspectKeybindHeader",
                "<b>Data Center Mouse Inspect Keybinds</b>",
                TextAnchor.MiddleLeft);

            UIFactory.SetLayoutElement(header.gameObject, minHeight: 22, flexibleWidth: 9999, flexibleHeight: 0);

            Text hint = UIFactory.CreateLabel(
                block,
                "DataCenterMouseInspectKeybindHint",
                "Use these dropdowns if the normal KeyCode field does not accept input. Recommended: World = F8, UI = F9.",
                TextAnchor.MiddleLeft);

            hint.fontSize = 12;
            hint.color = new Color(0.75f, 0.75f, 0.75f);
            UIFactory.SetLayoutElement(hint.gameObject, minHeight: 24, flexibleWidth: 9999, flexibleHeight: 0);

            CreateMouseInspectKeybindRow(
                block,
                "World Inspect",
                ConfigManager.World_MouseInspect_Keybind,
                KeyCode.F8);

            CreateMouseInspectKeybindRow(
                block,
                "UI Inspect",
                ConfigManager.UI_MouseInspect_Keybind,
                KeyCode.F9);
        }

        private void CreateMouseInspectKeybindRow(
            GameObject parent,
            string label,
            ConfigElement<KeyCode> config,
            KeyCode recommendedKey)
        {
            GameObject row = UIFactory.CreateHorizontalGroup(
                parent,
                label.Replace(" ", "") + "KeybindRow",
                true,
                false,
                true,
                true,
                6,
                new Vector4(0, 0, 0, 0));

            UIFactory.SetLayoutElement(row, minHeight: 28, flexibleHeight: 0, flexibleWidth: 9999);

            Text labelText = UIFactory.CreateLabel(
                row,
                label.Replace(" ", "") + "KeybindLabel",
                label,
                TextAnchor.MiddleLeft);

            UIFactory.SetLayoutElement(labelText.gameObject, minWidth: 120, minHeight: 24, flexibleWidth: 0, flexibleHeight: 0);

            GameObject dropdownObject = UIFactory.CreateDropdown(
                row,
                label.Replace(" ", "") + "KeybindDropdown",
                out Dropdown dropdown,
                config.Value.ToString(),
                14,
                index =>
                {
                    if (index < 0 || index >= MouseInspectKeybindChoices.Length)
                        return;

                    KeyCode selected = MouseInspectKeybindChoices[index];
                    config.Value = selected;

                    ExplorerCore.Log(label + " keybind set to " + selected);

                    try
                    {
                        ConfigManager.Handler.SaveConfig();
                    }
                    catch (Exception ex)
                    {
                        ExplorerCore.LogWarning("Failed to save " + label + " keybind: " + ex.Message);
                    }
                });

            dropdown.options.Clear();

            foreach (KeyCode key in MouseInspectKeybindChoices)
            {
                string text = key == recommendedKey
                    ? key + "  (recommended)"
                    : key.ToString();

                dropdown.options.Add(new Dropdown.OptionData(text));
            }

            dropdown.value = GetMouseInspectKeyIndex(config.Value);
            dropdown.RefreshShownValue();

            UIFactory.SetLayoutElement(dropdownObject, minWidth: 220, minHeight: 24, flexibleWidth: 9999, flexibleHeight: 0);
        }

        private static int GetMouseInspectKeyIndex(KeyCode key)
        {
            for (int i = 0; i < MouseInspectKeybindChoices.Length; i++)
            {
                if (MouseInspectKeybindChoices[i] == key)
                    return i;
            }

            return 0;
        }
    }
}
