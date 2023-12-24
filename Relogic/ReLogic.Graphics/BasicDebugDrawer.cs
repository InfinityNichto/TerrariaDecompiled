using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReLogic.Graphics;

public class BasicDebugDrawer : IDebugDrawer, IDisposable
{
	private SpriteBatch _spriteBatch;

	private Texture2D _texture;

	private bool _disposedValue;

	public BasicDebugDrawer(GraphicsDevice graphicsDevice)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		_spriteBatch = new SpriteBatch(graphicsDevice);
		_texture = new Texture2D(graphicsDevice, 4, 4);
		Color[] array = (Color[])(object)new Color[16];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Color.White;
		}
		_texture.SetData<Color>(array);
	}

	public void Begin(Matrix matrix)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		_spriteBatch.Begin((SpriteSortMode)0, (BlendState)null, (SamplerState)null, (DepthStencilState)null, (RasterizerState)null, (Effect)null, matrix);
	}

	public void Begin()
	{
		_spriteBatch.Begin();
	}

	public void DrawSquare(Vector4 positionAndSize, Color color)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		_spriteBatch.Draw(_texture, new Vector2(positionAndSize.X, positionAndSize.Y), (Rectangle?)null, color, 0f, Vector2.Zero, new Vector2(positionAndSize.Z, positionAndSize.W) / 4f, (SpriteEffects)0, 1f);
	}

	public void DrawSquare(Vector2 position, Vector2 size, Color color)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		_spriteBatch.Draw(_texture, position, (Rectangle?)null, color, 0f, Vector2.Zero, size / 4f, (SpriteEffects)0, 1f);
	}

	public void DrawSquareFromCenter(Vector2 center, Vector2 size, float rotation, Color color)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		_spriteBatch.Draw(_texture, center, (Rectangle?)null, color, rotation, new Vector2(2f, 2f), size / 4f, (SpriteEffects)0, 1f);
	}

	public void DrawLine(Vector2 start, Vector2 end, float width, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		Vector2 vector = end - start;
		float rotation = (float)Math.Atan2(vector.Y, vector.X);
		Vector2 vector2 = default(Vector2);
		((Vector2)(ref vector2))._002Ector(((Vector2)(ref vector)).Length(), width);
		_spriteBatch.Draw(_texture, start, (Rectangle?)null, color, rotation, new Vector2(0f, 2f), vector2 / 4f, (SpriteEffects)0, 1f);
	}

	public void End()
	{
		_spriteBatch.End();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_disposedValue)
		{
			return;
		}
		if (disposing)
		{
			if (_spriteBatch != null)
			{
				((GraphicsResource)_spriteBatch).Dispose();
				_spriteBatch = null;
			}
			if (_texture != null)
			{
				((GraphicsResource)_texture).Dispose();
				_texture = null;
			}
		}
		_disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
