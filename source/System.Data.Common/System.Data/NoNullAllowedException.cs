using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class NoNullAllowedException : DataException
{
	protected NoNullAllowedException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public NoNullAllowedException()
		: base(System.SR.DataSet_DefaultNoNullAllowedException)
	{
		base.HResult = -2146232026;
	}

	public NoNullAllowedException(string? s)
		: base(s)
	{
		base.HResult = -2146232026;
	}

	public NoNullAllowedException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232026;
	}
}
