using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidExpressionException : DataException
{
	protected InvalidExpressionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public InvalidExpressionException()
	{
	}

	public InvalidExpressionException(string? s)
		: base(s)
	{
	}

	public InvalidExpressionException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
