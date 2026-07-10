using UnityExplorer.UI.Panels;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ButtonList;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.ObjectExplorer
{
    public class ObjectSearch : UIModel
    {
        public ObjectExplorerPanel Parent { get; }

        public ObjectSearch(ObjectExplorerPanel parent)
        {
            Parent = parent;
        }

        private SearchContext context = SearchContext.UnityObject;
        private SceneFilter sceneFilter = SceneFilter.Any;
        private ChildFilter childFilter = ChildFilter.Any;
        private ActiveFilter activeFilter = ActiveFilter.Any;
        private string desiredTypeInput;
        private string lastCheckedTypeInput;
        private bool lastTypeCanHaveGameObject;

        public ButtonListHandler<object, ButtonCell> dataHandler;
        private ScrollPool<ButtonCell> resultsScrollPool;
        private List<object> currentResults = new();
        private readonly List<object> recentObjects = new();
        private readonly List<object> favoriteObjects = new();
        private string currentResultMode = "search";

        //public TypeCompleter typeAutocompleter;
        public TypeCompleter unityObjectTypeCompleter;
        public TypeCompleter allTypesCompleter;

        public override GameObject UIRoot => uiRoot;
        private GameObject uiRoot;
        private GameObject sceneFilterRow;
        private GameObject childFilterRow;
        private GameObject classInputRow;
        private GameObject nameInputRow;
        private GameObject tagInputRow;
        private GameObject layerInputRow;
        private GameObject componentInputRow;
        private GameObject activeFilterRow;
        private InputFieldRef nameInputField;
        private InputFieldRef tagInputField;
        private InputFieldRef layerInputField;
        private InputFieldRef componentInputField;
        private Text resultsLabel;

        public List<object> GetEntries() => currentResults;

        public void DoSearch()
        {
            cachedCellTexts.Clear();

            if (context == SearchContext.Singleton)
            {
                currentResultMode = "singleton";
                currentResults = SearchProvider.InstanceSearch(desiredTypeInput).ToList();
            }
            else if (context == SearchContext.Class)
            {
                currentResultMode = "class";
                currentResults = SearchProvider.ClassSearch(desiredTypeInput);
            }
            else
            {
                currentResultMode = "search";
                currentResults = SearchProvider.UnityObjectSearch(
                    nameInputField.Text,
                    desiredTypeInput,
                    childFilter,
                    sceneFilter,
                    tagInputField.Text,
                    layerInputField.Text,
                    componentInputField.Text,
                    activeFilter);
            }

            RefreshResults();
        }

        public void Update()
        {
            if (context == SearchContext.UnityObject && lastCheckedTypeInput != desiredTypeInput)
            {
                lastCheckedTypeInput = desiredTypeInput;

                //var type = ReflectionUtility.GetTypeByName(desiredTypeInput);
                if (ReflectionUtility.GetTypeByName(desiredTypeInput) is Type cachedType)
                {
                    Type type = cachedType;
                    lastTypeCanHaveGameObject = typeof(Component).IsAssignableFrom(type) || type == typeof(GameObject);
                    sceneFilterRow.SetActive(lastTypeCanHaveGameObject);
                    childFilterRow.SetActive(lastTypeCanHaveGameObject);
                }
                else
                {
                    sceneFilterRow.SetActive(false);
                    childFilterRow.SetActive(false);
                    lastTypeCanHaveGameObject = false;
                }
            }
        }

        // UI Callbacks

        private void OnContextDropdownChanged(int value)
        {
            context = (SearchContext)value;

            lastCheckedTypeInput = null;
            sceneFilterRow.SetActive(false);
            childFilterRow.SetActive(false);

            if (nameInputRow != null)
                nameInputRow.SetActive(context == SearchContext.UnityObject);
            if (tagInputRow != null)
                tagInputRow.SetActive(context == SearchContext.UnityObject);
            if (layerInputRow != null)
                layerInputRow.SetActive(context == SearchContext.UnityObject);
            if (componentInputRow != null)
                componentInputRow.SetActive(context == SearchContext.UnityObject);
            if (activeFilterRow != null)
                activeFilterRow.SetActive(context == SearchContext.UnityObject);

            switch (context)
            {
                case SearchContext.UnityObject:
                    unityObjectTypeCompleter.Enabled = true;
                    allTypesCompleter.Enabled = false;
                    break;
                case SearchContext.Singleton:
                case SearchContext.Class:
                    allTypesCompleter.Enabled = true;
                    unityObjectTypeCompleter.Enabled = false;
                    break;
            }
        }

        private void OnSceneFilterDropChanged(int value) => sceneFilter = (SceneFilter)value;

        private void OnChildFilterDropChanged(int value) => childFilter = (ChildFilter)value;

        private void OnActiveFilterDropChanged(int value) => activeFilter = (ActiveFilter)value;

        private void OnTypeInputChanged(string val)
        {
            desiredTypeInput = val;

            if (string.IsNullOrEmpty(val))
            {
                sceneFilterRow.SetActive(false);
                childFilterRow.SetActive(false);
                lastCheckedTypeInput = val;
            }
        }

        // Cache the syntax-highlighted text for each search result to reduce allocs.
        private static readonly Dictionary<int, string> cachedCellTexts = new();

        public void SetCell(ButtonCell cell, int index)
        {
            if (index < 0 || index >= currentResults.Count)
            {
                cell.Disable();
                return;
            }

            if (!cachedCellTexts.ContainsKey(index))
            {
                string text;
                if (context == SearchContext.Class)
                {
                    Type type = currentResults[index] as Type;
                    text = $"{SignatureHighlighter.Parse(type, true)} <color=grey><i>({type.Assembly.GetName().Name})</i></color>";
                }
                else
                    text = ToStringUtility.ToStringWithType(currentResults[index], currentResults[index]?.GetActualType());

                cachedCellTexts.Add(index, text);
            }

            cell.Button.ButtonText.text = cachedCellTexts[index];
        }

        private void OnCellClicked(int dataIndex)
        {
            if (dataIndex < 0 || dataIndex >= currentResults.Count)
            {
                ExplorerCore.LogWarning("Object Search ignored stale result click at index " + dataIndex + ".");
                return;
            }

            AddRecent(currentResults[dataIndex]);

            if (context == SearchContext.Class)
                InspectorManager.Inspect(currentResults[dataIndex] as Type);
            else
                InspectorManager.Inspect(currentResults[dataIndex]);
        }

        private void ShowFavorites()
        {
            currentResultMode = "favorites";
            currentResults = favoriteObjects.Where(IsStillValid).ToList();
            RefreshResults();
        }

        private void ShowRecent()
        {
            currentResultMode = "recent";
            currentResults = recentObjects.Where(IsStillValid).ToList();
            RefreshResults();
        }

        private void ClearFavorites()
        {
            favoriteObjects.Clear();
            if (currentResultMode == "favorites")
                ShowFavorites();
        }

        private void AddActiveInspectorToFavorites()
        {
            object target = InspectorManager.ActiveInspector?.Target;
            if (!IsStillValid(target))
            {
                ExplorerCore.LogWarning("No active inspector target is available to favorite.");
                return;
            }

            AddUnique(favoriteObjects, target, 50);
            ExplorerCore.Log("Added favorite Object Search target: " + ToStringUtility.ToStringWithType(target, target.GetActualType()));
            ShowFavorites();
        }

        private void AddRecent(object target)
        {
            AddUnique(recentObjects, target, 25);
        }

        private static void AddUnique(List<object> list, object target, int maxCount)
        {
            if (!IsStillValid(target))
                return;

            list.RemoveAll(item => item.ReferenceEqual(target));
            list.Insert(0, target);

            while (list.Count > maxCount)
                list.RemoveAt(list.Count - 1);
        }

        private static bool IsStillValid(object target)
        {
            return target != null && !target.IsNullOrDestroyed();
        }

        private void RefreshResults()
        {
            cachedCellTexts.Clear();
            dataHandler.RefreshData();
            resultsScrollPool.Refresh(true);
            resultsLabel.text = $"{currentResults.Count} {currentResultMode} result(s)  |  recent {recentObjects.Count}  |  favorites {favoriteObjects.Count}";
        }

        private bool ShouldDisplayCell(object arg1, string arg2) => true;

        public override void ConstructUI(GameObject parent)
        {
            uiRoot = UIFactory.CreateVerticalGroup(parent, "ObjectSearch", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(uiRoot, flexibleHeight: 9999);

            // Search context row

            GameObject contextGroup = UIFactory.CreateHorizontalGroup(uiRoot, "SearchContextRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(contextGroup, minHeight: 25, flexibleHeight: 0);

            Text contextLbl = UIFactory.CreateLabel(contextGroup, "SearchContextLabel", "Searching for:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(contextLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            GameObject contextDropObj = UIFactory.CreateDropdown(contextGroup, "ContextDropdown", out Dropdown contextDrop, null, 14, OnContextDropdownChanged);
            foreach (string name in Enum.GetNames(typeof(SearchContext)))
                contextDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(contextDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Class input

            classInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "ClassRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(classInputRow, minHeight: 25, flexibleHeight: 0);

            Text unityClassLbl = UIFactory.CreateLabel(classInputRow, "ClassLabel", "Class filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(unityClassLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            InputFieldRef classInputField = UIFactory.CreateInputField(classInputRow, "ClassInput", "...");
            UIFactory.SetLayoutElement(classInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            unityObjectTypeCompleter = new(typeof(UnityEngine.Object), classInputField, true, false, true);
            allTypesCompleter = new(null, classInputField, true, false, true);
            allTypesCompleter.Enabled = false;
            classInputField.OnValueChanged += OnTypeInputChanged;

            //unityObjectClassRow.SetActive(false);

            // Child filter row

            childFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "ChildFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(childFilterRow, minHeight: 25, flexibleHeight: 0);

            Text childLbl = UIFactory.CreateLabel(childFilterRow, "ChildLabel", "Child filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(childLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            GameObject childDropObj = UIFactory.CreateDropdown(childFilterRow, "ChildFilterDropdown", out Dropdown childDrop, null, 14, OnChildFilterDropChanged);
            foreach (string name in Enum.GetNames(typeof(ChildFilter)))
                childDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(childDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            childFilterRow.SetActive(false);

            // Scene filter row

            sceneFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "SceneFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(sceneFilterRow, minHeight: 25, flexibleHeight: 0);

            Text sceneLbl = UIFactory.CreateLabel(sceneFilterRow, "SceneLabel", "Scene filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(sceneLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            GameObject sceneDropObj = UIFactory.CreateDropdown(sceneFilterRow, "SceneFilterDropdown", out Dropdown sceneDrop, null, 14, OnSceneFilterDropChanged);
            foreach (string name in Enum.GetNames(typeof(SceneFilter)))
            {
                if (!SceneHandler.DontDestroyExists && name == "DontDestroyOnLoad")
                    continue;
                sceneDrop.options.Add(new Dropdown.OptionData(name));
            }
            UIFactory.SetLayoutElement(sceneDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            sceneFilterRow.SetActive(false);

            // Name filter input

            nameInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "NameRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(nameInputRow, minHeight: 25, flexibleHeight: 0);

            Text nameLbl = UIFactory.CreateLabel(nameInputRow, "NameFilterLabel", "Name contains:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(nameLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            nameInputField = UIFactory.CreateInputField(nameInputRow, "NameFilterInput", "...");
            UIFactory.SetLayoutElement(nameInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Tag filter input

            tagInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "TagRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(tagInputRow, minHeight: 25, flexibleHeight: 0);

            Text tagLbl = UIFactory.CreateLabel(tagInputRow, "TagFilterLabel", "Tag contains:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(tagLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            tagInputField = UIFactory.CreateInputField(tagInputRow, "TagFilterInput", "...");
            UIFactory.SetLayoutElement(tagInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Layer filter input

            layerInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "LayerRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(layerInputRow, minHeight: 25, flexibleHeight: 0);

            Text layerLbl = UIFactory.CreateLabel(layerInputRow, "LayerFilterLabel", "Layer contains:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(layerLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            layerInputField = UIFactory.CreateInputField(layerInputRow, "LayerFilterInput", "number or name");
            UIFactory.SetLayoutElement(layerInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Component filter input

            componentInputRow = UIFactory.CreateHorizontalGroup(uiRoot, "ComponentRow", true, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(componentInputRow, minHeight: 25, flexibleHeight: 0);

            Text componentLbl = UIFactory.CreateLabel(componentInputRow, "ComponentFilterLabel", "Has component:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(componentLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            componentInputField = UIFactory.CreateInputField(componentInputRow, "ComponentFilterInput", "...");
            UIFactory.SetLayoutElement(componentInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);
            _ = new TypeCompleter(typeof(Component), componentInputField, true, false, true);

            // Active filter row

            activeFilterRow = UIFactory.CreateHorizontalGroup(uiRoot, "ActiveFilterRow", false, true, true, true, 2, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(activeFilterRow, minHeight: 25, flexibleHeight: 0);

            Text activeLbl = UIFactory.CreateLabel(activeFilterRow, "ActiveLabel", "Active filter:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(activeLbl.gameObject, minWidth: 110, flexibleWidth: 0);

            GameObject activeDropObj = UIFactory.CreateDropdown(activeFilterRow, "ActiveFilterDropdown", out Dropdown activeDrop, null, 14, OnActiveFilterDropChanged);
            foreach (string name in Enum.GetNames(typeof(ActiveFilter)))
                activeDrop.options.Add(new Dropdown.OptionData(name));
            UIFactory.SetLayoutElement(activeDropObj, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            // Search button

            GameObject actionRow = UIFactory.CreateHorizontalGroup(uiRoot, "SearchActionRow", false, false, true, true, 4, new Vector4(2, 2, 2, 2));
            UIFactory.SetLayoutElement(actionRow, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            ButtonRef searchButton = UIFactory.CreateButton(actionRow, "SearchButton", "Search");
            UIFactory.SetLayoutElement(searchButton.Component.gameObject, minHeight: 25, minWidth: 90, flexibleHeight: 0);
            searchButton.OnClick += DoSearch;

            ButtonRef recentButton = UIFactory.CreateButton(actionRow, "RecentButton", "Recent");
            UIFactory.SetLayoutElement(recentButton.Component.gameObject, minHeight: 25, minWidth: 90, flexibleHeight: 0);
            recentButton.OnClick += ShowRecent;

            ButtonRef favoritesButton = UIFactory.CreateButton(actionRow, "FavoritesButton", "Favorites");
            UIFactory.SetLayoutElement(favoritesButton.Component.gameObject, minHeight: 25, minWidth: 100, flexibleHeight: 0);
            favoritesButton.OnClick += ShowFavorites;

            ButtonRef favoriteActiveButton = UIFactory.CreateButton(actionRow, "FavoriteActiveButton", "Favorite Active");
            UIFactory.SetLayoutElement(favoriteActiveButton.Component.gameObject, minHeight: 25, minWidth: 130, flexibleHeight: 0);
            favoriteActiveButton.OnClick += AddActiveInspectorToFavorites;

            ButtonRef clearFavoritesButton = UIFactory.CreateButton(actionRow, "ClearFavoritesButton", "Clear");
            UIFactory.SetLayoutElement(clearFavoritesButton.Component.gameObject, minHeight: 25, minWidth: 80, flexibleHeight: 0);
            clearFavoritesButton.OnClick += ClearFavorites;

            // Results count label

            GameObject resultsCountRow = UIFactory.CreateHorizontalGroup(uiRoot, "ResultsCountRow", true, true, true, true);
            UIFactory.SetLayoutElement(resultsCountRow, minHeight: 25, flexibleHeight: 0);

            resultsLabel = UIFactory.CreateLabel(resultsCountRow, "ResultsLabel", "0 results", TextAnchor.MiddleCenter);

            // RESULTS SCROLL POOL

            dataHandler = new ButtonListHandler<object, ButtonCell>(resultsScrollPool, GetEntries, SetCell, ShouldDisplayCell, OnCellClicked);
            resultsScrollPool = UIFactory.CreateScrollPool<ButtonCell>(uiRoot, "ResultsList", out GameObject scrollObj,
                out GameObject scrollContent);

            resultsScrollPool.Initialize(dataHandler);
            UIFactory.SetLayoutElement(scrollObj, flexibleHeight: 9999);
        }
    }
}
