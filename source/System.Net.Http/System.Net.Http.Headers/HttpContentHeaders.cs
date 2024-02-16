using System.Collections.Generic;

namespace System.Net.Http.Headers;

public sealed class HttpContentHeaders : HttpHeaders
{
	private readonly HttpContent _parent;

	private bool _contentLengthSet;

	private HttpHeaderValueCollection<string> _allow;

	private HttpHeaderValueCollection<string> _contentEncoding;

	private HttpHeaderValueCollection<string> _contentLanguage;

	public ICollection<string> Allow
	{
		get
		{
			if (_allow == null)
			{
				_allow = new HttpHeaderValueCollection<string>(KnownHeaders.Allow.Descriptor, this, HeaderUtilities.TokenValidator);
			}
			return _allow;
		}
	}

	public ContentDispositionHeaderValue? ContentDisposition
	{
		get
		{
			return (ContentDispositionHeaderValue)GetParsedValues(KnownHeaders.ContentDisposition.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentDisposition.Descriptor, value);
		}
	}

	public ICollection<string> ContentEncoding
	{
		get
		{
			if (_contentEncoding == null)
			{
				_contentEncoding = new HttpHeaderValueCollection<string>(KnownHeaders.ContentEncoding.Descriptor, this, HeaderUtilities.TokenValidator);
			}
			return _contentEncoding;
		}
	}

	public ICollection<string> ContentLanguage
	{
		get
		{
			if (_contentLanguage == null)
			{
				_contentLanguage = new HttpHeaderValueCollection<string>(KnownHeaders.ContentLanguage.Descriptor, this, HeaderUtilities.TokenValidator);
			}
			return _contentLanguage;
		}
	}

	public long? ContentLength
	{
		get
		{
			object parsedValues = GetParsedValues(KnownHeaders.ContentLength.Descriptor);
			if (!_contentLengthSet && parsedValues == null)
			{
				long? computedOrBufferLength = _parent.GetComputedOrBufferLength();
				if (computedOrBufferLength.HasValue)
				{
					SetParsedValue(KnownHeaders.ContentLength.Descriptor, computedOrBufferLength.Value);
				}
				return computedOrBufferLength;
			}
			if (parsedValues == null)
			{
				return null;
			}
			return (long)parsedValues;
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentLength.Descriptor, value);
			_contentLengthSet = true;
		}
	}

	public Uri? ContentLocation
	{
		get
		{
			return (Uri)GetParsedValues(KnownHeaders.ContentLocation.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentLocation.Descriptor, value);
		}
	}

	public byte[]? ContentMD5
	{
		get
		{
			return (byte[])GetParsedValues(KnownHeaders.ContentMD5.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentMD5.Descriptor, value);
		}
	}

	public ContentRangeHeaderValue? ContentRange
	{
		get
		{
			return (ContentRangeHeaderValue)GetParsedValues(KnownHeaders.ContentRange.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentRange.Descriptor, value);
		}
	}

	public MediaTypeHeaderValue? ContentType
	{
		get
		{
			return (MediaTypeHeaderValue)GetParsedValues(KnownHeaders.ContentType.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ContentType.Descriptor, value);
		}
	}

	public DateTimeOffset? Expires
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.Expires.Descriptor, this, DateTimeOffset.MinValue);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Expires.Descriptor, value);
		}
	}

	public DateTimeOffset? LastModified
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.LastModified.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.LastModified.Descriptor, value);
		}
	}

	internal HttpContentHeaders(HttpContent parent)
		: base(HttpHeaderType.Content | HttpHeaderType.Custom, HttpHeaderType.None)
	{
		_parent = parent;
	}
}
