namespace System.Diagnostics.Tracing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class EventDataAttribute : Attribute
{
	private EventLevel level = (EventLevel)(-1);

	private EventOpcode opcode = (EventOpcode)(-1);

	public string? Name { get; set; }

	internal EventLevel Level => level;

	internal EventOpcode Opcode => opcode;

	internal EventKeywords Keywords { get; }

	internal EventTags Tags { get; }
}
