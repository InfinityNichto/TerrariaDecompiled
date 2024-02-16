using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.ComponentModel;

public class ReferenceConverter : TypeConverter
{
	private struct ReferenceComparer : IComparer<object>
	{
		private readonly ReferenceConverter _converter;

		public ReferenceComparer(ReferenceConverter converter)
		{
			_converter = converter;
		}

		public int Compare(object item1, object item2)
		{
			string strA = _converter.ConvertToString(item1);
			string strB = _converter.ConvertToString(item2);
			return string.Compare(strA, strB, StringComparison.InvariantCulture);
		}
	}

	private static readonly string s_none = System.SR.toStringNone;

	private readonly Type _type;

	public ReferenceConverter(Type type)
	{
		_type = type;
	}

	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (sourceType == typeof(string) && context != null)
		{
			return true;
		}
		return base.CanConvertFrom(context, sourceType);
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			string text2 = text.Trim();
			if (!string.Equals(text2, s_none) && context != null)
			{
				if (context.GetService(typeof(IReferenceService)) is IReferenceService referenceService)
				{
					object reference = referenceService.GetReference(text2);
					if (reference != null)
					{
						return reference;
					}
				}
				IContainer container = context.Container;
				if (container != null)
				{
					object obj = container.Components[text2];
					if (obj != null)
					{
						return obj;
					}
				}
			}
			return null;
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(string))
		{
			if (value != null)
			{
				if (context?.GetService(typeof(IReferenceService)) is IReferenceService referenceService)
				{
					string name = referenceService.GetName(value);
					if (name != null)
					{
						return name;
					}
				}
				if (!Marshal.IsComObject(value) && value is IComponent component)
				{
					string text = component.Site?.Name;
					if (text != null)
					{
						return text;
					}
				}
				return string.Empty;
			}
			return s_none;
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
	{
		List<object> list = null;
		if (context != null)
		{
			list = new List<object> { null };
			if (context.GetService(typeof(IReferenceService)) is IReferenceService referenceService)
			{
				object[] references = referenceService.GetReferences(_type);
				if (references != null)
				{
					for (int i = 0; i < references.Length; i++)
					{
						if (IsValueAllowed(context, references[i]))
						{
							list.Add(references[i]);
						}
					}
				}
			}
			else
			{
				IContainer container = context.Container;
				if (container != null)
				{
					ComponentCollection components = container.Components;
					foreach (IComponent item in components)
					{
						if (item != null && _type != null && _type.IsInstanceOfType(item) && IsValueAllowed(context, item))
						{
							list.Add(item);
						}
					}
				}
			}
			list.Sort(new ReferenceComparer(this));
		}
		return new StandardValuesCollection(list);
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
	{
		return true;
	}

	public override bool GetStandardValuesSupported(ITypeDescriptorContext? context)
	{
		return true;
	}

	protected virtual bool IsValueAllowed(ITypeDescriptorContext context, object value)
	{
		return true;
	}
}
