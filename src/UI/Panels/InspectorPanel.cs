using UnityExplorer.Inspectors;
using UniverseLib.UI;

namespace UnityExplorer.UI.Panels
{
    public class InspectorPanel : UEPanel
    {
        public static InspectorPanel Instance { get; private set; }

        public override string Name => "Inspector";
        public override UIManager.Panels PanelType => UIManager.Panels.Inspector;
        public override bool ShouldSaveActiveState => false;

        public override int MinWidth => 810;
        public override int MinHeight => 300;
        // Data Center / Unity 6000 compact layout: less screen coverage by default.
        public override Vector2 DefaultAnchorMin => new(0.40f, 0.18f);
        public override Vector2 DefaultAnchorMax => new(0.83f, 0.82f);

        public GameObject NavbarHolder;
        public Dropdown MouseInspectDropdown;
        public GameObject ContentHolder;
        public RectTransform ContentRect;

        public static float CurrentPanelWidth
        {
            get
            {
                if (Instance == null || !Instance.Rect)
                    return 0f;

                return Instance.Rect.rect.width;
            }
        }

        public static float CurrentPanelHeight
        {
            get
            {
                if (Instance == null || !Instance.Rect)
                    return 0f;

                return Instance.Rect.rect.height;
            }
        }

        public InspectorPanel(UIBase owner) : base(owner)
        {
            Instance = this;
        }

        public override void Update()
        {
            try
            {
                InspectorManager.Update();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.Update exception: {e}");
            }
        }

        public override void OnFinishResize()
        {
            try
            {
                base.OnFinishResize();

                if (!Rect)
                    return;

                InspectorManager.PanelWidth = Rect.rect.width;
                InspectorManager.OnPanelResized(Rect.rect.width);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.OnFinishResize exception: {e}");
            }
        }

        protected override void ConstructPanelContent()
        {
            try
            {
                if (!ExplorerCore.IsUnity6000OrNewer)
                    ConstructFullPanelContent();
                else
                    ConstructUnity6000SafePanelContent();
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.ConstructPanelContent exception: {e}");
                throw;
            }
        }

