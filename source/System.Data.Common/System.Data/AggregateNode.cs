using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class AggregateNode : ExpressionNode
{
	private readonly AggregateType _type;

	private readonly Aggregate _aggregate;

	private readonly bool _local;

	private readonly string _relationName;

	private readonly string _columnName;

	private DataTable _childTable;

	private DataColumn _column;

	private DataRelation _relation;

	internal AggregateNode(DataTable table, FunctionId aggregateType, string columnName)
		: this(table, aggregateType, columnName, local: true, null)
	{
	}

	internal AggregateNode(DataTable table, FunctionId aggregateType, string columnName, bool local, string relationName)
		: base(table)
	{
		_aggregate = (Aggregate)aggregateType;
		switch (aggregateType)
		{
		case FunctionId.Sum:
			_type = AggregateType.Sum;
			break;
		case FunctionId.Avg:
			_type = AggregateType.Mean;
			break;
		case FunctionId.Min:
			_type = AggregateType.Min;
			break;
		case FunctionId.Max:
			_type = AggregateType.Max;
			break;
		case FunctionId.Count:
			_type = AggregateType.Count;
			break;
		case FunctionId.Var:
			_type = AggregateType.Var;
			break;
		case FunctionId.StDev:
			_type = AggregateType.StDev;
			break;
		default:
			throw ExprException.UndefinedFunction(Function.s_functionName[(int)aggregateType]);
		}
		_local = local;
		_relationName = relationName;
		_columnName = columnName;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
		if (table == null)
		{
			throw ExprException.AggregateUnbound(ToString());
		}
		if (_local)
		{
			_relation = null;
		}
		else
		{
			DataRelationCollection childRelations = table.ChildRelations;
			if (_relationName == null)
			{
				if (childRelations.Count > 1)
				{
					throw ExprException.UnresolvedRelation(table.TableName, ToString());
				}
				if (childRelations.Count != 1)
				{
					throw ExprException.AggregateUnbound(ToString());
				}
				_relation = childRelations[0];
			}
			else
			{
				_relation = childRelations[_relationName];
			}
		}
		_childTable = ((_relation == null) ? table : _relation.ChildTable);
		_column = _childTable.Columns[_columnName];
		if (_column == null)
		{
			throw ExprException.UnboundName(_columnName);
		}
		int i;
		for (i = 0; i < list.Count; i++)
		{
			DataColumn dataColumn = list[i];
			if (_column == dataColumn)
			{
				break;
			}
		}
		if (i >= list.Count)
		{
			list.Add(_column);
		}
		Bind(_relation, list);
	}

	internal static void Bind(DataRelation relation, List<DataColumn> list)
	{
		if (relation == null)
		{
			return;
		}
		DataColumn[] childColumnsReference = relation.ChildColumnsReference;
		foreach (DataColumn item in childColumnsReference)
		{
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		DataColumn[] parentColumnsReference = relation.ParentColumnsReference;
		foreach (DataColumn item2 in parentColumnsReference)
		{
			if (!list.Contains(item2))
			{
				list.Add(item2);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		return Eval(null, DataRowVersion.Default);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		if (_childTable == null)
		{
			throw ExprException.AggregateUnbound(ToString());
		}
		DataRow[] array;
		if (_local)
		{
			array = new DataRow[_childTable.Rows.Count];
			_childTable.Rows.CopyTo(array, 0);
		}
		else
		{
			if (row == null)
			{
				throw ExprException.EvalNoContext();
			}
			if (_relation == null)
			{
				throw ExprException.AggregateUnbound(ToString());
			}
			array = row.GetChildRows(_relation, version);
		}
		if (version == DataRowVersion.Proposed)
		{
			version = DataRowVersion.Default;
		}
		List<int> list = new List<int>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].RowState == DataRowState.Deleted)
			{
				if (DataRowAction.Rollback != array[i]._action)
				{
					continue;
				}
				version = DataRowVersion.Original;
			}
			else if (DataRowAction.Rollback == array[i]._action && array[i].RowState == DataRowState.Added)
			{
				continue;
			}
			if (version != DataRowVersion.Original || array[i]._oldRecord != -1)
			{
				list.Add(array[i].GetRecordFromVersion(version));
			}
		}
		int[] records = list.ToArray();
		return _column.GetAggregateValue(records, _type);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] records)
	{
		if (_childTable == null)
		{
			throw ExprException.AggregateUnbound(ToString());
		}
		if (!_local)
		{
			throw ExprException.ComputeNotAggregate(ToString());
		}
		return _column.GetAggregateValue(records, _type);
	}

	internal override bool IsConstant()
	{
		return false;
	}

	internal override bool IsTableConstant()
	{
		return _local;
	}

	internal override bool HasLocalAggregate()
	{
		return _local;
	}

	internal override bool HasRemoteAggregate()
	{
		return !_local;
	}

	internal override bool DependsOn(DataColumn column)
	{
		if (_column == column)
		{
			return true;
		}
		if (_column.Computed)
		{
			return _column.DataExpression.DependsOn(column);
		}
		return false;
	}

	internal override ExpressionNode Optimize()
	{
		return this;
	}
}
