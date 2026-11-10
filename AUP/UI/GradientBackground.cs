using UnityEngine;
using UnityEngine.UI;

namespace AUP.UI;

public sealed class GradientBackground : BaseMeshEffect
{
	private Color[] palette;
	private float phase;

	public void SetPalette(Color[] colors)
	{
		palette = colors;
		MarkDirty();
	}

	public void SetPhase(float value)
	{
		phase = value;
		MarkDirty();
	}

	public override void ModifyMesh(VertexHelper vertexHelper)
	{
		if (!IsActive() || palette == null || palette.Length < 2)
		{
			return;
		}

		Rect rect = graphic.rectTransform.rect;
		float opacity = graphic.color.a;
		UIVertex vertex = default(UIVertex);
		for (int i = 0; i < vertexHelper.currentVertCount; i++)
		{
			vertexHelper.PopulateUIVertex(ref vertex, i);
			float horizontal = Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
			float vertical = Mathf.InverseLerp(rect.yMax, rect.yMin, vertex.position.y);
			Color sampled = Sample((horizontal * 0.45f + vertical * 0.9f) / 1.35f + phase);
			vertex.color = new Color(sampled.r, sampled.g, sampled.b, opacity);
			vertexHelper.SetUIVertex(vertex, i);
		}
	}

	private void MarkDirty()
	{
		if (graphic != null)
		{
			graphic.SetVerticesDirty();
		}
	}

	private Color Sample(float position)
	{
		float scaled = Mathf.Repeat(position, 1f) * palette.Length;
		int index = Mathf.FloorToInt(scaled) % palette.Length;
		int next = (index + 1) % palette.Length;
		float blend = Mathf.SmoothStep(0f, 1f, scaled - Mathf.Floor(scaled));
		return Color.Lerp(palette[index], palette[next], blend);
	}
}
