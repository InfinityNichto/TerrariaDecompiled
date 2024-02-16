namespace System.Net.Http;

internal enum Http3SettingType : long
{
	QPackMaxTableCapacity = 1L,
	ReservedHttp2EnablePush,
	ReservedHttp2MaxConcurrentStreams,
	ReservedHttp2InitialWindowSize,
	ReservedHttp2MaxFrameSize,
	MaxHeaderListSize,
	QPackBlockedStreams
}
