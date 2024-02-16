using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Data.Common;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class DbException : ExternalException
{
	public virtual bool IsTransient => false;

	public virtual string? SqlState => null;

	public DbBatchCommand? BatchCommand => DbBatchCommand;

	protected virtual DbBatchCommand? DbBatchCommand => null;

	protected DbException()
	{
	}

	protected DbException(string? message)
		: base(message)
	{
	}

	protected DbException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected DbException(string? message, int errorCode)
		: base(message, errorCode)
	{
	}

	protected DbException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
