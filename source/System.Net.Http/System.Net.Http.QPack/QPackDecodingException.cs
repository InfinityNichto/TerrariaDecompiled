using System.Runtime.Serialization;

namespace System.Net.Http.QPack;

[Serializable]
internal sealed class QPackDecodingException : Exception
{
	public QPackDecodingException(string message)
		: base(message)
	{
	}

	public QPackDecodingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	private QPackDecodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
