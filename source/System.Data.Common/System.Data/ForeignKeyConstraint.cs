using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

[DefaultProperty("ConstraintName")]
[Editor("Microsoft.VSDesigner.Data.Design.ForeignKeyConstraintEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class ForeignKeyConstraint : Constraint
{
	internal Rule _deleteRule = Rule.Cascade;

	internal Rule _updateRule = Rule.Cascade;

	internal AcceptRejectRule _acceptRejectRule;

	private DataKey _childKey;

	private DataKey _parentKey;

	internal string _constraintName;

	internal string[] _parentColumnNames;

	internal string[] _childColumnNames;

	internal string _parentTableName;

	internal string _parentTableNamespace;

	internal DataKey ChildKey
	{
		get
		{
			CheckStateForProperty();
			return _childKey;
		}
	}

	[ReadOnly(true)]
	public virtual DataColumn[] Columns
	{
		get
		{
			CheckStateForProperty();
			return _childKey.ToArray();
		}
	}

	[ReadOnly(true)]
	public override DataTable? Table
	{
		get
		{
			CheckStateForProperty();
			return _childKey.Table;
		}
	}

	internal string[] ParentColumnNames => _parentKey.GetColumnNames();

	internal string[] ChildColumnNames => _childKey.GetColumnNames();

	[DefaultValue(AcceptRejectRule.None)]
	public virtual AcceptRejectRule AcceptRejectRule
	{
		get
		{
			CheckStateForProperty();
			return _acceptRejectRule;
		}
		set
		{
			if ((uint)value <= 1u)
			{
				_acceptRejectRule = value;
				return;
			}
			throw ADP.InvalidAcceptRejectRule(value);
		}
	}

	[DefaultValue(Rule.Cascade)]
	public virtual Rule DeleteRule
	{
		get
		{
			CheckStateForProperty();
			return _deleteRule;
		}
		set
		{
			if ((uint)value <= 3u)
			{
				_deleteRule = value;
				return;
			}
			throw ADP.InvalidRule(value);
		}
	}

	[ReadOnly(true)]
	public virtual DataColumn[] RelatedColumns
	{
		get
		{
			CheckStateForProperty();
			return _parentKey.ToArray();
		}
	}

	internal DataColumn[] RelatedColumnsReference
	{
		get
		{
			CheckStateForProperty();
			return _parentKey.ColumnsReference;
		}
	}

	internal DataKey ParentKey
	{
		get
		{
			CheckStateForProperty();
			return _parentKey;
		}
	}

	[ReadOnly(true)]
	public virtual DataTable RelatedTable
	{
		get
		{
			CheckStateForProperty();
			return _parentKey.Table;
		}
	}

	[DefaultValue(Rule.Cascade)]
	public virtual Rule UpdateRule
	{
		get
		{
			CheckStateForProperty();
			return _updateRule;
		}
		set
		{
			if ((uint)value <= 3u)
			{
				_updateRule = value;
				return;
			}
			throw ADP.InvalidRule(value);
		}
	}

	public ForeignKeyConstraint(DataColumn parentColumn, DataColumn childColumn)
		: this(null, parentColumn, childColumn)
	{
	}

	public ForeignKeyConstraint(string? constraintName, DataColumn parentColumn, DataColumn childColumn)
	{
		DataColumn[] parentColumns = new DataColumn[1] { parentColumn };
		DataColumn[] childColumns = new DataColumn[1] { childColumn };
		Create(constraintName, parentColumns, childColumns);
	}

	public ForeignKeyConstraint(DataColumn[] parentColumns, DataColumn[] childColumns)
		: this(null, parentColumns, childColumns)
	{
	}

	public ForeignKeyConstraint(string? constraintName, DataColumn[] parentColumns, DataColumn[] childColumns)
	{
		Create(constraintName, parentColumns, childColumns);
	}

	[Browsable(false)]
	public ForeignKeyConstraint(string? constraintName, string? parentTableName, string[] parentColumnNames, string[] childColumnNames, AcceptRejectRule acceptRejectRule, Rule deleteRule, Rule updateRule)
	{
		_constraintName = constraintName;
		_parentColumnNames = parentColumnNames;
		_childColumnNames = childColumnNames;
		_parentTableName = parentTableName;
		_acceptRejectRule = acceptRejectRule;
		_deleteRule = deleteRule;
		_updateRule = updateRule;
	}

	[Browsable(false)]
	public ForeignKeyConstraint(string? constraintName, string? parentTableName, string? parentTableNamespace, string[] parentColumnNames, string[] childColumnNames, AcceptRejectRule acceptRejectRule, Rule deleteRule, Rule updateRule)
	{
		_constraintName = constraintName;
		_parentColumnNames = parentColumnNames;
		_childColumnNames = childColumnNames;
		_parentTableName = parentTableName;
		_parentTableNamespace = parentTableNamespace;
		_acceptRejectRule = acceptRejectRule;
		_deleteRule = deleteRule;
		_updateRule = updateRule;
	}

	internal override void CheckCanAddToCollection(ConstraintCollection constraints)
	{
		if (Table != constraints.Table)
		{
			throw ExceptionBuilder.ConstraintAddFailed(constraints.Table);
		}
		if (Table.Locale.LCID != RelatedTable.Locale.LCID || Table.CaseSensitive != RelatedTable.CaseSensitive)
		{
			throw ExceptionBuilder.CaseLocaleMismatch();
		}
	}

	internal override bool CanBeRemovedFromCollection(ConstraintCollection constraints, bool fThrowException)
	{
		return true;
	}

	internal bool IsKeyNull(object[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (!DataStorage.IsObjectNull(values[i]))
			{
				return false;
			}
		}
		return true;
	}

	internal override bool IsConstraintViolated()
	{
		Index sortIndex = _childKey.GetSortIndex();
		object[] uniqueKeyValues = sortIndex.GetUniqueKeyValues();
		bool result = false;
		Index sortIndex2 = _parentKey.GetSortIndex();
		for (int i = 0; i < uniqueKeyValues.Length; i++)
		{
			object[] array = (object[])uniqueKeyValues[i];
			if (!IsKeyNull(array) && !sortIndex2.IsKeyInIndex(array))
			{
				DataRow[] rows = sortIndex.GetRows(sortIndex.FindRecords(array));
				string rowError = System.SR.Format(System.SR.DataConstraint_ForeignKeyViolation, ConstraintName, ExceptionBuilder.KeysToString(array));
				for (int j = 0; j < rows.Length; j++)
				{
					rows[j].RowError = rowError;
				}
				result = true;
			}
		}
		return result;
	}

	internal override bool CanEnableConstraint()
	{
		if (Table.DataSet == null || !Table.DataSet.EnforceConstraints)
		{
			return true;
		}
		Index sortIndex = _childKey.GetSortIndex();
		object[] uniqueKeyValues = sortIndex.GetUniqueKeyValues();
		Index sortIndex2 = _parentKey.GetSortIndex();
		for (int i = 0; i < uniqueKeyValues.Length; i++)
		{
			object[] array = (object[])uniqueKeyValues[i];
			if (!IsKeyNull(array) && !sortIndex2.IsKeyInIndex(array))
			{
				return false;
			}
		}
		return true;
	}

	internal void CascadeCommit(DataRow row)
	{
		if (row.RowState == DataRowState.Detached || _acceptRejectRule != AcceptRejectRule.Cascade)
		{
			return;
		}
		Index sortIndex = _childKey.GetSortIndex((row.RowState == DataRowState.Deleted) ? DataViewRowState.Deleted : DataViewRowState.CurrentRows);
		object[] keyValues = row.GetKeyValues(_parentKey, (row.RowState == DataRowState.Deleted) ? DataRowVersion.Original : DataRowVersion.Default);
		if (IsKeyNull(keyValues))
		{
			return;
		}
		Range range = sortIndex.FindRecords(keyValues);
		if (range.IsNull)
		{
			return;
		}
		DataRow[] rows = sortIndex.GetRows(range);
		DataRow[] array = rows;
		foreach (DataRow dataRow in array)
		{
			if (DataRowState.Detached != dataRow.RowState && !dataRow._inCascade)
			{
				dataRow.AcceptChanges();
			}
		}
	}

	internal void CascadeDelete(DataRow row)
	{
		if (-1 == row._newRecord)
		{
			return;
		}
		object[] keyValues = row.GetKeyValues(_parentKey, DataRowVersion.Current);
		if (IsKeyNull(keyValues))
		{
			return;
		}
		Index sortIndex = _childKey.GetSortIndex();
		switch (DeleteRule)
		{
		case Rule.None:
			if (row.Table.DataSet.EnforceConstraints)
			{
				Range range2 = sortIndex.FindRecords(keyValues);
				if (!range2.IsNull && (range2.Count != 1 || sortIndex.GetRow(range2.Min) != row))
				{
					throw ExceptionBuilder.FailedCascadeDelete(ConstraintName);
				}
			}
			break;
		case Rule.Cascade:
		{
			object[] keyValues2 = row.GetKeyValues(_parentKey, DataRowVersion.Default);
			Range range4 = sortIndex.FindRecords(keyValues2);
			if (range4.IsNull)
			{
				break;
			}
			DataRow[] rows3 = sortIndex.GetRows(range4);
			foreach (DataRow dataRow in rows3)
			{
				if (!dataRow._inCascade)
				{
					dataRow.Table.DeleteRow(dataRow);
				}
			}
			break;
		}
		case Rule.SetNull:
		{
			object[] array2 = new object[_childKey.ColumnsReference.Length];
			for (int k = 0; k < _childKey.ColumnsReference.Length; k++)
			{
				array2[k] = DBNull.Value;
			}
			Range range3 = sortIndex.FindRecords(keyValues);
			if (range3.IsNull)
			{
				break;
			}
			DataRow[] rows2 = sortIndex.GetRows(range3);
			for (int l = 0; l < rows2.Length; l++)
			{
				if (row != rows2[l])
				{
					rows2[l].SetKeyValues(_childKey, array2);
				}
			}
			break;
		}
		case Rule.SetDefault:
		{
			object[] array = new object[_childKey.ColumnsReference.Length];
			for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
			{
				array[i] = _childKey.ColumnsReference[i].DefaultValue;
			}
			Range range = sortIndex.FindRecords(keyValues);
			if (range.IsNull)
			{
				break;
			}
			DataRow[] rows = sortIndex.GetRows(range);
			for (int j = 0; j < rows.Length; j++)
			{
				if (row != rows[j])
				{
					rows[j].SetKeyValues(_childKey, array);
				}
			}
			break;
		}
		}
	}

	internal void CascadeRollback(DataRow row)
	{
		Index sortIndex = _childKey.GetSortIndex((row.RowState == DataRowState.Deleted) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
		object[] keyValues = row.GetKeyValues(_parentKey, (row.RowState == DataRowState.Modified) ? DataRowVersion.Current : DataRowVersion.Default);
		if (IsKeyNull(keyValues))
		{
			return;
		}
		Range range = sortIndex.FindRecords(keyValues);
		if (_acceptRejectRule == AcceptRejectRule.Cascade)
		{
			if (range.IsNull)
			{
				return;
			}
			DataRow[] rows = sortIndex.GetRows(range);
			for (int i = 0; i < rows.Length; i++)
			{
				if (!rows[i]._inCascade)
				{
					rows[i].RejectChanges();
				}
			}
		}
		else if (row.RowState != DataRowState.Deleted && row.Table.DataSet.EnforceConstraints && !range.IsNull && (range.Count != 1 || sortIndex.GetRow(range.Min) != row) && row.HasKeyChanged(_parentKey))
		{
			throw ExceptionBuilder.FailedCascadeUpdate(ConstraintName);
		}
	}

	internal void CascadeUpdate(DataRow row)
	{
		if (-1 == row._newRecord)
		{
			return;
		}
		object[] keyValues = row.GetKeyValues(_parentKey, DataRowVersion.Current);
		if (!Table.DataSet._fInReadXml && IsKeyNull(keyValues))
		{
			return;
		}
		Index sortIndex = _childKey.GetSortIndex();
		switch (UpdateRule)
		{
		case Rule.None:
			if (row.Table.DataSet.EnforceConstraints && !sortIndex.FindRecords(keyValues).IsNull)
			{
				throw ExceptionBuilder.FailedCascadeUpdate(ConstraintName);
			}
			break;
		case Rule.Cascade:
		{
			Range range3 = sortIndex.FindRecords(keyValues);
			if (!range3.IsNull)
			{
				object[] keyValues2 = row.GetKeyValues(_parentKey, DataRowVersion.Proposed);
				DataRow[] rows3 = sortIndex.GetRows(range3);
				for (int m = 0; m < rows3.Length; m++)
				{
					rows3[m].SetKeyValues(_childKey, keyValues2);
				}
			}
			break;
		}
		case Rule.SetNull:
		{
			object[] array2 = new object[_childKey.ColumnsReference.Length];
			for (int k = 0; k < _childKey.ColumnsReference.Length; k++)
			{
				array2[k] = DBNull.Value;
			}
			Range range2 = sortIndex.FindRecords(keyValues);
			if (!range2.IsNull)
			{
				DataRow[] rows2 = sortIndex.GetRows(range2);
				for (int l = 0; l < rows2.Length; l++)
				{
					rows2[l].SetKeyValues(_childKey, array2);
				}
			}
			break;
		}
		case Rule.SetDefault:
		{
			object[] array = new object[_childKey.ColumnsReference.Length];
			for (int i = 0; i < _childKey.ColumnsReference.Length; i++)
			{
				array[i] = _childKey.ColumnsReference[i].DefaultValue;
			}
			Range range = sortIndex.FindRecords(keyValues);
			if (!range.IsNull)
			{
				DataRow[] rows = sortIndex.GetRows(range);
				for (int j = 0; j < rows.Length; j++)
				{
					rows[j].SetKeyValues(_childKey, array);
				}
			}
			break;
		}
		}
	}

	internal void CheckCanClearParentTable(DataTable table)
	{
		if (Table.DataSet.EnforceConstraints && Table.Rows.Count > 0)
		{
			throw ExceptionBuilder.FailedClearParentTable(table.TableName, ConstraintName, Table.TableName);
		}
	}

	internal void CheckCanRemoveParentRow(DataRow row)
	{
		if (!Table.DataSet.EnforceConstraints || DataRelation.GetChildRows(ParentKey, ChildKey, row, DataRowVersion.Default).Length == 0)
		{
			return;
		}
		throw ExceptionBuilder.RemoveParentRow(this);
	}

	internal void CheckCascade(DataRow row, DataRowAction action)
	{
		if (row._inCascade)
		{
			return;
		}
		row._inCascade = true;
		try
		{
			switch (action)
			{
			case DataRowAction.Change:
				if (row.HasKeyChanged(_parentKey))
				{
					CascadeUpdate(row);
				}
				break;
			case DataRowAction.Delete:
				CascadeDelete(row);
				break;
			case DataRowAction.Commit:
				CascadeCommit(row);
				break;
			case DataRowAction.Rollback:
				CascadeRollback(row);
				break;
			default:
				_ = 16;
				break;
			}
		}
		finally
		{
			row._inCascade = false;
		}
	}

	internal override void CheckConstraint(DataRow childRow, DataRowAction action)
	{
		if ((action != DataRowAction.Change && action != DataRowAction.Add && action != DataRowAction.Rollback) || Table.DataSet == null || !Table.DataSet.EnforceConstraints || !childRow.HasKeyChanged(_childKey))
		{
			return;
		}
		DataRowVersion dataRowVersion = ((action == DataRowAction.Rollback) ? DataRowVersion.Original : DataRowVersion.Current);
		object[] keyValues = childRow.GetKeyValues(_childKey);
		if (childRow.HasVersion(dataRowVersion))
		{
			DataRow parentRow = DataRelation.GetParentRow(ParentKey, ChildKey, childRow, dataRowVersion);
			if (parentRow != null && parentRow._inCascade)
			{
				object[] keyValues2 = parentRow.GetKeyValues(_parentKey, (action == DataRowAction.Rollback) ? dataRowVersion : DataRowVersion.Default);
				int num = childRow.Table.NewRecord();
				childRow.Table.SetKeyValues(_childKey, keyValues2, num);
				if (_childKey.RecordsEqual(childRow._tempRecord, num))
				{
					return;
				}
			}
		}
		object[] keyValues3 = childRow.GetKeyValues(_childKey);
		if (IsKeyNull(keyValues3))
		{
			return;
		}
		Index sortIndex = _parentKey.GetSortIndex();
		if (sortIndex.IsKeyInIndex(keyValues3))
		{
			return;
		}
		if (_childKey.Table == _parentKey.Table && childRow._tempRecord != -1)
		{
			int num2 = 0;
			for (num2 = 0; num2 < keyValues3.Length; num2++)
			{
				DataColumn dataColumn = _parentKey.ColumnsReference[num2];
				object value = dataColumn.ConvertValue(keyValues3[num2]);
				if (dataColumn.CompareValueTo(childRow._tempRecord, value) != 0)
				{
					break;
				}
			}
			if (num2 == keyValues3.Length)
			{
				return;
			}
		}
		throw ExceptionBuilder.ForeignKeyViolation(ConstraintName, keyValues);
	}

	private void NonVirtualCheckState()
	{
		if (_DataSet != null)
		{
			return;
		}
		_parentKey.CheckState();
		_childKey.CheckState();
		if (_parentKey.Table.DataSet != _childKey.Table.DataSet)
		{
			throw ExceptionBuilder.TablesInDifferentSets();
		}
		for (int i = 0; i < _parentKey.ColumnsReference.Length; i++)
		{
			if (_parentKey.ColumnsReference[i].DataType != _childKey.ColumnsReference[i].DataType || (_parentKey.ColumnsReference[i].DataType == typeof(DateTime) && _parentKey.ColumnsReference[i].DateTimeMode != _childKey.ColumnsReference[i].DateTimeMode && (_parentKey.ColumnsReference[i].DateTimeMode & _childKey.ColumnsReference[i].DateTimeMode) != DataSetDateTime.Unspecified))
			{
				throw ExceptionBuilder.ColumnsTypeMismatch();
			}
		}
		if (_childKey.ColumnsEqual(_parentKey))
		{
			throw ExceptionBuilder.KeyColumnsIdentical();
		}
	}

	internal override void CheckState()
	{
		NonVirtualCheckState();
	}

	internal override bool ContainsColumn(DataColumn column)
	{
		if (!_parentKey.ContainsColumn(column))
		{
			return _childKey.ContainsColumn(column);
		}
		return true;
	}

	internal override Constraint Clone(DataSet destination)
	{
		return Clone(destination, ignorNSforTableLookup: false);
	}

	internal override Constraint Clone(DataSet destination, bool ignorNSforTableLookup)
	{
		int num = ((!ignorNSforTableLookup) ? destination.Tables.IndexOf(Table.TableName, Table.Namespace, chekforNull: false) : destination.Tables.IndexOf(Table.TableName));
		if (num < 0)
		{
			return null;
		}
		DataTable dataTable = destination.Tables[num];
		num = ((!ignorNSforTableLookup) ? destination.Tables.IndexOf(RelatedTable.TableName, RelatedTable.Namespace, chekforNull: false) : destination.Tables.IndexOf(RelatedTable.TableName));
		if (num < 0)
		{
			return null;
		}
		DataTable dataTable2 = destination.Tables[num];
		int num2 = Columns.Length;
		DataColumn[] array = new DataColumn[num2];
		DataColumn[] array2 = new DataColumn[num2];
		for (int i = 0; i < num2; i++)
		{
			DataColumn dataColumn = Columns[i];
			num = dataTable.Columns.IndexOf(dataColumn.ColumnName);
			if (num < 0)
			{
				return null;
			}
			array[i] = dataTable.Columns[num];
			dataColumn = RelatedColumnsReference[i];
			num = dataTable2.Columns.IndexOf(dataColumn.ColumnName);
			if (num < 0)
			{
				return null;
			}
			array2[i] = dataTable2.Columns[num];
		}
		ForeignKeyConstraint foreignKeyConstraint = new ForeignKeyConstraint(ConstraintName, array2, array);
		foreignKeyConstraint.UpdateRule = UpdateRule;
		foreignKeyConstraint.DeleteRule = DeleteRule;
		foreignKeyConstraint.AcceptRejectRule = AcceptRejectRule;
		foreach (object key in base.ExtendedProperties.Keys)
		{
			foreignKeyConstraint.ExtendedProperties[key] = base.ExtendedProperties[key];
		}
		return foreignKeyConstraint;
	}

	internal ForeignKeyConstraint Clone(DataTable destination)
	{
		int num = Columns.Length;
		DataColumn[] array = new DataColumn[num];
		DataColumn[] array2 = new DataColumn[num];
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			DataColumn dataColumn = Columns[i];
			num2 = destination.Columns.IndexOf(dataColumn.ColumnName);
			if (num2 < 0)
			{
				return null;
			}
			array[i] = destination.Columns[num2];
			dataColumn = RelatedColumnsReference[i];
			num2 = destination.Columns.IndexOf(dataColumn.ColumnName);
			if (num2 < 0)
			{
				return null;
			}
			array2[i] = destination.Columns[num2];
		}
		ForeignKeyConstraint foreignKeyConstraint = new ForeignKeyConstraint(ConstraintName, array2, array);
		foreignKeyConstraint.UpdateRule = UpdateRule;
		foreignKeyConstraint.DeleteRule = DeleteRule;
		foreignKeyConstraint.AcceptRejectRule = AcceptRejectRule;
		foreach (object key in base.ExtendedProperties.Keys)
		{
			foreignKeyConstraint.ExtendedProperties[key] = base.ExtendedProperties[key];
		}
		return foreignKeyConstraint;
	}

	private void Create(string relationName, DataColumn[] parentColumns, DataColumn[] childColumns)
	{
		if (parentColumns.Length == 0 || childColumns.Length == 0)
		{
			throw ExceptionBuilder.KeyLengthZero();
		}
		if (parentColumns.Length != childColumns.Length)
		{
			throw ExceptionBuilder.KeyLengthMismatch();
		}
		for (int i = 0; i < parentColumns.Length; i++)
		{
			if (parentColumns[i].Computed)
			{
				throw ExceptionBuilder.ExpressionInConstraint(parentColumns[i]);
			}
			if (childColumns[i].Computed)
			{
				throw ExceptionBuilder.ExpressionInConstraint(childColumns[i]);
			}
		}
		_parentKey = new DataKey(parentColumns, copyColumns: true);
		_childKey = new DataKey(childColumns, copyColumns: true);
		ConstraintName = relationName;
		NonVirtualCheckState();
	}

	public override bool Equals([NotNullWhen(true)] object? key)
	{
		if (!(key is ForeignKeyConstraint))
		{
			return false;
		}
		ForeignKeyConstraint foreignKeyConstraint = (ForeignKeyConstraint)key;
		if (ParentKey.ColumnsEqual(foreignKeyConstraint.ParentKey))
		{
			return ChildKey.ColumnsEqual(foreignKeyConstraint.ChildKey);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	internal DataRelation FindParentRelation()
	{
		DataRelationCollection parentRelations = Table.ParentRelations;
		for (int i = 0; i < parentRelations.Count; i++)
		{
			if (parentRelations[i].ChildKeyConstraint == this)
			{
				return parentRelations[i];
			}
		}
		return null;
	}
}
