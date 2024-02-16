namespace System.Diagnostics.Tracing;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EventAttribute : Attribute
{
	private EventOpcode m_opcode;

	private bool m_opcodeSet;

	public int EventId { get; private set; }

	public EventLevel Level { get; set; }

	public EventKeywords Keywords { get; set; }

	public EventOpcode Opcode
	{
		get
		{
			return m_opcode;
		}
		set
		{
			m_opcode = value;
			m_opcodeSet = true;
		}
	}

	internal bool IsOpcodeSet => m_opcodeSet;

	public EventTask Task { get; set; }

	public EventChannel Channel { get; set; }

	public byte Version { get; set; }

	public string? Message { get; set; }

	public EventTags Tags { get; set; }

	public EventActivityOptions ActivityOptions { get; set; }

	public EventAttribute(int eventId)
	{
		EventId = eventId;
		Level = EventLevel.Informational;
	}
}
