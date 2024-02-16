using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class EvaluateException : InvalidExpressionException
{
	protected EvaluateException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public EvaluateException()
	{
	}

	public EvaluateException(string? s)
		: base(s)
	{
	}

	public EvaluateException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}
}
