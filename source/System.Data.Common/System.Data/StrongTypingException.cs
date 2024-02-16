using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class StrongTypingException : DataException
{
	protected StrongTypingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public StrongTypingException()
	{
		base.HResult = -2146232021;
	}

	public StrongTypingException(string? message)
		: base(message)
	{
		base.HResult = -2146232021;
	}

	public StrongTypingException(string? s, Exception? innerException)
		: base(s, innerException)
	{
		base.HResult = -2146232021;
	}
}
