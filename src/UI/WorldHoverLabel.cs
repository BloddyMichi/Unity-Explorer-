using UnityExplorer.Config;
using UnityExplorer.Inspectors;
using UnityExplorer.Inspectors.MouseInspectors;
using UniverseLib.UI;

#nullable enable

namespace UnityExplorer.UI;

public static class WorldHoverLabel
{
    private const float UpdateInterval = 0.15f;
    private const float VerticalOffset = 48f;

    private static GameObject? root;
    private static Text? targetText;
    private static CanvasGroup? canvasGroup;
    private static float timeOfLastUpdate;
    private static GameObject? lastTarget;

    public static void Init(GameObject parent)
    {
        if (root != null)
            return;

        root = UIFactory.CreateUIObject("WorldHoverTargetLabel", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 34f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.03f, 0.03f, 0.03f, 0.78f);
        background.raycastTarget = false;

        UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(
            root,
            true,
            true,
            true,
            true,
            0,
            8,
            8,
            4,
            4,
            TextAnchor.MiddleCenter);

        targetText = UIFactory.CreateLabel(
            root,
            "WorldHoverTargetText",
            string.Empty,
            TextAnchor.MiddleCenter,
            Color.white,
            true,
            14);

        targetText.horizontalOverflow = HorizontalWrapMode.Wrap;
        targetText.verticalOverflow = VerticalWrapMode.Truncate;
        targetText.gameObject.AddComponent<Outline>();
        targetText.raycastTarget = false;
        UIFactory.SetLayoutElement(targetText.gameObject, minHeight: 24, flexibleWidth: 9999, flexibleHeight: 0);

        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Hide();
    }

    public static void Update()
    {
        if (root == null || targetText == null)
            return;

        if (!ConfigManager.World_Hover_Label.Value || MouseInspector.Inspecting)
        {
            Hide();
            return;
        }

        if (!timeOfLastUpdate.OccuredEarlierThan(UpdateInterval))
            return;

        timeOfLastUpdate = Time.realtimeSinceStartup;

        try
        {
            if (WorldInspector.TryFindAimTarget(out GameObject? target, out string source))
            {
                Show(target, source);
                return;
            }
        }
        catch (Exception ex)
        {
            ExplorerCore.LogWarning($"WorldHoverLabel update failed: {ex.Message}");
        }

        Hide();
    }

    public static void Hide()
    {
        lastTarget = null;

        if (targetText != null)
            targetText.text = string.Empty;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (root != null)
            root.SetActive(false);
    }

    private static void Show(GameObject target, string source)
    {
        if (root == null || targetText == null || target == null || !target)
            return;

        PositionNearAimPoint();

        if (target != lastTarget)
        {
            string sourceLabel = string.IsNullOrEmpty(source)
                ? string.Empty
                : $" <color=grey>({source})</color>";

            targetText.text = $"<b>Target:</b>{sourceLabel} <color=cyan>{target.name}</color>";
            lastTarget = target;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (!root.activeSelf)
            root.SetActive(true);
    }

    private static void PositionNearAimPoint()
    {
        if (root == null || UIManager.UIRootRect == null)
            return;

        Vector3 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 inversePos = UIManager.UIRootRect.InverseTransformPoint(screenCenter);
        root.transform.localPosition = new Vector3(inversePos.x, inversePos.y + VerticalOffset, 0f);
    }
}
