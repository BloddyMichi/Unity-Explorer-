// Patched MouseInspector.cs
using UnityExplorer.Config;
using UnityExplorer.Inspectors.MouseInspectors;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Panels;

namespace UnityExplorer.Inspectors
{
    public enum MouseInspectMode
    {
        World,
        UI
    }

    public class MouseInspector : PanelBase
    {
        public static MouseInspector Instance { get; private set; }

        private readonly WorldInspector worldInspector;
        private readonly UiInspector uiInspector;

        public static bool Inspecting { get; set; }
        public static MouseInspectMode Mode { get; set; }

        public MouseInspectorBase CurrentInspector => Mode switch
        {
            MouseInspectMode.UI => uiInspector,
            MouseInspectMode.World => worldInspector,
            _ => null,
        };

        private static Vector3 lastMousePos;

        internal static readonly string UIBaseGUID = $"{ExplorerCore.GUID}.MouseInspector";
        internal static UIBase inspectorUIBase;

        public override string Name => "Inspect Under Mouse";
        public override int MinWidth => -1;
        public override int MinHeight => -1;
        public override Vector2 DefaultAnchorMin => Vector2.zero;
        public override Vector2 DefaultAnchorMax => Vector2.zero;

        public override bool CanDragAndResize => false;

        private Text inspectorLabelTitle;
        private Text objNameLabel;
        private Text objPathLabel;
        private Text mousePosLabel;

        public MouseInspector(UIBase owner) : base(owner)
        {
            Instance = this;
            worldInspector = new WorldInspector();
            uiInspector = new UiInspector();
        }

        public static void OnDropdownSelect(int index)
        {
            try
            {
                if (index == 0)
                    return;

                if (Instance == null)
                {
                    ExplorerCore.LogWarning("MouseInspector.OnDropdownSelect called before MouseInspector.Instance existed.");
                    InspectorPanel.SafeResetMouseInspectDropdown();
                    return;
                }

                switch (index)
                {
                    case 1:
                        Instance.StartInspect(MouseInspectMode.World);
                        break;
                    case 2:
                        Instance.StartInspect(MouseInspectMode.UI);
                        break;
                }
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.OnDropdownSelect failed: {e}");
            }
            finally
            {
                InspectorPanel.SafeResetMouseInspectDropdown();
            }
        }

        public void StartInspect(MouseInspectMode mode)
        {
            try
            {
                Mode = mode;
                Inspecting = true;

                if (CurrentInspector == null)
                {
                    ExplorerCore.LogWarning("MouseInspector.StartInspect aborted because CurrentInspector was null.");
                    Inspecting = false;
                    return;
                }

                CurrentInspector.OnBeginMouseInspect();

                PanelManager.ForceEndResize();
                if (UIManager.NavBarRect != null)
                    UIManager.NavBarRect.gameObject.SetActive(false);
                if (UIManager.UiBase?.Panels?.PanelHolder != null)
                    UIManager.UiBase.Panels.PanelHolder.SetActive(false);
                UIManager.UiBase?.SetOnTop();

                SetActive(true);
            }
            catch (Exception e)
            {
                Inspecting = false;
                ExplorerCore.LogWarning($"MouseInspector.StartInspect failed: {e}");
                InspectorPanel.SafeResetMouseInspectDropdown();
            }
        }

        internal void ClearHitData()
        {
            try
            {
                CurrentInspector?.ClearHitData();

                if (objNameLabel)
                    objNameLabel.text = "No hits...";
                if (objPathLabel)
                    objPathLabel.text = "";
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.ClearHitData failed: {e}");
            }
        }

        public void StopInspect()
        {
            try
            {
                CurrentInspector?.OnEndInspect();
                ClearHitData();
                Inspecting = false;

                if (UIManager.NavBarRect != null)
                    UIManager.NavBarRect.gameObject.SetActive(true);
                if (UIManager.UiBase?.Panels?.PanelHolder != null)
                    UIManager.UiBase.Panels.PanelHolder.SetActive(true);

                Dropdown drop = InspectorPanel.Instance?.MouseInspectDropdown;
                if (!ExplorerCore.IsUnity6000OrNewer
                    && drop != null
                    && drop.transform.Find("Dropdown List") is Transform list)
                {
                    drop.DestroyDropdownList(list.gameObject);
                }

                if (UIRoot != null)
                    UIRoot.SetActive(false);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.StopInspect failed: {e}");
            }
            finally
            {
                InspectorPanel.SafeResetMouseInspectDropdown();
            }
        }

        private static float timeOfLastRaycast;

