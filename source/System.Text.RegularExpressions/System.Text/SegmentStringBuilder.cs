using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text;

[DebuggerDisplay("Count = {_count}")]
internal struct SegmentStringBuilder
{
	private ReadOnlyMemory<char>[] _array;

	private int _count;

	public int Count => _count;

	public static SegmentStringBuilder Create()
	{
		SegmentStringBuilder result = default(SegmentStringBuilder);
		result._array = Array.Empty<ReadOnlyMemory<char>>();
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(ReadOnlyMemory<char> segment)
	{
		ReadOnlyMemory<char>[] array = _array;
		int count = _count;
		if ((uint)count < (uint)array.Length)
		{
			array[count] = segment;
			_count = count + 1;
		}
		else
		{
			GrowAndAdd(segment);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void GrowAndAdd(ReadOnlyMemory<char> segment)
	{
		ReadOnlyMemory<char>[] array = _array;
		int minimumLength = ((array.Length == 0) ? 256 : (array.Length * 2));
		ReadOnlyMemory<char>[] array2 = (_array = ArrayPool<ReadOnlyMemory<char>>.Shared.Rent(minimumLength));
		Array.Copy(array, array2, _count);
		ArrayPool<ReadOnlyMemory<char>>.Shared.Return(array, clearArray: true);
		array2[_count++] = segment;
	}

	public Span<ReadOnlyMemory<char>> AsSpan()
	{
		return new Span<ReadOnlyMemory<char>>(_array, 0, _count);
	}

	public override string ToString()
	{
		ReadOnlyMemory<char>[] array = _array;
		Span<ReadOnlyMemory<char>> span = new Span<ReadOnlyMemory<char>>(array, 0, _count);
		int num = 0;
		for (int i = 0; i < span.Length; i++)
		{
			num += span[i].Length;
		}
		string result = string.Create(num, this, delegate(Span<char> dest, SegmentStringBuilder builder)
		{
			Span<ReadOnlyMemory<char>> span2 = builder.AsSpan();
			for (int j = 0; j < span2.Length; j++)
			{
				ReadOnlySpan<char> span3 = span2[j].Span;
				span3.CopyTo(dest);
				dest = dest.Slice(span3.Length);
			}
		});
		span.Clear();
		this = default(SegmentStringBuilder);
		ArrayPool<ReadOnlyMemory<char>>.Shared.Return(array);
		return result;
	}
}
