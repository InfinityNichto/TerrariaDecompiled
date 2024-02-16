namespace System.Net.Http.Headers;

internal sealed class HttpGeneralHeaders
{
	private HttpHeaderValueCollection<string> _connection;

	private HttpHeaderValueCollection<string> _trailer;

	private HttpHeaderValueCollection<TransferCodingHeaderValue> _transferEncoding;

	private HttpHeaderValueCollection<ProductHeaderValue> _upgrade;

	private HttpHeaderValueCollection<ViaHeaderValue> _via;

	private HttpHeaderValueCollection<WarningHeaderValue> _warning;

	private HttpHeaderValueCollection<NameValueHeaderValue> _pragma;

	private readonly HttpHeaders _parent;

	private bool _transferEncodingChunkedSet;

	private bool _connectionCloseSet;

	public CacheControlHeaderValue CacheControl
	{
		get
		{
			return (CacheControlHeaderValue)_parent.GetParsedValues(KnownHeaders.CacheControl.Descriptor);
		}
		set
		{
			_parent.SetOrRemoveParsedValue(KnownHeaders.CacheControl.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<string> Connection => ConnectionCore;

	public bool? ConnectionClose
	{
		get
		{
			return GetConnectionClose(_parent, this);
		}
		set
		{
			if (value == true)
			{
				_connectionCloseSet = true;
				ConnectionCore.SetSpecialValue();
			}
			else
			{
				_connectionCloseSet = value.HasValue;
				ConnectionCore.RemoveSpecialValue();
			}
		}
	}

	public DateTimeOffset? Date
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Date.Descriptor, _parent);
		}
		set
		{
			_parent.SetOrRemoveParsedValue(KnownHeaders.Date.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<NameValueHeaderValue> Pragma
	{
		get
		{
			if (_pragma == null)
			{
				_pragma = new HttpHeaderValueCollection<NameValueHeaderValue>(KnownHeaders.Pragma.Descriptor, _parent);
			}
			return _pragma;
		}
	}

	public HttpHeaderValueCollection<string> Trailer
	{
		get
		{
			if (_trailer == null)
			{
				_trailer = new HttpHeaderValueCollection<string>(KnownHeaders.Trailer.Descriptor, _parent, HeaderUtilities.TokenValidator);
			}
			return _trailer;
		}
	}

	public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding => TransferEncodingCore;

	public bool? TransferEncodingChunked
	{
		get
		{
			return GetTransferEncodingChunked(_parent, this);
		}
		set
		{
			if (value == true)
			{
				_transferEncodingChunkedSet = true;
				TransferEncodingCore.SetSpecialValue();
			}
			else
			{
				_transferEncodingChunkedSet = value.HasValue;
				TransferEncodingCore.RemoveSpecialValue();
			}
		}
	}

	public HttpHeaderValueCollection<ProductHeaderValue> Upgrade
	{
		get
		{
			if (_upgrade == null)
			{
				_upgrade = new HttpHeaderValueCollection<ProductHeaderValue>(KnownHeaders.Upgrade.Descriptor, _parent);
			}
			return _upgrade;
		}
	}

	public HttpHeaderValueCollection<ViaHeaderValue> Via
	{
		get
		{
			if (_via == null)
			{
				_via = new HttpHeaderValueCollection<ViaHeaderValue>(KnownHeaders.Via.Descriptor, _parent);
			}
			return _via;
		}
	}

	public HttpHeaderValueCollection<WarningHeaderValue> Warning
	{
		get
		{
			if (_warning == null)
			{
				_warning = new HttpHeaderValueCollection<WarningHeaderValue>(KnownHeaders.Warning.Descriptor, _parent);
			}
			return _warning;
		}
	}

	private HttpHeaderValueCollection<string> ConnectionCore
	{
		get
		{
			if (_connection == null)
			{
				_connection = new HttpHeaderValueCollection<string>(KnownHeaders.Connection.Descriptor, _parent, "close", HeaderUtilities.TokenValidator);
			}
			return _connection;
		}
	}

	private HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncodingCore
	{
		get
		{
			if (_transferEncoding == null)
			{
				_transferEncoding = new HttpHeaderValueCollection<TransferCodingHeaderValue>(KnownHeaders.TransferEncoding.Descriptor, _parent, HeaderUtilities.TransferEncodingChunked);
			}
			return _transferEncoding;
		}
	}

	internal static bool? GetConnectionClose(HttpHeaders parent, HttpGeneralHeaders headers)
	{
		if (headers?._connection != null)
		{
			if (headers._connection.IsSpecialValueSet)
			{
				return true;
			}
		}
		else if (parent.ContainsParsedValue(KnownHeaders.Connection.Descriptor, "close"))
		{
			return true;
		}
		if (headers != null && headers._connectionCloseSet)
		{
			return false;
		}
		return null;
	}

	internal static bool? GetTransferEncodingChunked(HttpHeaders parent, HttpGeneralHeaders headers)
	{
		if (headers?._transferEncoding != null)
		{
			if (headers._transferEncoding.IsSpecialValueSet)
			{
				return true;
			}
		}
		else if (parent.ContainsParsedValue(KnownHeaders.TransferEncoding.Descriptor, HeaderUtilities.TransferEncodingChunked))
		{
			return true;
		}
		if (headers != null && headers._transferEncodingChunkedSet)
		{
			return false;
		}
		return null;
	}

	internal HttpGeneralHeaders(HttpHeaders parent)
	{
		_parent = parent;
	}

	internal void AddSpecialsFrom(HttpGeneralHeaders sourceHeaders)
	{
		if (!TransferEncodingChunked.HasValue)
		{
			TransferEncodingChunked = sourceHeaders.TransferEncodingChunked;
		}
		if (!ConnectionClose.HasValue)
		{
			ConnectionClose = sourceHeaders.ConnectionClose;
		}
	}
}
