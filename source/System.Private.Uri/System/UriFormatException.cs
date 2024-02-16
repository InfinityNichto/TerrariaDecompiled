using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class UriFormatException : FormatException, ISerializable
{
	public UriFormatException()
	{
	}

	public UriFormatException(string? textString)
		: base(textString)
	{
	}

	public UriFormatException(string? textString, Exception? e)
		: base(textString, e)
	{
	}

	protected UriFormatException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}
}
