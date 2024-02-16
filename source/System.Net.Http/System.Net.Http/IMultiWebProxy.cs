namespace System.Net.Http;

internal interface IMultiWebProxy : IWebProxy
{
	MultiProxy GetMultiProxy(Uri uri);
}
