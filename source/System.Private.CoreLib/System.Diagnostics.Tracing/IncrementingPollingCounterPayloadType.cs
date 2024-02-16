namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class IncrementingPollingCounterPayloadType
{
	public IncrementingCounterPayload Payload { get; set; }

	public IncrementingPollingCounterPayloadType(IncrementingCounterPayload payload)
	{
		Payload = payload;
	}
}
