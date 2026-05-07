using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace UnityExplorer.Inspectors.MouseInspectors;

public class WorldInspector : MouseInspectorBase
{
    private static Camera? MainCamera;
    private static GameObject? lastHitObject;

    public override void OnBeginMouseInspect()
    {
        if (!EnsureMainCamera())
        {
            ExplorerCore.LogWarning("No valid cameras found! Cannot inspect world!");
        }
    }


    public override void ClearHitData()
    {
        lastHitObject = null;
    }

    public override void OnSelectMouseInspect()
    {
        GameObject? selected = lastHitObject;

        // Important for Data Center:
        // The game highlights/uses objects through a center-screen "look at" ray,
        // while UnityExplorer's mouse position can still be over the UI/last button.
        // If no object was tracked before the click, do one final center-aim lookup.
        if (selected == null)
        {
            Camera? camera = EnsureMainCamera();
            if (camera != null && TryFindObjectAtScreenPosition(camera, GetScreenCenter(), out GameObject? centerSelected))
            {
                selected = centerSelected;
                lastHitObject = selected;
                ExplorerCore.Log($"WorldInspector: click fallback selected center-aim object '{selected.transform.GetTransformPath(true)}'");
            }
        }

        if (selected == null)
        {
            ExplorerCore.LogWarning("WorldInspector: click ignored because no object was detected under the mouse or center aim.");
            return;
        }

        ExplorerCore.Log($"WorldInspector: selected '{selected.transform.GetTransformPath(true)}'");
        DumpObjectDetails(selected);
        RuntimeHelper.StartCoroutine(InspectSelectedObjectNextFrame(selected));
    }

    private static System.Collections.IEnumerator InspectSelectedObjectNextFrame(GameObject selected)
    {
        // Defer inspector creation to the next frame so MouseInspector shutdown can finish first.
        yield return null;

        try
        {
            if (selected)
                InspectorManager.Inspect(selected);
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldInspector deferred inspect failed: {ex}");
        }
    }

    public override void UpdateMouseInspect(Vector2 mousePos)
    {
        // Attempt to ensure camera each time UpdateMouseInspect is called
        // in case something changed or wasn't set initially.
        Camera? camera = EnsureMainCamera();
        if (camera == null)
        {
            ExplorerCore.LogWarning("No Main Camera was found, unable to inspect world!");
            MouseInspector.Instance.StopInspect();
            return;
        }

        // First try the actual UnityExplorer mouse position.
        if (TryFindObjectAtScreenPosition(camera, mousePos, out GameObject? mouseObject))
        {
            OnHitGameObject(mouseObject, "mouse");
            return;
        }

        // Data Center is a first-person game: the interact target is normally selected
        // by the center-screen look ray, not by the OS cursor position. The yellow
        // outline visible in-game follows this center ray.
        Vector2 centerPos = GetScreenCenter();
        if (Vector2.Distance(mousePos, centerPos) > 5f &&
            TryFindObjectAtScreenPosition(camera, centerPos, out GameObject? centerObject))
        {
            OnHitGameObject(centerObject, "center aim");
            return;
        }

        if (lastHitObject)
            MouseInspector.Instance.ClearHitData();
    }

    private static Vector2 GetScreenCenter()
    {
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private static bool TryFindObjectAtScreenPosition(Camera camera, Vector2 screenPosition, [NotNullWhen(true)] out GameObject? result)
    {
        result = null;

        try
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);

            // Data Center special case:
            // Large environment colliders such as wall/floor/ceiling are often hit first,
            // while small props like the office computer/monitor only have renderers or
            // child meshes. Therefore we evaluate BOTH physics and renderer candidates.
            bool hasPhysicsObject = TryFindBestPhysicsHit(ray, out GameObject? physicsObject);
            bool hasRendererObject = TryFindRendererUnderMouse(camera, screenPosition, out GameObject? rendererObject);

            if (hasPhysicsObject && hasRendererObject && physicsObject != null && rendererObject != null)
            {
                if (ShouldPreferRendererCandidate(physicsObject, rendererObject))
                {
                    result = PromoteToUsefulParent(rendererObject);
                    return true;
                }

                result = physicsObject;
                return true;
            }

            if (hasPhysicsObject && physicsObject != null)
            {
                result = physicsObject;
                return true;
            }

            if (hasRendererObject && rendererObject != null)
            {
                result = PromoteToUsefulParent(rendererObject);
                return true;
            }
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldInspector screen-position lookup failed: {ex.Message}");
        }

