namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class PollingPayloadType
{
	public CounterPayload Payload { get; set; }

	public PollingPayloadType(CounterPayload payload)
	{
		Payload = payload;
	}
}
