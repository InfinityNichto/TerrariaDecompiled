using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers;

public sealed class HttpHeaderValueCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T : class
{
	private readonly HeaderDescriptor _descriptor;

	private readonly HttpHeaders _store;

	private readonly T _specialValue;

	private readonly Action<HttpHeaderValueCollection<T>, T> _validator;

	public int Count => GetCount();

	public bool IsReadOnly => false;

	internal bool IsSpecialValueSet
	{
		get
		{
			if (_specialValue == null)
			{
				return false;
			}
			return _store.ContainsParsedValue(_descriptor, _specialValue);
		}
	}

	internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store)
		: this(descriptor, store, (T)null, (Action<HttpHeaderValueCollection<T>, T>)null)
	{
	}

	internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store, Action<HttpHeaderValueCollection<T>, T> validator)
		: this(descriptor, store, (T)null, validator)
	{
	}

	internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store, T specialValue)
		: this(descriptor, store, specialValue, (Action<HttpHeaderValueCollection<T>, T>)null)
	{
	}

	internal HttpHeaderValueCollection(HeaderDescriptor descriptor, HttpHeaders store, T specialValue, Action<HttpHeaderValueCollection<T>, T> validator)
	{
		_store = store;
		_descriptor = descriptor;
		_specialValue = specialValue;
		_validator = validator;
	}

	public void Add(T item)
	{
		CheckValue(item);
		_store.AddParsedValue(_descriptor, item);
	}

	public void ParseAdd(string? input)
	{
		_store.Add(_descriptor, input);
	}

	public bool TryParseAdd(string? input)
	{
		return _store.TryParseAndAddValue(_descriptor, input);
	}

	public void Clear()
	{
		_store.Remove(_descriptor);
	}

	public bool Contains(T item)
	{
		CheckValue(item);
		return _store.ContainsParsedValue(_descriptor, item);
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (arrayIndex < 0 || arrayIndex > array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex");
		}
		object parsedValues = _store.GetParsedValues(_descriptor);
		if (parsedValues == null)
		{
			return;
		}
		if (!(parsedValues is List<object> list))
		{
			if (arrayIndex == array.Length)
			{
				throw new ArgumentException(System.SR.net_http_copyto_array_too_small);
			}
			array[arrayIndex] = (T)parsedValues;
		}
		else
		{
			list.CopyTo(array, arrayIndex);
		}
	}

	public bool Remove(T item)
	{
		CheckValue(item);
		return _store.RemoveParsedValue(_descriptor, item);
	}

	public IEnumerator<T> GetEnumerator()
	{
		object parsedValues = _store.GetParsedValues(_descriptor);
		if (parsedValues != null)
		{
			return Iterate(parsedValues);
		}
		return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
		static IEnumerator<T> Iterate(object storeValue)
		{
			if (storeValue is List<object> list)
			{
				foreach (object item in list)
				{
					yield return (T)item;
				}
			}
			else
			{
				yield return (T)storeValue;
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		return _store.GetHeaderString(_descriptor);
	}

	internal void SetSpecialValue()
	{
		if (!_store.ContainsParsedValue(_descriptor, _specialValue))
		{
			_store.AddParsedValue(_descriptor, _specialValue);
		}
	}

	internal void RemoveSpecialValue()
	{
		_store.RemoveParsedValue(_descriptor, _specialValue);
	}

	private void CheckValue(T item)
	{
		if (item == null)
		{
			throw new ArgumentNullException("item");
		}
		if (_validator != null)
		{
			_validator(this, item);
		}
	}

	private int GetCount()
	{
		object parsedValues = _store.GetParsedValues(_descriptor);
		if (parsedValues == null)
		{
			return 0;
		}
		if (!(parsedValues is List<object> list))
		{
			return 1;
		}
		return list.Count;
	}
}
