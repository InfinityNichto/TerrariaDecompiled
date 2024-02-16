using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Net.Http;

[Serializable]
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class Http3ConnectionException : Http3ProtocolException
{
	public Http3ConnectionException(Http3ErrorCode errorCode)
		: base(System.SR.Format(System.SR.net_http_http3_connection_error, Http3ProtocolException.GetName(errorCode), ((long)errorCode).ToString("x")), errorCode)
	{
	}

	private Http3ConnectionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