        public bool TryUpdate()
        {
            try
            {
                if (InputManager.GetKeyDown(ConfigManager.World_MouseInspect_Keybind.Value))
                    Instance?.StartInspect(MouseInspectMode.World);

                if (InputManager.GetKeyDown(ConfigManager.UI_MouseInspect_Keybind.Value))
                    Instance?.StartInspect(MouseInspectMode.UI);

                if (Inspecting)
                    UpdateInspect();

                return Inspecting;
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.TryUpdate failed: {e}");
                return Inspecting;
            }
        }

        internal void UpdateInspectorTitle(string title)
        {
            if (inspectorLabelTitle)
                inspectorLabelTitle.text = title;
        }

        internal void UpdateObjectNameLabel(string name)
        {
            if (objNameLabel)
                objNameLabel.text = name;
        }

        internal void UpdateObjectPathLabel(string path)
        {
            if (objPathLabel)
                objPathLabel.text = path;
        }

        public void UpdateInspect()
        {
            try
            {
                if (InputManager.GetKeyDown(KeyCode.Escape))
                {
                    StopInspect();
                    return;
                }

                if (InputManager.GetMouseButtonDown(0))
                {
                    CurrentInspector?.OnSelectMouseInspect();
                    StopInspect();
                    return;
                }

                Vector3 mousePos = InputManager.MousePosition;
                if (mousePos != lastMousePos)
                    UpdatePosition(mousePos);

                if (!timeOfLastRaycast.OccuredEarlierThan(0.1f))
                    return;
                timeOfLastRaycast = Time.realtimeSinceStartup;

                CurrentInspector?.UpdateMouseInspect(mousePos);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.UpdateInspect failed: {e}");
                StopInspect();
            }
        }

        internal void UpdatePosition(Vector2 mousePos)
        {
            try
            {
                lastMousePos = mousePos;

                if (mousePosLabel)
                    mousePosLabel.text = $"<color=grey>Mouse Position:</color> {mousePos.ToString()}";

                if (mousePos.x < 350)
                    mousePos.x = 350;
                if (mousePos.x > Screen.width - 350)
                    mousePos.x = Screen.width - 350;
                if (mousePos.y < Rect.rect.height)
                    mousePos.y += Rect.rect.height + 10;
                else
                    mousePos.y -= 10;

                if (inspectorUIBase?.RootObject == null || UIRoot == null)
                    return;

                Vector3 inversePos = inspectorUIBase.RootObject.transform.InverseTransformPoint(mousePos);
                UIRoot.transform.localPosition = new Vector3(inversePos.x, inversePos.y, 0);
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"MouseInspector.UpdatePosition failed: {e}");
            }
        }

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.anchorMin = Vector2.zero;
            Rect.anchorMax = Vector2.zero;
            Rect.pivot = new Vector2(0.5f, 1);
            // Smaller overlay for Data Center: less obstruction while inspecting racks/devices.
            Rect.sizeDelta = new Vector2(560, 115);
        }

        protected override void ConstructPanelContent()
        {
            this.TitleBar.SetActive(false);
            this.UIRoot.transform.SetParent(UIManager.UIRoot.transform, false);

            GameObject inspectContent = UIFactory.CreateVerticalGroup(this.ContentRoot, "InspectContent", true, false, true, true, 2, new Vector4(4, 4, 3, 3));
            UIFactory.SetLayoutElement(inspectContent, flexibleWidth: 9999, flexibleHeight: 9999);

            inspectorLabelTitle = UIFactory.CreateLabel(inspectContent, "InspectLabel", "", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(inspectorLabelTitle.gameObject, minHeight: 20, preferredHeight: 20, flexibleWidth: 9999, flexibleHeight: 0);

            mousePosLabel = UIFactory.CreateLabel(inspectContent, "MousePosLabel", "Mouse Position:", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(mousePosLabel.gameObject, minHeight: 18, preferredHeight: 18, flexibleWidth: 9999, flexibleHeight: 0);

            objNameLabel = UIFactory.CreateLabel(inspectContent, "HitLabelObj", "No hits...", TextAnchor.MiddleLeft);
            objNameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            UIFactory.SetLayoutElement(objNameLabel.gameObject, minHeight: 20, preferredHeight: 20, flexibleWidth: 9999, flexibleHeight: 0);

            objPathLabel = UIFactory.CreateLabel(inspectContent, "PathLabel", "", TextAnchor.MiddleLeft);
            objPathLabel.fontStyle = FontStyle.Italic;
            objPathLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            UIFactory.SetLayoutElement(objPathLabel.gameObject, minHeight: 36, flexibleHeight: 9999);

            UIRoot.SetActive(false);
        }
    }
}
