using UnityEngine.SceneManagement;

namespace UnityExplorer.ObjectExplorer;

public enum SearchContext
{
    UnityObject,
    Singleton,
    Class
}

public enum ChildFilter
{
    Any,
    RootObject,
    HasParent
}

public enum SceneFilter
{
    Any,
    ActivelyLoaded,
    DontDestroyOnLoad,
    HideAndDontSave,
}

public enum ActiveFilter
{
    Any,
    ActiveInHierarchy,
    InactiveInHierarchy
}

public static class SearchProvider
{
    private static bool Filter(Scene scene, SceneFilter filter)
    {
        return filter switch
        {
            SceneFilter.Any => true,
            SceneFilter.DontDestroyOnLoad => false,
            SceneFilter.HideAndDontSave => !scene.IsValid(),
            SceneFilter.ActivelyLoaded => scene.IsValid() && scene.buildIndex != -1,
            _ => false,
        };
    }

    internal static List<object> UnityObjectSearch(
        string input,
        string customTypeInput,
        ChildFilter childFilter,
        SceneFilter sceneFilter,
        string tagInput = null,
        string layerInput = null,
        string componentTypeInput = null,
        ActiveFilter activeFilter = ActiveFilter.Any)
    {
        List<object> results = new();

        Type searchType = null;
        if (!string.IsNullOrEmpty(customTypeInput))
        {
            if (ReflectionUtility.GetTypeByName(customTypeInput) is Type customType)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(customType))
                    searchType = customType;
                else
                    ExplorerCore.LogWarning($"Custom type '{customType.FullName}' is not assignable from UnityEngine.Object!");
            }
            else
                ExplorerCore.LogWarning($"Could not find any type by name '{customTypeInput}'!");
        }

        if (searchType == null)
            searchType = typeof(UnityEngine.Object);

        UnityEngine.Object[] allObjects = RuntimeHelper.FindObjectsOfTypeAll(searchType);

        string nameFilter = null;
        if (!string.IsNullOrEmpty(input))
            nameFilter = input;

        bool shouldFilterGOs = searchType == typeof(GameObject) || typeof(Component).IsAssignableFrom(searchType);
        Type componentFilterType = ResolveComponentFilter(componentTypeInput);

        foreach (UnityEngine.Object obj in allObjects)
        {
            if (!string.IsNullOrEmpty(nameFilter) && !obj.name.ContainsIgnoreCase(nameFilter))
                continue;

            GameObject go = null;
            Type type = obj.GetActualType();

            if (type == typeof(GameObject))
                go = obj.TryCast<GameObject>();
            else if (typeof(Component).IsAssignableFrom(type) && obj is Component comp)
                go = comp.gameObject;

            if (go)
            {
                if (go.transform.root.name == "UniverseLibCanvas")
                    continue;

                if (!MatchesGameObjectFilters(go, tagInput, layerInput, componentFilterType, activeFilter))
                    continue;

                if (shouldFilterGOs)
                {
                    if (sceneFilter != SceneFilter.Any)
                    {
                        if (!Filter(go.scene, sceneFilter))
                            continue;
                    }

                    if (childFilter != ChildFilter.Any)
                    {
                        if (!go)
                            continue;

                        if (childFilter == ChildFilter.HasParent && !go.transform.parent)
                            continue;
                        else if (childFilter == ChildFilter.RootObject && go.transform.parent)
                            continue;
                    }
                }
            }

            results.Add(obj);
        }

        return results;
    }

    private static Type ResolveComponentFilter(string componentTypeInput)
    {
        if (string.IsNullOrWhiteSpace(componentTypeInput))
            return null;

        Type componentType = ReflectionUtility.GetTypeByName(componentTypeInput);
        if (componentType == null)
        {
            ExplorerCore.LogWarning($"Could not find component type by name '{componentTypeInput}'!");
            return null;
        }

        if (!typeof(Component).IsAssignableFrom(componentType))
        {
            ExplorerCore.LogWarning($"Component filter '{componentType.FullName}' is not assignable from UnityEngine.Component!");
            return null;
        }

        return componentType;
    }

    private static bool MatchesGameObjectFilters(
        GameObject go,
        string tagInput,
        string layerInput,
        Type componentFilterType,
        ActiveFilter activeFilter)
    {
        if (!string.IsNullOrWhiteSpace(tagInput))
        {
            string tag = string.Empty;
            try { tag = go.tag ?? string.Empty; }
            catch { tag = string.Empty; }

            if (!tag.ContainsIgnoreCase(tagInput))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(layerInput))
        {
            string layerNumber = go.layer.ToString();
            string layerName = string.Empty;

            if (!ExplorerCore.IsUnity6000OrNewer)
            {
                try { layerName = LayerMask.LayerToName(go.layer) ?? string.Empty; }
                catch { layerName = string.Empty; }
            }

            if (!layerNumber.ContainsIgnoreCase(layerInput) && !layerName.ContainsIgnoreCase(layerInput))
                return false;
        }

        if (activeFilter == ActiveFilter.ActiveInHierarchy && !go.activeInHierarchy)
            return false;

        if (activeFilter == ActiveFilter.InactiveInHierarchy && go.activeInHierarchy)
            return false;

        if (componentFilterType != null)
        {
            try
            {
#if CPP
#if INTEROP
                if (!go.GetComponent(Il2CppInterop.Runtime.Il2CppType.From(componentFilterType)))
                    return false;
#else
                if (!go.GetComponent(UnhollowerRuntimeLib.Il2CppType.From(componentFilterType)))
                    return false;
#endif
#else
                if (!go.GetComponent(componentFilterType))
                    return false;
#endif
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning("Component filter failed for '" + go.name + "': " + ex.Message);
                return false;
            }
        }

        return true;
    }

    internal static List<object> ClassSearch(string input)
    {
        List<object> list = new();

        if (string.IsNullOrEmpty(input))
            return list;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in asm.TryGetTypes())
            {
                try
                {
                    if (!type.FullName.ContainsIgnoreCase(input))
                        continue;
                    list.Add(type);
                }
                catch (Exception e)
                {
                    ExplorerCore.LogError($"Error while searching for singletons in {type.FullName}!  {e.Message}");
                }
            }
        }

        return list;
    }

    internal static string[] instanceNames = [
        "m_instance",
        "m_Instance",
        "s_instance",
        "s_Instance",
        "_instance",
        "_Instance",
        "instance",
        "Instance",
        "<Instance>k__BackingField",
        "<instance>k__BackingField",
    ];

    internal static List<object> InstanceSearch(string input)
    {
        List<object> instances = new();

        if (string.IsNullOrEmpty(input))
            return instances;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in asm.TryGetTypes().Where(it => !(it.IsSealed && it.IsAbstract) && !it.IsEnum))
            {
                try
                {
                    if (!type.FullName.ContainsIgnoreCase(input))
                        continue;

                    ReflectionUtility.FindSingleton(instanceNames, type, flags, instances);
                }
                catch (Exception e)
                {
                    ExplorerCore.LogError($"Error while searching for singletons in {type.FullName}!  {e.Message}");
                }
            }
        }

        return instances.Distinct().ToList();
    }
}
