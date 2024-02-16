using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers;

public readonly struct HttpHeadersNonValidated : IReadOnlyDictionary<string, HeaderStringValues>, IEnumerable<KeyValuePair<string, HeaderStringValues>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, HeaderStringValues>>
{
	public struct Enumerator : IEnumerator<KeyValuePair<string, HeaderStringValues>>, IEnumerator, IDisposable
	{
		private Dictionary<HeaderDescriptor, object>.Enumerator _headerStoreEnumerator;

		private KeyValuePair<string, HeaderStringValues> _current;

		private bool _valid;

		public KeyValuePair<string, HeaderStringValues> Current => _current;

		object IEnumerator.Current => _current;

		internal Enumerator(Dictionary<HeaderDescriptor, object>.Enumerator headerStoreEnumerator)
		{
			_headerStoreEnumerator = headerStoreEnumerator;
			_current = default(KeyValuePair<string, HeaderStringValues>);
			_valid = true;
		}

		public bool MoveNext()
		{
			if (_valid && _headerStoreEnumerator.MoveNext())
			{
				KeyValuePair<HeaderDescriptor, object> current = _headerStoreEnumerator.Current;
				HttpHeaders.GetStoreValuesAsStringOrStringArray(current.Key, current.Value, out var singleValue, out var multiValue);
				_current = new KeyValuePair<string, HeaderStringValues>(current.Key.Name, (singleValue != null) ? new HeaderStringValues(current.Key, singleValue) : new HeaderStringValues(current.Key, multiValue));
				return true;
			}
			_current = default(KeyValuePair<string, HeaderStringValues>);
			return false;
		}

		public void Dispose()
		{
		}

		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	private readonly HttpHeaders _headers;

	public int Count => (_headers?.HeaderStore?.Count).GetValueOrDefault();

	public HeaderStringValues this[string headerName]
	{
		get
		{
			if (TryGetValues(headerName, out var values))
			{
				return values;
			}
			throw new KeyNotFoundException(System.SR.net_http_headers_not_found);
		}
	}

	IEnumerable<string> IReadOnlyDictionary<string, HeaderStringValues>.Keys
	{
		get
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current.Key;
			}
		}
	}

	IEnumerable<HeaderStringValues> IReadOnlyDictionary<string, HeaderStringValues>.Values
	{
		get
		{
			using Enumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current.Value;
			}
		}
	}

	internal HttpHeadersNonValidated(HttpHeaders headers)
	{
		_headers = headers;
	}

	public bool Contains(string headerName)
	{
		HttpHeaders headers = _headers;
		object value;
		if (headers != null && HeaderDescriptor.TryGet(headerName, out var descriptor))
		{
			return headers.TryGetHeaderValue(descriptor, out value);
		}
		return false;
	}

	bool IReadOnlyDictionary<string, HeaderStringValues>.ContainsKey(string key)
	{
		return Contains(key);
	}

	public bool TryGetValues(string headerName, out HeaderStringValues values)
	{
		HttpHeaders headers = _headers;
		if (headers != null && HeaderDescriptor.TryGet(headerName, out var descriptor) && headers.TryGetHeaderValue(descriptor, out var value))
		{
			HttpHeaders.GetStoreValuesAsStringOrStringArray(descriptor, value, out var singleValue, out var multiValue);
			values = ((singleValue != null) ? new HeaderStringValues(descriptor, singleValue) : new HeaderStringValues(descriptor, multiValue));
			return true;
		}
		values = default(HeaderStringValues);
		return false;
	}

	bool IReadOnlyDictionary<string, HeaderStringValues>.TryGetValue(string key, out HeaderStringValues value)
	{
		return TryGetValues(key, out value);
	}

	public Enumerator GetEnumerator()
	{
		HttpHeaders headers = _headers;
		if (headers != null)
		{
			Dictionary<HeaderDescriptor, object> headerStore = headers.HeaderStore;
			if (headerStore != null)
			{
				return new Enumerator(headerStore.GetEnumerator());
			}
		}
		return default(Enumerator);
	}

	IEnumerator<KeyValuePair<string, HeaderStringValues>> IEnumerable<KeyValuePair<string, HeaderStringValues>>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
