namespace System.Net.Cache;

public enum HttpCacheAgeControl
{
	None = 0,
	MinFresh = 1,
	MaxAge = 2,
	MaxStale = 4,
	MaxAgeAndMinFresh = 3,
	MaxAgeAndMaxStale = 6
}
