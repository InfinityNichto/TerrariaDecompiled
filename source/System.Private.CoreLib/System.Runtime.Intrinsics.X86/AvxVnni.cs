using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86;

[Intrinsic]
[CLSCompliant(false)]
[RequiresPreviewFeatures("AvxVnni is in preview.")]
public abstract class AvxVnni : Avx2
{
	[Intrinsic]
	public new abstract class X64 : Avx2.X64
	{
		public new static bool IsSupported => IsSupported;
	}

	public new static bool IsSupported => IsSupported;

	public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right)
	{
		return MultiplyWideningAndAdd(addend, left, right);
	}

	public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right)
	{
		return MultiplyWideningAndAdd(addend, left, right);
	}

	public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right)
	{
		return MultiplyWideningAndAdd(addend, left, right);
	}

	public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<short> right)
	{
		return MultiplyWideningAndAdd(addend, left, right);
	}

	public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<byte> left, Vector128<sbyte> right)
	{
		return MultiplyWideningAndAddSaturate(addend, left, right);
	}

	public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<short> left, Vector128<short> right)
	{
		return MultiplyWideningAndAddSaturate(addend, left, right);
	}

	public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<byte> left, Vector256<sbyte> right)
	{
		return MultiplyWideningAndAddSaturate(addend, left, right);
	}

	public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<short> left, Vector256<short> right)
	{
		return MultiplyWideningAndAddSaturate(addend, left, right);
	}
}
