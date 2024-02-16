using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers;

public ref struct SequenceReader<T> where T : unmanaged, IEquatable<T>
{
	private SequencePosition _currentPosition;

	private SequencePosition _nextPosition;

	private bool _moreData;

	private readonly long _length;

	public readonly bool End => !_moreData;

	public ReadOnlySequence<T> Sequence { get; }

	public readonly ReadOnlySequence<T> UnreadSequence => Sequence.Slice(Position);

	public readonly SequencePosition Position => Sequence.GetPosition(CurrentSpanIndex, _currentPosition);

	public ReadOnlySpan<T> CurrentSpan { get; private set; }

	public int CurrentSpanIndex { get; private set; }

	public readonly ReadOnlySpan<T> UnreadSpan
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return CurrentSpan.Slice(CurrentSpanIndex);
		}
	}

	public long Consumed { get; private set; }

	public readonly long Remaining => Length - Consumed;

	public readonly long Length
	{
		get
		{
			if (_length < 0)
			{
				Internal.Runtime.CompilerServices.Unsafe.AsRef(in _length) = Sequence.Length;
			}
			return _length;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public SequenceReader(ReadOnlySequence<T> sequence)
	{
		CurrentSpanIndex = 0;
		Consumed = 0L;
		Sequence = sequence;
		_currentPosition = sequence.Start;
		_length = -1L;
		sequence.GetFirstSpan(out var first, out _nextPosition);
		CurrentSpan = first;
		_moreData = first.Length > 0;
		if (!_moreData && !sequence.IsSingleSegment)
		{
			_moreData = true;
			GetNextSpan();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryPeek(out T value)
	{
		if (_moreData)
		{
			value = CurrentSpan[CurrentSpanIndex];
			return true;
		}
		value = default(T);
		return false;
	}

	public readonly bool TryPeek(long offset, out T value)
	{
		if (offset < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException_OffsetOutOfRange();
		}
		if (!_moreData || Remaining <= offset)
		{
			value = default(T);
			return false;
		}
		if (CurrentSpanIndex + offset <= CurrentSpan.Length - 1)
		{
			value = CurrentSpan[CurrentSpanIndex + (int)offset];
			return true;
		}
		long num = offset - (CurrentSpan.Length - CurrentSpanIndex);
		SequencePosition position = _nextPosition;
		ReadOnlyMemory<T> memory;
		while (Sequence.TryGet(ref position, out memory))
		{
			if (memory.Length > 0)
			{
				if (num < memory.Length)
				{
					break;
				}
				num -= memory.Length;
			}
		}
		value = memory.Span[(int)num];
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryRead(out T value)
	{
		if (End)
		{
			value = default(T);
			return false;
		}
		value = CurrentSpan[CurrentSpanIndex];
		CurrentSpanIndex++;
		Consumed++;
		if (CurrentSpanIndex >= CurrentSpan.Length)
		{
			GetNextSpan();
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Rewind(long count)
	{
		if ((ulong)count > (ulong)Consumed)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.count);
		}
		Consumed -= count;
		if (CurrentSpanIndex >= count)
		{
			CurrentSpanIndex -= (int)count;
			_moreData = true;
		}
		else
		{
			RetreatToPreviousSpan(Consumed);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void RetreatToPreviousSpan(long consumed)
	{
		ResetReader();
		Advance(consumed);
	}

	private void ResetReader()
	{
		CurrentSpanIndex = 0;
		Consumed = 0L;
		_currentPosition = Sequence.Start;
		_nextPosition = _currentPosition;
		if (Sequence.TryGet(ref _nextPosition, out var memory))
		{
			_moreData = true;
			if (memory.Length == 0)
			{
				CurrentSpan = default(ReadOnlySpan<T>);
				GetNextSpan();
			}
			else
			{
				CurrentSpan = memory.Span;
			}
		}
		else
		{
			_moreData = false;
			CurrentSpan = default(ReadOnlySpan<T>);
		}
	}

	private void GetNextSpan()
	{
		if (!Sequence.IsSingleSegment)
		{
			SequencePosition nextPosition = _nextPosition;
			ReadOnlyMemory<T> memory;
			while (Sequence.TryGet(ref _nextPosition, out memory))
			{
				_currentPosition = nextPosition;
				if (memory.Length > 0)
				{
					CurrentSpan = memory.Span;
					CurrentSpanIndex = 0;
					return;
				}
				CurrentSpan = default(ReadOnlySpan<T>);
				CurrentSpanIndex = 0;
				nextPosition = _nextPosition;
			}
		}
		_moreData = false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Advance(long count)
	{
		if ((count & int.MinValue) == 0L && CurrentSpan.Length - CurrentSpanIndex > (int)count)
		{
			CurrentSpanIndex += (int)count;
			Consumed += count;
		}
		else
		{
			AdvanceToNextSpan(count);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void AdvanceCurrentSpan(long count)
	{
		Consumed += count;
		CurrentSpanIndex += (int)count;
		if (CurrentSpanIndex >= CurrentSpan.Length)
		{
			GetNextSpan();
		}
	}

	private void AdvanceToNextSpan(long count)
	{
		if (count < 0)
		{
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.count);
		}
		Consumed += count;
		while (_moreData)
		{
			int num = CurrentSpan.Length - CurrentSpanIndex;
			if (num > count)
			{
				CurrentSpanIndex += (int)count;
				count = 0L;
				break;
			}
			CurrentSpanIndex += num;
			count -= num;
			GetNextSpan();
			if (count == 0L)
			{
				break;
			}
		}
		if (count != 0L)
		{
			Consumed -= count;
			System.ThrowHelper.ThrowArgumentOutOfRangeException(System.ExceptionArgument.count);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public readonly bool TryCopyTo(Span<T> destination)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		if (unreadSpan.Length >= destination.Length)
		{
			unreadSpan.Slice(0, destination.Length).CopyTo(destination);
			return true;
		}
		return TryCopyMultisegment(destination);
	}

	internal readonly bool TryCopyMultisegment(Span<T> destination)
	{
		if (Remaining < destination.Length)
		{
			return false;
		}
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		unreadSpan.CopyTo(destination);
		int num = unreadSpan.Length;
		SequencePosition position = _nextPosition;
		ReadOnlyMemory<T> memory;
		while (Sequence.TryGet(ref position, out memory))
		{
			if (memory.Length > 0)
			{
				ReadOnlySpan<T> span = memory.Span;
				int num2 = Math.Min(span.Length, destination.Length - num);
				span.Slice(0, num2).CopyTo(destination.Slice(num));
				num += num2;
				if (num >= destination.Length)
				{
					break;
				}
			}
		}
		return true;
	}

	public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = unreadSpan.IndexOf(delimiter);
		if (num != -1)
		{
			span = ((num == 0) ? default(ReadOnlySpan<T>) : unreadSpan.Slice(0, num));
			AdvanceCurrentSpan(num + (advancePastDelimiter ? 1 : 0));
			return true;
		}
		return TryReadToSlow(out span, delimiter, advancePastDelimiter);
	}

	private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter)
	{
		if (!TryReadToInternal(out var sequence, delimiter, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
		{
			span = default(ReadOnlySpan<T>);
			return false;
		}
		span = (sequence.IsSingleSegment ? sequence.First.Span : ((ReadOnlySpan<T>)BuffersExtensions.ToArray(in sequence)));
		return true;
	}

	public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = unreadSpan.IndexOf(delimiter);
		if ((num > 0 && !unreadSpan[num - 1].Equals(delimiterEscape)) || num == 0)
		{
			span = unreadSpan.Slice(0, num);
			AdvanceCurrentSpan(num + (advancePastDelimiter ? 1 : 0));
			return true;
		}
		return TryReadToSlow(out span, delimiter, delimiterEscape, num, advancePastDelimiter);
	}

	private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
	{
		if (!TryReadToSlow(out ReadOnlySequence<T> sequence, delimiter, delimiterEscape, index, advancePastDelimiter))
		{
			span = default(ReadOnlySpan<T>);
			return false;
		}
		span = (sequence.IsSingleSegment ? sequence.First.Span : ((ReadOnlySpan<T>)BuffersExtensions.ToArray(in sequence)));
		return true;
	}

	private bool TryReadToSlow(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
	{
		SequenceReader<T> sequenceReader = this;
		ReadOnlySpan<T> span = UnreadSpan;
		bool flag = false;
		do
		{
			if (index >= 0)
			{
				if (!(index == 0 && flag))
				{
					if (index > 0 && span[index - 1].Equals(delimiterEscape))
					{
						int num = 1;
						int num2 = index - 2;
						while (num2 >= 0 && span[num2].Equals(delimiterEscape))
						{
							num2--;
						}
						if (num2 < 0 && flag)
						{
							num++;
						}
						num += index - 2 - num2;
						if (((uint)num & (true ? 1u : 0u)) != 0)
						{
							Advance(index + 1);
							flag = false;
							span = UnreadSpan;
							goto IL_01bd;
						}
					}
					AdvanceCurrentSpan(index);
					sequence = Sequence.Slice(sequenceReader.Position, Position);
					if (advancePastDelimiter)
					{
						Advance(1L);
					}
					return true;
				}
				flag = false;
				Advance(index + 1);
				span = UnreadSpan;
			}
			else
			{
				if (span.Length > 0 && span[span.Length - 1].Equals(delimiterEscape))
				{
					int num3 = 1;
					int num4 = span.Length - 2;
					while (num4 >= 0 && span[num4].Equals(delimiterEscape))
					{
						num4--;
					}
					num3 += span.Length - 2 - num4;
					flag = ((!(num4 < 0 && flag)) ? ((num3 & 1) != 0) : ((num3 & 1) == 0));
				}
				else
				{
					flag = false;
				}
				AdvanceCurrentSpan(span.Length);
				span = CurrentSpan;
			}
			goto IL_01bd;
			IL_01bd:
			index = span.IndexOf(delimiter);
		}
		while (!End);
		this = sequenceReader;
		sequence = default(ReadOnlySequence<T>);
		return false;
	}

	public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter = true)
	{
		return TryReadToInternal(out sequence, delimiter, advancePastDelimiter);
	}

	private bool TryReadToInternal(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter, int skip = 0)
	{
		SequenceReader<T> sequenceReader = this;
		if (skip > 0)
		{
			Advance(skip);
		}
		ReadOnlySpan<T> span = UnreadSpan;
		while (_moreData)
		{
			int num = span.IndexOf(delimiter);
			if (num != -1)
			{
				if (num > 0)
				{
					AdvanceCurrentSpan(num);
				}
				sequence = Sequence.Slice(sequenceReader.Position, Position);
				if (advancePastDelimiter)
				{
					Advance(1L);
				}
				return true;
			}
			AdvanceCurrentSpan(span.Length);
			span = CurrentSpan;
		}
		this = sequenceReader;
		sequence = default(ReadOnlySequence<T>);
		return false;
	}

	public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
	{
		SequenceReader<T> sequenceReader = this;
		ReadOnlySpan<T> span = UnreadSpan;
		bool flag = false;
		while (_moreData)
		{
			int num = span.IndexOf(delimiter);
			if (num != -1)
			{
				if (!(num == 0 && flag))
				{
					if (num > 0 && span[num - 1].Equals(delimiterEscape))
					{
						int num2 = 0;
						int num3 = num;
						while (num3 > 0 && span[num3 - 1].Equals(delimiterEscape))
						{
							num3--;
							num2++;
						}
						if (num2 == num && flag)
						{
							num2++;
						}
						flag = false;
						if (((uint)num2 & (true ? 1u : 0u)) != 0)
						{
							Advance(num + 1);
							span = UnreadSpan;
							continue;
						}
					}
					if (num > 0)
					{
						Advance(num);
					}
					sequence = Sequence.Slice(sequenceReader.Position, Position);
					if (advancePastDelimiter)
					{
						Advance(1L);
					}
					return true;
				}
				flag = false;
				Advance(num + 1);
				span = UnreadSpan;
			}
			else
			{
				int num4 = 0;
				int num5 = span.Length;
				while (num5 > 0 && span[num5 - 1].Equals(delimiterEscape))
				{
					num5--;
					num4++;
				}
				if (flag && num4 == span.Length)
				{
					num4++;
				}
				flag = num4 % 2 != 0;
				Advance(span.Length);
				span = CurrentSpan;
			}
		}
		this = sequenceReader;
		sequence = default(ReadOnlySequence<T>);
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadToAny(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = ((delimiters.Length == 2) ? unreadSpan.IndexOfAny(delimiters[0], delimiters[1]) : unreadSpan.IndexOfAny(delimiters));
		if (num != -1)
		{
			span = unreadSpan.Slice(0, num);
			Advance(num + (advancePastDelimiter ? 1 : 0));
			return true;
		}
		return TryReadToAnySlow(out span, delimiters, advancePastDelimiter);
	}

	private bool TryReadToAnySlow(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiters, bool advancePastDelimiter)
	{
		if (!TryReadToAnyInternal(out var sequence, delimiters, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
		{
			span = default(ReadOnlySpan<T>);
			return false;
		}
		span = (sequence.IsSingleSegment ? sequence.First.Span : ((ReadOnlySpan<T>)BuffersExtensions.ToArray(in sequence)));
		return true;
	}

	public bool TryReadToAny(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
	{
		return TryReadToAnyInternal(out sequence, delimiters, advancePastDelimiter);
	}

	private bool TryReadToAnyInternal(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiters, bool advancePastDelimiter, int skip = 0)
	{
		SequenceReader<T> sequenceReader = this;
		if (skip > 0)
		{
			Advance(skip);
		}
		ReadOnlySpan<T> span = UnreadSpan;
		while (!End)
		{
			int num = ((delimiters.Length == 2) ? span.IndexOfAny(delimiters[0], delimiters[1]) : span.IndexOfAny(delimiters));
			if (num != -1)
			{
				if (num > 0)
				{
					AdvanceCurrentSpan(num);
				}
				sequence = Sequence.Slice(sequenceReader.Position, Position);
				if (advancePastDelimiter)
				{
					Advance(1L);
				}
				return true;
			}
			Advance(span.Length);
			span = CurrentSpan;
		}
		this = sequenceReader;
		sequence = default(ReadOnlySequence<T>);
		return false;
	}

	public bool TryReadTo(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = unreadSpan.IndexOf(delimiter);
		if (num >= 0)
		{
			span = unreadSpan.Slice(0, num);
			AdvanceCurrentSpan(num + (advancePastDelimiter ? delimiter.Length : 0));
			return true;
		}
		return TryReadToSlow(out span, delimiter, advancePastDelimiter);
	}

	private bool TryReadToSlow(out ReadOnlySpan<T> span, ReadOnlySpan<T> delimiter, bool advancePastDelimiter)
	{
		if (!TryReadTo(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter))
		{
			span = default(ReadOnlySpan<T>);
			return false;
		}
		span = (sequence.IsSingleSegment ? sequence.First.Span : ((ReadOnlySpan<T>)BuffersExtensions.ToArray(in sequence)));
		return true;
	}

	public bool TryReadTo(out ReadOnlySequence<T> sequence, ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true)
	{
		if (delimiter.Length == 0)
		{
			sequence = default(ReadOnlySequence<T>);
			return true;
		}
		SequenceReader<T> sequenceReader = this;
		bool flag = false;
		while (!End)
		{
			if (!TryReadTo(out sequence, delimiter[0], advancePastDelimiter: false))
			{
				this = sequenceReader;
				return false;
			}
			if (delimiter.Length == 1)
			{
				if (advancePastDelimiter)
				{
					Advance(1L);
				}
				return true;
			}
			if (IsNext(delimiter))
			{
				if (flag)
				{
					sequence = sequenceReader.Sequence.Slice(sequenceReader.Consumed, Consumed - sequenceReader.Consumed);
				}
				if (advancePastDelimiter)
				{
					Advance(delimiter.Length);
				}
				return true;
			}
			Advance(1L);
			flag = true;
		}
		this = sequenceReader;
		sequence = default(ReadOnlySequence<T>);
		return false;
	}

	public bool TryAdvanceTo(T delimiter, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = unreadSpan.IndexOf(delimiter);
		if (num != -1)
		{
			Advance(advancePastDelimiter ? (num + 1) : num);
			return true;
		}
		ReadOnlySequence<T> sequence;
		return TryReadToInternal(out sequence, delimiter, advancePastDelimiter);
	}

	public bool TryAdvanceToAny(ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		int num = unreadSpan.IndexOfAny(delimiters);
		if (num != -1)
		{
			AdvanceCurrentSpan(num + (advancePastDelimiter ? 1 : 0));
			return true;
		}
		ReadOnlySequence<T> sequence;
		return TryReadToAnyInternal(out sequence, delimiters, advancePastDelimiter);
	}

	public long AdvancePast(T value)
	{
		long consumed = Consumed;
		do
		{
			int i;
			for (i = CurrentSpanIndex; i < CurrentSpan.Length && CurrentSpan[i].Equals(value); i++)
			{
			}
			int num = i - CurrentSpanIndex;
			if (num == 0)
			{
				break;
			}
			AdvanceCurrentSpan(num);
		}
		while (CurrentSpanIndex == 0 && !End);
		return Consumed - consumed;
	}

	public long AdvancePastAny(ReadOnlySpan<T> values)
	{
		long consumed = Consumed;
		do
		{
			int i;
			for (i = CurrentSpanIndex; i < CurrentSpan.Length && values.IndexOf(CurrentSpan[i]) != -1; i++)
			{
			}
			int num = i - CurrentSpanIndex;
			if (num == 0)
			{
				break;
			}
			AdvanceCurrentSpan(num);
		}
		while (CurrentSpanIndex == 0 && !End);
		return Consumed - consumed;
	}

	public long AdvancePastAny(T value0, T value1, T value2, T value3)
	{
		long consumed = Consumed;
		do
		{
			int i;
			for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
			{
				T val = CurrentSpan[i];
				if (!val.Equals(value0) && !val.Equals(value1) && !val.Equals(value2) && !val.Equals(value3))
				{
					break;
				}
			}
			int num = i - CurrentSpanIndex;
			if (num == 0)
			{
				break;
			}
			AdvanceCurrentSpan(num);
		}
		while (CurrentSpanIndex == 0 && !End);
		return Consumed - consumed;
	}

	public long AdvancePastAny(T value0, T value1, T value2)
	{
		long consumed = Consumed;
		do
		{
			int i;
			for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
			{
				T val = CurrentSpan[i];
				if (!val.Equals(value0) && !val.Equals(value1) && !val.Equals(value2))
				{
					break;
				}
			}
			int num = i - CurrentSpanIndex;
			if (num == 0)
			{
				break;
			}
			AdvanceCurrentSpan(num);
		}
		while (CurrentSpanIndex == 0 && !End);
		return Consumed - consumed;
	}

	public long AdvancePastAny(T value0, T value1)
	{
		long consumed = Consumed;
		do
		{
			int i;
			for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
			{
				T val = CurrentSpan[i];
				if (!val.Equals(value0) && !val.Equals(value1))
				{
					break;
				}
			}
			int num = i - CurrentSpanIndex;
			if (num == 0)
			{
				break;
			}
			AdvanceCurrentSpan(num);
		}
		while (CurrentSpanIndex == 0 && !End);
		return Consumed - consumed;
	}

	public void AdvanceToEnd()
	{
		if (_moreData)
		{
			Consumed = Length;
			CurrentSpan = default(ReadOnlySpan<T>);
			CurrentSpanIndex = 0;
			_currentPosition = Sequence.End;
			_nextPosition = default(SequencePosition);
			_moreData = false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNext(T next, bool advancePast = false)
	{
		if (End)
		{
			return false;
		}
		if (CurrentSpan[CurrentSpanIndex].Equals(next))
		{
			if (advancePast)
			{
				AdvanceCurrentSpan(1L);
			}
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool IsNext(ReadOnlySpan<T> next, bool advancePast = false)
	{
		ReadOnlySpan<T> unreadSpan = UnreadSpan;
		if (unreadSpan.StartsWith(next))
		{
			if (advancePast)
			{
				AdvanceCurrentSpan(next.Length);
			}
			return true;
		}
		if (unreadSpan.Length < next.Length)
		{
			return IsNextSlow(next, advancePast);
		}
		return false;
	}

	private bool IsNextSlow(ReadOnlySpan<T> next, bool advancePast)
	{
		ReadOnlySpan<T> value = UnreadSpan;
		int length = next.Length;
		SequencePosition position = _nextPosition;
		while (next.StartsWith(value))
		{
			if (next.Length == value.Length)
			{
				if (advancePast)
				{
					Advance(length);
				}
				return true;
			}
			ReadOnlyMemory<T> memory;
			do
			{
				if (!Sequence.TryGet(ref position, out memory))
				{
					return false;
				}
			}
			while (memory.Length <= 0);
			next = next.Slice(value.Length);
			value = memory.Span;
			if (value.Length > next.Length)
			{
				value = value.Slice(0, next.Length);
			}
		}
		return false;
	}
}
