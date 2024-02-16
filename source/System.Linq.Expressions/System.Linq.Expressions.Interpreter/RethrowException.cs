using System.Runtime.Serialization;

namespace System.Linq.Expressions.Interpreter;

[Serializable]
internal sealed class RethrowException : Exception
{
	public RethrowException()
	{
	}

	internal RethrowException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
