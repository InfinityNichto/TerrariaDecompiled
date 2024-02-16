using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

public class DataRowView : ICustomTypeDescriptor, IEditableObject, IDataErrorInfo, INotifyPropertyChanged
{
	private readonly DataView _dataView;

	private readonly DataRow _row;

	private bool _delayBeginEdit;

	private static readonly PropertyDescriptorCollection s_zeroPropertyDescriptorCollection = new PropertyDescriptorCollection(null);

	public DataView DataView => _dataView;

	public object this[int ndx]
	{
		get
		{
			return Row[ndx, RowVersionDefault];
		}
		[param: AllowNull]
		set
		{
			if (!_dataView.AllowEdit && !IsNew)
			{
				throw ExceptionBuilder.CanNotEdit();
			}
			SetColumnValue(_dataView.Table.Columns[ndx], value);
		}
	}

	public object this[string property]
	{
		get
		{
			DataColumn dataColumn = _dataView.Table.Columns[property];
			if (dataColumn != null)
			{
				return Row[dataColumn, RowVersionDefault];
			}
			if (_dataView.Table.DataSet != null && _dataView.Table.DataSet.Relations.Contains(property))
			{
				return CreateChildView(property);
			}
			throw ExceptionBuilder.PropertyNotFound(property, _dataView.Table.TableName);
		}
		[param: AllowNull]
		set
		{
			DataColumn dataColumn = _dataView.Table.Columns[property];
			if (dataColumn == null)
			{
				throw ExceptionBuilder.SetFailed(property);
			}
			if (!_dataView.AllowEdit && !IsNew)
			{
				throw ExceptionBuilder.CanNotEdit();
			}
			SetColumnValue(dataColumn, value);
		}
	}

	string IDataErrorInfo.this[string colName] => Row.GetColumnError(colName);

	string IDataErrorInfo.Error => Row.RowError;

	public DataRowVersion RowVersion => RowVersionDefault & (DataRowVersion)(-1025);

	private DataRowVersion RowVersionDefault => Row.GetDefaultRowVersion(_dataView.RowStateFilter);

	public DataRow Row => _row;

	public bool IsNew => _row == _dataView._addNewRow;

	public bool IsEdit
	{
		get
		{
			if (!Row.HasVersion(DataRowVersion.Proposed))
			{
				return _delayBeginEdit;
			}
			return true;
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	internal DataRowView(DataView dataView, DataRow row)
	{
		_dataView = dataView;
		_row = row;
	}

	public override bool Equals(object? other)
	{
		return this == other;
	}

	public override int GetHashCode()
	{
		return Row.GetHashCode();
	}

	internal int GetRecord()
	{
		return Row.GetRecordFromVersion(RowVersionDefault);
	}

	internal bool HasRecord()
	{
		return Row.HasVersion(RowVersionDefault);
	}

	internal object GetColumnValue(DataColumn column)
	{
		return Row[column, RowVersionDefault];
	}

	internal void SetColumnValue(DataColumn column, object value)
	{
		if (_delayBeginEdit)
		{
			_delayBeginEdit = false;
			Row.BeginEdit();
		}
		if (DataRowVersion.Original == RowVersionDefault)
		{
			throw ExceptionBuilder.SetFailed(column.ColumnName);
		}
		Row[column] = value;
	}

	public DataView CreateChildView(DataRelation relation, bool followParent)
	{
		if (relation == null || relation.ParentKey.Table != DataView.Table)
		{
			throw ExceptionBuilder.CreateChildView();
		}
		RelatedView relatedView;
		if (!followParent)
		{
			int record = GetRecord();
			object[] keyValues = relation.ParentKey.GetKeyValues(record);
			relatedView = new RelatedView(relation.ChildColumnsReference, keyValues);
		}
		else
		{
			relatedView = new RelatedView(this, relation.ParentKey, relation.ChildColumnsReference);
		}
		relatedView.SetIndex("", DataViewRowState.CurrentRows, null);
		relatedView.SetDataViewManager(DataView.DataViewManager);
		return relatedView;
	}

	public DataView CreateChildView(DataRelation relation)
	{
		return CreateChildView(relation, followParent: false);
	}

	public DataView CreateChildView(string relationName, bool followParent)
	{
		return CreateChildView(DataView.Table.ChildRelations[relationName], followParent);
	}

	public DataView CreateChildView(string relationName)
	{
		return CreateChildView(relationName, followParent: false);
	}

	public void BeginEdit()
	{
		_delayBeginEdit = true;
	}

	public void CancelEdit()
	{
		DataRow row = Row;
		if (IsNew)
		{
			_dataView.FinishAddNew(success: false);
		}
		else
		{
			row.CancelEdit();
		}
		_delayBeginEdit = false;
	}

	public void EndEdit()
	{
		if (IsNew)
		{
			_dataView.FinishAddNew(success: true);
		}
		else
		{
			Row.EndEdit();
		}
		_delayBeginEdit = false;
	}

	public void Delete()
	{
		_dataView.Delete(Row);
	}

	internal void RaisePropertyChangedEvent(string propName)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
	}

	AttributeCollection ICustomTypeDescriptor.GetAttributes()
	{
		return new AttributeCollection((Attribute[]?)null);
	}

	string ICustomTypeDescriptor.GetClassName()
	{
		return null;
	}

	string ICustomTypeDescriptor.GetComponentName()
	{
		return null;
	}

	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	TypeConverter ICustomTypeDescriptor.GetConverter()
	{
		return null;
	}

	[RequiresUnreferencedCode("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
	EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
	{
		return null;
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
	{
		return null;
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
	object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
	{
		return null;
	}

	EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
	{
		return new EventDescriptorCollection(null);
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
	{
		return new EventDescriptorCollection(null);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
	{
		return ((ICustomTypeDescriptor)this).GetProperties((Attribute[]?)null);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
	{
		if (_dataView.Table == null)
		{
			return s_zeroPropertyDescriptorCollection;
		}
		return _dataView.Table.GetPropertyDescriptorCollection(attributes);
	}

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
	{
		return this;
	}
}
