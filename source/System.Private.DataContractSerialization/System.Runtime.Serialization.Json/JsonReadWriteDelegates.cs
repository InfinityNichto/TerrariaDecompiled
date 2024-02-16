using System.Collections.Generic;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonReadWriteDelegates
{
	private static readonly Dictionary<DataContract, JsonReadWriteDelegates> s_jsonDelegates = new Dictionary<DataContract, JsonReadWriteDelegates>();

	public JsonFormatClassWriterDelegate ClassWriterDelegate { get; set; }

	public JsonFormatClassReaderDelegate ClassReaderDelegate { get; set; }

	public JsonFormatCollectionWriterDelegate CollectionWriterDelegate { get; set; }

	public JsonFormatCollectionReaderDelegate CollectionReaderDelegate { get; set; }

	public JsonFormatGetOnlyCollectionReaderDelegate GetOnlyCollectionReaderDelegate { get; set; }

	public static Dictionary<DataContract, JsonReadWriteDelegates> GetJsonDelegates()
	{
		return s_jsonDelegates;
	}
}
