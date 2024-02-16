namespace Microsoft.Xna.Framework.Graphics;

public class DualTextureEffect : Effect, IEffectMatrices, IEffectFog
{
	private EffectParameter textureParam;

	private EffectParameter texture2Param;

	private EffectParameter diffuseColorParam;

	private EffectParameter fogColorParam;

	private EffectParameter fogVectorParam;

	private EffectParameter worldViewProjParam;

	private EffectParameter shaderIndexParam;

	private bool fogEnabled;

	private bool vertexColorEnabled;

	private Matrix world = Matrix.Identity;

	private Matrix view = Matrix.Identity;

	private Matrix projection = Matrix.Identity;

	private Matrix worldView;

	private Vector3 diffuseColor = Vector3.One;

	private float alpha = 1f;

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
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.Fog;
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
			dirtyFlags |= EffectDirtyFlags.WorldViewProj | EffectDirtyFlags.Fog;
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

	public Texture2D Texture2
	{
		get
		{
			return texture2Param.GetValueTexture2D();
		}
		set
		{
			texture2Param.SetValue(value);
		}
	}

	public bool VertexColorEnabled
	{
		get
		{
			return vertexColorEnabled;
		}
		set
		{
			if (vertexColorEnabled != value)
			{
				vertexColorEnabled = value;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
	}

	public DualTextureEffect(GraphicsDevice device)
		: base(device, DualTextureEffectCode.Code)
	{
		CacheEffectParameters();
	}

	protected DualTextureEffect(DualTextureEffect cloneSource)
		: base(cloneSource)
	{
		CacheEffectParameters();
		fogEnabled = cloneSource.fogEnabled;
		vertexColorEnabled = cloneSource.vertexColorEnabled;
		world = cloneSource.world;
		view = cloneSource.view;
		projection = cloneSource.projection;
		diffuseColor = cloneSource.diffuseColor;
		alpha = cloneSource.alpha;
		fogStart = cloneSource.fogStart;
		fogEnd = cloneSource.fogEnd;
	}

	public override Effect Clone()
	{
		return new DualTextureEffect(this);
	}

	private void CacheEffectParameters()
	{
		textureParam = base.Parameters["Texture"];
		texture2Param = base.Parameters["Texture2"];
		diffuseColorParam = base.Parameters["DiffuseColor"];
		fogColorParam = base.Parameters["FogColor"];
		fogVectorParam = base.Parameters["FogVector"];
		worldViewProjParam = base.Parameters["WorldViewProj"];
		shaderIndexParam = base.Parameters["ShaderIndex"];
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
		if ((dirtyFlags & EffectDirtyFlags.MaterialColor) != 0)
		{
			diffuseColorParam.SetValue(new Vector4(diffuseColor * alpha, alpha));
			dirtyFlags &= ~EffectDirtyFlags.MaterialColor;
		}
		if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
		{
			int num = 0;
			if (!fogEnabled)
			{
				num++;
			}
			if (vertexColorEnabled)
			{
				num += 2;
			}
			shaderIndexParam.SetValue(num);
			dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;
		}
	}
}
