using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class TypeConverter
{
	protected abstract class SimplePropertyDescriptor : PropertyDescriptor
	{
		public override Type ComponentType { get; }

		public override bool IsReadOnly
		{
			[DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields, typeof(ReadOnlyAttribute))]
			[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The DynamicDependency ensures the correct members are preserved.")]
			get
			{
				return Attributes.Contains(ReadOnlyAttribute.Yes);
			}
		}

		public override Type PropertyType { get; }

		protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType)
			: this(componentType, name, propertyType, Array.Empty<Attribute>())
		{
		}

		protected SimplePropertyDescriptor(Type componentType, string name, Type propertyType, Attribute[]? attributes)
			: base(name, attributes)
		{
			ComponentType = componentType;
			PropertyType = propertyType;
		}

		public override bool CanResetValue(object component)
		{
			return ((DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)])?.Value.Equals(GetValue(component)) ?? false;
		}

		public override void ResetValue(object component)
		{
			DefaultValueAttribute defaultValueAttribute = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
			if (defaultValueAttribute != null)
			{
				SetValue(component, defaultValueAttribute.Value);
			}
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}

	public class StandardValuesCollection : ICollection, IEnumerable
	{
		private readonly ICollection _values;

		private Array _valueArray;

		public int Count
		{
			get
			{
				if (_valueArray != null)
				{
					return _valueArray.Length;
				}
				return _values.Count;
			}
		}

		public object? this[int index]
		{
			get
			{
				if (_valueArray != null)
				{
					return _valueArray.GetValue(index);
				}
				if (_values is IList list)
				{
					return list[index];
				}
				_valueArray = new object[_values.Count];
				_values.CopyTo(_valueArray, 0);
				return _valueArray.GetValue(index);
			}
		}

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => null;

		public StandardValuesCollection(ICollection? values)
		{
			if (values == null)
			{
				values = Array.Empty<object>();
			}
			if (values is Array valueArray)
			{
				_valueArray = valueArray;
			}
			_values = values;
		}

		public void CopyTo(Array array, int index)
		{
			_values.CopyTo(array, index);
		}

		public IEnumerator GetEnumerator()
		{
			return _values.GetEnumerator();
		}
	}

	public bool CanConvertFrom(Type sourceType)
	{
		return CanConvertFrom(null, sourceType);
	}

	public virtual bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		return sourceType == typeof(InstanceDescriptor);
	}

	public bool CanConvertTo(Type destinationType)
	{
		return CanConvertTo(null, destinationType);
	}

	public virtual bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		return destinationType == typeof(string);
	}

	public object? ConvertFrom(object value)
	{
		return ConvertFrom(null, CultureInfo.CurrentCulture, value);
	}

	public virtual object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is InstanceDescriptor instanceDescriptor)
		{
			return instanceDescriptor.Invoke();
		}
		throw GetConvertFromException(value);
	}

	public object? ConvertFromInvariantString(string text)
	{
		return ConvertFromString(null, CultureInfo.InvariantCulture, text);
	}

	public object? ConvertFromInvariantString(ITypeDescriptorContext? context, string text)
	{
		return ConvertFromString(context, CultureInfo.InvariantCulture, text);
	}

	public object? ConvertFromString(string text)
	{
		return ConvertFrom(null, null, text);
	}

	public object? ConvertFromString(ITypeDescriptorContext? context, string text)
	{
		return ConvertFrom(context, CultureInfo.CurrentCulture, text);
	}

	public object? ConvertFromString(ITypeDescriptorContext? context, CultureInfo? culture, string text)
	{
		return ConvertFrom(context, culture, text);
	}

	public object? ConvertTo(object? value, Type destinationType)
	{
		return ConvertTo(null, null, value, destinationType);
	}

	public virtual object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
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
			if (culture != null && culture != CultureInfo.CurrentCulture && value is IFormattable formattable)
			{
				return formattable.ToString(null, culture);
			}
			return value.ToString();
		}
		throw GetConvertToException(value, destinationType);
	}

	public string? ConvertToInvariantString(object? value)
	{
		return ConvertToString(null, CultureInfo.InvariantCulture, value);
	}

	public string? ConvertToInvariantString(ITypeDescriptorContext? context, object? value)
	{
		return ConvertToString(context, CultureInfo.InvariantCulture, value);
	}

	public string? ConvertToString(object? value)
	{
		return (string)ConvertTo(null, CultureInfo.CurrentCulture, value, typeof(string));
	}

	public string? ConvertToString(ITypeDescriptorContext? context, object? value)
	{
		return (string)ConvertTo(context, CultureInfo.CurrentCulture, value, typeof(string));
	}

	public string? ConvertToString(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
	{
		return (string)ConvertTo(context, culture, value, typeof(string));
	}

	public object? CreateInstance(IDictionary propertyValues)
	{
		return CreateInstance(null, propertyValues);
	}

	public virtual object? CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
	{
		return null;
	}

	protected Exception GetConvertFromException(object? value)
	{
		string p = ((value == null) ? System.SR.Null : value.GetType().FullName);
		throw new NotSupportedException(System.SR.Format(System.SR.ConvertFromException, GetType().Name, p));
	}

	protected Exception GetConvertToException(object? value, Type destinationType)
	{
		string p = ((value == null) ? System.SR.Null : value.GetType().FullName);
		throw new NotSupportedException(System.SR.Format(System.SR.ConvertToException, GetType().Name, p, destinationType.FullName));
	}

	public bool GetCreateInstanceSupported()
	{
		return GetCreateInstanceSupported(null);
	}

	public virtual bool GetCreateInstanceSupported(ITypeDescriptorContext? context)
	{
		return false;
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
	public PropertyDescriptorCollection? GetProperties(object value)
	{
		return GetProperties(null, value);
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered.")]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields, typeof(BrowsableAttribute))]
	public PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value)
	{
		return GetProperties(context, value, new Attribute[1] { BrowsableAttribute.Yes });
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public virtual PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
	{
		return null;
	}

	public bool GetPropertiesSupported()
	{
		return GetPropertiesSupported(null);
	}

	public virtual bool GetPropertiesSupported(ITypeDescriptorContext? context)
	{
		return false;
	}

	public ICollection? GetStandardValues()
	{
		return GetStandardValues(null);
	}

	public virtual StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? context)
	{
		return null;
	}

	public bool GetStandardValuesExclusive()
	{
		return GetStandardValuesExclusive(null);
	}

	public virtual bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
	{
		return false;
	}

	public bool GetStandardValuesSupported()
	{
		return GetStandardValuesSupported(null);
	}

	public virtual bool GetStandardValuesSupported(ITypeDescriptorContext? context)
	{
		return false;
	}

	public bool IsValid(object value)
	{
		return IsValid(null, value);
	}

	public virtual bool IsValid(ITypeDescriptorContext? context, object value)
	{
		bool result = true;
		try
		{
			if (value == null || CanConvertFrom(context, value.GetType()))
			{
				ConvertFrom(context, CultureInfo.InvariantCulture, value);
			}
			else
			{
				result = false;
			}
		}
		catch
		{
			result = false;
		}
		return result;
	}

	protected PropertyDescriptorCollection SortProperties(PropertyDescriptorCollection props, string[] names)
	{
		props.Sort(names);
		return props;
	}
}
