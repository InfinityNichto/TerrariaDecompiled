using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SyntaxErrorException : InvalidExpressionException
{
	protected SyntaxErrorException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public SyntaxErrorException()
	{
	}

	public SyntaxErrorException(string? s)
		: base(s)
	{
	}

	public SyntaxErrorException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
