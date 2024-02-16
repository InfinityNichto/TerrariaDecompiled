using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.Common;

public sealed class DataColumnMappingCollection : MarshalByRefObject, IColumnMappingCollection, IList, ICollection, IEnumerable
{
	private List<DataColumnMapping> _items;

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
			this[index] = (DataColumnMapping)value;
		}
	}

	object IColumnMappingCollection.this[string index]
	{
		get
		{
			return this[index];
		}
		set
		{
			ValidateType(value);
			this[index] = (DataColumnMapping)value;
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

	private Type ItemType => typeof(DataColumnMapping);

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DataColumnMapping this[int index]
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
	public DataColumnMapping this[string sourceColumn]
	{
		get
		{
			int index = RangeCheck(sourceColumn);
			return _items[index];
		}
		set
		{
			int index = RangeCheck(sourceColumn);
			Replace(index, value);
		}
	}

	IColumnMapping IColumnMappingCollection.Add(string sourceColumnName, string dataSetColumnName)
	{
		return Add(sourceColumnName, dataSetColumnName);
	}

	IColumnMapping IColumnMappingCollection.GetByDataSetColumn(string dataSetColumnName)
	{
		return GetByDataSetColumn(dataSetColumnName);
	}

	public int Add(object? value)
	{
		ValidateType(value);
		Add((DataColumnMapping)value);
		return Count - 1;
	}

	private DataColumnMapping Add(DataColumnMapping value)
	{
		AddWithoutEvents(value);
		return value;
	}

	public DataColumnMapping Add(string? sourceColumn, string? dataSetColumn)
	{
		return Add(new DataColumnMapping(sourceColumn, dataSetColumn));
	}

	public void AddRange(DataColumnMapping[] values)
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
				AddWithoutEvents(value3.Clone() as DataColumnMapping);
			}
			return;
		}
		foreach (DataColumnMapping value4 in values)
		{
			AddWithoutEvents(value4);
		}
	}

	private void AddWithoutEvents([NotNull] DataColumnMapping value)
	{
		Validate(-1, value);
		value.Parent = this;
		ArrayList().Add(value);
	}

	private List<DataColumnMapping> ArrayList()
	{
		if (_items == null)
		{
			_items = new List<DataColumnMapping>();
		}
		return _items;
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
		foreach (DataColumnMapping item in _items)
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

	public void CopyTo(DataColumnMapping[] array, int index)
	{
		ArrayList().CopyTo(array, index);
	}

	public DataColumnMapping GetByDataSetColumn(string value)
	{
		int num = IndexOfDataSetColumn(value);
		if (0 > num)
		{
			throw ADP.ColumnsDataSetColumn(value);
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

	public int IndexOf(string? sourceColumn)
	{
		if (!string.IsNullOrEmpty(sourceColumn))
		{
			int count = Count;
			for (int i = 0; i < count; i++)
			{
				if (ADP.SrcCompare(sourceColumn, _items[i].SourceColumn) == 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public int IndexOfDataSetColumn(string? dataSetColumn)
	{
		if (!string.IsNullOrEmpty(dataSetColumn))
		{
			int count = Count;
			for (int i = 0; i < count; i++)
			{
				if (ADP.DstCompare(dataSetColumn, _items[i].DataSetColumn) == 0)
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
		Insert(index, (DataColumnMapping)value);
	}

	public void Insert(int index, DataColumnMapping value)
	{
		if (value == null)
		{
			throw ADP.ColumnsAddNullAttempt("value");
		}
		Validate(-1, value);
		value.Parent = this;
		ArrayList().Insert(index, value);
	}

	private void RangeCheck(int index)
	{
		if (index < 0 || Count <= index)
		{
			throw ADP.ColumnsIndexInt32(index, this);
		}
	}

	private int RangeCheck(string sourceColumn)
	{
		int num = IndexOf(sourceColumn);
		if (num < 0)
		{
			throw ADP.ColumnsIndexSource(sourceColumn);
		}
		return num;
	}

	public void RemoveAt(int index)
	{
		RangeCheck(index);
		RemoveIndex(index);
	}

	public void RemoveAt(string sourceColumn)
	{
		int index = RangeCheck(sourceColumn);
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
		Remove((DataColumnMapping)value);
	}

	public void Remove(DataColumnMapping value)
	{
		if (value == null)
		{
			throw ADP.ColumnsAddNullAttempt("value");
		}
		int num = IndexOf(value);
		if (-1 != num)
		{
			RemoveIndex(num);
			return;
		}
		throw ADP.CollectionRemoveInvalidObject(ItemType, this);
	}

	private void Replace(int index, DataColumnMapping newValue)
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
			throw ADP.ColumnsAddNullAttempt("value");
		}
		if (!ItemType.IsInstanceOfType(value))
		{
			throw ADP.NotADataColumnMapping(value);
		}
	}

	private void Validate(int index, [NotNull] DataColumnMapping value)
	{
		if (value == null)
		{
			throw ADP.ColumnsAddNullAttempt("value");
		}
		if (value.Parent != null)
		{
			if (this != value.Parent)
			{
				throw ADP.ColumnsIsNotParent(this);
			}
			if (index != IndexOf(value))
			{
				throw ADP.ColumnsIsParent(this);
			}
		}
		string sourceColumn = value.SourceColumn;
		if (string.IsNullOrEmpty(sourceColumn))
		{
			index = 1;
			do
			{
				sourceColumn = "SourceColumn" + index.ToString(CultureInfo.InvariantCulture);
				index++;
			}
			while (-1 != IndexOf(sourceColumn));
			value.SourceColumn = sourceColumn;
		}
		else
		{
			ValidateSourceColumn(index, sourceColumn);
		}
	}

	internal void ValidateSourceColumn(int index, string value)
	{
		int num = IndexOf(value);
		if (-1 != num && index != num)
		{
			throw ADP.ColumnsUniqueSourceColumn(value);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static DataColumn? GetDataColumn(DataColumnMappingCollection? columnMappings, string sourceColumn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type? dataType, DataTable dataTable, MissingMappingAction mappingAction, MissingSchemaAction schemaAction)
	{
		if (columnMappings != null)
		{
			int num = columnMappings.IndexOf(sourceColumn);
			if (-1 != num)
			{
				return columnMappings._items[num].GetDataColumnBySchemaAction(dataTable, dataType, schemaAction);
			}
		}
		if (string.IsNullOrEmpty(sourceColumn))
		{
			throw ADP.InvalidSourceColumn("sourceColumn");
		}
		return mappingAction switch
		{
			MissingMappingAction.Passthrough => DataColumnMapping.GetDataColumnBySchemaAction(sourceColumn, sourceColumn, dataTable, dataType, schemaAction), 
			MissingMappingAction.Ignore => null, 
			MissingMappingAction.Error => throw ADP.MissingColumnMapping(sourceColumn), 
			_ => throw ADP.InvalidMissingMappingAction(mappingAction), 
		};
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static DataColumnMapping? GetColumnMappingBySchemaAction(DataColumnMappingCollection? columnMappings, string sourceColumn, MissingMappingAction mappingAction)
	{
		if (columnMappings != null)
		{
			int num = columnMappings.IndexOf(sourceColumn);
			if (-1 != num)
			{
				return columnMappings._items[num];
			}
		}
		if (string.IsNullOrEmpty(sourceColumn))
		{
			throw ADP.InvalidSourceColumn("sourceColumn");
		}
		return mappingAction switch
		{
			MissingMappingAction.Passthrough => new DataColumnMapping(sourceColumn, sourceColumn), 
			MissingMappingAction.Ignore => null, 
			MissingMappingAction.Error => throw ADP.MissingColumnMapping(sourceColumn), 
			_ => throw ADP.InvalidMissingMappingAction(mappingAction), 
		};
	}
}
