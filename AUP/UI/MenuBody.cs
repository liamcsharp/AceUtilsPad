using System.Collections.Generic;
using UnityEngine;

namespace AUP.UI;

internal static class MeshFactory
{
	private const int CornerSegments = 7;

	private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

	public static Mesh Slab(float width, float height, float depth, float radius, float bevel, bool backFirst)
	{
		radius = Mathf.Clamp(radius, 0.01f, Mathf.Min(width, height) * 0.5f);
		bevel = Mathf.Clamp(bevel, 0.01f, Mathf.Min(radius * 0.9f, depth * 0.45f));
		string key = width.ToString("0.##") + "x" + height.ToString("0.##") + "x" + depth.ToString("0.##") + "r" + radius.ToString("0.##") + "b" + bevel.ToString("0.##") + (backFirst ? "B" : "F");
		if (Cache.TryGetValue(key, out Mesh cached) && cached != null)
		{
			return cached;
		}

		List<Vector3> vertices = new List<Vector3>();
		List<Color> colors = new List<Color>();
		List<int> triangles = new List<int>();
		Vector2[] inner = Outline(width - 2f * bevel, height - 2f * bevel, radius - bevel);
		Vector2[] outer = Outline(width, height, radius);
		float halfHeight = height * 0.5f;

		if (backFirst)
		{
			Cap(vertices, colors, triangles, inner, depth, 0.6f, halfHeight, false);
			Band(vertices, colors, triangles, outer, depth - bevel, inner, depth, 0.5f, halfHeight);
			Band(vertices, colors, triangles, outer, bevel, outer, depth - bevel, 0.68f, halfHeight);
			Band(vertices, colors, triangles, inner, 0f, outer, bevel, 1f, halfHeight);
			Cap(vertices, colors, triangles, inner, 0f, 0.84f, halfHeight, true);
		}
		else
		{
			Cap(vertices, colors, triangles, inner, 0f, 0.84f, halfHeight, true);
			Band(vertices, colors, triangles, inner, 0f, outer, bevel, 1f, halfHeight);
			Band(vertices, colors, triangles, outer, bevel, outer, depth - bevel, 0.68f, halfHeight);
			Band(vertices, colors, triangles, outer, depth - bevel, inner, depth, 0.5f, halfHeight);
			Cap(vertices, colors, triangles, inner, depth, 0.78f, halfHeight, false);
		}

		Mesh mesh = new Mesh { name = "AUPSlab_" + key };
		mesh.SetVertices(vertices);
		mesh.SetColors(colors);
		mesh.SetTriangles(triangles, 0);
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		Cache[key] = mesh;
		return mesh;
	}

	private static Vector2[] Outline(float width, float height, float radius)
	{
		radius = Mathf.Max(0.01f, Mathf.Min(radius, Mathf.Min(width, height) * 0.5f));
		float halfWidth = width * 0.5f - radius;
		float halfHeight = height * 0.5f - radius;
		Vector2[] centers =
		{
			new Vector2(halfWidth, halfHeight), new Vector2(-halfWidth, halfHeight), new Vector2(-halfWidth, -halfHeight), new Vector2(halfWidth, -halfHeight)
		};

		Vector2[] points = new Vector2[4 * (CornerSegments + 1)];
		int index = 0;
		for (int corner = 0; corner < 4; corner++)
		{
			for (int step = 0; step <= CornerSegments; step++)
			{
				float angle = (corner + step / (float)CornerSegments) * Mathf.PI * 0.5f;
				points[index++] = centers[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			}
		}

		return points;
	}

	private static Color Shade(float shade, float y, float halfHeight)
	{
		float vertical = Mathf.Lerp(0.86f, 1.06f, Mathf.InverseLerp(-halfHeight, halfHeight, y));
		float value = Mathf.Clamp01(shade * vertical);
		return new Color(value, value, value, 1f);
	}

	private static void Cap(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector2[] ring, float z, float shade, float halfHeight, bool front)
	{
		int center = vertices.Count;
		vertices.Add(new Vector3(0f, 0f, z));
		colors.Add(Shade(shade, 0f, halfHeight));
		for (int i = 0; i < ring.Length; i++)
		{
			vertices.Add(new Vector3(ring[i].x, ring[i].y, z));
			colors.Add(Shade(shade, ring[i].y, halfHeight));
		}

		for (int i = 0; i < ring.Length; i++)
		{
			int current = center + 1 + i;
			int next = center + 1 + (i + 1) % ring.Length;
			triangles.Add(center);
			triangles.Add(front ? next : current);
			triangles.Add(front ? current : next);
		}
	}

	private static void Band(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector2[] near, float nearZ, Vector2[] far, float farZ,
		float shade, float halfHeight)
	{
		int start = vertices.Count;
		int count = near.Length;
		for (int i = 0; i < count; i++)
		{
			vertices.Add(new Vector3(near[i].x, near[i].y, nearZ));
			colors.Add(Shade(shade, near[i].y, halfHeight));
		}

		for (int i = 0; i < count; i++)
		{
			vertices.Add(new Vector3(far[i].x, far[i].y, farZ));
			colors.Add(Shade(shade * 0.94f, far[i].y, halfHeight));
		}

		for (int i = 0; i < count; i++)
		{
			int next = (i + 1) % count;
			int a = start + i;
			int aNext = start + next;
			int b = start + count + i;
			int bNext = start + count + next;
			triangles.Add(a);
			triangles.Add(aNext);
			triangles.Add(bNext);
			triangles.Add(a);
			triangles.Add(bNext);
			triangles.Add(b);
		}
	}
}

internal sealed class MenuBody
{
	private readonly List<Part> parts = new List<Part>();
	private bool behind;

