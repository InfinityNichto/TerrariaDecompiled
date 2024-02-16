using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class VersionNotFoundException : DataException
{
	protected VersionNotFoundException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public VersionNotFoundException()
		: base(System.SR.DataSet_DefaultVersionNotFoundException)
	{
		base.HResult = -2146232023;
	}

	public VersionNotFoundException(string? s)
		: base(s)
	{
		base.HResult = -2146232023;
	}

	public VersionNotFoundException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		base.HResult = -2146232023;
	}
}
