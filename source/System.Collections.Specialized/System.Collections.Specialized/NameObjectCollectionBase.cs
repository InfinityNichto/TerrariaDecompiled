using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Collections.Specialized;

public abstract class NameObjectCollectionBase : ICollection, IEnumerable, ISerializable, IDeserializationCallback
{
	internal sealed class NameObjectEntry
	{
		internal string Key;

		internal object Value;

		internal NameObjectEntry(string name, object value)
		{
			Key = name;
			Value = value;
		}
	}

	internal sealed class NameObjectKeysEnumerator : IEnumerator
	{
		private int _pos;

		private readonly NameObjectCollectionBase _coll;

		private readonly int _version;

		public object Current
		{
			get
			{
				if (_pos >= 0 && _pos < _coll.Count)
				{
					return _coll.BaseGetKey(_pos);
				}
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
			}
		}

		internal NameObjectKeysEnumerator(NameObjectCollectionBase coll)
		{
			_coll = coll;
			_version = _coll._version;
			_pos = -1;
		}

		public bool MoveNext()
		{
			if (_version != _coll._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			if (_pos < _coll.Count - 1)
			{
				_pos++;
				return true;
			}
			_pos = _coll.Count;
			return false;
		}

		public void Reset()
		{
			if (_version != _coll._version)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_EnumFailedVersion);
			}
			_pos = -1;
		}
	}

	public class KeysCollection : ICollection, IEnumerable
	{
		private readonly NameObjectCollectionBase _coll;

		public string? this[int index] => Get(index);

		public int Count => _coll.Count;

		object ICollection.SyncRoot => ((ICollection)_coll).SyncRoot;

		bool ICollection.IsSynchronized => false;

		internal KeysCollection(NameObjectCollectionBase coll)
		{
			_coll = coll;
		}

		public virtual string? Get(int index)
		{
			return _coll.BaseGetKey(index);
		}

		public IEnumerator GetEnumerator()
		{
			return new NameObjectKeysEnumerator(_coll);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_MultiRank, "array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum_Index);
			}
			if (array.Length - index < _coll.Count)
			{
				throw new ArgumentException(System.SR.Arg_InsufficientSpace);
			}
			IEnumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				array.SetValue(enumerator.Current, index++);
			}
		}
	}

	private bool _readOnly;

	private ArrayList _entriesArray;

	private IEqualityComparer _keyComparer;

	private volatile Hashtable _entriesTable;

	private volatile NameObjectEntry _nullKeyEntry;

	private KeysCollection _keys;

	private int _version;

	private static readonly StringComparer s_defaultComparer = CultureInfo.InvariantCulture.CompareInfo.GetStringComparer(CompareOptions.IgnoreCase);

	internal IEqualityComparer Comparer
	{
		get
		{
			return _keyComparer;
		}
		set
		{
			_keyComparer = value;
		}
	}

	protected bool IsReadOnly
	{
		get
		{
			return _readOnly;
		}
		set
		{
			_readOnly = value;
		}
	}

	public virtual int Count => _entriesArray.Count;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => false;

	public virtual KeysCollection Keys
	{
		get
		{
			if (_keys == null)
			{
				_keys = new KeysCollection(this);
			}
			return _keys;
		}
	}

	protected NameObjectCollectionBase()
		: this(s_defaultComparer)
	{
	}

	protected NameObjectCollectionBase(IEqualityComparer? equalityComparer)
	{
		IEqualityComparer keyComparer;
		if (equalityComparer != null)
		{
			keyComparer = equalityComparer;
		}
		else
		{
			IEqualityComparer equalityComparer2 = s_defaultComparer;
			keyComparer = equalityComparer2;
		}
		_keyComparer = keyComparer;
		Reset();
	}

	protected NameObjectCollectionBase(int capacity, IEqualityComparer? equalityComparer)
		: this(equalityComparer)
	{
		Reset(capacity);
	}

	[Obsolete("This constructor has been deprecated. Use NameObjectCollectionBase(IEqualityComparer) instead.")]
	protected NameObjectCollectionBase(IHashCodeProvider? hashProvider, IComparer? comparer)
	{
		_keyComparer = new System.Collections.CompatibleComparer(hashProvider, comparer);
		Reset();
	}

	[Obsolete("This constructor has been deprecated. Use NameObjectCollectionBase(Int32, IEqualityComparer) instead.")]
	protected NameObjectCollectionBase(int capacity, IHashCodeProvider? hashProvider, IComparer? comparer)
	{
		_keyComparer = new System.Collections.CompatibleComparer(hashProvider, comparer);
		Reset(capacity);
	}

	protected NameObjectCollectionBase(int capacity)
	{
		_keyComparer = s_defaultComparer;
		Reset(capacity);
	}

	protected NameObjectCollectionBase(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public virtual void OnDeserialization(object? sender)
	{
		throw new PlatformNotSupportedException();
	}

	[MemberNotNull("_entriesArray")]
	[MemberNotNull("_entriesTable")]
	private void Reset()
	{
		_entriesArray = new ArrayList();
		_entriesTable = new Hashtable(_keyComparer);
		_nullKeyEntry = null;
		_version++;
	}

	[MemberNotNull("_entriesArray")]
	[MemberNotNull("_entriesTable")]
	private void Reset(int capacity)
	{
		_entriesArray = new ArrayList(capacity);
		_entriesTable = new Hashtable(capacity, _keyComparer);
		_nullKeyEntry = null;
		_version++;
	}

	private NameObjectEntry FindEntry(string key)
	{
		if (key != null)
		{
			return (NameObjectEntry)_entriesTable[key];
		}
		return _nullKeyEntry;
	}

	protected bool BaseHasKeys()
	{
		return _entriesTable.Count > 0;
	}

	protected void BaseAdd(string? name, object? value)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		NameObjectEntry nameObjectEntry = new NameObjectEntry(name, value);
		if (name != null)
		{
			if (_entriesTable[name] == null)
			{
				_entriesTable.Add(name, nameObjectEntry);
			}
		}
		else if (_nullKeyEntry == null)
		{
			_nullKeyEntry = nameObjectEntry;
		}
		_entriesArray.Add(nameObjectEntry);
		_version++;
	}

	protected void BaseRemove(string? name)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		if (name != null)
		{
			_entriesTable.Remove(name);
			for (int num = _entriesArray.Count - 1; num >= 0; num--)
			{
				if (_keyComparer.Equals(name, BaseGetKey(num)))
				{
					_entriesArray.RemoveAt(num);
				}
			}
		}
		else
		{
			_nullKeyEntry = null;
			for (int num2 = _entriesArray.Count - 1; num2 >= 0; num2--)
			{
				if (BaseGetKey(num2) == null)
				{
					_entriesArray.RemoveAt(num2);
				}
			}
		}
		_version++;
	}

	protected void BaseRemoveAt(int index)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		string text = BaseGetKey(index);
		if (text != null)
		{
			_entriesTable.Remove(text);
		}
		else
		{
			_nullKeyEntry = null;
		}
		_entriesArray.RemoveAt(index);
		_version++;
	}

	protected void BaseClear()
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		Reset();
	}

	protected object? BaseGet(string? name)
	{
		return FindEntry(name)?.Value;
	}

	protected void BaseSet(string? name, object? value)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		NameObjectEntry nameObjectEntry = FindEntry(name);
		if (nameObjectEntry != null)
		{
			nameObjectEntry.Value = value;
			_version++;
		}
		else
		{
			BaseAdd(name, value);
		}
	}

	protected object? BaseGet(int index)
	{
		NameObjectEntry nameObjectEntry = (NameObjectEntry)_entriesArray[index];
		return nameObjectEntry.Value;
	}

	protected string? BaseGetKey(int index)
	{
		NameObjectEntry nameObjectEntry = (NameObjectEntry)_entriesArray[index];
		return nameObjectEntry.Key;
	}

	protected void BaseSet(int index, object? value)
	{
		if (_readOnly)
		{
			throw new NotSupportedException(System.SR.CollectionReadOnly);
		}
		NameObjectEntry nameObjectEntry = (NameObjectEntry)_entriesArray[index];
		nameObjectEntry.Value = value;
		_version++;
	}

	public virtual IEnumerator GetEnumerator()
	{
		return new NameObjectKeysEnumerator(this);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (array.Rank != 1)
		{
			throw new ArgumentException(System.SR.Arg_MultiRank, "array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum_Index);
		}
		if (array.Length - index < _entriesArray.Count)
		{
			throw new ArgumentException(System.SR.Arg_InsufficientSpace);
		}
		IEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			array.SetValue(enumerator.Current, index++);
		}
	}

	protected string?[] BaseGetAllKeys()
	{
		int count = _entriesArray.Count;
		string[] array = new string[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = BaseGetKey(i);
		}
		return array;
	}

	protected object?[] BaseGetAllValues()
	{
		int count = _entriesArray.Count;
		object[] array = new object[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = BaseGet(i);
		}
		return array;
	}

	protected object?[] BaseGetAllValues(Type type)
	{
		int count = _entriesArray.Count;
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		object[] array = (object[])Array.CreateInstance(type, count);
		for (int i = 0; i < count; i++)
		{
			array[i] = BaseGet(i);
		}
		return array;
	}
}
