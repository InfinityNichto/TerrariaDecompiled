using System.Text;

namespace System.Net.Http.HPack;

internal static class H2StaticTable
{
	private static readonly HeaderField[] s_staticDecoderTable = new HeaderField[61]
	{
		CreateHeaderField(":authority", ""),
		CreateHeaderField(":method", "GET"),
		CreateHeaderField(":method", "POST"),
		CreateHeaderField(":path", "/"),
		CreateHeaderField(":path", "/index.html"),
		CreateHeaderField(":scheme", "http"),
		CreateHeaderField(":scheme", "https"),
		CreateHeaderField(":status", "200"),
		CreateHeaderField(":status", "204"),
		CreateHeaderField(":status", "206"),
		CreateHeaderField(":status", "304"),
		CreateHeaderField(":status", "400"),
		CreateHeaderField(":status", "404"),
		CreateHeaderField(":status", "500"),
		CreateHeaderField("accept-charset", ""),
		CreateHeaderField("accept-encoding", "gzip, deflate"),
		CreateHeaderField("accept-language", ""),
		CreateHeaderField("accept-ranges", ""),
		CreateHeaderField("accept", ""),
		CreateHeaderField("access-control-allow-origin", ""),
		CreateHeaderField("age", ""),
		CreateHeaderField("allow", ""),
		CreateHeaderField("authorization", ""),
		CreateHeaderField("cache-control", ""),
		CreateHeaderField("content-disposition", ""),
		CreateHeaderField("content-encoding", ""),
		CreateHeaderField("content-language", ""),
		CreateHeaderField("content-length", ""),
		CreateHeaderField("content-location", ""),
		CreateHeaderField("content-range", ""),
		CreateHeaderField("content-type", ""),
		CreateHeaderField("cookie", ""),
		CreateHeaderField("date", ""),
		CreateHeaderField("etag", ""),
		CreateHeaderField("expect", ""),
		CreateHeaderField("expires", ""),
		CreateHeaderField("from", ""),
		CreateHeaderField("host", ""),
		CreateHeaderField("if-match", ""),
		CreateHeaderField("if-modified-since", ""),
		CreateHeaderField("if-none-match", ""),
		CreateHeaderField("if-range", ""),
		CreateHeaderField("if-unmodified-since", ""),
		CreateHeaderField("last-modified", ""),
		CreateHeaderField("link", ""),
		CreateHeaderField("location", ""),
		CreateHeaderField("max-forwards", ""),
		CreateHeaderField("proxy-authenticate", ""),
		CreateHeaderField("proxy-authorization", ""),
		CreateHeaderField("range", ""),
		CreateHeaderField("referer", ""),
		CreateHeaderField("refresh", ""),
		CreateHeaderField("retry-after", ""),
		CreateHeaderField("server", ""),
		CreateHeaderField("set-cookie", ""),
		CreateHeaderField("strict-transport-security", ""),
		CreateHeaderField("transfer-encoding", ""),
		CreateHeaderField("user-agent", ""),
		CreateHeaderField("vary", ""),
		CreateHeaderField("via", ""),
		CreateHeaderField("www-authenticate", "")
	};

	public static int Count => s_staticDecoderTable.Length;

	public static ref readonly HeaderField Get(int index)
	{
		return ref s_staticDecoderTable[index];
	}

	private static HeaderField CreateHeaderField(string name, string value)
	{
		return new HeaderField(Encoding.ASCII.GetBytes(name), (value.Length != 0) ? Encoding.ASCII.GetBytes(value) : Array.Empty<byte>());
	}
}
