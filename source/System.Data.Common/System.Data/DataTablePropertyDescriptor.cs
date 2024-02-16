using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class DataTablePropertyDescriptor : PropertyDescriptor
{
	public DataTable Table { get; }

	public override Type ComponentType => typeof(DataRowView);

	public override bool IsReadOnly => false;

	public override Type PropertyType => typeof(IBindingList);

	internal DataTablePropertyDescriptor(DataTable dataTable)
		: base(dataTable.TableName, null)
	{
		Table = dataTable;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (other is DataTablePropertyDescriptor)
		{
			DataTablePropertyDescriptor dataTablePropertyDescriptor = (DataTablePropertyDescriptor)other;
			return dataTablePropertyDescriptor.Table == Table;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Table.GetHashCode();
	}

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override object GetValue(object component)
	{
		DataViewManagerListItemTypeDescriptor dataViewManagerListItemTypeDescriptor = (DataViewManagerListItemTypeDescriptor)component;
		return dataViewManagerListItemTypeDescriptor.GetDataView(Table);
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}
}
