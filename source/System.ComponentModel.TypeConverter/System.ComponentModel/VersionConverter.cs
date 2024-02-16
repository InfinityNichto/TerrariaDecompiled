using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel;

public class VersionConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
	{
		if (!(sourceType == typeof(string)) && !(sourceType == typeof(Version)))
		{
			return base.CanConvertFrom(context, sourceType);
		}
		return true;
	}

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(Version)) && !(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			try
			{
				return Version.Parse(text);
			}
			catch (Exception innerException)
			{
				throw new FormatException(System.SR.Format(System.SR.ConvertInvalidPrimitive, text, "Version"), innerException);
			}
		}
		if (value is Version version)
		{
			return new Version(version.Major, version.Minor, version.Build, version.Revision);
		}
		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (value is Version version)
		{
			if (destinationType == typeof(InstanceDescriptor))
			{
				ConstructorInfo constructor = typeof(Version).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[4]
				{
					typeof(int),
					typeof(int),
					typeof(int),
					typeof(int)
				}, null);
				return new InstanceDescriptor(constructor, new object[4] { version.Major, version.Minor, version.Build, version.Revision });
			}
			if (destinationType == typeof(string))
			{
				return version.ToString();
			}
			if (destinationType == typeof(Version))
			{
				return new Version(version.Major, version.Minor, version.Build, version.Revision);
			}
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	public override bool IsValid(ITypeDescriptorContext? context, object? value)
	{
		if (value is string input)
		{
			Version result;
			return Version.TryParse(input, out result);
		}
		return value is Version;
	}
}
