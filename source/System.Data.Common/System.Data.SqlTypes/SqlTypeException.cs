using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data.SqlTypes;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class SqlTypeException : SystemException
{
	public SqlTypeException()
		: this(System.SR.SqlMisc_SqlTypeMessage, null)
	{
	}

	public SqlTypeException(string? message)
		: this(message, null)
	{
	}

	public SqlTypeException(string? message, Exception? e)
		: base(message, e)
	{
		base.HResult = -2146232016;
	}

	protected SqlTypeException(SerializationInfo si, StreamingContext sc)
		: base(SqlTypeExceptionSerialization(si, sc), sc)
	{
	}

	private static SerializationInfo SqlTypeExceptionSerialization(SerializationInfo si, StreamingContext sc)
	{
		if (si != null && 1 == si.MemberCount)
		{
			string @string = si.GetString("SqlTypeExceptionMessage");
			SqlTypeException ex = new SqlTypeException(@string);
			ex.GetObjectData(si, sc);
		}
		return si;
	}
}
