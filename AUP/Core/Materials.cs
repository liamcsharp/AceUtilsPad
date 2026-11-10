using UnityEngine;

namespace AUP;

internal static class Materials
{
	private static readonly string[] ShaderNames = { "Sprites/Default", "UI/Default", "Unlit/Color", "Hidden/Internal-Colored" };

	private static readonly string[] LitShaderNames =
	{
		"Standard", "Universal Render Pipeline/Lit", "Legacy Shaders/Diffuse", "Mobile/Diffuse", "Diffuse"
	};

	private static bool litChecked;
	private static Shader litShader;

	public static Material CreateLit(Color color)
	{
		if (!litChecked)
		{
			litChecked = true;
			foreach (string shaderName in LitShaderNames)
			{
				litShader = Shader.Find(shaderName);
				if (litShader != null)
				{
					break;
				}
			}
		}

		if (litShader == null)
		{
			return CreateUnlit(color);
		}

		Material material = new Material(litShader) { color = color };
		if (color.a < 0.99f && material.HasProperty("_Mode"))
		{
			material.SetFloat("_Mode", 2f);
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.EnableKeyword("_ALPHABLEND_ON");
			material.renderQueue = 3000;
		}

		return material;
	}

	public static Material CreateUnlit(Color color)
	{
		foreach (string shaderName in ShaderNames)
		{
			Shader shader = Shader.Find(shaderName);
			if (shader != null)
			{
				return new Material(shader) { color = color };
			}
		}

		Debug.LogWarning("[AceUtilsPad] No unlit shader was found in this build. Pointer and wind visuals may not render.");
		return new Material(Shader.Find("Standard")) { color = color };
	}
}
