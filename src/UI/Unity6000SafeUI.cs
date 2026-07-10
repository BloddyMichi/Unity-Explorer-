using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI;

internal static class Unity6000SafeUI
{
    private static readonly Color SafeInputColor = new(0.12f, 0.12f, 0.12f);
    private static readonly Color SafeContainerColor = new(0.1f, 0.1f, 0.1f);

    public static InputFieldRef CreateSafeMultiLineInput(GameObject parent, string name, string placeholder, int fontSize)
    {
        InputFieldRef input = UIFactory.CreateInputField(parent, name, placeholder);
        UIFactory.SetLayoutElement(input.Component.gameObject, minWidth: 100, minHeight: 30, flexibleWidth: 5000, flexibleHeight: 5000);

        input.Component.lineType = InputField.LineType.MultiLineNewline;
        input.Component.targetGraphic.color = SafeInputColor;
        input.Component.textComponent.alignment = TextAnchor.UpperLeft;
        input.Component.textComponent.fontSize = fontSize;
        input.Component.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        input.PlaceholderText.alignment = TextAnchor.UpperLeft;
        input.PlaceholderText.fontSize = fontSize;
        input.PlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;

        return input;
    }

    public static GameObject CreateSafeVerticalContainer(GameObject parent, string name, Vector4 padding)
    {
        GameObject container = UIFactory.CreateVerticalGroup(
            parent,
            name,
            true,
            false,
            true,
            true,
            0,
            padding,
            SafeContainerColor,
            TextAnchor.UpperLeft);

        UIFactory.SetLayoutElement(container, flexibleWidth: 9999, flexibleHeight: 9999);
        return container;
    }
}
