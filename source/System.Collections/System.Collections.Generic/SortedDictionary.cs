using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[DebuggerTypeProxy(typeof(System.Collections.Generic.IDictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SortedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
	{
		private SortedSet<KeyValuePair<TKey, TValue>>.Enumerator _treeEnum;

		private readonly int _getEnumeratorRetType;

		public KeyValuePair<TKey, TValue> Current => _treeEnum.Current;

		internal bool NotStartedOrEnded => _treeEnum.NotStartedOrEnded;

		object? IEnumerator.Current
		{
			get
			{
				if (NotStartedOrEnded)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				if (_getEnumeratorRetType == 2)
				{
					return new DictionaryEntry(Current.Key, Current.Value);
				}
				return new KeyValuePair<TKey, TValue>(Current.Key, Current.Value);
			}
		}

		object IDictionaryEnumerator.Key
		{
			get
			{
				if (NotStartedOrEnded)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return Current.Key;
			}
		}

		object? IDictionaryEnumerator.Value
		{
			get
			{
				if (NotStartedOrEnded)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return Current.Value;
			}
		}

		DictionaryEntry IDictionaryEnumerator.Entry
		{
			get
			{
				if (NotStartedOrEnded)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
				}
				return new DictionaryEntry(Current.Key, Current.Value);
			}
		}

		internal Enumerator(SortedDictionary<TKey, TValue> dictionary, int getEnumeratorRetType)
		{
			_treeEnum = dictionary._set.GetEnumerator();
			_getEnumeratorRetType = getEnumeratorRetType;
		}

		public bool MoveNext()
		{
			return _treeEnum.MoveNext();
		}

		public void Dispose()
		{
			_treeEnum.Dispose();
		}

		internal void Reset()
		{
			_treeEnum.Reset();
		}

		void IEnumerator.Reset()
		{
			_treeEnum.Reset();
		}
	}

	[DebuggerTypeProxy(typeof(System.Collections.Generic.DictionaryKeyCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
	{
		public struct Enumerator : IEnumerator<TKey>, IDisposable, IEnumerator
		{
			private SortedDictionary<TKey, TValue>.Enumerator _dictEnum;

			public TKey Current => _dictEnum.Current.Key;

			object? IEnumerator.Current
			{
				get
				{
					if (_dictEnum.NotStartedOrEnded)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
					}
					return Current;
				}
			}

			internal Enumerator(SortedDictionary<TKey, TValue> dictionary)
			{
				_dictEnum = dictionary.GetEnumerator();
			}

			public void Dispose()
			{
				_dictEnum.Dispose();
			}

			public bool MoveNext()
			{
				return _dictEnum.MoveNext();
			}

			void IEnumerator.Reset()
			{
				_dictEnum.Reset();
			}
		}

		private readonly SortedDictionary<TKey, TValue> _dictionary;

		public int Count => _dictionary.Count;

		bool ICollection<TKey>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

		public KeyCollection(SortedDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			_dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		public void CopyTo(TKey[] array, int index)
		{
			TKey[] array2 = array;
			if (array2 == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array2.Length - index < Count)
			{
				throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
			}
			_dictionary._set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
			{
				array2[index++] = node.Item.Key;
				return true;
			});
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			if (array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < _dictionary.Count)
			{
				throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
			}
			if (array is TKey[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			try
			{
				object[] objects = (object[])array;
				_dictionary._set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
				{
					objects[index++] = node.Item.Key;
					return true;
				});
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
			}
		}

		void ICollection<TKey>.Add(TKey item)
		{
			throw new NotSupportedException(System.SR.NotSupported_KeyCollectionSet);
		}

		void ICollection<TKey>.Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_KeyCollectionSet);
		}

		bool ICollection<TKey>.Contains(TKey item)
		{
			return _dictionary.ContainsKey(item);
		}

		bool ICollection<TKey>.Remove(TKey item)
		{
			throw new NotSupportedException(System.SR.NotSupported_KeyCollectionSet);
		}
	}

	[DebuggerTypeProxy(typeof(System.Collections.Generic.DictionaryValueCollectionDebugView<, >))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
	{
		public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
		{
			private SortedDictionary<TKey, TValue>.Enumerator _dictEnum;

			public TValue Current => _dictEnum.Current.Value;

			object? IEnumerator.Current
			{
				get
				{
					if (_dictEnum.NotStartedOrEnded)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_EnumOpCantHappen);
					}
					return Current;
				}
			}

			internal Enumerator(SortedDictionary<TKey, TValue> dictionary)
			{
				_dictEnum = dictionary.GetEnumerator();
			}

			public void Dispose()
			{
				_dictEnum.Dispose();
			}

			public bool MoveNext()
			{
				return _dictEnum.MoveNext();
			}

			void IEnumerator.Reset()
			{
				_dictEnum.Reset();
			}
		}

		private readonly SortedDictionary<TKey, TValue> _dictionary;

		public int Count => _dictionary.Count;

		bool ICollection<TValue>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

		public ValueCollection(SortedDictionary<TKey, TValue> dictionary)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException("dictionary");
			}
			_dictionary = dictionary;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(_dictionary);
		}

		public void CopyTo(TValue[] array, int index)
		{
			TValue[] array2 = array;
			if (array2 == null)
			{
				throw new ArgumentNullException("array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array2.Length - index < Count)
			{
				throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
			}
			_dictionary._set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
			{
				array2[index++] = node.Item.Value;
				return true;
			});
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (array.Rank != 1)
			{
				throw new ArgumentException(System.SR.Arg_RankMultiDimNotSupported, "array");
			}
			if (array.GetLowerBound(0) != 0)
			{
				throw new ArgumentException(System.SR.Arg_NonZeroLowerBound, "array");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index", index, System.SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			if (array.Length - index < _dictionary.Count)
			{
				throw new ArgumentException(System.SR.Arg_ArrayPlusOffTooSmall);
			}
			if (array is TValue[] array2)
			{
				CopyTo(array2, index);
				return;
			}
			try
			{
				object[] objects = (object[])array;
				_dictionary._set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
				{
					objects[index++] = node.Item.Value;
					return true;
				});
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
			}
		}

		void ICollection<TValue>.Add(TValue item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ValueCollectionSet);
		}

		void ICollection<TValue>.Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_ValueCollectionSet);
		}

		bool ICollection<TValue>.Contains(TValue item)
		{
			return _dictionary.ContainsValue(item);
		}

		bool ICollection<TValue>.Remove(TValue item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ValueCollectionSet);
		}
	}

	[Serializable]
	public sealed class KeyValuePairComparer : Comparer<KeyValuePair<TKey, TValue>>
	{
		internal IComparer<TKey> keyComparer;

		public KeyValuePairComparer(IComparer<TKey>? keyComparer)
		{
			if (keyComparer == null)
			{
				this.keyComparer = Comparer<TKey>.Default;
			}
			else
			{
				this.keyComparer = keyComparer;
			}
		}

		public override int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
		{
			return keyComparer.Compare(x.Key, y.Key);
		}

		public override bool Equals(object? obj)
		{
			if (obj is KeyValuePairComparer keyValuePairComparer)
			{
				if (keyComparer != keyValuePairComparer.keyComparer)
				{
					return keyComparer.Equals(keyValuePairComparer.keyComparer);
				}
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return keyComparer.GetHashCode();
		}
	}

	[NonSerialized]
	private KeyCollection _keys;

	[NonSerialized]
	private ValueCollection _values;

	private readonly TreeSet<KeyValuePair<TKey, TValue>> _set;

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	public TValue this[TKey key]
	{
		get
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			SortedSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
			if (node == null)
			{
				throw new KeyNotFoundException(System.SR.Format(System.SR.Arg_KeyNotFoundWithKey, key.ToString()));
			}
			return node.Item.Value;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			SortedSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
			if (node == null)
			{
				_set.Add(new KeyValuePair<TKey, TValue>(key, value));
				return;
			}
			node.Item = new KeyValuePair<TKey, TValue>(node.Item.Key, value);
			_set.UpdateVersion();
		}
	}

	public int Count => _set.Count;

	public IComparer<TKey> Comparer => ((KeyValuePairComparer)_set.Comparer).keyComparer;

	public KeyCollection Keys
	{
		get
		{
			if (_keys == null)
			{
				_keys = new KeyCollection(this);
			}
			return _keys;
		}
	}

	ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

	public ValueCollection Values
	{
		get
		{
			if (_values == null)
			{
				_values = new ValueCollection(this);
			}
			return _values;
		}
	}

	ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

	bool IDictionary.IsFixedSize => false;

	bool IDictionary.IsReadOnly => false;

	ICollection IDictionary.Keys => Keys;

	ICollection IDictionary.Values => Values;

	object? IDictionary.this[object key]
	{
		get
		{
			if (IsCompatibleKey(key) && TryGetValue((TKey)key, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			if (value == null && default(TValue) != null)
			{
				throw new ArgumentNullException("value");
			}
			try
			{
				TKey key2 = (TKey)key;
				try
				{
					this[key2] = (TValue)value;
				}
				catch (InvalidCastException)
				{
					throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, value, typeof(TValue)), "value");
				}
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, key, typeof(TKey)), "key");
			}
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => ((ICollection)_set).SyncRoot;

	public SortedDictionary()
		: this((IComparer<TKey>?)null)
	{
	}

	public SortedDictionary(IDictionary<TKey, TValue> dictionary)
		: this(dictionary, (IComparer<TKey>?)null)
	{
	}

	public SortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		KeyValuePairComparer keyValuePairComparer = new KeyValuePairComparer(comparer);
		if (dictionary is SortedDictionary<TKey, TValue> sortedDictionary && sortedDictionary._set.Comparer is KeyValuePairComparer keyValuePairComparer2 && keyValuePairComparer2.keyComparer.Equals(keyValuePairComparer.keyComparer))
		{
			_set = new TreeSet<KeyValuePair<TKey, TValue>>(sortedDictionary._set, keyValuePairComparer);
			return;
		}
		_set = new TreeSet<KeyValuePair<TKey, TValue>>(keyValuePairComparer);
		foreach (KeyValuePair<TKey, TValue> item in dictionary)
		{
			_set.Add(item);
		}
	}

	public SortedDictionary(IComparer<TKey>? comparer)
	{
		_set = new TreeSet<KeyValuePair<TKey, TValue>>(new KeyValuePairComparer(comparer));
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
	{
		_set.Add(keyValuePair);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
	{
		SortedSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(keyValuePair);
		if (node == null)
		{
			return false;
		}
		if (keyValuePair.Value == null)
		{
			return node.Item.Value == null;
		}
		return EqualityComparer<TValue>.Default.Equals(node.Item.Value, keyValuePair.Value);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
	{
		SortedSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(keyValuePair);
		if (node == null)
		{
			return false;
		}
		if (EqualityComparer<TValue>.Default.Equals(node.Item.Value, keyValuePair.Value))
		{
			_set.Remove(keyValuePair);
			return true;
		}
		return false;
	}

	public void Add(TKey key, TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		_set.Add(new KeyValuePair<TKey, TValue>(key, value));
	}

	public void Clear()
	{
		_set.Clear();
	}

	public bool ContainsKey(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return _set.Contains(new KeyValuePair<TKey, TValue>(key, default(TValue)));
	}

	public bool ContainsValue(TValue value)
	{
		TValue value2 = value;
		bool found = false;
		if (value2 == null)
		{
			_set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
			{
				if (node.Item.Value == null)
				{
					found = true;
					return false;
				}
				return true;
			});
		}
		else
		{
			EqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;
			_set.InOrderTreeWalk(delegate(SortedSet<KeyValuePair<TKey, TValue>>.Node node)
			{
				if (valueComparer.Equals(node.Item.Value, value2))
				{
					found = true;
					return false;
				}
				return true;
			});
		}
		return found;
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		_set.CopyTo(array, index);
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}

	public bool Remove(TKey key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return _set.Remove(new KeyValuePair<TKey, TValue>(key, default(TValue)));
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		SortedSet<KeyValuePair<TKey, TValue>>.Node node = _set.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));
		if (node == null)
		{
			value = default(TValue);
			return false;
		}
		value = node.Item.Value;
		return true;
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_set).CopyTo(array, index);
	}

	void IDictionary.Add(object key, object value)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		if (value == null && default(TValue) != null)
		{
			throw new ArgumentNullException("value");
		}
		try
		{
			TKey key2 = (TKey)key;
			try
			{
				Add(key2, (TValue)value);
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, value, typeof(TValue)), "value");
			}
		}
		catch (InvalidCastException)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Arg_WrongType, key, typeof(TKey)), "key");
		}
	}

	bool IDictionary.Contains(object key)
	{
		if (IsCompatibleKey(key))
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	private static bool IsCompatibleKey(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return key is TKey;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		return new Enumerator(this, 2);
	}

	void IDictionary.Remove(object key)
	{
		if (IsCompatibleKey(key))
		{
			Remove((TKey)key);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this, 1);
	}
}
