using System.Collections;
using UnityEngine.SceneManagement;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets;

namespace UnityExplorer.ObjectExplorer
{
    public class SceneExplorer : UIModel
    {
        public ObjectExplorerPanel Parent { get; }

        private static bool Unity6000SafeSceneMode =>
            Application.unityVersion != null && Application.unityVersion.StartsWith("6000");

        public SceneExplorer(ObjectExplorerPanel parent)
        {
            Parent = parent;

            if (!Unity6000SafeSceneMode)
            {
                SceneHandler.OnInspectedSceneChanged += SceneHandler_OnInspectedSceneChanged;
                SceneHandler.OnLoadedScenesUpdated += SceneHandler_OnLoadedScenesUpdated;
            }
        }

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;

        public bool AutoUpdate = false;

        public TransformTree Tree;
        private float timeOfLastUpdate = -1f;

        private GameObject refreshRow;
        private Dropdown sceneDropdown;
        private readonly Dictionary<string, Dropdown.OptionData> sceneToDropdownOption = new();

        private Dropdown allSceneDropdown;
        private ButtonRef loadButton;
        private ButtonRef loadAdditiveButton;
        private Toggle refreshToggle;
        private int lastAllSceneDropdownValue = -1;

        private IEnumerable<GameObject> GetRootEntries() => SceneHandler.CurrentRootObjects;

        public void Update()
        {
            if (Unity6000SafeSceneMode)
                return;

            if (refreshToggle != null)
                AutoUpdate = refreshToggle.isOn;

            if (allSceneDropdown != null && allSceneDropdown.value != lastAllSceneDropdownValue)
            {
                lastAllSceneDropdownValue = allSceneDropdown.value;
                RefreshSceneLoaderButtons();
            }

            if ((AutoUpdate || !SceneHandler.InspectingAssetScene) && timeOfLastUpdate.OccuredEarlierThan(1))
            {
                timeOfLastUpdate = Time.realtimeSinceStartup;
                UpdateTree();
            }
        }

        public void UpdateTree()
        {
            if (Unity6000SafeSceneMode)
                return;

            SceneHandler.Update();

            if (Tree != null)
                Tree.RefreshData(true, false, false, false);
        }

        public void JumpToTransform(Transform transform)
        {
            if (Unity6000SafeSceneMode)
            {
                UIManager.SetPanelActive(this.Parent, true);
                ExplorerCore.Log("SceneExplorer.JumpToTransform skipped on Unity 6000 safe mode.");
                return;
            }

            if (!transform)
                return;

            UIManager.SetPanelActive(this.Parent, true);
            this.Parent.SetTab(0);

            GameObject go = transform.gameObject;
            if (!SceneHandler.AreSameScene(SceneHandler.SelectedScene, go.scene))
            {
                int idx = 0;

                string sceneKey = SceneHandler.GetSceneKey(go.scene);

                if (sceneToDropdownOption.TryGetValue(sceneKey, out Dropdown.OptionData option))
                {
                    idx = sceneDropdown.options.IndexOf(option);
                    if (idx < 0)
                        idx = 0;
                }

                sceneDropdown.value = idx;
            }

            Tree.JumpAndExpandToTransform(transform);
        }

        private void OnSceneSelectionDropdownChanged(int value)
        {
            if (value < 0 || SceneHandler.LoadedScenes.Count <= value)
                return;

            SceneHandler.SelectedScene = SceneHandler.LoadedScenes[value];
            SceneHandler.Update();

            if (Tree != null)
                Tree.RefreshData(true, true, true, false);

            OnSelectedSceneChanged(SceneHandler.SelectedScene.Value);
        }

