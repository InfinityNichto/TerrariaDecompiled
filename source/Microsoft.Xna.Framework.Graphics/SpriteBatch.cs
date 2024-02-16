using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

public class SpriteBatch : GraphicsResource
{
	private struct SpriteInfo
	{
		public Vector4 Source;

		public Vector4 Destination;

		public Vector2 Origin;

		public float Rotation;

		public float Depth;

		public SpriteEffects Effects;

		public Color Color;
	}

	private class TextureComparer : IComparer<int>
	{
		private SpriteBatch parent;

		public TextureComparer(SpriteBatch parent)
		{
			this.parent = parent;
		}

		public int Compare(int x, int y)
		{
			Texture texture = parent.spriteTextures[x];
			Texture other = parent.spriteTextures[y];
			return texture.CompareTo(other);
		}
	}

	private class BackToFrontComparer : IComparer<int>
	{
		private SpriteBatch parent;

		public BackToFrontComparer(SpriteBatch parent)
		{
			this.parent = parent;
		}

		public int Compare(int x, int y)
		{
			float depth = parent.spriteQueue[x].Depth;
			float depth2 = parent.spriteQueue[y].Depth;
			if (depth > depth2)
			{
				return -1;
			}
			if (depth < depth2)
			{
				return 1;
			}
			return 0;
		}
	}

	private class FrontToBackComparer : IComparer<int>
	{
		private SpriteBatch parent;

		public FrontToBackComparer(SpriteBatch parent)
		{
			this.parent = parent;
		}

		public int Compare(int x, int y)
		{
			float depth = parent.spriteQueue[x].Depth;
			float depth2 = parent.spriteQueue[y].Depth;
			if (depth > depth2)
			{
				return 1;
			}
			if (depth < depth2)
			{
				return -1;
			}
			return 0;
		}
	}

	private const int MaxBatchSize = 2048;

	private DynamicVertexBuffer vertexBuffer;

	private DynamicIndexBuffer indexBuffer;

	private VertexPositionColorTexture[] outputVertices = new VertexPositionColorTexture[8192];

	private int vertexBufferPosition;

	private static readonly float[] xCornerOffsets = new float[4] { 0f, 1f, 1f, 0f };

	private static readonly float[] yCornerOffsets = new float[4] { 0f, 0f, 1f, 1f };

	private Effect spriteEffect;

	private EffectParameter effectMatrixTransform;

	private SpriteSortMode spriteSortMode;

	private BlendState blendState;

	private DepthStencilState depthStencilState;

	private RasterizerState rasterizerState;

	private SamplerState samplerState;

	private Effect customEffect;

	private Matrix transformMatrix;

	private bool inBeginEndPair;

	private SpriteInfo[] spriteQueue = new SpriteInfo[2048];

	private int spriteQueueCount;

	private Texture2D[] spriteTextures;

	private int[] sortIndices;

	private SpriteInfo[] sortedSprites;

	private TextureComparer textureComparer;

	private BackToFrontComparer backToFrontComparer;

	private FrontToBackComparer frontToBackComparer;

	private static Vector2 vector2Zero = Vector2.Zero;

	private static Rectangle? nullRectangle = null;

	private void ConstructPlatformData()
	{
		AllocateBuffers();
	}

	private void DisposePlatformData()
	{
		if (vertexBuffer != null)
		{
			vertexBuffer.Dispose();
		}
		if (indexBuffer != null)
		{
			indexBuffer.Dispose();
		}
	}

	private void AllocateBuffers()
	{
		if (vertexBuffer == null || vertexBuffer.IsDisposed)
		{
			vertexBuffer = new DynamicVertexBuffer(_parent, typeof(VertexPositionColorTexture), 8192, BufferUsage.WriteOnly);
			vertexBufferPosition = 0;
			vertexBuffer.ContentLost += delegate
			{
				vertexBufferPosition = 0;
			};
		}
		if (indexBuffer == null || indexBuffer.IsDisposed)
		{
			indexBuffer = new DynamicIndexBuffer(_parent, typeof(short), 12288, BufferUsage.WriteOnly);
			indexBuffer.SetData(CreateIndexData());
			indexBuffer.ContentLost += delegate
			{
				indexBuffer.SetData(CreateIndexData());
			};
		}
	}

