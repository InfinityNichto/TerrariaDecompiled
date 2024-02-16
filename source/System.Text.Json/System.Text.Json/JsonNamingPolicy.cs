namespace System.Text.Json;

public abstract class JsonNamingPolicy
{
	public static JsonNamingPolicy CamelCase { get; } = new JsonCamelCaseNamingPolicy();


	public abstract string ConvertName(string name);
}
