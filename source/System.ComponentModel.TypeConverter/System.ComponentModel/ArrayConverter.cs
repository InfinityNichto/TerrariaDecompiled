using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel;

public class ArrayConverter : CollectionConverter
{
	private sealed class ArrayPropertyDescriptor : SimplePropertyDescriptor
	{
		private readonly int _index;

		public ArrayPropertyDescriptor(Type arrayType, Type elementType, int index)
			: base(arrayType, "[" + index + "]", elementType, null)
		{
			_index = index;
		}

		public override object GetValue(object instance)
		{
			if (instance is Array array && array.GetLength(0) > _index)
			{
				return array.GetValue(_index);
			}
			return null;
		}

		public override void SetValue(object instance, object value)
		{
			if (instance is Array array)
			{
				if (array.GetLength(0) > _index)
				{
					array.SetValue(value, _index);
				}
				OnValueChanged(instance, EventArgs.Empty);
			}
		}
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string) && value is Array)
		{
			return System.SR.Format(System.SR.Array, value.GetType().Name);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	[RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	[return: NotNullIfNotNull("value")]
	public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
	{
		if (value == null)
		{
			return null;
		}
		if (!(value is Array array))
		{
			return new PropertyDescriptorCollection(null);
		}
		int length = array.GetLength(0);
		PropertyDescriptor[] array2 = new PropertyDescriptor[length];
		Type type = value.GetType();
		Type elementType = type.GetElementType();
		for (int i = 0; i < length; i++)
		{
			array2[i] = new ArrayPropertyDescriptor(type, elementType, i);
		}
		return new PropertyDescriptorCollection(array2);
	}

	public override bool GetPropertiesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}
}
