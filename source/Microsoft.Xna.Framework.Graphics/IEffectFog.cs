namespace Microsoft.Xna.Framework.Graphics;

public interface IEffectFog
{
	bool FogEnabled { get; set; }

	float FogStart { get; set; }

	float FogEnd { get; set; }

	Vector3 FogColor { get; set; }
}
