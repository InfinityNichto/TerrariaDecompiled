namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class IncrementingEventCounterPayloadType
{
	public IncrementingCounterPayload Payload { get; set; }

	public IncrementingEventCounterPayloadType(IncrementingCounterPayload payload)
	{
		Payload = payload;
	}
}
