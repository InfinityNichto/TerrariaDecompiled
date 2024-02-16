using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices;

public static class MemoryMarshal
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T GetArrayDataReference<T>(T[] array)
	{
		return ref Unsafe.As<byte, T>(ref Unsafe.As<RawArrayData>(array).Data);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static ref byte GetArrayDataReference(Array array)
	{
		return ref Unsafe.AddByteOffset(ref Unsafe.As<RawData>(array).Data, (nuint)RuntimeHelpers.GetMethodTable(array)->BaseSize - (nuint)(2 * sizeof(IntPtr)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<byte> AsBytes<T>(Span<T> span) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		return new Span<byte>(ref Unsafe.As<T, byte>(ref GetReference(span)), checked(span.Length * Unsafe.SizeOf<T>()));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		return new ReadOnlySpan<byte>(ref Unsafe.As<T, byte>(ref GetReference(span)), checked(span.Length * Unsafe.SizeOf<T>()));
	}

	public static Memory<T> AsMemory<T>(ReadOnlyMemory<T> memory)
	{
		return Unsafe.As<ReadOnlyMemory<T>, Memory<T>>(ref memory);
	}

	public static ref T GetReference<T>(Span<T> span)
	{
		return ref span._pointer.Value;
	}

	public static ref T GetReference<T>(ReadOnlySpan<T> span)
	{
		return ref span._pointer.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static ref T GetNonNullPinnableReference<T>(Span<T> span)
	{
		if (span.Length == 0)
		{
			return ref Unsafe.AsRef<T>((void*)1);
		}
		return ref span._pointer.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe static ref T GetNonNullPinnableReference<T>(ReadOnlySpan<T> span)
	{
		if (span.Length == 0)
		{
			return ref Unsafe.AsRef<T>((void*)1);
		}
		return ref span._pointer.Value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span) where TFrom : struct where TTo : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TFrom>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TFrom));
		}
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TTo>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TTo));
		}
		uint num = (uint)Unsafe.SizeOf<TFrom>();
		uint num2 = (uint)Unsafe.SizeOf<TTo>();
		uint length = (uint)span.Length;
		int length2;
		if (num == num2)
		{
			length2 = (int)length;
		}
		else if (num == 1)
		{
			length2 = (int)(length / num2);
		}
		else
		{
			ulong num3 = (ulong)((long)length * (long)num) / (ulong)num2;
			length2 = checked((int)num3);
		}
		return new Span<TTo>(ref Unsafe.As<TFrom, TTo>(ref span._pointer.Value), length2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span) where TFrom : struct where TTo : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TFrom>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TFrom));
		}
		if (RuntimeHelpers.IsReferenceOrContainsReferences<TTo>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(TTo));
		}
		uint num = (uint)Unsafe.SizeOf<TFrom>();
		uint num2 = (uint)Unsafe.SizeOf<TTo>();
		uint length = (uint)span.Length;
		int length2;
		if (num == num2)
		{
			length2 = (int)length;
		}
		else if (num == 1)
		{
			length2 = (int)(length / num2);
		}
		else
		{
			ulong num3 = (ulong)((long)length * (long)num) / (ulong)num2;
			length2 = checked((int)num3);
		}
		return new ReadOnlySpan<TTo>(ref Unsafe.As<TFrom, TTo>(ref GetReference(span)), length2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> CreateSpan<T>(ref T reference, int length)
	{
		return new Span<T>(ref reference, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> CreateReadOnlySpan<T>(ref T reference, int length)
	{
		return new ReadOnlySpan<T>(ref reference, length);
	}

	[CLSCompliant(false)]
	public unsafe static ReadOnlySpan<char> CreateReadOnlySpanFromNullTerminated(char* value)
	{
		if (value == null)
		{
			return default(ReadOnlySpan<char>);
		}
		return new ReadOnlySpan<char>(value, string.wcslen(value));
	}

	[CLSCompliant(false)]
	public unsafe static ReadOnlySpan<byte> CreateReadOnlySpanFromNullTerminated(byte* value)
	{
		if (value == null)
		{
			return default(ReadOnlySpan<byte>);
		}
		return new ReadOnlySpan<byte>(value, string.strlen(value));
	}

	public static bool TryGetArray<T>(ReadOnlyMemory<T> memory, out ArraySegment<T> segment)
	{
		int start;
		int length;
		object objectStartLength = memory.GetObjectStartLength(out start, out length);
		if (objectStartLength != null && (!(typeof(T) == typeof(char)) || !(objectStartLength.GetType() == typeof(string))))
		{
			if (RuntimeHelpers.ObjectHasComponentSize(objectStartLength))
			{
				segment = new ArraySegment<T>(Unsafe.As<T[]>(objectStartLength), start & 0x7FFFFFFF, length);
				return true;
			}
			if (Unsafe.As<MemoryManager<T>>(objectStartLength).TryGetArray(out var segment2))
			{
				segment = new ArraySegment<T>(segment2.Array, segment2.Offset + start, length);
				return true;
			}
		}
		if (length == 0)
		{
			segment = ArraySegment<T>.Empty;
			return true;
		}
		segment = default(ArraySegment<T>);
		return false;
	}

	public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, [NotNullWhen(true)] out TManager? manager) where TManager : MemoryManager<T>
	{
		int start;
		int length;
		return (manager = memory.GetObjectStartLength(out start, out length) as TManager) != null;
	}

	public static bool TryGetMemoryManager<T, TManager>(ReadOnlyMemory<T> memory, [NotNullWhen(true)] out TManager? manager, out int start, out int length) where TManager : MemoryManager<T>
	{
		if ((manager = memory.GetObjectStartLength(out start, out length) as TManager) == null)
		{
			start = 0;
			length = 0;
			return false;
		}
		return true;
	}

	public static IEnumerable<T> ToEnumerable<T>(ReadOnlyMemory<T> memory)
	{
		for (int i = 0; i < memory.Length; i++)
		{
			yield return memory.Span[i];
		}
	}

	public static bool TryGetString(ReadOnlyMemory<char> memory, [NotNullWhen(true)] out string? text, out int start, out int length)
	{
		if (memory.GetObjectStartLength(out var start2, out var length2) is string text2)
		{
			text = text2;
			start = start2;
			length = length2;
			return true;
		}
		text = null;
		start = 0;
		length = 0;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Read<T>(ReadOnlySpan<byte> source) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (Unsafe.SizeOf<T>() > source.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return Unsafe.ReadUnaligned<T>(ref GetReference(source));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryRead<T>(ReadOnlySpan<byte> source, out T value) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (Unsafe.SizeOf<T>() > (uint)source.Length)
		{
			value = default(T);
			return false;
		}
		value = Unsafe.ReadUnaligned<T>(ref GetReference(source));
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Write<T>(Span<byte> destination, ref T value) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if ((uint)Unsafe.SizeOf<T>() > (uint)destination.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		Unsafe.WriteUnaligned(ref GetReference(destination), value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWrite<T>(Span<byte> destination, ref T value) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (Unsafe.SizeOf<T>() > (uint)destination.Length)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref GetReference(destination), value);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T AsRef<T>(Span<byte> span) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (Unsafe.SizeOf<T>() > (uint)span.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return ref Unsafe.As<byte, T>(ref GetReference(span));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref readonly T AsRef<T>(ReadOnlySpan<byte> span) where T : struct
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (Unsafe.SizeOf<T>() > (uint)span.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
		}
		return ref Unsafe.As<byte, T>(ref GetReference(span));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Memory<T> CreateFromPinnedArray<T>(T[]? array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			return default(Memory<T>);
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Memory<T>((object)array, start | int.MinValue, length);
	}
}