	private static short[] CreateIndexData()
	{
		short[] array = new short[12288];
		for (int i = 0; i < 2048; i++)
		{
			array[i * 6] = (short)(i * 4);
			array[i * 6 + 1] = (short)(i * 4 + 1);
			array[i * 6 + 2] = (short)(i * 4 + 2);
			array[i * 6 + 3] = (short)(i * 4);
			array[i * 6 + 4] = (short)(i * 4 + 2);
			array[i * 6 + 5] = (short)(i * 4 + 3);
		}
		return array;
	}

	private void SetPlatformRenderState()
	{
		AllocateBuffers();
		_parent.SetVertexBuffer(vertexBuffer);
		_parent.Indices = indexBuffer;
	}

	private unsafe void PlatformRenderBatch(Texture2D texture, SpriteInfo[] sprites, int offset, int count)
	{
		float num = 1f / (float)texture.Width;
		float num2 = 1f / (float)texture.Height;
		while (count > 0)
		{
			SetDataOptions options = SetDataOptions.NoOverwrite;
			int num3 = count;
			if (num3 > 2048 - vertexBufferPosition)
			{
				num3 = 2048 - vertexBufferPosition;
				if (num3 < 256)
				{
					vertexBufferPosition = 0;
					options = SetDataOptions.Discard;
					num3 = count;
					if (num3 > 2048)
					{
						num3 = 2048;
					}
				}
			}
			fixed (SpriteInfo* ptr = &sprites[offset])
			{
				fixed (VertexPositionColorTexture* ptr3 = &outputVertices[0])
				{
					SpriteInfo* ptr2 = ptr;
					VertexPositionColorTexture* ptr4 = ptr3;
					for (int i = 0; i < num3; i++)
					{
						float num4;
						float num5;
						if (ptr2->Rotation != 0f)
						{
							num4 = (float)Math.Cos(ptr2->Rotation);
							num5 = (float)Math.Sin(ptr2->Rotation);
						}
						else
						{
							num4 = 1f;
							num5 = 0f;
						}
						float num6 = ((ptr2->Source.Z != 0f) ? (ptr2->Origin.X / ptr2->Source.Z) : (ptr2->Origin.X * 2E+32f));
						float num7 = ((ptr2->Source.W != 0f) ? (ptr2->Origin.Y / ptr2->Source.W) : (ptr2->Origin.Y * 2E+32f));
						for (int j = 0; j < 4; j++)
						{
							float num8 = xCornerOffsets[j];
							float num9 = yCornerOffsets[j];
							float num10 = (num8 - num6) * ptr2->Destination.Z;
							float num11 = (num9 - num7) * ptr2->Destination.W;
							float x = ptr2->Destination.X + num10 * num4 - num11 * num5;
							float y = ptr2->Destination.Y + num10 * num5 + num11 * num4;
							if ((ptr2->Effects & SpriteEffects.FlipHorizontally) != 0)
							{
								num8 = 1f - num8;
							}
							if ((ptr2->Effects & SpriteEffects.FlipVertically) != 0)
							{
								num9 = 1f - num9;
							}
							ptr4->Position.X = x;
							ptr4->Position.Y = y;
							ptr4->Position.Z = ptr2->Depth;
							ptr4->Color = ptr2->Color;
							ptr4->TextureCoordinate.X = (ptr2->Source.X + num8 * ptr2->Source.Z) * num;
							ptr4->TextureCoordinate.Y = (ptr2->Source.Y + num9 * ptr2->Source.W) * num2;
							ptr4++;
						}
						ptr2++;
					}
				}
			}
			int num12 = sizeof(VertexPositionColorTexture);
			int offsetInBytes = vertexBufferPosition * num12 * 4;
			vertexBuffer.SetData(offsetInBytes, outputVertices, 0, num3 * 4, num12, options);
			int minVertexIndex = vertexBufferPosition * 4;
			int numVertices = num3 * 4;
			int startIndex = vertexBufferPosition * 6;
			int primitiveCount = num3 * 2;
			_parent.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, minVertexIndex, numVertices, startIndex, primitiveCount);
			vertexBufferPosition += num3;
			offset += num3;
			count -= num3;
		}
	}

	public SpriteBatch(GraphicsDevice graphicsDevice)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		_parent = graphicsDevice;
		spriteEffect = new Effect(graphicsDevice, SpriteEffectCode.Code);
		effectMatrixTransform = spriteEffect.Parameters["MatrixTransform"];
		ConstructPlatformData();
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && !base.IsDisposed)
			{
				if (spriteEffect != null)
				{
					spriteEffect.Dispose();
				}
				DisposePlatformData();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public void Begin()
	{
		Begin(SpriteSortMode.Deferred, null, null, null, null, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState)
	{
		Begin(sortMode, blendState, null, null, null, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
	{
		Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, null, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
	{
		Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, Matrix.Identity);
	}

	public void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
	{
		if (inBeginEndPair)
		{
			throw new InvalidOperationException(FrameworkResources.EndMustBeCalledBeforeBegin);
		}
		spriteSortMode = sortMode;
		this.blendState = blendState;
		this.samplerState = samplerState;
		this.depthStencilState = depthStencilState;
		this.rasterizerState = rasterizerState;
		customEffect = effect;
		this.transformMatrix = transformMatrix;
		if (sortMode == SpriteSortMode.Immediate)
		{
			if (_parent.spriteBeginCount > 0)
			{
				throw new InvalidOperationException(FrameworkResources.CannotNextSpriteBeginImmediate);
			}
			SetRenderState();
			_parent.spriteImmediateBeginCount++;
		}
		else if (_parent.spriteImmediateBeginCount > 0)
		{
			throw new InvalidOperationException(FrameworkResources.CannotNextSpriteBeginImmediate);
		}
		_parent.spriteBeginCount++;
		inBeginEndPair = true;
	}

	public void End()
	{
		if (!inBeginEndPair)
		{
			throw new InvalidOperationException(FrameworkResources.BeginMustBeCalledBeforeEnd);
		}
		if (spriteSortMode != SpriteSortMode.Immediate)
		{
			SetRenderState();
		}
		else
		{
			_parent.spriteImmediateBeginCount--;
		}
		if (spriteQueueCount > 0)
		{
			Flush();
		}
		inBeginEndPair = false;
		_parent.spriteBeginCount--;
	}

	public void Draw(Texture2D texture, Vector2 position, Color color)
	{
		Vector4 destination = default(Vector4);
		destination.X = position.X;
		destination.Y = position.Y;
		destination.Z = 1f;
		destination.W = 1f;
		InternalDraw(texture, ref destination, scaleDestination: true, ref nullRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f);
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
	{
		Vector4 destination = default(Vector4);
		destination.X = position.X;
		destination.Y = position.Y;
		destination.Z = 1f;
		destination.W = 1f;
		InternalDraw(texture, ref destination, scaleDestination: true, ref sourceRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f);
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		Vector4 destination = default(Vector4);
		destination.X = position.X;
		destination.Y = position.Y;
		destination.Z = scale;
		destination.W = scale;
		InternalDraw(texture, ref destination, scaleDestination: true, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth);
	}

	public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		Vector4 destination = default(Vector4);
		destination.X = position.X;
		destination.Y = position.Y;
		destination.Z = scale.X;
		destination.W = scale.Y;
		InternalDraw(texture, ref destination, scaleDestination: true, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth);
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
	{
		Vector4 destination = default(Vector4);
		destination.X = destinationRectangle.X;
		destination.Y = destinationRectangle.Y;
		destination.Z = destinationRectangle.Width;
		destination.W = destinationRectangle.Height;
		InternalDraw(texture, ref destination, scaleDestination: false, ref nullRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f);
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
	{
		Vector4 destination = default(Vector4);
		destination.X = destinationRectangle.X;
		destination.Y = destinationRectangle.Y;
		destination.Z = destinationRectangle.Width;
		destination.W = destinationRectangle.Height;
		InternalDraw(texture, ref destination, scaleDestination: false, ref sourceRectangle, color, 0f, ref vector2Zero, SpriteEffects.None, 0f);
	}

	public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
	{
		Vector4 destination = default(Vector4);
		destination.X = destinationRectangle.X;
		destination.Y = destinationRectangle.Y;
		destination.Z = destinationRectangle.Width;
		destination.W = destinationRectangle.Height;
		InternalDraw(texture, ref destination, scaleDestination: false, ref sourceRectangle, color, rotation, ref origin, effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		Vector2 scale = Vector2.One;
		spriteFont.InternalDraw(ref text2, this, position, color, 0f, Vector2.Zero, ref scale, SpriteEffects.None, 0f);
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		Vector2 scale = Vector2.One;
		spriteFont.InternalDraw(ref text2, this, position, color, 0f, Vector2.Zero, ref scale, SpriteEffects.None, 0f);
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		Vector2 scale2 = default(Vector2);
		scale2.X = scale;
		scale2.Y = scale;
		spriteFont.InternalDraw(ref text2, this, position, color, rotation, origin, ref scale2, effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		Vector2 scale2 = default(Vector2);
		scale2.X = scale;
		scale2.Y = scale;
		spriteFont.InternalDraw(ref text2, this, position, color, rotation, origin, ref scale2, effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		spriteFont.InternalDraw(ref text2, this, position, color, rotation, origin, ref scale, effects, layerDepth);
	}

	public void DrawString(SpriteFont spriteFont, StringBuilder text, Vector2 position, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
	{
		if (spriteFont == null)
		{
			throw new ArgumentNullException("spriteFont");
		}
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		SpriteFont.StringProxy text2 = new SpriteFont.StringProxy(text);
		spriteFont.InternalDraw(ref text2, this, position, color, rotation, origin, ref scale, effects, layerDepth);
	}

	private unsafe void InternalDraw(Texture2D texture, ref Vector4 destination, bool scaleDestination, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, SpriteEffects effects, float depth)
	{
		if (texture == null)
		{
			throw new ArgumentNullException("texture", FrameworkResources.NullNotAllowed);
		}
		if (!inBeginEndPair)
		{
			throw new InvalidOperationException(FrameworkResources.BeginMustBeCalledBeforeDraw);
		}
		if (spriteQueueCount >= spriteQueue.Length)
		{
			Array.Resize(ref spriteQueue, spriteQueue.Length * 2);
		}
		fixed (SpriteInfo* ptr = &spriteQueue[spriteQueueCount])
		{
			float num = destination.Z;
			float num2 = destination.W;
			if (sourceRectangle.HasValue)
			{
				Rectangle value = sourceRectangle.Value;
				ptr->Source.X = value.X;
				ptr->Source.Y = value.Y;
				ptr->Source.Z = value.Width;
				ptr->Source.W = value.Height;
				if (scaleDestination)
				{
					num *= (float)value.Width;
					num2 *= (float)value.Height;
				}
			}
			else
			{
				float num3 = texture.Width;
				float num4 = texture.Height;
				ptr->Source.X = 0f;
				ptr->Source.Y = 0f;
				ptr->Source.Z = num3;
				ptr->Source.W = num4;
				if (scaleDestination)
				{
					num *= num3;
					num2 *= num4;
				}
			}
			ptr->Destination.X = destination.X;
			ptr->Destination.Y = destination.Y;
			ptr->Destination.Z = num;
			ptr->Destination.W = num2;
			ptr->Origin.X = origin.X;
			ptr->Origin.Y = origin.Y;
			ptr->Rotation = rotation;
			ptr->Depth = depth;
			ptr->Effects = effects;
			ptr->Color = color;
		}
		if (spriteSortMode == SpriteSortMode.Immediate)
		{
			RenderBatch(texture, spriteQueue, 0, 1);
			return;
		}
		if (spriteTextures == null || spriteTextures.Length != spriteQueue.Length)
		{
			Array.Resize(ref spriteTextures, spriteQueue.Length);
		}
		spriteTextures[spriteQueueCount] = texture;
		spriteQueueCount++;
	}

	private void Flush()
	{
		SpriteInfo[] array;
		if (spriteSortMode == SpriteSortMode.Deferred)
		{
			array = spriteQueue;
		}
		else
		{
			SortSprites();
			array = sortedSprites;
		}
		int num = 0;
		Texture2D texture2D = null;
		for (int i = 0; i < spriteQueueCount; i++)
		{
			Texture2D texture2D2;
			if (spriteSortMode == SpriteSortMode.Deferred)
			{
				texture2D2 = spriteTextures[i];
			}
			else
			{
				int num2 = sortIndices[i];
				ref SpriteInfo reference = ref array[i];
				reference = spriteQueue[num2];
				texture2D2 = spriteTextures[num2];
			}
			if (texture2D2 != texture2D)
			{
				if (i > num)
				{
					RenderBatch(texture2D, array, num, i - num);
				}
				num = i;
				texture2D = texture2D2;
			}
		}
		RenderBatch(texture2D, array, num, spriteQueueCount - num);
		Array.Clear(spriteTextures, 0, spriteQueueCount);
		spriteQueueCount = 0;
	}

	private void SortSprites()
	{
		if (sortIndices == null || sortIndices.Length < spriteQueueCount)
		{
			sortIndices = new int[spriteQueueCount];
			sortedSprites = new SpriteInfo[spriteQueueCount];
		}
		IComparer<int> comparer;
		switch (spriteSortMode)
		{
		case SpriteSortMode.Texture:
			if (textureComparer == null)
			{
				textureComparer = new TextureComparer(this);
			}
			comparer = textureComparer;
			break;
		case SpriteSortMode.BackToFront:
			if (backToFrontComparer == null)
			{
				backToFrontComparer = new BackToFrontComparer(this);
			}
			comparer = backToFrontComparer;
			break;
		case SpriteSortMode.FrontToBack:
			if (frontToBackComparer == null)
			{
				frontToBackComparer = new FrontToBackComparer(this);
			}
			comparer = frontToBackComparer;
			break;
		default:
			throw new NotSupportedException();
		}
		for (int i = 0; i < spriteQueueCount; i++)
		{
			sortIndices[i] = i;
		}
		Array.Sort(sortIndices, 0, spriteQueueCount, comparer);
	}

	private void RenderBatch(Texture2D texture, SpriteInfo[] sprites, int offset, int count)
	{
		if (customEffect != null)
		{
			int count2 = customEffect.CurrentTechnique.Passes.Count;
			for (int i = 0; i < count2; i++)
			{
				customEffect.CurrentTechnique.Passes[i].Apply();
				_parent.Textures[0] = texture;
				PlatformRenderBatch(texture, sprites, offset, count);
			}
		}
		else
		{
			_parent.Textures[0] = texture;
			PlatformRenderBatch(texture, sprites, offset, count);
		}
	}

	private void SetRenderState()
	{
		if (blendState != null)
		{
			_parent.BlendState = blendState;
		}
		else
		{
			_parent.BlendState = BlendState.AlphaBlend;
		}
		if (depthStencilState != null)
		{
			_parent.DepthStencilState = depthStencilState;
		}
		else
		{
			_parent.DepthStencilState = DepthStencilState.None;
		}
		if (rasterizerState != null)
		{
			_parent.RasterizerState = rasterizerState;
		}
		else
		{
			_parent.RasterizerState = RasterizerState.CullCounterClockwise;
		}
		if (samplerState != null)
		{
			_parent.SamplerStates[0] = samplerState;
		}
		else
		{
			_parent.SamplerStates[0] = SamplerState.LinearClamp;
		}
		Viewport viewport = _parent.Viewport;
		float num = ((viewport.Width > 0) ? (1f / (float)viewport.Width) : 0f);
		float num2 = ((viewport.Height > 0) ? (-1f / (float)viewport.Height) : 0f);
		Matrix matrix = default(Matrix);
		matrix.M11 = num * 2f;
		matrix.M22 = num2 * 2f;
		matrix.M33 = 1f;
		matrix.M44 = 1f;
		matrix.M41 = -1f;
		matrix.M42 = 1f;
		matrix.M41 -= num;
		matrix.M42 -= num2;
		effectMatrixTransform.SetValue(transformMatrix * matrix);
		spriteEffect.CurrentTechnique.Passes[0].Apply();
		SetPlatformRenderState();
	}
}
