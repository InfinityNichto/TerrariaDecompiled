namespace System.Net.Http;

internal enum Http3ErrorCode : long
{
	NoError = 256L,
	ProtocolError,
	InternalError,
	StreamCreationError,
	ClosedCriticalStream,
	UnexpectedFrame,
	FrameError,
	ExcessiveLoad,
	IdError,
	SettingsError,
	MissingSettings,
	RequestRejected,
	RequestCancelled,
	RequestIncomplete,
	MessageError,
	ConnectError,
	VersionFallback
}
