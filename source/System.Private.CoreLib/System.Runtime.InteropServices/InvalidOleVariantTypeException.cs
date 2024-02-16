using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidOleVariantTypeException : SystemException
{
	public InvalidOleVariantTypeException()
		: base(SR.Arg_InvalidOleVariantTypeException)
	{
		base.HResult = -2146233039;
	}

	public InvalidOleVariantTypeException(string? message)
		: base(message)
	{
		base.HResult = -2146233039;
	}

	public InvalidOleVariantTypeException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146233039;
	}

	protected InvalidOleVariantTypeException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
