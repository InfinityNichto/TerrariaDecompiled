namespace System.Diagnostics.Tracing;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventSourceAttribute : Attribute
{
	public string? Name { get; set; }

	public string? Guid { get; set; }

	public string? LocalizationResources { get; set; }
}
