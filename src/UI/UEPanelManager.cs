using UnityExplorer.UI.Panels;
using UniverseLib.Input;
using UniverseLib.UI;
using UniverseLib.UI.Panels;

namespace UnityExplorer.UI
{
    public class UEPanelManager : PanelManager
    {
        public UEPanelManager(UIBase owner) : base(owner) { }

        protected override Vector3 MousePosition => ExplorerCore.IsUnity6000OrNewer
            ? InputManager.MousePosition
            : DisplayManager.MousePosition;

        protected override Vector2 ScreenDimensions => ExplorerCore.IsUnity6000OrNewer
            ? new Vector2(Screen.width, Screen.height)
            : new Vector2(DisplayManager.Width, DisplayManager.Height);

        protected override bool MouseInTargetDisplay => ExplorerCore.IsUnity6000OrNewer
            ? true
            : DisplayManager.MouseInTargetDisplay;

        internal void DoInvokeOnPanelsReordered()
        {
            InvokeOnPanelsReordered();
        }

        protected override void SortDraggerHeirarchy()
        {
            base.SortDraggerHeirarchy();

            // move AutoCompleter to first update
            if (!UIManager.Initializing && AutoCompleteModal.Instance != null)
            {
                this.draggerInstances.Remove(AutoCompleteModal.Instance.Dragger);
                this.draggerInstances.Insert(0, AutoCompleteModal.Instance.Dragger);
            }
        }
    }
}
