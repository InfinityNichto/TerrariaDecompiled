using System.Collections;
using System.Collections.Generic;

namespace System.Net;

public class HttpListenerPrefixCollection : ICollection<string>, IEnumerable<string>, IEnumerable
{
	private readonly HttpListener _httpListener;

	public int Count => _httpListener.PrefixCollection.Count;

	public bool IsSynchronized => false;

	public bool IsReadOnly => false;

	internal HttpListenerPrefixCollection(HttpListener listener)
	{
		_httpListener = listener;
	}

	public void CopyTo(Array array, int offset)
	{
		_httpListener.CheckDisposed();
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (Count > array.Length)
		{
			throw new ArgumentOutOfRangeException("array", System.SR.net_array_too_small);
		}
		if (offset + Count > array.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		int num = 0;
		foreach (string item in _httpListener.PrefixCollection)
		{
			array.SetValue(item, offset + num++);
		}
	}

	public void CopyTo(string[] array, int offset)
	{
		_httpListener.CheckDisposed();
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (Count > array.Length)
		{
			throw new ArgumentOutOfRangeException("array", System.SR.net_array_too_small);
		}
		if (offset + Count > array.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		int num = 0;
		foreach (string item in _httpListener.PrefixCollection)
		{
			array[offset + num++] = item;
		}
	}

	public void Add(string uriPrefix)
	{
		_httpListener.AddPrefix(uriPrefix);
	}

	public bool Contains(string uriPrefix)
	{
		return _httpListener.ContainsPrefix(uriPrefix);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<string> GetEnumerator()
	{
		return new ListenerPrefixEnumerator(_httpListener.PrefixCollection.GetEnumerator());
	}

	public bool Remove(string uriPrefix)
	{
		return _httpListener.RemovePrefix(uriPrefix);
	}

	public void Clear()
	{
		_httpListener.RemoveAll(clear: true);
	}
}
