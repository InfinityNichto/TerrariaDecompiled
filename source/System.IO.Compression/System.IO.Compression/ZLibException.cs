using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.IO.Compression;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ZLibException : IOException, ISerializable
{
	private readonly string _zlibErrorContext = string.Empty;

	private readonly string _zlibErrorMessage = string.Empty;

	private readonly ZLibNative.ErrorCode _zlibErrorCode;

	public ZLibException(string? message, string? zlibErrorContext, int zlibErrorCode, string? zlibErrorMessage)
		: base(message)
	{
		_zlibErrorContext = zlibErrorContext;
		_zlibErrorCode = (ZLibNative.ErrorCode)zlibErrorCode;
		_zlibErrorMessage = zlibErrorMessage;
	}

	public ZLibException()
	{
	}

	public ZLibException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected ZLibException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		_zlibErrorContext = info.GetString("zlibErrorContext");
		_zlibErrorCode = (ZLibNative.ErrorCode)info.GetInt32("zlibErrorCode");
		_zlibErrorMessage = info.GetString("zlibErrorMessage");
	}

	void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
	{
		base.GetObjectData(si, context);
		si.AddValue("zlibErrorContext", _zlibErrorContext);
		si.AddValue("zlibErrorCode", (int)_zlibErrorCode);
		si.AddValue("zlibErrorMessage", _zlibErrorMessage);
	}
}
