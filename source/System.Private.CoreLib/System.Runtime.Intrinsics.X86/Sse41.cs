using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Sse41 : Ssse3
{
	[Intrinsic]
	public new abstract class X64 : Ssse3.X64
	{
		public new static bool IsSupported => IsSupported;

		public static long Extract(Vector128<long> value, byte index)
		{
			return Extract(value, index);
		}

		public static ulong Extract(Vector128<ulong> value, byte index)
		{
			return Extract(value, index);
		}

		public static Vector128<long> Insert(Vector128<long> value, long data, byte index)
		{
			return Insert(value, data, index);
		}

		public static Vector128<ulong> Insert(Vector128<ulong> value, ulong data, byte index)
		{
			return Insert(value, data, index);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<short> Blend(Vector128<short> left, Vector128<short> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector128<ushort> Blend(Vector128<ushort> left, Vector128<ushort> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector128<float> Blend(Vector128<float> left, Vector128<float> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector128<double> Blend(Vector128<double> left, Vector128<double> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector128<sbyte> BlendVariable(Vector128<sbyte> left, Vector128<sbyte> right, Vector128<sbyte> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<byte> BlendVariable(Vector128<byte> left, Vector128<byte> right, Vector128<byte> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<short> BlendVariable(Vector128<short> left, Vector128<short> right, Vector128<short> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<ushort> BlendVariable(Vector128<ushort> left, Vector128<ushort> right, Vector128<ushort> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<int> BlendVariable(Vector128<int> left, Vector128<int> right, Vector128<int> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<uint> BlendVariable(Vector128<uint> left, Vector128<uint> right, Vector128<uint> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<long> BlendVariable(Vector128<long> left, Vector128<long> right, Vector128<long> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<ulong> BlendVariable(Vector128<ulong> left, Vector128<ulong> right, Vector128<ulong> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<float> BlendVariable(Vector128<float> left, Vector128<float> right, Vector128<float> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<double> BlendVariable(Vector128<double> left, Vector128<double> right, Vector128<double> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector128<float> Ceiling(Vector128<float> value)
	{
		return Ceiling(value);
	}

	public static Vector128<double> Ceiling(Vector128<double> value)
	{
		return Ceiling(value);
	}

	public static Vector128<double> CeilingScalar(Vector128<double> value)
	{
		return CeilingScalar(value);
	}

	public static Vector128<float> CeilingScalar(Vector128<float> value)
	{
		return CeilingScalar(value);
	}

	public static Vector128<double> CeilingScalar(Vector128<double> upper, Vector128<double> value)
	{
		return CeilingScalar(upper, value);
	}

	public static Vector128<float> CeilingScalar(Vector128<float> upper, Vector128<float> value)
	{
		return CeilingScalar(upper, value);
	}

	public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<short> ConvertToVector128Int16(Vector128<sbyte> value)
	{
		return ConvertToVector128Int16(value);
	}

	public static Vector128<short> ConvertToVector128Int16(Vector128<byte> value)
	{
		return ConvertToVector128Int16(value);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<sbyte> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<byte> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<short> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<ushort> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<sbyte> value)
	{
		return ConvertToVector128Int64(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<byte> value)
	{
		return ConvertToVector128Int64(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<short> value)
	{
		return ConvertToVector128Int64(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<ushort> value)
	{
		return ConvertToVector128Int64(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<int> value)
	{
		return ConvertToVector128Int64(value);
	}

	public static Vector128<long> ConvertToVector128Int64(Vector128<uint> value)
	{
		return ConvertToVector128Int64(value);
	}

	public unsafe static Vector128<short> ConvertToVector128Int16(sbyte* address)
	{
		return ConvertToVector128Int16(address);
	}

	public unsafe static Vector128<short> ConvertToVector128Int16(byte* address)
	{
		return ConvertToVector128Int16(address);
	}

	public unsafe static Vector128<int> ConvertToVector128Int32(sbyte* address)
	{
		return ConvertToVector128Int32(address);
	}

	public unsafe static Vector128<int> ConvertToVector128Int32(byte* address)
	{
		return ConvertToVector128Int32(address);
	}

	public unsafe static Vector128<int> ConvertToVector128Int32(short* address)
	{
		return ConvertToVector128Int32(address);
	}

	public unsafe static Vector128<int> ConvertToVector128Int32(ushort* address)
	{
		return ConvertToVector128Int32(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(sbyte* address)
	{
		return ConvertToVector128Int64(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(byte* address)
	{
		return ConvertToVector128Int64(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(short* address)
	{
		return ConvertToVector128Int64(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(ushort* address)
	{
		return ConvertToVector128Int64(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(int* address)
	{
		return ConvertToVector128Int64(address);
	}

	public unsafe static Vector128<long> ConvertToVector128Int64(uint* address)
	{
		return ConvertToVector128Int64(address);
	}

	public static Vector128<float> DotProduct(Vector128<float> left, Vector128<float> right, byte control)
	{
		return DotProduct(left, right, control);
	}

	public static Vector128<double> DotProduct(Vector128<double> left, Vector128<double> right, byte control)
	{
		return DotProduct(left, right, control);
	}

	public static byte Extract(Vector128<byte> value, byte index)
	{
		return Extract(value, index);
	}

	public static int Extract(Vector128<int> value, byte index)
	{
		return Extract(value, index);
	}

	public static uint Extract(Vector128<uint> value, byte index)
	{
		return Extract(value, index);
	}

	public static float Extract(Vector128<float> value, byte index)
	{
		return Extract(value, index);
	}

	public static Vector128<float> Floor(Vector128<float> value)
	{
		return Floor(value);
	}

	public static Vector128<double> Floor(Vector128<double> value)
	{
		return Floor(value);
	}

	public static Vector128<double> FloorScalar(Vector128<double> value)
	{
		return FloorScalar(value);
	}

	public static Vector128<float> FloorScalar(Vector128<float> value)
	{
		return FloorScalar(value);
	}

	public static Vector128<double> FloorScalar(Vector128<double> upper, Vector128<double> value)
	{
		return FloorScalar(upper, value);
	}

	public static Vector128<float> FloorScalar(Vector128<float> upper, Vector128<float> value)
	{
		return FloorScalar(upper, value);
	}

	public static Vector128<sbyte> Insert(Vector128<sbyte> value, sbyte data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<byte> Insert(Vector128<byte> value, byte data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<int> Insert(Vector128<int> value, int data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<uint> Insert(Vector128<uint> value, uint data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<float> Insert(Vector128<float> value, Vector128<float> data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<sbyte> Max(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Max(left, right);
	}

	public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Max(left, right);
	}

	public static Vector128<int> Max(Vector128<int> left, Vector128<int> right)
	{
		return Max(left, right);
	}

	public static Vector128<uint> Max(Vector128<uint> left, Vector128<uint> right)
	{
		return Max(left, right);
	}

	public static Vector128<sbyte> Min(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Min(left, right);
	}

	public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Min(left, right);
	}

	public static Vector128<int> Min(Vector128<int> left, Vector128<int> right)
	{
		return Min(left, right);
	}

	public static Vector128<uint> Min(Vector128<uint> left, Vector128<uint> right)
	{
		return Min(left, right);
	}

	public static Vector128<ushort> MinHorizontal(Vector128<ushort> value)
	{
		return MinHorizontal(value);
	}

	public static Vector128<ushort> MultipleSumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right, byte mask)
	{
		return MultipleSumAbsoluteDifferences(left, right, mask);
	}

	public static Vector128<long> Multiply(Vector128<int> left, Vector128<int> right)
	{
		return Multiply(left, right);
	}

	public static Vector128<int> MultiplyLow(Vector128<int> left, Vector128<int> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector128<uint> MultiplyLow(Vector128<uint> left, Vector128<uint> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector128<ushort> PackUnsignedSaturate(Vector128<int> left, Vector128<int> right)
	{
		return PackUnsignedSaturate(left, right);
	}

	public static Vector128<float> RoundToNearestInteger(Vector128<float> value)
	{
		return RoundToNearestInteger(value);
	}

	public static Vector128<float> RoundToNegativeInfinity(Vector128<float> value)
	{
		return RoundToNegativeInfinity(value);
	}

	public static Vector128<float> RoundToPositiveInfinity(Vector128<float> value)
	{
		return RoundToPositiveInfinity(value);
	}

	public static Vector128<float> RoundToZero(Vector128<float> value)
	{
		return RoundToZero(value);
	}

	public static Vector128<float> RoundCurrentDirection(Vector128<float> value)
	{
		return RoundCurrentDirection(value);
	}

	public static Vector128<double> RoundToNearestInteger(Vector128<double> value)
	{
		return RoundToNearestInteger(value);
	}

	public static Vector128<double> RoundToNegativeInfinity(Vector128<double> value)
	{
		return RoundToNegativeInfinity(value);
	}

	public static Vector128<double> RoundToPositiveInfinity(Vector128<double> value)
	{
		return RoundToPositiveInfinity(value);
	}

	public static Vector128<double> RoundToZero(Vector128<double> value)
	{
		return RoundToZero(value);
	}

	public static Vector128<double> RoundCurrentDirection(Vector128<double> value)
	{
		return RoundCurrentDirection(value);
	}

	public static Vector128<double> RoundCurrentDirectionScalar(Vector128<double> value)
	{
		return RoundCurrentDirectionScalar(value);
	}

	public static Vector128<double> RoundToNearestIntegerScalar(Vector128<double> value)
	{
		return RoundToNearestIntegerScalar(value);
	}

	public static Vector128<double> RoundToNegativeInfinityScalar(Vector128<double> value)
	{
		return RoundToNegativeInfinityScalar(value);
	}

	public static Vector128<double> RoundToPositiveInfinityScalar(Vector128<double> value)
	{
		return RoundToPositiveInfinityScalar(value);
	}

	public static Vector128<double> RoundToZeroScalar(Vector128<double> value)
	{
		return RoundToZeroScalar(value);
	}

	public static Vector128<double> RoundCurrentDirectionScalar(Vector128<double> upper, Vector128<double> value)
	{
		return RoundCurrentDirectionScalar(upper, value);
	}

	public static Vector128<double> RoundToNearestIntegerScalar(Vector128<double> upper, Vector128<double> value)
	{
		return RoundToNearestIntegerScalar(upper, value);
	}

	public static Vector128<double> RoundToNegativeInfinityScalar(Vector128<double> upper, Vector128<double> value)
	{
		return RoundToNegativeInfinityScalar(upper, value);
	}

	public static Vector128<double> RoundToPositiveInfinityScalar(Vector128<double> upper, Vector128<double> value)
	{
		return RoundToPositiveInfinityScalar(upper, value);
	}

	public static Vector128<double> RoundToZeroScalar(Vector128<double> upper, Vector128<double> value)
	{
		return RoundToZeroScalar(upper, value);
	}

	public static Vector128<float> RoundCurrentDirectionScalar(Vector128<float> value)
	{
		return RoundCurrentDirectionScalar(value);
	}

	public static Vector128<float> RoundToNearestIntegerScalar(Vector128<float> value)
	{
		return RoundToNearestIntegerScalar(value);
	}

	public static Vector128<float> RoundToNegativeInfinityScalar(Vector128<float> value)
	{
		return RoundToNegativeInfinityScalar(value);
	}

	public static Vector128<float> RoundToPositiveInfinityScalar(Vector128<float> value)
	{
		return RoundToPositiveInfinityScalar(value);
	}

	public static Vector128<float> RoundToZeroScalar(Vector128<float> value)
	{
		return RoundToZeroScalar(value);
	}

	public static Vector128<float> RoundCurrentDirectionScalar(Vector128<float> upper, Vector128<float> value)
	{
		return RoundCurrentDirectionScalar(upper, value);
	}

	public static Vector128<float> RoundToNearestIntegerScalar(Vector128<float> upper, Vector128<float> value)
	{
		return RoundToNearestIntegerScalar(upper, value);
	}

	public static Vector128<float> RoundToNegativeInfinityScalar(Vector128<float> upper, Vector128<float> value)
	{
		return RoundToNegativeInfinityScalar(upper, value);
	}

	public static Vector128<float> RoundToPositiveInfinityScalar(Vector128<float> upper, Vector128<float> value)
	{
		return RoundToPositiveInfinityScalar(upper, value);
	}

	public static Vector128<float> RoundToZeroScalar(Vector128<float> upper, Vector128<float> value)
	{
		return RoundToZeroScalar(upper, value);
	}

	public unsafe static Vector128<sbyte> LoadAlignedVector128NonTemporal(sbyte* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<byte> LoadAlignedVector128NonTemporal(byte* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<short> LoadAlignedVector128NonTemporal(short* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<ushort> LoadAlignedVector128NonTemporal(ushort* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<int> LoadAlignedVector128NonTemporal(int* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<uint> LoadAlignedVector128NonTemporal(uint* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<long> LoadAlignedVector128NonTemporal(long* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public unsafe static Vector128<ulong> LoadAlignedVector128NonTemporal(ulong* address)
	{
		return LoadAlignedVector128NonTemporal(address);
	}

	public static bool TestC(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<byte> left, Vector128<byte> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<short> left, Vector128<short> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<ushort> left, Vector128<ushort> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<int> left, Vector128<int> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<uint> left, Vector128<uint> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<long> left, Vector128<long> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<ulong> left, Vector128<ulong> right)
	{
		return TestC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<byte> left, Vector128<byte> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<short> left, Vector128<short> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<ushort> left, Vector128<ushort> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<int> left, Vector128<int> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<uint> left, Vector128<uint> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<long> left, Vector128<long> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<ulong> left, Vector128<ulong> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestZ(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<byte> left, Vector128<byte> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<short> left, Vector128<short> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<ushort> left, Vector128<ushort> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<int> left, Vector128<int> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<uint> left, Vector128<uint> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<long> left, Vector128<long> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<ulong> left, Vector128<ulong> right)
	{
		return TestZ(left, right);
	}
}
