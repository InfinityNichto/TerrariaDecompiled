namespace Microsoft.Xna.Framework.Graphics;

public class AlphaTestEffect : Effect, IEffectMatrices, IEffectFog
{
	private EffectParameter textureParam;

	private EffectParameter diffuseColorParam;

	private EffectParameter alphaTestParam;

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

	private CompareFunction alphaFunction = CompareFunction.Greater;

	private int referenceAlpha;

	private bool isEqNe;

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

	public CompareFunction AlphaFunction
	{
		get
		{
			return alphaFunction;
		}
		set
		{
			alphaFunction = value;
			dirtyFlags |= EffectDirtyFlags.AlphaTest;
		}
	}

	public int ReferenceAlpha
	{
		get
		{
			return referenceAlpha;
		}
		set
		{
			referenceAlpha = value;
			dirtyFlags |= EffectDirtyFlags.AlphaTest;
		}
	}

	public AlphaTestEffect(GraphicsDevice device)
		: base(device, AlphaTestEffectCode.Code)
	{
		CacheEffectParameters();
	}

	protected AlphaTestEffect(AlphaTestEffect cloneSource)
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
		alphaFunction = cloneSource.alphaFunction;
		referenceAlpha = cloneSource.referenceAlpha;
	}

	public override Effect Clone()
	{
		return new AlphaTestEffect(this);
	}

	private void CacheEffectParameters()
	{
		textureParam = base.Parameters["Texture"];
		diffuseColorParam = base.Parameters["DiffuseColor"];
		alphaTestParam = base.Parameters["AlphaTest"];
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
		if ((dirtyFlags & EffectDirtyFlags.AlphaTest) != 0)
		{
			Vector4 value = default(Vector4);
			bool flag = false;
			float num = (float)referenceAlpha / 255f;
			switch (alphaFunction)
			{
			case CompareFunction.Less:
				value.X = num - 0.0019607844f;
				value.Z = 1f;
				value.W = -1f;
				break;
			case CompareFunction.LessEqual:
				value.X = num + 0.0019607844f;
				value.Z = 1f;
				value.W = -1f;
				break;
			case CompareFunction.GreaterEqual:
				value.X = num - 0.0019607844f;
				value.Z = -1f;
				value.W = 1f;
				break;
			case CompareFunction.Greater:
				value.X = num + 0.0019607844f;
				value.Z = -1f;
				value.W = 1f;
				break;
			case CompareFunction.Equal:
				value.X = num;
				value.Y = 0.0019607844f;
				value.Z = 1f;
				value.W = -1f;
				flag = true;
				break;
			case CompareFunction.NotEqual:
				value.X = num;
				value.Y = 0.0019607844f;
				value.Z = -1f;
				value.W = 1f;
				flag = true;
				break;
			case CompareFunction.Never:
				value.Z = -1f;
				value.W = -1f;
				break;
			default:
				value.Z = 1f;
				value.W = 1f;
				break;
			}
			alphaTestParam.SetValue(value);
			dirtyFlags &= ~EffectDirtyFlags.AlphaTest;
			if (isEqNe != flag)
			{
				isEqNe = flag;
				dirtyFlags |= EffectDirtyFlags.ShaderIndex;
			}
		}
		if ((dirtyFlags & EffectDirtyFlags.ShaderIndex) != 0)
		{
			int num2 = 0;
			if (!fogEnabled)
			{
				num2++;
			}
			if (vertexColorEnabled)
			{
				num2 += 2;
			}
			if (isEqNe)
			{
				num2 += 4;
			}
			shaderIndexParam.SetValue(num2);
			dirtyFlags &= ~EffectDirtyFlags.ShaderIndex;
		}
	}
}