        return false;
    }

    private static bool TryFindBestPhysicsHit(Ray ray, [NotNullWhen(true)] out GameObject? result)
    {
        result = null;

        try
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            GameObject? bestObject = null;
            int bestScore = int.MinValue;
            float bestDistance = float.MaxValue;

            foreach (RaycastHit hit in hits)
            {
                if (!hit.transform)
                    continue;

                GameObject hitObject = hit.transform.gameObject;
                if (!hitObject || !hitObject.activeInHierarchy)
                    continue;

                GameObject candidate = PromoteToUsefulParent(hitObject);
                int score = ScoreDataCenterHit(candidate, hitObject);

                if (score > bestScore || (score == bestScore && hit.distance < bestDistance))
                {
                    bestObject = candidate;
                    bestScore = score;
                    bestDistance = hit.distance;
                }
            }

            if (bestObject)
            {
                result = bestObject;
                return true;
            }
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldInspector physics hit fallback failed: {ex.Message}");
        }

        return false;
    }

    private static int ScoreDataCenterHit(GameObject candidate, GameObject originalHit)
    {
        int score = 0;

        try
        {
            // IMPORTANT for Unity 6000 IL2CPP/CoreCLR:
            // Do NOT call LayerMask.LayerToName here. In Data Center it can crash inside
            // UnityEngine.Bindings.OutStringMarshaller.GetStringAndDispose with an
            // AccessViolationException. Use the numeric layer only.
            score += ScoreKnownLayer(originalHit.layer);
            score += ScoreKnownLayer(candidate.layer) / 2;

            if (HasNamedComponent(candidate, "Interact")) score += 700;
            if (HasNamedComponent(candidate, "UsableObject")) score += 650;
            if (HasNamedComponent(candidate, "RayLookAt")) score += 620;
            if (HasNamedComponent(candidate, "Rack")) score += 550;
            if (HasNamedComponent(candidate, "RackMount")) score += 520;
            if (HasNamedComponent(candidate, "Server")) score += 500;
            if (HasNamedComponent(candidate, "NetworkSwitch")) score += 500;
            if (HasNamedComponent(candidate, "PatchPanel")) score += 500;
            if (HasNamedComponent(candidate, "RackDoor")) score += 450;
            if (HasNamedComponent(candidate, "Computer")) score += 430;
            if (HasNamedComponent(candidate, "Monitor")) score += 420;

            if (candidate.GetComponent<Collider>()) score += 100;
            if (candidate.GetComponent<Renderer>()) score += 80;
            if (candidate.transform.parent == null) score += 10;
        }
        catch
        {
        }

        return score;
    }

    private static int ScoreKnownLayer(int layer)
    {
        // Known/observed Data Center layers from globalgamemanagers.
        // This intentionally does not resolve layer names through Unity.
        return layer switch
        {
            2 => -100,  // Ignore Raycast
            3 => -1000, // Player in Data Center build
            5 => 800,   // CableHolder / game interaction layer in this build
            6 => 200,   // Wall
            7 => 900,   // OnlyForRayCast
            8 => 700,   // Trolley
            9 => 700,   // ObjectOnTrolley
            _ => 0,
        };
    }

    private static string SafeLayerLabel(int layer)
    {
        // Avoid LayerMask.LayerToName on this Unity 6000 IL2CPP build.
        return layer switch
        {
            0 => "Default",
            1 => "TransparentFX",
            2 => "Ignore Raycast",
            3 => "Player",
            4 => "Water",
            5 => "CableHolder/Interaction",
            6 => "Wall",
            7 => "OnlyForRayCast",
            8 => "Trolley",
            9 => "ObjectOnTrolley",
            _ => "Unknown/Custom",
        };
    }

    private static GameObject PromoteToUsefulParent(GameObject hitObject)
    {
        try
        {
            Transform? current = hitObject.transform;
            GameObject best = hitObject;

            for (int depth = 0; current != null && depth < 8; depth++)
            {
                GameObject go = current.gameObject;

                if (HasNamedComponent(go, "Interact") ||
                    HasNamedComponent(go, "UsableObject") ||
                    HasNamedComponent(go, "Rack") ||
                    HasNamedComponent(go, "RackMount") ||
                    HasNamedComponent(go, "Server") ||
                    HasNamedComponent(go, "NetworkSwitch") ||
                    HasNamedComponent(go, "PatchPanel") ||
                    HasNamedComponent(go, "RackDoor"))
                {
                    best = go;
                    break;
                }

                // Avoid LayerMask.LayerToName on Unity 6000 IL2CPP/CoreCLR.
                if (go.layer == 7 || go.layer == 5)
                    best = go;

                current = current.parent;
            }

            return best;
        }
        catch
        {
            return hitObject;
        }
    }

    private static bool HasNamedComponent(GameObject go, string typeName)
    {
        try
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (!component)
                    continue;

                Type? type = component.GetType();
                if (type != null && string.Equals(type.Name, typeName, StringComparison.Ordinal))
                    return true;

                try
                {
                    string? il2CppName = component.GetIl2CppType()?.Name;
                    if (string.Equals(il2CppName, typeName, StringComparison.Ordinal))
                        return true;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static void DumpObjectDetails(GameObject go)
    {
        try
        {
            if (!go)
            {
                ExplorerCore.LogWarning("WorldInspector Dump: GameObject is null.");
                return;
            }

            ExplorerCore.Log("========== WorldInspector Object Dump ==========");
            ExplorerCore.Log($"Name: {go.name}");
            ExplorerCore.Log($"Path: {go.transform.GetTransformPath(true)}");
            ExplorerCore.Log($"Layer: {go.layer} ({SafeLayerLabel(go.layer)})");
            // Avoid reading go.tag in this diagnostic build; Unity string marshalling is unstable on this game/runtime.
            ExplorerCore.Log("Tag: <not queried in Unity6000-safe build>");
            ExplorerCore.Log($"ActiveSelf: {go.activeSelf}");
            ExplorerCore.Log($"ActiveInHierarchy: {go.activeInHierarchy}");

            try
            {
                ExplorerCore.Log($"Scene: {go.scene.name} / Valid: {go.scene.IsValid()} / Loaded: {go.scene.isLoaded}");
            }
            catch (Exception sceneEx)
            {
                ExplorerCore.LogWarning($"Scene info failed: {sceneEx.Message}");
            }

            Component[] components = go.GetComponents<Component>();
            ExplorerCore.Log($"Components on selected object: {components.Length}");
            foreach (Component component in components)
            {
                if (!component)
                {
                    ExplorerCore.Log(" - <null component>");
                    continue;
                }

                string componentName = component.GetType().FullName ?? component.GetType().Name;
                try
                {
                    componentName = component.GetIl2CppType()?.FullName ?? componentName;
                }
                catch
                {
                }

                ExplorerCore.Log($" - {componentName}");
            }

            Collider? ownCollider = go.GetComponent<Collider>();
            Renderer? ownRenderer = go.GetComponent<Renderer>();
            Collider[] childColliders = go.GetComponentsInChildren<Collider>(true);
            Renderer[] childRenderers = go.GetComponentsInChildren<Renderer>(true);

            ExplorerCore.Log($"Own Collider: {(ownCollider ? ownCollider.GetType().Name : "none")}");
            ExplorerCore.Log($"Own Renderer: {(ownRenderer ? ownRenderer.GetType().Name : "none")}");
            ExplorerCore.Log($"Child Colliders: {childColliders.Length}");
            ExplorerCore.Log($"Child Renderers: {childRenderers.Length}");
            ExplorerCore.Log("================================================");
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldInspector DumpObjectDetails failed: {ex}");
        }
    }

    private static bool TryFindRendererUnderMouse(Camera camera, Vector2 mousePos, [NotNullWhen(true)] out GameObject? result)
    {
        result = null;

        try
        {
            UnityEngine.Object[] renderers = RuntimeHelper.FindObjectsOfTypeAll(typeof(Renderer));

            float bestAnyScore = float.MaxValue;
            GameObject? bestAnyObject = null;

            float bestPropScore = float.MaxValue;
            GameObject? bestPropObject = null;

            const float edgePadding = 18f;
            const float maxCenterDistance = 120f;

            foreach (UnityEngine.Object obj in renderers)
            {
                Renderer? renderer = obj.TryCast<Renderer>();
                if (!renderer)
                    continue;

                GameObject go = renderer.gameObject;
                if (!go || !go.activeInHierarchy || !renderer.enabled)
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 centerScreen = camera.WorldToScreenPoint(bounds.center);
                if (centerScreen.z <= 0f)
                    continue;

                if (!TryGetScreenBounds(camera, bounds, out Rect screenRect))
                    continue;

                Rect paddedRect = new(
                    screenRect.xMin - edgePadding,
                    screenRect.yMin - edgePadding,
                    screenRect.width + edgePadding * 2f,
                    screenRect.height + edgePadding * 2f);

                bool mouseInsideBounds = paddedRect.Contains(mousePos);
                float centerDistance = Vector2.Distance(new Vector2(centerScreen.x, centerScreen.y), mousePos);

                if (!mouseInsideBounds && centerDistance > maxCenterDistance)
                    continue;

                float projectedArea = Math.Max(1f, Math.Abs(screenRect.width * screenRect.height));

                // Prefer small visible props directly under the cursor.
                // Big environment meshes like walls/floor/ceiling cover huge screen areas and
                // otherwise steal the hit from small objects such as the office computer.
                float score = centerScreen.z;
                score += centerDistance * (mouseInsideBounds ? 0.02f : 1.0f);
                score += Math.Min(projectedArea, 750000f) * 0.004f;

                if (!mouseInsideBounds)
                    score += 5000f;

                bool environment = IsEnvironmentObject(go);
                bool useful = LooksLikeUsefulVisibleObject(go);

                if (environment)
                    score += 20000f;

                if (useful)
                    score -= 4000f;

                if (score < bestAnyScore)
                {
                    bestAnyScore = score;
                    bestAnyObject = go;
                }

                if (!environment && score < bestPropScore)
                {
                    bestPropScore = score;
                    bestPropObject = go;
                }
            }

            // Prefer non-environment renderers when present. This prevents a wall/floor collider
            // behind a desk object from stealing the selection.
            if (bestPropObject)
            {
                result = PromoteToUsefulParent(bestPropObject);
                return true;
            }

            if (bestAnyObject)
            {
                result = PromoteToUsefulParent(bestAnyObject);
                return true;
            }
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldInspector renderer fallback failed: {ex.Message}");
        }

        return false;
    }

    private static bool ShouldPreferRendererCandidate(GameObject physicsObject, GameObject rendererObject)
    {
        try
        {
            if (!physicsObject || !rendererObject)
                return false;

            if (physicsObject == rendererObject)
                return false;

            bool physicsIsEnvironment = IsEnvironmentObject(physicsObject);
            bool rendererIsEnvironment = IsEnvironmentObject(rendererObject);

            if (physicsIsEnvironment && !rendererIsEnvironment)
                return true;

            if (LooksLikeUsefulVisibleObject(rendererObject) && !LooksLikeUsefulVisibleObject(physicsObject))
                return true;
        }
        catch
        {
        }

        return false;
    }

    private static bool IsEnvironmentObject(GameObject go)
    {
        try
        {
            if (!go)
                return false;

            string name = go.name ?? string.Empty;
            string lower = name.ToLowerInvariant();

            if (lower.Contains("wall") ||
                lower.Contains("floor") ||
                lower.Contains("ceiling") ||
                lower.Contains("roof") ||
                lower.Contains("static"))
            {
                return true;
            }

            Transform? current = go.transform.parent;
            int depth = 0;
            while (current != null && depth < 4)
            {
                string parentName = current.gameObject.name ?? string.Empty;
                string parentLower = parentName.ToLowerInvariant();

                if (parentLower.Contains("walls") ||
                    parentLower.Contains("floors") ||
                    parentLower.Contains("ceiling") ||
                    parentLower.Contains("office/walls"))
                {
                    return true;
                }

                current = current.parent;
                depth++;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool LooksLikeUsefulVisibleObject(GameObject go)
    {
        try
        {
            if (!go)
                return false;

            if (HasNamedComponent(go, "Interact") ||
                HasNamedComponent(go, "UsableObject") ||
                HasNamedComponent(go, "RayLookAt") ||
                HasNamedComponent(go, "Rack") ||
                HasNamedComponent(go, "RackMount") ||
                HasNamedComponent(go, "Server") ||
                HasNamedComponent(go, "NetworkSwitch") ||
                HasNamedComponent(go, "PatchPanel") ||
                HasNamedComponent(go, "Computer") ||
                HasNamedComponent(go, "Monitor"))
            {
                return true;
            }

            string lower = (go.name ?? string.Empty).ToLowerInvariant();
            return lower.Contains("computer") ||
                   lower.Contains("monitor") ||
                   lower.Contains("screen") ||
                   lower.Contains("keyboard") ||
                   lower.Contains("mouse") ||
                   lower.Contains("desk") ||
                   lower.Contains("table") ||
                   lower.Contains("chair") ||
                   lower.Contains("rack") ||
                   lower.Contains("server") ||
                   lower.Contains("switch") ||
                   lower.Contains("radio") ||
                   lower.Contains("fridge") ||
                   lower.Contains("whiteboard");
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetScreenBounds(Camera camera, Bounds bounds, out Rect rect)
    {
        rect = default;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners =
        {
            new(min.x, min.y, min.z),
            new(min.x, min.y, max.z),
            new(min.x, max.y, min.z),
            new(min.x, max.y, max.z),
            new(max.x, min.y, min.z),
            new(max.x, min.y, max.z),
            new(max.x, max.y, min.z),
            new(max.x, max.y, max.z),
        };

        bool foundVisibleCorner = false;
        float xMin = float.MaxValue;
        float yMin = float.MaxValue;
        float xMax = float.MinValue;
        float yMax = float.MinValue;

        foreach (Vector3 corner in corners)
        {
            Vector3 screen = camera.WorldToScreenPoint(corner);
            if (screen.z <= 0f)
                continue;

            foundVisibleCorner = true;
            xMin = Math.Min(xMin, screen.x);
            yMin = Math.Min(yMin, screen.y);
            xMax = Math.Max(xMax, screen.x);
            yMax = Math.Max(yMax, screen.y);
        }

        if (!foundVisibleCorner)
            return false;

        rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return true;
    }

    internal void OnHitGameObject(GameObject obj, string source = "")
    {
        if (obj != lastHitObject)
        {
            lastHitObject = obj;

            string sourceLabel = string.IsNullOrEmpty(source) ? string.Empty : $" <color=grey>({source})</color>";
            MouseInspector.Instance.UpdateObjectNameLabel($"<b>Click to Inspect:</b>{sourceLabel} <color=cyan>{obj.name}</color>");
            MouseInspector.Instance.UpdateObjectPathLabel($"Path: {obj.transform.GetTransformPath(true)}");
        }
    }

    public override void OnEndInspect()
    {
        // not needed
    }


    private static Camera? EnsureMainCamera()
    {
        if (MainCamera != null)
        {
            try
            {
                MouseInspector.Instance.UpdateInspectorTitle(
                    $"<b>World Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)"
                );
            }
            catch
            {
            }

            return MainCamera;
        }

        if (TryGetValidMainCamera(out var camera))
        {
            MainCamera = camera;
            MouseInspector.Instance.UpdateInspectorTitle(
                $"<b>World Inspector ({camera.name})</b> (press <b>ESC</b> to cancel)"
            );
            ExplorerCore.Log($"Using '{camera.transform.GetTransformPath(true)}'");
            return camera;
        }
        return null;
    }

    private static bool TryGetValidMainCamera(
#if NETFRAMEWORK
        out Camera camera)
#else
        [NotNullWhen(true)] out Camera? camera)
#endif
    {
        camera = Camera.main;
        if (camera != null)
        {
            return true;
        }

        ExplorerCore.LogWarning("No Camera.main found, trying to find a camera named 'Main Camera' or 'MainCamera'...");
        camera = Camera.allCameras.FirstOrDefault(
            c => c.name == "Main Camera" || c.name == "MainCamera");
        if (camera != null)
        {
            return true;
        }

        ExplorerCore.LogWarning("No camera named 'Main Camera' or 'MainCamera' found, using the first camera created...");
        camera = Camera.allCameras.FirstOrDefault();
        if (camera != null)
        {
            return true;
        }

        // If we reach here, no cameras were found at all.
        ExplorerCore.LogWarning("No valid main cameras found!");
        return false;
    }
}
