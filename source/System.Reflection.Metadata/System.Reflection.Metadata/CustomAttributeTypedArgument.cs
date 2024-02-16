namespace System.Reflection.Metadata;

public readonly struct CustomAttributeTypedArgument<TType>
{
	public TType Type { get; }

	public object? Value { get; }

	public CustomAttributeTypedArgument(TType type, object? value)
	{
		Type = type;
		Value = value;
	}
}
