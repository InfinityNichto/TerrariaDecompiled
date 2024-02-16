namespace System.Text.Json.Serialization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class JsonSourceGenerationOptionsAttribute : JsonAttribute
{
	public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

	public bool IgnoreReadOnlyFields { get; set; }

	public bool IgnoreReadOnlyProperties { get; set; }

	public bool IncludeFields { get; set; }

	public JsonKnownNamingPolicy PropertyNamingPolicy { get; set; }

	public bool WriteIndented { get; set; }

	public JsonSourceGenerationMode GenerationMode { get; set; }
}
