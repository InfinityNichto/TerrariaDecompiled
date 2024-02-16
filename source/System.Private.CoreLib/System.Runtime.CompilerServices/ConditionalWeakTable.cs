using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

public sealed class ConditionalWeakTable<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable where TKey : class where TValue : class?
{
	public delegate TValue CreateValueCallback(TKey key);

	private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator
	{
		private ConditionalWeakTable<TKey, TValue> _table;

		private readonly int _maxIndexInclusive;

		private int _currentIndex;

		private KeyValuePair<TKey, TValue> _current;

		public KeyValuePair<TKey, TValue> Current
		{
			get
			{
				if (_currentIndex < 0)
				{
					ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
				}
				return _current;
			}
		}

		object IEnumerator.Current => Current;

		public Enumerator(ConditionalWeakTable<TKey, TValue> table)
		{
			_table = table;
			table._activeEnumeratorRefCount++;
			_maxIndexInclusive = table._container.FirstFreeEntry - 1;
			_currentIndex = -1;
		}

		~Enumerator()
		{
			Dispose();
		}

		public void Dispose()
		{
			ConditionalWeakTable<TKey, TValue> conditionalWeakTable = Interlocked.Exchange(ref _table, null);
			if (conditionalWeakTable != null)
			{
				_current = default(KeyValuePair<TKey, TValue>);
				lock (conditionalWeakTable._lock)
				{
					conditionalWeakTable._activeEnumeratorRefCount--;
				}
				GC.SuppressFinalize(this);
			}
		}

		public bool MoveNext()
		{
			ConditionalWeakTable<TKey, TValue> table = _table;
			if (table != null)
			{
				lock (table._lock)
				{
					Container container = table._container;
					if (container != null)
					{
						while (_currentIndex < _maxIndexInclusive)
						{
							_currentIndex++;
							if (container.TryGetEntry(_currentIndex, out var key, out var value))
							{
								_current = new KeyValuePair<TKey, TValue>(key, value);
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		public void Reset()
		{
		}
	}

	private struct Entry
	{
		public DependentHandle depHnd;

		public int HashCode;

		public int Next;
	}

	private sealed class Container
	{
		private readonly ConditionalWeakTable<TKey, TValue> _parent;

		private int[] _buckets;

		private Entry[] _entries;

		private int _firstFreeEntry;

		private bool _invalid;

		private bool _finalized;

		private volatile object _oldKeepAlive;

		internal bool HasCapacity => _firstFreeEntry < _entries.Length;

		internal int FirstFreeEntry => _firstFreeEntry;

		internal Container(ConditionalWeakTable<TKey, TValue> parent)
		{
			_buckets = new int[8];
			for (int i = 0; i < _buckets.Length; i++)
			{
				_buckets[i] = -1;
			}
			_entries = new Entry[8];
			_parent = parent;
		}

		private Container(ConditionalWeakTable<TKey, TValue> parent, int[] buckets, Entry[] entries, int firstFreeEntry)
		{
			_parent = parent;
			_buckets = buckets;
			_entries = entries;
			_firstFreeEntry = firstFreeEntry;
		}

		internal void CreateEntryNoResize(TKey key, TValue value)
		{
			VerifyIntegrity();
			_invalid = true;
			int num = RuntimeHelpers.GetHashCode(key) & 0x7FFFFFFF;
			int num2 = _firstFreeEntry++;
			_entries[num2].HashCode = num;
			_entries[num2].depHnd = new DependentHandle(key, value);
			int num3 = num & (_buckets.Length - 1);
			_entries[num2].Next = _buckets[num3];
			Volatile.Write(ref _buckets[num3], num2);
			_invalid = false;
		}

		internal bool TryGetValueWorker(TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			object value2;
			int num = FindEntry(key, out value2);
			value = Unsafe.As<TValue>(value2);
			return num != -1;
		}

		internal int FindEntry(TKey key, out object value)
		{
			int num = RuntimeHelpers.GetHashCode(key) & 0x7FFFFFFF;
			int num2 = num & (_buckets.Length - 1);
			for (int num3 = Volatile.Read(ref _buckets[num2]); num3 != -1; num3 = _entries[num3].Next)
			{
				if (_entries[num3].HashCode == num && _entries[num3].depHnd.UnsafeGetTargetAndDependent(out value) == key)
				{
					GC.KeepAlive(this);
					return num3;
				}
			}
			GC.KeepAlive(this);
			value = null;
			return -1;
		}

		internal bool TryGetEntry(int index, [NotNullWhen(true)] out TKey key, [MaybeNullWhen(false)] out TValue value)
		{
			if (index < _entries.Length)
			{
				object dependent;
				object obj = _entries[index].depHnd.UnsafeGetTargetAndDependent(out dependent);
				GC.KeepAlive(this);
				if (obj != null)
				{
					key = Unsafe.As<TKey>(obj);
					value = Unsafe.As<TValue>(dependent);
					return true;
				}
			}
			key = null;
			value = null;
			return false;
		}

		internal void RemoveAllKeys()
		{
			for (int i = 0; i < _firstFreeEntry; i++)
			{
				RemoveIndex(i);
			}
		}

		internal bool Remove(TKey key)
		{
			VerifyIntegrity();
			object value;
			int num = FindEntry(key, out value);
			if (num != -1)
			{
				RemoveIndex(num);
				return true;
			}
			return false;
		}

		private void RemoveIndex(int entryIndex)
		{
			ref Entry reference = ref _entries[entryIndex];
			Volatile.Write(ref reference.HashCode, -1);
			reference.depHnd.UnsafeSetTargetToNull();
		}

		internal void UpdateValue(int entryIndex, TValue newValue)
		{
			VerifyIntegrity();
			_invalid = true;
			_entries[entryIndex].depHnd.UnsafeSetDependent(newValue);
			_invalid = false;
		}

		internal Container Resize()
		{
			bool flag = false;
			int newSize = _buckets.Length;
			if (_parent == null || _parent._activeEnumeratorRefCount == 0)
			{
				for (int i = 0; i < _entries.Length; i++)
				{
					ref Entry reference = ref _entries[i];
					if (reference.HashCode == -1)
					{
						flag = true;
						break;
					}
					if (reference.depHnd.IsAllocated && reference.depHnd.UnsafeGetTarget() == null)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				newSize = _buckets.Length * 2;
			}
			return Resize(newSize);
		}

		internal Container Resize(int newSize)
		{
			int[] array = new int[newSize];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = -1;
			}
			Entry[] array2 = new Entry[newSize];
			int j = 0;
			bool flag = _parent != null && _parent._activeEnumeratorRefCount > 0;
			if (flag)
			{
				for (; j < _entries.Length; j++)
				{
					ref Entry reference = ref _entries[j];
					ref Entry reference2 = ref array2[j];
					int num = (reference2.HashCode = reference.HashCode);
					reference2.depHnd = reference.depHnd;
					int num2 = num & (array.Length - 1);
					reference2.Next = array[num2];
					array[num2] = j;
				}
			}
			else
			{
				for (int k = 0; k < _entries.Length; k++)
				{
					ref Entry reference3 = ref _entries[k];
					int hashCode = reference3.HashCode;
					DependentHandle depHnd = reference3.depHnd;
					if (hashCode != -1 && depHnd.IsAllocated)
					{
						if (depHnd.UnsafeGetTarget() != null)
						{
							ref Entry reference4 = ref array2[j];
							reference4.HashCode = hashCode;
							reference4.depHnd = depHnd;
							int num3 = hashCode & (array.Length - 1);
							reference4.Next = array[num3];
							array[num3] = j;
							j++;
						}
						else
						{
							Volatile.Write(ref reference3.HashCode, -1);
						}
					}
				}
			}
			Container container = new Container(_parent, array, array2, j);
			if (flag)
			{
				GC.SuppressFinalize(this);
			}
			_oldKeepAlive = container;
			GC.KeepAlive(this);
			return container;
		}

		private void VerifyIntegrity()
		{
			if (_invalid)
			{
				throw new InvalidOperationException(SR.InvalidOperation_CollectionCorrupted);
			}
		}

		~Container()
		{
			if (_invalid || _parent == null)
			{
				return;
			}
			if (!_finalized)
			{
				_finalized = true;
				lock (_parent._lock)
				{
					if (_parent._container == this)
					{
						_parent._container = null;
					}
				}
				GC.ReRegisterForFinalize(this);
				return;
			}
			Entry[] entries = _entries;
			_invalid = true;
			_entries = null;
			_buckets = null;
			if (entries == null)
			{
				return;
			}
			int i = 0;
			for (; i < entries.Length; i++)
			{
				if (_oldKeepAlive == null || entries[i].HashCode == -1)
				{
					entries[i].depHnd.Dispose();
				}
			}
		}
	}

	private readonly object _lock;

	private volatile Container _container;

	private int _activeEnumeratorRefCount;

	public ConditionalWeakTable()
	{
		_lock = new object();
		_container = new Container(this);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		return _container.TryGetValueWorker(key, out value);
	}

	public void Add(TKey key, TValue value)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		lock (_lock)
		{
			object value2;
			int num = _container.FindEntry(key, out value2);
			if (num != -1)
			{
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
			}
			CreateEntry(key, value);
		}
	}

	public void AddOrUpdate(TKey key, TValue value)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		lock (_lock)
		{
			object value2;
			int num = _container.FindEntry(key, out value2);
			if (num != -1)
			{
				_container.UpdateValue(num, value);
			}
			else
			{
				CreateEntry(key, value);
			}
		}
	}

	public bool Remove(TKey key)
	{
		if (key == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
		}
		lock (_lock)
		{
			return _container.Remove(key);
		}
	}

	public void Clear()
	{
		lock (_lock)
		{
			if (_activeEnumeratorRefCount > 0)
			{
				_container.RemoveAllKeys();
			}
			else
			{
				_container = new Container(this);
			}
		}
	}

	public TValue GetValue(TKey key, CreateValueCallback createValueCallback)
	{
		if (createValueCallback == null)
		{
			throw new ArgumentNullException("createValueCallback");
		}
		if (!TryGetValue(key, out var value))
		{
			return GetValueLocked(key, createValueCallback);
		}
		return value;
	}

	private TValue GetValueLocked(TKey key, CreateValueCallback createValueCallback)
	{
		TValue val = createValueCallback(key);
		lock (_lock)
		{
			if (_container.TryGetValueWorker(key, out var value))
			{
				return value;
			}
			CreateEntry(key, val);
			return val;
		}
	}

	public TValue GetOrCreateValue(TKey key)
	{
		return GetValue(key, (TKey _) => Activator.CreateInstance<TValue>());
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		lock (_lock)
		{
			Container container = _container;
			IEnumerator<KeyValuePair<TKey, TValue>> result;
			if (container != null && container.FirstFreeEntry != 0)
			{
				IEnumerator<KeyValuePair<TKey, TValue>> enumerator = new Enumerator(this);
				result = enumerator;
			}
			else
			{
				result = ((IEnumerable<KeyValuePair<TKey, TValue>>)Array.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();
			}
			return result;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
	}

	private void CreateEntry(TKey key, TValue value)
	{
		Container container = _container;
		if (!container.HasCapacity)
		{
			container = (_container = container.Resize());
		}
		container.CreateEntryNoResize(key, value);
	}
}
