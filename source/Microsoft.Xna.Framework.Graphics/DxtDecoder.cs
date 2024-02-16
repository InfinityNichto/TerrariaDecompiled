using System;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Microsoft.Xna.Framework.Graphics;

internal class DxtDecoder
{
	private const int EndianOffset0 = 0;

	private const int EndianOffset1 = 1;

	private int width;

	private int height;

	private SurfaceFormat format;

	private Color[] colorPalette = new Color[4];

	private byte[] alphaPalette = new byte[8];

	public int PackedDataSize
	{
		get
		{
			int num = ((format == SurfaceFormat.Dxt1) ? 8 : 16);
			int num2 = width / 4;
			int num3 = height / 4;
			return num * num2 * num3;
		}
	}

	public DxtDecoder(int width, int height, SurfaceFormat format)
	{
		if (((uint)(width | height) & 3u) != 0)
		{
			throw new ArgumentException();
		}
		this.width = width;
		this.height = height;
		this.format = format;
	}

	public Color[] Decode(byte[] source)
	{
		Color[] result = new Color[width * height];
		int num = 0;
		for (int i = 0; i < height; i += 4)
		{
			for (int j = 0; j < width; j += 4)
			{
				int resultOffset = i * width + j;
				switch (format)
				{
				case SurfaceFormat.Dxt1:
					DecodeRgbBlock(source, num, result, resultOffset, isDxt1: true);
					num += 8;
					break;
				case SurfaceFormat.Dxt3:
					DecodeRgbBlock(source, num + 8, result, resultOffset, isDxt1: false);
					DecodeExplicitAlphaBlock(source, num, result, resultOffset);
					num += 16;
					break;
				case SurfaceFormat.Dxt5:
					DecodeRgbBlock(source, num + 8, result, resultOffset, isDxt1: false);
					DecodeInterpolatedAlphaBlock(source, num, result, resultOffset);
					num += 16;
					break;
				}
			}
		}
		return result;
	}

	private void DecodeRgbBlock(byte[] source, int sourceOffset, Color[] result, int resultOffset, bool isDxt1)
	{
		ushort num = Read16(source, sourceOffset);
		ushort num2 = Read16(source, sourceOffset + 2);
		ref Color reference = ref colorPalette[0];
		Bgr565 bgr = new Bgr565
		{
			PackedValue = num
		};
		reference = new Color(bgr.ToVector3());
		ref Color reference2 = ref colorPalette[1];
		Bgr565 bgr2 = new Bgr565
		{
			PackedValue = num2
		};
		reference2 = new Color(bgr2.ToVector3());
		if (num > num2 || !isDxt1)
		{
			ref Color reference3 = ref colorPalette[2];
			reference3 = Color.Lerp(colorPalette[0], colorPalette[1], 1f / 3f);
			ref Color reference4 = ref colorPalette[3];
			reference4 = Color.Lerp(colorPalette[0], colorPalette[1], 2f / 3f);
		}
		else
		{
			ref Color reference5 = ref colorPalette[2];
			reference5 = Color.Lerp(colorPalette[0], colorPalette[1], 0.5f);
			ref Color reference6 = ref colorPalette[3];
			reference6 = Color.Transparent;
		}
		uint num3 = Read32(source, sourceOffset + 4);
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				uint num4 = num3 & 3u;
				num3 >>= 2;
				ref Color reference7 = ref result[resultOffset + i * width + j];
				reference7 = colorPalette[num4];
			}
		}
	}

	private void DecodeExplicitAlphaBlock(byte[] source, int sourceOffset, Color[] result, int resultOffset)
	{
		for (int i = 0; i < 4; i++)
		{
			ushort num = Read16(source, sourceOffset + i * 2);
			for (int j = 0; j < 4; j++)
			{
				int num2 = num & 0xF;
				num >>= 4;
				result[resultOffset + i * width + j].A = (byte)(num2 * 255 / 15);
			}
		}
	}

	private void DecodeInterpolatedAlphaBlock(byte[] source, int sourceOffset, Color[] result, int resultOffset)
	{
		byte b = source[sourceOffset];
		byte b2 = source[sourceOffset + 1];
		alphaPalette[0] = b;
		alphaPalette[1] = b2;
		if (b > b2)
		{
			alphaPalette[2] = (byte)((6 * b + b2 + 3) / 7);
			alphaPalette[3] = (byte)((5 * b + 2 * b2 + 3) / 7);
			alphaPalette[4] = (byte)((4 * b + 3 * b2 + 3) / 7);
			alphaPalette[5] = (byte)((3 * b + 4 * b2 + 3) / 7);
			alphaPalette[6] = (byte)((2 * b + 5 * b2 + 3) / 7);
			alphaPalette[7] = (byte)((b + 6 * b2 + 3) / 7);
		}
		else
		{
			alphaPalette[2] = (byte)((4 * b + b2 + 2) / 5);
			alphaPalette[3] = (byte)((3 * b + 2 * b2 + 2) / 5);
			alphaPalette[4] = (byte)((2 * b + 3 * b2 + 2) / 5);
			alphaPalette[5] = (byte)((b + 4 * b2 + 2) / 5);
			alphaPalette[6] = 0;
			alphaPalette[7] = byte.MaxValue;
		}
		ulong num = Read48(source, sourceOffset + 2);
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				int num2 = (int)num & 7;
				num >>= 3;
				result[resultOffset + i * width + j].A = alphaPalette[num2];
			}
		}
	}

	private static ushort Read16(byte[] source, int offset)
	{
		return (ushort)(source[offset] | (source[offset + 1] << 8));
	}

	private static uint Read32(byte[] source, int offset)
	{
		return (uint)(Read16(source, offset) | (Read16(source, offset + 2) << 16));
	}

	private static ulong Read48(byte[] source, int offset)
	{
		return Read16(source, offset) | ((ulong)Read16(source, offset + 2) << 16) | ((ulong)Read16(source, offset + 4) << 32);
	}
}
