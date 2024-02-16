using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.ObjectModel;

[Serializable]
[DebuggerTypeProxy(typeof(DictionaryDebugView<, >))]
[DebuggerDisplay("Count = {Count}")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary, ICollection, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	private struct DictionaryEnumerator : IDictionaryEnumerator, IEnumerator
	{
		private readonly IDictionary<TKey, TValue> _dictionary;

		private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

		public DictionaryEntry Entry => new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);

		public object Key => _enumerator.Current.Key;

		public object Value => _enumerator.Current.Value;

		public object Current => Entry;

		public DictionaryEnumerator(IDictionary<TKey, TValue> dictionary)
		{
			_dictionary = dictionary;
			_enumerator = _dictionary.GetEnumerator();
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}
	}

	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class KeyCollection : ICollection<TKey>, IEnumerable<TKey>, IEnumerable, ICollection, IReadOnlyCollection<TKey>
	{
		private readonly ICollection<TKey> _collection;

		public int Count => _collection.Count;

		bool ICollection<TKey>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if (!(_collection is ICollection collection))
				{
					return this;
				}
				return collection.SyncRoot;
			}
		}

		internal KeyCollection(ICollection<TKey> collection)
		{
			_collection = collection ?? throw new ArgumentNullException("collection");
		}

		void ICollection<TKey>.Add(TKey item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		void ICollection<TKey>.Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		bool ICollection<TKey>.Contains(TKey item)
		{
			return _collection.Contains(item);
		}

		public void CopyTo(TKey[] array, int arrayIndex)
		{
			_collection.CopyTo(array, arrayIndex);
		}

		bool ICollection<TKey>.Remove(TKey item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		public IEnumerator<TKey> GetEnumerator()
		{
			return _collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_collection).GetEnumerator();
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CollectionHelpers.CopyTo(_collection, array, index);
		}
	}

	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ValueCollection : ICollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
	{
		private readonly ICollection<TValue> _collection;

		public int Count => _collection.Count;

		bool ICollection<TValue>.IsReadOnly => true;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if (!(_collection is ICollection collection))
				{
					return this;
				}
				return collection.SyncRoot;
			}
		}

		internal ValueCollection(ICollection<TValue> collection)
		{
			_collection = collection ?? throw new ArgumentNullException("collection");
		}

		void ICollection<TValue>.Add(TValue item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		void ICollection<TValue>.Clear()
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		bool ICollection<TValue>.Contains(TValue item)
		{
			return _collection.Contains(item);
		}

		public void CopyTo(TValue[] array, int arrayIndex)
		{
			_collection.CopyTo(array, arrayIndex);
		}

		bool ICollection<TValue>.Remove(TValue item)
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return _collection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_collection).GetEnumerator();
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CollectionHelpers.CopyTo(_collection, array, index);
		}
	}

	private readonly IDictionary<TKey, TValue> m_dictionary;

	[NonSerialized]
	private KeyCollection _keys;

	[NonSerialized]
	private ValueCollection _values;

	protected IDictionary<TKey, TValue> Dictionary => m_dictionary;

	public KeyCollection Keys => _keys ?? (_keys = new KeyCollection(m_dictionary.Keys));

	public ValueCollection Values => _values ?? (_values = new ValueCollection(m_dictionary.Values));

	ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

	ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

	public TValue this[TKey key] => m_dictionary[key];

	TValue IDictionary<TKey, TValue>.this[TKey key]
	{
		get
		{
			return m_dictionary[key];
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	public int Count => m_dictionary.Count;

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

	bool IDictionary.IsFixedSize => true;

	bool IDictionary.IsReadOnly => true;

	ICollection IDictionary.Keys => Keys;

	ICollection IDictionary.Values => Values;

	object? IDictionary.this[object key]
	{
		get
		{
			if (!IsCompatibleKey(key))
			{
				return null;
			}
			if (m_dictionary.TryGetValue((TKey)key, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot
	{
		get
		{
			if (!(m_dictionary is ICollection collection))
			{
				return this;
			}
			return collection.SyncRoot;
		}
	}

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

	public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
	{
		m_dictionary = dictionary ?? throw new ArgumentNullException("dictionary");
	}

	public bool ContainsKey(TKey key)
	{
		return m_dictionary.ContainsKey(key);
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		return m_dictionary.TryGetValue(key, out value);
	}

	void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool IDictionary<TKey, TValue>.Remove(TKey key)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
	{
		return m_dictionary.Contains(item);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		m_dictionary.CopyTo(array, arrayIndex);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		return m_dictionary.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)m_dictionary).GetEnumerator();
	}

	private static bool IsCompatibleKey(object key)
	{
		if (key == null)
		{
			throw new ArgumentNullException("key");
		}
		return key is TKey;
	}

	void IDictionary.Add(object key, object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IDictionary.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool IDictionary.Contains(object key)
	{
		if (IsCompatibleKey(key))
		{
			return ContainsKey((TKey)key);
		}
		return false;
	}

	IDictionaryEnumerator IDictionary.GetEnumerator()
	{
		if (m_dictionary is IDictionary dictionary)
		{
			return dictionary.GetEnumerator();
		}
		return new DictionaryEnumerator(m_dictionary);
	}

	void IDictionary.Remove(object key)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		CollectionHelpers.ValidateCopyToArguments(Count, array, index);
		if (array is KeyValuePair<TKey, TValue>[] array2)
		{
			m_dictionary.CopyTo(array2, index);
			return;
		}
		if (array is DictionaryEntry[] array3)
		{
			{
				foreach (KeyValuePair<TKey, TValue> item in m_dictionary)
				{
					array3[index++] = new DictionaryEntry(item.Key, item.Value);
				}
				return;
			}
		}
		if (!(array is object[] array4))
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
		try
		{
			foreach (KeyValuePair<TKey, TValue> item2 in m_dictionary)
			{
				array4[index++] = new KeyValuePair<TKey, TValue>(item2.Key, item2.Value);
			}
		}
		catch (ArrayTypeMismatchException)
		{
			throw new ArgumentException(System.SR.Argument_InvalidArrayType, "array");
		}
	}
}
