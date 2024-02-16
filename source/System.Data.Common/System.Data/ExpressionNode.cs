using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

internal abstract class ExpressionNode
{
	private DataTable _table;

	internal IFormatProvider FormatProvider
	{
		get
		{
			if (_table == null)
			{
				return CultureInfo.CurrentCulture;
			}
			return _table.FormatProvider;
		}
	}

	internal virtual bool IsSqlColumn => false;

	protected DataTable table => _table;

	protected ExpressionNode(DataTable table)
	{
		_table = table;
	}

	protected void BindTable(DataTable table)
	{
		_table = table;
	}

	internal abstract void Bind(DataTable table, List<DataColumn> list);

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal abstract object Eval();

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal abstract object Eval(DataRow row, DataRowVersion version);

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal abstract object Eval(int[] recordNos);

	internal abstract bool IsConstant();

	internal abstract bool IsTableConstant();

	internal abstract bool HasLocalAggregate();

	internal abstract bool HasRemoteAggregate();

	internal abstract ExpressionNode Optimize();

	internal virtual bool DependsOn(DataColumn column)
	{
		return false;
	}

	internal static bool IsInteger(StorageType type)
	{
		if (type != StorageType.Int16 && type != StorageType.Int32 && type != StorageType.Int64 && type != StorageType.UInt16 && type != StorageType.UInt32 && type != StorageType.UInt64 && type != StorageType.SByte)
		{
			return type == StorageType.Byte;
		}
		return true;
	}

	internal static bool IsIntegerSql(StorageType type)
	{
		if (type != StorageType.Int16 && type != StorageType.Int32 && type != StorageType.Int64 && type != StorageType.UInt16 && type != StorageType.UInt32 && type != StorageType.UInt64 && type != StorageType.SByte && type != StorageType.Byte && type != StorageType.SqlInt64 && type != StorageType.SqlInt32 && type != StorageType.SqlInt16)
		{
			return type == StorageType.SqlByte;
		}
		return true;
	}

	internal static bool IsSigned(StorageType type)
	{
		if (type != StorageType.Int16 && type != StorageType.Int32 && type != StorageType.Int64 && type != StorageType.SByte)
		{
			return IsFloat(type);
		}
		return true;
	}

	internal static bool IsSignedSql(StorageType type)
	{
		if (type != StorageType.Int16 && type != StorageType.Int32 && type != StorageType.Int64 && type != StorageType.SByte && type != StorageType.SqlInt64 && type != StorageType.SqlInt32 && type != StorageType.SqlInt16)
		{
			return IsFloatSql(type);
		}
		return true;
	}

	internal static bool IsUnsigned(StorageType type)
	{
		if (type != StorageType.UInt16 && type != StorageType.UInt32 && type != StorageType.UInt64)
		{
			return type == StorageType.Byte;
		}
		return true;
	}

	internal static bool IsUnsignedSql(StorageType type)
	{
		if (type != StorageType.UInt16 && type != StorageType.UInt32 && type != StorageType.UInt64 && type != StorageType.SqlByte)
		{
			return type == StorageType.Byte;
		}
		return true;
	}

	internal static bool IsNumeric(StorageType type)
	{
		if (!IsFloat(type))
		{
			return IsInteger(type);
		}
		return true;
	}

	internal static bool IsNumericSql(StorageType type)
	{
		if (!IsFloatSql(type))
		{
			return IsIntegerSql(type);
		}
		return true;
	}

	internal static bool IsFloat(StorageType type)
	{
		if (type != StorageType.Single && type != StorageType.Double)
		{
			return type == StorageType.Decimal;
		}
		return true;
	}

	internal static bool IsFloatSql(StorageType type)
	{
		if (type != StorageType.Single && type != StorageType.Double && type != StorageType.Decimal && type != StorageType.SqlDouble && type != StorageType.SqlDecimal && type != StorageType.SqlMoney)
		{
			return type == StorageType.SqlSingle;
		}
		return true;
	}
}
