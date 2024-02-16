namespace System.Net.Http.Headers;

public sealed class HttpResponseHeaders : HttpHeaders
{
	private object[] _specialCollectionsSlots;

	private HttpGeneralHeaders _generalHeaders;

	private bool _containsTrailingHeaders;

	public HttpHeaderValueCollection<string> AcceptRanges => GetSpecializedCollection(0, (HttpResponseHeaders thisRef) => new HttpHeaderValueCollection<string>(KnownHeaders.AcceptRanges.Descriptor, thisRef, HeaderUtilities.TokenValidator));

	public TimeSpan? Age
	{
		get
		{
			return HeaderUtilities.GetTimeSpanValue(KnownHeaders.Age.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Age.Descriptor, value);
		}
	}

	public EntityTagHeaderValue? ETag
	{
		get
		{
			return (EntityTagHeaderValue)GetParsedValues(KnownHeaders.ETag.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ETag.Descriptor, value);
		}
	}

	public Uri? Location
	{
		get
		{
			return (Uri)GetParsedValues(KnownHeaders.Location.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Location.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<AuthenticationHeaderValue> ProxyAuthenticate => GetSpecializedCollection(1, (HttpResponseHeaders thisRef) => new HttpHeaderValueCollection<AuthenticationHeaderValue>(KnownHeaders.ProxyAuthenticate.Descriptor, thisRef));

	public RetryConditionHeaderValue? RetryAfter
	{
		get
		{
			return (RetryConditionHeaderValue)GetParsedValues(KnownHeaders.RetryAfter.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.RetryAfter.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<ProductInfoHeaderValue> Server => GetSpecializedCollection(2, (HttpResponseHeaders thisRef) => new HttpHeaderValueCollection<ProductInfoHeaderValue>(KnownHeaders.Server.Descriptor, thisRef));

	public HttpHeaderValueCollection<string> Vary => GetSpecializedCollection(3, (HttpResponseHeaders thisRef) => new HttpHeaderValueCollection<string>(KnownHeaders.Vary.Descriptor, thisRef, HeaderUtilities.TokenValidator));

	public HttpHeaderValueCollection<AuthenticationHeaderValue> WwwAuthenticate => GetSpecializedCollection(4, (HttpResponseHeaders thisRef) => new HttpHeaderValueCollection<AuthenticationHeaderValue>(KnownHeaders.WWWAuthenticate.Descriptor, thisRef));

	public CacheControlHeaderValue? CacheControl
	{
		get
		{
			return GeneralHeaders.CacheControl;
		}
		set
		{
			GeneralHeaders.CacheControl = value;
		}
	}

	public HttpHeaderValueCollection<string> Connection => GeneralHeaders.Connection;

	public bool? ConnectionClose
	{
		get
		{
			return HttpGeneralHeaders.GetConnectionClose(this, _generalHeaders);
		}
		set
		{
			GeneralHeaders.ConnectionClose = value;
		}
	}

	public DateTimeOffset? Date
	{
		get
		{
			return GeneralHeaders.Date;
		}
		set
		{
			GeneralHeaders.Date = value;
		}
	}

	public HttpHeaderValueCollection<NameValueHeaderValue> Pragma => GeneralHeaders.Pragma;

	public HttpHeaderValueCollection<string> Trailer => GeneralHeaders.Trailer;

	public HttpHeaderValueCollection<TransferCodingHeaderValue> TransferEncoding => GeneralHeaders.TransferEncoding;

	public bool? TransferEncodingChunked
	{
		get
		{
			return HttpGeneralHeaders.GetTransferEncodingChunked(this, _generalHeaders);
		}
		set
		{
			GeneralHeaders.TransferEncodingChunked = value;
		}
	}

	public HttpHeaderValueCollection<ProductHeaderValue> Upgrade => GeneralHeaders.Upgrade;

	public HttpHeaderValueCollection<ViaHeaderValue> Via => GeneralHeaders.Via;

	public HttpHeaderValueCollection<WarningHeaderValue> Warning => GeneralHeaders.Warning;

	private HttpGeneralHeaders GeneralHeaders => _generalHeaders ?? (_generalHeaders = new HttpGeneralHeaders(this));

	private T GetSpecializedCollection<T>(int slot, Func<HttpResponseHeaders, T> creationFunc)
	{
		object[] array = _specialCollectionsSlots ?? (_specialCollectionsSlots = new object[5]);
		object obj = array[slot];
		if (obj == null)
		{
			obj = (array[slot] = creationFunc(this));
		}
		return (T)obj;
	}

	internal HttpResponseHeaders(bool containsTrailingHeaders = false)
		: base(containsTrailingHeaders ? (HttpHeaderType.General | HttpHeaderType.Response | HttpHeaderType.Content | HttpHeaderType.Custom | HttpHeaderType.NonTrailing) : (HttpHeaderType.General | HttpHeaderType.Response | HttpHeaderType.Custom), HttpHeaderType.Request)
	{
		_containsTrailingHeaders = containsTrailingHeaders;
	}

	internal override void AddHeaders(HttpHeaders sourceHeaders)
	{
		base.AddHeaders(sourceHeaders);
		HttpResponseHeaders httpResponseHeaders = sourceHeaders as HttpResponseHeaders;
		if (httpResponseHeaders._generalHeaders != null)
		{
			GeneralHeaders.AddSpecialsFrom(httpResponseHeaders._generalHeaders);
		}
	}

	internal override bool IsAllowedHeaderName(HeaderDescriptor descriptor)
	{
		if (!_containsTrailingHeaders)
		{
			return true;
		}
		KnownHeader knownHeader = KnownHeaders.TryGetKnownHeader(descriptor.Name);
		if (knownHeader == null)
		{
			return true;
		}
		return (knownHeader.HeaderType & HttpHeaderType.NonTrailing) == 0;
	}
}
