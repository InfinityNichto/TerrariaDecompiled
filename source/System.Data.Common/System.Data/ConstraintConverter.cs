using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.Data;

internal sealed class ConstraintConverter : ExpandableObjectConverter
{
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == typeof(InstanceDescriptor) && value is Constraint)
		{
			if (value is UniqueConstraint)
			{
				UniqueConstraint uniqueConstraint = (UniqueConstraint)value;
				ConstructorInfo constructor = typeof(UniqueConstraint).GetConstructor(new Type[3]
				{
					typeof(string),
					typeof(string[]),
					typeof(bool)
				});
				if (constructor != null)
				{
					return new InstanceDescriptor(constructor, new object[3] { uniqueConstraint.ConstraintName, uniqueConstraint.ColumnNames, uniqueConstraint.IsPrimaryKey });
				}
			}
			else
			{
				ForeignKeyConstraint foreignKeyConstraint = (ForeignKeyConstraint)value;
				ConstructorInfo constructor2 = typeof(ForeignKeyConstraint).GetConstructor(new Type[7]
				{
					typeof(string),
					typeof(string),
					typeof(string[]),
					typeof(string[]),
					typeof(AcceptRejectRule),
					typeof(Rule),
					typeof(Rule)
				});
				if (constructor2 != null)
				{
					return new InstanceDescriptor(constructor2, new object[7]
					{
						foreignKeyConstraint.ConstraintName,
						foreignKeyConstraint.ParentKey.Table.TableName,
						foreignKeyConstraint.ParentColumnNames,
						foreignKeyConstraint.ChildColumnNames,
						foreignKeyConstraint.AcceptRejectRule,
						foreignKeyConstraint.DeleteRule,
						foreignKeyConstraint.UpdateRule
					});
				}
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
