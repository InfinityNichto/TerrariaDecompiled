namespace Microsoft.Xna.Framework.Graphics;

public sealed class ModelMeshPart
{
	internal VertexBuffer vertexBuffer;

	internal IndexBuffer indexBuffer;

	private int startIndex;

	private int primitiveCount;

	private int vertexOffset;

	private int numVertices;

	private Effect effect;

	private object tag;

	internal ModelMesh parent;

	public int StartIndex => startIndex;

	public int PrimitiveCount => primitiveCount;

	public int VertexOffset => vertexOffset;

	public int NumVertices => numVertices;

	public IndexBuffer IndexBuffer => indexBuffer;

	public VertexBuffer VertexBuffer => vertexBuffer;

	public Effect Effect
	{
		get
		{
			return effect;
		}
		set
		{
			if (value == effect)
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			int count = parent.MeshParts.Count;
			for (int i = 0; i < count; i++)
			{
				ModelMeshPart modelMeshPart = parent.MeshParts[i];
				if (!object.ReferenceEquals(modelMeshPart, this))
				{
					Effect objA = modelMeshPart.Effect;
					if (object.ReferenceEquals(objA, effect))
					{
						flag = true;
					}
					else if (object.ReferenceEquals(objA, value))
					{
						flag2 = true;
					}
				}
			}
			if (!flag && effect != null)
			{
				parent.Effects.Remove(effect);
			}
			if (!flag2 && value != null)
			{
				parent.Effects.Add(value);
			}
			effect = value;
		}
	}

	public object Tag
	{
		get
		{
			return tag;
		}
		set
		{
			tag = value;
		}
	}

	internal ModelMeshPart(int vertexOffset, int numVertices, int startIndex, int primitiveCount, object tag)
	{
		this.vertexOffset = vertexOffset;
		this.numVertices = numVertices;
		this.startIndex = startIndex;
		this.primitiveCount = primitiveCount;
		this.tag = tag;
	}

	internal void Draw()
	{
		if (NumVertices > 0)
		{
			GraphicsDevice graphicsDevice = vertexBuffer.GraphicsDevice;
			graphicsDevice.SetVertexBuffer(vertexBuffer, vertexOffset);
			graphicsDevice.Indices = indexBuffer;
			graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, startIndex, primitiveCount);
		}
	}
}
