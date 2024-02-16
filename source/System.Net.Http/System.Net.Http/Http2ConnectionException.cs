using System.Runtime.Serialization;

namespace System.Net.Http;

[Serializable]
internal sealed class Http2ConnectionException : Http2ProtocolException
{
	public Http2ConnectionException(Http2ProtocolErrorCode protocolError)
		: base(System.SR.Format(System.SR.net_http_http2_connection_error, Http2ProtocolException.GetName(protocolError), ((int)protocolError).ToString("x")), protocolError)
	{
	}

	private Http2ConnectionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
