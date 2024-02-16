using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace System.Data;

internal sealed class Merger
{
	private readonly DataSet _dataSet;

	private readonly DataTable _dataTable;

	private readonly bool _preserveChanges;

	private readonly MissingSchemaAction _missingSchemaAction;

	private readonly bool _isStandAlonetable;

	private bool _IgnoreNSforTableLookup;

	internal Merger(DataSet dataSet, bool preserveChanges, MissingSchemaAction missingSchemaAction)
	{
		_dataSet = dataSet;
		_preserveChanges = preserveChanges;
		_missingSchemaAction = ((missingSchemaAction == MissingSchemaAction.AddWithKey) ? MissingSchemaAction.Add : missingSchemaAction);
	}

	internal Merger(DataTable dataTable, bool preserveChanges, MissingSchemaAction missingSchemaAction)
	{
		_isStandAlonetable = true;
		_dataTable = dataTable;
		_preserveChanges = preserveChanges;
		_missingSchemaAction = ((missingSchemaAction == MissingSchemaAction.AddWithKey) ? MissingSchemaAction.Add : missingSchemaAction);
	}

	internal void MergeDataSet(DataSet source)
	{
		if (source == _dataSet)
		{
			return;
		}
		bool enforceConstraints = _dataSet.EnforceConstraints;
		_dataSet.EnforceConstraints = false;
		_IgnoreNSforTableLookup = _dataSet._namespaceURI != source._namespaceURI;
		List<DataColumn> list = null;
		if (MissingSchemaAction.Add == _missingSchemaAction)
		{
			list = new List<DataColumn>();
			foreach (DataTable table in _dataSet.Tables)
			{
				foreach (DataColumn column in table.Columns)
				{
					list.Add(column);
				}
			}
		}
		for (int i = 0; i < source.Tables.Count; i++)
		{
			MergeTableData(source.Tables[i]);
		}
		if (MissingSchemaAction.Ignore != _missingSchemaAction)
		{
			MergeConstraints(source);
			for (int j = 0; j < source.Relations.Count; j++)
			{
				MergeRelation(source.Relations[j]);
			}
		}
		if (MissingSchemaAction.Add == _missingSchemaAction)
		{
			foreach (DataTable table2 in source.Tables)
			{
				DataTable dataTable3 = ((!_IgnoreNSforTableLookup) ? _dataSet.Tables[table2.TableName, table2.Namespace] : _dataSet.Tables[table2.TableName]);
				foreach (DataColumn column2 in table2.Columns)
				{
					if (column2.Computed)
					{
						DataColumn dataColumn2 = dataTable3.Columns[column2.ColumnName];
						if (!list.Contains(dataColumn2))
						{
							dataColumn2.CopyExpressionFrom(column2);
						}
					}
				}
			}
		}
		MergeExtendedProperties(source.ExtendedProperties, _dataSet.ExtendedProperties);
		foreach (DataTable table3 in _dataSet.Tables)
		{
			table3.EvaluateExpressions();
		}
		_dataSet.EnforceConstraints = enforceConstraints;
	}

	internal void MergeTable(DataTable src)
	{
		bool enforceConstraints = false;
		if (!_isStandAlonetable)
		{
			if (src.DataSet == _dataSet)
			{
				return;
			}
			enforceConstraints = _dataSet.EnforceConstraints;
			_dataSet.EnforceConstraints = false;
		}
		else
		{
			if (src == _dataTable)
			{
				return;
			}
			_dataTable.SuspendEnforceConstraints = true;
		}
		if (_dataSet != null)
		{
			if (src.DataSet == null || src.DataSet._namespaceURI != _dataSet._namespaceURI)
			{
				_IgnoreNSforTableLookup = true;
			}
		}
		else if (_dataTable.DataSet == null || src.DataSet == null || src.DataSet._namespaceURI != _dataTable.DataSet._namespaceURI)
		{
			_IgnoreNSforTableLookup = true;
		}
		MergeTableData(src);
		DataTable dataTable = _dataTable;
		if (dataTable == null && _dataSet != null)
		{
			dataTable = (_IgnoreNSforTableLookup ? _dataSet.Tables[src.TableName] : _dataSet.Tables[src.TableName, src.Namespace]);
		}
		dataTable?.EvaluateExpressions();
		if (!_isStandAlonetable)
		{
			_dataSet.EnforceConstraints = enforceConstraints;
			return;
		}
		_dataTable.SuspendEnforceConstraints = false;
		try
		{
			if (_dataTable.EnforceConstraints)
			{
				_dataTable.EnableConstraints();
			}
		}
		catch (ConstraintException)
		{
			if (_dataTable.DataSet != null)
			{
				_dataTable.DataSet.EnforceConstraints = false;
			}
			throw;
		}
	}

