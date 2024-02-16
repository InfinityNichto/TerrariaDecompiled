namespace System.Net.Http;

internal enum Http3FrameType : long
{
	Data = 0L,
	Headers = 1L,
	ReservedHttp2Priority = 2L,
	CancelPush = 3L,
	Settings = 4L,
	PushPromise = 5L,
	ReservedHttp2Ping = 6L,
	GoAway = 7L,
	ReservedHttp2WindowUpdate = 8L,
	ReservedHttp2Continuation = 9L,
	MaxPushId = 13L
}
