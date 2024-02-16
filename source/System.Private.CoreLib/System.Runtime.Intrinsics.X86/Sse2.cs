using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Sse2 : Sse
{
	[Intrinsic]
	public new abstract class X64 : Sse.X64
	{
		public new static bool IsSupported => IsSupported;

		public static long ConvertToInt64(Vector128<double> value)
		{
			return ConvertToInt64(value);
		}

		public static long ConvertToInt64(Vector128<long> value)
		{
			return ConvertToInt64(value);
		}

		public static ulong ConvertToUInt64(Vector128<ulong> value)
		{
			return ConvertToUInt64(value);
		}

		public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value)
		{
			return ConvertScalarToVector128Double(upper, value);
		}

		public static Vector128<long> ConvertScalarToVector128Int64(long value)
		{
			return ConvertScalarToVector128Int64(value);
		}

		public static Vector128<ulong> ConvertScalarToVector128UInt64(ulong value)
		{
			return ConvertScalarToVector128UInt64(value);
		}

		public static long ConvertToInt64WithTruncation(Vector128<double> value)
		{
			return ConvertToInt64WithTruncation(value);
		}

		public unsafe static void StoreNonTemporal(long* address, long value)
		{
			StoreNonTemporal(address, value);
		}

		public unsafe static void StoreNonTemporal(ulong* address, ulong value)
		{
			StoreNonTemporal(address, value);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right)
	{
		return Add(left, right);
	}

	public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Add(left, right);
	}

	public static Vector128<short> Add(Vector128<short> left, Vector128<short> right)
	{
		return Add(left, right);
	}

	public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Add(left, right);
	}

	public static Vector128<int> Add(Vector128<int> left, Vector128<int> right)
	{
		return Add(left, right);
	}

	public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right)
	{
		return Add(left, right);
	}

	public static Vector128<long> Add(Vector128<long> left, Vector128<long> right)
	{
		return Add(left, right);
	}

	public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right)
	{
		return Add(left, right);
	}

	public static Vector128<double> Add(Vector128<double> left, Vector128<double> right)
	{
		return Add(left, right);
	}

	public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right)
	{
		return AddScalar(left, right);
	}

	public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right)
	{
		return AddSaturate(left, right);
	}

	public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right)
	{
		return And(left, right);
	}

	public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return And(left, right);
	}

	public static Vector128<short> And(Vector128<short> left, Vector128<short> right)
	{
		return And(left, right);
	}

	public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right)
	{
		return And(left, right);
	}

	public static Vector128<int> And(Vector128<int> left, Vector128<int> right)
	{
		return And(left, right);
	}

	public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right)
	{
		return And(left, right);
	}

	public static Vector128<long> And(Vector128<long> left, Vector128<long> right)
	{
		return And(left, right);
	}

	public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right)
	{
		return And(left, right);
	}

	public static Vector128<double> And(Vector128<double> left, Vector128<double> right)
	{
		return And(left, right);
	}

	public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<byte> Average(Vector128<byte> left, Vector128<byte> right)
	{
		return Average(left, right);
	}

	public static Vector128<ushort> Average(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Average(left, right);
	}

	public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right)
	{
		return CompareEqual(left, right);
	}

	public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareEqual(left, right);
	}

	public static bool CompareScalarOrderedEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedEqual(left, right);
	}

	public static bool CompareScalarUnorderedEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedEqual(left, right);
	}

	public static Vector128<double> CompareScalarEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarEqual(left, right);
	}

	public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static bool CompareScalarOrderedGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedGreaterThan(left, right);
	}

	public static bool CompareScalarUnorderedGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedGreaterThan(left, right);
	}

	public static Vector128<double> CompareScalarGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarGreaterThan(left, right);
	}

	public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedGreaterThanOrEqual(left, right);
	}

	public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedGreaterThanOrEqual(left, right);
	}

	public static Vector128<double> CompareScalarGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarGreaterThanOrEqual(left, right);
	}

	public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right)
	{
		return CompareLessThan(left, right);
	}

	public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareLessThan(left, right);
	}

	public static bool CompareScalarOrderedLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedLessThan(left, right);
	}

	public static bool CompareScalarUnorderedLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedLessThan(left, right);
	}

	public static Vector128<double> CompareScalarLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarLessThan(left, right);
	}

	public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static bool CompareScalarOrderedLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedLessThanOrEqual(left, right);
	}

	public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedLessThanOrEqual(left, right);
	}

	public static Vector128<double> CompareScalarLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarLessThanOrEqual(left, right);
	}

	public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareNotEqual(left, right);
	}

	public static bool CompareScalarOrderedNotEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrderedNotEqual(left, right);
	}

	public static bool CompareScalarUnorderedNotEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnorderedNotEqual(left, right);
	}

	public static Vector128<double> CompareScalarNotEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarNotEqual(left, right);
	}

	public static Vector128<double> CompareNotGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareNotGreaterThan(left, right);
	}

	public static Vector128<double> CompareScalarNotGreaterThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarNotGreaterThan(left, right);
	}

	public static Vector128<double> CompareNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareNotGreaterThanOrEqual(left, right);
	}

	public static Vector128<double> CompareScalarNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarNotGreaterThanOrEqual(left, right);
	}

	public static Vector128<double> CompareNotLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareNotLessThan(left, right);
	}

	public static Vector128<double> CompareScalarNotLessThan(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarNotLessThan(left, right);
	}

	public static Vector128<double> CompareNotLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareNotLessThanOrEqual(left, right);
	}

	public static Vector128<double> CompareScalarNotLessThanOrEqual(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarNotLessThanOrEqual(left, right);
	}

	public static Vector128<double> CompareOrdered(Vector128<double> left, Vector128<double> right)
	{
		return CompareOrdered(left, right);
	}

	public static Vector128<double> CompareScalarOrdered(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarOrdered(left, right);
	}

	public static Vector128<double> CompareUnordered(Vector128<double> left, Vector128<double> right)
	{
		return CompareUnordered(left, right);
	}

	public static Vector128<double> CompareScalarUnordered(Vector128<double> left, Vector128<double> right)
	{
		return CompareScalarUnordered(left, right);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<float> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector128<double> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<float> ConvertToVector128Single(Vector128<int> value)
	{
		return ConvertToVector128Single(value);
	}

	public static Vector128<float> ConvertToVector128Single(Vector128<double> value)
	{
		return ConvertToVector128Single(value);
	}

	public static Vector128<double> ConvertToVector128Double(Vector128<int> value)
	{
		return ConvertToVector128Double(value);
	}

	public static Vector128<double> ConvertToVector128Double(Vector128<float> value)
	{
		return ConvertToVector128Double(value);
	}

	public static int ConvertToInt32(Vector128<double> value)
	{
		return ConvertToInt32(value);
	}

	public static int ConvertToInt32(Vector128<int> value)
	{
		return ConvertToInt32(value);
	}

	public static uint ConvertToUInt32(Vector128<uint> value)
	{
		return ConvertToUInt32(value);
	}

	public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, int value)
	{
		return ConvertScalarToVector128Double(upper, value);
	}

	public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, Vector128<float> value)
	{
		return ConvertScalarToVector128Double(upper, value);
	}

	public static Vector128<int> ConvertScalarToVector128Int32(int value)
	{
		return ConvertScalarToVector128Int32(value);
	}

	public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value)
	{
		return ConvertScalarToVector128Single(upper, value);
	}

	public static Vector128<uint> ConvertScalarToVector128UInt32(uint value)
	{
		return ConvertScalarToVector128UInt32(value);
	}

	public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<float> value)
	{
		return ConvertToVector128Int32WithTruncation(value);
	}

	public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<double> value)
	{
		return ConvertToVector128Int32WithTruncation(value);
	}

	public static int ConvertToInt32WithTruncation(Vector128<double> value)
	{
		return ConvertToInt32WithTruncation(value);
	}

	public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right)
	{
		return Divide(left, right);
	}

	public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right)
	{
		return DivideScalar(left, right);
	}

	public static ushort Extract(Vector128<ushort> value, byte index)
	{
		return Extract(value, index);
	}

	public static Vector128<short> Insert(Vector128<short> value, short data, byte index)
	{
		return Insert(value, data, index);
	}

	public static Vector128<ushort> Insert(Vector128<ushort> value, ushort data, byte index)
	{
		return Insert(value, data, index);
	}

	public unsafe static Vector128<sbyte> LoadVector128(sbyte* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<byte> LoadVector128(byte* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<short> LoadVector128(short* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<ushort> LoadVector128(ushort* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<int> LoadVector128(int* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<uint> LoadVector128(uint* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<long> LoadVector128(long* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<ulong> LoadVector128(ulong* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<double> LoadVector128(double* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<double> LoadScalarVector128(double* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static Vector128<sbyte> LoadAlignedVector128(sbyte* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<byte> LoadAlignedVector128(byte* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<short> LoadAlignedVector128(short* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<ushort> LoadAlignedVector128(ushort* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<int> LoadAlignedVector128(int* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<uint> LoadAlignedVector128(uint* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<long> LoadAlignedVector128(long* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<ulong> LoadAlignedVector128(ulong* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<double> LoadAlignedVector128(double* address)
	{
		return LoadAlignedVector128(address);
	}

	public static void LoadFence()
	{
		LoadFence();
	}

	public unsafe static Vector128<double> LoadHigh(Vector128<double> lower, double* address)
	{
		return LoadHigh(lower, address);
	}

	public unsafe static Vector128<double> LoadLow(Vector128<double> upper, double* address)
	{
		return LoadLow(upper, address);
	}

	public unsafe static Vector128<int> LoadScalarVector128(int* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static Vector128<uint> LoadScalarVector128(uint* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static Vector128<long> LoadScalarVector128(long* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static Vector128<ulong> LoadScalarVector128(ulong* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static void MaskMove(Vector128<sbyte> source, Vector128<sbyte> mask, sbyte* address)
	{
		MaskMove(source, mask, address);
	}

	public unsafe static void MaskMove(Vector128<byte> source, Vector128<byte> mask, byte* address)
	{
		MaskMove(source, mask, address);
	}

	public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right)
	{
		return Max(left, right);
	}

	public static Vector128<short> Max(Vector128<short> left, Vector128<short> right)
	{
		return Max(left, right);
	}

	public static Vector128<double> Max(Vector128<double> left, Vector128<double> right)
	{
		return Max(left, right);
	}

	public static Vector128<double> MaxScalar(Vector128<double> left, Vector128<double> right)
	{
		return MaxScalar(left, right);
	}

	public static void MemoryFence()
	{
		MemoryFence();
	}

	public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right)
	{
		return Min(left, right);
	}

	public static Vector128<short> Min(Vector128<short> left, Vector128<short> right)
	{
		return Min(left, right);
	}

	public static Vector128<double> Min(Vector128<double> left, Vector128<double> right)
	{
		return Min(left, right);
	}

	public static Vector128<double> MinScalar(Vector128<double> left, Vector128<double> right)
	{
		return MinScalar(left, right);
	}

	public static Vector128<double> MoveScalar(Vector128<double> upper, Vector128<double> value)
	{
		return MoveScalar(upper, value);
	}

	public static int MoveMask(Vector128<sbyte> value)
	{
		return MoveMask(value);
	}

	public static int MoveMask(Vector128<byte> value)
	{
		return MoveMask(value);
	}

	public static int MoveMask(Vector128<double> value)
	{
		return MoveMask(value);
	}

	public static Vector128<long> MoveScalar(Vector128<long> value)
	{
		return MoveScalar(value);
	}

	public static Vector128<ulong> MoveScalar(Vector128<ulong> value)
	{
		return MoveScalar(value);
	}

	public static Vector128<ulong> Multiply(Vector128<uint> left, Vector128<uint> right)
	{
		return Multiply(left, right);
	}

	public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right)
	{
		return Multiply(left, right);
	}

	public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right)
	{
		return MultiplyScalar(left, right);
	}

	public static Vector128<short> MultiplyHigh(Vector128<short> left, Vector128<short> right)
	{
		return MultiplyHigh(left, right);
	}

	public static Vector128<ushort> MultiplyHigh(Vector128<ushort> left, Vector128<ushort> right)
	{
		return MultiplyHigh(left, right);
	}

	public static Vector128<int> MultiplyAddAdjacent(Vector128<short> left, Vector128<short> right)
	{
		return MultiplyAddAdjacent(left, right);
	}

	public static Vector128<short> MultiplyLow(Vector128<short> left, Vector128<short> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector128<ushort> MultiplyLow(Vector128<ushort> left, Vector128<ushort> right)
	{
		return MultiplyLow(left, right);
	}

	public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right)
	{
		return Or(left, right);
	}

	public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Or(left, right);
	}

	public static Vector128<short> Or(Vector128<short> left, Vector128<short> right)
	{
		return Or(left, right);
	}

	public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Or(left, right);
	}

	public static Vector128<int> Or(Vector128<int> left, Vector128<int> right)
	{
		return Or(left, right);
	}

	public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right)
	{
		return Or(left, right);
	}

	public static Vector128<long> Or(Vector128<long> left, Vector128<long> right)
	{
		return Or(left, right);
	}

	public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right)
	{
		return Or(left, right);
	}

	public static Vector128<double> Or(Vector128<double> left, Vector128<double> right)
	{
		return Or(left, right);
	}

	public static Vector128<sbyte> PackSignedSaturate(Vector128<short> left, Vector128<short> right)
	{
		return PackSignedSaturate(left, right);
	}

	public static Vector128<short> PackSignedSaturate(Vector128<int> left, Vector128<int> right)
	{
		return PackSignedSaturate(left, right);
	}

	public static Vector128<byte> PackUnsignedSaturate(Vector128<short> left, Vector128<short> right)
	{
		return PackUnsignedSaturate(left, right);
	}

	public static Vector128<ushort> SumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right)
	{
		return SumAbsoluteDifferences(left, right);
	}

	public static Vector128<int> Shuffle(Vector128<int> value, byte control)
	{
		return Shuffle(value, control);
	}

	public static Vector128<uint> Shuffle(Vector128<uint> value, byte control)
	{
		return Shuffle(value, control);
	}

	public static Vector128<double> Shuffle(Vector128<double> left, Vector128<double> right, byte control)
	{
		return Shuffle(left, right, control);
	}

	public static Vector128<short> ShuffleHigh(Vector128<short> value, byte control)
	{
		return ShuffleHigh(value, control);
	}

	public static Vector128<ushort> ShuffleHigh(Vector128<ushort> value, byte control)
	{
		return ShuffleHigh(value, control);
	}

	public static Vector128<short> ShuffleLow(Vector128<short> value, byte control)
	{
		return ShuffleLow(value, control);
	}

	public static Vector128<ushort> ShuffleLow(Vector128<ushort> value, byte control)
	{
		return ShuffleLow(value, control);
	}

	public static Vector128<short> ShiftLeftLogical(Vector128<short> value, Vector128<short> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, Vector128<ushort> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<int> ShiftLeftLogical(Vector128<int> value, Vector128<int> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, Vector128<uint> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<long> ShiftLeftLogical(Vector128<long> value, Vector128<long> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, Vector128<ulong> count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<short> ShiftLeftLogical(Vector128<short> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<int> ShiftLeftLogical(Vector128<int> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<long> ShiftLeftLogical(Vector128<long> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, byte count)
	{
		return ShiftLeftLogical(value, count);
	}

	public static Vector128<sbyte> ShiftLeftLogical128BitLane(Vector128<sbyte> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<byte> ShiftLeftLogical128BitLane(Vector128<byte> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<short> ShiftLeftLogical128BitLane(Vector128<short> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<ushort> ShiftLeftLogical128BitLane(Vector128<ushort> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<int> ShiftLeftLogical128BitLane(Vector128<int> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<uint> ShiftLeftLogical128BitLane(Vector128<uint> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<long> ShiftLeftLogical128BitLane(Vector128<long> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<ulong> ShiftLeftLogical128BitLane(Vector128<ulong> value, byte numBytes)
	{
		return ShiftLeftLogical128BitLane(value, numBytes);
	}

	public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, Vector128<short> count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, Vector128<int> count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, byte count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, byte count)
	{
		return ShiftRightArithmetic(value, count);
	}

	public static Vector128<short> ShiftRightLogical(Vector128<short> value, Vector128<short> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, Vector128<ushort> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<int> ShiftRightLogical(Vector128<int> value, Vector128<int> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, Vector128<uint> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<long> ShiftRightLogical(Vector128<long> value, Vector128<long> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, Vector128<ulong> count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<short> ShiftRightLogical(Vector128<short> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<int> ShiftRightLogical(Vector128<int> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<long> ShiftRightLogical(Vector128<long> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, byte count)
	{
		return ShiftRightLogical(value, count);
	}

	public static Vector128<sbyte> ShiftRightLogical128BitLane(Vector128<sbyte> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<byte> ShiftRightLogical128BitLane(Vector128<byte> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<short> ShiftRightLogical128BitLane(Vector128<short> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<ushort> ShiftRightLogical128BitLane(Vector128<ushort> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<int> ShiftRightLogical128BitLane(Vector128<int> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<uint> ShiftRightLogical128BitLane(Vector128<uint> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<long> ShiftRightLogical128BitLane(Vector128<long> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<ulong> ShiftRightLogical128BitLane(Vector128<ulong> value, byte numBytes)
	{
		return ShiftRightLogical128BitLane(value, numBytes);
	}

	public static Vector128<double> Sqrt(Vector128<double> value)
	{
		return Sqrt(value);
	}

	public static Vector128<double> SqrtScalar(Vector128<double> value)
	{
		return SqrtScalar(value);
	}

	public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value)
	{
		return SqrtScalar(upper, value);
	}

	public unsafe static void StoreScalar(double* address, Vector128<double> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreScalar(int* address, Vector128<int> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreScalar(long* address, Vector128<long> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreScalar(uint* address, Vector128<uint> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreScalar(ulong* address, Vector128<ulong> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreAligned(sbyte* address, Vector128<sbyte> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(byte* address, Vector128<byte> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(short* address, Vector128<short> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(ushort* address, Vector128<ushort> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(int* address, Vector128<int> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(uint* address, Vector128<uint> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(long* address, Vector128<long> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(ulong* address, Vector128<ulong> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(double* address, Vector128<double> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(sbyte* address, Vector128<sbyte> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(byte* address, Vector128<byte> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(short* address, Vector128<short> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(ushort* address, Vector128<ushort> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(int* address, Vector128<int> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(uint* address, Vector128<uint> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(long* address, Vector128<long> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(ulong* address, Vector128<ulong> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(double* address, Vector128<double> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void Store(sbyte* address, Vector128<sbyte> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(byte* address, Vector128<byte> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(short* address, Vector128<short> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(ushort* address, Vector128<ushort> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(int* address, Vector128<int> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(uint* address, Vector128<uint> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(long* address, Vector128<long> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(ulong* address, Vector128<ulong> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(double* address, Vector128<double> source)
	{
		Store(address, source);
	}

	public unsafe static void StoreHigh(double* address, Vector128<double> source)
	{
		StoreHigh(address, source);
	}

	public unsafe static void StoreLow(double* address, Vector128<double> source)
	{
		StoreLow(address, source);
	}

	public unsafe static void StoreNonTemporal(int* address, int value)
	{
		StoreNonTemporal(address, value);
	}

	public unsafe static void StoreNonTemporal(uint* address, uint value)
	{
		StoreNonTemporal(address, value);
	}

	public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right)
	{
		return SubtractScalar(left, right);
	}

	public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right)
	{
		return SubtractSaturate(left, right);
	}

	public static Vector128<byte> UnpackHigh(Vector128<byte> left, Vector128<byte> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<sbyte> UnpackHigh(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<short> UnpackHigh(Vector128<short> left, Vector128<short> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<ushort> UnpackHigh(Vector128<ushort> left, Vector128<ushort> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<int> UnpackHigh(Vector128<int> left, Vector128<int> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<uint> UnpackHigh(Vector128<uint> left, Vector128<uint> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<long> UnpackHigh(Vector128<long> left, Vector128<long> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<ulong> UnpackHigh(Vector128<ulong> left, Vector128<ulong> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<double> UnpackHigh(Vector128<double> left, Vector128<double> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<byte> UnpackLow(Vector128<byte> left, Vector128<byte> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<sbyte> UnpackLow(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<short> UnpackLow(Vector128<short> left, Vector128<short> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<ushort> UnpackLow(Vector128<ushort> left, Vector128<ushort> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<int> UnpackLow(Vector128<int> left, Vector128<int> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<uint> UnpackLow(Vector128<uint> left, Vector128<uint> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<long> UnpackLow(Vector128<long> left, Vector128<long> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<ulong> UnpackLow(Vector128<ulong> left, Vector128<ulong> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<double> UnpackLow(Vector128<double> left, Vector128<double> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right)
	{
		return Xor(left, right);
	}

	public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right)
	{
		return Xor(left, right);
	}

	public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right)
	{
		return Xor(left, right);
	}

	public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right)
	{
		return Xor(left, right);
	}

	public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right)
	{
		return Xor(left, right);
	}

	public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right)
	{
		return Xor(left, right);
	}

	public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right)
	{
		return Xor(left, right);
	}

	public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right)
	{
		return Xor(left, right);
	}

	public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right)
	{
		return Xor(left, right);
	}
}
