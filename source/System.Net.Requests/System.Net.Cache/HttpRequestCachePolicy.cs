using System.Globalization;

namespace System.Net.Cache;

public class HttpRequestCachePolicy : RequestCachePolicy
{
	private readonly DateTime _lastSyncDateUtc = DateTime.MinValue;

	private readonly TimeSpan _maxAge = TimeSpan.MaxValue;

	private readonly TimeSpan _minFresh = TimeSpan.MinValue;

	private readonly TimeSpan _maxStale = TimeSpan.MinValue;

	public new HttpRequestCacheLevel Level { get; }

	public DateTime CacheSyncDate
	{
		get
		{
			if (!(_lastSyncDateUtc == DateTime.MinValue) && !(_lastSyncDateUtc == DateTime.MaxValue))
			{
				return _lastSyncDateUtc.ToLocalTime();
			}
			return _lastSyncDateUtc;
		}
	}

	public TimeSpan MaxAge => _maxAge;

	public TimeSpan MinFresh => _minFresh;

	public TimeSpan MaxStale => _maxStale;

	public HttpRequestCachePolicy()
		: this(HttpRequestCacheLevel.Default)
	{
	}

	public HttpRequestCachePolicy(HttpRequestCacheLevel level)
		: base(MapLevel(level))
	{
		Level = level;
	}

	public HttpRequestCachePolicy(HttpCacheAgeControl cacheAgeControl, TimeSpan ageOrFreshOrStale)
		: this(HttpRequestCacheLevel.Default)
	{
		switch (cacheAgeControl)
		{
		case HttpCacheAgeControl.MinFresh:
			_minFresh = ageOrFreshOrStale;
			break;
		case HttpCacheAgeControl.MaxAge:
			_maxAge = ageOrFreshOrStale;
			break;
		case HttpCacheAgeControl.MaxStale:
			_maxStale = ageOrFreshOrStale;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "HttpCacheAgeControl"), "cacheAgeControl");
		}
	}

	public HttpRequestCachePolicy(HttpCacheAgeControl cacheAgeControl, TimeSpan maxAge, TimeSpan freshOrStale)
		: this(HttpRequestCacheLevel.Default)
	{
		switch (cacheAgeControl)
		{
		case HttpCacheAgeControl.MinFresh:
			_minFresh = freshOrStale;
			break;
		case HttpCacheAgeControl.MaxAge:
			_maxAge = maxAge;
			break;
		case HttpCacheAgeControl.MaxStale:
			_maxStale = freshOrStale;
			break;
		case HttpCacheAgeControl.MaxAgeAndMinFresh:
			_maxAge = maxAge;
			_minFresh = freshOrStale;
			break;
		case HttpCacheAgeControl.MaxAgeAndMaxStale:
			_maxAge = maxAge;
			_maxStale = freshOrStale;
			break;
		default:
			throw new ArgumentException(System.SR.Format(System.SR.net_invalid_enum, "HttpCacheAgeControl"), "cacheAgeControl");
		}
	}

	public HttpRequestCachePolicy(DateTime cacheSyncDate)
		: this(HttpRequestCacheLevel.Default)
	{
		_lastSyncDateUtc = cacheSyncDate.ToUniversalTime();
	}

	public HttpRequestCachePolicy(HttpCacheAgeControl cacheAgeControl, TimeSpan maxAge, TimeSpan freshOrStale, DateTime cacheSyncDate)
		: this(cacheAgeControl, maxAge, freshOrStale)
	{
		_lastSyncDateUtc = cacheSyncDate.ToUniversalTime();
	}

	public override string ToString()
	{
		return "Level:" + Level.ToString() + ((_maxAge == TimeSpan.MaxValue) ? string.Empty : (" MaxAge:" + _maxAge)) + ((_minFresh == TimeSpan.MinValue) ? string.Empty : (" MinFresh:" + _minFresh)) + ((_maxStale == TimeSpan.MinValue) ? string.Empty : (" MaxStale:" + _maxStale)) + ((CacheSyncDate == DateTime.MinValue) ? string.Empty : (" CacheSyncDate:" + CacheSyncDate.ToString(CultureInfo.CurrentCulture)));
	}

	private static RequestCacheLevel MapLevel(HttpRequestCacheLevel level)
	{
		if (level <= HttpRequestCacheLevel.NoCacheNoStore)
		{
			return (RequestCacheLevel)level;
		}
		return level switch
		{
			HttpRequestCacheLevel.CacheOrNextCacheOnly => RequestCacheLevel.CacheOnly, 
			HttpRequestCacheLevel.Refresh => RequestCacheLevel.Reload, 
			_ => throw new ArgumentOutOfRangeException("level"), 
		};
	}
}