        private void ConstructFullPanelContent()
        {
            GameObject closeHolder = GetSafeControlsParent();

            // Inspect under mouse dropdown on title bar.
            GameObject mouseDropdown = UIFactory.CreateDropdown(closeHolder, "MouseInspectDropdown", out MouseInspectDropdown, "Mouse Inspect", 14,
                SafeMouseInspectDropdownSelect);
            UIFactory.SetLayoutElement(mouseDropdown, minHeight: 25, minWidth: 140);
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("Mouse Inspect"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("World"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("UI"));
            mouseDropdown.transform.SetSiblingIndex(0);

            // Add close all button to title bar.
            UniverseLib.UI.Models.ButtonRef closeAllBtn = UIFactory.CreateButton(closeHolder, "CloseAllBtn", "Close All",
                new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(closeAllBtn.Component.gameObject, minHeight: 25, minWidth: 80);
            closeAllBtn.Component.transform.SetSiblingIndex(closeAllBtn.Component.transform.GetSiblingIndex() - 1);
            closeAllBtn.OnClick += InspectorManager.CloseAllTabs;

            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, true, true, true, 4, padLeft: 5, padRight: 5);

            NavbarHolder = UIFactory.CreateGridGroup(ContentRoot, "Navbar", new Vector2(200f, 22f), new Vector2(4f, 4f),
                new Color(0.05f, 0.05f, 0.05f));
            NavbarHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ContentHolder = UIFactory.CreateVerticalGroup(ContentRoot, "ContentHolder", true, true, true, true, 0, default,
                new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ContentHolder, flexibleHeight: 9999);
            ContentRect = ContentHolder.GetComponent<RectTransform>();

            SetActive(false);
        }

        private void ConstructUnity6000SafePanelContent()
        {
            // Minimal safe bootstrap for Unity 6000 IL2CPP/CoreCLR.
            // Compact layout: keep the control row small and give the real inspector content the space.
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(
                ContentRoot,
                forceWidth: true,
                forceHeight: false,
                childControlWidth: true,
                childControlHeight: true,
                spacing: 3,
                padLeft: 4,
                padRight: 4,
                padTop: 2,
                padBottom: 4);

            GameObject controlsRow = UIFactory.CreateHorizontalGroup(
                ContentRoot,
                "InspectorControlsRow",
                false,
                true,
                true,
                true,
                5,
                new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(controlsRow, minHeight: 28, preferredHeight: 28, flexibleHeight: 0);

            GameObject mouseDropdown = UIFactory.CreateDropdown(
                controlsRow,
                "MouseInspectDropdown",
                out MouseInspectDropdown,
                "Mouse Inspect",
                14,
                SafeMouseInspectDropdownSelect);
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("Mouse Inspect"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("World"));
            MouseInspectDropdown.options.Add(new Dropdown.OptionData("UI"));
            UIFactory.SetLayoutElement(mouseDropdown, minHeight: 24, preferredHeight: 24, minWidth: 160, flexibleWidth: 9999, flexibleHeight: 0);

            UniverseLib.UI.Models.ButtonRef inspectWorldBtn = UIFactory.CreateButton(
                controlsRow,
                "InspectWorldBtn",
                "World",
                new Color(0.2f, 0.28f, 0.22f));
            UIFactory.SetLayoutElement(inspectWorldBtn.Component.gameObject, minHeight: 24, preferredHeight: 24, minWidth: 64, flexibleWidth: 0, flexibleHeight: 0);
            inspectWorldBtn.OnClick += () => StartMouseInspectSafe(MouseInspectMode.World);

            UniverseLib.UI.Models.ButtonRef inspectUiBtn = UIFactory.CreateButton(
                controlsRow,
                "InspectUiBtn",
                "UI",
                new Color(0.2f, 0.24f, 0.3f));
            UIFactory.SetLayoutElement(inspectUiBtn.Component.gameObject, minHeight: 24, preferredHeight: 24, minWidth: 48, flexibleWidth: 0, flexibleHeight: 0);
            inspectUiBtn.OnClick += () => StartMouseInspectSafe(MouseInspectMode.UI);

            UniverseLib.UI.Models.ButtonRef closeAllBtn = UIFactory.CreateButton(
                controlsRow,
                "CloseAllBtn",
                "Close All",
                new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(closeAllBtn.Component.gameObject, minHeight: 24, preferredHeight: 24, minWidth: 78, flexibleWidth: 0, flexibleHeight: 0);
            closeAllBtn.OnClick += InspectorManager.CloseAllTabs;

            NavbarHolder = UIFactory.CreateGridGroup(
                ContentRoot,
                "Navbar",
                new Vector2(185f, 20f),
                new Vector2(3f, 3f),
                new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(NavbarHolder, minHeight: 24, preferredHeight: 24, flexibleHeight: 0);

            ContentSizeFitter fitter = NavbarHolder.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ContentHolder = UIFactory.CreateVerticalGroup(
                ContentRoot,
                "ContentHolder",
                forceWidth: true,
                forceHeight: true,
                childControlWidth: true,
                childControlHeight: true,
                spacing: 0,
                bgColor: new Color(0.1f, 0.1f, 0.1f));

            UIFactory.SetLayoutElement(ContentHolder, minHeight: 140, flexibleWidth: 9999, flexibleHeight: 9999);
            ContentRect = ContentHolder.GetComponent<RectTransform>();
        }

        internal static void SafeResetMouseInspectDropdown()
        {
            try
            {
                if (Instance == null || Instance.MouseInspectDropdown == null)
                    return;

                if (Instance.MouseInspectDropdown.value != 0)
                    Instance.MouseInspectDropdown.value = 0;
            }
            catch
            {
            }
        }

        private GameObject GetSafeControlsParent()
        {
            try
            {
                if (TitleBar != null)
                {
                    Transform titleBarTransform = TitleBar.transform;
                    for (int i = 0; i < titleBarTransform.childCount; i++)
                    {
                        Transform child = titleBarTransform.GetChild(i);
                        if (child != null && string.Equals(child.name, "CloseHolder", StringComparison.Ordinal))
                            return child.gameObject;
                    }

                    return TitleBar;
                }
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.GetSafeControlsParent exception: {e}");
            }

            return TitleBar != null ? TitleBar : ContentRoot;
        }

        private void SafeMouseInspectDropdownSelect(int index)
        {
            try
            {
                ExplorerCore.Log($"InspectorPanel: MouseInspectDropdown changed -> {index}");

                if (MouseInspectDropdown == null)
                    return;

                // Ignore the initial "Mouse Inspect" placeholder selection.
                if (index <= 0)
                {
                    ExplorerCore.Log("InspectorPanel: MouseInspectDropdown ignored placeholder selection.");
                    return;
                }

                if (MouseInspector.Instance == null)
                {
                    ExplorerCore.LogWarning("MouseInspectDropdown fired before MouseInspector.Instance was available.");
                    MouseInspectDropdown.value = 0;
                    return;
                }

                MouseInspector.OnDropdownSelect(index);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.SafeMouseInspectDropdownSelect exception: {e}");

                try
                {
                    if (MouseInspectDropdown != null)
                        MouseInspectDropdown.value = 0;
                }
                catch
                {
                }
            }
        }

        private void StartMouseInspectSafe(MouseInspectMode mode)
        {
            try
            {
                ExplorerCore.Log($"InspectorPanel: StartMouseInspectSafe -> {mode}");

                if (MouseInspector.Instance == null)
                {
                    ExplorerCore.LogWarning($"InspectorPanel.StartMouseInspectSafe called for {mode}, but MouseInspector.Instance was null.");
                    return;
                }

                MouseInspector.Instance.StartInspect(mode);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"InspectorPanel.StartMouseInspectSafe({mode}) exception: {e}");
            }
        }
    }
}
