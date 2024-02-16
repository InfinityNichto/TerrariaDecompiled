using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Avx : Sse42
{
	[Intrinsic]
	public new abstract class X64 : Sse42.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector256<float> Add(Vector256<float> left, Vector256<float> right)
	{
		return Add(left, right);
	}

	public static Vector256<double> Add(Vector256<double> left, Vector256<double> right)
	{
		return Add(left, right);
	}

	public static Vector256<float> AddSubtract(Vector256<float> left, Vector256<float> right)
	{
		return AddSubtract(left, right);
	}

	public static Vector256<double> AddSubtract(Vector256<double> left, Vector256<double> right)
	{
		return AddSubtract(left, right);
	}

	public static Vector256<float> And(Vector256<float> left, Vector256<float> right)
	{
		return And(left, right);
	}

	public static Vector256<double> And(Vector256<double> left, Vector256<double> right)
	{
		return And(left, right);
	}

	public static Vector256<float> AndNot(Vector256<float> left, Vector256<float> right)
	{
		return AndNot(left, right);
	}

	public static Vector256<double> AndNot(Vector256<double> left, Vector256<double> right)
	{
		return AndNot(left, right);
	}

	public static Vector256<float> Blend(Vector256<float> left, Vector256<float> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector256<double> Blend(Vector256<double> left, Vector256<double> right, byte control)
	{
		return Blend(left, right, control);
	}

	public static Vector256<float> BlendVariable(Vector256<float> left, Vector256<float> right, Vector256<float> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public static Vector256<double> BlendVariable(Vector256<double> left, Vector256<double> right, Vector256<double> mask)
	{
		return BlendVariable(left, right, mask);
	}

	public unsafe static Vector128<float> BroadcastScalarToVector128(float* source)
	{
		return BroadcastScalarToVector128(source);
	}

	public unsafe static Vector256<float> BroadcastScalarToVector256(float* source)
	{
		return BroadcastScalarToVector256(source);
	}

	public unsafe static Vector256<double> BroadcastScalarToVector256(double* source)
	{
		return BroadcastScalarToVector256(source);
	}

	public unsafe static Vector256<float> BroadcastVector128ToVector256(float* address)
	{
		return BroadcastVector128ToVector256(address);
	}

	public unsafe static Vector256<double> BroadcastVector128ToVector256(double* address)
	{
		return BroadcastVector128ToVector256(address);
	}

	public static Vector256<float> Ceiling(Vector256<float> value)
	{
		return Ceiling(value);
	}

	public static Vector256<double> Ceiling(Vector256<double> value)
	{
		return Ceiling(value);
	}

	public static Vector128<float> Compare(Vector128<float> left, Vector128<float> right, FloatComparisonMode mode)
	{
		return Compare(left, right, mode);
	}

	public static Vector128<double> Compare(Vector128<double> left, Vector128<double> right, FloatComparisonMode mode)
	{
		return Compare(left, right, mode);
	}

	public static Vector256<float> Compare(Vector256<float> left, Vector256<float> right, FloatComparisonMode mode)
	{
		return Compare(left, right, mode);
	}

	public static Vector256<double> Compare(Vector256<double> left, Vector256<double> right, FloatComparisonMode mode)
	{
		return Compare(left, right, mode);
	}

	public static Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedEqualNonSignaling);
	}

	public static Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedEqualNonSignaling);
	}

	public static Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedGreaterThanSignaling);
	}

	public static Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedGreaterThanSignaling);
	}

	public static Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedGreaterThanOrEqualSignaling);
	}

	public static Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedGreaterThanOrEqualSignaling);
	}

	public static Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedLessThanSignaling);
	}

	public static Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedLessThanSignaling);
	}

	public static Vector256<float> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedLessThanOrEqualSignaling);
	}

	public static Vector256<double> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedLessThanOrEqualSignaling);
	}

	public static Vector256<float> CompareNotEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotEqualNonSignaling);
	}

	public static Vector256<double> CompareNotEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotEqualNonSignaling);
	}

	public static Vector256<float> CompareNotGreaterThan(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotGreaterThanSignaling);
	}

	public static Vector256<double> CompareNotGreaterThan(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotGreaterThanSignaling);
	}

	public static Vector256<float> CompareNotGreaterThanOrEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotGreaterThanOrEqualSignaling);
	}

	public static Vector256<double> CompareNotGreaterThanOrEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotGreaterThanOrEqualSignaling);
	}

	public static Vector256<float> CompareNotLessThan(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotLessThanSignaling);
	}

	public static Vector256<double> CompareNotLessThan(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotLessThanSignaling);
	}

	public static Vector256<float> CompareNotLessThanOrEqual(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotLessThanOrEqualSignaling);
	}

	public static Vector256<double> CompareNotLessThanOrEqual(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNotLessThanOrEqualSignaling);
	}

	public static Vector256<float> CompareOrdered(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedNonSignaling);
	}

	public static Vector256<double> CompareOrdered(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.OrderedNonSignaling);
	}

	public static Vector128<double> CompareScalar(Vector128<double> left, Vector128<double> right, FloatComparisonMode mode)
	{
		return CompareScalar(left, right, mode);
	}

	public static Vector128<float> CompareScalar(Vector128<float> left, Vector128<float> right, FloatComparisonMode mode)
	{
		return CompareScalar(left, right, mode);
	}

	public static Vector256<float> CompareUnordered(Vector256<float> left, Vector256<float> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNonSignaling);
	}

	public static Vector256<double> CompareUnordered(Vector256<double> left, Vector256<double> right)
	{
		return Compare(left, right, FloatComparisonMode.UnorderedNonSignaling);
	}

	public static Vector128<int> ConvertToVector128Int32(Vector256<double> value)
	{
		return ConvertToVector128Int32(value);
	}

	public static Vector128<float> ConvertToVector128Single(Vector256<double> value)
	{
		return ConvertToVector128Single(value);
	}

	public static Vector256<int> ConvertToVector256Int32(Vector256<float> value)
	{
		return ConvertToVector256Int32(value);
	}

	public static Vector256<float> ConvertToVector256Single(Vector256<int> value)
	{
		return ConvertToVector256Single(value);
	}

	public static Vector256<double> ConvertToVector256Double(Vector128<float> value)
	{
		return ConvertToVector256Double(value);
	}

	public static Vector256<double> ConvertToVector256Double(Vector128<int> value)
	{
		return ConvertToVector256Double(value);
	}

	public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector256<double> value)
	{
		return ConvertToVector128Int32WithTruncation(value);
	}

	public static Vector256<int> ConvertToVector256Int32WithTruncation(Vector256<float> value)
	{
		return ConvertToVector256Int32WithTruncation(value);
	}

	public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right)
	{
		return Divide(left, right);
	}

	public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right)
	{
		return Divide(left, right);
	}

	public static Vector256<float> DotProduct(Vector256<float> left, Vector256<float> right, byte control)
	{
		return DotProduct(left, right, control);
	}

	public static Vector256<float> DuplicateEvenIndexed(Vector256<float> value)
	{
		return DuplicateEvenIndexed(value);
	}

	public static Vector256<double> DuplicateEvenIndexed(Vector256<double> value)
	{
		return DuplicateEvenIndexed(value);
	}

	public static Vector256<float> DuplicateOddIndexed(Vector256<float> value)
	{
		return DuplicateOddIndexed(value);
	}

	public static Vector128<byte> ExtractVector128(Vector256<byte> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<sbyte> ExtractVector128(Vector256<sbyte> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<short> ExtractVector128(Vector256<short> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<ushort> ExtractVector128(Vector256<ushort> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<int> ExtractVector128(Vector256<int> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<uint> ExtractVector128(Vector256<uint> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<long> ExtractVector128(Vector256<long> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<ulong> ExtractVector128(Vector256<ulong> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<float> ExtractVector128(Vector256<float> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector128<double> ExtractVector128(Vector256<double> value, byte index)
	{
		return ExtractVector128(value, index);
	}

	public static Vector256<float> Floor(Vector256<float> value)
	{
		return Floor(value);
	}

	public static Vector256<double> Floor(Vector256<double> value)
	{
		return Floor(value);
	}

	public static Vector256<float> HorizontalAdd(Vector256<float> left, Vector256<float> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector256<double> HorizontalAdd(Vector256<double> left, Vector256<double> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector256<float> HorizontalSubtract(Vector256<float> left, Vector256<float> right)
	{
		return HorizontalSubtract(left, right);
	}

	public static Vector256<double> HorizontalSubtract(Vector256<double> left, Vector256<double> right)
	{
		return HorizontalSubtract(left, right);
	}

	public static Vector256<byte> InsertVector128(Vector256<byte> value, Vector128<byte> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<sbyte> InsertVector128(Vector256<sbyte> value, Vector128<sbyte> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<short> InsertVector128(Vector256<short> value, Vector128<short> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<ushort> InsertVector128(Vector256<ushort> value, Vector128<ushort> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<int> InsertVector128(Vector256<int> value, Vector128<int> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<uint> InsertVector128(Vector256<uint> value, Vector128<uint> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<long> InsertVector128(Vector256<long> value, Vector128<long> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<ulong> InsertVector128(Vector256<ulong> value, Vector128<ulong> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<float> InsertVector128(Vector256<float> value, Vector128<float> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public static Vector256<double> InsertVector128(Vector256<double> value, Vector128<double> data, byte index)
	{
		return InsertVector128(value, data, index);
	}

	public unsafe static Vector256<sbyte> LoadVector256(sbyte* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<byte> LoadVector256(byte* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<short> LoadVector256(short* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<ushort> LoadVector256(ushort* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<int> LoadVector256(int* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<uint> LoadVector256(uint* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<long> LoadVector256(long* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<ulong> LoadVector256(ulong* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<float> LoadVector256(float* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<double> LoadVector256(double* address)
	{
		return LoadVector256(address);
	}

	public unsafe static Vector256<sbyte> LoadAlignedVector256(sbyte* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<byte> LoadAlignedVector256(byte* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<short> LoadAlignedVector256(short* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<ushort> LoadAlignedVector256(ushort* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<int> LoadAlignedVector256(int* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<uint> LoadAlignedVector256(uint* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<long> LoadAlignedVector256(long* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<ulong> LoadAlignedVector256(ulong* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<float> LoadAlignedVector256(float* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<double> LoadAlignedVector256(double* address)
	{
		return LoadAlignedVector256(address);
	}

	public unsafe static Vector256<sbyte> LoadDquVector256(sbyte* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<byte> LoadDquVector256(byte* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<short> LoadDquVector256(short* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<ushort> LoadDquVector256(ushort* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<int> LoadDquVector256(int* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<uint> LoadDquVector256(uint* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<long> LoadDquVector256(long* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector256<ulong> LoadDquVector256(ulong* address)
	{
		return LoadDquVector256(address);
	}

	public unsafe static Vector128<float> MaskLoad(float* address, Vector128<float> mask)
	{
		return MaskLoad(address, mask);
	}

	public unsafe static Vector128<double> MaskLoad(double* address, Vector128<double> mask)
	{
		return MaskLoad(address, mask);
	}

	public unsafe static Vector256<float> MaskLoad(float* address, Vector256<float> mask)
	{
		return MaskLoad(address, mask);
	}

	public unsafe static Vector256<double> MaskLoad(double* address, Vector256<double> mask)
	{
		return MaskLoad(address, mask);
	}

	public unsafe static void MaskStore(float* address, Vector128<float> mask, Vector128<float> source)
	{
		MaskStore(address, mask, source);
	}

	public unsafe static void MaskStore(double* address, Vector128<double> mask, Vector128<double> source)
	{
		MaskStore(address, mask, source);
	}

	public unsafe static void MaskStore(float* address, Vector256<float> mask, Vector256<float> source)
	{
		MaskStore(address, mask, source);
	}

	public unsafe static void MaskStore(double* address, Vector256<double> mask, Vector256<double> source)
	{
		MaskStore(address, mask, source);
	}

	public static Vector256<float> Max(Vector256<float> left, Vector256<float> right)
	{
		return Max(left, right);
	}

	public static Vector256<double> Max(Vector256<double> left, Vector256<double> right)
	{
		return Max(left, right);
	}

	public static Vector256<float> Min(Vector256<float> left, Vector256<float> right)
	{
		return Min(left, right);
	}

	public static Vector256<double> Min(Vector256<double> left, Vector256<double> right)
	{
		return Min(left, right);
	}

	public static int MoveMask(Vector256<float> value)
	{
		return MoveMask(value);
	}

	public static int MoveMask(Vector256<double> value)
	{
		return MoveMask(value);
	}

	public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right)
	{
		return Multiply(left, right);
	}

	public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right)
	{
		return Multiply(left, right);
	}

	public static Vector256<float> Or(Vector256<float> left, Vector256<float> right)
	{
		return Or(left, right);
	}

	public static Vector256<double> Or(Vector256<double> left, Vector256<double> right)
	{
		return Or(left, right);
	}

	public static Vector128<float> Permute(Vector128<float> value, byte control)
	{
		return Permute(value, control);
	}

	public static Vector128<double> Permute(Vector128<double> value, byte control)
	{
		return Permute(value, control);
	}

	public static Vector256<float> Permute(Vector256<float> value, byte control)
	{
		return Permute(value, control);
	}

	public static Vector256<double> Permute(Vector256<double> value, byte control)
	{
		return Permute(value, control);
	}

	public static Vector256<byte> Permute2x128(Vector256<byte> left, Vector256<byte> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<sbyte> Permute2x128(Vector256<sbyte> left, Vector256<sbyte> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<short> Permute2x128(Vector256<short> left, Vector256<short> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<ushort> Permute2x128(Vector256<ushort> left, Vector256<ushort> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<int> Permute2x128(Vector256<int> left, Vector256<int> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<uint> Permute2x128(Vector256<uint> left, Vector256<uint> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<long> Permute2x128(Vector256<long> left, Vector256<long> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<ulong> Permute2x128(Vector256<ulong> left, Vector256<ulong> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<float> Permute2x128(Vector256<float> left, Vector256<float> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector256<double> Permute2x128(Vector256<double> left, Vector256<double> right, byte control)
	{
		return Permute2x128(left, right, control);
	}

	public static Vector128<float> PermuteVar(Vector128<float> left, Vector128<int> control)
	{
		return PermuteVar(left, control);
	}

	public static Vector128<double> PermuteVar(Vector128<double> left, Vector128<long> control)
	{
		return PermuteVar(left, control);
	}

	public static Vector256<float> PermuteVar(Vector256<float> left, Vector256<int> control)
	{
		return PermuteVar(left, control);
	}

	public static Vector256<double> PermuteVar(Vector256<double> left, Vector256<long> control)
	{
		return PermuteVar(left, control);
	}

	public static Vector256<float> Reciprocal(Vector256<float> value)
	{
		return Reciprocal(value);
	}

	public static Vector256<float> ReciprocalSqrt(Vector256<float> value)
	{
		return ReciprocalSqrt(value);
	}

	public static Vector256<float> RoundToNearestInteger(Vector256<float> value)
	{
		return RoundToNearestInteger(value);
	}

	public static Vector256<float> RoundToNegativeInfinity(Vector256<float> value)
	{
		return RoundToNegativeInfinity(value);
	}

	public static Vector256<float> RoundToPositiveInfinity(Vector256<float> value)
	{
		return RoundToPositiveInfinity(value);
	}

	public static Vector256<float> RoundToZero(Vector256<float> value)
	{
		return RoundToZero(value);
	}

	public static Vector256<float> RoundCurrentDirection(Vector256<float> value)
	{
		return RoundCurrentDirection(value);
	}

	public static Vector256<double> RoundToNearestInteger(Vector256<double> value)
	{
		return RoundToNearestInteger(value);
	}

	public static Vector256<double> RoundToNegativeInfinity(Vector256<double> value)
	{
		return RoundToNegativeInfinity(value);
	}

	public static Vector256<double> RoundToPositiveInfinity(Vector256<double> value)
	{
		return RoundToPositiveInfinity(value);
	}

	public static Vector256<double> RoundToZero(Vector256<double> value)
	{
		return RoundToZero(value);
	}

	public static Vector256<double> RoundCurrentDirection(Vector256<double> value)
	{
		return RoundCurrentDirection(value);
	}

	public static Vector256<float> Shuffle(Vector256<float> value, Vector256<float> right, byte control)
	{
		return Shuffle(value, right, control);
	}

	public static Vector256<double> Shuffle(Vector256<double> value, Vector256<double> right, byte control)
	{
		return Shuffle(value, right, control);
	}

	public static Vector256<float> Sqrt(Vector256<float> value)
	{
		return Sqrt(value);
	}

	public static Vector256<double> Sqrt(Vector256<double> value)
	{
		return Sqrt(value);
	}

	public unsafe static void StoreAligned(sbyte* address, Vector256<sbyte> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(byte* address, Vector256<byte> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(short* address, Vector256<short> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(ushort* address, Vector256<ushort> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(int* address, Vector256<int> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(uint* address, Vector256<uint> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(long* address, Vector256<long> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(ulong* address, Vector256<ulong> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(float* address, Vector256<float> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAligned(double* address, Vector256<double> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(sbyte* address, Vector256<sbyte> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(byte* address, Vector256<byte> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(short* address, Vector256<short> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(ushort* address, Vector256<ushort> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(int* address, Vector256<int> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(uint* address, Vector256<uint> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(long* address, Vector256<long> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(ulong* address, Vector256<ulong> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(float* address, Vector256<float> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(double* address, Vector256<double> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void Store(sbyte* address, Vector256<sbyte> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(byte* address, Vector256<byte> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(short* address, Vector256<short> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(ushort* address, Vector256<ushort> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(int* address, Vector256<int> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(uint* address, Vector256<uint> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(long* address, Vector256<long> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(ulong* address, Vector256<ulong> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(float* address, Vector256<float> source)
	{
		Store(address, source);
	}

	public unsafe static void Store(double* address, Vector256<double> source)
	{
		Store(address, source);
	}

	public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right)
	{
		return Subtract(left, right);
	}

	public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right)
	{
		return Subtract(left, right);
	}

	public static bool TestC(Vector128<float> left, Vector128<float> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector128<double> left, Vector128<double> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<byte> left, Vector256<byte> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<sbyte> left, Vector256<sbyte> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<short> left, Vector256<short> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<ushort> left, Vector256<ushort> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<int> left, Vector256<int> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<uint> left, Vector256<uint> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<long> left, Vector256<long> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<ulong> left, Vector256<ulong> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<float> left, Vector256<float> right)
	{
		return TestC(left, right);
	}

	public static bool TestC(Vector256<double> left, Vector256<double> right)
	{
		return TestC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<float> left, Vector128<float> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector128<double> left, Vector128<double> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<byte> left, Vector256<byte> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<sbyte> left, Vector256<sbyte> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<short> left, Vector256<short> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<ushort> left, Vector256<ushort> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<int> left, Vector256<int> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<uint> left, Vector256<uint> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<long> left, Vector256<long> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<ulong> left, Vector256<ulong> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<float> left, Vector256<float> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestNotZAndNotC(Vector256<double> left, Vector256<double> right)
	{
		return TestNotZAndNotC(left, right);
	}

	public static bool TestZ(Vector128<float> left, Vector128<float> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector128<double> left, Vector128<double> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<byte> left, Vector256<byte> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<sbyte> left, Vector256<sbyte> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<short> left, Vector256<short> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<ushort> left, Vector256<ushort> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<int> left, Vector256<int> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<uint> left, Vector256<uint> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<long> left, Vector256<long> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<ulong> left, Vector256<ulong> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<float> left, Vector256<float> right)
	{
		return TestZ(left, right);
	}

	public static bool TestZ(Vector256<double> left, Vector256<double> right)
	{
		return TestZ(left, right);
	}

	public static Vector256<float> UnpackHigh(Vector256<float> left, Vector256<float> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector256<double> UnpackHigh(Vector256<double> left, Vector256<double> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector256<float> UnpackLow(Vector256<float> left, Vector256<float> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector256<double> UnpackLow(Vector256<double> left, Vector256<double> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector256<float> Xor(Vector256<float> left, Vector256<float> right)
	{
		return Xor(left, right);
	}

	public static Vector256<double> Xor(Vector256<double> left, Vector256<double> right)
	{
		return Xor(left, right);
	}
}
