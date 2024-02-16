using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Xna.Framework.Graphics;

[SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
public struct Viewport
{
	private int _x;

	private int _y;

	private int _width;

	private int _height;

	private float _minZ;

	private float _maxZ;

	public int X
	{
		get
		{
			return _x;
		}
		set
		{
			_x = value;
		}
	}

	public int Y
	{
		get
		{
			return _y;
		}
		set
		{
			_y = value;
		}
	}

	public int Width
	{
		get
		{
			return _width;
		}
		set
		{
			_width = value;
		}
	}

	public int Height
	{
		get
		{
			return _height;
		}
		set
		{
			_height = value;
		}
	}

	public float MinDepth
	{
		get
		{
			return _minZ;
		}
		set
		{
			_minZ = value;
		}
	}

	public float MaxDepth
	{
		get
		{
			return _maxZ;
		}
		set
		{
			_maxZ = value;
		}
	}

	public Rectangle Bounds
	{
		get
		{
			Rectangle result = default(Rectangle);
			result.X = _x;
			result.Y = _y;
			result.Width = _width;
			result.Height = _height;
			return result;
		}
		set
		{
			_x = value.X;
			_y = value.Y;
			_width = value.Width;
			_height = value.Height;
		}
	}

	public float AspectRatio
	{
		get
		{
			if (_height == 0 || _width == 0)
			{
				return 0f;
			}
			return (float)_width / (float)_height;
		}
	}

	public Rectangle TitleSafeArea => GetTitleSafeArea(_x, _y, _width, _height);

	public Viewport(int x, int y, int width, int height)
	{
		_x = x;
		_y = y;
		_width = width;
		_height = height;
		_minZ = 0f;
		_maxZ = 1f;
	}

	public Viewport(Rectangle bounds)
	{
		_x = bounds.X;
		_y = bounds.Y;
		_width = bounds.Width;
		_height = bounds.Height;
		_minZ = 0f;
		_maxZ = 1f;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{X:{0} Y:{1} Width:{2} Height:{3} MinDepth:{4} MaxDepth:{5}}}", X, Y, Width, Height, MinDepth, MaxDepth);
	}

	private static bool WithinEpsilon(float a, float b)
	{
		float num = a - b;
		if (-1E-45f <= num)
		{
			return num <= float.Epsilon;
		}
		return false;
	}

	public Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
	{
		Matrix matrix = Matrix.Multiply(world, view);
		matrix = Matrix.Multiply(matrix, projection);
		Vector3 result = Vector3.Transform(source, matrix);
		float num = source.X * matrix.M14 + source.Y * matrix.M24 + source.Z * matrix.M34 + matrix.M44;
		if (!WithinEpsilon(num, 1f))
		{
			result /= num;
		}
		result.X = (result.X + 1f) * 0.5f * (float)Width + (float)X;
		result.Y = (0f - result.Y + 1f) * 0.5f * (float)Height + (float)Y;
		result.Z = result.Z * (MaxDepth - MinDepth) + MinDepth;
		return result;
	}

	public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
	{
		Matrix matrix = Matrix.Multiply(world, view);
		matrix = Matrix.Multiply(matrix, projection);
		matrix = Matrix.Invert(matrix);
		source.X = (source.X - (float)X) / (float)Width * 2f - 1f;
		source.Y = 0f - ((source.Y - (float)Y) / (float)Height * 2f - 1f);
		source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
		Vector3 result = Vector3.Transform(source, matrix);
		float num = source.X * matrix.M14 + source.Y * matrix.M24 + source.Z * matrix.M34 + matrix.M44;
		if (!WithinEpsilon(num, 1f))
		{
			result /= num;
		}
		return result;
	}

	internal static Rectangle GetTitleSafeArea(int x, int y, int w, int h)
	{
		return new Rectangle(x, y, w, h);
	}
}
