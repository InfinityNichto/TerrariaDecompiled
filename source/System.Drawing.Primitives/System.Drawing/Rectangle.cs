using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Drawing;

[Serializable]
[TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[TypeConverter("System.Drawing.RectangleConverter, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public struct Rectangle : IEquatable<Rectangle>
{
	public static readonly Rectangle Empty;

	private int x;

	private int y;

	private int width;

	private int height;

	[Browsable(false)]
	public Point Location
	{
		readonly get
		{
			return new Point(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	[Browsable(false)]
	public Size Size
	{
		readonly get
		{
			return new Size(Width, Height);
		}
		set
		{
			Width = value.Width;
			Height = value.Height;
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

	public int Width
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

	public int Height
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

	[Browsable(false)]
	public readonly int Left => X;

	[Browsable(false)]
	public readonly int Top => Y;

	[Browsable(false)]
	public readonly int Right => X + Width;

	[Browsable(false)]
	public readonly int Bottom => Y + Height;

	[Browsable(false)]
	public readonly bool IsEmpty
	{
		get
		{
			if (height == 0 && width == 0 && x == 0)
			{
				return y == 0;
			}
			return false;
		}
	}

	public Rectangle(int x, int y, int width, int height)
	{
		this.x = x;
		this.y = y;
		this.width = width;
		this.height = height;
	}

	public Rectangle(Point location, Size size)
	{
		x = location.X;
		y = location.Y;
		width = size.Width;
		height = size.Height;
	}

	public static Rectangle FromLTRB(int left, int top, int right, int bottom)
	{
		return new Rectangle(left, top, right - left, bottom - top);
	}

	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Rectangle)
		{
			return Equals((Rectangle)obj);
		}
		return false;
	}

	public readonly bool Equals(Rectangle other)
	{
		return this == other;
	}

	public static bool operator ==(Rectangle left, Rectangle right)
	{
		if (left.X == right.X && left.Y == right.Y && left.Width == right.Width)
		{
			return left.Height == right.Height;
		}
		return false;
	}

	public static bool operator !=(Rectangle left, Rectangle right)
	{
		return !(left == right);
	}

	public static Rectangle Ceiling(RectangleF value)
	{
		return new Rectangle((int)Math.Ceiling(value.X), (int)Math.Ceiling(value.Y), (int)Math.Ceiling(value.Width), (int)Math.Ceiling(value.Height));
	}

	public static Rectangle Truncate(RectangleF value)
	{
		return new Rectangle((int)value.X, (int)value.Y, (int)value.Width, (int)value.Height);
	}

	public static Rectangle Round(RectangleF value)
	{
		return new Rectangle((int)Math.Round(value.X), (int)Math.Round(value.Y), (int)Math.Round(value.Width), (int)Math.Round(value.Height));
	}

	public readonly bool Contains(int x, int y)
	{
		if (X <= x && x < X + Width && Y <= y)
		{
			return y < Y + Height;
		}
		return false;
	}

	public readonly bool Contains(Point pt)
	{
		return Contains(pt.X, pt.Y);
	}

	public readonly bool Contains(Rectangle rect)
	{
		if (X <= rect.X && rect.X + rect.Width <= X + Width && Y <= rect.Y)
		{
			return rect.Y + rect.Height <= Y + Height;
		}
		return false;
	}

	public override readonly int GetHashCode()
	{
		return HashCode.Combine(X, Y, Width, Height);
	}

	public void Inflate(int width, int height)
	{
		X -= width;
		Y -= height;
		Width += 2 * width;
		Height += 2 * height;
	}

	public void Inflate(Size size)
	{
		Inflate(size.Width, size.Height);
	}

	public static Rectangle Inflate(Rectangle rect, int x, int y)
	{
		Rectangle result = rect;
		result.Inflate(x, y);
		return result;
	}

	public void Intersect(Rectangle rect)
	{
		Rectangle rectangle = Intersect(rect, this);
		X = rectangle.X;
		Y = rectangle.Y;
		Width = rectangle.Width;
		Height = rectangle.Height;
	}

	public static Rectangle Intersect(Rectangle a, Rectangle b)
	{
		int num = Math.Max(a.X, b.X);
		int num2 = Math.Min(a.X + a.Width, b.X + b.Width);
		int num3 = Math.Max(a.Y, b.Y);
		int num4 = Math.Min(a.Y + a.Height, b.Y + b.Height);
		if (num2 >= num && num4 >= num3)
		{
			return new Rectangle(num, num3, num2 - num, num4 - num3);
		}
		return Empty;
	}

	public readonly bool IntersectsWith(Rectangle rect)
	{
		if (rect.X < X + Width && X < rect.X + rect.Width && rect.Y < Y + Height)
		{
			return Y < rect.Y + rect.Height;
		}
		return false;
	}

	public static Rectangle Union(Rectangle a, Rectangle b)
	{
		int num = Math.Min(a.X, b.X);
		int num2 = Math.Max(a.X + a.Width, b.X + b.Width);
		int num3 = Math.Min(a.Y, b.Y);
		int num4 = Math.Max(a.Y + a.Height, b.Y + b.Height);
		return new Rectangle(num, num3, num2 - num, num4 - num3);
	}

	public void Offset(Point pos)
	{
		Offset(pos.X, pos.Y);
	}

	public void Offset(int x, int y)
	{
		X += x;
		Y += y;
	}

	public override readonly string ToString()
	{
		return $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
	}
}
