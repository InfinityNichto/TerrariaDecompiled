namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class JsonSerializableAttribute : JsonAttribute
{
	public string? TypeInfoPropertyName { get; set; }

	public JsonSourceGenerationMode GenerationMode { get; set; }

	public JsonSerializableAttribute(Type type)
	{
	}
}
