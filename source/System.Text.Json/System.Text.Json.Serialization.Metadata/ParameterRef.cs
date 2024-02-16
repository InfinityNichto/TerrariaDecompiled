namespace System.Text.Json.Serialization.Metadata;

internal readonly struct ParameterRef
{
	public readonly ulong Key;

	public readonly JsonParameterInfo Info;

	public readonly byte[] NameFromJson;

	public ParameterRef(ulong key, JsonParameterInfo info, byte[] nameFromJson)
	{
		Key = key;
		Info = info;
		NameFromJson = nameFromJson;
	}
}
