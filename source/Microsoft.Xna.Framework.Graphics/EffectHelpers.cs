namespace Microsoft.Xna.Framework.Graphics;

internal static class EffectHelpers
{
	internal static Vector3 EnableDefaultLighting(DirectionalLight light0, DirectionalLight light1, DirectionalLight light2)
	{
		light0.Direction = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f);
		light0.DiffuseColor = new Vector3(1f, 0.9607844f, 0.8078432f);
		light0.SpecularColor = new Vector3(1f, 0.9607844f, 0.8078432f);
		light0.Enabled = true;
		light1.Direction = new Vector3(0.7198464f, 0.3420201f, 0.6040227f);
		light1.DiffuseColor = new Vector3(82f / 85f, 0.7607844f, 0.4078432f);
		light1.SpecularColor = Vector3.Zero;
		light1.Enabled = true;
		light2.Direction = new Vector3(0.4545195f, -0.7660444f, 0.4545195f);
		light2.DiffuseColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
		light2.SpecularColor = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
		light2.Enabled = true;
		return new Vector3(0.05333332f, 0.09882354f, 0.1819608f);
	}

	internal static EffectDirtyFlags SetWorldViewProjAndFog(EffectDirtyFlags dirtyFlags, ref Matrix world, ref Matrix view, ref Matrix projection, ref Matrix worldView, bool fogEnabled, float fogStart, float fogEnd, EffectParameter worldViewProjParam, EffectParameter fogVectorParam)
	{
		if ((dirtyFlags & EffectDirtyFlags.WorldViewProj) != 0)
		{
			Matrix.Multiply(ref world, ref view, out worldView);
			Matrix.Multiply(ref worldView, ref projection, out var result);
			worldViewProjParam.SetValue(result);
			dirtyFlags &= ~EffectDirtyFlags.WorldViewProj;
		}
		if (fogEnabled)
		{
			if ((dirtyFlags & (EffectDirtyFlags.Fog | EffectDirtyFlags.FogEnable)) != 0)
			{
				SetFogVector(ref worldView, fogStart, fogEnd, fogVectorParam);
				dirtyFlags &= ~(EffectDirtyFlags.Fog | EffectDirtyFlags.FogEnable);
			}
		}
		else if ((dirtyFlags & EffectDirtyFlags.FogEnable) != 0)
		{
			fogVectorParam.SetValue(Vector4.Zero);
			dirtyFlags &= ~EffectDirtyFlags.FogEnable;
		}
		return dirtyFlags;
	}

	private static void SetFogVector(ref Matrix worldView, float fogStart, float fogEnd, EffectParameter fogVectorParam)
	{
		if (fogStart == fogEnd)
		{
			fogVectorParam.SetValue(new Vector4(0f, 0f, 0f, 1f));
			return;
		}
		float num = 1f / (fogStart - fogEnd);
		Vector4 value = default(Vector4);
		value.X = worldView.M13 * num;
		value.Y = worldView.M23 * num;
		value.Z = worldView.M33 * num;
		value.W = (worldView.M43 + fogStart) * num;
		fogVectorParam.SetValue(value);
	}

	internal static EffectDirtyFlags SetLightingMatrices(EffectDirtyFlags dirtyFlags, ref Matrix world, ref Matrix view, EffectParameter worldParam, EffectParameter worldInverseTransposeParam, EffectParameter eyePositionParam)
	{
		if ((dirtyFlags & EffectDirtyFlags.World) != 0)
		{
			Matrix.Invert(ref world, out var result);
			Matrix.Transpose(ref result, out var result2);
			worldParam.SetValue(world);
			worldInverseTransposeParam.SetValue(result2);
			dirtyFlags &= ~EffectDirtyFlags.World;
		}
		if ((dirtyFlags & EffectDirtyFlags.EyePosition) != 0)
		{
			Matrix.Invert(ref view, out var result3);
			eyePositionParam.SetValue(result3.Translation);
			dirtyFlags &= ~EffectDirtyFlags.EyePosition;
		}
		return dirtyFlags;
	}

	internal static void SetMaterialColor(bool lightingEnabled, float alpha, ref Vector3 diffuseColor, ref Vector3 emissiveColor, ref Vector3 ambientLightColor, EffectParameter diffuseColorParam, EffectParameter emissiveColorParam)
	{
		if (lightingEnabled)
		{
			Vector4 value = default(Vector4);
			value.X = diffuseColor.X * alpha;
			value.Y = diffuseColor.Y * alpha;
			value.Z = diffuseColor.Z * alpha;
			value.W = alpha;
			Vector3 value2 = default(Vector3);
			value2.X = (emissiveColor.X + ambientLightColor.X * diffuseColor.X) * alpha;
			value2.Y = (emissiveColor.Y + ambientLightColor.Y * diffuseColor.Y) * alpha;
			value2.Z = (emissiveColor.Z + ambientLightColor.Z * diffuseColor.Z) * alpha;
			diffuseColorParam.SetValue(value);
			emissiveColorParam.SetValue(value2);
		}
		else
		{
			Vector4 value3 = default(Vector4);
			value3.X = (diffuseColor.X + emissiveColor.X) * alpha;
			value3.Y = (diffuseColor.Y + emissiveColor.Y) * alpha;
			value3.Z = (diffuseColor.Z + emissiveColor.Z) * alpha;
			value3.W = alpha;
			diffuseColorParam.SetValue(value3);
		}
	}
}
