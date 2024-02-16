using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class LookupNode : ExpressionNode
{
	private readonly string _relationName;

	private readonly string _columnName;

	private DataColumn _column;

	private DataRelation _relation;

	internal LookupNode(DataTable table, string columnName, string relationName)
		: base(table)
	{
		_relationName = relationName;
		_columnName = columnName;
	}

	internal override void Bind(DataTable table, List<DataColumn> list)
	{
		BindTable(table);
		_column = null;
		_relation = null;
		if (table == null)
		{
			throw ExprException.ExpressionUnbound(ToString());
		}
		DataRelationCollection parentRelations = table.ParentRelations;
		if (_relationName == null)
		{
			if (parentRelations.Count > 1)
			{
				throw ExprException.UnresolvedRelation(table.TableName, ToString());
			}
			_relation = parentRelations[0];
		}
		else
		{
			_relation = parentRelations[_relationName];
		}
		if (_relation == null)
		{
			throw ExprException.BindFailure(_relationName);
		}
		DataTable parentTable = _relation.ParentTable;
		_column = parentTable.Columns[_columnName];
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
		AggregateNode.Bind(_relation, list);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval()
	{
		throw ExprException.EvalNoContext();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(DataRow row, DataRowVersion version)
	{
		if (_column == null || _relation == null)
		{
			throw ExprException.ExpressionUnbound(ToString());
		}
		DataRow parentRow = row.GetParentRow(_relation, version);
		if (parentRow == null)
		{
			return DBNull.Value;
		}
		return parentRow[_column, parentRow.HasVersion(version) ? version : DataRowVersion.Current];
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal override object Eval(int[] recordNos)
	{
		throw ExprException.ComputeNotAggregate(ToString());
	}

	internal override bool IsConstant()
	{
		return false;
	}

	internal override bool IsTableConstant()
	{
		return false;
	}

	internal override bool HasLocalAggregate()
	{
		return false;
	}

	internal override bool HasRemoteAggregate()
	{
		return false;
	}

	internal override bool DependsOn(DataColumn column)
	{
		if (_column == column)
		{
			return true;
		}
		return false;
	}

	internal override ExpressionNode Optimize()
	{
		return this;
	}
}
