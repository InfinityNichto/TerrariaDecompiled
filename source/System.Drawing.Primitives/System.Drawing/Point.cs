using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Drawing;

[Serializable]
[TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[TypeConverter("System.Drawing.PointConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public struct Point : IEquatable<Point>
{
	public static readonly Point Empty;

	private int x;

	private int y;

	[Browsable(false)]
	public readonly bool IsEmpty
	{
		get
		{
			if (x == 0)
			{
				return y == 0;
			}
			return false;
		}
	}

	public int X
	{
		readonly get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	public int Y
	{
		readonly get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public Point(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public Point(Size sz)
	{
		x = sz.Width;
		y = sz.Height;
	}

	public Point(int dw)
	{
		x = LowInt16(dw);
		y = HighInt16(dw);
	}

	public static implicit operator PointF(Point p)
	{
		return new PointF(p.X, p.Y);
	}

	public static explicit operator Size(Point p)
	{
		return new Size(p.X, p.Y);
	}

	public static Point operator +(Point pt, Size sz)
	{
		return Add(pt, sz);
	}

	public static Point operator -(Point pt, Size sz)
	{
		return Subtract(pt, sz);
	}

	public static bool operator ==(Point left, Point right)
	{
		if (left.X == right.X)
		{
			return left.Y == right.Y;
		}
		return false;
	}

	public static bool operator !=(Point left, Point right)
	{
		return !(left == right);
	}

	public static Point Add(Point pt, Size sz)
	{
		return new Point(pt.X + sz.Width, pt.Y + sz.Height);
	}

	public static Point Subtract(Point pt, Size sz)
	{
		return new Point(pt.X - sz.Width, pt.Y - sz.Height);
	}

	public static Point Ceiling(PointF value)
	{
		return new Point((int)Math.Ceiling(value.X), (int)Math.Ceiling(value.Y));
	}

	public static Point Truncate(PointF value)
	{
		return new Point((int)value.X, (int)value.Y);
	}

	public static Point Round(PointF value)
	{
		return new Point((int)Math.Round(value.X), (int)Math.Round(value.Y));
	}

	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Point)
		{
			return Equals((Point)obj);
		}
		return false;
	}

	public readonly bool Equals(Point other)
	{
		return this == other;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	public void Offset(int dx, int dy)
	{
		X += dx;
		Y += dy;
	}

	public void Offset(Point p)
	{
		Offset(p.X, p.Y);
	}

	public override readonly string ToString()
	{
		return $"{{X={X},Y={Y}}}";
	}

	private static short HighInt16(int n)
	{
		return (short)((n >> 16) & 0xFFFF);
	}

	private static short LowInt16(int n)
	{
		return (short)(n & 0xFFFF);
	}
}
