using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Data;

[Serializable]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class DBConcurrencyException : SystemException
{
	private DataRow[] _dataRows;

	public DataRow? Row
	{
		get
		{
			DataRow[] dataRows = _dataRows;
			if (dataRows == null || dataRows.Length == 0)
			{
				return null;
			}
			return dataRows[0];
		}
		[param: DisallowNull]
		set
		{
			_dataRows = new DataRow[1] { value };
		}
	}

	public int RowCount
	{
		get
		{
			DataRow[] dataRows = _dataRows;
			if (dataRows == null)
			{
				return 0;
			}
			return dataRows.Length;
		}
	}

	public DBConcurrencyException()
		: this(System.SR.ADP_DBConcurrencyExceptionMessage, null)
	{
	}

	public DBConcurrencyException(string? message)
		: this(message, null)
	{
	}

	public DBConcurrencyException(string? message, Exception? inner)
		: base(message, inner)
	{
		base.HResult = -2146232011;
	}

	public DBConcurrencyException(string? message, Exception? inner, DataRow[]? dataRows)
		: base(message, inner)
	{
		base.HResult = -2146232011;
		_dataRows = dataRows;
	}

	private DBConcurrencyException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
	}

	public void CopyToRows(DataRow[] array)
	{
		CopyToRows(array, 0);
	}

	public void CopyToRows(DataRow[] array, int arrayIndex)
	{
		_dataRows?.CopyTo(array, arrayIndex);
	}
}
