namespace System.Diagnostics.Tracing;

internal sealed class EtwEventProvider : IEventProvider
{
	unsafe uint IEventProvider.EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle)
	{
		Guid providerId = eventSource.Guid;
		return Interop.Advapi32.EventRegister(in providerId, enableCallback, callbackContext, ref registrationHandle);
	}

	uint IEventProvider.EventUnregister(long registrationHandle)
	{
		return Interop.Advapi32.EventUnregister(registrationHandle);
	}

	unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(long registrationHandle, in EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		switch (Interop.Advapi32.EventWriteTransfer(registrationHandle, in eventDescriptor, activityId, relatedActivityId, userDataCount, userData))
		{
		case 234:
		case 534:
			return EventProvider.WriteEventErrorCode.EventTooBig;
		case 8:
			return EventProvider.WriteEventErrorCode.NoFreeBuffers;
		default:
			return EventProvider.WriteEventErrorCode.NoError;
		}
	}

	unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		throw new NotSupportedException();
	}
}
