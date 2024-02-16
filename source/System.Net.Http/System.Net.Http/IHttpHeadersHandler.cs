namespace System.Net.Http;

internal interface IHttpHeadersHandler
{
	void OnStaticIndexedHeader(int index);

	void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value);

	void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
}
