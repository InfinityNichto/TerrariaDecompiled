using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class SqlNullValueException : SqlTypeException
{
	public SqlNullValueException()
		: this(SQLResource.NullValueMessage, null)
	{
	}

	public SqlNullValueException(string? message)
		: this(message, null)
	{
	}

	public SqlNullValueException(string? message, Exception? e)
		: base(message, e)
	{
		base.HResult = -2146232015;
	}

	private SqlNullValueException(SerializationInfo si, StreamingContext sc)
		: base(SqlNullValueExceptionSerialization(si, sc), sc)
	{
	}

	private static SerializationInfo SqlNullValueExceptionSerialization(SerializationInfo si, StreamingContext sc)
	{
		if (si != null && 1 == si.MemberCount)
		{
			string @string = si.GetString("SqlNullValueExceptionMessage");
			SqlNullValueException ex = new SqlNullValueException(@string);
			ex.GetObjectData(si, sc);
		}
		return si;
	}
}
