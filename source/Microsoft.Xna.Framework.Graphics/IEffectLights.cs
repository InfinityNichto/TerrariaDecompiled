namespace Microsoft.Xna.Framework.Graphics;

public interface IEffectLights
{
	DirectionalLight DirectionalLight0 { get; }

	DirectionalLight DirectionalLight1 { get; }

	DirectionalLight DirectionalLight2 { get; }

	Vector3 AmbientLightColor { get; set; }

	bool LightingEnabled { get; set; }

	void EnableDefaultLighting();
}
