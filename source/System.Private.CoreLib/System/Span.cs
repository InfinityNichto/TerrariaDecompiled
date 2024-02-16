using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[DebuggerTypeProxy(typeof(SpanDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
[NonVersionable]
public readonly ref struct Span<T>
{
	public ref struct Enumerator
	{
		private readonly Span<T> _span;

		private int _index;

		public ref T Current
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return ref _span[_index];
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Enumerator(Span<T> span)
		{
			_span = span;
			_index = -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext()
		{
			int num = _index + 1;
			if (num < _span.Length)
			{
				_index = num;
				return true;
			}
			return false;
		}
	}

	internal readonly ByReference<T> _pointer;

	private readonly int _length;

	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[Intrinsic]
		[NonVersionable]
		get
		{
			if ((uint)index >= (uint)_length)
			{
				ThrowHelper.ThrowIndexOutOfRangeException();
			}
			return ref Unsafe.Add(ref _pointer.Value, (nint)(uint)index);
		}
	}

	public int Length
	{
		[NonVersionable]
		get
		{
			return _length;
		}
	}

	public bool IsEmpty
	{
		[NonVersionable]
		get
		{
			return 0u >= (uint)_length;
		}
	}

	public static Span<T> Empty => default(Span<T>);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span(T[]? array)
	{
		if (array == null)
		{
			this = default(Span<T>);
			return;
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		_pointer = new ByReference<T>(ref MemoryMarshal.GetArrayDataReference(array));
		_length = array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span(T[]? array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(Span<T>);
			return;
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_pointer = new ByReference<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)start));
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public unsafe Span(void* pointer, int length)
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		if (length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_pointer = new ByReference<T>(ref Unsafe.As<byte, T>(ref *(byte*)pointer));
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Span(ref T ptr, int length)
	{
		_pointer = new ByReference<T>(ref ptr);
		_length = length;
	}

	public static bool operator !=(Span<T> left, Span<T> right)
	{
		return !(left == right);
	}

	[Obsolete("Equals() on Span will always throw an exception. Use the equality operator instead.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals(object? obj)
	{
		throw new NotSupportedException(SR.NotSupported_CannotCallEqualsOnSpan);
	}

	[Obsolete("GetHashCode() on Span will always throw an exception.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		throw new NotSupportedException(SR.NotSupported_CannotCallGetHashCodeOnSpan);
	}

	public static implicit operator Span<T>(T[]? array)
	{
		return new Span<T>(array);
	}

	public static implicit operator Span<T>(ArraySegment<T> segment)
	{
		return new Span<T>(segment.Array, segment.Offset, segment.Count);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public ref T GetPinnableReference()
	{
		ref T result = ref Unsafe.NullRef<T>();
		if (_length != 0)
		{
			result = ref _pointer.Value;
		}
		return ref result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void Clear()
	{
		if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			SpanHelpers.ClearWithReferences(ref Unsafe.As<T, IntPtr>(ref _pointer.Value), (nuint)(uint)_length * (nuint)(Unsafe.SizeOf<T>() / sizeof(UIntPtr)));
		}
		else
		{
			SpanHelpers.ClearWithoutReferences(ref Unsafe.As<T, byte>(ref _pointer.Value), (nuint)(uint)_length * (nuint)Unsafe.SizeOf<T>());
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Fill(T value)
	{
		if (Unsafe.SizeOf<T>() == 1)
		{
			Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref _pointer.Value), Unsafe.As<T, byte>(ref value), (uint)_length);
		}
		else
		{
			SpanHelpers.Fill(ref _pointer.Value, (uint)_length, value);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyTo(Span<T> destination)
	{
		if ((uint)_length <= (uint)destination.Length)
		{
			Buffer.Memmove<T>(ref destination._pointer.Value, ref _pointer.Value, (uint)_length);
		}
		else
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
	}

	public bool TryCopyTo(Span<T> destination)
	{
		bool result = false;
		if ((uint)_length <= (uint)destination.Length)
		{
			Buffer.Memmove<T>(ref destination._pointer.Value, ref _pointer.Value, (uint)_length);
			result = true;
		}
		return result;
	}

	public static bool operator ==(Span<T> left, Span<T> right)
	{
		if (left._length == right._length)
		{
			return Unsafe.AreSame(ref left._pointer.Value, ref right._pointer.Value);
		}
		return false;
	}

	public static implicit operator ReadOnlySpan<T>(Span<T> span)
	{
		return new ReadOnlySpan<T>(ref span._pointer.Value, span._length);
	}

	public override string ToString()
	{
		if (typeof(T) == typeof(char))
		{
			return new string(new ReadOnlySpan<char>(ref Unsafe.As<T, char>(ref _pointer.Value), _length));
		}
		return $"System.Span<{typeof(T).Name}>[{_length}]";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> Slice(int start)
	{
		if ((uint)start > (uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Span<T>(ref Unsafe.Add(ref _pointer.Value, (nint)(uint)start), _length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<T> Slice(int start, int length)
	{
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Span<T>(ref Unsafe.Add(ref _pointer.Value, (nint)(uint)start), length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T[] ToArray()
	{
		if (_length == 0)
		{
			return Array.Empty<T>();
		}
		T[] array = new T[_length];
		Buffer.Memmove(ref MemoryMarshal.GetArrayDataReference(array), ref _pointer.Value, (uint)_length);
		return array;
	}
}
