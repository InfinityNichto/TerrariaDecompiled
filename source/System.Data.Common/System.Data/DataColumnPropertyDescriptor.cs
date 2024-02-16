using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class DataColumnPropertyDescriptor : PropertyDescriptor
{
	public override AttributeCollection Attributes
	{
		get
		{
			if (typeof(IList).IsAssignableFrom(PropertyType))
			{
				Attribute[] array = new Attribute[base.Attributes.Count + 1];
				base.Attributes.CopyTo(array, 0);
				array[^1] = new ListBindableAttribute(listBindable: false);
				return new AttributeCollection(array);
			}
			return base.Attributes;
		}
	}

	internal DataColumn Column { get; }

	public override Type ComponentType => typeof(DataRowView);

	public override bool IsReadOnly => Column.ReadOnly;

	public override Type PropertyType => Column.DataType;

	public override bool IsBrowsable
	{
		get
		{
			if (Column.ColumnMapping != MappingType.Hidden)
			{
				return base.IsBrowsable;
			}
			return false;
		}
	}

	internal DataColumnPropertyDescriptor(DataColumn dataColumn)
		: base(dataColumn.ColumnName, null)
	{
		Column = dataColumn;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (other is DataColumnPropertyDescriptor)
		{
			DataColumnPropertyDescriptor dataColumnPropertyDescriptor = (DataColumnPropertyDescriptor)other;
			return dataColumnPropertyDescriptor.Column == Column;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Column.GetHashCode();
	}

	public override bool CanResetValue(object component)
	{
		DataRowView dataRowView = (DataRowView)component;
		if (!Column.IsSqlType)
		{
			return dataRowView.GetColumnValue(Column) != DBNull.Value;
		}
		return !DataStorage.IsObjectNull(dataRowView.GetColumnValue(Column));
	}

	public override object GetValue(object component)
	{
		DataRowView dataRowView = (DataRowView)component;
		return dataRowView.GetColumnValue(Column);
	}

	public override void ResetValue(object component)
	{
		DataRowView dataRowView = (DataRowView)component;
		dataRowView.SetColumnValue(Column, DBNull.Value);
	}

	public override void SetValue(object component, object value)
	{
		DataRowView dataRowView = (DataRowView)component;
		dataRowView.SetColumnValue(Column, value);
		OnValueChanged(component, EventArgs.Empty);
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}
}
