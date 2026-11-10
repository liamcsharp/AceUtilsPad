using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

internal static class UiKit
{
	public static RectTransform NewRect(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		gameObject.transform.SetParent(parent, false);
		return (RectTransform)gameObject.transform;
	}

	public static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
	{
		Vector2 topLeft = new Vector2(0f, 1f);
		rect.anchorMin = topLeft;
		rect.anchorMax = topLeft;
		rect.pivot = topLeft;
		rect.anchoredPosition = new Vector2(x, -y);
		rect.sizeDelta = new Vector2(width, height);
	}

	public static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	public static Image NewImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
	{
		RectTransform rect = NewRect(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.sprite = sprite;
		image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
		image.raycastTarget = raycastTarget;
		return image;
	}

	public static TextMeshProUGUI NewText(string name, Transform parent, float fontSize, TextAlignmentOptions alignment, FontStyles style)
	{
		RectTransform rect = NewRect(name, parent);
		TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.alignment = alignment;
		text.fontStyle = style;
		text.richText = true;
		text.raycastTarget = false;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.overflowMode = TextOverflowModes.Ellipsis;
		return text;
	}
}
