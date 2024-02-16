using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public struct VertexPositionNormalTexture : IVertexType
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Position;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Normal;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector2 TextureCoordinate;

	[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0), new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0))
	{
		Name = "VertexPositionNormalTexture.VertexDeclaration"
	};

	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

	public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
	{
		Position = position;
		Normal = normal;
		TextureCoordinate = textureCoordinate;
	}

	public override int GetHashCode()
	{
		return Helpers.SmartGetHashCode(this);
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Position:{0} Normal:{1} TextureCoordinate:{2}}}", new object[3] { Position, Normal, TextureCoordinate });
	}

	public static bool operator ==(VertexPositionNormalTexture left, VertexPositionNormalTexture right)
	{
		if (left.Position == right.Position && left.Normal == right.Normal)
		{
			return left.TextureCoordinate == right.TextureCoordinate;
		}
		return false;
	}

	public static bool operator !=(VertexPositionNormalTexture left, VertexPositionNormalTexture right)
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
		return this == (VertexPositionNormalTexture)obj;
	}
}
