using System.Runtime.Serialization;

namespace System.Net.Http.HPack;

[Serializable]
internal sealed class HPackDecodingException : Exception
{
	public HPackDecodingException(string message)
		: base(message)
	{
	}

	public HPackDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public HPackDecodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
