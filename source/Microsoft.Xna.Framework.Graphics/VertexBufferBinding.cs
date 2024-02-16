using System;

namespace Microsoft.Xna.Framework.Graphics;

public struct VertexBufferBinding
{
	internal VertexBuffer _vertexBuffer;

	internal int _vertexOffset;

	internal int _instanceFrequency;

	public int InstanceFrequency => _instanceFrequency;

	public int VertexOffset => _vertexOffset;

	public VertexBuffer VertexBuffer => _vertexBuffer;

	public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset, int instanceFrequency)
	{
		if (vertexBuffer == null)
		{
			throw new ArgumentNullException("vertexBuffer", FrameworkResources.NullNotAllowed);
		}
		if (vertexOffset >= 0 && (uint)vertexOffset < vertexBuffer._vertexCount)
		{
			if (instanceFrequency < 0)
			{
				throw new ArgumentOutOfRangeException("instanceFrequency");
			}
			_vertexBuffer = vertexBuffer;
			_vertexOffset = vertexOffset;
			_instanceFrequency = instanceFrequency;
			return;
		}
		throw new ArgumentOutOfRangeException("vertexOffset");
	}

	public VertexBufferBinding(VertexBuffer vertexBuffer, int vertexOffset)
	{
		if (vertexBuffer == null)
		{
			throw new ArgumentNullException("vertexBuffer", FrameworkResources.NullNotAllowed);
		}
		if (vertexOffset >= 0 && (uint)vertexOffset < vertexBuffer._vertexCount)
		{
			_vertexBuffer = vertexBuffer;
			_vertexOffset = vertexOffset;
			_instanceFrequency = 0;
			return;
		}
		throw new ArgumentOutOfRangeException("vertexOffset");
	}

	public VertexBufferBinding(VertexBuffer vertexBuffer)
	{
		if (vertexBuffer == null)
		{
			throw new ArgumentNullException("vertexBuffer", FrameworkResources.NullNotAllowed);
		}
		_vertexBuffer = vertexBuffer;
		_vertexOffset = 0;
		_instanceFrequency = 0;
	}

	public static implicit operator VertexBufferBinding(VertexBuffer vertexBuffer)
	{
		return new VertexBufferBinding(vertexBuffer);
	}
}
