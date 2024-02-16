using System;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

public class EnvironmentMapEffect : Effect, IEffectMatrices, IEffectLights, IEffectFog
{
	private EffectParameter textureParam;

	private EffectParameter environmentMapParam;

	private EffectParameter environmentMapAmountParam;

	private EffectParameter environmentMapSpecularParam;

	private EffectParameter fresnelFactorParam;

	private EffectParameter diffuseColorParam;

	private EffectParameter emissiveColorParam;

	private EffectParameter eyePositionParam;

	private EffectParameter fogColorParam;

	private EffectParameter fogVectorParam;

	private EffectParameter worldParam;

	private EffectParameter worldInverseTransposeParam;

	private EffectParameter worldViewProjParam;

	private EffectParameter shaderIndexParam;

	private bool oneLight;

	private bool fogEnabled;

	private bool fresnelEnabled;

	private bool specularEnabled;

	private Matrix world = Matrix.Identity;

	private Matrix view = Matrix.Identity;

	private Matrix projection = Matrix.Identity;

	private Matrix worldView;

	private Vector3 diffuseColor = Vector3.One;

	private Vector3 emissiveColor = Vector3.Zero;

	private Vector3 ambientLightColor = Vector3.Zero;

	private float alpha = 1f;

	private DirectionalLight light0;

	private DirectionalLight light1;

	private DirectionalLight light2;

	private float fogStart;

	private float fogEnd = 1f;

	private EffectDirtyFlags dirtyFlags = EffectDirtyFlags.All;

	public Matrix World
	{
		get
		{
			return world;
		}
		set
		{
			world = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.World | EffectDirtyFlags.Fog;
		}
	}

	public Matrix View
	{
		get
		{
			return view;
		}
		set
		{
			view = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.EyePosition | EffectDirtyFlags.Fog;
		}
	}

	public Matrix Projection
	{
		get
		{
			return projection;
		}
		set
		{
			projection = value;
			dirtyFlags |= EffectDirtyFlags.WorldViewProj;
		}
	}

