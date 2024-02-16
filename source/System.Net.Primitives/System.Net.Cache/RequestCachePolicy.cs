namespace System.Net.Cache;

public class RequestCachePolicy
{
	public RequestCacheLevel Level { get; }

	public RequestCachePolicy()
	{
		Level = RequestCacheLevel.Default;
	}

	public RequestCachePolicy(RequestCacheLevel level)
	{
		if (level < RequestCacheLevel.Default || level > RequestCacheLevel.NoCacheNoStore)
		{
			throw new ArgumentOutOfRangeException("level");
		}
		Level = level;
	}

	public override string ToString()
	{
		return "Level:" + Level;
	}
}
