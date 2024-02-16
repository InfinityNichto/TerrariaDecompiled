using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System;

public class UriTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)) && !(sourceType == typeof(Uri)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(Uri)) && !(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
	{
		if (value is string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return new Uri(text, UriKind.RelativeOrAbsolute);
		}
		if (value is Uri uri)
		{
			return new Uri(uri.OriginalString, GetUriKind(uri));
		}
		throw GetConvertFromException(value);
	}

	public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (value is Uri uri)
		{
			if (destinationType == typeof(InstanceDescriptor))
			{
				ConstructorInfo constructor = typeof(Uri).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[2]
				{
					typeof(string),
					typeof(UriKind)
				}, null);
				return new InstanceDescriptor(constructor, new object[2]
				{
					uri.OriginalString,
					GetUriKind(uri)
				});
			}
			if (destinationType == typeof(string))
			{
				return uri.OriginalString;
			}
			if (destinationType == typeof(Uri))
			{
				return new Uri(uri.OriginalString, GetUriKind(uri));
			}
		}
		throw GetConvertToException(value, destinationType);
	}

	public override bool IsValid(ITypeDescriptorContext? context, object? value)
	{
		if (value is string uriString)
		{
			Uri result;
			return Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out result);
		}
		return value is Uri;
	}

	private static UriKind GetUriKind(Uri uri)
	{
		if (!uri.IsAbsoluteUri)
		{
			return UriKind.Relative;
		}
		return UriKind.Absolute;
	}
}
