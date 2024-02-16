namespace System.Net;

internal static class HttpResponseHeaderExtensions
{
	private static readonly string[] s_names = new string[30]
	{
		"Cache-Control", "Connection", "Date", "Keep-Alive", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning",
		"Allow", "Content-Length", "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", "Content-MD5", "Content-Range", "Expires", "Last-Modified",
		"Accept-Ranges", "Age", "ETag", "Location", "Proxy-Authenticate", "Retry-After", "Server", "Set-Cookie", "Vary", "WWW-Authenticate"
	};

	public static string GetName(this HttpResponseHeader header)
	{
		return s_names[(int)header];
	}
}
