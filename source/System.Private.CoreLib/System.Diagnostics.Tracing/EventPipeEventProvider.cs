namespace System.Diagnostics.Tracing;

internal sealed class EventPipeEventProvider : IEventProvider
{
	private IntPtr m_provHandle = IntPtr.Zero;

	unsafe uint IEventProvider.EventRegister(EventSource eventSource, Interop.Advapi32.EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle)
	{
		uint result = 0u;
		m_provHandle = EventPipeInternal.CreateProvider(eventSource.Name, enableCallback);
		if (m_provHandle != IntPtr.Zero)
		{
			registrationHandle = 1L;
		}
		else
		{
			result = 1u;
		}
		return result;
	}

	uint IEventProvider.EventUnregister(long registrationHandle)
	{
		EventPipeInternal.DeleteProvider(m_provHandle);
		return 0u;
	}

	unsafe EventProvider.WriteEventErrorCode IEventProvider.EventWriteTransfer(long registrationHandle, in EventDescriptor eventDescriptor, IntPtr eventHandle, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
	{
		if (eventHandle != IntPtr.Zero)
		{
			if (userDataCount == 0)
			{
				EventPipeInternal.WriteEventData(eventHandle, null, 0u, activityId, relatedActivityId);
				return EventProvider.WriteEventErrorCode.NoError;
			}
			if (eventDescriptor.Channel == 11)
			{
				userData += 3;
				userDataCount -= 3;
			}
			EventPipeInternal.WriteEventData(eventHandle, userData, (uint)userDataCount, activityId, relatedActivityId);
		}
		return EventProvider.WriteEventErrorCode.NoError;
	}

	unsafe IntPtr IEventProvider.DefineEventHandle(uint eventID, string eventName, long keywords, uint eventVersion, uint level, byte* pMetadata, uint metadataLength)
	{
		return EventPipeInternal.DefineEvent(m_provHandle, eventID, keywords, eventVersion, level, pMetadata, metadataLength);
	}

	internal static int EventActivityIdControl(Interop.Advapi32.ActivityControl ControlCode, ref Guid ActivityId)
	{
		return EventPipeInternal.EventActivityIdControl((uint)ControlCode, ref ActivityId);
	}
}
