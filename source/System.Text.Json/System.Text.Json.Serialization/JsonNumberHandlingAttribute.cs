namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class JsonNumberHandlingAttribute : JsonAttribute
{
	public JsonNumberHandling Handling { get; }

	public JsonNumberHandlingAttribute(JsonNumberHandling handling)
	{
		if (!JsonSerializer.IsValidNumberHandlingValue(handling))
		{
			throw new ArgumentOutOfRangeException("handling");
		}
		Handling = handling;
	}
}
