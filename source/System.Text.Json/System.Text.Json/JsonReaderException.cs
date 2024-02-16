using System.Runtime.Serialization;

namespace System.Text.Json;

[Serializable]
internal sealed class JsonReaderException : JsonException
{
	public JsonReaderException(string message, long lineNumber, long bytePositionInLine)
		: base(message, null, lineNumber, bytePositionInLine)
	{
	}

	private JsonReaderException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
