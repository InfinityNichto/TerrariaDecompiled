using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

internal ref struct ValueListBuilder<T>
{
	private Span<T> _span;

	private T[] _arrayFromPool;

	private int _pos;

	public int Length
	{
		get
		{
			return _pos;
		}
		set
		{
			_pos = value;
		}
	}

	public ref T this[int index] => ref _span[index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public T Pop()
	{
		_pos--;
		return _span[_pos];
	}

	public ValueListBuilder(Span<T> initialSpan)
	{
		_span = initialSpan;
		_arrayFromPool = null;
		_pos = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Append(T item)
	{
		int pos = _pos;
		if (pos >= _span.Length)
		{
			Grow();
		}
		_span[pos] = item;
		_pos = pos + 1;
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return _span.Slice(0, _pos);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose()
	{
		T[] arrayFromPool = _arrayFromPool;
		if (arrayFromPool != null)
		{
			_arrayFromPool = null;
			ArrayPool<T>.Shared.Return(arrayFromPool);
		}
	}

	private void Grow()
	{
		T[] array = ArrayPool<T>.Shared.Rent(_span.Length * 2);
		bool flag = _span.TryCopyTo(array);
		T[] arrayFromPool = _arrayFromPool;
		_span = (_arrayFromPool = array);
		if (arrayFromPool != null)
		{
			ArrayPool<T>.Shared.Return(arrayFromPool);
		}
	}
}
