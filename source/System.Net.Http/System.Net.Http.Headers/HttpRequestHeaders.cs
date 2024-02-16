namespace System.Net.Http.Headers;

public sealed class HttpRequestHeaders : HttpHeaders
{
	private object[] _specialCollectionsSlots;

	private HttpGeneralHeaders _generalHeaders;

	private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> _expect;

	private bool _expectContinueSet;

	public HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue> Accept => GetSpecializedCollection(0, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<MediaTypeWithQualityHeaderValue>(KnownHeaders.Accept.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptCharset => GetSpecializedCollection(1, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptCharset.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptEncoding => GetSpecializedCollection(2, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptEncoding.Descriptor, thisRef));

	public HttpHeaderValueCollection<StringWithQualityHeaderValue> AcceptLanguage => GetSpecializedCollection(3, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<StringWithQualityHeaderValue>(KnownHeaders.AcceptLanguage.Descriptor, thisRef));

	public AuthenticationHeaderValue? Authorization
	{
		get
		{
			return (AuthenticationHeaderValue)GetParsedValues(KnownHeaders.Authorization.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Authorization.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<NameValueWithParametersHeaderValue> Expect => ExpectCore;

	public bool? ExpectContinue
	{
		get
		{
			if (_expectContinueSet || ContainsParsedValue(KnownHeaders.Expect.Descriptor, HeaderUtilities.ExpectContinue))
			{
				if (ExpectCore.IsSpecialValueSet)
				{
					return true;
				}
				if (_expectContinueSet)
				{
					return false;
				}
			}
			return null;
		}
		set
		{
			if (value == true)
			{
				_expectContinueSet = true;
				ExpectCore.SetSpecialValue();
			}
			else
			{
				_expectContinueSet = value.HasValue;
				ExpectCore.RemoveSpecialValue();
			}
		}
	}

	public string? From
	{
		get
		{
			return (string)GetParsedValues(KnownHeaders.From.Descriptor);
		}
		set
		{
			if (value == string.Empty)
			{
				value = null;
			}
			HttpHeaders.CheckContainsNewLine(value);
			SetOrRemoveParsedValue(KnownHeaders.From.Descriptor, value);
		}
	}

	public string? Host
	{
		get
		{
			return (string)GetParsedValues(KnownHeaders.Host.Descriptor);
		}
		set
		{
			if (value == string.Empty)
			{
				value = null;
			}
			if (value != null && HttpRuleParser.GetHostLength(value, 0, allowToken: false, out var _) != value.Length)
			{
				throw new FormatException(System.SR.net_http_headers_invalid_host_header);
			}
			SetOrRemoveParsedValue(KnownHeaders.Host.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<EntityTagHeaderValue> IfMatch => GetSpecializedCollection(4, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfMatch.Descriptor, thisRef));

	public DateTimeOffset? IfModifiedSince
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfModifiedSince.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfModifiedSince.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<EntityTagHeaderValue> IfNoneMatch => GetSpecializedCollection(5, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<EntityTagHeaderValue>(KnownHeaders.IfNoneMatch.Descriptor, thisRef));

	public RangeConditionHeaderValue? IfRange
	{
		get
		{
			return (RangeConditionHeaderValue)GetParsedValues(KnownHeaders.IfRange.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfRange.Descriptor, value);
		}
	}

	public DateTimeOffset? IfUnmodifiedSince
	{
		get
		{
			return HeaderUtilities.GetDateTimeOffsetValue(KnownHeaders.IfUnmodifiedSince.Descriptor, this);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.IfUnmodifiedSince.Descriptor, value);
		}
	}

	public int? MaxForwards
	{
		get
		{
			object parsedValues = GetParsedValues(KnownHeaders.MaxForwards.Descriptor);
			if (parsedValues != null)
			{
				return (int)parsedValues;
			}
			return null;
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.MaxForwards.Descriptor, value);
		}
	}

	public AuthenticationHeaderValue? ProxyAuthorization
	{
		get
		{
			return (AuthenticationHeaderValue)GetParsedValues(KnownHeaders.ProxyAuthorization.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.ProxyAuthorization.Descriptor, value);
		}
	}

	public RangeHeaderValue? Range
	{
		get
		{
			return (RangeHeaderValue)GetParsedValues(KnownHeaders.Range.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Range.Descriptor, value);
		}
	}

	public Uri? Referrer
	{
		get
		{
			return (Uri)GetParsedValues(KnownHeaders.Referer.Descriptor);
		}
		set
		{
			SetOrRemoveParsedValue(KnownHeaders.Referer.Descriptor, value);
		}
	}

	public HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue> TE => GetSpecializedCollection(6, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<TransferCodingWithQualityHeaderValue>(KnownHeaders.TE.Descriptor, thisRef));

	public HttpHeaderValueCollection<ProductInfoHeaderValue> UserAgent => GetSpecializedCollection(7, (HttpRequestHeaders thisRef) => new HttpHeaderValueCollection<ProductInfoHeaderValue>(KnownHeaders.UserAgent.Descriptor, thisRef));

	private HttpHeaderValueCollection<NameValueWithParametersHeaderValue> ExpectCore => _expect ?? (_expect = new HttpHeaderValueCollection<NameValueWithParametersHeaderValue>(KnownHeaders.Expect.Descriptor, this, HeaderUtilities.ExpectContinue));

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

	private T GetSpecializedCollection<T>(int slot, Func<HttpRequestHeaders, T> creationFunc)
	{
		if (_specialCollectionsSlots == null)
		{
			_specialCollectionsSlots = new object[8];
		}
		object[] specialCollectionsSlots = _specialCollectionsSlots;
		return (T)(specialCollectionsSlots[slot] ?? (specialCollectionsSlots[slot] = creationFunc(this)));
	}

	internal HttpRequestHeaders()
		: base(HttpHeaderType.General | HttpHeaderType.Request | HttpHeaderType.Custom, HttpHeaderType.Response)
	{
	}

	internal override void AddHeaders(HttpHeaders sourceHeaders)
	{
		base.AddHeaders(sourceHeaders);
		HttpRequestHeaders httpRequestHeaders = sourceHeaders as HttpRequestHeaders;
		if (httpRequestHeaders._generalHeaders != null)
		{
			GeneralHeaders.AddSpecialsFrom(httpRequestHeaders._generalHeaders);
		}
		if (!ExpectContinue.HasValue)
		{
			ExpectContinue = httpRequestHeaders.ExpectContinue;
		}
	}
}
