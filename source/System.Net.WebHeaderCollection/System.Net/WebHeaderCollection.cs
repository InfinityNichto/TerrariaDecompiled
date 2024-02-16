using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Text;

namespace System.Net;

public class WebHeaderCollection : NameValueCollection, ISerializable
{
	private WebHeaderCollectionType _type;

	private NameValueCollection _innerCollection;

	private static HeaderInfoTable _headerInfo;

	private bool AllowHttpRequestHeader
	{
		get
		{
			if (_type == WebHeaderCollectionType.Unknown)
			{
				_type = WebHeaderCollectionType.WebRequest;
			}
			return _type == WebHeaderCollectionType.WebRequest;
		}
	}

	private static HeaderInfoTable HeaderInfo
	{
		get
		{
			if (_headerInfo == null)
			{
				_headerInfo = new HeaderInfoTable();
			}
			return _headerInfo;
		}
	}

	private NameValueCollection InnerCollection
	{
		get
		{
			if (_innerCollection == null)
			{
				_innerCollection = new NameValueCollection(16, CaseInsensitiveAscii.StaticInstance);
			}
			return _innerCollection;
		}
	}

	private bool AllowHttpResponseHeader
	{
		get
		{
			if (_type == WebHeaderCollectionType.Unknown)
			{
				_type = WebHeaderCollectionType.WebResponse;
			}
			return _type == WebHeaderCollectionType.WebResponse;
		}
	}

	public string? this[HttpRequestHeader header]
	{
		get
		{
			if (!AllowHttpRequestHeader)
			{
				throw new InvalidOperationException(System.SR.net_headers_req);
			}
			return base[header.GetName()];
		}
		set
		{
			if (!AllowHttpRequestHeader)
			{
				throw new InvalidOperationException(System.SR.net_headers_req);
			}
			base[header.GetName()] = value;
		}
	}

	public string? this[HttpResponseHeader header]
	{
		get
		{
			if (!AllowHttpResponseHeader)
			{
				throw new InvalidOperationException(System.SR.net_headers_rsp);
			}
			return base[header.GetName()];
		}
		set
		{
			if (!AllowHttpResponseHeader)
			{
				throw new InvalidOperationException(System.SR.net_headers_rsp);
			}
			base[header.GetName()] = value;
		}
	}

	public override int Count
	{
		get
		{
			if (_innerCollection != null)
			{
				return _innerCollection.Count;
			}
			return 0;
		}
	}

	public override KeysCollection Keys => InnerCollection.Keys;

	public override string[] AllKeys => InnerCollection.AllKeys;

	protected WebHeaderCollection(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	public override void Set(string name, string? value)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
		value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
		InvalidateCachedArrays();
		InnerCollection.Set(name, value);
	}

	public void Set(HttpRequestHeader header, string? value)
	{
		if (!AllowHttpRequestHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_req);
		}
		Set(header.GetName(), value);
	}

	public void Set(HttpResponseHeader header, string? value)
	{
		if (!AllowHttpResponseHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_rsp);
		}
		Set(header.GetName(), value);
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	public void Remove(HttpRequestHeader header)
	{
		if (!AllowHttpRequestHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_req);
		}
		Remove(header.GetName());
	}

	public void Remove(HttpResponseHeader header)
	{
		if (!AllowHttpResponseHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_rsp);
		}
		Remove(header.GetName());
	}

	public override void OnDeserialization(object? sender)
	{
	}

	public static bool IsRestricted(string headerName)
	{
		return IsRestricted(headerName, response: false);
	}

	public static bool IsRestricted(string headerName, bool response)
	{
		headerName = HttpValidationHelpers.CheckBadHeaderNameChars(headerName);
		if (!response)
		{
			return HeaderInfo[headerName].IsRequestRestricted;
		}
		return HeaderInfo[headerName].IsResponseRestricted;
	}

	public override string[]? GetValues(int index)
	{
		return InnerCollection.GetValues(index);
	}

	public override string[]? GetValues(string header)
	{
		HeaderInfo headerInfo = HeaderInfo[header];
		string[] values = InnerCollection.GetValues(header);
		if (headerInfo == null || values == null || !headerInfo.AllowMultiValues)
		{
			return values;
		}
		List<string> list = null;
		for (int i = 0; i < values.Length; i++)
		{
			string[] array = headerInfo.Parser(values[i]);
			if (list == null)
			{
				if (array != null)
				{
					list = new List<string>(values);
					list.RemoveRange(i, values.Length - i);
					list.AddRange(array);
				}
			}
			else
			{
				list.AddRange(array);
			}
		}
		if (list != null)
		{
			return list.ToArray();
		}
		return values;
	}

	public override string GetKey(int index)
	{
		return InnerCollection.GetKey(index);
	}

	public override void Clear()
	{
		InvalidateCachedArrays();
		if (_innerCollection != null)
		{
			_innerCollection.Clear();
		}
	}

	public override string? Get(int index)
	{
		if (_innerCollection == null)
		{
			return null;
		}
		return _innerCollection.Get(index);
	}

	public override string? Get(string? name)
	{
		if (_innerCollection == null)
		{
			return null;
		}
		return _innerCollection.Get(name);
	}

	public void Add(HttpRequestHeader header, string? value)
	{
		if (!AllowHttpRequestHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_req);
		}
		Add(header.GetName(), value);
	}

	public void Add(HttpResponseHeader header, string? value)
	{
		if (!AllowHttpResponseHeader)
		{
			throw new InvalidOperationException(System.SR.net_headers_rsp);
		}
		Add(header.GetName(), value);
	}

	public void Add(string header)
	{
		if (string.IsNullOrEmpty(header))
		{
			throw new ArgumentNullException("header");
		}
		int num = header.IndexOf(':');
		if (num < 0)
		{
			throw new ArgumentException(System.SR.net_WebHeaderMissingColon, "header");
		}
		string name = header.Substring(0, num);
		string value = header.Substring(num + 1);
		name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
		value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
		InvalidateCachedArrays();
		InnerCollection.Add(name, value);
	}

	public override void Add(string name, string? value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptyStringCall, "name"), "name");
		}
		name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
		value = HttpValidationHelpers.CheckBadHeaderValueChars(value);
		InvalidateCachedArrays();
		InnerCollection.Add(name, value);
	}

	protected void AddWithoutValidate(string headerName, string? headerValue)
	{
		headerName = HttpValidationHelpers.CheckBadHeaderNameChars(headerName);
		headerValue = HttpValidationHelpers.CheckBadHeaderValueChars(headerValue);
		InvalidateCachedArrays();
		InnerCollection.Add(headerName, headerValue);
	}

	public override void Remove(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		name = HttpValidationHelpers.CheckBadHeaderNameChars(name);
		if (_innerCollection != null)
		{
			InvalidateCachedArrays();
			_innerCollection.Remove(name);
		}
	}

	public override string ToString()
	{
		if (Count == 0)
		{
			return "\r\n";
		}
		StringBuilder stringBuilder = new StringBuilder(30 * Count);
		foreach (string item in InnerCollection)
		{
			string value = InnerCollection.Get(item);
			stringBuilder.Append(item).Append(": ").Append(value)
				.Append("\r\n");
		}
		stringBuilder.Append("\r\n");
		return stringBuilder.ToString();
	}

	public byte[] ToByteArray()
	{
		string s = ToString();
		return Encoding.ASCII.GetBytes(s);
	}

	public WebHeaderCollection()
	{
	}

	public override IEnumerator GetEnumerator()
	{
		return InnerCollection.Keys.GetEnumerator();
	}
}
