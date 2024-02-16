using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class MissingPrimaryKeyException : DataException
{
	protected MissingPrimaryKeyException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public MissingPrimaryKeyException()
		: base(System.SR.DataSet_DefaultMissingPrimaryKeyException)
	{
		base.HResult = -2146232027;
	}

	public MissingPrimaryKeyException(string? s)
		: base(s)
	{
		base.HResult = -2146232027;
	}

	public MissingPrimaryKeyException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232027;
	}
}
