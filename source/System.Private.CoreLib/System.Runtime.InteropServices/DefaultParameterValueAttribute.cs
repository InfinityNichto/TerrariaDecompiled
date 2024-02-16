namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DefaultParameterValueAttribute : Attribute
{
	public object? Value { get; }

	public DefaultParameterValueAttribute(object? value)
	{
		Value = value;
	}
}