        private void SceneHandler_OnInspectedSceneChanged(Scene scene)
        {
            string sceneKey = SceneHandler.GetSceneKey(scene);

            if (!sceneToDropdownOption.ContainsKey(sceneKey))
                PopulateSceneDropdown(SceneHandler.LoadedScenes);

            if (sceneToDropdownOption.TryGetValue(sceneKey, out Dropdown.OptionData opt))
            {
                int idx = sceneDropdown.options.IndexOf(opt);
                if (idx >= 0)
                {
                    if (sceneDropdown.value != idx)
                        sceneDropdown.value = idx;
                    else
                        sceneDropdown.captionText.text = opt.text;
                }
            }

            OnSelectedSceneChanged(scene);
        }

        private void OnSelectedSceneChanged(Scene scene)
        {
            if (refreshRow)
                refreshRow.SetActive(!scene.IsValid());
        }

        private void SceneHandler_OnLoadedScenesUpdated(List<Scene> loadedScenes)
        {
            PopulateSceneDropdown(loadedScenes);
        }

        private void PopulateSceneDropdown(List<Scene> loadedScenes)
        {
            sceneToDropdownOption.Clear();
            sceneDropdown.options.Clear();

            foreach (Scene scene in loadedScenes)
            {
                string sceneKey = SceneHandler.GetSceneKey(scene);
                if (sceneToDropdownOption.ContainsKey(sceneKey))
                    continue;

                string name = scene.name?.Trim();

                if (!scene.IsValid())
                    name = "<invalid>";
                else if (string.IsNullOrEmpty(name))
                    name = "<untitled>";

                Dropdown.OptionData option = new(name);
                sceneDropdown.options.Add(option);
                sceneToDropdownOption.Add(sceneKey, option);
            }

            if (sceneDropdown.options.Count == 0)
                sceneDropdown.options.Add(new Dropdown.OptionData("<no scenes>"));

            sceneDropdown.RefreshShownValue();
        }

        private void OnFilterInput(string input)
        {
            if ((!string.IsNullOrEmpty(input) && !Tree.Filtering) || (string.IsNullOrEmpty(input) && Tree.Filtering))
                Tree.Clear();

            Tree.CurrentFilter = input;
            Tree.RefreshData(true, false, true, false);
        }

        private void TryLoadScene(LoadSceneMode mode, Dropdown allSceneDrop)
        {
            string text = allSceneDrop.captionText.text;

            if (text == DEFAULT_LOAD_TEXT)
                return;

            try
            {
                SceneManager.LoadScene(text, mode);
                allSceneDrop.value = 0;
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Unable to load the Scene! {ex.ReflectionExToString()}");
            }
        }

