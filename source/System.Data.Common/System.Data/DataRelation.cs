using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Data;

[DefaultProperty("RelationName")]
[Editor("Microsoft.VSDesigner.Data.Design.DataRelationEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[TypeConverter(typeof(RelationshipConverter))]
public class DataRelation
{
	private DataSet _dataSet;

	internal PropertyCollection _extendedProperties;

	internal string _relationName = string.Empty;

	private DataKey _childKey;

	private DataKey _parentKey;

	private UniqueConstraint _parentKeyConstraint;

	private ForeignKeyConstraint _childKeyConstraint;

	internal string[] _parentColumnNames;

	internal string[] _childColumnNames;

	internal string _parentTableName;

	internal string _childTableName;

	internal string _parentTableNamespace;

	internal string _childTableNamespace;

	internal bool _nested;

	internal bool _createConstraints;

	private bool _checkMultipleNested = true;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	public virtual DataColumn[] ChildColumns
	{
		get
		{
			CheckStateForProperty();
			return _childKey.ToArray();
		}
	}

	internal DataColumn[] ChildColumnsReference
	{
		get
		{
			CheckStateForProperty();
			return _childKey.ColumnsReference;
		}
	}

	internal DataKey ChildKey
	{
		get
		{
			CheckStateForProperty();
			return _childKey;
		}
	}

	public virtual DataTable ChildTable
	{
		get
		{
			CheckStateForProperty();
			return _childKey.Table;
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false)]
	public virtual DataSet? DataSet
	{
		get
		{
			CheckStateForProperty();
			return _dataSet;
		}
	}

	internal string[] ParentColumnNames => _parentKey.GetColumnNames();

	internal string[] ChildColumnNames => _childKey.GetColumnNames();

	public virtual DataColumn[] ParentColumns
	{
		get
		{
			CheckStateForProperty();
			return _parentKey.ToArray();
		}
	}

	internal DataColumn[] ParentColumnsReference => _parentKey.ColumnsReference;

	internal DataKey ParentKey
	{
		get
		{
			CheckStateForProperty();
			return _parentKey;
		}
	}

	public virtual DataTable ParentTable
	{
		get
		{
			CheckStateForProperty();
			return _parentKey.Table;
		}
	}

	[DefaultValue("")]
	public virtual string RelationName
	{
		get
		{
			CheckStateForProperty();
			return _relationName;
		}
		[param: AllowNull]
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.set_RelationName|API> {0}, '{1}'", ObjectID, value);
			try
			{
				if (value == null)
				{
					value = string.Empty;
				}
				CultureInfo culture = ((_dataSet != null) ? _dataSet.Locale : CultureInfo.CurrentCulture);
				if (string.Compare(_relationName, value, ignoreCase: true, culture) != 0)
				{
					if (_dataSet != null)
					{
						if (value.Length == 0)
						{
							throw ExceptionBuilder.NoRelationName();
						}
						_dataSet.Relations.RegisterName(value);
						if (_relationName.Length != 0)
						{
							_dataSet.Relations.UnregisterName(_relationName);
						}
					}
					_relationName = value;
					((DataRelationCollection.DataTableRelationCollection)ParentTable.ChildRelations).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
					((DataRelationCollection.DataTableRelationCollection)ChildTable.ParentRelations).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
				}
				else if (string.Compare(_relationName, value, ignoreCase: false, culture) != 0)
				{
					_relationName = value;
					((DataRelationCollection.DataTableRelationCollection)ParentTable.ChildRelations).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
					((DataRelationCollection.DataTableRelationCollection)ChildTable.ParentRelations).OnRelationPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	[DefaultValue(false)]
	public virtual bool Nested
	{
		get
		{
			CheckStateForProperty();
			return _nested;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.set_Nested|API> {0}, {1}", ObjectID, value);
			try
			{
				if (_nested == value)
				{
					return;
				}
				if (_dataSet != null && value)
				{
					if (ChildTable.IsNamespaceInherited())
					{
						CheckNamespaceValidityForNestedRelations(ParentTable.Namespace);
					}
					ChildTable.Constraints.FindForeignKeyConstraint(ChildKey.ColumnsReference, ParentKey.ColumnsReference)?.CheckConstraint();
					ValidateMultipleNestedRelations();
				}
				if (!value && _parentKey.ColumnsReference[0].ColumnMapping == MappingType.Hidden)
				{
					throw ExceptionBuilder.RelationNestedReadOnly();
				}
				if (value)
				{
					ParentTable.Columns.RegisterColumnName(ChildTable.TableName, null);
				}
				else
				{
					ParentTable.Columns.UnregisterName(ChildTable.TableName);
				}
				RaisePropertyChanging("Nested");
				if (value)
				{
					CheckNestedRelations();
					if (DataSet != null)
					{
						if (ParentTable == ChildTable)
						{
							foreach (DataRow row in ChildTable.Rows)
							{
								row.CheckForLoops(this);
							}
							if (ChildTable.DataSet != null && string.Compare(ChildTable.TableName, ChildTable.DataSet.DataSetName, ignoreCase: true, ChildTable.DataSet.Locale) == 0)
							{
								throw ExceptionBuilder.DatasetConflictingName(DataSet.DataSetName);
							}
							ChildTable._fNestedInDataset = false;
						}
						else
						{
							foreach (DataRow row2 in ChildTable.Rows)
							{
								row2.GetParentRow(this);
							}
						}
					}
					ParentTable.ElementColumnCount++;
				}
				else
				{
					ParentTable.ElementColumnCount--;
				}
				_nested = value;
				ChildTable.CacheNestedParent();
				if (!value || !string.IsNullOrEmpty(ChildTable.Namespace) || (ChildTable.NestedParentsCount <= 1 && (ChildTable.NestedParentsCount <= 0 || ChildTable.DataSet.Relations.Contains(RelationName))))
				{
					return;
				}
				string text = null;
				foreach (DataRelation parentRelation in ChildTable.ParentRelations)
				{
					if (parentRelation.Nested)
					{
						if (text == null)
						{
							text = parentRelation.ParentTable.Namespace;
						}
						else if (!string.Equals(text, parentRelation.ParentTable.Namespace, StringComparison.Ordinal))
						{
							_nested = false;
							throw ExceptionBuilder.InvalidParentNamespaceinNestedRelation(ChildTable.TableName);
						}
					}
				}
				if (CheckMultipleNested && ChildTable._tableNamespace != null && ChildTable._tableNamespace.Length == 0)
				{
					throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
				}
				ChildTable._tableNamespace = null;
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	public virtual UniqueConstraint? ParentKeyConstraint
	{
		get
		{
			CheckStateForProperty();
			return _parentKeyConstraint;
		}
	}

	public virtual ForeignKeyConstraint? ChildKeyConstraint
	{
		get
		{
			CheckStateForProperty();
			return _childKeyConstraint;
		}
	}

	[Browsable(false)]
	public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

	internal bool CheckMultipleNested
	{
		get
		{
			return _checkMultipleNested;
		}
		set
		{
			_checkMultipleNested = value;
		}
	}

	internal int ObjectID => _objectID;

	internal event PropertyChangedEventHandler? PropertyChanging;

	public DataRelation(string? relationName, DataColumn parentColumn, DataColumn childColumn)
		: this(relationName, parentColumn, childColumn, createConstraints: true)
	{
	}

	public DataRelation(string? relationName, DataColumn parentColumn, DataColumn childColumn, bool createConstraints)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelation.DataRelation|API> {0}, relationName='{1}', parentColumn={2}, childColumn={3}, createConstraints={4}", ObjectID, relationName, parentColumn?.ObjectID ?? 0, childColumn?.ObjectID ?? 0, createConstraints);
		Create(relationName, new DataColumn[1] { parentColumn }, new DataColumn[1] { childColumn }, createConstraints);
	}

	public DataRelation(string? relationName, DataColumn[] parentColumns, DataColumn[] childColumns)
		: this(relationName, parentColumns, childColumns, createConstraints: true)
	{
	}

	public DataRelation(string? relationName, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
	{
		Create(relationName, parentColumns, childColumns, createConstraints);
	}

	[Browsable(false)]
	public DataRelation(string relationName, string? parentTableName, string? childTableName, string[]? parentColumnNames, string[]? childColumnNames, bool nested)
	{
		_relationName = relationName;
		_parentColumnNames = parentColumnNames;
		_childColumnNames = childColumnNames;
		_parentTableName = parentTableName;
		_childTableName = childTableName;
		_nested = nested;
	}

	[Browsable(false)]
	public DataRelation(string relationName, string? parentTableName, string? parentTableNamespace, string? childTableName, string? childTableNamespace, string[]? parentColumnNames, string[]? childColumnNames, bool nested)
	{
		_relationName = relationName;
		_parentColumnNames = parentColumnNames;
		_childColumnNames = childColumnNames;
		_parentTableName = parentTableName;
		_childTableName = childTableName;
		_parentTableNamespace = parentTableNamespace;
		_childTableNamespace = childTableNamespace;
		_nested = nested;
	}

	private static bool IsKeyNull(object[] values)
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

	internal static DataRow[] GetChildRows(DataKey parentKey, DataKey childKey, DataRow parentRow, DataRowVersion version)
	{
		object[] keyValues = parentRow.GetKeyValues(parentKey, version);
		if (IsKeyNull(keyValues))
		{
			return childKey.Table.NewRowArray(0);
		}
		Index sortIndex = childKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
		return sortIndex.GetRows(keyValues);
	}

	internal static DataRow[] GetParentRows(DataKey parentKey, DataKey childKey, DataRow childRow, DataRowVersion version)
	{
		object[] keyValues = childRow.GetKeyValues(childKey, version);
		if (IsKeyNull(keyValues))
		{
			return parentKey.Table.NewRowArray(0);
		}
		Index sortIndex = parentKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
		return sortIndex.GetRows(keyValues);
	}

	internal static DataRow GetParentRow(DataKey parentKey, DataKey childKey, DataRow childRow, DataRowVersion version)
	{
		if (!childRow.HasVersion((version == DataRowVersion.Original) ? DataRowVersion.Original : DataRowVersion.Current) && childRow._tempRecord == -1)
		{
			return null;
		}
		object[] keyValues = childRow.GetKeyValues(childKey, version);
		if (IsKeyNull(keyValues))
		{
			return null;
		}
		Index sortIndex = parentKey.GetSortIndex((version == DataRowVersion.Original) ? DataViewRowState.OriginalRows : DataViewRowState.CurrentRows);
		Range range = sortIndex.FindRecords(keyValues);
		if (range.IsNull)
		{
			return null;
		}
		if (range.Count > 1)
		{
			throw ExceptionBuilder.MultipleParents();
		}
		return parentKey.Table._recordManager[sortIndex.GetRecord(range.Min)];
	}

	internal void SetDataSet(DataSet dataSet)
	{
		if (_dataSet != dataSet)
		{
			_dataSet = dataSet;
		}
	}

	internal void CheckNamespaceValidityForNestedRelations(string ns)
	{
		foreach (DataRelation parentRelation in ChildTable.ParentRelations)
		{
			if ((parentRelation == this || parentRelation.Nested) && parentRelation.ParentTable.Namespace != ns)
			{
				throw ExceptionBuilder.InValidNestedRelation(ChildTable.TableName);
			}
		}
	}

	internal void CheckNestedRelations()
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelation.CheckNestedRelations|INFO> {0}", ObjectID);
		if (ChildTable == ParentTable)
		{
			if (string.Compare(ChildTable.TableName, ChildTable.DataSet.DataSetName, ignoreCase: true, ChildTable.DataSet.Locale) == 0)
			{
				throw ExceptionBuilder.SelfnestedDatasetConflictingName(ChildTable.TableName);
			}
			return;
		}
		List<DataTable> list = new List<DataTable>();
		list.Add(ChildTable);
		for (int i = 0; i < list.Count; i++)
		{
			DataRelation[] nestedParentRelations = list[i].NestedParentRelations;
			DataRelation[] array = nestedParentRelations;
			foreach (DataRelation dataRelation in array)
			{
				if (dataRelation.ParentTable == ChildTable && dataRelation.ChildTable != ChildTable)
				{
					throw ExceptionBuilder.LoopInNestedRelations(ChildTable.TableName);
				}
				if (!list.Contains(dataRelation.ParentTable))
				{
					list.Add(dataRelation.ParentTable);
				}
			}
		}
	}

	internal void SetParentKeyConstraint(UniqueConstraint value)
	{
		_parentKeyConstraint = value;
	}

	internal void SetChildKeyConstraint(ForeignKeyConstraint value)
	{
		_childKeyConstraint = value;
	}

	internal void CheckState()
	{
		if (_dataSet != null)
		{
			return;
		}
		_parentKey.CheckState();
		_childKey.CheckState();
		if (_parentKey.Table.DataSet != _childKey.Table.DataSet)
		{
			throw ExceptionBuilder.RelationDataSetMismatch();
		}
		if (_childKey.ColumnsEqual(_parentKey))
		{
			throw ExceptionBuilder.KeyColumnsIdentical();
		}
		for (int i = 0; i < _parentKey.ColumnsReference.Length; i++)
		{
			if (_parentKey.ColumnsReference[i].DataType != _childKey.ColumnsReference[i].DataType || (_parentKey.ColumnsReference[i].DataType == typeof(DateTime) && _parentKey.ColumnsReference[i].DateTimeMode != _childKey.ColumnsReference[i].DateTimeMode && (_parentKey.ColumnsReference[i].DateTimeMode & _childKey.ColumnsReference[i].DateTimeMode) != DataSetDateTime.Unspecified))
			{
				throw ExceptionBuilder.ColumnsTypeMismatch();
			}
		}
	}

	protected void CheckStateForProperty()
	{
		try
		{
			CheckState();
		}
		catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
		{
			throw ExceptionBuilder.BadObjectPropertyAccess(ex.Message);
		}
	}

	private void Create(string relationName, DataColumn[] parentColumns, DataColumn[] childColumns, bool createConstraints)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRelation.Create|INFO> {0}, relationName='{1}', createConstraints={2}", ObjectID, relationName, createConstraints);
		try
		{
			_parentKey = new DataKey(parentColumns, copyColumns: true);
			_childKey = new DataKey(childColumns, copyColumns: true);
			if (parentColumns.Length != childColumns.Length)
			{
				throw ExceptionBuilder.KeyLengthMismatch();
			}
			for (int i = 0; i < parentColumns.Length; i++)
			{
				if (parentColumns[i].Table.DataSet == null || childColumns[i].Table.DataSet == null)
				{
					throw ExceptionBuilder.ParentOrChildColumnsDoNotHaveDataSet();
				}
			}
			CheckState();
			_relationName = ((relationName == null) ? "" : relationName);
			_createConstraints = createConstraints;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal DataRelation Clone(DataSet destination)
	{
		DataCommonEventSource.Log.Trace("<ds.DataRelation.Clone|INFO> {0}, destination={1}", ObjectID, destination?.ObjectID ?? 0);
		DataTable dataTable = destination.Tables[ParentTable.TableName, ParentTable.Namespace];
		DataTable dataTable2 = destination.Tables[ChildTable.TableName, ChildTable.Namespace];
		int num = _parentKey.ColumnsReference.Length;
		DataColumn[] array = new DataColumn[num];
		DataColumn[] array2 = new DataColumn[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = dataTable.Columns[ParentKey.ColumnsReference[i].ColumnName];
			array2[i] = dataTable2.Columns[ChildKey.ColumnsReference[i].ColumnName];
		}
		DataRelation dataRelation = new DataRelation(_relationName, array, array2, createConstraints: false);
		dataRelation.CheckMultipleNested = false;
		dataRelation.Nested = Nested;
		dataRelation.CheckMultipleNested = true;
		if (_extendedProperties != null)
		{
			foreach (object key in _extendedProperties.Keys)
			{
				dataRelation.ExtendedProperties[key] = _extendedProperties[key];
			}
		}
		return dataRelation;
	}

	protected internal void OnPropertyChanging(PropertyChangedEventArgs pcevent)
	{
		if (this.PropertyChanging != null)
		{
			DataCommonEventSource.Log.Trace("<ds.DataRelation.OnPropertyChanging|INFO> {0}", ObjectID);
			this.PropertyChanging(this, pcevent);
		}
	}

	protected internal void RaisePropertyChanging(string name)
	{
		OnPropertyChanging(new PropertyChangedEventArgs(name));
	}

	public override string ToString()
	{
		return RelationName;
	}

	internal void ValidateMultipleNestedRelations()
	{
		if (!Nested || !CheckMultipleNested || ChildTable.NestedParentRelations.Length == 0)
		{
			return;
		}
		DataColumn[] childColumns = ChildColumns;
		if (childColumns.Length != 1 || !IsAutoGenerated(childColumns[0]))
		{
			throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
		}
		if (!XmlTreeGen.AutoGenerated(this))
		{
			throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
		}
		foreach (Constraint constraint in ChildTable.Constraints)
		{
			if (constraint is ForeignKeyConstraint)
			{
				ForeignKeyConstraint fk = (ForeignKeyConstraint)constraint;
				if (!XmlTreeGen.AutoGenerated(fk, checkRelation: true))
				{
					throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
				}
			}
			else
			{
				UniqueConstraint unique = (UniqueConstraint)constraint;
				if (!XmlTreeGen.AutoGenerated(unique))
				{
					throw ExceptionBuilder.TableCantBeNestedInTwoTables(ChildTable.TableName);
				}
			}
		}
	}

	private bool IsAutoGenerated(DataColumn col)
	{
		if (col.ColumnMapping != MappingType.Hidden)
		{
			return false;
		}
		if (col.DataType != typeof(int))
		{
			return false;
		}
		string text = col.Table.TableName + "_Id";
		if (col.ColumnName == text || col.ColumnName == text + "_0")
		{
			return true;
		}
		text = ParentColumnsReference[0].Table.TableName + "_Id";
		if (col.ColumnName == text || col.ColumnName == text + "_0")
		{
			return true;
		}
		return false;
	}
}