	private void MergeTable(DataTable src, DataTable dst)
	{
		int count = src.Rows.Count;
		bool flag = dst.Rows.Count == 0;
		if (0 < count)
		{
			Index index = null;
			DataKey key = default(DataKey);
			dst.SuspendIndexEvents();
			try
			{
				if (!flag && dst._primaryKey != null)
				{
					key = GetSrcKey(src, dst);
					if (key.HasValue)
					{
						index = dst._primaryKey.Key.GetSortIndex(DataViewRowState.OriginalRows | DataViewRowState.Added);
					}
				}
				foreach (DataRow row in src.Rows)
				{
					DataRow targetRow = null;
					if (index != null)
					{
						targetRow = dst.FindMergeTarget(row, key, index);
					}
					dst.MergeRow(row, targetRow, _preserveChanges, index);
				}
			}
			finally
			{
				dst.RestoreIndexEvents(forceReset: true);
			}
		}
		MergeExtendedProperties(src.ExtendedProperties, dst.ExtendedProperties);
	}

	internal void MergeRows(DataRow[] rows)
	{
		DataTable dataTable = null;
		DataTable dataTable2 = null;
		DataKey key = default(DataKey);
		Index index = null;
		bool enforceConstraints = _dataSet.EnforceConstraints;
		_dataSet.EnforceConstraints = false;
		for (int i = 0; i < rows.Length; i++)
		{
			DataRow dataRow = rows[i];
			if (dataRow == null)
			{
				throw ExceptionBuilder.ArgumentNull($"{"rows"}[{i}]");
			}
			if (dataRow.Table == null)
			{
				throw ExceptionBuilder.ArgumentNull($"{"rows"}[{i}].{"Table"}");
			}
			if (dataRow.Table.DataSet == _dataSet)
			{
				continue;
			}
			if (dataTable != dataRow.Table)
			{
				dataTable = dataRow.Table;
				dataTable2 = MergeSchema(dataRow.Table);
				if (dataTable2 == null)
				{
					_dataSet.EnforceConstraints = enforceConstraints;
					return;
				}
				if (dataTable2._primaryKey != null)
				{
					key = GetSrcKey(dataTable, dataTable2);
				}
				if (key.HasValue)
				{
					if (index != null)
					{
						index.RemoveRef();
						index = null;
					}
					index = new Index(dataTable2, dataTable2._primaryKey.Key.GetIndexDesc(), DataViewRowState.OriginalRows | DataViewRowState.Added, null);
					index.AddRef();
					index.AddRef();
				}
			}
			if (dataRow._newRecord != -1 || dataRow._oldRecord != -1)
			{
				DataRow targetRow = null;
				if (0 < dataTable2.Rows.Count && index != null)
				{
					targetRow = dataTable2.FindMergeTarget(dataRow, key, index);
				}
				targetRow = dataTable2.MergeRow(dataRow, targetRow, _preserveChanges, index);
				if (targetRow.Table._dependentColumns != null && targetRow.Table._dependentColumns.Count > 0)
				{
					targetRow.Table.EvaluateExpressions(targetRow, DataRowAction.Change, null);
				}
			}
		}
		if (index != null)
		{
			index.RemoveRef();
			index = null;
		}
		_dataSet.EnforceConstraints = enforceConstraints;
	}

