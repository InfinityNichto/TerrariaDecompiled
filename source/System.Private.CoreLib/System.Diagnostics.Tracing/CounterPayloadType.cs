namespace System.Diagnostics.Tracing;

[EventData]
internal sealed class CounterPayloadType
{
	public CounterPayload Payload { get; set; }

	public CounterPayloadType(CounterPayload payload)
	{
		Payload = payload;
	}
}
