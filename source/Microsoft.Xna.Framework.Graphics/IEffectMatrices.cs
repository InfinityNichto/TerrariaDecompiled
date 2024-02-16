namespace Microsoft.Xna.Framework.Graphics;

public interface IEffectMatrices
{
	Matrix World { get; set; }

	Matrix View { get; set; }

	Matrix Projection { get; set; }
}
