using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json;

internal class JsonPropertyDictionary<T> where T : class
{
	private sealed class KeyCollection : ICollection<string>, IEnumerable<string>, IEnumerable
	{
		private readonly JsonPropertyDictionary<T> _parent;

		public int Count => _parent.Count;

		public bool IsReadOnly => true;

		public KeyCollection(JsonPropertyDictionary<T> jsonObject)
		{
			_parent = jsonObject;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Key;
			}
		}

		public void Add(string propertyName)
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}

		public void Clear()
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}

		public bool Contains(string propertyName)
		{
			return _parent.ContainsProperty(propertyName);
		}

		public void CopyTo(string[] propertyNameArray, int index)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative("index");
			}
			foreach (KeyValuePair<string, T> item in _parent)
			{
				if (index >= propertyNameArray.Length)
				{
					ThrowHelper.ThrowArgumentException_NodeArrayTooSmall("propertyNameArray");
				}
				propertyNameArray[index++] = item.Key;
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Key;
			}
		}

		bool ICollection<string>.Remove(string propertyName)
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}
	}

	private sealed class ValueCollection : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly JsonPropertyDictionary<T> _parent;

		public int Count => _parent.Count;

		public bool IsReadOnly => true;

		public ValueCollection(JsonPropertyDictionary<T> jsonObject)
		{
			_parent = jsonObject;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Value;
			}
		}

		public void Add(T jsonNode)
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}

		public void Clear()
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}

		public bool Contains(T jsonNode)
		{
			return _parent.ContainsValue(jsonNode);
		}

		public void CopyTo(T[] nodeArray, int index)
		{
			if (index < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative("index");
			}
			foreach (KeyValuePair<string, T> item in _parent)
			{
				if (index >= nodeArray.Length)
				{
					ThrowHelper.ThrowArgumentException_NodeArrayTooSmall("nodeArray");
				}
				nodeArray[index++] = item.Value;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			foreach (KeyValuePair<string, T> item in _parent)
			{
				yield return item.Value;
			}
		}

		bool ICollection<T>.Remove(T node)
		{
			throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
		}
	}

	private Dictionary<string, T> _propertyDictionary;

	private readonly List<KeyValuePair<string, T>> _propertyList;

	private StringComparer _stringComparer;

	private KeyCollection _keyCollection;

	private ValueCollection _valueCollection;

	public List<KeyValuePair<string, T>> List => _propertyList;

	public int Count => _propertyList.Count;

	public ICollection<string> Keys => GetKeyCollection();

	public ICollection<T> Values => GetValueCollection();

	public bool IsReadOnly { get; }

	public T this[string propertyName]
	{
		get
		{
			if (TryGetPropertyValue(propertyName, out var value))
			{
				return value;
			}
			return null;
		}
		set
		{
			SetValue(propertyName, value);
		}
	}

	public JsonPropertyDictionary(bool caseInsensitive)
	{
		_stringComparer = (caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		_propertyList = new List<KeyValuePair<string, T>>();
	}

	public JsonPropertyDictionary(bool caseInsensitive, int capacity)
	{
		_stringComparer = (caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		_propertyList = new List<KeyValuePair<string, T>>(capacity);
	}

	public void Add(string propertyName, T value)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		AddValue(propertyName, value);
	}

	public void Add(KeyValuePair<string, T> property)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		Add(property.Key, property.Value);
	}

	public bool TryAdd(string propertyName, T value)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		return TryAddValue(propertyName, value);
	}

	public void Clear()
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		_propertyList.Clear();
		_propertyDictionary?.Clear();
	}

	public bool ContainsKey(string propertyName)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		return ContainsProperty(propertyName);
	}

	public bool Contains(KeyValuePair<string, T> item)
	{
		using (IEnumerator<KeyValuePair<string, T>> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, T> current = enumerator.Current;
				if (item.Value == current.Value && _stringComparer.Equals(item.Key, current.Key))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void CopyTo(KeyValuePair<string, T>[] array, int index)
	{
		if (index < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative("index");
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (index >= array.Length)
			{
				ThrowHelper.ThrowArgumentException_NodeArrayTooSmall("array");
			}
			array[index++] = property;
		}
	}

	public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
	{
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			yield return property;
		}
	}

	public bool TryGetValue(string propertyName, out T value)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		if (_propertyDictionary != null)
		{
			return _propertyDictionary.TryGetValue(propertyName, out value);
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (_stringComparer.Equals(propertyName, property.Key))
			{
				value = property.Value;
				return true;
			}
		}
		value = null;
		return false;
	}

	public T SetValue(string propertyName, T value, Action assignParent = null)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		CreateDictionaryIfThresholdMet();
		T val = null;
		if (_propertyDictionary != null)
		{
			if (_propertyDictionary.TryAdd(in propertyName, in value))
			{
				assignParent?.Invoke();
				_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
				return null;
			}
			val = _propertyDictionary[propertyName];
			if (val == value)
			{
				return null;
			}
		}
		int num = FindValueIndex(propertyName);
		if (num >= 0)
		{
			if (_propertyDictionary != null)
			{
				_propertyDictionary[propertyName] = value;
			}
			else
			{
				KeyValuePair<string, T> keyValuePair = _propertyList[num];
				if (keyValuePair.Value == value)
				{
					return null;
				}
				val = keyValuePair.Value;
			}
			assignParent?.Invoke();
			_propertyList[num] = new KeyValuePair<string, T>(propertyName, value);
		}
		else
		{
			assignParent?.Invoke();
			_propertyDictionary?.Add(propertyName, value);
			_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
		}
		return val;
	}

	private void AddValue(string propertyName, T value)
	{
		if (!TryAddValue(propertyName, value))
		{
			ThrowHelper.ThrowArgumentException_DuplicateKey(propertyName);
		}
	}

	private bool TryAddValue(string propertyName, T value)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		CreateDictionaryIfThresholdMet();
		if (_propertyDictionary == null)
		{
			if (ContainsProperty(propertyName))
			{
				return false;
			}
		}
		else if (!_propertyDictionary.TryAdd(in propertyName, in value))
		{
			return false;
		}
		_propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
		return true;
	}

	private void CreateDictionaryIfThresholdMet()
	{
		if (_propertyDictionary == null && _propertyList.Count > 9)
		{
			_propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _stringComparer);
		}
	}

	private bool ContainsValue(T value)
	{
		foreach (T item in GetValueCollection())
		{
			if (item == value)
			{
				return true;
			}
		}
		return false;
	}

	public KeyValuePair<string, T>? FindValue(T value)
	{
		using (IEnumerator<KeyValuePair<string, T>> enumerator = GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<string, T> current = enumerator.Current;
				if (current.Value == value)
				{
					return current;
				}
			}
		}
		return null;
	}

	private bool ContainsProperty(string propertyName)
	{
		if (_propertyDictionary != null)
		{
			return _propertyDictionary.ContainsKey(propertyName);
		}
		foreach (KeyValuePair<string, T> property in _propertyList)
		{
			if (_stringComparer.Equals(propertyName, property.Key))
			{
				return true;
			}
		}
		return false;
	}

	private int FindValueIndex(string propertyName)
	{
		for (int i = 0; i < _propertyList.Count; i++)
		{
			KeyValuePair<string, T> keyValuePair = _propertyList[i];
			if (_stringComparer.Equals(propertyName, keyValuePair.Key))
			{
				return i;
			}
		}
		return -1;
	}

	public bool TryGetPropertyValue(string propertyName, out T value)
	{
		return TryGetValue(propertyName, out value);
	}

	public bool TryRemoveProperty(string propertyName, out T existing)
	{
		if (IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
		}
		if (_propertyDictionary != null)
		{
			if (!_propertyDictionary.TryGetValue(propertyName, out existing))
			{
				return false;
			}
			bool flag = _propertyDictionary.Remove(propertyName);
		}
		for (int i = 0; i < _propertyList.Count; i++)
		{
			KeyValuePair<string, T> keyValuePair = _propertyList[i];
			if (_stringComparer.Equals(keyValuePair.Key, propertyName))
			{
				_propertyList.RemoveAt(i);
				existing = keyValuePair.Value;
				return true;
			}
		}
		existing = null;
		return false;
	}

	public ICollection<string> GetKeyCollection()
	{
		return _keyCollection ?? (_keyCollection = new KeyCollection(this));
	}

	public ICollection<T> GetValueCollection()
	{
		return _valueCollection ?? (_valueCollection = new ValueCollection(this));
	}
}
