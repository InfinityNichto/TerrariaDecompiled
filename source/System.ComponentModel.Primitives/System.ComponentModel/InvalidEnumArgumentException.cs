using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.ComponentModel;

[Serializable]
[TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidEnumArgumentException : ArgumentException
{
	public InvalidEnumArgumentException()
		: this(null)
	{
	}

	public InvalidEnumArgumentException(string? message)
		: base(message)
	{
	}

	public InvalidEnumArgumentException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	public InvalidEnumArgumentException(string? argumentName, int invalidValue, Type enumClass)
		: base(System.SR.Format(System.SR.InvalidEnumArgument, argumentName, invalidValue, enumClass?.Name), argumentName)
	{
		if (enumClass == null)
		{
			throw new ArgumentNullException("enumClass");
		}
	}

	protected InvalidEnumArgumentException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
