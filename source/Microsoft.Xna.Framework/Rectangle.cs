using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Xna.Framework.Design;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(RectangleConverter))]
public struct Rectangle : IEquatable<Rectangle>
{
	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public int X;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public int Y;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public int Width;

	[SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
	public int Height;

	private static Rectangle _empty = default(Rectangle);

	public int Left => X;

	public int Right => X + Width;

	public int Top => Y;

	public int Bottom => Y + Height;

	public Point Location
	{
		get
		{
			return new Point(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public Point Center => new Point(X + Width / 2, Y + Height / 2);

	public static Rectangle Empty => _empty;

	public bool IsEmpty
	{
		get
		{
			if (Width == 0 && Height == 0 && X == 0)
			{
				return Y == 0;
			}
			return false;
		}
	}

	public Rectangle(int x, int y, int width, int height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public void Offset(Point amount)
	{
		X += amount.X;
		Y += amount.Y;
	}

	public void Offset(int offsetX, int offsetY)
	{
		X += offsetX;
		Y += offsetY;
	}

	public void Inflate(int horizontalAmount, int verticalAmount)
	{
		X -= horizontalAmount;
		Y -= verticalAmount;
		Width += horizontalAmount * 2;
		Height += verticalAmount * 2;
	}

	public bool Contains(int x, int y)
	{
		if (X <= x && x < X + Width && Y <= y)
		{
			return y < Y + Height;
		}
		return false;
	}

	public bool Contains(Point value)
	{
		if (X <= value.X && value.X < X + Width && Y <= value.Y)
		{
			return value.Y < Y + Height;
		}
		return false;
	}

	public void Contains(ref Point value, out bool result)
	{
		result = X <= value.X && value.X < X + Width && Y <= value.Y && value.Y < Y + Height;
	}

	public bool Contains(Rectangle value)
	{
		if (X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y)
		{
			return value.Y + value.Height <= Y + Height;
		}
		return false;
	}

	public void Contains(ref Rectangle value, out bool result)
	{
		result = X <= value.X && value.X + value.Width <= X + Width && Y <= value.Y && value.Y + value.Height <= Y + Height;
	}

	public bool Intersects(Rectangle value)
	{
		if (value.X < X + Width && X < value.X + value.Width && value.Y < Y + Height)
		{
			return Y < value.Y + value.Height;
		}
		return false;
	}

	public void Intersects(ref Rectangle value, out bool result)
	{
		result = value.X < X + Width && X < value.X + value.Width && value.Y < Y + Height && Y < value.Y + value.Height;
	}

	public static Rectangle Intersect(Rectangle value1, Rectangle value2)
	{
		int num = value1.X + value1.Width;
		int num2 = value2.X + value2.Width;
		int num3 = value1.Y + value1.Height;
		int num4 = value2.Y + value2.Height;
		int num5 = ((value1.X > value2.X) ? value1.X : value2.X);
		int num6 = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
		int num7 = ((num < num2) ? num : num2);
		int num8 = ((num3 < num4) ? num3 : num4);
		Rectangle result = default(Rectangle);
		if (num7 > num5 && num8 > num6)
		{
			result.X = num5;
			result.Y = num6;
			result.Width = num7 - num5;
			result.Height = num8 - num6;
		}
		else
		{
			result.X = 0;
			result.Y = 0;
			result.Width = 0;
			result.Height = 0;
		}
		return result;
	}

	public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
	{
		int num = value1.X + value1.Width;
		int num2 = value2.X + value2.Width;
		int num3 = value1.Y + value1.Height;
		int num4 = value2.Y + value2.Height;
		int num5 = ((value1.X > value2.X) ? value1.X : value2.X);
		int num6 = ((value1.Y > value2.Y) ? value1.Y : value2.Y);
		int num7 = ((num < num2) ? num : num2);
		int num8 = ((num3 < num4) ? num3 : num4);
		if (num7 > num5 && num8 > num6)
		{
			result.X = num5;
			result.Y = num6;
			result.Width = num7 - num5;
			result.Height = num8 - num6;
		}
		else
		{
			result.X = 0;
			result.Y = 0;
			result.Width = 0;
			result.Height = 0;
		}
	}

	public static Rectangle Union(Rectangle value1, Rectangle value2)
	{
		int num = value1.X + value1.Width;
		int num2 = value2.X + value2.Width;
		int num3 = value1.Y + value1.Height;
		int num4 = value2.Y + value2.Height;
		int num5 = ((value1.X < value2.X) ? value1.X : value2.X);
		int num6 = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
		int num7 = ((num > num2) ? num : num2);
		int num8 = ((num3 > num4) ? num3 : num4);
		Rectangle result = default(Rectangle);
		result.X = num5;
		result.Y = num6;
		result.Width = num7 - num5;
		result.Height = num8 - num6;
		return result;
	}

	public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
	{
		int num = value1.X + value1.Width;
		int num2 = value2.X + value2.Width;
		int num3 = value1.Y + value1.Height;
		int num4 = value2.Y + value2.Height;
		int num5 = ((value1.X < value2.X) ? value1.X : value2.X);
		int num6 = ((value1.Y < value2.Y) ? value1.Y : value2.Y);
		int num7 = ((num > num2) ? num : num2);
		int num8 = ((num3 > num4) ? num3 : num4);
		result.X = num5;
		result.Y = num6;
		result.Width = num7 - num5;
		result.Height = num8 - num6;
	}

	public bool Equals(Rectangle other)
	{
		if (X == other.X && Y == other.Y && Width == other.Width)
		{
			return Height == other.Height;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		bool result = false;
		if (obj is Rectangle)
		{
			result = Equals((Rectangle)obj);
		}
		return result;
	}

	public override string ToString()
	{
		CultureInfo currentCulture = CultureInfo.CurrentCulture;
		return string.Format(currentCulture, "{{X:{0} Y:{1} Width:{2} Height:{3}}}", X.ToString(currentCulture), Y.ToString(currentCulture), Width.ToString(currentCulture), Height.ToString(currentCulture));
	}

	public override int GetHashCode()
	{
		return X.GetHashCode() + Y.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();
	}

	public static bool operator ==(Rectangle a, Rectangle b)
	{
		if (a.X == b.X && a.Y == b.Y && a.Width == b.Width)
		{
			return a.Height == b.Height;
		}
		return false;
	}

	public static bool operator !=(Rectangle a, Rectangle b)
	{
		if (a.X == b.X && a.Y == b.Y && a.Width == b.Width)
		{
			return a.Height != b.Height;
		}
		return true;
	}
}
