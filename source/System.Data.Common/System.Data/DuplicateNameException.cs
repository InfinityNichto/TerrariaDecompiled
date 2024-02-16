using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class DuplicateNameException : DataException
{
	protected DuplicateNameException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public DuplicateNameException()
		: base(System.SR.DataSet_DefaultDuplicateNameException)
	{
		base.HResult = -2146232030;
	}

	public DuplicateNameException(string? s)
		: base(s)
	{
		base.HResult = -2146232030;
	}

	public DuplicateNameException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232030;
	}
}
