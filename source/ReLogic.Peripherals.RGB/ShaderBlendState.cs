namespace ReLogic.Peripherals.RGB;

public struct ShaderBlendState
{
	public readonly float GlobalOpacity;

	public readonly BlendMode Mode;

	public ShaderBlendState(BlendMode blendMode, float alpha = 1f)
	{
		GlobalOpacity = alpha;
		Mode = blendMode;
	}
}
