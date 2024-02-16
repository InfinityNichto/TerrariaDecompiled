using System.Runtime.Serialization;

namespace System.Net.Http.QPack;

[Serializable]
internal sealed class QPackEncodingException : Exception
{
	public QPackEncodingException(string message)
		: base(message)
	{
	}

	private QPackEncodingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
