using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class SqlNotFilledException : SqlTypeException
{
	public SqlNotFilledException()
		: this(SQLResource.NotFilledMessage, null)
	{
	}

	public SqlNotFilledException(string? message)
		: this(message, null)
	{
	}

	public SqlNotFilledException(string? message, Exception? e)
		: base(message, e)
	{
		base.HResult = -2146232015;
	}

	private SqlNotFilledException(SerializationInfo si, StreamingContext sc)
		: base(si, sc)
	{
	}
}
