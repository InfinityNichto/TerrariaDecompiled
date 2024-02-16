namespace Microsoft.Xna.Framework;

internal enum ManagedCallType
{
	NoManagedCall = 1,
	RunUnitTestDelegate,
	AsyncOperationCompleted,
	Media_ActiveSongChanged,
	Media_PlayStateChanged,
	Net_WriteLeaderboards,
	System_DeviceChanged,
	System_DownloadCompleted,
	CaptureBufferReady,
	PlaybackBufferNeeded
}
