using System.Runtime.Serialization;

namespace System.Threading.Channels;

[Serializable]
public class ChannelClosedException : InvalidOperationException
{
	public ChannelClosedException()
		: base(System.SR.ChannelClosedException_DefaultMessage)
	{
	}

	public ChannelClosedException(string? message)
		: base(message)
	{
	}

	public ChannelClosedException(Exception? innerException)
		: base(System.SR.ChannelClosedException_DefaultMessage, innerException)
	{
	}

	public ChannelClosedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected ChannelClosedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
