using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Drawing;

[Serializable]
[TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[TypeConverter("System.Drawing.SizeFConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public struct SizeF : IEquatable<SizeF>
{
	public static readonly SizeF Empty;

	private float width;

	private float height;

	[Browsable(false)]
	public readonly bool IsEmpty
	{
		get
		{
			if (width == 0f)
			{
				return height == 0f;
			}
			return false;
		}
	}

	public float Width
	{
		readonly get
		{
			return width;
		}
		set
		{
			width = value;
		}
	}

	public float Height
	{
		readonly get
		{
			return height;
		}
		set
		{
			height = value;
		}
	}

	public SizeF(SizeF size)
	{
		width = size.width;
		height = size.height;
	}

	public SizeF(PointF pt)
	{
		width = pt.X;
		height = pt.Y;
	}

	public SizeF(Vector2 vector)
	{
		width = vector.X;
		height = vector.Y;
	}

	public Vector2 ToVector2()
	{
		return new Vector2(width, height);
	}

	public SizeF(float width, float height)
	{
		this.width = width;
		this.height = height;
	}

	public static explicit operator Vector2(SizeF size)
	{
		return size.ToVector2();
	}

	public static explicit operator SizeF(Vector2 vector)
	{
		return new SizeF(vector);
	}

	public static SizeF operator +(SizeF sz1, SizeF sz2)
	{
		return Add(sz1, sz2);
	}

	public static SizeF operator -(SizeF sz1, SizeF sz2)
	{
		return Subtract(sz1, sz2);
	}

	public static SizeF operator *(float left, SizeF right)
	{
		return Multiply(right, left);
	}

	public static SizeF operator *(SizeF left, float right)
	{
		return Multiply(left, right);
	}

	public static SizeF operator /(SizeF left, float right)
	{
		return new SizeF(left.width / right, left.height / right);
	}

	public static bool operator ==(SizeF sz1, SizeF sz2)
	{
		if (sz1.Width == sz2.Width)
		{
			return sz1.Height == sz2.Height;
		}
		return false;
	}

	public static bool operator !=(SizeF sz1, SizeF sz2)
	{
		return !(sz1 == sz2);
	}

	public static explicit operator PointF(SizeF size)
	{
		return new PointF(size.Width, size.Height);
	}

	public static SizeF Add(SizeF sz1, SizeF sz2)
	{
		return new SizeF(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
	}

	public static SizeF Subtract(SizeF sz1, SizeF sz2)
	{
		return new SizeF(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
	}

	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is SizeF)
		{
			return Equals((SizeF)obj);
		}
		return false;
	}

	public readonly bool Equals(SizeF other)
	{
		return this == other;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(Width, Height);
	}

	public readonly PointF ToPointF()
	{
		return (PointF)this;
	}

	public readonly Size ToSize()
	{
		return Size.Truncate(this);
	}

	public override readonly string ToString()
	{
		return $"{{Width={width}, Height={height}}}";
	}

	private static SizeF Multiply(SizeF size, float multiplier)
	{
		return new SizeF(size.width * multiplier, size.height * multiplier);
	}
}
