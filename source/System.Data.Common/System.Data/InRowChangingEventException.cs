using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class InRowChangingEventException : DataException
{
	protected InRowChangingEventException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public InRowChangingEventException()
		: base(System.SR.DataSet_DefaultInRowChangingEventException)
	{
		base.HResult = -2146232029;
	}

	public InRowChangingEventException(string? s)
		: base(s)
	{
		base.HResult = -2146232029;
	}

	public InRowChangingEventException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232029;
	}
}
