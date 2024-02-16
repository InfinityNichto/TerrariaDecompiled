using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Net;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CookieException : FormatException, ISerializable
{
	public CookieException()
	{
	}

	internal CookieException(string message)
		: base(message)
	{
	}

	internal CookieException(string message, Exception inner)
		: base(message, inner)
	{
	}

	protected CookieException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}

	public override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		base.GetObjectData(serializationInfo, streamingContext);
	}
}
