using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections;

[Serializable]
[DebuggerTypeProxy(typeof(QueueDebugView))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class Queue : ICollection, IEnumerable, ICloneable
{
	private sealed class SynchronizedQueue : Queue
	{
		private readonly Queue _q;

		private readonly object _root;

		public override bool IsSynchronized => true;

		public override object SyncRoot => _root;

		public override int Count
		{
			get
			{
				lock (_root)
				{
					return _q.Count;
				}
			}
		}

		internal SynchronizedQueue(Queue q)
		{
			_q = q;
			_root = _q.SyncRoot;
		}

		public override void Clear()
		{
			lock (_root)
			{
				_q.Clear();
			}
		}

		public override object Clone()
		{
			lock (_root)
			{
				return new SynchronizedQueue((Queue)_q.Clone());
			}
		}

		public override bool Contains(object obj)
		{
			lock (_root)
			{
				return _q.Contains(obj);
			}
		}

		public override void CopyTo(Array array, int arrayIndex)
		{
			lock (_root)
			{
				_q.CopyTo(array, arrayIndex);
			}
		}

		public override void Enqueue(object value)
		{
			lock (_root)
			{
				_q.Enqueue(value);
			}
		}

		public override object Dequeue()
		{
			lock (_root)
			{
				return _q.Dequeue();
			}
		}

		public override IEnumerator GetEnumerator()
		{
			lock (_root)
			{
				return _q.GetEnumerator();
			}
		}

		public override object Peek()
		{
			lock (_root)
			{
				return _q.Peek();
			}
		}

		public override object[] ToArray()
		{
			lock (_root)
			{
				return _q.ToArray();
			}
		}

		public override void TrimToSize()
		{
			lock (_root)
			{
				_q.TrimToSize();
			}
		}
	}

	private sealed class QueueEnumerator : IEnumerator, ICloneable
	{
		private readonly Queue _q;

		private int _index;

		private readonly int _version;

		private object _currentElement;

		public object Current
		{
			get
			{
				if (_currentElement == _q._array)
				{
					if (_index == 0)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_EnumNotStarted);
					}
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumEnded);
				}
				return _currentElement;
			}
		}

		internal QueueEnumerator(Queue q)
		{
			_q = q;
			_version = _q._version;
			_index = 0;
			_currentElement = _q._array;
			if (_q._size == 0)
			{
				_index = -1;
			}
		}

		public object Clone()
		{
			return MemberwiseClone();
		}

		public bool MoveNext()
		{
			if (_version != _q._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if (_index < 0)
			{
				_currentElement = _q._array;
				return false;
			}
			_currentElement = _q.GetElement(_index);
			_index++;
			if (_index == _q._size)
			{
				_index = -1;
			}
			return true;
		}

		public void Reset()
		{
			if (_version != _q._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if (_q._size == 0)
			{
				_index = -1;
			}
			else
			{
				_index = 0;
			}
			_currentElement = _q._array;
		}
	}

	internal sealed class QueueDebugView
	{
		private readonly Queue _queue;

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public object[] Items => _queue.ToArray();

		public QueueDebugView(Queue queue)
		{
			if (queue == null)
			{
				throw new ArgumentNullException("queue");
			}
			_queue = queue;
		}
	}

	private object[] _array;

	private int _head;

	private int _tail;

	private int _size;

	private readonly int _growFactor;

	private int _version;

	public virtual int Count => _size;

	public virtual bool IsSynchronized => false;

	public virtual object SyncRoot => this;

	public Queue()
		: this(32, 2f)
	{
	}

	public Queue(int capacity)
		: this(capacity, 2f)
	{
	}

	public Queue(int capacity, float growFactor)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException("capacity", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (!((double)growFactor >= 1.0) || !((double)growFactor <= 10.0))
		{
			throw new ArgumentOutOfRangeException("growFactor", System.SR.Format(System.SR.ArgumentOutOfRange_QueueGrowFactor, 1, 10));
		}
		_array = new object[capacity];
		_head = 0;
		_tail = 0;
		_size = 0;
		_growFactor = (int)(growFactor * 100f);
	}

	public Queue(ICollection col)
		: this(col?.Count ?? 32)
	{
		if (col == null)
		{
			throw new ArgumentNullException("col");
		}
		IEnumerator enumerator = col.GetEnumerator();
		while (enumerator.MoveNext())
		{
			Enqueue(enumerator.Current);
		}
	}

	public virtual object Clone()
	{
		Queue queue = new Queue(_size);
		queue._size = _size;
		int size = _size;
		int num = ((_array.Length - _head < size) ? (_array.Length - _head) : size);
		Array.Copy(_array, _head, queue._array, 0, num);
		size -= num;
		if (size > 0)
		{
			Array.Copy(_array, 0, queue._array, _array.Length - _head, size);
		}
		queue._version = _version;
		return queue;
	}

	public virtual void Clear()
	{
		if (_size != 0)
		{
			if (_head < _tail)
			{
				Array.Clear(_array, _head, _size);
			}
			else
			{
				Array.Clear(_array, _head, _array.Length - _head);
				Array.Clear(_array, 0, _tail);
			}
			_size = 0;
		}
		_head = 0;
		_tail = 0;
		_version++;
	}

	public virtual void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", System.SR.ArgumentOutOfRange_Index);
		}
		int length = array.Length;
		if (length - index < _size)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
		int size = _size;
		if (size != 0)
		{
			int num = ((_array.Length - _head < size) ? (_array.Length - _head) : size);
			Array.Copy(_array, _head, array, index, num);
			size -= num;
			if (size > 0)
			{
				Array.Copy(_array, 0, array, index + _array.Length - _head, size);
			}
		}
	}

	public virtual void Enqueue(object? obj)
	{
		if (_size == _array.Length)
		{
			int num = (int)((long)_array.Length * (long)_growFactor / 100);
			if (num < _array.Length + 4)
			{
				num = _array.Length + 4;
			}
			SetCapacity(num);
		}
		_array[_tail] = obj;
		_tail = (_tail + 1) % _array.Length;
		_size++;
		_version++;
	}

	public virtual IEnumerator GetEnumerator()
	{
		return new QueueEnumerator(this);
	}

	public virtual object? Dequeue()
	{
		if (Count == 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_EmptyQueue);
		}
		object result = _array[_head];
		_array[_head] = null;
		_head = (_head + 1) % _array.Length;
		_size--;
		_version++;
		return result;
	}

	public virtual object? Peek()
	{
		if (Count == 0)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_EmptyQueue);
		}
		return _array[_head];
	}

	public static Queue Synchronized(Queue queue)
	{
		if (queue == null)
		{
			throw new ArgumentNullException("queue");
		}
		return new SynchronizedQueue(queue);
	}

	public virtual bool Contains(object? obj)
	{
		int num = _head;
		int size = _size;
		while (size-- > 0)
		{
			if (obj == null)
			{
				if (_array[num] == null)
				{
					return true;
				}
			}
			else if (_array[num] != null && _array[num].Equals(obj))
			{
				return true;
			}
			num = (num + 1) % _array.Length;
		}
		return false;
	}

	internal object GetElement(int i)
	{
		return _array[(_head + i) % _array.Length];
	}

	public virtual object?[] ToArray()
	{
		if (_size == 0)
		{
			return Array.Empty<object>();
		}
		object[] array = new object[_size];
		if (_head < _tail)
		{
			Array.Copy(_array, _head, array, 0, _size);
		}
		else
		{
			Array.Copy(_array, _head, array, 0, _array.Length - _head);
			Array.Copy(_array, 0, array, _array.Length - _head, _tail);
		}
		return array;
	}

	private void SetCapacity(int capacity)
	{
		object[] array = new object[capacity];
		if (_size > 0)
		{
			if (_head < _tail)
			{
				Array.Copy(_array, _head, array, 0, _size);
			}
			else
			{
				Array.Copy(_array, _head, array, 0, _array.Length - _head);
				Array.Copy(_array, 0, array, _array.Length - _head, _tail);
			}
		}
		_array = array;
		_head = 0;
		_tail = ((_size != capacity) ? _size : 0);
		_version++;
	}

	public virtual void TrimToSize()
	{
		SetCapacity(_size);
	}
}
