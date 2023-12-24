using Microsoft.Xna.Framework;

namespace ReLogic.Graphics;

public interface IDebugDrawer
{
	void DrawSquare(Vector4 positionAndSize, Color color);

	void DrawSquareFromCenter(Vector2 center, Vector2 size, float rotation, Color color);
}
