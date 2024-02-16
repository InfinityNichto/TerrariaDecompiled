namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class)]
public sealed class InitializationEventAttribute : Attribute
{
	public string EventName { get; }

	public InitializationEventAttribute(string eventName)
	{
		EventName = eventName;
	}
}
