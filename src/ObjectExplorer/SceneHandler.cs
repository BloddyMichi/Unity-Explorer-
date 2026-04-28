using UnityEngine.SceneManagement;

#nullable enable

namespace UnityExplorer.ObjectExplorer;

public static class SceneHandler
{
    public static Scene? SelectedScene
    {
        get => selectedScene;
        internal set
        {
            if (!value.HasValue)
                return;

            if (selectedScene.HasValue && selectedScene.Value.IsValid() && value.Value.IsValid())
            {
                if (selectedScene.Value.buildIndex == value.Value.buildIndex &&
                    selectedScene.Value.name == value.Value.name)
                    return;
            }

            selectedScene = value;
            OnInspectedSceneChanged?.Invoke(selectedScene.Value);
        }
    }
    private static Scene? selectedScene;

    public static IEnumerable<GameObject> CurrentRootObjects { get; private set; } = new GameObject[0];

    public static List<Scene> LoadedScenes { get; private set; } = new();

    public static List<string> AllSceneNames { get; private set; } = new();

    public static event Action<Scene>? OnInspectedSceneChanged;
    public static event Action<List<Scene>>? OnLoadedScenesUpdated;

    internal static int DefaultSceneCount => 1;

    public static bool InspectingAssetScene => false;

    public static bool WasAbleToGetScenesInBuild { get; private set; }

    public static bool DontDestroyExists { get; private set; }

    internal static bool Unity6000SafeSceneMode =>
        Application.unityVersion != null && Application.unityVersion.StartsWith("6000");

    private const string dontDestroyName = "DontDestroyOnLoad";

    internal static void Init()
    {
        DontDestroyExists = false;
        WasAbleToGetScenesInBuild = false;
        AllSceneNames.Clear();

        if (Unity6000SafeSceneMode)
        {
            LoadedScenes.Clear();
            CurrentRootObjects = new GameObject[0];
            ExplorerCore.Log("SceneHandler.Init skipped on Unity 6000 safe mode to avoid SceneManager native access.");
            return;
        }

        TryInitDontDestroy();
        TryInitSceneList();
    }

    private static void TryInitDontDestroy()
    {
        // Unity 6.3 / 6000.3 changed Scene.handle from int to SceneHandle.
        // Older reflection and interop paths around DontDestroyOnLoad detection can
        // throw at runtime on precompiled assemblies. Disable this optional probe
        // instead of spamming warnings or breaking scene enumeration.
        DontDestroyExists = false;
    }

    private static void TryInitSceneList()
    {
        if (Unity6000SafeSceneMode)
        {
            WasAbleToGetScenesInBuild = false;
            AllSceneNames.Clear();
            return;
        }

        try
        {
            Type? sceneUtil = ReflectionUtility.GetTypeByName("UnityEngine.SceneManagement.SceneUtility");
            if (sceneUtil == null)
                throw new Exception("SceneUtility type not found or not unstripped.");

            MethodInfo? method = sceneUtil.GetMethod("GetScenePathByBuildIndex", ReflectionUtility.FLAGS);
            if (method == null)
                throw new Exception("SceneUtility.GetScenePathByBuildIndex not found.");

            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                try
                {
                    string? scenePath = (string?)method.Invoke(null, new object[] { i });
                    if (!string.IsNullOrEmpty(scenePath))
                        AllSceneNames.Add(scenePath);
                }
                catch (Exception ex)
                {
                    ExplorerCore.LogWarning($"Skipping build scene index {i}: {ex.Message}");
                }
            }

            WasAbleToGetScenesInBuild = true;
        }
        catch (Exception ex)
        {
            WasAbleToGetScenesInBuild = false;
            ExplorerCore.LogWarning($"Unable to generate list of all scenes in build: {ex.Message}");
        }
    }

    internal static bool SceneLooksUsable(Scene scene)
    {
        try
        {
            return scene.IsValid() && scene.isLoaded;
        }
        catch
        {
            return false;
        }
    }

    internal static string GetSceneKey(Scene scene)
    {
        try
        {
            string name = scene.name ?? string.Empty;
            int buildIndex = scene.buildIndex;
            return $"{buildIndex}|{name}";
        }
        catch
        {
            return "<invalid-scene>";
        }
    }

    internal static bool AreSameScene(Scene? a, Scene b)
    {
        if (!a.HasValue)
            return false;

        try
        {
            if (!a.Value.IsValid() || !b.IsValid())
                return false;

            return a.Value.buildIndex == b.buildIndex &&
                   string.Equals(a.Value.name, b.name, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    internal static bool AreSameScene(Scene a, Scene b)
    {
        try
        {
            if (!a.IsValid() || !b.IsValid())
                return false;

            return a.buildIndex == b.buildIndex &&
                   string.Equals(a.name, b.name, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    internal static void Update()
    {
        if (Unity6000SafeSceneMode)
        {
            LoadedScenes.Clear();
            CurrentRootObjects = new GameObject[0];
            return;
        }

        try
        {
            bool inspectedExists = false;

            LoadedScenes.Clear();

            string? selectedName = null;
            int selectedBuildIndex = int.MinValue;
            bool hasSelected = SelectedScene.HasValue && SceneLooksUsable(SelectedScene.Value);

            if (hasSelected)
            {
                selectedName = SelectedScene.Value.name;
                selectedBuildIndex = SelectedScene.Value.buildIndex;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!SceneLooksUsable(scene))
                    continue;

                if (!inspectedExists && hasSelected)
                {
                    if (scene.buildIndex == selectedBuildIndex && scene.name == selectedName)
                        inspectedExists = true;
                }

                LoadedScenes.Add(scene);
            }

            if (!inspectedExists && LoadedScenes.Count > 0)
                SelectedScene = LoadedScenes.First();

            OnLoadedScenesUpdated?.Invoke(LoadedScenes);

            if (SelectedScene.HasValue && SceneLooksUsable(SelectedScene.Value))
            {
                CurrentRootObjects = RuntimeHelper.GetRootGameObjects(SelectedScene.Value);
            }
            else
            {
                UnityEngine.Object[] allObjects = RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject));
                List<GameObject> objects = new();

                foreach (UnityEngine.Object obj in allObjects)
                {
                    GameObject? go = obj.TryCast<GameObject>();
                    if (go != null &&
                        go.transform != null &&
                        go.transform.parent == null &&
                        !go.scene.IsValid())
                    {
                        objects.Add(go);
                    }
                }

                CurrentRootObjects = objects;
            }
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"SceneHandler.Update failed: {ex.Message}");
            LoadedScenes.Clear();
            CurrentRootObjects = new GameObject[0];
        }
    }
}