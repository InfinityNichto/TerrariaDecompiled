using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class ReadOnlyException : DataException
{
	protected ReadOnlyException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public ReadOnlyException()
		: base(System.SR.DataSet_DefaultReadOnlyException)
	{
		base.HResult = -2146232025;
	}

	public ReadOnlyException(string? s)
		: base(s)
	{
		base.HResult = -2146232025;
	}

	public ReadOnlyException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232025;
	}
}
