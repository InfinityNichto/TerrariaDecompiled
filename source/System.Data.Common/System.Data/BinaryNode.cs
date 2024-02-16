using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

internal class BinaryNode : ExpressionNode
{
	private enum DataTypePrecedence
	{
		SqlDateTime = 25,
		DateTimeOffset = 24,
		DateTime = 23,
		TimeSpan = 20,
		SqlDouble = 19,
		Double = 18,
		SqlSingle = 17,
		Single = 16,
		SqlDecimal = 15,
		Decimal = 14,
		SqlMoney = 13,
		UInt64 = 12,
		SqlInt64 = 11,
		Int64 = 10,
		UInt32 = 9,
		SqlInt32 = 8,
		Int32 = 7,
		UInt16 = 6,
		SqlInt16 = 5,
		Int16 = 4,
		Byte = 3,
		SqlByte = 2,
		SByte = 1,
		Error = 0,
		SqlBoolean = -1,
		Boolean = -2,
		SqlGuid = -3,
		SqlString = -4,
		String = -5,
		SqlXml = -6,
		SqlChars = -7,
		Char = -8,
		SqlBytes = -9,
		SqlBinary = -10
	}

	internal int _op;

	internal ExpressionNode _left;

	internal ExpressionNode _right;

	internal BinaryNode(DataTable table, int op, ExpressionNode left, ExpressionNode right)
		: base(table)
	{
		_op = op;
		_left = left;
		_right = right;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
		_left.Bind(table, list);
		_right.Bind(table, list);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		return Eval(null, DataRowVersion.Default);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		return EvalBinaryOp(_op, _left, _right, row, version, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] recordNos)
	{
		return EvalBinaryOp(_op, _left, _right, null, DataRowVersion.Default, recordNos);
	}

	internal override bool IsConstant()
	{
		if (_left.IsConstant())
		{
			return _right.IsConstant();
		}
		return false;
	}

	internal override bool IsTableConstant()
	{
		if (_left.IsTableConstant())
		{
			return _right.IsTableConstant();
		}
		return false;
	}

	internal override bool HasLocalAggregate()
	{
		if (!_left.HasLocalAggregate())
		{
			return _right.HasLocalAggregate();
		}
		return true;
	}

	internal override bool HasRemoteAggregate()
	{
		if (!_left.HasRemoteAggregate())
		{
			return _right.HasRemoteAggregate();
		}
		return true;
	}

	internal override bool DependsOn(DataColumn column)
	{
		if (_left.DependsOn(column))
		{
			return true;
		}
		return _right.DependsOn(column);
	}

	internal override ExpressionNode Optimize()
	{
		_left = _left.Optimize();
		if (_op == 13)
		{
			if (_right is UnaryNode)
			{
				UnaryNode unaryNode = (UnaryNode)_right;
				if (unaryNode._op != 3)
				{
					throw ExprException.InvalidIsSyntax();
				}
				_op = 39;
				_right = unaryNode._right;
			}
			if (!(_right is ZeroOpNode))
			{
				throw ExprException.InvalidIsSyntax();
			}
			if (((ZeroOpNode)_right)._op != 32)
			{
				throw ExprException.InvalidIsSyntax();
			}
		}
		else
		{
			_right = _right.Optimize();
		}
		if (IsConstant())
		{
			object obj = EvalConstant();
			if (obj == DBNull.Value)
			{
				return new ZeroOpNode(32);
			}
			if (obj is bool)
			{
				if ((bool)obj)
				{
					return new ZeroOpNode(33);
				}
				return new ZeroOpNode(34);
			}
			return new ConstNode(base.table, ValueType.Object, obj, fParseQuotes: false);
		}
		return this;
	}