	private DataTable MergeSchema(DataTable table)
	{
		DataTable dataTable = null;
		if (!_isStandAlonetable)
		{
			if (_dataSet.Tables.Contains(table.TableName, caseSensitive: true))
			{
				dataTable = ((!_IgnoreNSforTableLookup) ? _dataSet.Tables[table.TableName, table.Namespace] : _dataSet.Tables[table.TableName]);
			}
		}
		else
		{
			dataTable = _dataTable;
		}
		if (dataTable == null)
		{
			if (MissingSchemaAction.Add == _missingSchemaAction)
			{
				dataTable = table.Clone(table.DataSet);
				_dataSet.Tables.Add(dataTable);
			}
			else if (MissingSchemaAction.Error == _missingSchemaAction)
			{
				throw ExceptionBuilder.MergeMissingDefinition(table.TableName);
			}
		}
		else
		{
			if (MissingSchemaAction.Ignore != _missingSchemaAction)
			{
				int count = dataTable.Columns.Count;
				for (int i = 0; i < table.Columns.Count; i++)
				{
					DataColumn dataColumn = table.Columns[i];
					DataColumn dataColumn2 = (dataTable.Columns.Contains(dataColumn.ColumnName, caseSensitive: true) ? dataTable.Columns[dataColumn.ColumnName] : null);
					if (dataColumn2 == null)
					{
						if (MissingSchemaAction.Add == _missingSchemaAction)
						{
							dataColumn2 = dataColumn.Clone();
							dataTable.Columns.Add(dataColumn2);
							continue;
						}
						if (!_isStandAlonetable)
						{
							_dataSet.RaiseMergeFailed(dataTable, System.SR.Format(System.SR.DataMerge_MissingColumnDefinition, table.TableName, dataColumn.ColumnName), _missingSchemaAction);
							continue;
						}
						throw ExceptionBuilder.MergeFailed(System.SR.Format(System.SR.DataMerge_MissingColumnDefinition, table.TableName, dataColumn.ColumnName));
					}
					if (dataColumn2.DataType != dataColumn.DataType || (dataColumn2.DataType == typeof(DateTime) && dataColumn2.DateTimeMode != dataColumn.DateTimeMode && (dataColumn2.DateTimeMode & dataColumn.DateTimeMode) != DataSetDateTime.Unspecified))
					{
						if (_isStandAlonetable)
						{
							throw ExceptionBuilder.MergeFailed(System.SR.Format(System.SR.DataMerge_DataTypeMismatch, dataColumn.ColumnName));
						}
						_dataSet.RaiseMergeFailed(dataTable, System.SR.Format(System.SR.DataMerge_DataTypeMismatch, dataColumn.ColumnName), MissingSchemaAction.Error);
					}
					MergeExtendedProperties(dataColumn.ExtendedProperties, dataColumn2.ExtendedProperties);
				}
				if (_isStandAlonetable)
				{
					for (int j = count; j < dataTable.Columns.Count; j++)
					{
						dataTable.Columns[j].CopyExpressionFrom(table.Columns[dataTable.Columns[j].ColumnName]);
					}
				}
				DataColumn[] primaryKey = dataTable.PrimaryKey;
				DataColumn[] primaryKey2 = table.PrimaryKey;
				if (primaryKey.Length != primaryKey2.Length)
				{
					if (primaryKey.Length == 0)
					{
						DataColumn[] array = new DataColumn[primaryKey2.Length];
						for (int k = 0; k < primaryKey2.Length; k++)
						{
							array[k] = dataTable.Columns[primaryKey2[k].ColumnName];
						}
						dataTable.PrimaryKey = array;
					}
					else if (primaryKey2.Length != 0)
					{
						_dataSet.RaiseMergeFailed(dataTable, System.SR.DataMerge_PrimaryKeyMismatch, _missingSchemaAction);
					}
				}
				else
				{
					for (int l = 0; l < primaryKey.Length; l++)
					{
						if (string.Compare(primaryKey[l].ColumnName, primaryKey2[l].ColumnName, ignoreCase: false, dataTable.Locale) != 0)
						{
							_dataSet.RaiseMergeFailed(table, System.SR.Format(System.SR.DataMerge_PrimaryKeyColumnsMismatch, primaryKey[l].ColumnName, primaryKey2[l].ColumnName), _missingSchemaAction);
						}
					}
				}
			}
			MergeExtendedProperties(table.ExtendedProperties, dataTable.ExtendedProperties);
		}
		return dataTable;
	}

	private void MergeTableData(DataTable src)
	{
		DataTable dataTable = MergeSchema(src);
		if (dataTable == null)
		{
			return;
		}
		dataTable.MergingData = true;
		try
		{
			MergeTable(src, dataTable);
		}
		finally
		{
			dataTable.MergingData = false;
		}
	}

	private void MergeConstraints(DataSet source)
	{
		for (int i = 0; i < source.Tables.Count; i++)
		{
			MergeConstraints(source.Tables[i]);
		}
	}

	private void MergeConstraints(DataTable table)
	{
		for (int i = 0; i < table.Constraints.Count; i++)
		{
			Constraint constraint = table.Constraints[i];
			Constraint constraint2 = constraint.Clone(_dataSet, _IgnoreNSforTableLookup);
			if (constraint2 == null)
			{
				_dataSet.RaiseMergeFailed(table, System.SR.Format(System.SR.DataMerge_MissingConstraint, constraint.GetType().FullName, constraint.ConstraintName), _missingSchemaAction);
				continue;
			}
			Constraint constraint3 = constraint2.Table.Constraints.FindConstraint(constraint2);
			if (constraint3 == null)
			{
				if (MissingSchemaAction.Add == _missingSchemaAction)
				{
					try
					{
						constraint2.Table.Constraints.Add(constraint2);
					}
					catch (DuplicateNameException)
					{
						constraint2.ConstraintName = string.Empty;
						constraint2.Table.Constraints.Add(constraint2);
					}
				}
				else if (MissingSchemaAction.Error == _missingSchemaAction)
				{
					_dataSet.RaiseMergeFailed(table, System.SR.Format(System.SR.DataMerge_MissingConstraint, constraint.GetType().FullName, constraint.ConstraintName), _missingSchemaAction);
				}
			}
			else
			{
				MergeExtendedProperties(constraint.ExtendedProperties, constraint3.ExtendedProperties);
			}
		}
	}

