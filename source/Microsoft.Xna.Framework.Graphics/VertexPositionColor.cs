using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

[Serializable]
public struct VertexPositionColor : IVertexType
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Vector3 Position;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public Color Color;

	[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
	public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0))
	{
		Name = "VertexPositionColor.VertexDeclaration"
	};

	VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

	public VertexPositionColor(Vector3 position, Color color)
	{
		Position = position;
		Color = color;
	}

	public override int GetHashCode()
	{
		return Helpers.SmartGetHashCode(this);
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{Position:{0} Color:{1}}}", new object[2] { Position, Color });
	}

	public static bool operator ==(VertexPositionColor left, VertexPositionColor right)
	{
		if (left.Color == right.Color)
		{
			return left.Position == right.Position;
		}
		return false;
	}

	public static bool operator !=(VertexPositionColor left, VertexPositionColor right)
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
		return this == (VertexPositionColor)obj;
	}
}
