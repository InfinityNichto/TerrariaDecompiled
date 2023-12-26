namespace ReLogic.Peripherals.RGB;

internal struct ShaderOperation
{
	public readonly ShaderBlendState BlendState;

	private readonly ChromaShader _shader;

	private readonly EffectDetailLevel _detailLevel;

	public ShaderOperation(ChromaShader shader, ShaderBlendState blendState, EffectDetailLevel detailLevel)
	{
		BlendState = blendState;
		_shader = shader;
		_detailLevel = detailLevel;
	}

	public void Process(RgbDevice device, Fragment fragment, float time)
	{
		_shader.Process(device, fragment, _detailLevel, time);
	}

	public ShaderOperation WithBlendState(ShaderBlendState blendState)
	{
		return new ShaderOperation(_shader, blendState, _detailLevel);
	}
}