	public Vector3 DiffuseColor
	{
		get
		{
			return diffuseColor;
		}
		set
		{
			diffuseColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public Vector3 EmissiveColor
	{
		get
		{
			return emissiveColor;
		}
		set
		{
			emissiveColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public float Alpha
	{
		get
		{
			return alpha;
		}
		set
		{
			alpha = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public Vector3 AmbientLightColor
	{
		get
		{
			return ambientLightColor;
		}
		set
		{
			ambientLightColor = value;
			dirtyFlags |= EffectDirtyFlags.MaterialColor;
		}
	}

	public DirectionalLight DirectionalLight0 => light0;

	public DirectionalLight DirectionalLight1 => light1;

	public DirectionalLight DirectionalLight2 => light2;

	public bool FogEnabled
	{
		get
		{
			return fogEnabled;
		}
		set
		{
			if (fogEnabled != value)
			{
				fogEnabled = value;
				dirtyFlags |= EffectDirtyFlags.FogEnable | EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public float FogStart
	{
		get
		{
			return fogStart;
		}
		set
		{
			fogStart = value;
			dirtyFlags |= EffectDirtyFlags.Fog;
		}
	}

	public float FogEnd
	{
		get
		{
			return fogEnd;
		}
		set
		{
			fogEnd = value;
			dirtyFlags |= EffectDirtyFlags.Fog;
		}
	}

	public Vector3 FogColor
	{
		get
		{
			return fogColorParam.GetValueVector3();
		}
		set
		{
			fogColorParam.SetValue(value);
		}
	}

	public Texture2D Texture
	{
		get
		{
			return textureParam.GetValueTexture2D();
		}
		set
		{
			textureParam.SetValue(value);
		}
	}

	public TextureCube EnvironmentMap
	{
		get
		{
			return environmentMapParam.GetValueTextureCube();
		}
		set
		{
			environmentMapParam.SetValue(value);
		}
	}

	public float EnvironmentMapAmount
	{
		get
		{
			return environmentMapAmountParam.GetValueSingle();
		}
		set
		{
			environmentMapAmountParam.SetValue(value);
		}
	}

	public Vector3 EnvironmentMapSpecular
	{
		get
		{
			return environmentMapSpecularParam.GetValueVector3();
		}
		set
		{
			environmentMapSpecularParam.SetValue(value);
			bool flag = value != Vector3.Zero;
			if (specularEnabled != flag)
			{
				specularEnabled = flag;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public float FresnelFactor
	{
		get
		{
			return fresnelFactorParam.GetValueSingle();
		}
		set
		{
			fresnelFactorParam.SetValue(value);
			bool flag = value != 0f;
			if (fresnelEnabled != flag)
			{
				fresnelEnabled = flag;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	bool IEffectLights.LightingEnabled
	{
		get
		{
			return true;
		}
		set
		{
			if (!value)
			{
				throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.CantDisableLighting, new object[1] { typeof(EnvironmentMapEffect).Name }));
			}
		}
	}

	public EnvironmentMapEffect(GraphicsDevice device)
		: base(device, EnvironmentMapEffectCode.Code)
	{
		CacheEffectParameters(null);
		DirectionalLight0.Enabled = true;
		EnvironmentMapAmount = 1f;
		EnvironmentMapSpecular = Vector3.Zero;
		FresnelFactor = 1f;
	}

	protected EnvironmentMapEffect(EnvironmentMapEffect cloneSource)
		: base(cloneSource)
	{
		CacheEffectParameters(cloneSource);
		fogEnabled = cloneSource.fogEnabled;
		fresnelEnabled = cloneSource.fresnelEnabled;
		specularEnabled = cloneSource.specularEnabled;
		world = cloneSource.world;
		view = cloneSource.view;
		projection = cloneSource.projection;
		diffuseColor = cloneSource.diffuseColor;
		emissiveColor = cloneSource.emissiveColor;
		ambientLightColor = cloneSource.ambientLightColor;
		alpha = cloneSource.alpha;
		fogStart = cloneSource.fogStart;
		fogEnd = cloneSource.fogEnd;
	}

	public override Effect Clone()
	{
		return new EnvironmentMapEffect(this);
	}

	public void EnableDefaultLighting()
	{
		AmbientLightColor = EffectHelpers.EnableDefaultLighting(light0, light1, light2);
	}

	private void CacheEffectParameters(EnvironmentMapEffect cloneSource)
	{
		textureParam = base.Parameters["Texture"];
		environmentMapParam = base.Parameters["EnvironmentMap"];
		environmentMapAmountParam = base.Parameters["EnvironmentMapAmount"];
		environmentMapSpecularParam = base.Parameters["EnvironmentMapSpecular"];
		fresnelFactorParam = base.Parameters["FresnelFactor"];
		diffuseColorParam = base.Parameters["DiffuseColor"];
		emissiveColorParam = base.Parameters["EmissiveColor"];
		eyePositionParam = base.Parameters["EyePosition"];
		fogColorParam = base.Parameters["FogColor"];
		fogVectorParam = base.Parameters["FogVector"];
		worldParam = base.Parameters["World"];
		worldInverseTransposeParam = base.Parameters["WorldInverseTranspose"];
		worldViewProjParam = base.Parameters["WorldViewProj"];
		shaderIndexParam = base.Parameters["ShaderIndex"];
		light0 = new DirectionalLight(base.Parameters["DirLight0Direction"], base.Parameters["DirLight0DiffuseColor"], null, cloneSource?.light0);
		light1 = new DirectionalLight(base.Parameters["DirLight1Direction"], base.Parameters["DirLight1DiffuseColor"], null, cloneSource?.light1);
		light2 = new DirectionalLight(base.Parameters["DirLight2Direction"], base.Parameters["DirLight2DiffuseColor"], null, cloneSource?.light2);
	}

	internal override bool WantParameter(EffectParameter parameter)
	{
		if (parameter.Name != "VSIndices")
		{
			return parameter.Name != "PSIndices";
		}
		return false;
	}

	protected internal override void OnApply()
	{
		dirtyFlags = EffectHelpers.SetWorldViewProjAndFog(dirtyFlags, ref world, ref view, ref projection, ref worldView, fogEnabled, fogStart, fogEnd, worldViewProjParam, fogVectorParam);
		dirtyFlags = EffectHelpers.SetLightingMatrices(dirtyFlags, ref world, ref view, worldParam, worldInverseTransposeParam, eyePositionParam);
		if ((dirtyFlags & EffectDirtyFlags.MaterialColor) != 0)
		{
			EffectHelpers.SetMaterialColor(lightingEnabled: true, alpha, ref diffuseColor, ref emissiveColor, ref ambientLightColor, diffuseColorParam, emissiveColorParam);
			dirtyFlags &= ~EffectDirtyFlags.MaterialColor;
		}
		bool flag = !light1.Enabled && !light2.Enabled;
		if (oneLight != flag)
		{
			oneLight = flag;
			dirtyFlags |= EffectDirtyFlags.ShaderIndex;
		}
		if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
		{
			int num = 0;
			if (!fogEnabled)
			{
				num++;
			}
			if (fresnelEnabled)
			{
				num += 2;
			}
			if (specularEnabled)
			{
				num += 4;
			}
			if (oneLight)
			{
				num += 8;
			}
			shaderIndexParam.SetValue(num);
			dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;
		}
	}
}
