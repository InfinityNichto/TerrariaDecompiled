namespace System.Text.Json.Serialization.Metadata;

internal readonly struct PropertyRef
{
	public readonly ulong Key;

	public readonly JsonPropertyInfo Info;

	public readonly byte[] NameFromJson;

	public PropertyRef(ulong key, JsonPropertyInfo info, byte[] nameFromJson)
	{
		Key = key;
		Info = info;
		NameFromJson = nameFromJson;
	}
}
