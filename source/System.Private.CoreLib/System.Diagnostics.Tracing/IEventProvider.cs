namespace System.Diagnostics.Tracing;

internal interface IEventProvider
{
	unsafe uint EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle);

	uint EventUnregister(long registrationHandle);

	unsafe EventProvider.WriteEventErrorCode EventWriteTransfer(long registrationHandle, in EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData);

	unsafe IntPtr DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength);
}
