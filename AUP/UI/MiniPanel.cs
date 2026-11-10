using System;
using System.Collections.Generic;
using UnityEngine;

namespace AUP.UI;

public sealed class DelegatePage : MenuPage
{
	private readonly string title;
	private readonly string iconId;
	private readonly Action<PageBuilder> build;

	public DelegatePage(MenuContext context, string title, string iconId, Action<PageBuilder> build) : base(context)
	{
		this.title = title;
		this.iconId = iconId;
		this.build = build;
	}

	public override string Title => title;

	public override string IconId => iconId;

	public override void Build(PageBuilder ui)
	{
		build(ui);
	}
}

internal sealed class MiniPanel
{
	private const float TabHeight = 22f;
	private const float TabGap = 6f;

	private readonly List<PageView> views = new List<PageView>();
	private readonly List<UiButton> tabs = new List<UiButton>();
	private readonly List<UiButton> extras = new List<UiButton>();
	private readonly List<UiLabel> extraLabels = new List<UiLabel>();
	private int current;

	public MiniPanel(MenuContext context, RectTransform parent, float x, float y, float width, float height, IReadOnlyList<MenuPage> pages)
	{
		float tabWidth = width / Mathf.Max(1, pages.Count);
		for (int i = 0; i < pages.Count; i++)
		{
			int index = i;
			ButtonSpec spec = new ButtonSpec(pages[i].Title, () => Select(index), () => current == index);
			UiButton tab = ButtonFactory.Create(parent, "Tab", spec, tabWidth - 2f, TabHeight, 6, 8.5f, context.RequestRefresh);
			UiKit.PlaceTopLeft(tab.Rect, x + i * tabWidth, y, tabWidth - 2f, TabHeight);
			tabs.Add(tab);

			RectTransform root = UiKit.NewRect(pages[i].Title, parent);
			UiKit.PlaceTopLeft(root, x, y + TabHeight + TabGap, width, height - TabHeight - TabGap);
			PageView view = new PageView { Page = pages[i], Root = root };
			PageBuilder builder = new PageBuilder(context, view, width, height - TabHeight - TabGap);
			try
			{
				pages[i].Build(builder);
			}
			catch (Exception exception)
			{
				Debug.LogError("[AceUtilsPad] Panel '" + pages[i].Title + "' failed to build: " + exception);
			}

			builder.Finish();
			views.Add(view);
		}

		Select(0);
	}

	public bool Dirty { get; set; } = true;

	public void AddExtra(UiButton button)
	{
		extras.Add(button);
	}

	public void AddExtra(UiLabel label)
	{
		extraLabels.Add(label);
	}

	public void Select(int index)
	{
		current = Mathf.Clamp(index, 0, views.Count - 1);
		for (int i = 0; i < views.Count; i++)
		{
			views[i].Root.gameObject.SetActive(i == current);
			foreach (SectionView section in views[i].Sections)
			{
				section.Root.gameObject.SetActive(i == current && ReferenceEquals(section, views[i].Sections[0]));
			}
		}

		Dirty = true;
	}

	public void Collect(List<UiButton> targets)
	{
		Add(targets, tabs);
		Add(targets, extras);
		if (views.Count > 0 && views[current].Sections.Count > 0)
		{
			Add(targets, views[current].Sections[0].Buttons);
		}
	}

	public void Tick(float deltaTime)
	{
		foreach (UiButton tab in tabs)
		{
			tab.Tick(deltaTime);
		}

		foreach (UiButton extra in extras)
		{
			extra.Tick(deltaTime);
		}

		if (views.Count > 0 && views[current].Sections.Count > 0)
		{
			foreach (UiButton button in views[current].Sections[0].Buttons)
			{
				button.Tick(deltaTime);
			}
		}
	}

	public void Refresh(MenuTheme theme)
	{
		Dirty = false;
		foreach (UiButton tab in tabs)
		{
			tab.Refresh(theme);
		}

		foreach (UiButton extra in extras)
		{
			extra.Refresh(theme);
		}

		foreach (UiLabel label in extraLabels)
		{
			label.Refresh(theme);
		}

		if (views.Count == 0 || views[current].Sections.Count == 0)
		{
			return;
		}

		SectionView section = views[current].Sections[0];
		try
		{
			foreach (UiLabel label in section.Labels)
			{
				label.Refresh(theme);
			}

			foreach (UiButton button in section.Buttons)
			{
				button.Refresh(theme);
			}

			foreach (UiBar bar in section.Bars)
			{
				bar.Refresh(theme);
			}
		}
		catch (Exception exception)
		{
			Debug.LogWarning("[AceUtilsPad] Panel refresh failed: " + exception.Message);
		}
	}

	private static void Add(List<UiButton> targets, List<UiButton> candidates)
	{
		foreach (UiButton candidate in candidates)
		{
			if (candidate != null && candidate.CanPress)
			{
				targets.Add(candidate);
			}
		}
	}
}
