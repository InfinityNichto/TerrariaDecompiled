using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Sse : X86Base
{
	[Intrinsic]
	public new abstract class X64 : X86Base.X64
	{
		public new static bool IsSupported => IsSupported;

		public static long ConvertToInt64(Vector128<float> value)
		{
			return ConvertToInt64(value);
		}

		public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value)
		{
			return ConvertScalarToVector128Single(upper, value);
		}

		public static long ConvertToInt64WithTruncation(Vector128<float> value)
		{
			return ConvertToInt64WithTruncation(value);
		}
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<float> Add(Vector128<float> left, Vector128<float> right)
	{
		return Add(left, right);
	}

	public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right)
	{
		return AddScalar(left, right);
	}

	public static Vector128<float> And(Vector128<float> left, Vector128<float> right)
	{
		return And(left, right);
	}

	public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right)
	{
		return AndNot(left, right);
	}

	public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareEqual(left, right);
	}

	public static bool CompareScalarOrderedEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedEqual(left, right);
	}

	public static bool CompareScalarUnorderedEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedEqual(left, right);
	}

	public static Vector128<float> CompareScalarEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarEqual(left, right);
	}

	public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareGreaterThan(left, right);
	}

	public static bool CompareScalarOrderedGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedGreaterThan(left, right);
	}

	public static bool CompareScalarUnorderedGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedGreaterThan(left, right);
	}

	public static Vector128<float> CompareScalarGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarGreaterThan(left, right);
	}

	public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareGreaterThanOrEqual(left, right);
	}

	public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedGreaterThanOrEqual(left, right);
	}

	public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedGreaterThanOrEqual(left, right);
	}

	public static Vector128<float> CompareScalarGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarGreaterThanOrEqual(left, right);
	}

	public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareLessThan(left, right);
	}

	public static bool CompareScalarOrderedLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedLessThan(left, right);
	}

	public static bool CompareScalarUnorderedLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedLessThan(left, right);
	}

	public static Vector128<float> CompareScalarLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarLessThan(left, right);
	}

	public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareLessThanOrEqual(left, right);
	}

	public static bool CompareScalarOrderedLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedLessThanOrEqual(left, right);
	}

	public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedLessThanOrEqual(left, right);
	}

	public static Vector128<float> CompareScalarLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarLessThanOrEqual(left, right);
	}

	public static Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareNotEqual(left, right);
	}

	public static bool CompareScalarOrderedNotEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrderedNotEqual(left, right);
	}

	public static bool CompareScalarUnorderedNotEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnorderedNotEqual(left, right);
	}

	public static Vector128<float> CompareScalarNotEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarNotEqual(left, right);
	}

	public static Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareNotGreaterThan(left, right);
	}

	public static Vector128<float> CompareScalarNotGreaterThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarNotGreaterThan(left, right);
	}

	public static Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareNotGreaterThanOrEqual(left, right);
	}

	public static Vector128<float> CompareScalarNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarNotGreaterThanOrEqual(left, right);
	}

	public static Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareNotLessThan(left, right);
	}

	public static Vector128<float> CompareScalarNotLessThan(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarNotLessThan(left, right);
	}

	public static Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareNotLessThanOrEqual(left, right);
	}

	public static Vector128<float> CompareScalarNotLessThanOrEqual(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarNotLessThanOrEqual(left, right);
	}

	public static Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right)
	{
		return CompareOrdered(left, right);
	}

	public static Vector128<float> CompareScalarOrdered(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarOrdered(left, right);
	}

	public static Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right)
	{
		return CompareUnordered(left, right);
	}

	public static Vector128<float> CompareScalarUnordered(Vector128<float> left, Vector128<float> right)
	{
		return CompareScalarUnordered(left, right);
	}

	public static int ConvertToInt32(Vector128<float> value)
	{
		return ConvertToInt32(value);
	}

	public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value)
	{
		return ConvertScalarToVector128Single(upper, value);
	}

	public static int ConvertToInt32WithTruncation(Vector128<float> value)
	{
		return ConvertToInt32WithTruncation(value);
	}

	public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right)
	{
		return Divide(left, right);
	}

	public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right)
	{
		return DivideScalar(left, right);
	}

	public unsafe static Vector128<float> LoadVector128(float* address)
	{
		return LoadVector128(address);
	}

	public unsafe static Vector128<float> LoadScalarVector128(float* address)
	{
		return LoadScalarVector128(address);
	}

	public unsafe static Vector128<float> LoadAlignedVector128(float* address)
	{
		return LoadAlignedVector128(address);
	}

	public unsafe static Vector128<float> LoadHigh(Vector128<float> lower, float* address)
	{
		return LoadHigh(lower, address);
	}

	public unsafe static Vector128<float> LoadLow(Vector128<float> upper, float* address)
	{
		return LoadLow(upper, address);
	}

	public static Vector128<float> Max(Vector128<float> left, Vector128<float> right)
	{
		return Max(left, right);
	}

	public static Vector128<float> MaxScalar(Vector128<float> left, Vector128<float> right)
	{
		return MaxScalar(left, right);
	}

	public static Vector128<float> Min(Vector128<float> left, Vector128<float> right)
	{
		return Min(left, right);
	}

	public static Vector128<float> MinScalar(Vector128<float> left, Vector128<float> right)
	{
		return MinScalar(left, right);
	}

	public static Vector128<float> MoveScalar(Vector128<float> upper, Vector128<float> value)
	{
		return MoveScalar(upper, value);
	}

	public static Vector128<float> MoveHighToLow(Vector128<float> left, Vector128<float> right)
	{
		return MoveHighToLow(left, right);
	}

	public static Vector128<float> MoveLowToHigh(Vector128<float> left, Vector128<float> right)
	{
		return MoveLowToHigh(left, right);
	}

	public static int MoveMask(Vector128<float> value)
	{
		return MoveMask(value);
	}

	public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right)
	{
		return Multiply(left, right);
	}

	public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right)
	{
		return MultiplyScalar(left, right);
	}

	public static Vector128<float> Or(Vector128<float> left, Vector128<float> right)
	{
		return Or(left, right);
	}

	public unsafe static void Prefetch0(void* address)
	{
		Prefetch0(address);
	}

	public unsafe static void Prefetch1(void* address)
	{
		Prefetch1(address);
	}

	public unsafe static void Prefetch2(void* address)
	{
		Prefetch2(address);
	}

	public unsafe static void PrefetchNonTemporal(void* address)
	{
		PrefetchNonTemporal(address);
	}

	public static Vector128<float> Reciprocal(Vector128<float> value)
	{
		return Reciprocal(value);
	}

	public static Vector128<float> ReciprocalScalar(Vector128<float> value)
	{
		return ReciprocalScalar(value);
	}

	public static Vector128<float> ReciprocalScalar(Vector128<float> upper, Vector128<float> value)
	{
		return ReciprocalScalar(upper, value);
	}

	public static Vector128<float> ReciprocalSqrt(Vector128<float> value)
	{
		return ReciprocalSqrt(value);
	}

	public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> value)
	{
		return ReciprocalSqrtScalar(value);
	}

	public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> upper, Vector128<float> value)
	{
		return ReciprocalSqrtScalar(upper, value);
	}

	public static Vector128<float> Shuffle(Vector128<float> left, Vector128<float> right, byte control)
	{
		return Shuffle(left, right, control);
	}

	public static Vector128<float> Sqrt(Vector128<float> value)
	{
		return Sqrt(value);
	}

	public static Vector128<float> SqrtScalar(Vector128<float> value)
	{
		return SqrtScalar(value);
	}

	public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value)
	{
		return SqrtScalar(upper, value);
	}

	public unsafe static void StoreAligned(float* address, Vector128<float> source)
	{
		StoreAligned(address, source);
	}

	public unsafe static void StoreAlignedNonTemporal(float* address, Vector128<float> source)
	{
		StoreAlignedNonTemporal(address, source);
	}

	public unsafe static void Store(float* address, Vector128<float> source)
	{
		Store(address, source);
	}

	public static void StoreFence()
	{
		StoreFence();
	}

	public unsafe static void StoreScalar(float* address, Vector128<float> source)
	{
		StoreScalar(address, source);
	}

	public unsafe static void StoreHigh(float* address, Vector128<float> source)
	{
		StoreHigh(address, source);
	}

	public unsafe static void StoreLow(float* address, Vector128<float> source)
	{
		StoreLow(address, source);
	}

	public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right)
	{
		return Subtract(left, right);
	}

	public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right)
	{
		return SubtractScalar(left, right);
	}

	public static Vector128<float> UnpackHigh(Vector128<float> left, Vector128<float> right)
	{
		return UnpackHigh(left, right);
	}

	public static Vector128<float> UnpackLow(Vector128<float> left, Vector128<float> right)
	{
		return UnpackLow(left, right);
	}

	public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right)
	{
		return Xor(left, right);
	}
}