	public MenuBody(Transform parent, string name)
	{
		Root = new GameObject(name).transform;
		Root.SetParent(parent, false);
	}

	public Transform Root { get; }

	public void Add(Vector3 center, float width, float height, float depth, float radius, float bevel, int tone)
	{
		GameObject part = new GameObject("Part");
		part.transform.SetParent(Root, false);
		part.transform.localPosition = new Vector3(center.x, center.y, center.z - depth * 0.5f);
		MeshFilter filter = part.AddComponent<MeshFilter>();
		Mesh frontView = MeshFactory.Slab(width, height, depth, radius, bevel, true);
		Mesh backView = MeshFactory.Slab(width, height, depth, radius, bevel, false);
		filter.sharedMesh = behind ? backView : frontView;
		MeshRenderer renderer = part.AddComponent<MeshRenderer>();
		renderer.sortingOrder = behind ? 1 : -1;
		Material material = Materials.CreateUnlit(Color.white);
		renderer.sharedMaterial = material;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		parts.Add(new Part { Material = material, Tone = tone, Filter = filter, Renderer = renderer, FrontView = frontView, BackView = backView });
	}

	public bool UpdateFacing(Transform plane)
	{
		Camera camera = PhysicsLayers.ViewCamera();
		if (camera == null || plane == null)
		{
			return behind;
		}

		SetBehind(Vector3.Dot(camera.transform.position - plane.position, plane.forward) > 0f);
		return behind;
	}

	public void SetBehind(bool viewedFromBehind)
	{
		if (behind == viewedFromBehind)
		{
			return;
		}

		behind = viewedFromBehind;
		foreach (Part part in parts)
		{
			part.Filter.sharedMesh = behind ? part.BackView : part.FrontView;
			part.Renderer.sortingOrder = behind ? 1 : -1;
		}
	}

	public void SetVisible(bool visible)
	{
		if (Root != null && Root.gameObject.activeSelf != visible)
		{
			Root.gameObject.SetActive(visible);
		}
	}

	public void Tint(Color body, Color accent)
	{
		foreach (Part part in parts)
		{
			Color color;
			switch (part.Tone)
			{
				case 1:
					color = accent;
					break;
				case 2:
					color = Color.Lerp(body, Color.black, 0.55f);
					break;
				case 3:
					color = Color.Lerp(body, Color.white, 0.22f);
					break;
				default:
					color = body;
					break;
			}

			color.a = 1f;
			if (part.Material != null && part.Material.color != color)
			{
				part.Material.color = color;
			}
		}
	}

	public void Destroy()
	{
		foreach (Part part in parts)
		{
			if (part.Material != null)
			{
				Object.Destroy(part.Material);
			}
		}

		parts.Clear();
	}

	private sealed class Part
	{
		public Material Material;
		public int Tone;
		public MeshFilter Filter;
		public MeshRenderer Renderer;
		public Mesh FrontView;
		public Mesh BackView;
	}
}
