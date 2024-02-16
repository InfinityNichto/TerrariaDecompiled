using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class RowNotInTableException : DataException
{
	protected RowNotInTableException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public RowNotInTableException()
		: base(System.SR.DataSet_DefaultRowNotInTableException)
	{
		base.HResult = -2146232024;
	}

	public RowNotInTableException(string? s)
		: base(s)
	{
		base.HResult = -2146232024;
	}

	public RowNotInTableException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232024;
	}
}
