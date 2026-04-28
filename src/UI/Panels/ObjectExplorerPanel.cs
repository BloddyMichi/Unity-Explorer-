using UnityExplorer.ObjectExplorer;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI.Panels
{
    public class ObjectExplorerPanel : UEPanel
    {
        public override string Name => "Object Explorer";
        public override UIManager.Panels PanelType => UIManager.Panels.ObjectExplorer;

        public override int MinWidth => 350;
        public override int MinHeight => 200;
        public override Vector2 DefaultAnchorMin => new(0.125f, 0.175f);
        public override Vector2 DefaultAnchorMax => new(0.325f, 0.925f);

        public SceneExplorer SceneExplorer;
        public ObjectSearch ObjectSearch;

        public override bool ShowByDefault => true;
        public override bool ShouldSaveActiveState => true;

        public int SelectedTab = 0;
        private readonly List<UIModel> tabPages = new();
        private readonly List<ButtonRef> tabButtons = new();

        private static bool Unity6000SafeObjectExplorerMode =>
            Application.unityVersion != null && Application.unityVersion.StartsWith("6000");

        public ObjectExplorerPanel(UIBase owner) : base(owner)
        {
        }

        public void SetTab(int tabIndex)
        {
            if (tabPages.Count == 0 || tabButtons.Count == 0)
                return;

            tabIndex = Math.Max(0, tabIndex);
            tabIndex = Math.Min(tabPages.Count - 1, tabIndex);

            if (SelectedTab >= 0 && SelectedTab < tabPages.Count)
                DisableTab(SelectedTab);

            UIModel content = tabPages[tabIndex];
            content.SetActive(true);

            ButtonRef button = tabButtons[tabIndex];
            RuntimeHelper.SetColorBlock(button.Component, UniversalUI.EnabledButtonColor, UniversalUI.EnabledButtonColor * 1.2f);

            SelectedTab = tabIndex;
            SaveInternalData();
        }

        private void DisableTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabPages.Count || tabIndex >= tabButtons.Count)
                return;

            tabPages[tabIndex].SetActive(false);
            RuntimeHelper.SetColorBlock(tabButtons[tabIndex].Component, UniversalUI.DisabledButtonColor, UniversalUI.DisabledButtonColor * 1.2f);
        }

        public override void Update()
        {
            if (tabPages.Count == 0 || SelectedTab < 0 || SelectedTab >= tabPages.Count)
                return;

            if (SceneExplorer != null && tabPages[SelectedTab] == SceneExplorer)
                SceneExplorer.Update();
            else if (ObjectSearch != null && tabPages[SelectedTab] == ObjectSearch)
                ObjectSearch.Update();
        }

        public override string ToSaveData()
        {
            return string.Join("|", new string[] { base.ToSaveData(), SelectedTab.ToString() });
        }

        protected override void ApplySaveData(string data)
        {
            base.ApplySaveData(data);

            try
            {
                int tab = int.Parse(data.Split('|').Last());
                SelectedTab = tab;
            }
            catch
            {
                SelectedTab = 0;
            }

            SelectedTab = Math.Max(0, SelectedTab);

            if (tabPages.Count > 0)
                SelectedTab = Math.Min(tabPages.Count - 1, SelectedTab);
            else
                SelectedTab = 0;

            SetTab(SelectedTab);
        }

        protected override void ConstructPanelContent()
        {
            // Tab bar
            GameObject tabGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "TabBar", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(tabGroup, minHeight: 25, flexibleHeight: 0);

            if (!Unity6000SafeObjectExplorerMode)
            {
                // Scene Explorer
                SceneExplorer = new SceneExplorer(this);
                SceneExplorer.ConstructUI(ContentRoot);
                tabPages.Add(SceneExplorer);
                AddTabButton(tabGroup, "Scene Explorer");
            }
            else
            {
                ExplorerCore.Log("ObjectExplorerPanel: Unity 6000 safe mode skips Scene Explorer to avoid SceneManager.sceneCount AccessViolation.");
            }

            // Object search
            ObjectSearch = new ObjectSearch(this);
            ObjectSearch.ConstructUI(ContentRoot);
            tabPages.Add(ObjectSearch);
            AddTabButton(tabGroup, Unity6000SafeObjectExplorerMode ? "Object Search (Safe)" : "Object Search");

            // default active state: Active
            this.SetActive(true);

            // In Unity 6000 safe mode only Object Search exists, so force the valid tab.
            if (Unity6000SafeObjectExplorerMode)
                SetTab(0);
        }

        private void AddTabButton(GameObject tabGroup, string label)
        {
            ButtonRef button = UIFactory.CreateButton(tabGroup, $"Button_{label}", label);

            int idx = tabButtons.Count;
            //button.onClick.AddListener(() => { SetTab(idx); });
            button.OnClick += () => { SetTab(idx); };

            tabButtons.Add(button);

            DisableTab(tabButtons.Count - 1);
        }
    }
}
