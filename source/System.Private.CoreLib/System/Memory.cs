using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System;

[DebuggerTypeProxy(typeof(MemoryDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly struct Memory<T> : IEquatable<Memory<T>>
{
	private readonly object _object;

	private readonly int _index;

	private readonly int _length;

	public static Memory<T> Empty => default(Memory<T>);

	public int Length => _length;

	public bool IsEmpty => _length == 0;

	public unsafe Span<T> Span
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			ref T ptr = ref Unsafe.NullRef<T>();
			int length = 0;
			object @object = _object;
			if (@object != null)
			{
				if (typeof(T) == typeof(char) && @object.GetType() == typeof(string))
				{
					ptr = ref Unsafe.As<char, T>(ref Unsafe.As<string>(@object).GetRawStringData());
					length = Unsafe.As<string>(@object).Length;
				}
				else if (RuntimeHelpers.ObjectHasComponentSize(@object))
				{
					ptr = ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(@object));
					length = Unsafe.As<T[]>(@object).Length;
				}
				else
				{
					Span<T> span = Unsafe.As<MemoryManager<T>>(@object).GetSpan();
					ptr = ref MemoryMarshal.GetReference(span);
					length = span.Length;
				}
				nuint num = (uint)_index & 0x7FFFFFFFu;
				int length2 = _length;
				if ((ulong)((long)num + (long)(uint)length2) > (ulong)(uint)length)
				{
					ThrowHelper.ThrowArgumentOutOfRangeException();
				}
				ptr = ref Unsafe.Add(ref ptr, (IntPtr)(void*)num);
				length = length2;
			}
			return new Span<T>(ref ptr, length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory(T[]? array)
	{
		if (array == null)
		{
			this = default(Memory<T>);
			return;
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		_object = array;
		_index = 0;
		_length = array.Length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(T[] array, int start)
	{
		if (array == null)
		{
			if (start != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(Memory<T>);
			return;
		}
		if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
		{
			ThrowHelper.ThrowArrayTypeMismatchException();
		}
		if ((uint)start > (uint)array.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = array;
		_index = start;
		_length = array.Length - start;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory(T[]? array, int start, int length)
	{
		if (array == null)
		{
			if (start != 0 || length != 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException();
			}
			this = default(Memory<T>);
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
		_object = array;
		_index = start;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(MemoryManager<T> manager, int length)
	{
		if (length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = manager;
		_index = 0;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(MemoryManager<T> manager, int start, int length)
	{
		if (length < 0 || start < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		_object = manager;
		_index = start;
		_length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Memory(object obj, int start, int length)
	{
		_object = obj;
		_index = start;
		_length = length;
	}

	public static implicit operator Memory<T>(T[]? array)
	{
		return new Memory<T>(array);
	}

	public static implicit operator Memory<T>(ArraySegment<T> segment)
	{
		return new Memory<T>(segment.Array, segment.Offset, segment.Count);
	}

	public static implicit operator ReadOnlyMemory<T>(Memory<T> memory)
	{
		return Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);
	}

	public override string ToString()
	{
		if (typeof(T) == typeof(char))
		{
			if (!(_object is string text))
			{
				return Span.ToString();
			}
			return text.Substring(_index, _length);
		}
		return $"System.Memory<{typeof(T).Name}>[{_length}]";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<T> Slice(int start)
	{
		if ((uint)start > (uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
		}
		return new Memory<T>(_object, _index + start, _length - start);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Memory<T> Slice(int start, int length)
	{
		if ((ulong)((long)(uint)start + (long)(uint)length) > (ulong)(uint)_length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException();
		}
		return new Memory<T>(_object, _index + start, length);
	}

	public void CopyTo(Memory<T> destination)
	{
		Span.CopyTo(destination.Span);
	}

	public bool TryCopyTo(Memory<T> destination)
	{
		return Span.TryCopyTo(destination.Span);
	}

	public unsafe MemoryHandle Pin()
	{
		object @object = _object;
		if (@object != null)
		{
			if (typeof(T) == typeof(char) && @object is string text)
			{
				GCHandle handle = GCHandle.Alloc(@object, GCHandleType.Pinned);
				return new MemoryHandle(Unsafe.AsPointer(ref Unsafe.Add(ref text.GetRawStringData(), _index)), handle);
			}
			if (RuntimeHelpers.ObjectHasComponentSize(@object))
			{
				if (_index < 0)
				{
					void* pointer = Unsafe.Add<T>(Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(@object))), _index & 0x7FFFFFFF);
					return new MemoryHandle(pointer);
				}
				GCHandle handle2 = GCHandle.Alloc(@object, GCHandleType.Pinned);
				void* pointer2 = Unsafe.Add<T>(Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<T[]>(@object))), _index);
				return new MemoryHandle(pointer2, handle2);
			}
			return Unsafe.As<MemoryManager<T>>(@object).Pin(_index);
		}
		return default(MemoryHandle);
	}

	public T[] ToArray()
	{
		return Span.ToArray();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ReadOnlyMemory<T> readOnlyMemory)
		{
			return readOnlyMemory.Equals(this);
		}
		if (obj is Memory<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Memory<T> other)
	{
		if (_object == other._object && _index == other._index)
		{
			return _length == other._length;
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public override int GetHashCode()
	{
		if (_object == null)
		{
			return 0;
		}
		return HashCode.Combine(RuntimeHelpers.GetHashCode(_object), _index, _length);
	}
}