	private void MergeRelation(DataRelation relation)
	{
		DataRelation dataRelation = null;
		int num = _dataSet.Relations.InternalIndexOf(relation.RelationName);
		if (num >= 0)
		{
			dataRelation = _dataSet.Relations[num];
			if (relation.ParentKey.ColumnsReference.Length != dataRelation.ParentKey.ColumnsReference.Length)
			{
				_dataSet.RaiseMergeFailed(null, System.SR.Format(System.SR.DataMerge_MissingDefinition, relation.RelationName), _missingSchemaAction);
			}
			for (int i = 0; i < relation.ParentKey.ColumnsReference.Length; i++)
			{
				DataColumn dataColumn = dataRelation.ParentKey.ColumnsReference[i];
				DataColumn dataColumn2 = relation.ParentKey.ColumnsReference[i];
				if (string.Compare(dataColumn.ColumnName, dataColumn2.ColumnName, ignoreCase: false, dataColumn.Table.Locale) != 0)
				{
					_dataSet.RaiseMergeFailed(null, System.SR.Format(System.SR.DataMerge_ReltionKeyColumnsMismatch, relation.RelationName), _missingSchemaAction);
				}
				dataColumn = dataRelation.ChildKey.ColumnsReference[i];
				dataColumn2 = relation.ChildKey.ColumnsReference[i];
				if (string.Compare(dataColumn.ColumnName, dataColumn2.ColumnName, ignoreCase: false, dataColumn.Table.Locale) != 0)
				{
					_dataSet.RaiseMergeFailed(null, System.SR.Format(System.SR.DataMerge_ReltionKeyColumnsMismatch, relation.RelationName), _missingSchemaAction);
				}
			}
		}
		else
		{
			if (MissingSchemaAction.Add != _missingSchemaAction)
			{
				throw ExceptionBuilder.MergeMissingDefinition(relation.RelationName);
			}
			DataTable dataTable = (_IgnoreNSforTableLookup ? _dataSet.Tables[relation.ParentTable.TableName] : _dataSet.Tables[relation.ParentTable.TableName, relation.ParentTable.Namespace]);
			DataTable dataTable2 = (_IgnoreNSforTableLookup ? _dataSet.Tables[relation.ChildTable.TableName] : _dataSet.Tables[relation.ChildTable.TableName, relation.ChildTable.Namespace]);
			DataColumn[] array = new DataColumn[relation.ParentKey.ColumnsReference.Length];
			DataColumn[] array2 = new DataColumn[relation.ParentKey.ColumnsReference.Length];
			for (int j = 0; j < relation.ParentKey.ColumnsReference.Length; j++)
			{
				array[j] = dataTable.Columns[relation.ParentKey.ColumnsReference[j].ColumnName];
				array2[j] = dataTable2.Columns[relation.ChildKey.ColumnsReference[j].ColumnName];
			}
			try
			{
				dataRelation = new DataRelation(relation.RelationName, array, array2, relation._createConstraints);
				dataRelation.Nested = relation.Nested;
				_dataSet.Relations.Add(dataRelation);
			}
			catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
			{
				ExceptionBuilder.TraceExceptionForCapture(ex);
				_dataSet.RaiseMergeFailed(null, ex.Message, _missingSchemaAction);
			}
		}
		MergeExtendedProperties(relation.ExtendedProperties, dataRelation.ExtendedProperties);
	}

	private void MergeExtendedProperties(PropertyCollection src, PropertyCollection dst)
	{
		if (MissingSchemaAction.Ignore == _missingSchemaAction)
		{
			return;
		}
		IDictionaryEnumerator enumerator = src.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (!_preserveChanges || dst[enumerator.Key] == null)
			{
				dst[enumerator.Key] = enumerator.Value;
			}
		}
	}

	private DataKey GetSrcKey(DataTable src, DataTable dst)
	{
		if (src._primaryKey != null)
		{
			return src._primaryKey.Key;
		}
		DataKey result = default(DataKey);
		if (dst._primaryKey != null)
		{
			DataColumn[] columnsReference = dst._primaryKey.Key.ColumnsReference;
			DataColumn[] array = new DataColumn[columnsReference.Length];
			for (int i = 0; i < columnsReference.Length; i++)
			{
				array[i] = src.Columns[columnsReference[i].ColumnName];
			}
			result = new DataKey(array, copyColumns: false);
		}
		return result;
	}
}