	internal void SetTypeMismatchError(int op, Type left, Type right)
	{
		throw ExprException.TypeMismatchInBinop(op, left, right);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Evaluating constant expression is safe.")]
	private object EvalConstant()
	{
		return Eval();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static object Eval(ExpressionNode expr, DataRow row, DataRowVersion version, int[] recordNos)
	{
		if (recordNos == null)
		{
			return expr.Eval(row, version);
		}
		return expr.Eval(recordNos);
	}

	internal int BinaryCompare(object vLeft, object vRight, StorageType resultType, int op)
	{
		return BinaryCompare(vLeft, vRight, resultType, op, null);
	}

	internal int BinaryCompare(object vLeft, object vRight, StorageType resultType, int op, CompareInfo comparer)
	{
		int result = 0;
		try
		{
			if (!DataStorage.IsSqlType(resultType))
			{
				switch (resultType)
				{
				case StorageType.SByte:
				case StorageType.Byte:
				case StorageType.Int16:
				case StorageType.UInt16:
				case StorageType.Int32:
					return Convert.ToInt32(vLeft, base.FormatProvider).CompareTo(Convert.ToInt32(vRight, base.FormatProvider));
				case StorageType.UInt32:
				case StorageType.Int64:
				case StorageType.UInt64:
				case StorageType.Decimal:
					return decimal.Compare(Convert.ToDecimal(vLeft, base.FormatProvider), Convert.ToDecimal(vRight, base.FormatProvider));
				case StorageType.Char:
					return Convert.ToInt32(vLeft, base.FormatProvider).CompareTo(Convert.ToInt32(vRight, base.FormatProvider));
				case StorageType.Double:
					return Convert.ToDouble(vLeft, base.FormatProvider).CompareTo(Convert.ToDouble(vRight, base.FormatProvider));
				case StorageType.Single:
					return Convert.ToSingle(vLeft, base.FormatProvider).CompareTo(Convert.ToSingle(vRight, base.FormatProvider));
				case StorageType.DateTime:
					return DateTime.Compare(Convert.ToDateTime(vLeft, base.FormatProvider), Convert.ToDateTime(vRight, base.FormatProvider));
				case StorageType.DateTimeOffset:
					return DateTimeOffset.Compare((DateTimeOffset)vLeft, (DateTimeOffset)vRight);
				case StorageType.String:
					return base.table.Compare(Convert.ToString(vLeft, base.FormatProvider), Convert.ToString(vRight, base.FormatProvider), comparer);
				case StorageType.Guid:
					return ((Guid)vLeft).CompareTo((Guid)vRight);
				case StorageType.Boolean:
					if (op == 7 || op == 12)
					{
						return Convert.ToInt32(DataExpression.ToBoolean(vLeft), base.FormatProvider) - Convert.ToInt32(DataExpression.ToBoolean(vRight), base.FormatProvider);
					}
					break;
				case StorageType.TimeSpan:
				case StorageType.ByteArray:
				case StorageType.CharArray:
				case StorageType.Type:
					break;
				}
			}
			else
			{
				switch (resultType)
				{
				case StorageType.SByte:
				case StorageType.Byte:
				case StorageType.Int16:
				case StorageType.UInt16:
				case StorageType.Int32:
				case StorageType.SqlByte:
				case StorageType.SqlInt16:
				case StorageType.SqlInt32:
					return SqlConvert.ConvertToSqlInt32(vLeft).CompareTo(SqlConvert.ConvertToSqlInt32(vRight));
				case StorageType.UInt32:
				case StorageType.Int64:
				case StorageType.SqlInt64:
					return SqlConvert.ConvertToSqlInt64(vLeft).CompareTo(SqlConvert.ConvertToSqlInt64(vRight));
				case StorageType.UInt64:
				case StorageType.SqlDecimal:
					return SqlConvert.ConvertToSqlDecimal(vLeft).CompareTo(SqlConvert.ConvertToSqlDecimal(vRight));
				case StorageType.SqlDouble:
					return SqlConvert.ConvertToSqlDouble(vLeft).CompareTo(SqlConvert.ConvertToSqlDouble(vRight));
				case StorageType.SqlSingle:
					return SqlConvert.ConvertToSqlSingle(vLeft).CompareTo(SqlConvert.ConvertToSqlSingle(vRight));
				case StorageType.SqlString:
					return base.table.Compare(vLeft.ToString(), vRight.ToString());
				case StorageType.SqlGuid:
					return ((SqlGuid)vLeft).CompareTo(vRight);
				case StorageType.SqlBoolean:
					if (op == 7 || op == 12)
					{
						result = 1;
						if ((vLeft.GetType() == typeof(SqlBoolean) && (vRight.GetType() == typeof(SqlBoolean) || vRight.GetType() == typeof(bool))) || (vRight.GetType() == typeof(SqlBoolean) && (vLeft.GetType() == typeof(SqlBoolean) || vLeft.GetType() == typeof(bool))))
						{
							return SqlConvert.ConvertToSqlBoolean(vLeft).CompareTo(SqlConvert.ConvertToSqlBoolean(vRight));
						}
					}
					break;
				case StorageType.SqlBinary:
					return SqlConvert.ConvertToSqlBinary(vLeft).CompareTo(SqlConvert.ConvertToSqlBinary(vRight));
				case StorageType.SqlDateTime:
					return SqlConvert.ConvertToSqlDateTime(vLeft).CompareTo(SqlConvert.ConvertToSqlDateTime(vRight));
				case StorageType.SqlMoney:
					return SqlConvert.ConvertToSqlMoney(vLeft).CompareTo(SqlConvert.ConvertToSqlMoney(vRight));
				case StorageType.Single:
				case StorageType.Double:
				case StorageType.Decimal:
				case StorageType.DateTime:
				case StorageType.TimeSpan:
				case StorageType.String:
				case StorageType.Guid:
				case StorageType.ByteArray:
				case StorageType.CharArray:
				case StorageType.Type:
				case StorageType.DateTimeOffset:
				case StorageType.BigInteger:
				case StorageType.Uri:
				case StorageType.SqlBytes:
				case StorageType.SqlChars:
					break;
				}
			}
		}
		catch (ArgumentException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
		}
		catch (FormatException e2)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e2);
		}
		catch (InvalidCastException e3)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e3);
		}
		catch (OverflowException e4)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e4);
		}
		catch (EvaluateException e5)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e5);
		}
		SetTypeMismatchError(op, vLeft.GetType(), vRight.GetType());
		return result;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private object EvalBinaryOp(int op, ExpressionNode left, ExpressionNode right, DataRow row, DataRowVersion version, int[] recordNos)
	{
		object obj2;
		StorageType storageType3;
		object obj;
		if (op != 27 && op != 26 && op != 5 && op != 13 && op != 39)
		{
			obj = Eval(left, row, version, recordNos);
			obj2 = Eval(right, row, version, recordNos);
			Type type = obj.GetType();
			Type type2 = obj2.GetType();
			StorageType storageType = DataStorage.GetStorageType(type);
			StorageType storageType2 = DataStorage.GetStorageType(type2);
			bool flag = DataStorage.IsSqlType(storageType);
			bool flag2 = DataStorage.IsSqlType(storageType2);
			if (flag && DataStorage.IsObjectSqlNull(obj))
			{
				return obj;
			}
			if (flag2 && DataStorage.IsObjectSqlNull(obj2))
			{
				return obj2;
			}
			if (obj == DBNull.Value || obj2 == DBNull.Value)
			{
				return DBNull.Value;
			}
			storageType3 = ((!(flag || flag2)) ? ResultType(storageType, storageType2, left is ConstNode, right is ConstNode, op) : ResultSqlType(storageType, storageType2, left is ConstNode, right is ConstNode, op));
			if (storageType3 == StorageType.Empty)
			{
				SetTypeMismatchError(op, type, type2);
			}
		}
		else
		{
			obj = (obj2 = DBNull.Value);
			storageType3 = StorageType.Empty;
		}
		object result = DBNull.Value;
		bool flag3 = false;
		try
		{
			switch (op)
			{
			case 15:
				switch (storageType3)
				{
				case StorageType.Byte:
					result = Convert.ToByte(Convert.ToByte(obj, base.FormatProvider) + Convert.ToByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SByte:
					result = Convert.ToSByte(Convert.ToSByte(obj, base.FormatProvider) + Convert.ToSByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int16:
					result = Convert.ToInt16(Convert.ToInt16(obj, base.FormatProvider) + Convert.ToInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.UInt16:
					result = Convert.ToUInt16(Convert.ToUInt16(obj, base.FormatProvider) + Convert.ToUInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int32:
					result = checked(Convert.ToInt32(obj, base.FormatProvider) + Convert.ToInt32(obj2, base.FormatProvider));
					break;
				case StorageType.UInt32:
					result = checked(Convert.ToUInt32(obj, base.FormatProvider) + Convert.ToUInt32(obj2, base.FormatProvider));
					break;
				case StorageType.UInt64:
					result = checked(Convert.ToUInt64(obj, base.FormatProvider) + Convert.ToUInt64(obj2, base.FormatProvider));
					break;
				case StorageType.Int64:
					result = checked(Convert.ToInt64(obj, base.FormatProvider) + Convert.ToInt64(obj2, base.FormatProvider));
					break;
				case StorageType.Decimal:
					result = Convert.ToDecimal(obj, base.FormatProvider) + Convert.ToDecimal(obj2, base.FormatProvider);
					break;
				case StorageType.Single:
					result = Convert.ToSingle(obj, base.FormatProvider) + Convert.ToSingle(obj2, base.FormatProvider);
					break;
				case StorageType.Double:
					result = Convert.ToDouble(obj, base.FormatProvider) + Convert.ToDouble(obj2, base.FormatProvider);
					break;
				case StorageType.Char:
				case StorageType.String:
					result = Convert.ToString(obj, base.FormatProvider) + Convert.ToString(obj2, base.FormatProvider);
					break;
				case StorageType.DateTime:
					if (obj is TimeSpan && obj2 is DateTime)
					{
						result = (DateTime)obj2 + (TimeSpan)obj;
					}
					else if (obj is DateTime && obj2 is TimeSpan)
					{
						result = (DateTime)obj + (TimeSpan)obj2;
					}
					else
					{
						flag3 = true;
					}
					break;
				case StorageType.TimeSpan:
					result = (TimeSpan)obj + (TimeSpan)obj2;
					break;
				case StorageType.SqlInt16:
					result = SqlConvert.ConvertToSqlInt16(obj) + SqlConvert.ConvertToSqlInt16(obj2);
					break;
				case StorageType.SqlInt32:
					result = SqlConvert.ConvertToSqlInt32(obj) + SqlConvert.ConvertToSqlInt32(obj2);
					break;
				case StorageType.SqlInt64:
					result = SqlConvert.ConvertToSqlInt64(obj) + SqlConvert.ConvertToSqlInt64(obj2);
					break;
				case StorageType.SqlDouble:
					result = SqlConvert.ConvertToSqlDouble(obj) + SqlConvert.ConvertToSqlDouble(obj2);
					break;
				case StorageType.SqlSingle:
					result = SqlConvert.ConvertToSqlSingle(obj) + SqlConvert.ConvertToSqlSingle(obj2);
					break;
				case StorageType.SqlDecimal:
					result = SqlConvert.ConvertToSqlDecimal(obj) + SqlConvert.ConvertToSqlDecimal(obj2);
					break;
				case StorageType.SqlMoney:
					result = SqlConvert.ConvertToSqlMoney(obj) + SqlConvert.ConvertToSqlMoney(obj2);
					break;
				case StorageType.SqlByte:
					result = SqlConvert.ConvertToSqlByte(obj) + SqlConvert.ConvertToSqlByte(obj2);
					break;
				case StorageType.SqlString:
					result = SqlConvert.ConvertToSqlString(obj) + SqlConvert.ConvertToSqlString(obj2);
					break;
				case StorageType.SqlDateTime:
					if (obj is TimeSpan && obj2 is SqlDateTime)
					{
						result = SqlConvert.ConvertToSqlDateTime(SqlConvert.ConvertToSqlDateTime(obj2).Value + (TimeSpan)obj);
					}
					else if (obj is SqlDateTime && obj2 is TimeSpan)
					{
						result = SqlConvert.ConvertToSqlDateTime(SqlConvert.ConvertToSqlDateTime(obj).Value + (TimeSpan)obj2);
					}
					else
					{
						flag3 = true;
					}
					break;
				default:
					flag3 = true;
					break;
				}
				break;
			case 16:
				switch (storageType3)
				{
				case StorageType.Byte:
					result = Convert.ToByte(Convert.ToByte(obj, base.FormatProvider) - Convert.ToByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlByte:
					result = SqlConvert.ConvertToSqlByte(obj) - SqlConvert.ConvertToSqlByte(obj2);
					break;
				case StorageType.SByte:
					result = Convert.ToSByte(Convert.ToSByte(obj, base.FormatProvider) - Convert.ToSByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int16:
					result = Convert.ToInt16(Convert.ToInt16(obj, base.FormatProvider) - Convert.ToInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlInt16:
					result = SqlConvert.ConvertToSqlInt16(obj) - SqlConvert.ConvertToSqlInt16(obj2);
					break;
				case StorageType.UInt16:
					result = Convert.ToUInt16(Convert.ToUInt16(obj, base.FormatProvider) - Convert.ToUInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int32:
					result = checked(Convert.ToInt32(obj, base.FormatProvider) - Convert.ToInt32(obj2, base.FormatProvider));
					break;
				case StorageType.SqlInt32:
					result = SqlConvert.ConvertToSqlInt32(obj) - SqlConvert.ConvertToSqlInt32(obj2);
					break;
				case StorageType.UInt32:
					result = checked(Convert.ToUInt32(obj, base.FormatProvider) - Convert.ToUInt32(obj2, base.FormatProvider));
					break;
				case StorageType.Int64:
					result = checked(Convert.ToInt64(obj, base.FormatProvider) - Convert.ToInt64(obj2, base.FormatProvider));
					break;
				case StorageType.SqlInt64:
					result = SqlConvert.ConvertToSqlInt64(obj) - SqlConvert.ConvertToSqlInt64(obj2);
					break;
				case StorageType.UInt64:
					result = checked(Convert.ToUInt64(obj, base.FormatProvider) - Convert.ToUInt64(obj2, base.FormatProvider));
					break;
				case StorageType.Decimal:
					result = Convert.ToDecimal(obj, base.FormatProvider) - Convert.ToDecimal(obj2, base.FormatProvider);
					break;
				case StorageType.SqlDecimal:
					result = SqlConvert.ConvertToSqlDecimal(obj) - SqlConvert.ConvertToSqlDecimal(obj2);
					break;
				case StorageType.Single:
					result = Convert.ToSingle(obj, base.FormatProvider) - Convert.ToSingle(obj2, base.FormatProvider);
					break;
				case StorageType.SqlSingle:
					result = SqlConvert.ConvertToSqlSingle(obj) - SqlConvert.ConvertToSqlSingle(obj2);
					break;
				case StorageType.Double:
					result = Convert.ToDouble(obj, base.FormatProvider) - Convert.ToDouble(obj2, base.FormatProvider);
					break;
				case StorageType.SqlDouble:
					result = SqlConvert.ConvertToSqlDouble(obj) - SqlConvert.ConvertToSqlDouble(obj2);
					break;
				case StorageType.SqlMoney:
					result = SqlConvert.ConvertToSqlMoney(obj) - SqlConvert.ConvertToSqlMoney(obj2);
					break;
				case StorageType.DateTime:
					result = (DateTime)obj - (TimeSpan)obj2;
					break;
				case StorageType.TimeSpan:
					result = ((!(obj is DateTime)) ? ((object)((TimeSpan)obj - (TimeSpan)obj2)) : ((object)((DateTime)obj - (DateTime)obj2)));
					break;
				case StorageType.SqlDateTime:
					if (obj is TimeSpan && obj2 is SqlDateTime)
					{
						result = SqlConvert.ConvertToSqlDateTime(SqlConvert.ConvertToSqlDateTime(obj2).Value - (TimeSpan)obj);
					}
					else if (obj is SqlDateTime && obj2 is TimeSpan)
					{
						result = SqlConvert.ConvertToSqlDateTime(SqlConvert.ConvertToSqlDateTime(obj).Value - (TimeSpan)obj2);
					}
					else
					{
						flag3 = true;
					}
					break;
				default:
					flag3 = true;
					break;
				}
				break;
			case 17:
				switch (storageType3)
				{
				case StorageType.Byte:
					result = Convert.ToByte(Convert.ToByte(obj, base.FormatProvider) * Convert.ToByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlByte:
					result = SqlConvert.ConvertToSqlByte(obj) * SqlConvert.ConvertToSqlByte(obj2);
					break;
				case StorageType.SByte:
					result = Convert.ToSByte(Convert.ToSByte(obj, base.FormatProvider) * Convert.ToSByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int16:
					result = Convert.ToInt16(Convert.ToInt16(obj, base.FormatProvider) * Convert.ToInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlInt16:
					result = SqlConvert.ConvertToSqlInt16(obj) * SqlConvert.ConvertToSqlInt16(obj2);
					break;
				case StorageType.UInt16:
					result = Convert.ToUInt16(Convert.ToUInt16(obj, base.FormatProvider) * Convert.ToUInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int32:
					result = checked(Convert.ToInt32(obj, base.FormatProvider) * Convert.ToInt32(obj2, base.FormatProvider));
					break;
				case StorageType.SqlInt32:
					result = SqlConvert.ConvertToSqlInt32(obj) * SqlConvert.ConvertToSqlInt32(obj2);
					break;
				case StorageType.UInt32:
					result = checked(Convert.ToUInt32(obj, base.FormatProvider) * Convert.ToUInt32(obj2, base.FormatProvider));
					break;
				case StorageType.Int64:
					result = checked(Convert.ToInt64(obj, base.FormatProvider) * Convert.ToInt64(obj2, base.FormatProvider));
					break;
				case StorageType.SqlInt64:
					result = SqlConvert.ConvertToSqlInt64(obj) * SqlConvert.ConvertToSqlInt64(obj2);
					break;
				case StorageType.UInt64:
					result = checked(Convert.ToUInt64(obj, base.FormatProvider) * Convert.ToUInt64(obj2, base.FormatProvider));
					break;
				case StorageType.Decimal:
					result = Convert.ToDecimal(obj, base.FormatProvider) * Convert.ToDecimal(obj2, base.FormatProvider);
					break;
				case StorageType.SqlDecimal:
					result = SqlConvert.ConvertToSqlDecimal(obj) * SqlConvert.ConvertToSqlDecimal(obj2);
					break;
				case StorageType.Single:
					result = Convert.ToSingle(obj, base.FormatProvider) * Convert.ToSingle(obj2, base.FormatProvider);
					break;
				case StorageType.SqlSingle:
					result = SqlConvert.ConvertToSqlSingle(obj) * SqlConvert.ConvertToSqlSingle(obj2);
					break;
				case StorageType.SqlMoney:
					result = SqlConvert.ConvertToSqlMoney(obj) * SqlConvert.ConvertToSqlMoney(obj2);
					break;
				case StorageType.Double:
					result = Convert.ToDouble(obj, base.FormatProvider) * Convert.ToDouble(obj2, base.FormatProvider);
					break;
				case StorageType.SqlDouble:
					result = SqlConvert.ConvertToSqlDouble(obj) * SqlConvert.ConvertToSqlDouble(obj2);
					break;
				default:
					flag3 = true;
					break;
				}
				break;
			case 18:
				switch (storageType3)
				{
				case StorageType.Byte:
					result = Convert.ToByte(Convert.ToByte(obj, base.FormatProvider) / Convert.ToByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlByte:
					result = SqlConvert.ConvertToSqlByte(obj) / SqlConvert.ConvertToSqlByte(obj2);
					break;
				case StorageType.SByte:
					result = Convert.ToSByte(Convert.ToSByte(obj, base.FormatProvider) / Convert.ToSByte(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int16:
					result = Convert.ToInt16(Convert.ToInt16(obj, base.FormatProvider) / Convert.ToInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.SqlInt16:
					result = SqlConvert.ConvertToSqlInt16(obj) / SqlConvert.ConvertToSqlInt16(obj2);
					break;
				case StorageType.UInt16:
					result = Convert.ToUInt16(Convert.ToUInt16(obj, base.FormatProvider) / Convert.ToUInt16(obj2, base.FormatProvider), base.FormatProvider);
					break;
				case StorageType.Int32:
					result = Convert.ToInt32(obj, base.FormatProvider) / Convert.ToInt32(obj2, base.FormatProvider);
					break;
				case StorageType.SqlInt32:
					result = SqlConvert.ConvertToSqlInt32(obj) / SqlConvert.ConvertToSqlInt32(obj2);
					break;
				case StorageType.UInt32:
					result = Convert.ToUInt32(obj, base.FormatProvider) / Convert.ToUInt32(obj2, base.FormatProvider);
					break;
				case StorageType.UInt64:
					result = Convert.ToUInt64(obj, base.FormatProvider) / Convert.ToUInt64(obj2, base.FormatProvider);
					break;
				case StorageType.Int64:
					result = Convert.ToInt64(obj, base.FormatProvider) / Convert.ToInt64(obj2, base.FormatProvider);
					break;
				case StorageType.SqlInt64:
					result = SqlConvert.ConvertToSqlInt64(obj) / SqlConvert.ConvertToSqlInt64(obj2);
					break;
				case StorageType.Decimal:
					result = Convert.ToDecimal(obj, base.FormatProvider) / Convert.ToDecimal(obj2, base.FormatProvider);
					break;
				case StorageType.SqlDecimal:
					result = SqlConvert.ConvertToSqlDecimal(obj) / SqlConvert.ConvertToSqlDecimal(obj2);
					break;
				case StorageType.Single:
					result = Convert.ToSingle(obj, base.FormatProvider) / Convert.ToSingle(obj2, base.FormatProvider);
					break;
				case StorageType.SqlSingle:
					result = SqlConvert.ConvertToSqlSingle(obj) / SqlConvert.ConvertToSqlSingle(obj2);
					break;
				case StorageType.SqlMoney:
					result = SqlConvert.ConvertToSqlMoney(obj) / SqlConvert.ConvertToSqlMoney(obj2);
					break;
				case StorageType.Double:
				{
					double num = Convert.ToDouble(obj2, base.FormatProvider);
					result = Convert.ToDouble(obj, base.FormatProvider) / num;
					break;
				}
				case StorageType.SqlDouble:
					result = SqlConvert.ConvertToSqlDouble(obj) / SqlConvert.ConvertToSqlDouble(obj2);
					break;
				default:
					flag3 = true;
					break;
				}
				break;
			case 7:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return BinaryCompare(obj, obj2, storageType3, 7) == 0;
			case 8:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return 0 < BinaryCompare(obj, obj2, storageType3, op);
			case 9:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return 0 > BinaryCompare(obj, obj2, storageType3, op);
			case 10:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return 0 <= BinaryCompare(obj, obj2, storageType3, op);
			case 11:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return 0 >= BinaryCompare(obj, obj2, storageType3, op);
			case 12:
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)) || obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				return BinaryCompare(obj, obj2, storageType3, op) != 0;
			case 13:
				obj = Eval(left, row, version, recordNos);
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
				{
					return true;
				}
				return false;
			case 39:
				obj = Eval(left, row, version, recordNos);
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
				{
					return false;
				}
				return true;
			case 26:
				obj = Eval(left, row, version, recordNos);
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
				{
					return DBNull.Value;
				}
				if (!(obj is bool) && !(obj is SqlBoolean))
				{
					obj2 = Eval(right, row, version, recordNos);
					flag3 = true;
					break;
				}
				if (obj is bool)
				{
					if (!(bool)obj)
					{
						result = false;
						break;
					}
				}
				else if (((SqlBoolean)obj).IsFalse)
				{
					result = false;
					break;
				}
				obj2 = Eval(right, row, version, recordNos);
				if (obj2 == DBNull.Value || (right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
				{
					return DBNull.Value;
				}
				if (obj2 is bool || obj2 is SqlBoolean)
				{
					result = ((!(obj2 is bool)) ? ((object)((SqlBoolean)obj2).IsTrue) : ((object)(bool)obj2));
				}
				else
				{
					flag3 = true;
				}
				break;
			case 27:
				obj = Eval(left, row, version, recordNos);
				if (obj != DBNull.Value && !DataStorage.IsObjectSqlNull(obj))
				{
					if (!(obj is bool) && !(obj is SqlBoolean))
					{
						obj2 = Eval(right, row, version, recordNos);
						flag3 = true;
						break;
					}
					if ((bool)obj)
					{
						result = true;
						break;
					}
				}
				obj2 = Eval(right, row, version, recordNos);
				if (obj2 == DBNull.Value || DataStorage.IsObjectSqlNull(obj2))
				{
					return obj;
				}
				if (obj == DBNull.Value || DataStorage.IsObjectSqlNull(obj))
				{
					return obj2;
				}
				if (!(obj2 is bool) && !(obj2 is SqlBoolean))
				{
					flag3 = true;
				}
				else
				{
					result = ((obj2 is bool) ? ((bool)obj2) : ((SqlBoolean)obj2).IsTrue);
				}
				break;
			case 20:
				if (ExpressionNode.IsIntegerSql(storageType3))
				{
					if (storageType3 == StorageType.UInt64)
					{
						result = Convert.ToUInt64(obj, base.FormatProvider) % Convert.ToUInt64(obj2, base.FormatProvider);
					}
					else if (DataStorage.IsSqlType(storageType3))
					{
						SqlInt64 sqlInt = SqlConvert.ConvertToSqlInt64(obj) % SqlConvert.ConvertToSqlInt64(obj2);
						result = storageType3 switch
						{
							StorageType.SqlInt32 => sqlInt.ToSqlInt32(), 
							StorageType.SqlInt16 => sqlInt.ToSqlInt16(), 
							StorageType.SqlByte => sqlInt.ToSqlByte(), 
							_ => sqlInt, 
						};
					}
					else
					{
						result = Convert.ToInt64(obj, base.FormatProvider) % Convert.ToInt64(obj2, base.FormatProvider);
						result = Convert.ChangeType(result, DataStorage.GetTypeStorage(storageType3), base.FormatProvider);
					}
				}
				else
				{
					flag3 = true;
				}
				break;
			case 5:
			{
				if (!(right is FunctionNode))
				{
					throw ExprException.InWithoutParentheses();
				}
				obj = Eval(left, row, version, recordNos);
				if (obj == DBNull.Value || (left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
				{
					return DBNull.Value;
				}
				result = false;
				FunctionNode functionNode = (FunctionNode)right;
				for (int i = 0; i < functionNode._argumentCount; i++)
				{
					obj2 = functionNode._arguments[i].Eval();
					if (obj2 != DBNull.Value && (!right.IsSqlColumn || !DataStorage.IsObjectSqlNull(obj2)))
					{
						storageType3 = DataStorage.GetStorageType(obj.GetType());
						if (BinaryCompare(obj, obj2, storageType3, 7) == 0)
						{
							result = true;
							break;
						}
					}
				}
				break;
			}
			default:
				throw ExprException.UnsupportedOperator(op);
			}
		}
		catch (OverflowException)
		{
			throw ExprException.Overflow(DataStorage.GetTypeStorage(storageType3));
		}
		if (flag3)
		{
			SetTypeMismatchError(op, obj.GetType(), obj2.GetType());
		}
		return result;
	}

	private DataTypePrecedence GetPrecedence(StorageType storageType)
	{
		return storageType switch
		{
			StorageType.Boolean => DataTypePrecedence.Boolean, 
			StorageType.Char => DataTypePrecedence.Char, 
			StorageType.SByte => DataTypePrecedence.SByte, 
			StorageType.Byte => DataTypePrecedence.Byte, 
			StorageType.Int16 => DataTypePrecedence.Int16, 
			StorageType.UInt16 => DataTypePrecedence.UInt16, 
			StorageType.Int32 => DataTypePrecedence.Int32, 
			StorageType.UInt32 => DataTypePrecedence.UInt32, 
			StorageType.Int64 => DataTypePrecedence.Int64, 
			StorageType.UInt64 => DataTypePrecedence.UInt64, 
			StorageType.Single => DataTypePrecedence.Single, 
			StorageType.Double => DataTypePrecedence.Double, 
			StorageType.Decimal => DataTypePrecedence.Decimal, 
			StorageType.DateTime => DataTypePrecedence.DateTime, 
			StorageType.DateTimeOffset => DataTypePrecedence.DateTimeOffset, 
			StorageType.TimeSpan => DataTypePrecedence.TimeSpan, 
			StorageType.String => DataTypePrecedence.String, 
			StorageType.SqlBinary => DataTypePrecedence.SqlBinary, 
			StorageType.SqlBoolean => DataTypePrecedence.SqlBoolean, 
			StorageType.SqlByte => DataTypePrecedence.SqlByte, 
			StorageType.SqlBytes => DataTypePrecedence.SqlBytes, 
			StorageType.SqlChars => DataTypePrecedence.SqlChars, 
			StorageType.SqlDateTime => DataTypePrecedence.SqlDateTime, 
			StorageType.SqlDecimal => DataTypePrecedence.SqlDecimal, 
			StorageType.SqlDouble => DataTypePrecedence.SqlDouble, 
			StorageType.SqlGuid => DataTypePrecedence.SqlGuid, 
			StorageType.SqlInt16 => DataTypePrecedence.SqlInt16, 
			StorageType.SqlInt32 => DataTypePrecedence.SqlInt32, 
			StorageType.SqlInt64 => DataTypePrecedence.SqlInt64, 
			StorageType.SqlMoney => DataTypePrecedence.SqlMoney, 
			StorageType.SqlSingle => DataTypePrecedence.SqlSingle, 
			StorageType.SqlString => DataTypePrecedence.SqlString, 
			_ => DataTypePrecedence.Error, 
		};
	}

	private static StorageType GetPrecedenceType(DataTypePrecedence code)
	{
		return code switch
		{
			DataTypePrecedence.SByte => StorageType.SByte, 
			DataTypePrecedence.Byte => StorageType.Byte, 
			DataTypePrecedence.Int16 => StorageType.Int16, 
			DataTypePrecedence.UInt16 => StorageType.UInt16, 
			DataTypePrecedence.Int32 => StorageType.Int32, 
			DataTypePrecedence.UInt32 => StorageType.UInt32, 
			DataTypePrecedence.Int64 => StorageType.Int64, 
			DataTypePrecedence.UInt64 => StorageType.UInt64, 
			DataTypePrecedence.Decimal => StorageType.Decimal, 
			DataTypePrecedence.Single => StorageType.Single, 
			DataTypePrecedence.Double => StorageType.Double, 
			DataTypePrecedence.Boolean => StorageType.Boolean, 
			DataTypePrecedence.String => StorageType.String, 
			DataTypePrecedence.Char => StorageType.Char, 
			DataTypePrecedence.DateTimeOffset => StorageType.DateTimeOffset, 
			DataTypePrecedence.DateTime => StorageType.DateTime, 
			DataTypePrecedence.TimeSpan => StorageType.TimeSpan, 
			DataTypePrecedence.SqlDateTime => StorageType.SqlDateTime, 
			DataTypePrecedence.SqlDouble => StorageType.SqlDouble, 
			DataTypePrecedence.SqlSingle => StorageType.SqlSingle, 
			DataTypePrecedence.SqlDecimal => StorageType.SqlDecimal, 
			DataTypePrecedence.SqlInt64 => StorageType.SqlInt64, 
			DataTypePrecedence.SqlInt32 => StorageType.SqlInt32, 
			DataTypePrecedence.SqlInt16 => StorageType.SqlInt16, 
			DataTypePrecedence.SqlByte => StorageType.SqlByte, 
			DataTypePrecedence.SqlBoolean => StorageType.SqlBoolean, 
			DataTypePrecedence.SqlString => StorageType.SqlString, 
			DataTypePrecedence.SqlGuid => StorageType.SqlGuid, 
			DataTypePrecedence.SqlBinary => StorageType.SqlBinary, 
			DataTypePrecedence.SqlMoney => StorageType.SqlMoney, 
			_ => StorageType.Empty, 
		};
	}

	private bool IsMixed(StorageType left, StorageType right)
	{
		if (!ExpressionNode.IsSigned(left) || !ExpressionNode.IsUnsigned(right))
		{
			if (ExpressionNode.IsUnsigned(left))
			{
				return ExpressionNode.IsSigned(right);
			}
			return false;
		}
		return true;
	}

	private bool IsMixedSql(StorageType left, StorageType right)
	{
		if (!ExpressionNode.IsSignedSql(left) || !ExpressionNode.IsUnsignedSql(right))
		{
			if (ExpressionNode.IsUnsignedSql(left))
			{
				return ExpressionNode.IsSignedSql(right);
			}
			return false;
		}
		return true;
	}

	internal StorageType ResultType(StorageType left, StorageType right, bool lc, bool rc, int op)
	{
		if (left == StorageType.Guid && right == StorageType.Guid && Operators.IsRelational(op))
		{
			return left;
		}
		if (left == StorageType.String && right == StorageType.Guid && Operators.IsRelational(op))
		{
			return left;
		}
		if (left == StorageType.Guid && right == StorageType.String && Operators.IsRelational(op))
		{
			return right;
		}
		int precedence = (int)GetPrecedence(left);
		if (precedence == 0)
		{
			return StorageType.Empty;
		}
		int precedence2 = (int)GetPrecedence(right);
		if (precedence2 == 0)
		{
			return StorageType.Empty;
		}
		if (Operators.IsLogical(op))
		{
			if (left == StorageType.Boolean && right == StorageType.Boolean)
			{
				return StorageType.Boolean;
			}
			return StorageType.Empty;
		}
		if (left == StorageType.DateTimeOffset || right == StorageType.DateTimeOffset)
		{
			if (Operators.IsRelational(op) && left == StorageType.DateTimeOffset && right == StorageType.DateTimeOffset)
			{
				return StorageType.DateTimeOffset;
			}
			return StorageType.Empty;
		}
		if (op == 15 && (left == StorageType.String || right == StorageType.String))
		{
			return StorageType.String;
		}
		DataTypePrecedence dataTypePrecedence = (DataTypePrecedence)Math.Max(precedence, precedence2);
		StorageType precedenceType = GetPrecedenceType(dataTypePrecedence);
		if (Operators.IsArithmetical(op) && precedenceType != StorageType.String && precedenceType != StorageType.Char)
		{
			if (!ExpressionNode.IsNumeric(left))
			{
				return StorageType.Empty;
			}
			if (!ExpressionNode.IsNumeric(right))
			{
				return StorageType.Empty;
			}
		}
		if (op == 18 && ExpressionNode.IsInteger(precedenceType))
		{
			return StorageType.Double;
		}
		if (IsMixed(left, right))
		{
			if (lc && !rc)
			{
				return right;
			}
			if (!lc && rc)
			{
				return left;
			}
			if (ExpressionNode.IsUnsigned(precedenceType))
			{
				if (dataTypePrecedence >= DataTypePrecedence.UInt64)
				{
					throw ExprException.AmbiguousBinop(op, DataStorage.GetTypeStorage(left), DataStorage.GetTypeStorage(right));
				}
				precedenceType = GetPrecedenceType(dataTypePrecedence + 1);
			}
		}
		return precedenceType;
	}

	internal StorageType ResultSqlType(StorageType left, StorageType right, bool lc, bool rc, int op)
	{
		int precedence = (int)GetPrecedence(left);
		if (precedence == 0)
		{
			return StorageType.Empty;
		}
		int precedence2 = (int)GetPrecedence(right);
		if (precedence2 == 0)
		{
			return StorageType.Empty;
		}
		if (Operators.IsLogical(op))
		{
			if ((left != StorageType.Boolean && left != StorageType.SqlBoolean) || (right != StorageType.Boolean && right != StorageType.SqlBoolean))
			{
				return StorageType.Empty;
			}
			if (left == StorageType.Boolean && right == StorageType.Boolean)
			{
				return StorageType.Boolean;
			}
			return StorageType.SqlBoolean;
		}
		if (op == 15)
		{
			if (left == StorageType.SqlString || right == StorageType.SqlString)
			{
				return StorageType.SqlString;
			}
			if (left == StorageType.String || right == StorageType.String)
			{
				return StorageType.String;
			}
		}
		if ((left == StorageType.SqlBinary && right != StorageType.SqlBinary) || (left != StorageType.SqlBinary && right == StorageType.SqlBinary))
		{
			return StorageType.Empty;
		}
		if ((left == StorageType.SqlGuid && right != StorageType.SqlGuid) || (left != StorageType.SqlGuid && right == StorageType.SqlGuid))
		{
			return StorageType.Empty;
		}
		if (precedence > 19 && precedence2 < 20)
		{
			return StorageType.Empty;
		}
		if (precedence < 20 && precedence2 > 19)
		{
			return StorageType.Empty;
		}
		if (precedence > 19)
		{
			if (op == 15 || op == 16)
			{
				if (left == StorageType.TimeSpan)
				{
					return right;
				}
				if (right == StorageType.TimeSpan)
				{
					return left;
				}
				return StorageType.Empty;
			}
			if (!Operators.IsRelational(op))
			{
				return StorageType.Empty;
			}
			return left;
		}
		DataTypePrecedence dataTypePrecedence = (DataTypePrecedence)Math.Max(precedence, precedence2);
		StorageType precedenceType = GetPrecedenceType(dataTypePrecedence);
		precedenceType = GetPrecedenceType((DataTypePrecedence)SqlResultType((int)dataTypePrecedence));
		if (Operators.IsArithmetical(op) && precedenceType != StorageType.String && precedenceType != StorageType.Char && precedenceType != StorageType.SqlString)
		{
			if (!ExpressionNode.IsNumericSql(left))
			{
				return StorageType.Empty;
			}
			if (!ExpressionNode.IsNumericSql(right))
			{
				return StorageType.Empty;
			}
		}
		if (op == 18 && ExpressionNode.IsIntegerSql(precedenceType))
		{
			return StorageType.SqlDouble;
		}
		if (precedenceType == StorageType.SqlMoney && left != StorageType.SqlMoney && right != StorageType.SqlMoney)
		{
			precedenceType = StorageType.SqlDecimal;
		}
		if (IsMixedSql(left, right) && ExpressionNode.IsUnsignedSql(precedenceType))
		{
			if (dataTypePrecedence >= DataTypePrecedence.UInt64)
			{
				throw ExprException.AmbiguousBinop(op, DataStorage.GetTypeStorage(left), DataStorage.GetTypeStorage(right));
			}
			precedenceType = GetPrecedenceType(dataTypePrecedence + 1);
		}
		return precedenceType;
	}

	private int SqlResultType(int typeCode)
	{
		switch (typeCode)
		{
		case 23:
			return 24;
		case 20:
			return 21;
		case 18:
			return 19;
		case 16:
			return 17;
		case 14:
			return 15;
		case 12:
			return 13;
		case 9:
		case 10:
			return 11;
		case 6:
		case 7:
			return 8;
		case 3:
		case 4:
			return 5;
		case 1:
			return 2;
		case -2:
			return -1;
		case -5:
			return -4;
		case -8:
			return -7;
		default:
			return typeCode;
		}
	}
}
