using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Text;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class EncoderFallbackException : ArgumentException
{
	private readonly char _charUnknown;

	private readonly char _charUnknownHigh;

	private readonly char _charUnknownLow;

	private readonly int _index;

	public char CharUnknown => _charUnknown;

	public char CharUnknownHigh => _charUnknownHigh;

	public char CharUnknownLow => _charUnknownLow;

	public int Index => _index;

	public EncoderFallbackException()
		: base(SR.Arg_ArgumentException)
	{
		base.HResult = -2147024809;
	}

	public EncoderFallbackException(string? message)
		: base(message)
	{
		base.HResult = -2147024809;
	}

	public EncoderFallbackException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2147024809;
	}

	internal EncoderFallbackException(string message, char charUnknown, int index)
		: base(message)
	{
		_charUnknown = charUnknown;
		_index = index;
	}

	internal EncoderFallbackException(string message, char charUnknownHigh, char charUnknownLow, int index)
		: base(message)
	{
		if (!char.IsHighSurrogate(charUnknownHigh))
		{
			throw new ArgumentOutOfRangeException("charUnknownHigh", SR.Format(SR.ArgumentOutOfRange_Range, 55296, 56319));
		}
		if (!char.IsLowSurrogate(charUnknownLow))
		{
			throw new ArgumentOutOfRangeException("CharUnknownLow", SR.Format(SR.ArgumentOutOfRange_Range, 56320, 57343));
		}
		_charUnknownHigh = charUnknownHigh;
		_charUnknownLow = charUnknownLow;
		_index = index;
	}

	private EncoderFallbackException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
	}

	public bool IsUnknownSurrogate()
	{
		return _charUnknownHigh != '\0';
	}
}
