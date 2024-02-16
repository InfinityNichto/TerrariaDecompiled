using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Specialized;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class OrderedDictionary : IOrderedDictionary, IDictionary, ICollection, IEnumerable, ISerializable, IDeserializationCallback
{
	private sealed class OrderedDictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private readonly int _objectReturnType;

		private readonly IEnumerator _arrayEnumerator;

		public object Current
		{
			get
			{
				if (_objectReturnType == 1)
				{
					return ((DictionaryEntry)_arrayEnumerator.Current).Key;
				}
				if (_objectReturnType == 2)
				{
					return ((DictionaryEntry)_arrayEnumerator.Current).Value;
				}
				return Entry;
			}
		}

		public DictionaryEntry Entry => new DictionaryEntry(((DictionaryEntry)_arrayEnumerator.Current).Key, ((DictionaryEntry)_arrayEnumerator.Current).Value);

		public object Key => ((DictionaryEntry)_arrayEnumerator.Current).Key;

		public object Value => ((DictionaryEntry)_arrayEnumerator.Current).Value;

		internal OrderedDictionaryEnumerator(ArrayList array, int objectReturnType)
		{
			_arrayEnumerator = array.GetEnumerator();
			_objectReturnType = objectReturnType;
		}

		public bool MoveNext()
		{
			return _arrayEnumerator.MoveNext();
		}

		public void Reset()
		{
			_arrayEnumerator.Reset();
		}
	}

	private sealed class OrderedDictionaryKeyValueCollection : ICollection, IEnumerable
	{
		private readonly ArrayList _objects;

		private readonly bool _isKeys;

		int ICollection.Count => _objects.Count;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => _objects.SyncRoot;

		public OrderedDictionaryKeyValueCollection(ArrayList array, bool isKeys)
		{
			_objects = array;
			_isKeys = isKeys;
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum_Index);
			}
			foreach (object @object in _objects)
			{
				array.SetValue(_isKeys ? ((DictionaryEntry)@object).Key : ((DictionaryEntry)@object).Value, index);
				index++;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new OrderedDictionaryEnumerator(_objects, _isKeys ? 1 : 2);
		}
	}

	private ArrayList _objectsArray;

	private Hashtable _objectsTable;

	private int _initialCapacity;

	private IEqualityComparer _comparer;

	private bool _readOnly;

	private readonly SerializationInfo _siInfo;

	public int Count
	{
		get
		{
			if (_objectsArray == null)
			{
				return 0;
			}
			return _objectsArray.Count;
		}
	}

	bool IDictionary.IsFixedSize => _readOnly;

	public bool IsReadOnly => _readOnly;

	bool ICollection.IsSynchronized => false;

	public ICollection Keys
	{
		get
		{
			ArrayList array = EnsureObjectsArray();
			return new OrderedDictionaryKeyValueCollection(array, isKeys: true);
		}
	}

	object ICollection.SyncRoot => this;

	public object? this[int index]
	{
		get
		{
			ArrayList arrayList = EnsureObjectsArray();
			return ((DictionaryEntry)arrayList[index]).Value;
		}
		set
		{
			if (_readOnly)
			{
				throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
			}
			if (_objectsArray == null || index < 0 || index >= _objectsArray.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			ArrayList arrayList = EnsureObjectsArray();
			Hashtable hashtable = EnsureObjectsTable();
			object key = ((DictionaryEntry)arrayList[index]).Key;
			arrayList[index] = new DictionaryEntry(key, value);
			hashtable[key] = value;
		}
	}

	public object? this[object key]
	{
		get
		{
			if (_objectsTable == null)
			{
				return null;
			}
			return _objectsTable[key];
		}
		set
		{
			if (_readOnly)
			{
				throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
			}
			Hashtable hashtable = EnsureObjectsTable();
			if (hashtable.Contains(key))
			{
				hashtable[key] = value;
				ArrayList arrayList = EnsureObjectsArray();
				arrayList[IndexOfKey(key)] = new DictionaryEntry(key, value);
			}
			else
			{
				Add(key, value);
			}
		}
	}

	public ICollection Values
	{
		get
		{
			ArrayList array = EnsureObjectsArray();
			return new OrderedDictionaryKeyValueCollection(array, isKeys: false);
		}
	}

	public OrderedDictionary()
		: this(0)
	{
	}

	public OrderedDictionary(int capacity)
		: this(capacity, null)
	{
	}

	public OrderedDictionary(IEqualityComparer? comparer)
		: this(0, comparer)
	{
	}

	public OrderedDictionary(int capacity, IEqualityComparer? comparer)
	{
		_initialCapacity = capacity;
		_comparer = comparer;
	}

	private OrderedDictionary(OrderedDictionary dictionary)
	{
		_readOnly = true;
		_objectsArray = dictionary._objectsArray;
		_objectsTable = dictionary._objectsTable;
		_comparer = dictionary._comparer;
		_initialCapacity = dictionary._initialCapacity;
	}

	protected OrderedDictionary(SerializationInfo info, StreamingContext context)
	{
		_siInfo = info;
	}

	private ArrayList EnsureObjectsArray()
	{
		return _objectsArray ?? (_objectsArray = new ArrayList(_initialCapacity));
	}

	private Hashtable EnsureObjectsTable()
	{
		return _objectsTable ?? (_objectsTable = new Hashtable(_initialCapacity, _comparer));
	}

	public void Add(object key, object? value)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
		}
		Hashtable hashtable = EnsureObjectsTable();
		ArrayList arrayList = EnsureObjectsArray();
		hashtable.Add(key, value);
		arrayList.Add(new DictionaryEntry(key, value));
	}

	public void Clear()
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
		}
		if (_objectsTable != null)
		{
			_objectsTable.Clear();
		}
		if (_objectsArray != null)
		{
			_objectsArray.Clear();
		}
	}

	public OrderedDictionary AsReadOnly()
	{
		return new OrderedDictionary(this);
	}

	public bool Contains(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (_objectsTable == null)
		{
			return false;
		}
		return _objectsTable.Contains(key);
	}

	public void CopyTo(Array array, int index)
	{
		Hashtable hashtable = EnsureObjectsTable();
		hashtable.CopyTo(array, index);
	}

	private int IndexOfKey(object key)
	{
		if (_objectsArray == null)
		{
			return -1;
		}
		for (int i = 0; i < _objectsArray.Count; i++)
		{
			object key2 = ((DictionaryEntry)_objectsArray[i]).Key;
			if (_comparer != null)
			{
				if (_comparer.Equals(key2, key))
				{
					return i;
				}
			}
			else if (key2.Equals(key))
			{
				return i;
			}
		}
		return -1;
	}

	public void Insert(int index, object key, object? value)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
		}
		if (index > Count || index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		Hashtable hashtable = EnsureObjectsTable();
		ArrayList arrayList = EnsureObjectsArray();
		hashtable.Add(key, value);
		arrayList.Insert(index, new DictionaryEntry(key, value));
	}

	public void RemoveAt(int index)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
		}
		if (index >= Count || index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		Hashtable hashtable = EnsureObjectsTable();
		ArrayList arrayList = EnsureObjectsArray();
		object key = ((DictionaryEntry)arrayList[index]).Key;
		arrayList.RemoveAt(index);
		hashtable.Remove(key);
	}

	public void Remove(object key)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.OrderedDictionary_ReadOnly);
		}
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		int num = IndexOfKey(key);
		if (num >= 0)
		{
			Hashtable hashtable = EnsureObjectsTable();
			ArrayList arrayList = EnsureObjectsArray();
			hashtable.Remove(key);
			arrayList.RemoveAt(num);
		}
	}

	public virtual IDictionaryEnumerator GetEnumerator()
	{
		ArrayList array = EnsureObjectsArray();
		return new OrderedDictionaryEnumerator(array, 3);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		ArrayList array = EnsureObjectsArray();
		return new OrderedDictionaryEnumerator(array, 3);
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("KeyComparer", _comparer, typeof(IEqualityComparer));
		info.AddValue("ReadOnly", _readOnly);
		info.AddValue("InitialCapacity", _initialCapacity);
		object[] array = new object[Count];
		ArrayList arrayList = EnsureObjectsArray();
		arrayList.CopyTo(array);
		info.AddValue("ArrayList", array);
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		OnDeserialization(sender);
	}

	protected virtual void OnDeserialization(object? sender)
	{
		if (_siInfo == null)
		{
			throw new SerializationException(System.SR.Serialization_InvalidOnDeser);
		}
		_comparer = (IEqualityComparer)_siInfo.GetValue("KeyComparer", typeof(IEqualityComparer));
		_readOnly = _siInfo.GetBoolean("ReadOnly");
		_initialCapacity = _siInfo.GetInt32("InitialCapacity");
		object[] array = (object[])_siInfo.GetValue("ArrayList", typeof(object[]));
		if (array == null)
		{
			return;
		}
		Hashtable hashtable = EnsureObjectsTable();
		ArrayList arrayList = EnsureObjectsArray();
		object[] array2 = array;
		foreach (object obj in array2)
		{
			DictionaryEntry dictionaryEntry;
			try
			{
				dictionaryEntry = (DictionaryEntry)obj;
			}
			catch
			{
				throw new SerializationException(System.SR.OrderedDictionary_SerializationMismatch);
			}
			arrayList.Add(dictionaryEntry);
			hashtable.Add(dictionaryEntry.Key, dictionaryEntry.Value);
		}
	}
}
