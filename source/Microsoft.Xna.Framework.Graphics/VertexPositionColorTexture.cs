using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public struct VertexPositionColorTexture : IVertexType
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Position;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Color Color;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector2 TextureCoordinate;

	[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0), new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0))
	{
		Name = "VertexPositionColorTexture.VertexDeclaration"
	};

	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

	public VertexPositionColorTexture(Vector3 position, Color color, Vector2 textureCoordinate)
	{
		Position = position;
		Color = color;
		TextureCoordinate = textureCoordinate;
	}

	public override int GetHashCode()
	{
		return Helpers.SmartGetHashCode(this);
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Position:{0} Color:{1} TextureCoordinate:{2}}}", new object[3] { Position, Color, TextureCoordinate });
	}

	public static bool operator ==(VertexPositionColorTexture left, VertexPositionColorTexture right)
	{
		if (left.Position == right.Position && left.Color == right.Color)
		{
			return left.TextureCoordinate == right.TextureCoordinate;
		}
		return false;
	}

	public static bool operator !=(VertexPositionColorTexture left, VertexPositionColorTexture right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return this == (VertexPositionColorTexture)obj;
	}
}
