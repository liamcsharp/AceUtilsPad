using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

internal static class UiMotion
{
	public static bool Enabled = true;

	public static float EaseOutCubic(float t)
	{
		t = Mathf.Clamp01(t);
		float inverse = 1f - t;
		return 1f - inverse * inverse * inverse;
	}

	public static float EaseOutBack(float t)
	{
		t = Mathf.Clamp01(t);
		const float overshoot = 1.4f;
		float shifted = t - 1f;
		return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
	}

	public static float EaseInCubic(float t)
	{
		t = Mathf.Clamp01(t);
		return t * t * t;
	}

	public static float Damp(float current, float target, float sharpness, float deltaTime)
	{
		return Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));
	}
}

internal static class ButtonFactory
{
	private const float IconInset = 0.2f;
	private const float LabelMargin = 4f;

	public static UiButton Create(Transform parent, string name, ButtonSpec spec, float width, float height,
		int cornerRadius, float fontSize, Action afterClick, ButtonStyle style = ButtonStyle.Solid)
	{
		RectTransform rect = UiKit.NewRect(name, parent);
		rect.sizeDelta = new Vector2(width, height);

		Image background = AddBackground(rect, cornerRadius);
		Button button = AddButton(rect, background);

		TextMeshProUGUI label = UiKit.NewText("Text", rect, fontSize, TextAlignmentOptions.Center,
			style == ButtonStyle.Tab ? FontStyles.Bold : FontStyles.Normal);
		UiKit.Stretch(label.rectTransform);
		label.margin = new Vector4(LabelMargin, 0f, LabelMargin, 0f);

		UiButton result = new UiButton(rect, background, label, null, button, spec.Text, spec.Active, spec.Visible, style);
		Wire(button, result, spec.Click, afterClick);
		return result;
	}

	public static UiButton CreateIcon(Transform parent, string name, string iconId, float size, int cornerRadius,
		Action click, Func<bool> active, Action afterClick, ButtonStyle style = ButtonStyle.Rail)
	{
		RectTransform rect = UiKit.NewRect(name, parent);
		rect.sizeDelta = new Vector2(size, size);

		Image background = AddBackground(rect, cornerRadius);
		Button button = AddButton(rect, background);

		Image icon = UiKit.NewImage("Icon", rect, SpriteFactory.Icon(iconId), false);
		icon.type = Image.Type.Simple;
		icon.preserveAspect = true;
		icon.rectTransform.anchorMin = new Vector2(IconInset, IconInset);
		icon.rectTransform.anchorMax = new Vector2(1f - IconInset, 1f - IconInset);
		icon.rectTransform.offsetMin = Vector2.zero;
		icon.rectTransform.offsetMax = Vector2.zero;

		UiButton result = new UiButton(rect, background, null, icon, button, null, active, null, style);
		Wire(button, result, click, afterClick);
		return result;
	}

	private static Image AddBackground(RectTransform rect, int cornerRadius)
	{
		Image background = rect.gameObject.AddComponent<Image>();
		background.sprite = SpriteFactory.Rounded(cornerRadius);
		background.type = Image.Type.Sliced;
		background.raycastTarget = true;
		return background;
	}

	private static Button AddButton(RectTransform rect, Image background)
	{
		Button button = rect.gameObject.AddComponent<Button>();
		button.transition = Selectable.Transition.None;
		button.targetGraphic = background;
		button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
		return button;
	}

	private static void Wire(Button button, UiButton owner, Action click, Action afterClick)
	{
		button.onClick.AddListener(() =>
		{
			owner.NotifyPressed(UiMotion.Enabled);
			Feedback.Click();
			if (click != null)
			{
				click();
			}

			if (afterClick != null)
			{
				afterClick();
			}
		});
	}
}
