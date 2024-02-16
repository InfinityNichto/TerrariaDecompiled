using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal;

namespace System.Threading.Tasks;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(SingleProducerSingleConsumerQueue<>.SingleProducerSingleConsumerQueue_DebugView))]
internal sealed class SingleProducerSingleConsumerQueue<T> : IProducerConsumerQueue<T>, IEnumerable<T>, IEnumerable
{
	[StructLayout(LayoutKind.Sequential)]
	private sealed class Segment
	{
		internal Segment m_next;

		internal readonly T[] m_array;

		internal SegmentState m_state;

		internal Segment(int size)
		{
			m_array = new T[size];
		}
	}

	private struct SegmentState
	{
		internal PaddingFor32 m_pad0;

		internal volatile int m_first;

		internal int m_lastCopy;

		internal PaddingFor32 m_pad1;

		internal int m_firstCopy;

		internal volatile int m_last;

		internal PaddingFor32 m_pad2;
	}

	private sealed class SingleProducerSingleConsumerQueue_DebugView
	{
		private readonly SingleProducerSingleConsumerQueue<T> m_queue;

		public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
		{
			m_queue = queue;
		}
	}

	private volatile Segment m_head;

	private volatile Segment m_tail;

	public bool IsEmpty
	{
		get
		{
			Segment head = m_head;
			if (head.m_state.m_first != head.m_state.m_lastCopy)
			{
				return false;
			}
			if (head.m_state.m_first != head.m_state.m_last)
			{
				return false;
			}
			return head.m_next == null;
		}
	}

	public int Count
	{
		get
		{
			int num = 0;
			for (Segment segment = m_head; segment != null; segment = segment.m_next)
			{
				int num2 = segment.m_array.Length;
				int first;
				int last;
				do
				{
					first = segment.m_state.m_first;
					last = segment.m_state.m_last;
				}
				while (first != segment.m_state.m_first);
				num += (last - first) & (num2 - 1);
			}
			return num;
		}
	}

	internal SingleProducerSingleConsumerQueue()
	{
		m_head = (m_tail = new Segment(32));
	}

	public void Enqueue(T item)
	{
		Segment segment = m_tail;
		T[] array = segment.m_array;
		int last = segment.m_state.m_last;
		int num = (last + 1) & (array.Length - 1);
		if (num != segment.m_state.m_firstCopy)
		{
			array[last] = item;
			segment.m_state.m_last = num;
		}
		else
		{
			EnqueueSlow(item, ref segment);
		}
	}

	private void EnqueueSlow(T item, ref Segment segment)
	{
		if (segment.m_state.m_firstCopy != segment.m_state.m_first)
		{
			segment.m_state.m_firstCopy = segment.m_state.m_first;
			Enqueue(item);
			return;
		}
		int num = m_tail.m_array.Length << 1;
		if (num > 16777216)
		{
			num = 16777216;
		}
		Segment segment2 = new Segment(num);
		segment2.m_array[0] = item;
		segment2.m_state.m_last = 1;
		segment2.m_state.m_lastCopy = 1;
		Volatile.Write(ref m_tail.m_next, segment2);
		m_tail = segment2;
	}

	public bool TryDequeue([MaybeNullWhen(false)] out T result)
	{
		Segment segment = m_head;
		T[] array = segment.m_array;
		int first = segment.m_state.m_first;
		if (first != segment.m_state.m_lastCopy)
		{
			result = array[first];
			array[first] = default(T);
			segment.m_state.m_first = (first + 1) & (array.Length - 1);
			return true;
		}
		return TryDequeueSlow(ref segment, ref array, out result);
	}

	private bool TryDequeueSlow(ref Segment segment, ref T[] array, [MaybeNullWhen(false)] out T result)
	{
		if (segment.m_state.m_last != segment.m_state.m_lastCopy)
		{
			segment.m_state.m_lastCopy = segment.m_state.m_last;
			return TryDequeue(out result);
		}
		if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
		{
			segment = segment.m_next;
			array = segment.m_array;
			m_head = segment;
		}
		int first = segment.m_state.m_first;
		if (first == segment.m_state.m_last)
		{
			result = default(T);
			return false;
		}
		result = array[first];
		array[first] = default(T);
		segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
		segment.m_state.m_lastCopy = segment.m_state.m_last;
		return true;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (Segment segment = m_head; segment != null; segment = segment.m_next)
		{
			for (int pt = segment.m_state.m_first; pt != segment.m_state.m_last; pt = (pt + 1) & (segment.m_array.Length - 1))
			{
				yield return segment.m_array[pt];
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
