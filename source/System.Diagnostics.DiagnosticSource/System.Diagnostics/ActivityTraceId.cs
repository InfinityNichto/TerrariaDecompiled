using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

public readonly struct ActivityTraceId : IEquatable<ActivityTraceId>
{
	private readonly string _hexString;

	internal ActivityTraceId(string hexString)
	{
		_hexString = hexString;
	}

	public static ActivityTraceId CreateRandom()
	{
		Span<byte> span = stackalloc byte[16];
		SetToRandomBytes(span);
		return CreateFromBytes(span);
	}

	public static ActivityTraceId CreateFromBytes(ReadOnlySpan<byte> idData)
	{
		if (idData.Length != 16)
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		return new ActivityTraceId(System.HexConverter.ToString(idData, System.HexConverter.Casing.Lower));
	}

	public static ActivityTraceId CreateFromUtf8String(ReadOnlySpan<byte> idData)
	{
		return new ActivityTraceId(idData);
	}

	public static ActivityTraceId CreateFromString(ReadOnlySpan<char> idData)
	{
		if (idData.Length != 32 || !IsLowerCaseHexAndNotAllZeros(idData))
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		return new ActivityTraceId(idData.ToString());
	}

	public string ToHexString()
	{
		return _hexString ?? "00000000000000000000000000000000";
	}

	public override string ToString()
	{
		return ToHexString();
	}

	public static bool operator ==(ActivityTraceId traceId1, ActivityTraceId traceId2)
	{
		return traceId1._hexString == traceId2._hexString;
	}

	public static bool operator !=(ActivityTraceId traceId1, ActivityTraceId traceId2)
	{
		return traceId1._hexString != traceId2._hexString;
	}

	public bool Equals(ActivityTraceId traceId)
	{
		return _hexString == traceId._hexString;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ActivityTraceId activityTraceId)
		{
			return _hexString == activityTraceId._hexString;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ToHexString().GetHashCode();
	}

	private ActivityTraceId(ReadOnlySpan<byte> idData)
	{
		if (idData.Length != 32)
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		Span<ulong> span = stackalloc ulong[2];
		if (!Utf8Parser.TryParse(idData.Slice(0, 16), out span[0], out int bytesConsumed, 'x'))
		{
			_hexString = CreateRandom()._hexString;
			return;
		}
		if (!Utf8Parser.TryParse(idData.Slice(16, 16), out span[1], out bytesConsumed, 'x'))
		{
			_hexString = CreateRandom()._hexString;
			return;
		}
		if (BitConverter.IsLittleEndian)
		{
			span[0] = BinaryPrimitives.ReverseEndianness(span[0]);
			span[1] = BinaryPrimitives.ReverseEndianness(span[1]);
		}
		_hexString = System.HexConverter.ToString(MemoryMarshal.AsBytes(span), System.HexConverter.Casing.Lower);
	}

	public void CopyTo(Span<byte> destination)
	{
		SetSpanFromHexChars(ToHexString().AsSpan(), destination);
	}

	internal static void SetToRandomBytes(Span<byte> outBytes)
	{
		RandomNumberGenerator current = RandomNumberGenerator.Current;
		Unsafe.WriteUnaligned(ref outBytes[0], current.Next());
		if (outBytes.Length == 16)
		{
			Unsafe.WriteUnaligned(ref outBytes[8], current.Next());
		}
	}

	internal static void SetSpanFromHexChars(ReadOnlySpan<char> charData, Span<byte> outBytes)
	{
		for (int i = 0; i < outBytes.Length; i++)
		{
			outBytes[i] = HexByteFromChars(charData[i * 2], charData[i * 2 + 1]);
		}
	}

	internal static byte HexByteFromChars(char char1, char char2)
	{
		int num = System.HexConverter.FromLowerChar(char1);
		int num2 = System.HexConverter.FromLowerChar(char2);
		if ((num | num2) == 255)
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		return (byte)((num << 4) | num2);
	}

	internal static bool IsLowerCaseHexAndNotAllZeros(ReadOnlySpan<char> idData)
	{
		bool result = false;
		for (int i = 0; i < idData.Length; i++)
		{
			char c = idData[i];
			if (!System.HexConverter.IsHexLowerChar(c))
			{
				return false;
			}
			if (c != '0')
			{
				result = true;
			}
		}
		return result;
	}
}
