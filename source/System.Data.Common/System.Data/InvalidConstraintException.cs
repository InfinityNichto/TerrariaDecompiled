using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InvalidConstraintException : DataException
{
	protected InvalidConstraintException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public InvalidConstraintException()
		: base(System.SR.DataSet_DefaultInvalidConstraintException)
	{
		base.HResult = -2146232028;
	}

	public InvalidConstraintException(string? s)
		: base(s)
	{
		base.HResult = -2146232028;
	}

	public InvalidConstraintException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232028;
	}
}
