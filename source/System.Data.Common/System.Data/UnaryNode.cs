using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class UnaryNode : ExpressionNode
{
	internal readonly int _op;

	internal ExpressionNode _right;

	internal UnaryNode(DataTable table, int op, ExpressionNode right)
		: base(table)
	{
		_op = op;
		_right = right;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
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
		return EvalUnaryOp(_op, _right.Eval(row, version));
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] recordNos)
	{
		return _right.Eval(recordNos);
	}

	private object EvalUnaryOp(int op, object vl)
	{
		object value = DBNull.Value;
		if (DataExpression.IsUnknown(vl))
		{
			return DBNull.Value;
		}
		switch (op)
		{
		case 0:
			return vl;
		case 2:
		{
			StorageType storageType = DataStorage.GetStorageType(vl.GetType());
			if (ExpressionNode.IsNumericSql(storageType))
			{
				return vl;
			}
			throw ExprException.TypeMismatch(ToString());
		}
		case 1:
		{
			StorageType storageType = DataStorage.GetStorageType(vl.GetType());
			if (ExpressionNode.IsNumericSql(storageType))
			{
				return storageType switch
				{
					StorageType.Byte => -(byte)vl, 
					StorageType.Int16 => -(short)vl, 
					StorageType.Int32 => -(int)vl, 
					StorageType.Int64 => -(long)vl, 
					StorageType.Single => 0f - (float)vl, 
					StorageType.Double => 0.0 - (double)vl, 
					StorageType.Decimal => -(decimal)vl, 
					StorageType.SqlDecimal => -(SqlDecimal)vl, 
					StorageType.SqlDouble => -(SqlDouble)vl, 
					StorageType.SqlSingle => -(SqlSingle)vl, 
					StorageType.SqlMoney => -(SqlMoney)vl, 
					StorageType.SqlInt64 => -(SqlInt64)vl, 
					StorageType.SqlInt32 => -(SqlInt32)vl, 
					StorageType.SqlInt16 => -(SqlInt16)vl, 
					_ => DBNull.Value, 
				};
			}
			throw ExprException.TypeMismatch(ToString());
		}
		case 3:
			if (vl is SqlBoolean sqlBoolean)
			{
				if (sqlBoolean.IsFalse)
				{
					return SqlBoolean.True;
				}
				if (((SqlBoolean)vl).IsTrue)
				{
					return SqlBoolean.False;
				}
				throw ExprException.UnsupportedOperator(op);
			}
			if (DataExpression.ToBoolean(vl))
			{
				return false;
			}
			return true;
		default:
			throw ExprException.UnsupportedOperator(op);
		}
	}

	internal override bool IsConstant()
	{
		return _right.IsConstant();
	}

	internal override bool IsTableConstant()
	{
		return _right.IsTableConstant();
	}

	internal override bool HasLocalAggregate()
	{
		return _right.HasLocalAggregate();
	}

	internal override bool HasRemoteAggregate()
	{
		return _right.HasRemoteAggregate();
	}

	internal override bool DependsOn(DataColumn column)
	{
		return _right.DependsOn(column);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Evaluating constant expression is safe")]
	internal override ExpressionNode Optimize()
	{
		_right = _right.Optimize();
		if (IsConstant())
		{
			object constant = Eval();
			return new ConstNode(base.table, ValueType.Object, constant, fParseQuotes: false);
		}
		return this;
	}
}