        public override void ConstructUI(GameObject content)
        {
            uiRoot = UIFactory.CreateUIObject("SceneExplorer", content);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(uiRoot, true, true, true, true, 0, 2, 2, 2, 2);
            UIFactory.SetLayoutElement(uiRoot, flexibleHeight: 9999);

            if (Unity6000SafeSceneMode)
            {
                Text disabledLabel = UIFactory.CreateLabel(uiRoot, "SceneExplorerDisabledLabel",
                    "Scene Explorer disabled for Unity 6000 safe mode. Use Object Search instead.",
                    TextAnchor.MiddleCenter, Color.yellow, true, 14);
                UIFactory.SetLayoutElement(disabledLabel.gameObject, minHeight: 80, flexibleHeight: 9999, flexibleWidth: 9999);
                return;
            }

            GameObject toolbar = UIFactory.CreateVerticalGroup(uiRoot, "Toolbar", true, true, true, true, 2, new Vector4(2, 2, 2, 2),
               new Color(0.15f, 0.15f, 0.15f));

            GameObject dropRow = UIFactory.CreateHorizontalGroup(toolbar, "DropdownRow", true, true, true, true, 5, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(dropRow, minHeight: 25, flexibleWidth: 9999);

            Text dropLabel = UIFactory.CreateLabel(dropRow, "SelectorLabel", "Scene:", TextAnchor.MiddleLeft, Color.cyan, false, 15);
            UIFactory.SetLayoutElement(dropLabel.gameObject, minHeight: 25, minWidth: 60, flexibleWidth: 0);

            GameObject dropdownObj = UIFactory.CreateDropdown(dropRow, "SceneDropdown", out sceneDropdown, "<notset>", 13, OnSceneSelectionDropdownChanged);
            UIFactory.SetLayoutElement(dropdownObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            SceneHandler.Update();
            PopulateSceneDropdown(SceneHandler.LoadedScenes);
            sceneDropdown.captionText.text = sceneDropdown.options.Count > 0 ? sceneDropdown.options[0].text : "<no scenes>";

            GameObject filterRow = UIFactory.CreateHorizontalGroup(toolbar, "FilterGroup", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(filterRow, minHeight: 25, flexibleHeight: 0);

            InputFieldRef inputField = UIFactory.CreateInputField(filterRow, "FilterInput", "Search...");
            inputField.Component.targetGraphic.color = new Color(0.2f, 0.2f, 0.2f);
            RuntimeHelper.SetColorBlock(inputField.Component, new Color(0.4f, 0.4f, 0.4f), new Color(0.2f, 0.2f, 0.2f),
                new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25);
            inputField.OnValueChanged += OnFilterInput;

            refreshRow = UIFactory.CreateHorizontalGroup(toolbar, "RefreshGroup", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(refreshRow, minHeight: 30, flexibleHeight: 0);

            ButtonRef refreshButton = UIFactory.CreateButton(refreshRow, "RefreshButton", "Update");
            UIFactory.SetLayoutElement(refreshButton.Component.gameObject, minWidth: 65, flexibleWidth: 0);
            refreshButton.OnClick += UpdateTree;

            GameObject refreshToggle = UIFactory.CreateToggle(refreshRow, "RefreshToggle", out Toggle toggle, out Text text);
            UIFactory.SetLayoutElement(refreshToggle, flexibleWidth: 9999);
            text.text = "Auto-update (1 second)";
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.fontSize = 12;
            toggle.isOn = false;
            this.refreshToggle = toggle;

            refreshRow.SetActive(false);

            GameObject labelsRow = UIFactory.CreateHorizontalGroup(toolbar, "LabelsRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(labelsRow, minHeight: 30, flexibleHeight: 0);

            Text nameLabel = UIFactory.CreateLabel(labelsRow, "NameLabel", "Name", TextAnchor.MiddleLeft, color: Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, flexibleWidth: 9999, minHeight: 25);

            Text indexLabel = UIFactory.CreateLabel(labelsRow, "IndexLabel", "Sibling Index", TextAnchor.MiddleLeft, fontSize: 12, color: Color.grey);
            UIFactory.SetLayoutElement(indexLabel.gameObject, minWidth: 100, flexibleWidth: 0, minHeight: 25);

            UniverseLib.UI.Widgets.ScrollView.ScrollPool<TransformCell> scrollPool = UIFactory.CreateScrollPool<TransformCell>(uiRoot, "TransformTree", out GameObject scrollObj,
                out GameObject scrollContent, new Color(0.11f, 0.11f, 0.11f));
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
            UIFactory.SetLayoutElement(scrollContent, flexibleHeight: 9999);

            Tree = new TransformTree(scrollPool, GetRootEntries, OnCellClicked);
            Tree.RefreshData(true, true, true, false);

            ConstructSceneLoader();

            RuntimeHelper.StartCoroutine(TempFixCoro());
        }

        void OnCellClicked(GameObject obj) => InspectorManager.Inspect(obj);

        private IEnumerator TempFixCoro()
        {
            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < 2.5f)
                yield return null;

            if (sceneDropdown == null || sceneDropdown.options == null || sceneDropdown.options.Count == 0)
                yield break;

            sceneDropdown.value = 0;
        }

        private const string DEFAULT_LOAD_TEXT = "[Select a scene]";

        private void RefreshSceneLoaderOptions(string filter)
        {
            allSceneDropdown.options.Clear();
            allSceneDropdown.options.Add(new Dropdown.OptionData(DEFAULT_LOAD_TEXT));

            foreach (string scene in SceneHandler.AllSceneNames)
            {
                if (string.IsNullOrEmpty(filter) || scene.ContainsIgnoreCase(filter))
                    allSceneDropdown.options.Add(new Dropdown.OptionData(Path.GetFileNameWithoutExtension(scene)));
            }

            allSceneDropdown.RefreshShownValue();

            if (loadButton != null)
                RefreshSceneLoaderButtons();
        }

        private void RefreshSceneLoaderButtons()
        {
            string text = allSceneDropdown.captionText.text;
            if (text == DEFAULT_LOAD_TEXT)
            {
                loadButton.Component.interactable = false;
                loadAdditiveButton.Component.interactable = false;
            }
            else
            {
                loadButton.Component.interactable = true;
                loadAdditiveButton.Component.interactable = true;
            }
        }

        private void ConstructSceneLoader()
        {
            try
            {
                if (SceneHandler.WasAbleToGetScenesInBuild)
                {
                    GameObject sceneLoaderObj = UIFactory.CreateVerticalGroup(uiRoot, "SceneLoader", true, true, true, true);
                    UIFactory.SetLayoutElement(sceneLoaderObj, minHeight: 25);

                    Text loaderTitle = UIFactory.CreateLabel(sceneLoaderObj, "SceneLoaderLabel", LocalizationManager.GetText("scene_loader", "Scene Loader"), TextAnchor.MiddleLeft, Color.white, true, 14);
                    UIFactory.SetLayoutElement(loaderTitle.gameObject, minHeight: 25, flexibleHeight: 0);

                    InputFieldRef searchFilterObj = UIFactory.CreateInputField(sceneLoaderObj, "SearchFilterInput", "Filter scene names...");
                    UIFactory.SetLayoutElement(searchFilterObj.UIRoot, minHeight: 25, flexibleHeight: 0);
                    searchFilterObj.OnValueChanged += RefreshSceneLoaderOptions;

                    GameObject allSceneDropObj = UIFactory.CreateDropdown(sceneLoaderObj, "SceneLoaderDropdown", out allSceneDropdown, "", 14, null);
                    UIFactory.SetLayoutElement(allSceneDropObj, minHeight: 25, minWidth: 150, flexibleWidth: 0, flexibleHeight: 0);

                    RefreshSceneLoaderOptions(string.Empty);

                    GameObject buttonRow = UIFactory.CreateHorizontalGroup(sceneLoaderObj, "LoadButtons", true, true, true, true, 4);

                    loadButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Single)", new Color(0.1f, 0.3f, 0.3f));
                    UIFactory.SetLayoutElement(loadButton.Component.gameObject, minHeight: 25, minWidth: 150);
                    loadButton.OnClick += () => { TryLoadScene(LoadSceneMode.Single, allSceneDropdown); };

                    loadAdditiveButton = UIFactory.CreateButton(buttonRow, "LoadSceneButton", "Load (Additive)", new Color(0.1f, 0.3f, 0.3f));
                    UIFactory.SetLayoutElement(loadAdditiveButton.Component.gameObject, minHeight: 25, minWidth: 150);
                    loadAdditiveButton.OnClick += () => { TryLoadScene(LoadSceneMode.Additive, allSceneDropdown); };

                    Color disabledColor = new(0.24f, 0.24f, 0.24f);
                    RuntimeHelper.SetColorBlock(loadButton.Component, disabled: disabledColor);
                    RuntimeHelper.SetColorBlock(loadAdditiveButton.Component, disabled: disabledColor);

                    loadButton.Component.interactable = false;
                    loadAdditiveButton.Component.interactable = false;
                    lastAllSceneDropdownValue = allSceneDropdown.value;
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Could not create the Scene Loader helper! {ex.ReflectionExToString()}");
            }
        }
    }
}
