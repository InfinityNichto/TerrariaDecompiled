using System.Runtime.Serialization;

namespace System.Formats.Asn1;

[Serializable]
public class AsnContentException : Exception
{
	public AsnContentException()
		: base(System.SR.ContentException_DefaultMessage)
	{
	}

	public AsnContentException(string? message)
		: base(message ?? System.SR.ContentException_DefaultMessage)
	{
	}

	public AsnContentException(string? message, Exception? inner)
		: base(message ?? System.SR.ContentException_DefaultMessage, inner)
	{
	}

	protected AsnContentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
