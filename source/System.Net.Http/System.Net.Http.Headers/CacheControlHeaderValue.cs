using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers;

public class CacheControlHeaderValue : ICloneable
{
	private static readonly HttpHeaderParser s_nameValueListParser = GenericHeaderParser.MultipleValueNameValueParser;

	private static readonly Action<string> s_checkIsValidToken = CheckIsValidToken;

	private bool _noCache;

	private ObjectCollection<string> _noCacheHeaders;

	private bool _noStore;

	private TimeSpan? _maxAge;

	private TimeSpan? _sharedMaxAge;

	private bool _maxStale;

	private TimeSpan? _maxStaleLimit;

	private TimeSpan? _minFresh;

	private bool _noTransform;

	private bool _onlyIfCached;

	private bool _publicField;

	private bool _privateField;

	private ObjectCollection<string> _privateHeaders;

	private bool _mustRevalidate;

	private bool _proxyRevalidate;

	private ObjectCollection<NameValueHeaderValue> _extensions;

	public bool NoCache
	{
		get
		{
			return _noCache;
		}
		set
		{
			_noCache = value;
		}
	}

	public ICollection<string> NoCacheHeaders => _noCacheHeaders ?? (_noCacheHeaders = new ObjectCollection<string>(s_checkIsValidToken));

	public bool NoStore
	{
		get
		{
			return _noStore;
		}
		set
		{
			_noStore = value;
		}
	}

	public TimeSpan? MaxAge
	{
		get
		{
			return _maxAge;
		}
		set
		{
			_maxAge = value;
		}
	}

	public TimeSpan? SharedMaxAge
	{
		get
		{
			return _sharedMaxAge;
		}
		set
		{
			_sharedMaxAge = value;
		}
	}

	public bool MaxStale
	{
		get
		{
			return _maxStale;
		}
		set
		{
			_maxStale = value;
		}
	}

	public TimeSpan? MaxStaleLimit
	{
		get
		{
			return _maxStaleLimit;
		}
		set
		{
			_maxStaleLimit = value;
		}
	}

	public TimeSpan? MinFresh
	{
		get
		{
			return _minFresh;
		}
		set
		{
			_minFresh = value;
		}
	}

	public bool NoTransform
	{
		get
		{
			return _noTransform;
		}
		set
		{
			_noTransform = value;
		}
	}

	public bool OnlyIfCached
	{
		get
		{
			return _onlyIfCached;
		}
		set
		{
			_onlyIfCached = value;
		}
	}

	public bool Public
	{
		get
		{
			return _publicField;
		}
		set
		{
			_publicField = value;
		}
	}

	public bool Private
	{
		get
		{
			return _privateField;
		}
		set
		{
			_privateField = value;
		}
	}

	public ICollection<string> PrivateHeaders => _privateHeaders ?? (_privateHeaders = new ObjectCollection<string>(s_checkIsValidToken));

	public bool MustRevalidate
	{
		get
		{
			return _mustRevalidate;
		}
		set
		{
			_mustRevalidate = value;
		}
	}

	public bool ProxyRevalidate
	{
		get
		{
			return _proxyRevalidate;
		}
		set
		{
			_proxyRevalidate = value;
		}
	}

	public ICollection<NameValueHeaderValue> Extensions => _extensions ?? (_extensions = new ObjectCollection<NameValueHeaderValue>());

	public CacheControlHeaderValue()
	{
	}

