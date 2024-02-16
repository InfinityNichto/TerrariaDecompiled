using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Concurrent;

[DebuggerDisplay("Capacity = {Capacity}")]
internal sealed class ConcurrentQueueSegment<T>
{
	[StructLayout(LayoutKind.Auto)]
	[DebuggerDisplay("Item = {Item}, SequenceNumber = {SequenceNumber}")]
	internal struct Slot
	{
		public T Item;

		public int SequenceNumber;
	}

	internal readonly Slot[] _slots;

	internal readonly int _slotsMask;

	internal PaddedHeadAndTail _headAndTail;

	internal bool _preservedForObservation;

	internal bool _frozenForEnqueues;

	internal ConcurrentQueueSegment<T> _nextSegment;

	internal int Capacity => _slots.Length;

	internal int FreezeOffset => _slots.Length * 2;

	internal ConcurrentQueueSegment(int boundedLength)
	{
		_slots = new Slot[boundedLength];
		_slotsMask = boundedLength - 1;
		for (int i = 0; i < _slots.Length; i++)
		{
			_slots[i].SequenceNumber = i;
		}
	}

	internal void EnsureFrozenForEnqueues()
	{
		if (!_frozenForEnqueues)
		{
			_frozenForEnqueues = true;
			Interlocked.Add(ref _headAndTail.Tail, FreezeOffset);
		}
	}

	public bool TryDequeue([MaybeNullWhen(false)] out T item)
	{
		Slot[] slots = _slots;
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int num = Volatile.Read(ref _headAndTail.Head);
			int num2 = num & _slotsMask;
			int num3 = Volatile.Read(ref slots[num2].SequenceNumber);
			int num4 = num3 - (num + 1);
			if (num4 == 0)
			{
				if (Interlocked.CompareExchange(ref _headAndTail.Head, num + 1, num) != num)
				{
					continue;
				}
				item = slots[num2].Item;
				if (!Volatile.Read(ref _preservedForObservation))
				{
					if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
					{
						slots[num2].Item = default(T);
					}
					Volatile.Write(ref slots[num2].SequenceNumber, num + slots.Length);
				}
				return true;
			}
			if (num4 < 0)
			{
				bool frozenForEnqueues = _frozenForEnqueues;
				int num5 = Volatile.Read(ref _headAndTail.Tail);
				if (num5 - num <= 0 || (frozenForEnqueues && num5 - FreezeOffset - num <= 0))
				{
					break;
				}
				spinWait.SpinOnce(-1);
			}
		}
		item = default(T);
		return false;
	}

	public bool TryPeek([MaybeNullWhen(false)] out T result, bool resultUsed)
	{
		if (resultUsed)
		{
			_preservedForObservation = true;
			Interlocked.MemoryBarrier();
		}
		Slot[] slots = _slots;
		SpinWait spinWait = default(SpinWait);
		while (true)
		{
			int num = Volatile.Read(ref _headAndTail.Head);
			int num2 = num & _slotsMask;
			int num3 = Volatile.Read(ref slots[num2].SequenceNumber);
			int num4 = num3 - (num + 1);
			if (num4 == 0)
			{
				result = (resultUsed ? slots[num2].Item : default(T));
				return true;
			}
			if (num4 < 0)
			{
				bool frozenForEnqueues = _frozenForEnqueues;
				int num5 = Volatile.Read(ref _headAndTail.Tail);
				if (num5 - num <= 0 || (frozenForEnqueues && num5 - FreezeOffset - num <= 0))
				{
					break;
				}
				spinWait.SpinOnce(-1);
			}
		}
		result = default(T);
		return false;
	}

	public bool TryEnqueue(T item)
	{
		Slot[] slots = _slots;
		while (true)
		{
			int num = Volatile.Read(ref _headAndTail.Tail);
			int num2 = num & _slotsMask;
			int num3 = Volatile.Read(ref slots[num2].SequenceNumber);
			int num4 = num3 - num;
			if (num4 == 0)
			{
				if (Interlocked.CompareExchange(ref _headAndTail.Tail, num + 1, num) == num)
				{
					slots[num2].Item = item;
					Volatile.Write(ref slots[num2].SequenceNumber, num + 1);
					return true;
				}
			}
			else if (num4 < 0)
			{
				break;
			}
		}
		return false;
	}
}
