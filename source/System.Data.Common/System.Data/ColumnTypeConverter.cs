using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Data;

internal sealed class ColumnTypeConverter : TypeConverter
{
	private static readonly Type[] s_types = new Type[35]
	{
		typeof(bool),
		typeof(byte),
		typeof(byte[]),
		typeof(char),
		typeof(DateTime),
		typeof(decimal),
		typeof(double),
		typeof(Guid),
		typeof(short),
		typeof(int),
		typeof(long),
		typeof(object),
		typeof(sbyte),
		typeof(float),
		typeof(string),
		typeof(TimeSpan),
		typeof(ushort),
		typeof(uint),
		typeof(ulong),
		typeof(SqlInt16),
		typeof(SqlInt32),
		typeof(SqlInt64),
		typeof(SqlDecimal),
		typeof(SqlSingle),
		typeof(SqlDouble),
		typeof(SqlString),
		typeof(SqlBoolean),
		typeof(SqlBinary),
		typeof(SqlByte),
		typeof(SqlDateTime),
		typeof(SqlGuid),
		typeof(SqlMoney),
		typeof(SqlBytes),
		typeof(SqlChars),
		typeof(SqlXml)
	};

	private StandardValuesCollection _values;

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "InstanceDescriptor calls GetType(string) on AssemblyQualifiedName of instance of type we already have in here.")]
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (destinationType == typeof(string))
		{
			if (value == null)
			{
				return string.Empty;
			}
			return value.ToString();
		}
		if (value != null && destinationType == typeof(InstanceDescriptor))
		{
			object obj = value;
			if (value is string)
			{
				for (int i = 0; i < s_types.Length; i++)
				{
					if (s_types[i].ToString().Equals(value))
					{
						obj = s_types[i];
					}
				}
			}
			if (value is Type || value is string)
			{
				MethodInfo method = typeof(Type).GetMethod("GetType", new Type[1] { typeof(string) });
				if (method != null)
				{
					return new InstanceDescriptor(method, new object[1] { ((Type)obj).AssemblyQualifiedName });
				}
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		if (!(sourceType == typeof(string)))
		{
			return base.CanConvertTo(context, sourceType);
		}
		return true;
	}

	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if (value != null && value.GetType() == typeof(string))
		{
			for (int i = 0; i < s_types.Length; i++)
			{
				if (s_types[i].ToString().Equals(value))
				{
					return s_types[i];
				}
			}
			return typeof(string);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		if (_values == null)
		{
			object[] array;
			if (s_types != null)
			{
				array = new object[s_types.Length];
				Array.Copy(s_types, array, s_types.Length);
			}
			else
			{
				array = null;
			}
			_values = new StandardValuesCollection(array);
		}
		return _values;
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
	{
		return true;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}
}
