using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.Common;

[ListBindable(false)]
[Editor("Microsoft.VSDesigner.Data.Design.DataTableMappingCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public sealed class DataTableMappingCollection : MarshalByRefObject, ITableMappingCollection, IList, ICollection, IEnumerable
{
	private List<DataTableMapping> _items;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	bool IList.IsReadOnly => false;

	bool IList.IsFixedSize => false;

	object? IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			ValidateType(value);
			this[index] = (DataTableMapping)value;
		}
	}

	object ITableMappingCollection.this[string index]
	{
		get
		{
			return this[index];
		}
		set
		{
			ValidateType(value);
			this[index] = (DataTableMapping)value;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int Count
	{
		get
		{
			if (_items == null)
			{
				return 0;
			}
			return _items.Count;
		}
	}

	private Type ItemType => typeof(DataTableMapping);

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DataTableMapping this[int index]
	{
		get
		{
			RangeCheck(index);
			return _items[index];
		}
		set
		{
			RangeCheck(index);
			Replace(index, value);
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DataTableMapping this[string sourceTable]
	{
		get
		{
			int index = RangeCheck(sourceTable);
			return _items[index];
		}
		set
		{
			int index = RangeCheck(sourceTable);
			Replace(index, value);
		}
	}

	ITableMapping ITableMappingCollection.Add(string sourceTableName, string dataSetTableName)
	{
		return Add(sourceTableName, dataSetTableName);
	}

	ITableMapping ITableMappingCollection.GetByDataSetTable(string dataSetTableName)
	{
		return GetByDataSetTable(dataSetTableName);
	}

	public int Add(object? value)
	{
		ValidateType(value);
		Add((DataTableMapping)value);
		return Count - 1;
	}

	private DataTableMapping Add(DataTableMapping value)
	{
		AddWithoutEvents(value);
		return value;
	}

	public void AddRange(DataTableMapping[] values)
	{
		AddEnumerableRange(values, doClone: false);
	}

	public void AddRange(Array values)
	{
		AddEnumerableRange(values, doClone: false);
	}

	private void AddEnumerableRange(IEnumerable values, bool doClone)
	{
		if (values == null)
		{
			throw ADP.ArgumentNull("values");
		}
		foreach (object value2 in values)
		{
			ValidateType(value2);
		}
		if (doClone)
		{
			foreach (ICloneable value3 in values)
			{
				AddWithoutEvents(value3.Clone() as DataTableMapping);
			}
			return;
		}
		foreach (DataTableMapping value4 in values)
		{
			AddWithoutEvents(value4);
		}
	}

	public DataTableMapping Add(string? sourceTable, string? dataSetTable)
	{
		return Add(new DataTableMapping(sourceTable, dataSetTable));
	}

	private void AddWithoutEvents(DataTableMapping value)
	{
		Validate(-1, value);
		value.Parent = this;
		ArrayList().Add(value);
	}

	private List<DataTableMapping> ArrayList()
	{
		return _items ?? (_items = new List<DataTableMapping>());
	}

	public void Clear()
	{
		if (0 < Count)
		{
			ClearWithoutEvents();
		}
	}

	private void ClearWithoutEvents()
	{
		if (_items == null)
		{
			return;
		}
		foreach (DataTableMapping item in _items)
		{
			item.Parent = null;
		}
		_items.Clear();
	}

	public bool Contains(string? value)
	{
		return -1 != IndexOf(value);
	}

	public bool Contains(object? value)
	{
		return -1 != IndexOf(value);
	}

	public void CopyTo(Array array, int index)
	{
		((ICollection)ArrayList()).CopyTo(array, index);
	}

	public void CopyTo(DataTableMapping[] array, int index)
	{
		ArrayList().CopyTo(array, index);
	}

	public DataTableMapping GetByDataSetTable(string dataSetTable)
	{
		int num = IndexOfDataSetTable(dataSetTable);
		if (0 > num)
		{
			throw ADP.TablesDataSetTable(dataSetTable);
		}
		return _items[num];
	}

	public IEnumerator GetEnumerator()
	{
		return ArrayList().GetEnumerator();
	}

	public int IndexOf(object? value)
	{
		if (value != null)
		{
			ValidateType(value);
			for (int i = 0; i < Count; i++)
			{
				if (_items[i] == value)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public int IndexOf(string? sourceTable)
	{
		if (!string.IsNullOrEmpty(sourceTable))
		{
			for (int i = 0; i < Count; i++)
			{
				string sourceTable2 = _items[i].SourceTable;
				if (sourceTable2 != null && ADP.SrcCompare(sourceTable, sourceTable2) == 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public int IndexOfDataSetTable(string? dataSetTable)
	{
		if (!string.IsNullOrEmpty(dataSetTable))
		{
			for (int i = 0; i < Count; i++)
			{
				string dataSetTable2 = _items[i].DataSetTable;
				if (dataSetTable2 != null && ADP.DstCompare(dataSetTable, dataSetTable2) == 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public void Insert(int index, object? value)
	{
		ValidateType(value);
		Insert(index, (DataTableMapping)value);
	}

	public void Insert(int index, DataTableMapping value)
	{
		if (value == null)
		{
			throw ADP.TablesAddNullAttempt("value");
		}
		Validate(-1, value);
		value.Parent = this;
		ArrayList().Insert(index, value);
	}

	private void RangeCheck(int index)
	{
		if (index < 0 || Count <= index)
		{
			throw ADP.TablesIndexInt32(index, this);
		}
	}

	private int RangeCheck(string sourceTable)
	{
		int num = IndexOf(sourceTable);
		if (num < 0)
		{
			throw ADP.TablesSourceIndex(sourceTable);
		}
		return num;
	}

	public void RemoveAt(int index)
	{
		RangeCheck(index);
		RemoveIndex(index);
	}

	public void RemoveAt(string sourceTable)
	{
		int index = RangeCheck(sourceTable);
		RemoveIndex(index);
	}

	private void RemoveIndex(int index)
	{
		_items[index].Parent = null;
		_items.RemoveAt(index);
	}

	public void Remove(object? value)
	{
		ValidateType(value);
		Remove((DataTableMapping)value);
	}

	public void Remove(DataTableMapping value)
	{
		if (value == null)
		{
			throw ADP.TablesAddNullAttempt("value");
		}
		int num = IndexOf(value);
		if (-1 != num)
		{
			RemoveIndex(num);
			return;
		}
		throw ADP.CollectionRemoveInvalidObject(ItemType, this);
	}

	private void Replace(int index, DataTableMapping newValue)
	{
		Validate(index, newValue);
		_items[index].Parent = null;
		newValue.Parent = this;
		_items[index] = newValue;
	}

	private void ValidateType([NotNull] object value)
	{
		if (value == null)
		{
			throw ADP.TablesAddNullAttempt("value");
		}
		if (!ItemType.IsInstanceOfType(value))
		{
			throw ADP.NotADataTableMapping(value);
		}
	}

	private void Validate(int index, [NotNull] DataTableMapping value)
	{
		if (value == null)
		{
			throw ADP.TablesAddNullAttempt("value");
		}
		if (value.Parent != null)
		{
			if (this != value.Parent)
			{
				throw ADP.TablesIsNotParent(this);
			}
			if (index != IndexOf(value))
			{
				throw ADP.TablesIsParent(this);
			}
		}
		string sourceTable = value.SourceTable;
		if (string.IsNullOrEmpty(sourceTable))
		{
			index = 1;
			do
			{
				sourceTable = "SourceTable" + index.ToString(CultureInfo.InvariantCulture);
				index++;
			}
			while (-1 != IndexOf(sourceTable));
			value.SourceTable = sourceTable;
		}
		else
		{
			ValidateSourceTable(index, sourceTable);
		}
	}

	internal void ValidateSourceTable(int index, string value)
	{
		int num = IndexOf(value);
		if (-1 != num && index != num)
		{
			throw ADP.TablesUniqueSourceTable(value);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static DataTableMapping? GetTableMappingBySchemaAction(DataTableMappingCollection? tableMappings, string sourceTable, string dataSetTable, MissingMappingAction mappingAction)
	{
		if (tableMappings != null)
		{
			int num = tableMappings.IndexOf(sourceTable);
			if (-1 != num)
			{
				return tableMappings._items[num];
			}
		}
		if (string.IsNullOrEmpty(sourceTable))
		{
			throw ADP.InvalidSourceTable("sourceTable");
		}
		return mappingAction switch
		{
			MissingMappingAction.Passthrough => new DataTableMapping(sourceTable, dataSetTable), 
			MissingMappingAction.Ignore => null, 
			MissingMappingAction.Error => throw ADP.MissingTableMapping(sourceTable), 
			_ => throw ADP.InvalidMissingMappingAction(mappingAction), 
		};
	}
}
