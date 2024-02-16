namespace System.Net.Http;

public readonly struct HttpRequestOptionsKey<TValue>
{
	public string Key { get; }

	public HttpRequestOptionsKey(string key)
	{
		Key = key;
	}
}
