using System.Runtime.Serialization;

namespace System.Net.Http;

[Serializable]
internal sealed class Http2StreamException : Http2ProtocolException
{
	public Http2StreamException(Http2ProtocolErrorCode protocolError)
		: base(System.SR.Format(System.SR.net_http_http2_stream_error, Http2ProtocolException.GetName(protocolError), ((int)protocolError).ToString("x")), protocolError)
	{
	}

	private Http2StreamException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
