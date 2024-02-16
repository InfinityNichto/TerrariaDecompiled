using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ConstraintException : DataException
{
	protected ConstraintException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public ConstraintException()
		: base(System.SR.DataSet_DefaultConstraintException)
	{
		base.HResult = -2146232022;
	}

	public ConstraintException(string? s)
		: base(s)
	{
		base.HResult = -2146232022;
	}

	public ConstraintException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232022;
	}
}
