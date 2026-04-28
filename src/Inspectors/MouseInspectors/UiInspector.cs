using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class UiInspector : MouseInspectorBase
    {
        public static readonly List<GameObject> LastHitObjects = new();

        private static GraphicRaycaster[] graphicRaycasters;

        private static readonly List<GameObject> currentHitObjects = new();

        private static readonly List<Graphic> wasDisabledGraphics = new();
        private static readonly List<CanvasGroup> wasDisabledCanvasGroups = new();
        private static readonly List<GameObject> objectsAddedCastersTo = new();

        private const string DEFAULT_INSPECTOR_TITLE = "<b>UI Inspector</b> (press <b>ESC</b> to cancel)";

        public override void OnBeginMouseInspect()
        {
            SetupUIRaycast();
            MouseInspector.Instance.UpdateInspectorTitle(DEFAULT_INSPECTOR_TITLE);
            MouseInspector.Instance.UpdateObjectPathLabel("");
        }

        public override void ClearHitData()
        {
            currentHitObjects.Clear();
        }

        public override void OnSelectMouseInspect()
        {
            LastHitObjects.Clear();
            LastHitObjects.AddRange(currentHitObjects);
            RuntimeHelper.StartCoroutine(SetPanelActiveCoro());
        }

        IEnumerator SetPanelActiveCoro()
        {
            yield return null;

            try
            {
                MouseInspectorResultsPanel panel = UIManager.GetPanel<MouseInspectorResultsPanel>(UIManager.Panels.UIInspectorResults);
                panel.SetActive(true);
                panel.ShowResults();
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"UiInspector: failed to open UIInspectorResults panel: {ex}");
            }
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            currentHitObjects.Clear();

            if (ExplorerCore.IsUnity6000OrNewer)
            {
                UpdateMouseInspectUnity6000(mousePos);
                return;
            }

            if (graphicRaycasters == null || graphicRaycasters.Length == 0)
            {
                MouseInspector.Instance.UpdateObjectNameLabel("No UI raycasters found.");
                return;
            }

            PointerEventData ped = new(null)
            {
                position = mousePos
            };

            foreach (GraphicRaycaster gr in graphicRaycasters)
            {
                if (!gr || !gr.canvas)
                    continue;

                List<RaycastResult> list = new();
                RuntimeHelper.GraphicRaycast(gr, ped, list);
                if (list.Count > 0)
                {
                    foreach (RaycastResult hit in list)
                    {
                        if (hit.gameObject)
                            currentHitObjects.Add(hit.gameObject);
                    }
                }
            }

            if (currentHitObjects.Any())
                MouseInspector.Instance.UpdateObjectNameLabel($"Click to view UI Objects under mouse: {currentHitObjects.Count}");
            else
                MouseInspector.Instance.UpdateObjectNameLabel("No UI objects under mouse.");
        }

        private static void UpdateMouseInspectUnity6000(Vector2 mousePos)
        {
            EventSystem currentEventSystem = EventSystem.current;
            if (currentEventSystem == null)
            {
                MouseInspector.Instance.UpdateObjectNameLabel("No EventSystem available.");
                return;
            }

            PointerEventData ped = new(currentEventSystem)
            {
                position = mousePos
            };

            Il2CppSystem.Collections.Generic.List<RaycastResult> list = new();
            currentEventSystem.RaycastAll(ped, list);

            for (int i = 0; i < list.Count; i++)
            {
                RaycastResult hit = list[i];
                if (!hit.gameObject)
                    continue;

                currentHitObjects.Add(hit.gameObject);
            }

            if (currentHitObjects.Any())
                MouseInspector.Instance.UpdateObjectNameLabel($"Click to view UI Objects under mouse: {currentHitObjects.Count}");
            else
                MouseInspector.Instance.UpdateObjectNameLabel("No UI objects under mouse.");
        }

        private static void SetupUIRaycast()
        {
            if (ExplorerCore.IsUnity6000OrNewer)
            {
                // Scene/global object scans are unstable on some Unity 6000 IL2CPP builds.
                // Use EventSystem.RaycastAll in UpdateMouseInspectUnity6000 instead.
                ExplorerCore.Log("UiInspector: Unity6000 mode uses EventSystem.RaycastAll (no global scene scan).");
                return;
            }

            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Canvas)))
            {
                Canvas canvas = obj.TryCast<Canvas>();
                if (!canvas || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    continue;
                if (!canvas.GetComponent<GraphicRaycaster>())
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    objectsAddedCastersTo.Add(canvas.gameObject);
                }
            }

            // recache Graphic Raycasters each time we start
            UnityEngine.Object[] casters = RuntimeHelper.FindObjectsOfTypeAll(typeof(GraphicRaycaster));
            graphicRaycasters = new GraphicRaycaster[casters.Length];
            for (int i = 0; i < casters.Length; i++)
            {
                graphicRaycasters[i] = casters[i].TryCast<GraphicRaycaster>();
            }

            // enable raycastTarget on Graphics
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(Graphic)))
            {
                Graphic graphic = obj.TryCast<Graphic>();
                if (!graphic || !graphic.enabled || graphic.raycastTarget || !graphic.gameObject.activeInHierarchy)
                    continue;
                graphic.raycastTarget = true;
                wasDisabledGraphics.Add(graphic);
            }

            // enable blocksRaycasts on CanvasGroups
            foreach (UnityEngine.Object obj in RuntimeHelper.FindObjectsOfTypeAll(typeof(CanvasGroup)))
            {
                CanvasGroup canvas = obj.TryCast<CanvasGroup>();
                if (!canvas || !canvas.gameObject.activeInHierarchy || canvas.blocksRaycasts)
                    continue;
                canvas.blocksRaycasts = true;
                wasDisabledCanvasGroups.Add(canvas);
            }
        }

        public override void OnEndInspect()
        {
            foreach (GameObject obj in objectsAddedCastersTo)
            {
                if (obj.GetComponent<GraphicRaycaster>() is GraphicRaycaster raycaster)
                    GameObject.Destroy(raycaster);
            }

            foreach (Graphic graphic in wasDisabledGraphics)
           {
               if (graphic)
                 graphic.raycastTarget = false;
           }

            foreach (CanvasGroup canvas in wasDisabledCanvasGroups)
           {
               if (canvas)
                 canvas.blocksRaycasts = false;
           }

            objectsAddedCastersTo.Clear();
            wasDisabledCanvasGroups.Clear();
            wasDisabledGraphics.Clear();
        }
    }
}