	private CacheControlHeaderValue(CacheControlHeaderValue source)
	{
		_noCache = source._noCache;
		_noStore = source._noStore;
		_maxAge = source._maxAge;
		_sharedMaxAge = source._sharedMaxAge;
		_maxStale = source._maxStale;
		_maxStaleLimit = source._maxStaleLimit;
		_minFresh = source._minFresh;
		_noTransform = source._noTransform;
		_onlyIfCached = source._onlyIfCached;
		_publicField = source._publicField;
		_privateField = source._privateField;
		_mustRevalidate = source._mustRevalidate;
		_proxyRevalidate = source._proxyRevalidate;
		if (source._noCacheHeaders != null)
		{
			foreach (string noCacheHeader in source._noCacheHeaders)
			{
				NoCacheHeaders.Add(noCacheHeader);
			}
		}
		if (source._privateHeaders != null)
		{
			foreach (string privateHeader in source._privateHeaders)
			{
				PrivateHeaders.Add(privateHeader);
			}
		}
		_extensions = source._extensions.Clone();
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire();
		AppendValueIfRequired(stringBuilder, _noStore, "no-store");
		AppendValueIfRequired(stringBuilder, _noTransform, "no-transform");
		AppendValueIfRequired(stringBuilder, _onlyIfCached, "only-if-cached");
		AppendValueIfRequired(stringBuilder, _publicField, "public");
		AppendValueIfRequired(stringBuilder, _mustRevalidate, "must-revalidate");
		AppendValueIfRequired(stringBuilder, _proxyRevalidate, "proxy-revalidate");
		if (_noCache)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "no-cache");
			if (_noCacheHeaders != null && _noCacheHeaders.Count > 0)
			{
				stringBuilder.Append("=\"");
				AppendValues(stringBuilder, _noCacheHeaders);
				stringBuilder.Append('"');
			}
		}
		if (_maxAge.HasValue)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "max-age");
			stringBuilder.Append('=');
			int num = (int)_maxAge.GetValueOrDefault().TotalSeconds;
			if (num >= 0)
			{
				stringBuilder.Append(num);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler.AppendFormatted(num);
				stringBuilder3.Append(provider, ref handler);
			}
		}
		if (_sharedMaxAge.HasValue)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "s-maxage");
			stringBuilder.Append('=');
			int num2 = (int)_sharedMaxAge.GetValueOrDefault().TotalSeconds;
			if (num2 >= 0)
			{
				stringBuilder.Append(num2);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider2 = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler2.AppendFormatted(num2);
				stringBuilder4.Append(provider2, ref handler2);
			}
		}
		if (_maxStale)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "max-stale");
			if (_maxStaleLimit.HasValue)
			{
				stringBuilder.Append('=');
				int num3 = (int)_maxStaleLimit.GetValueOrDefault().TotalSeconds;
				if (num3 >= 0)
				{
					stringBuilder.Append(num3);
				}
				else
				{
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder5 = stringBuilder2;
					IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
					IFormatProvider provider3 = invariantInfo;
					StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
					handler3.AppendFormatted(num3);
					stringBuilder5.Append(provider3, ref handler3);
				}
			}
		}
		if (_minFresh.HasValue)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "min-fresh");
			stringBuilder.Append('=');
			int num4 = (int)_minFresh.GetValueOrDefault().TotalSeconds;
			if (num4 >= 0)
			{
				stringBuilder.Append(num4);
			}
			else
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
				IFormatProvider provider4 = invariantInfo;
				StringBuilder.AppendInterpolatedStringHandler handler4 = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2, invariantInfo);
				handler4.AppendFormatted(num4);
				stringBuilder6.Append(provider4, ref handler4);
			}
		}
		if (_privateField)
		{
			AppendValueWithSeparatorIfRequired(stringBuilder, "private");
			if (_privateHeaders != null && _privateHeaders.Count > 0)
			{
				stringBuilder.Append("=\"");
				AppendValues(stringBuilder, _privateHeaders);
				stringBuilder.Append('"');
			}
		}
		NameValueHeaderValue.ToString(_extensions, ',', leadingSeparator: false, stringBuilder);
		return System.Text.StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is CacheControlHeaderValue cacheControlHeaderValue))
		{
			return false;
		}
		if (_noCache == cacheControlHeaderValue._noCache && _noStore == cacheControlHeaderValue._noStore && !(_maxAge != cacheControlHeaderValue._maxAge))
		{
			TimeSpan? sharedMaxAge = _sharedMaxAge;
			TimeSpan? sharedMaxAge2 = cacheControlHeaderValue._sharedMaxAge;
			if (sharedMaxAge.HasValue == sharedMaxAge2.HasValue && (!sharedMaxAge.HasValue || !(sharedMaxAge.GetValueOrDefault() != sharedMaxAge2.GetValueOrDefault())) && _maxStale == cacheControlHeaderValue._maxStale && !(_maxStaleLimit != cacheControlHeaderValue._maxStaleLimit))
			{
				sharedMaxAge = _minFresh;
				sharedMaxAge2 = cacheControlHeaderValue._minFresh;
				if (sharedMaxAge.HasValue == sharedMaxAge2.HasValue && (!sharedMaxAge.HasValue || !(sharedMaxAge.GetValueOrDefault() != sharedMaxAge2.GetValueOrDefault())) && _noTransform == cacheControlHeaderValue._noTransform && _onlyIfCached == cacheControlHeaderValue._onlyIfCached && _publicField == cacheControlHeaderValue._publicField && _privateField == cacheControlHeaderValue._privateField && _mustRevalidate == cacheControlHeaderValue._mustRevalidate && _proxyRevalidate == cacheControlHeaderValue._proxyRevalidate)
				{
					if (!HeaderUtilities.AreEqualCollections(_noCacheHeaders, cacheControlHeaderValue._noCacheHeaders, StringComparer.OrdinalIgnoreCase))
					{
						return false;
					}
					if (!HeaderUtilities.AreEqualCollections(_privateHeaders, cacheControlHeaderValue._privateHeaders, StringComparer.OrdinalIgnoreCase))
					{
						return false;
					}
					if (!HeaderUtilities.AreEqualCollections(_extensions, cacheControlHeaderValue._extensions))
					{
						return false;
					}
					return true;
				}
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = _noCache.GetHashCode() ^ (_noStore.GetHashCode() << 1) ^ (_maxStale.GetHashCode() << 2) ^ (_noTransform.GetHashCode() << 3) ^ (_onlyIfCached.GetHashCode() << 4) ^ (_publicField.GetHashCode() << 5) ^ (_privateField.GetHashCode() << 6) ^ (_mustRevalidate.GetHashCode() << 7) ^ (_proxyRevalidate.GetHashCode() << 8);
		num = num ^ (_maxAge.HasValue ? (_maxAge.Value.GetHashCode() ^ 1) : 0) ^ (_sharedMaxAge.HasValue ? (_sharedMaxAge.Value.GetHashCode() ^ 2) : 0) ^ (_maxStaleLimit.HasValue ? (_maxStaleLimit.Value.GetHashCode() ^ 4) : 0) ^ (_minFresh.HasValue ? (_minFresh.Value.GetHashCode() ^ 8) : 0);
		if (_noCacheHeaders != null && _noCacheHeaders.Count > 0)
		{
			foreach (string noCacheHeader in _noCacheHeaders)
			{
				num ^= StringComparer.OrdinalIgnoreCase.GetHashCode(noCacheHeader);
			}
		}
		if (_privateHeaders != null && _privateHeaders.Count > 0)
		{
			foreach (string privateHeader in _privateHeaders)
			{
				num ^= StringComparer.OrdinalIgnoreCase.GetHashCode(privateHeader);
			}
		}
		if (_extensions != null && _extensions.Count > 0)
		{
			foreach (NameValueHeaderValue extension in _extensions)
			{
				num ^= extension.GetHashCode();
			}
		}
		return num;
	}

	public static CacheControlHeaderValue Parse(string? input)
	{
		int index = 0;
		return (CacheControlHeaderValue)CacheControlHeaderParser.Parser.ParseValue(input, null, ref index);
	}

	public static bool TryParse(string? input, out CacheControlHeaderValue? parsedValue)
	{
		int index = 0;
		parsedValue = null;
		if (CacheControlHeaderParser.Parser.TryParseValue(input, null, ref index, out var parsedValue2))
		{
			parsedValue = (CacheControlHeaderValue)parsedValue2;
			return true;
		}
		return false;
	}

	internal static int GetCacheControlLength(string input, int startIndex, CacheControlHeaderValue storeValue, out CacheControlHeaderValue parsedValue)
	{
		parsedValue = null;
		if (string.IsNullOrEmpty(input) || startIndex >= input.Length)
		{
			return 0;
		}
		int index = startIndex;
		List<NameValueHeaderValue> list = new List<NameValueHeaderValue>();
		while (index < input.Length)
		{
			if (!s_nameValueListParser.TryParseValue(input, null, ref index, out var parsedValue2))
			{
				return 0;
			}
			list.Add((NameValueHeaderValue)parsedValue2);
		}
		CacheControlHeaderValue cacheControlHeaderValue = storeValue;
		if (cacheControlHeaderValue == null)
		{
			cacheControlHeaderValue = new CacheControlHeaderValue();
		}
		if (!TrySetCacheControlValues(cacheControlHeaderValue, list))
		{
			return 0;
		}
		if (storeValue == null)
		{
			parsedValue = cacheControlHeaderValue;
		}
		return input.Length - startIndex;
	}

	private static bool TrySetCacheControlValues(CacheControlHeaderValue cc, List<NameValueHeaderValue> nameValueList)
	{
		foreach (NameValueHeaderValue nameValue in nameValueList)
		{
			bool flag = true;
			switch (nameValue.Name.ToLowerInvariant())
			{
			case "no-cache":
				flag = TrySetOptionalTokenList(nameValue, ref cc._noCache, ref cc._noCacheHeaders);
				break;
			case "no-store":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._noStore);
				break;
			case "max-age":
				flag = TrySetTimeSpan(nameValue, ref cc._maxAge);
				break;
			case "max-stale":
				flag = nameValue.Value == null || TrySetTimeSpan(nameValue, ref cc._maxStaleLimit);
				if (flag)
				{
					cc._maxStale = true;
				}
				break;
			case "min-fresh":
				flag = TrySetTimeSpan(nameValue, ref cc._minFresh);
				break;
			case "no-transform":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._noTransform);
				break;
			case "only-if-cached":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._onlyIfCached);
				break;
			case "public":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._publicField);
				break;
			case "private":
				flag = TrySetOptionalTokenList(nameValue, ref cc._privateField, ref cc._privateHeaders);
				break;
			case "must-revalidate":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._mustRevalidate);
				break;
			case "proxy-revalidate":
				flag = TrySetTokenOnlyValue(nameValue, ref cc._proxyRevalidate);
				break;
			case "s-maxage":
				flag = TrySetTimeSpan(nameValue, ref cc._sharedMaxAge);
				break;
			default:
				cc.Extensions.Add(nameValue);
				break;
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	private static bool TrySetTokenOnlyValue(NameValueHeaderValue nameValue, ref bool boolField)
	{
		if (nameValue.Value != null)
		{
			return false;
		}
		boolField = true;
		return true;
	}

	private static bool TrySetOptionalTokenList(NameValueHeaderValue nameValue, ref bool boolField, ref ObjectCollection<string> destination)
	{
		if (nameValue.Value == null)
		{
			boolField = true;
			return true;
		}
		string value = nameValue.Value;
		if (value.Length < 3 || value[0] != '"' || value[value.Length - 1] != '"')
		{
			return false;
		}
		int num = 1;
		int num2 = value.Length - 1;
		bool separatorFound = false;
		int num3 = ((destination != null) ? destination.Count : 0);
		while (num < num2)
		{
			num = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(value, num, skipEmptyValues: true, out separatorFound);
			if (num == num2)
			{
				break;
			}
			int tokenLength = HttpRuleParser.GetTokenLength(value, num);
			if (tokenLength == 0)
			{
				return false;
			}
			if (destination == null)
			{
				destination = new ObjectCollection<string>(s_checkIsValidToken);
			}
			destination.Add(value.Substring(num, tokenLength));
			num += tokenLength;
		}
		if (destination != null && destination.Count > num3)
		{
			boolField = true;
			return true;
		}
		return false;
	}

	private static bool TrySetTimeSpan(NameValueHeaderValue nameValue, ref TimeSpan? timeSpan)
	{
		if (nameValue.Value == null)
		{
			return false;
		}
		if (!HeaderUtilities.TryParseInt32(nameValue.Value, out var result))
		{
			return false;
		}
		timeSpan = new TimeSpan(0, 0, result);
		return true;
	}

	private static void AppendValueIfRequired(StringBuilder sb, bool appendValue, string value)
	{
		if (appendValue)
		{
			AppendValueWithSeparatorIfRequired(sb, value);
		}
	}

	private static void AppendValueWithSeparatorIfRequired(StringBuilder sb, string value)
	{
		if (sb.Length > 0)
		{
			sb.Append(", ");
		}
		sb.Append(value);
	}

	private static void AppendValues(StringBuilder sb, ObjectCollection<string> values)
	{
		bool flag = true;
		foreach (string value in values)
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				sb.Append(", ");
			}
			sb.Append(value);
		}
	}

	private static void CheckIsValidToken(string item)
	{
		HeaderUtilities.CheckValidToken(item, "item");
	}

	object ICloneable.Clone()
	{
		return new CacheControlHeaderValue(this);
	}
}
