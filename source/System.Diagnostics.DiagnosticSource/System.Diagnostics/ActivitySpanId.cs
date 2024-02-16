using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Diagnostics;

public readonly struct ActivitySpanId : IEquatable<ActivitySpanId>
{
	private readonly string _hexString;

	internal ActivitySpanId(string hexString)
	{
		_hexString = hexString;
	}

	public unsafe static ActivitySpanId CreateRandom()
	{
		Unsafe.SkipInit(out ulong num);
		ActivityTraceId.SetToRandomBytes(new Span<byte>(&num, 8));
		return new ActivitySpanId(System.HexConverter.ToString(new ReadOnlySpan<byte>(&num, 8), System.HexConverter.Casing.Lower));
	}

	public static ActivitySpanId CreateFromBytes(ReadOnlySpan<byte> idData)
	{
		if (idData.Length != 8)
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		return new ActivitySpanId(System.HexConverter.ToString(idData, System.HexConverter.Casing.Lower));
	}

	public static ActivitySpanId CreateFromUtf8String(ReadOnlySpan<byte> idData)
	{
		return new ActivitySpanId(idData);
	}

	public static ActivitySpanId CreateFromString(ReadOnlySpan<char> idData)
	{
		if (idData.Length != 16 || !ActivityTraceId.IsLowerCaseHexAndNotAllZeros(idData))
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		return new ActivitySpanId(idData.ToString());
	}

	public string ToHexString()
	{
		return _hexString ?? "0000000000000000";
	}

	public override string ToString()
	{
		return ToHexString();
	}

	public static bool operator ==(ActivitySpanId spanId1, ActivitySpanId spandId2)
	{
		return spanId1._hexString == spandId2._hexString;
	}

	public static bool operator !=(ActivitySpanId spanId1, ActivitySpanId spandId2)
	{
		return spanId1._hexString != spandId2._hexString;
	}

	public bool Equals(ActivitySpanId spanId)
	{
		return _hexString == spanId._hexString;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ActivitySpanId activitySpanId)
		{
			return _hexString == activitySpanId._hexString;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ToHexString().GetHashCode();
	}

	private unsafe ActivitySpanId(ReadOnlySpan<byte> idData)
	{
		if (idData.Length != 16)
		{
			throw new ArgumentOutOfRangeException("idData");
		}
		if (!Utf8Parser.TryParse(idData, out ulong value, out int _, 'x'))
		{
			_hexString = CreateRandom()._hexString;
			return;
		}
		if (BitConverter.IsLittleEndian)
		{
			value = BinaryPrimitives.ReverseEndianness(value);
		}
		_hexString = System.HexConverter.ToString(new ReadOnlySpan<byte>(&value, 8), System.HexConverter.Casing.Lower);
	}

	public void CopyTo(Span<byte> destination)
	{
		ActivityTraceId.SetSpanFromHexChars(ToHexString().AsSpan(), destination);
	}
}
