using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public struct VertexPositionTexture : IVertexType
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Position;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector2 TextureCoordinate;

	[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0))
	{
		Name = "VertexPositionTexture.VertexDeclaration"
	};

	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

	public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
	{
		Position = position;
		TextureCoordinate = textureCoordinate;
	}

	public override int GetHashCode()
	{
		return Helpers.SmartGetHashCode(this);
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Position:{0} TextureCoordinate:{1}}}", new object[2] { Position, TextureCoordinate });
	}

	public static bool operator ==(VertexPositionTexture left, VertexPositionTexture right)
	{
		if (left.Position == right.Position)
		{
			return left.TextureCoordinate == right.TextureCoordinate;
		}
		return false;
	}

	public static bool operator !=(VertexPositionTexture left, VertexPositionTexture right)
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
		return this == (VertexPositionTexture)obj;
	}
}
