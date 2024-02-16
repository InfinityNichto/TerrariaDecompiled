using System.Numerics;

namespace System;

internal struct Sha1ForNonSecretPurposes
{
	private long _length;

	private uint[] _w;

	private int _pos;

	public void Start()
	{
		if (_w == null)
		{
			_w = new uint[85];
		}
		_length = 0L;
		_pos = 0;
		_w[80] = 1732584193u;
		_w[81] = 4023233417u;
		_w[82] = 2562383102u;
		_w[83] = 271733878u;
		_w[84] = 3285377520u;
	}

	public void Append(byte input)
	{
		int num = _pos >> 2;
		_w[num] = (_w[num] << 8) | input;
		if (64 == ++_pos)
		{
			Drain();
		}
	}

	public void Append(ReadOnlySpan<byte> input)
	{
		ReadOnlySpan<byte> readOnlySpan = input;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte input2 = readOnlySpan[i];
			Append(input2);
		}
	}

	public void Finish(Span<byte> output)
	{
		long num = _length + 8 * _pos;
		Append(128);
		while (_pos != 56)
		{
			Append(0);
		}
		Append((byte)(num >> 56));
		Append((byte)(num >> 48));
		Append((byte)(num >> 40));
		Append((byte)(num >> 32));
		Append((byte)(num >> 24));
		Append((byte)(num >> 16));
		Append((byte)(num >> 8));
		Append((byte)num);
		int num2 = ((output.Length < 20) ? output.Length : 20);
		for (int i = 0; i != num2; i++)
		{
			uint num3 = _w[80 + i / 4];
			output[i] = (byte)(num3 >> 24);
			_w[80 + i / 4] = num3 << 8;
		}
	}

	private void Drain()
	{
		for (int i = 16; i != 80; i++)
		{
			_w[i] = BitOperations.RotateLeft(_w[i - 3] ^ _w[i - 8] ^ _w[i - 14] ^ _w[i - 16], 1);
		}
		uint num = _w[80];
		uint num2 = _w[81];
		uint num3 = _w[82];
		uint num4 = _w[83];
		uint num5 = _w[84];
		for (int j = 0; j != 20; j++)
		{
			uint num6 = (num2 & num3) | (~num2 & num4);
			uint num7 = BitOperations.RotateLeft(num, 5) + num6 + num5 + 1518500249 + _w[j];
			num5 = num4;
			num4 = num3;
			num3 = BitOperations.RotateLeft(num2, 30);
			num2 = num;
			num = num7;
		}
		for (int k = 20; k != 40; k++)
		{
			uint num8 = num2 ^ num3 ^ num4;
			uint num9 = BitOperations.RotateLeft(num, 5) + num8 + num5 + 1859775393 + _w[k];
			num5 = num4;
			num4 = num3;
			num3 = BitOperations.RotateLeft(num2, 30);
			num2 = num;
			num = num9;
		}
		for (int l = 40; l != 60; l++)
		{
			uint num10 = (num2 & num3) | (num2 & num4) | (num3 & num4);
			uint num11 = (uint)((int)(BitOperations.RotateLeft(num, 5) + num10 + num5) + -1894007588) + _w[l];
			num5 = num4;
			num4 = num3;
			num3 = BitOperations.RotateLeft(num2, 30);
			num2 = num;
			num = num11;
		}
		for (int m = 60; m != 80; m++)
		{
			uint num12 = num2 ^ num3 ^ num4;
			uint num13 = (uint)((int)(BitOperations.RotateLeft(num, 5) + num12 + num5) + -899497514) + _w[m];
			num5 = num4;
			num4 = num3;
			num3 = BitOperations.RotateLeft(num2, 30);
			num2 = num;
			num = num13;
		}
		_w[80] += num;
		_w[81] += num2;
		_w[82] += num3;
		_w[83] += num4;
		_w[84] += num5;
		_length += 512L;
		_pos = 0;
	}
}
