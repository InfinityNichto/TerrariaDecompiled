using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Text;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class DecoderFallbackException : ArgumentException
{
	private readonly byte[] _bytesUnknown;

	private readonly int _index;

	public byte[]? BytesUnknown => _bytesUnknown;

	public int Index => _index;

	public DecoderFallbackException()
		: base(SR.Arg_ArgumentException)
	{
		base.HResult = -2147024809;
	}

	public DecoderFallbackException(string? message)
		: base(message)
	{
		base.HResult = -2147024809;
	}

	public DecoderFallbackException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024809;
	}

	public DecoderFallbackException(string? message, byte[]? bytesUnknown, int index)
		: base(message)
	{
		_bytesUnknown = bytesUnknown;
		_index = index;
	}

	private DecoderFallbackException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}
}
