using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Reflection;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class CustomAttributeFormatException : FormatException
{
	public CustomAttributeFormatException()
		: this(SR.Arg_CustomAttributeFormatException)
	{
	}

	public CustomAttributeFormatException(string? message)
		: this(message, null)
	{
	}

	public CustomAttributeFormatException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232827;
	}

	protected CustomAttributeFormatException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
