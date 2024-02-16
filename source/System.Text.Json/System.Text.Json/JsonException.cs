using System.Runtime.Serialization;

namespace System.Text.Json;

[Serializable]
public class JsonException : Exception
{
	internal string _message;

	internal bool AppendPathInformation { get; set; }

	public long? LineNumber { get; internal set; }

	public long? BytePositionInLine { get; internal set; }

	public string? Path { get; internal set; }

	public override string Message => _message ?? base.Message;

	public JsonException(string? message, string? path, long? lineNumber, long? bytePositionInLine, Exception? innerException)
		: base(message, innerException)
	{
		_message = message;
		LineNumber = lineNumber;
		BytePositionInLine = bytePositionInLine;
		Path = path;
	}

	public JsonException(string? message, string? path, long? lineNumber, long? bytePositionInLine)
		: base(message)
	{
		_message = message;
		LineNumber = lineNumber;
		BytePositionInLine = bytePositionInLine;
		Path = path;
	}

	public JsonException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		_message = message;
	}

	public JsonException(string? message)
		: base(message)
	{
		_message = message;
	}

	public JsonException()
	{
	}

	protected JsonException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		LineNumber = (long?)info.GetValue("LineNumber", typeof(long?));
		BytePositionInLine = (long?)info.GetValue("BytePositionInLine", typeof(long?));
		Path = info.GetString("Path");
		SetMessage(info.GetString("ActualMessage"));
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("LineNumber", LineNumber, typeof(long?));
		info.AddValue("BytePositionInLine", BytePositionInLine, typeof(long?));
		info.AddValue("Path", Path, typeof(string));
		info.AddValue("ActualMessage", Message, typeof(string));
	}

	internal void SetMessage(string message)
	{
		_message = message;
	}
}
