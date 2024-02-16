using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers;

[DebuggerTypeProxy(typeof(ReadOnlySequenceDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly struct ReadOnlySequence<T>
{
	public struct Enumerator
	{
		private readonly ReadOnlySequence<T> _sequence;

		private SequencePosition _next;

		private ReadOnlyMemory<T> _currentMemory;

		public ReadOnlyMemory<T> Current => _currentMemory;

		public Enumerator(in ReadOnlySequence<T> sequence)
		{
			_currentMemory = default(ReadOnlyMemory<T>);
			_next = sequence.Start;
			_sequence = sequence;
		}

		public bool MoveNext()
		{
			if (_next.GetObject() == null)
			{
				return false;
			}
			return _sequence.TryGet(ref _next, out _currentMemory);
		}
	}

	private enum SequenceType
	{
		MultiSegment,
		Array,
		MemoryManager,
		String,
		Empty
	}

	private readonly object _startObject;

	private readonly object _endObject;

	private readonly int _startInteger;

	private readonly int _endInteger;

	public static readonly ReadOnlySequence<T> Empty = new ReadOnlySequence<T>(Array.Empty<T>());

	public long Length => GetLength();

	public bool IsEmpty => Length == 0;

	public bool IsSingleSegment
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return _startObject == _endObject;
		}
	}

	public ReadOnlyMemory<T> First => GetFirstBuffer();

	public ReadOnlySpan<T> FirstSpan => GetFirstSpan();

	public SequencePosition Start
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return new SequencePosition(_startObject, GetIndex(_startInteger));
		}
	}

	public SequencePosition End
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return new SequencePosition(_endObject, GetIndex(_endInteger));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlySequence(object startSegment, int startIndexAndFlags, object endSegment, int endIndexAndFlags)
	{
		_startObject = startSegment;
		_endObject = endSegment;
		_startInteger = startIndexAndFlags;
		_endInteger = endIndexAndFlags;
	}

	public ReadOnlySequence(ReadOnlySequenceSegment<T> startSegment, int startIndex, ReadOnlySequenceSegment<T> endSegment, int endIndex)
	{
		if (startSegment == null || endSegment == null || (startSegment != endSegment && startSegment.RunningIndex > endSegment.RunningIndex) || (uint)startSegment.Memory.Length < (uint)startIndex || (uint)endSegment.Memory.Length < (uint)endIndex || (startSegment == endSegment && endIndex < startIndex))
		{
			System.ThrowHelper.ThrowArgumentValidationException(startSegment, startIndex, endSegment);
		}
		_startObject = startSegment;
		_endObject = endSegment;
		_startInteger = startIndex;
		_endInteger = endIndex;
	}

	public ReadOnlySequence(T[] array)
	{
		if (array == null)
		{
			System.ThrowHelper.ThrowArgumentNullException(System.ExceptionArgument.array);
		}
		_startObject = array;
		_endObject = array;
		_startInteger = 0;
		_endInteger = ReadOnlySequence.ArrayToSequenceEnd(array.Length);
	}

	public ReadOnlySequence(T[] array, int start, int length)
	{
		if (array == null || (uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
		{
			System.ThrowHelper.ThrowArgumentValidationException(array, start);
		}
		_startObject = array;
		_endObject = array;
		_startInteger = start;
		_endInteger = ReadOnlySequence.ArrayToSequenceEnd(start + length);
	}

	public ReadOnlySequence(ReadOnlyMemory<T> memory)
	{
		ArraySegment<T> segment;
		if (MemoryMarshal.TryGetMemoryManager<T, MemoryManager<T>>(memory, out MemoryManager<T> manager, out int start, out int length))
		{
			_startObject = manager;
			_endObject = manager;
			_startInteger = ReadOnlySequence.MemoryManagerToSequenceStart(start);
			_endInteger = start + length;
		}
		else if (MemoryMarshal.TryGetArray(memory, out segment))
		{
			T[] array = segment.Array;
			int offset = segment.Offset;
			_startObject = array;
			_endObject = array;
			_startInteger = offset;
			_endInteger = ReadOnlySequence.ArrayToSequenceEnd(offset + segment.Count);
		}
		else if (typeof(T) == typeof(char))
		{
			if (!MemoryMarshal.TryGetString((ReadOnlyMemory<char>)(object)memory, out string text, out int start2, out length))
			{
				System.ThrowHelper.ThrowInvalidOperationException();
			}
			_startObject = text;
			_endObject = text;
			_startInteger = ReadOnlySequence.StringToSequenceStart(start2);
			_endInteger = ReadOnlySequence.StringToSequenceEnd(start2 + length);
		}
		else
		{
			System.ThrowHelper.ThrowInvalidOperationException();
			_startObject = null;
			_endObject = null;
			_startInteger = 0;
			_endInteger = 0;
		}
	}

	public ReadOnlySequence<T> Slice(long start, long length)
	{
		if (start < 0 || length < 0)
		{
			System.ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
		}
		int index = GetIndex(_startInteger);
		int index2 = GetIndex(_endInteger);
		object startObject = _startObject;
		object endObject = _endObject;
		SequencePosition start2;
		SequencePosition end;
		if (startObject != endObject)
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
			int num = readOnlySequenceSegment.Memory.Length - index;
			if (num > start)
			{
				index += (int)start;
				start2 = new SequencePosition(startObject, index);
				end = GetEndPosition(readOnlySequenceSegment, startObject, index, endObject, index2, length);
			}
			else
			{
				if (num < 0)
				{
					System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				start2 = SeekMultiSegment(readOnlySequenceSegment.Next, endObject, index2, start - num, System.ExceptionArgument.start);
				int integer = start2.GetInteger();
				object @object = start2.GetObject();
				if (@object != endObject)
				{
					end = GetEndPosition((ReadOnlySequenceSegment<T>)@object, @object, integer, endObject, index2, length);
				}
				else
				{
					if (index2 - integer < length)
					{
						System.ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
					}
					end = new SequencePosition(@object, integer + (int)length);
				}
			}
		}
		else
		{
			if (index2 - index < start)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
			}
			index += (int)start;
			start2 = new SequencePosition(startObject, index);
			if (index2 - index < length)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
			}
			end = new SequencePosition(startObject, index + (int)length);
		}
		return SliceImpl(in start2, in end);
	}

	public ReadOnlySequence<T> Slice(long start, SequencePosition end)
	{
		if (start < 0)
		{
			System.ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
		}
		uint index = (uint)GetIndex(_startInteger);
		object startObject = _startObject;
		uint index2 = (uint)GetIndex(_endInteger);
		object endObject = _endObject;
		uint num = (uint)end.GetInteger();
		object obj = end.GetObject();
		if (obj == null)
		{
			obj = _startObject;
			num = index;
		}
		if (startObject == endObject)
		{
			if (!InRange(num, index, index2))
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			if (num - index < start)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(-1L);
			}
		}
		else
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
			ulong num2 = (ulong)(readOnlySequenceSegment.RunningIndex + index);
			ulong num3 = (ulong)(((ReadOnlySequenceSegment<T>)obj).RunningIndex + num);
			if (!InRange(num3, num2, (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + index2)))
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			if ((ulong)((long)num2 + start) > num3)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.start);
			}
			int num4 = readOnlySequenceSegment.Memory.Length - (int)index;
			if (num4 <= start)
			{
				if (num4 < 0)
				{
					System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				SequencePosition start2 = SeekMultiSegment(readOnlySequenceSegment.Next, obj, (int)num, start - num4, System.ExceptionArgument.start);
				return SliceImpl(in start2, in end);
			}
		}
		SequencePosition start3 = new SequencePosition(startObject, (int)index + (int)start);
		SequencePosition end2 = new SequencePosition(obj, (int)num);
		return SliceImpl(in start3, in end2);
	}

	public ReadOnlySequence<T> Slice(SequencePosition start, long length)
	{
		uint index = (uint)GetIndex(_startInteger);
		object startObject = _startObject;
		uint index2 = (uint)GetIndex(_endInteger);
		object endObject = _endObject;
		uint num = (uint)start.GetInteger();
		object obj = start.GetObject();
		if (obj == null)
		{
			num = index;
			obj = _startObject;
		}
		if (startObject == endObject)
		{
			if (!InRange(num, index, index2))
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			if (length < 0)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
			}
			if (index2 - num < length)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
			}
		}
		else
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)obj;
			ulong num2 = (ulong)(readOnlySequenceSegment.RunningIndex + num);
			ulong start2 = (ulong)(((ReadOnlySequenceSegment<T>)startObject).RunningIndex + index);
			ulong num3 = (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + index2);
			if (!InRange(num2, start2, num3))
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			if (length < 0)
			{
				System.ThrowHelper.ThrowStartOrEndArgumentValidationException(0L);
			}
			if ((ulong)((long)num2 + length) > num3)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.length);
			}
			int num4 = readOnlySequenceSegment.Memory.Length - (int)num;
			if (num4 < length)
			{
				if (num4 < 0)
				{
					System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				SequencePosition end = SeekMultiSegment(readOnlySequenceSegment.Next, endObject, (int)index2, length - num4, System.ExceptionArgument.length);
				return SliceImpl(in start, in end);
			}
		}
		SequencePosition start3 = new SequencePosition(obj, (int)num);
		SequencePosition end2 = new SequencePosition(obj, (int)num + (int)length);
		return SliceImpl(in start3, in end2);
	}

	public ReadOnlySequence<T> Slice(int start, int length)
	{
		return Slice((long)start, (long)length);
	}

	public ReadOnlySequence<T> Slice(int start, SequencePosition end)
	{
		return Slice((long)start, end);
	}

	public ReadOnlySequence<T> Slice(SequencePosition start, int length)
	{
		return Slice(start, (long)length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySequence<T> Slice(SequencePosition start, SequencePosition end)
	{
		BoundsCheck((uint)start.GetInteger(), start.GetObject(), (uint)end.GetInteger(), end.GetObject());
		return SliceImpl(in start, in end);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySequence<T> Slice(SequencePosition start)
	{
		bool flag = start.GetObject() != null;
		BoundsCheck(in start, flag);
		SequencePosition start2 = (flag ? start : Start);
		return SliceImpl(in start2);
	}

	public ReadOnlySequence<T> Slice(long start)
	{
		if (start < 0)
		{
			System.ThrowHelper.ThrowStartOrEndArgumentValidationException(start);
		}
		if (start == 0L)
		{
			return this;
		}
		SequencePosition start2 = Seek(start, System.ExceptionArgument.start);
		return SliceImpl(in start2);
	}

	public override string ToString()
	{
		if (typeof(T) == typeof(char))
		{
			ReadOnlySequence<T> source = this;
			ReadOnlySequence<char> state = Internal.Runtime.CompilerServices.Unsafe.As<ReadOnlySequence<T>, ReadOnlySequence<char>>(ref source);
			if (state.TryGetString(out var text, out var start, out var length))
			{
				return text.Substring(start, length);
			}
			if (Length < int.MaxValue)
			{
				return string.Create((int)Length, state, delegate(Span<char> span, ReadOnlySequence<char> sequence)
				{
					sequence.CopyTo(span);
				});
			}
		}
		return $"System.Buffers.ReadOnlySequence<{typeof(T).Name}>[{Length}]";
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(in this);
	}

	public SequencePosition GetPosition(long offset)
	{
		if (offset < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
		}
		return Seek(offset);
	}

	public long GetOffset(SequencePosition position)
	{
		object obj = position.GetObject();
		bool flag = obj == null;
		BoundsCheck(in position, !flag);
		object startObject = _startObject;
		object endObject = _endObject;
		uint num = (uint)position.GetInteger();
		if (flag)
		{
			obj = _startObject;
			num = (uint)GetIndex(_startInteger);
		}
		if (startObject == endObject)
		{
			return num;
		}
		if (((ReadOnlySequenceSegment<T>)obj).Memory.Length - num < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
		ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
		while (readOnlySequenceSegment != null && readOnlySequenceSegment != obj)
		{
			readOnlySequenceSegment = readOnlySequenceSegment.Next;
		}
		if (readOnlySequenceSegment == null)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
		return readOnlySequenceSegment.RunningIndex + num;
	}

	public SequencePosition GetPosition(long offset, SequencePosition origin)
	{
		if (offset < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
		}
		return Seek(in origin, offset);
	}

	public bool TryGet(ref SequencePosition position, out ReadOnlyMemory<T> memory, bool advance = true)
	{
		SequencePosition next;
		bool result = TryGetBuffer(in position, out memory, out next);
		if (advance)
		{
			position = next;
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal bool TryGetBuffer(in SequencePosition position, out ReadOnlyMemory<T> memory, out SequencePosition next)
	{
		object @object = position.GetObject();
		next = default(SequencePosition);
		if (@object == null)
		{
			memory = default(ReadOnlyMemory<T>);
			return false;
		}
		SequenceType sequenceType = GetSequenceType();
		object endObject = _endObject;
		int integer = position.GetInteger();
		int index = GetIndex(_endInteger);
		if (sequenceType == SequenceType.MultiSegment)
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
			if (readOnlySequenceSegment != endObject)
			{
				ReadOnlySequenceSegment<T> next2 = readOnlySequenceSegment.Next;
				if (next2 == null)
				{
					System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				next = new SequencePosition(next2, 0);
				memory = readOnlySequenceSegment.Memory.Slice(integer);
			}
			else
			{
				memory = readOnlySequenceSegment.Memory.Slice(integer, index - integer);
			}
		}
		else
		{
			if (@object != endObject)
			{
				System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
			}
			if (sequenceType == SequenceType.Array)
			{
				memory = new ReadOnlyMemory<T>((T[])@object, integer, index - integer);
			}
			else if (typeof(T) == typeof(char) && sequenceType == SequenceType.String)
			{
				memory = (ReadOnlyMemory<T>)(object)((string)@object).AsMemory(integer, index - integer);
			}
			else
			{
				memory = ((MemoryManager<T>)@object).Memory.Slice(integer, index - integer);
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlyMemory<T> GetFirstBuffer()
	{
		object startObject = _startObject;
		if (startObject == null)
		{
			return default(ReadOnlyMemory<T>);
		}
		int startInteger = _startInteger;
		int endInteger = _endInteger;
		bool flag = startObject != _endObject;
		if ((startInteger | endInteger) >= 0)
		{
			ReadOnlyMemory<T> memory = ((ReadOnlySequenceSegment<T>)startObject).Memory;
			if (flag)
			{
				return memory.Slice(startInteger);
			}
			return memory.Slice(startInteger, endInteger - startInteger);
		}
		return GetFirstBufferSlow(startObject, flag);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private ReadOnlyMemory<T> GetFirstBufferSlow(object startObject, bool isMultiSegment)
	{
		if (isMultiSegment)
		{
			System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
		}
		int startInteger = _startInteger;
		int endInteger = _endInteger;
		if (startInteger >= 0)
		{
			return new ReadOnlyMemory<T>((T[])startObject, startInteger, (endInteger & 0x7FFFFFFF) - startInteger);
		}
		if (typeof(T) == typeof(char) && endInteger < 0)
		{
			return (ReadOnlyMemory<T>)(object)((string)startObject).AsMemory(startInteger & 0x7FFFFFFF, endInteger - startInteger);
		}
		startInteger &= 0x7FFFFFFF;
		return ((MemoryManager<T>)startObject).Memory.Slice(startInteger, endInteger - startInteger);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlySpan<T> GetFirstSpan()
	{
		object startObject = _startObject;
		if (startObject == null)
		{
			return default(ReadOnlySpan<T>);
		}
		int startInteger = _startInteger;
		int endInteger = _endInteger;
		bool flag = startObject != _endObject;
		if ((startInteger | endInteger) >= 0)
		{
			ReadOnlySpan<T> span = ((ReadOnlySequenceSegment<T>)startObject).Memory.Span;
			if (flag)
			{
				return span.Slice(startInteger);
			}
			return span.Slice(startInteger, endInteger - startInteger);
		}
		return GetFirstSpanSlow(startObject, flag);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private ReadOnlySpan<T> GetFirstSpanSlow(object startObject, bool isMultiSegment)
	{
		if (isMultiSegment)
		{
			System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
		}
		int startInteger = _startInteger;
		int endInteger = _endInteger;
		if (startInteger >= 0)
		{
			return ((ReadOnlySpan<T>)(T[])startObject).Slice(startInteger, (endInteger & 0x7FFFFFFF) - startInteger);
		}
		if (typeof(T) == typeof(char) && endInteger < 0)
		{
			return ((ReadOnlyMemory<T>)(object)((string)startObject).AsMemory()).Span.Slice(startInteger & 0x7FFFFFFF, endInteger - startInteger);
		}
		startInteger &= 0x7FFFFFFF;
		return ((MemoryManager<T>)startObject).Memory.Span.Slice(startInteger, endInteger - startInteger);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal SequencePosition Seek(long offset, System.ExceptionArgument exceptionArgument = System.ExceptionArgument.offset)
	{
		object startObject = _startObject;
		object endObject = _endObject;
		int index = GetIndex(_startInteger);
		int index2 = GetIndex(_endInteger);
		if (startObject != endObject)
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
			int num = readOnlySequenceSegment.Memory.Length - index;
			if (num <= offset && offset != 0L)
			{
				if (num < 0)
				{
					System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				return SeekMultiSegment(readOnlySequenceSegment.Next, endObject, index2, offset - num, exceptionArgument);
			}
		}
		else if (index2 - index < offset)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(exceptionArgument);
		}
		return new SequencePosition(startObject, index + (int)offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private SequencePosition Seek(in SequencePosition start, long offset)
	{
		object @object = start.GetObject();
		object endObject = _endObject;
		int integer = start.GetInteger();
		int index = GetIndex(_endInteger);
		if (@object != endObject)
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)@object;
			int num = readOnlySequenceSegment.Memory.Length - integer;
			if (num <= offset)
			{
				if (num < 0)
				{
					System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
				}
				return SeekMultiSegment(readOnlySequenceSegment.Next, endObject, index, offset - num, System.ExceptionArgument.offset);
			}
		}
		else if (index - integer < offset)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.offset);
		}
		return new SequencePosition(@object, integer + (int)offset);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static SequencePosition SeekMultiSegment(ReadOnlySequenceSegment<T> currentSegment, object endObject, int endIndex, long offset, System.ExceptionArgument argument)
	{
		while (true)
		{
			if (currentSegment != null && currentSegment != endObject)
			{
				int length = currentSegment.Memory.Length;
				if (length > offset)
				{
					break;
				}
				offset -= length;
				currentSegment = currentSegment.Next;
				continue;
			}
			if (currentSegment == null || endIndex < offset)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException(argument);
			}
			break;
		}
		return new SequencePosition(currentSegment, (int)offset);
	}

	private void BoundsCheck(in SequencePosition position, bool positionIsNotNull)
	{
		uint integer = (uint)position.GetInteger();
		object startObject = _startObject;
		object endObject = _endObject;
		uint index = (uint)GetIndex(_startInteger);
		uint index2 = (uint)GetIndex(_endInteger);
		if (startObject == endObject)
		{
			if (!InRange(integer, index, index2))
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			return;
		}
		ulong start = (ulong)(((ReadOnlySequenceSegment<T>)startObject).RunningIndex + index);
		long num = 0L;
		if (positionIsNotNull)
		{
			num = ((ReadOnlySequenceSegment<T>)position.GetObject()).RunningIndex;
		}
		if (!InRange((ulong)(num + integer), start, (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + index2)))
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
	}

	private void BoundsCheck(uint sliceStartIndex, object sliceStartObject, uint sliceEndIndex, object sliceEndObject)
	{
		object startObject = _startObject;
		object endObject = _endObject;
		uint index = (uint)GetIndex(_startInteger);
		uint index2 = (uint)GetIndex(_endInteger);
		if (startObject == endObject)
		{
			if (sliceStartObject != sliceEndObject || sliceStartObject != startObject || sliceStartIndex > sliceEndIndex || sliceStartIndex < index || sliceEndIndex > index2)
			{
				System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
			}
			return;
		}
		ulong num = sliceStartIndex;
		ulong num2 = sliceEndIndex;
		if (sliceStartObject != null)
		{
			num += (ulong)((ReadOnlySequenceSegment<T>)sliceStartObject).RunningIndex;
		}
		if (sliceEndObject != null)
		{
			num2 += (ulong)((ReadOnlySequenceSegment<T>)sliceEndObject).RunningIndex;
		}
		if (num > num2)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
		if (num < (ulong)(((ReadOnlySequenceSegment<T>)startObject).RunningIndex + index) || num2 > (ulong)(((ReadOnlySequenceSegment<T>)endObject).RunningIndex + index2))
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
	}

	private static SequencePosition GetEndPosition(ReadOnlySequenceSegment<T> startSegment, object startObject, int startIndex, object endObject, int endIndex, long length)
	{
		int num = startSegment.Memory.Length - startIndex;
		if (num > length)
		{
			return new SequencePosition(startObject, startIndex + (int)length);
		}
		if (num < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_PositionOutOfRange();
		}
		return SeekMultiSegment(startSegment.Next, endObject, endIndex, length - num, System.ExceptionArgument.length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private SequenceType GetSequenceType()
	{
		return (SequenceType)(-(2 * (_startInteger >> 31) + (_endInteger >> 31)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetIndex(int Integer)
	{
		return Integer & 0x7FFFFFFF;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlySequence<T> SliceImpl(in SequencePosition start, in SequencePosition end)
	{
		return new ReadOnlySequence<T>(start.GetObject(), start.GetInteger() | (_startInteger & int.MinValue), end.GetObject(), end.GetInteger() | (_endInteger & int.MinValue));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ReadOnlySequence<T> SliceImpl(in SequencePosition start)
	{
		return new ReadOnlySequence<T>(start.GetObject(), start.GetInteger() | (_startInteger & int.MinValue), _endObject, _endInteger);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long GetLength()
	{
		object startObject = _startObject;
		object endObject = _endObject;
		int index = GetIndex(_startInteger);
		int index2 = GetIndex(_endInteger);
		if (startObject != endObject)
		{
			ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
			ReadOnlySequenceSegment<T> readOnlySequenceSegment2 = (ReadOnlySequenceSegment<T>)endObject;
			return readOnlySequenceSegment2.RunningIndex + index2 - (readOnlySequenceSegment.RunningIndex + index);
		}
		return index2 - index;
	}

	internal bool TryGetReadOnlySequenceSegment([NotNullWhen(true)] out ReadOnlySequenceSegment<T> startSegment, out int startIndex, [NotNullWhen(true)] out ReadOnlySequenceSegment<T> endSegment, out int endIndex)
	{
		object startObject = _startObject;
		if (startObject == null || GetSequenceType() != 0)
		{
			startSegment = null;
			startIndex = 0;
			endSegment = null;
			endIndex = 0;
			return false;
		}
		startSegment = (ReadOnlySequenceSegment<T>)startObject;
		startIndex = GetIndex(_startInteger);
		endSegment = (ReadOnlySequenceSegment<T>)_endObject;
		endIndex = GetIndex(_endInteger);
		return true;
	}

	internal bool TryGetArray(out ArraySegment<T> segment)
	{
		if (GetSequenceType() != SequenceType.Array)
		{
			segment = default(ArraySegment<T>);
			return false;
		}
		int index = GetIndex(_startInteger);
		segment = new ArraySegment<T>((T[])_startObject, index, GetIndex(_endInteger) - index);
		return true;
	}

	internal bool TryGetString([NotNullWhen(true)] out string text, out int start, out int length)
	{
		if (typeof(T) != typeof(char) || GetSequenceType() != SequenceType.String)
		{
			start = 0;
			length = 0;
			text = null;
			return false;
		}
		start = GetIndex(_startInteger);
		length = GetIndex(_endInteger) - start;
		text = (string)_startObject;
		return true;
	}

	private static bool InRange(uint value, uint start, uint end)
	{
		return value - start <= end - start;
	}

	private static bool InRange(ulong value, ulong start, ulong end)
	{
		return value - start <= end - start;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void GetFirstSpan(out ReadOnlySpan<T> first, out SequencePosition next)
	{
		first = default(ReadOnlySpan<T>);
		next = default(SequencePosition);
		object startObject = _startObject;
		int startInteger = _startInteger;
		if (startObject == null)
		{
			return;
		}
		bool flag = startObject != _endObject;
		int endInteger = _endInteger;
		if (startInteger >= 0)
		{
			if (endInteger >= 0)
			{
				ReadOnlySequenceSegment<T> readOnlySequenceSegment = (ReadOnlySequenceSegment<T>)startObject;
				first = readOnlySequenceSegment.Memory.Span;
				if (flag)
				{
					first = first.Slice(startInteger);
					next = new SequencePosition(readOnlySequenceSegment.Next, 0);
				}
				else
				{
					first = first.Slice(startInteger, endInteger - startInteger);
				}
			}
			else
			{
				if (flag)
				{
					System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
				}
				first = new ReadOnlySpan<T>((T[])startObject, startInteger, (endInteger & 0x7FFFFFFF) - startInteger);
			}
		}
		else
		{
			first = GetFirstSpanSlow(startObject, startInteger, endInteger, flag);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ReadOnlySpan<T> GetFirstSpanSlow(object startObject, int startIndex, int endIndex, bool hasMultipleSegments)
	{
		if (hasMultipleSegments)
		{
			System.ThrowHelper.ThrowInvalidOperationException_EndPositionNotReached();
		}
		if (typeof(T) == typeof(char) && endIndex < 0)
		{
			ReadOnlySpan<char> span = ((string)startObject).AsSpan(startIndex & 0x7FFFFFFF, endIndex - startIndex);
			return MemoryMarshal.CreateReadOnlySpan(ref Internal.Runtime.CompilerServices.Unsafe.As<char, T>(ref MemoryMarshal.GetReference(span)), span.Length);
		}
		startIndex &= 0x7FFFFFFF;
		return ((MemoryManager<T>)startObject).Memory.Span.Slice(startIndex, endIndex - startIndex);
	}
}
internal static class ReadOnlySequence
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ArrayToSequenceEnd(int endIndex)
	{
		return endIndex | int.MinValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int MemoryManagerToSequenceStart(int startIndex)
	{
		return startIndex | int.MinValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int StringToSequenceStart(int startIndex)
	{
		return startIndex | int.MinValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int StringToSequenceEnd(int endIndex)
	{
		return endIndex | int.MinValue;
	}
}
