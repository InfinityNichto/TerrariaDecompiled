namespace System.Net.Http;

internal enum Http2ProtocolErrorCode
{
	NoError,
	ProtocolError,
	InternalError,
	FlowControlError,
	SettingsTimeout,
	StreamClosed,
	FrameSizeError,
	RefusedStream,
	Cancel,
	CompressionError,
	ConnectError,
	EnhanceYourCalm,
	InadequateSecurity,
	Http11Required
}
