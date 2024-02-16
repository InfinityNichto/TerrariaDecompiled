namespace System.Text.Json.Serialization.Metadata;

public sealed class JsonParameterInfoValues
{
	public string Name { get; init; }

	public Type ParameterType { get; init; }

	public int Position { get; init; }

	public bool HasDefaultValue { get; init; }

	public object? DefaultValue { get; init; }
}
