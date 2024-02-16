using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class InvalidProgramException : SystemException
{
	public InvalidProgramException()
		: base(SR.InvalidProgram_Default)
	{
		base.HResult = -2146233030;
	}

	public InvalidProgramException(string? message)
		: base(message)
	{
		base.HResult = -2146233030;
	}

	public InvalidProgramException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233030;
	}

	private InvalidProgramException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
