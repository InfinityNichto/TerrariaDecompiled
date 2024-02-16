using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class DataViewManagerListItemTypeDescriptor : ICustomTypeDescriptor
{
	private readonly DataViewManager _dataViewManager;

	private PropertyDescriptorCollection _propsCollection;

	internal DataViewManagerListItemTypeDescriptor(DataViewManager dataViewManager)
	{
		_dataViewManager = dataViewManager;
	}

	internal void Reset()
	{
		_propsCollection = null;
	}

	internal DataView GetDataView(DataTable table)
	{
		DataView dataView = new DataView(table);
		dataView.SetDataViewManager(_dataViewManager);
		return dataView;
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
		return GetPropertiesInternal();
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
	{
		return GetPropertiesInternal();
	}

	internal PropertyDescriptorCollection GetPropertiesInternal()
	{
		if (_propsCollection == null)
		{
			PropertyDescriptor[] array = null;
			DataSet dataSet = _dataViewManager.DataSet;
			if (dataSet != null)
			{
				int count = dataSet.Tables.Count;
				array = new PropertyDescriptor[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = new DataTablePropertyDescriptor(dataSet.Tables[i]);
				}
			}
			_propsCollection = new PropertyDescriptorCollection(array);
		}
		return _propsCollection;
	}

	object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
	{
		return this;
	}
}
