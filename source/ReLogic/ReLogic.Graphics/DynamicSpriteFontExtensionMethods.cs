using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReLogic.Graphics;

public static class DynamicSpriteFontExtensionMethods
{
	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, string text, Vector2 position, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Vector2 scale = Vector2.One;
		spriteFont.InternalDraw(text, spriteBatch, position, color, 0f, Vector2.Zero, ref scale, (SpriteEffects)0, 0f);
	}

	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Vector2 scale = Vector2.One;
		spriteFont.InternalDraw(text.ToString(), spriteBatch, position, color, 0f, Vector2.Zero, ref scale, (SpriteEffects)0, 0f);
	}

	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		Vector2 scale2 = default(Vector2);
		scale2.X = scale;
		scale2.Y = scale;
		spriteFont.InternalDraw(text, spriteBatch, position, color, rotation, origin, ref scale2, effects, layerDepth);
	}

	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 scale2 = default(Vector2);
		((Vector2)(ref scale2))._002Ector(scale);
		spriteFont.InternalDraw(text.ToString(), spriteBatch, position, color, rotation, origin, ref scale2, effects, layerDepth);
	}

	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		spriteFont.InternalDraw(text, spriteBatch, position, color, rotation, origin, ref scale, effects, layerDepth);
	}

	public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		spriteFont.InternalDraw(text.ToString(), spriteBatch, position, color, rotation, origin, ref scale, effects, layerDepth);
	}
}
