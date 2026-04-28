using UnityExplorer.Config;
using UnityExplorer.CSConsole;
using UnityExplorer.Inspectors;
using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI
{
    public static class UIManager
    {
        public enum Panels
        {
            ObjectExplorer,
            Inspector,
            CSConsole,
            Options,
            ConsoleLog,
            AutoCompleter,
            UIInspectorResults,
            HookManager,
            Clipboard,
            Freecam
        }

        public enum VerticalAnchor
        {
            Top,
            Bottom
        }

        public static VerticalAnchor NavbarAnchor = VerticalAnchor.Top;

        public static bool Initializing { get; internal set; } = true;

        internal static UIBase UiBase { get; private set; }
        public static GameObject UIRoot => UiBase?.RootObject;
        public static RectTransform UIRootRect { get; private set; }
        public static Canvas UICanvas { get; private set; }

        internal static readonly Dictionary<Panels, UEPanel> UIPanels = new();
        internal static readonly Dictionary<Panels, Func<UEPanel>> DeferredPanelFactories = new();
        internal static readonly Dictionary<Panels, ButtonRef> DeferredNavButtons = new();

        public static RectTransform NavBarRect;
        public static GameObject NavbarTabButtonHolder;
        private static readonly Vector2 NAVBAR_DIMENSIONS = new(1020f, 35f);

        private static ButtonRef closeBtn;
        private static Text titleLabel;

        private static int lastScreenWidth;
        private static int lastScreenHeight;
        private static bool mouseInspectorInitialized;
        private static bool consoleControllerInitialized;

        private static bool UseUnity6000Fallbacks => ExplorerCore.IsUnity6000OrNewer;

        public static bool ShowMenu
        {
            get => UiBase != null && UiBase.Enabled;
            set
            {
                if (UiBase == null || !UIRoot || UiBase.Enabled == value)
                    return;

                UniversalUI.SetUIActive(ExplorerCore.GUID, value);
                if (MouseInspector.inspectorUIBase != null)
                    UniversalUI.SetUIActive(MouseInspector.UIBaseGUID, value);
            }
        }

        // Initialization

        internal static void InitUI()
        {
            UiBase = UniversalUI.RegisterUI<ExplorerUIBase>(ExplorerCore.GUID, Update);

            UIRootRect = UIRoot.GetComponent<RectTransform>();
            UICanvas = UIRoot.GetComponent<Canvas>();

            if (UseUnity6000Fallbacks)
                ExplorerCore.Log($"UI Stage: Skip DisplayManager.Init on Unity {Application.unityVersion}");
            else
                DisplayManager.Init();

            (lastScreenWidth, lastScreenHeight) = GetCurrentScreenDimensions();

            // Create UI.
            CreateTopNavBar();
            // This could be automated with Assembly.GetTypes(),
            // but the order is important and I'd have to write something to handle the order.
            if (UseUnity6000Fallbacks)
            {
                ExplorerCore.Log($"UI Stage: Enable deferred panel bootstrap on Unity {Application.unityVersion}");
                RegisterDeferredPanels();
            }
            else
            {
                TryCreatePanel(Panels.AutoCompleter, () => new AutoCompleteModal(UiBase));
                TryCreatePanel(Panels.ObjectExplorer, () => new ObjectExplorerPanel(UiBase));
                TryCreatePanel(Panels.Inspector, () => new InspectorPanel(UiBase));
                TryCreatePanel(Panels.CSConsole, () => new CSConsolePanel(UiBase));
                TryCreatePanel(Panels.HookManager, () => new HookManagerPanel(UiBase));
                TryCreatePanel(Panels.Freecam, () => new FreeCamPanel(UiBase));
                TryCreatePanel(Panels.Clipboard, () => new ClipboardPanel(UiBase));
                TryCreatePanel(Panels.ConsoleLog, () => new LogPanel(UiBase));
                TryCreatePanel(Panels.Options, () => new OptionsPanel(UiBase));
                TryCreatePanel(Panels.UIInspectorResults, () => new MouseInspectorResultsPanel(UiBase));

                InitializeMouseInspector();
            }

            // Call some initialize methods
            Notification.Init(UIRoot);
            if (UseUnity6000Fallbacks)
                ExplorerCore.Log($"UI Stage: Skip ConsoleController.Init until CSConsole is opened on Unity {Application.unityVersion}");
            else
                InitializeConsoleController();

            // Failsafe fix, in some games all dropdowns displayed values are blank on startup for some reason.
            foreach (Dropdown dropdown in UIRoot.GetComponentsInChildren<Dropdown>(true))
                dropdown.RefreshShownValue();

            Initializing = false;

            if (ConfigManager.Hide_On_Startup.Value)
                ShowMenu = false;
        }

        // Main UI Update loop

        public static void Update()
        {
            if (!UIRoot)
                return;

            // If we are doing a Mouse Inspect, we don't need to update anything else.
            if (MouseInspector.Instance != null && MouseInspector.Instance.TryUpdate())
                return;

            // Update Notification modal
            Notification.Update();

            // Check forceUnlockMouse toggle
            if (InputManager.GetKeyDown(ConfigManager.Force_Unlock_Toggle.Value))
                UniverseLib.Config.ConfigManager.Force_Unlock_Mouse = !UniverseLib.Config.ConfigManager.Force_Unlock_Mouse;

            // update the timescale value
            TimeScaleWidget.Instance?.Update();

            // check screen dimension change
            (int currentWidth, int currentHeight) = GetCurrentScreenDimensions();
            if (currentWidth != lastScreenWidth || currentHeight != lastScreenHeight)
                OnScreenDimensionsChanged();
        }

        // Panels

        public static UEPanel GetPanel(Panels panel) => UIPanels[panel];

        public static T GetPanel<T>(Panels panel) where T : UEPanel => (T)UIPanels[panel];

        public static void TogglePanel(Panels panel)
        {
            bool createdNow = false;
            if (!UIPanels.ContainsKey(panel))
            {
                createdNow = EnsurePanelCreated(panel);
                if (!createdNow)
                    return;
            }

            UEPanel uiPanel = GetPanel(panel);
            if (createdNow)
            {
                SetPanelActive(panel, true);
                return;
            }

            SetPanelActive(panel, !uiPanel.Enabled);
        }

        public static void SetPanelActive(Panels panelType, bool active)
        {
            GetPanel(panelType).SetActive(active);
        }

        public static void SetPanelActive(UEPanel panel, bool active)
        {
            panel.SetActive(active);
        }

        // navbar

        public static void SetNavBarAnchor()
        {
            switch (NavbarAnchor)
            {
                case VerticalAnchor.Top:
                    NavBarRect.anchorMin = new Vector2(0.5f, 1f);
                    NavBarRect.anchorMax = new Vector2(0.5f, 1f);
                    NavBarRect.anchoredPosition = new Vector2(NavBarRect.anchoredPosition.x, 0);
                    NavBarRect.sizeDelta = NAVBAR_DIMENSIONS;
                    break;

                case VerticalAnchor.Bottom:
                    NavBarRect.anchorMin = new Vector2(0.5f, 0f);
                    NavBarRect.anchorMax = new Vector2(0.5f, 0f);
                    NavBarRect.anchoredPosition = new Vector2(NavBarRect.anchoredPosition.x, 35);
                    NavBarRect.sizeDelta = NAVBAR_DIMENSIONS;
                    break;
            }
        }


        public static void RefreshLocalizedTexts()
        {
            try
            {
                foreach (UEPanel panel in UIPanels.Values)
                    panel.RefreshLocalizedText();
            }
            catch { }
        }

        // listeners

        private static void OnScreenDimensionsChanged()
        {
            (lastScreenWidth, lastScreenHeight) = GetCurrentScreenDimensions();

            foreach (KeyValuePair<Panels, UEPanel> panel in UIPanels)
            {
                panel.Value.EnsureValidSize();
                panel.Value.EnsureValidPosition();
                panel.Value.Dragger.OnEndResize();
            }
        }

        private static void OnCloseButtonClicked()
        {
            ShowMenu = false;
        }

        private static void Master_Toggle_OnValueChanged(KeyCode val)
        {
            if (closeBtn?.ButtonText != null)
                closeBtn.ButtonText.text = val.ToString();
        }



        // UI Construction

        private static void CreateTopNavBar()
        {
            GameObject navbarPanel = UIFactory.CreateUIObject("MainNavbar", UIRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navbarPanel, false, false, true, true, 5, 4, 4, 4, 4, TextAnchor.MiddleCenter);
            navbarPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
            NavBarRect = navbarPanel.GetComponent<RectTransform>();
            NavBarRect.pivot = new Vector2(0.5f, 1f);

            NavbarAnchor = ConfigManager.Main_Navbar_Anchor.Value;
            SetNavBarAnchor();
            ConfigManager.Main_Navbar_Anchor.OnValueChanged += (VerticalAnchor val) =>
            {
                NavbarAnchor = val;
                SetNavBarAnchor();
            };

            // UnityExplorer title

            string titleTxt = $"UE <i><color=grey>{ExplorerCore.VERSION}</color></i>";
            Text title = UIFactory.CreateLabel(navbarPanel, "Title", titleTxt, TextAnchor.MiddleCenter, default, true, 14);
            titleLabel = title;
            UIFactory.SetLayoutElement(title.gameObject, minWidth: 75, flexibleWidth: 0);

            // panel tabs

            NavbarTabButtonHolder = UIFactory.CreateUIObject("NavTabButtonHolder", navbarPanel);
            UIFactory.SetLayoutElement(NavbarTabButtonHolder, minHeight: 25, flexibleHeight: 999, flexibleWidth: 999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(NavbarTabButtonHolder, false, true, true, true, 4, 2, 2, 2, 2);

            // Time scale widget
            if (UseUnity6000Fallbacks)
                ExplorerCore.Log($"UI Stage: Navbar - Skip TimeScaleWidget on Unity {Application.unityVersion}");
            else
                TimeScaleWidget.SetUp(navbarPanel);

            //spacer
            GameObject spacer = UIFactory.CreateUIObject("Spacer", navbarPanel);
            UIFactory.SetLayoutElement(spacer, minWidth: 15);

            // Hide menu button

            closeBtn = UIFactory.CreateButton(navbarPanel, "CloseButton", ConfigManager.Master_Toggle.Value.ToString());
            UIFactory.SetLayoutElement(closeBtn.Component.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);
            RuntimeHelper.SetColorBlock(closeBtn.Component, new Color(0.63f, 0.32f, 0.31f),
                new Color(0.81f, 0.25f, 0.2f), new Color(0.6f, 0.18f, 0.16f));

            ConfigManager.Master_Toggle.OnValueChanged += Master_Toggle_OnValueChanged;
            if (ConfigManager.Menu_Language != null)
                ConfigManager.Menu_Language.OnValueChanged += _ => RefreshLocalizedTexts();
            closeBtn.OnClick += OnCloseButtonClicked;
            RefreshLocalizedTexts();
        }

        private static (int Width, int Height) GetCurrentScreenDimensions()
        {
            if (UseUnity6000Fallbacks)
                return (Screen.width, Screen.height);

            Display display = DisplayManager.ActiveDisplay;
            return (display.renderingWidth, display.renderingHeight);
        }

        private static void TryCreatePanel(Panels panelType, Func<UEPanel> factory)
        {
            try
            {
                ExplorerCore.Log($"UI Stage: Create {panelType} panel");
                UIPanels.Add(panelType, factory());
                ExplorerCore.Log($"UI Stage: Created {panelType} panel");
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Failed to create {panelType} panel: {ex}");
            }
        }

        internal static bool TryConsumeDeferredNavButton(Panels panelType, out ButtonRef button)
        {
            if (DeferredNavButtons.TryGetValue(panelType, out button))
            {
                DeferredNavButtons.Remove(panelType);
                return true;
            }

            button = null;
            return false;
        }

        private static void RegisterDeferredPanels()
        {
            DeferredPanelFactories.Clear();
            DeferredNavButtons.Clear();

            RegisterDeferredFactory(Panels.AutoCompleter, () => new AutoCompleteModal(UiBase));
            RegisterDeferredFactory(Panels.UIInspectorResults, () => new MouseInspectorResultsPanel(UiBase));

            RegisterDeferredPanel(Panels.ObjectExplorer, "Object Explorer", () => new ObjectExplorerPanel(UiBase));
            RegisterDeferredPanel(Panels.Inspector, "Inspector", () => new InspectorPanel(UiBase));
            RegisterDeferredPanel(Panels.CSConsole, "C# Console", () => new CSConsolePanel(UiBase));
            RegisterDeferredPanel(Panels.HookManager, "Hooks", () => new HookManagerPanel(UiBase));
            RegisterDeferredPanel(Panels.Freecam, "Freecam", () => new FreeCamPanel(UiBase));
            RegisterDeferredPanel(Panels.Clipboard, "Clipboard", () => new ClipboardPanel(UiBase));
            RegisterDeferredPanel(Panels.ConsoleLog, "Log", () => new LogPanel(UiBase));
            RegisterDeferredPanel(Panels.Options, "Options", () => new OptionsPanel(UiBase));
        }

        private static void RegisterDeferredFactory(Panels panelType, Func<UEPanel> factory)
        {
            DeferredPanelFactories[panelType] = factory;
        }

        private static void RegisterDeferredPanel(Panels panelType, string label, Func<UEPanel> factory)
        {
            DeferredPanelFactories[panelType] = factory;

            ButtonRef button = UIFactory.CreateButton(NavbarTabButtonHolder, $"DeferredButton_{panelType}", label);
            ConfigureNavButton(button, label);
            RuntimeHelper.SetColorBlock(button.Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
            button.OnClick += () =>
            {
                ExplorerCore.Log($"UI Stage: Deferred open request for {panelType}");
                TogglePanel(panelType);
            };

            DeferredNavButtons[panelType] = button;
        }

        internal static void ConfigureNavButton(ButtonRef button, string label)
        {
            if (button == null || button.Component == null)
                return;

            button.OnClick = null;

            GameObject navBtn = button.Component.gameObject;
            ContentSizeFitter navFitter = navBtn.GetComponent<ContentSizeFitter>();
            if (navFitter == null)
                navFitter = navBtn.AddComponent<ContentSizeFitter>();
            navFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(navBtn, false, true, true, true, 0, 0, 0, 5, 5, TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(navBtn, minWidth: 80);

            if (button.ButtonText != null)
            {
                button.ButtonText.text = label;

                ContentSizeFitter textFitter = button.ButtonText.gameObject.GetComponent<ContentSizeFitter>();
                if (textFitter == null)
                    textFitter = button.ButtonText.gameObject.AddComponent<ContentSizeFitter>();
                textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static bool EnsurePanelCreated(Panels panelType)
        {
            if (UIPanels.ContainsKey(panelType))
                return true;

            if (!DeferredPanelFactories.TryGetValue(panelType, out Func<UEPanel> factory))
                return false;

            try
            {
                if (PanelNeedsAutoCompleter(panelType))
                    EnsurePanelCreated(Panels.AutoCompleter);

                ExplorerCore.Log($"UI Stage: Deferred create {panelType} panel");
                UIPanels.Add(panelType, factory());
                DeferredPanelFactories.Remove(panelType);
                ExplorerCore.Log($"UI Stage: Deferred create {panelType} panel complete");

                if (panelType == Panels.CSConsole)
                    InitializeConsoleController();

                if (panelType == Panels.Inspector)
                {
                    EnsurePanelCreated(Panels.UIInspectorResults);
                    InitializeMouseInspector();
                }

                return true;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Deferred create for {panelType} failed: {ex}");
                return false;
            }
        }

        private static bool PanelNeedsAutoCompleter(Panels panelType)
        {
            return panelType == Panels.ObjectExplorer
                || panelType == Panels.Inspector
                || panelType == Panels.CSConsole
                || panelType == Panels.Options
                || panelType == Panels.HookManager;
        }

        private static void InitializeMouseInspector()
        {
            if (mouseInspectorInitialized)
                return;

            MouseInspector.inspectorUIBase = UniversalUI.RegisterUI(MouseInspector.UIBaseGUID, null);
            new MouseInspector(MouseInspector.inspectorUIBase);
            mouseInspectorInitialized = true;
            ExplorerCore.Log("UI Stage: MouseInspector initialized");
        }

        private static void InitializeConsoleController()
        {
            if (consoleControllerInitialized)
                return;

            ConsoleController.Init();
            consoleControllerInitialized = true;
            ExplorerCore.Log("UI Stage: ConsoleController initialized");
        }
    }
}
