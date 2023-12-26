using System;
using Microsoft.Xna.Framework;

namespace ReLogic.Peripherals.RGB;

public class Fragment
{
	public readonly Vector4[] Colors;

	private readonly Vector2[] _canvasPositions;

	private readonly Point[] _gridPositions;

	public readonly int Count;

	public Vector2 CanvasTopLeft { get; private set; }

	public Vector2 CanvasBottomRight { get; private set; }

	public Vector2 CanvasSize { get; private set; }

	public Vector2 CanvasCenter { get; private set; }

	private Fragment(Point[] gridPositions)
		: this(gridPositions, CreateCanvasPositions(gridPositions))
	{
	}

	private Fragment(Point[] gridPositions, Vector2[] canvasPositions)
	{
		Count = gridPositions.Length;
		_gridPositions = gridPositions;
		_canvasPositions = canvasPositions;
		Colors = (Vector4[])(object)new Vector4[Count];
		SetupCanvasBounds();
	}

	public static Fragment FromGrid(Rectangle grid)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Point[] array = (Point[])(object)new Point[grid.Width * grid.Height];
		int num = 0;
		for (int i = 0; i < grid.Height; i++)
		{
			for (int j = 0; j < grid.Width; j++)
			{
				array[num++] = new Point(grid.X + j, grid.Y + i);
			}
		}
		return new Fragment(array);
	}

	public static Fragment FromCustom(Point[] gridPositions)
	{
		return new Fragment(gridPositions);
	}

	public static Fragment FromCustom(Point[] gridPositions, Vector2[] canvasPositions)
	{
		return new Fragment(gridPositions, canvasPositions);
	}

	public Fragment CreateCopy()
	{
		return new Fragment(_gridPositions, _canvasPositions);
	}

	public Vector2 GetCanvasPositionOfIndex(int index)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return _canvasPositions[index];
	}

	public Point GetGridPositionOfIndex(int index)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return _gridPositions[index];
	}

	public void Clear()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < Colors.Length; i++)
		{
			Colors[i] = Vector4.Zero;
		}
	}

	public void SetColor(int index, Vector4 color)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Colors[index] = color;
	}

	public void SetColor(int index, float r, float g, float b, float a = 1f)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Colors[index] = new Vector4(r, g, b, a);
	}

	public Vector4 GetAverage()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		Vector4 zero = Vector4.Zero;
		for (int i = 0; i < Colors.Length; i++)
		{
			zero += Colors[i];
		}
		return zero / (float)Colors.Length;
	}

	private void SetupCanvasBounds()
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		if (_canvasPositions.Length != 0)
		{
			float num = _canvasPositions[0].X;
			float num2 = _canvasPositions[0].X;
			float num3 = _canvasPositions[0].Y;
			float num4 = _canvasPositions[0].Y;
			for (int i = 1; i < _canvasPositions.Length; i++)
			{
				num = Math.Min(num, _canvasPositions[i].X);
				num3 = Math.Min(num3, _canvasPositions[i].Y);
				num2 = Math.Max(num2, _canvasPositions[i].X);
				num4 = Math.Max(num4, _canvasPositions[i].Y);
			}
			CanvasTopLeft = new Vector2(num, num3);
			CanvasBottomRight = new Vector2(num2, num4);
			CanvasSize = new Vector2(num2 - num, num4 - num3);
			CanvasCenter = CanvasTopLeft + CanvasSize * 0.5f;
		}
	}

	private static Vector2[] CreateCanvasPositions(Point[] gridPositions)
	{
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = (Vector2[])(object)new Vector2[gridPositions.Length];
		int num = gridPositions[0].Y;
		int num2 = gridPositions[0].Y;
		int num3 = gridPositions[0].X;
		for (int i = 1; i < gridPositions.Length; i++)
		{
			num3 = Math.Min(num3, gridPositions[i].X);
			num = Math.Min(num, gridPositions[i].Y);
			num2 = Math.Max(num2, gridPositions[i].Y);
		}
		float num4 = 1f;
		if (num2 != num)
		{
			num4 = 1f / (float)(num2 - num);
		}
		Vector2 vector = default(Vector2);
		((Vector2)(ref vector))._002Ector((float)num3 * (1f / 6f), (float)num * (1f / 6f));
		for (int j = 0; j < gridPositions.Length; j++)
		{
			array[j] = new Vector2((float)(gridPositions[j].X - num3), (float)(gridPositions[j].Y - num)) * num4 + vector;
		}
		return array;
	}
}
