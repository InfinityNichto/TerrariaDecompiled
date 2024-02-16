using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
public abstract class Sse3 : Sse2
{
	[Intrinsic]
	public new abstract class X64 : Sse2.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<float> AddSubtract(Vector128<float> left, Vector128<float> right)
	{
		return AddSubtract(left, right);
	}

	public static Vector128<double> AddSubtract(Vector128<double> left, Vector128<double> right)
	{
		return AddSubtract(left, right);
	}

	public static Vector128<float> HorizontalAdd(Vector128<float> left, Vector128<float> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector128<double> HorizontalAdd(Vector128<double> left, Vector128<double> right)
	{
		return HorizontalAdd(left, right);
	}

	public static Vector128<float> HorizontalSubtract(Vector128<float> left, Vector128<float> right)
	{
		return HorizontalSubtract(left, right);
	}

	public static Vector128<double> HorizontalSubtract(Vector128<double> left, Vector128<double> right)
	{
		return HorizontalSubtract(left, right);
	}

	public unsafe static Vector128<double> LoadAndDuplicateToVector128(double* address)
	{
		return LoadAndDuplicateToVector128(address);
	}

	public unsafe static Vector128<sbyte> LoadDquVector128(sbyte* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<byte> LoadDquVector128(byte* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<short> LoadDquVector128(short* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<ushort> LoadDquVector128(ushort* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<int> LoadDquVector128(int* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<uint> LoadDquVector128(uint* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<long> LoadDquVector128(long* address)
	{
		return LoadDquVector128(address);
	}

	public unsafe static Vector128<ulong> LoadDquVector128(ulong* address)
	{
		return LoadDquVector128(address);
	}

	public static Vector128<double> MoveAndDuplicate(Vector128<double> source)
	{
		return MoveAndDuplicate(source);
	}

	public static Vector128<float> MoveHighAndDuplicate(Vector128<float> source)
	{
		return MoveHighAndDuplicate(source);
	}

	public static Vector128<float> MoveLowAndDuplicate(Vector128<float> source)
	{
		return MoveLowAndDuplicate(source);
	}
}
