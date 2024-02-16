namespace System.Diagnostics.Tracing;

internal sealed class NoOpEventProvider : IEventProvider
{
	unsafe uint IEventProvider.EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle)
	{
		return 0u;
	}

	uint IEventProvider.EventUnregister(long registrationHandle)
	{
		return 0u;
	}

	unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(long registrationHandle, in EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		return EventProvider.WriteEventErrorCode.NoError;
	}

	unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		return IntPtr.Zero;
	}
}
