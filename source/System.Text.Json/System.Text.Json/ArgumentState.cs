using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

internal sealed class ArgumentState
{
	public object Arguments;

	public (JsonPropertyInfo, JsonReaderState, long, byte[], string)[] FoundProperties;

	public (JsonPropertyInfo, object, string)[] FoundPropertiesAsync;

	public int FoundPropertyCount;

	public JsonParameterInfo JsonParameterInfo;

	public int ParameterIndex;

	public List<ParameterRef> ParameterRefCache;

	public bool FoundKey;

	public bool FoundValue;
}
