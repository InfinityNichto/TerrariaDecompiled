using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Security.Authentication.ExtendedProtection;

public class ExtendedProtectionPolicyTypeConverter : TypeConverter
{
	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
	{
		if (!(destinationType == typeof(InstanceDescriptor)))
		{
			return base.CanConvertTo(context, destinationType);
		}
		return true;
	}

	[UnsupportedOSPlatform("browser")]
	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == typeof(InstanceDescriptor) && value is ExtendedProtectionPolicy extendedProtectionPolicy)
		{
			Type[] types;
			object[] arguments;
			if (extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Never)
			{
				types = new Type[1] { typeof(PolicyEnforcement) };
				arguments = new object[1] { PolicyEnforcement.Never };
			}
			else
			{
				types = new Type[3]
				{
					typeof(PolicyEnforcement),
					typeof(ProtectionScenario),
					typeof(ICollection)
				};
				object[] array = null;
				ServiceNameCollection? customServiceNames = extendedProtectionPolicy.CustomServiceNames;
				if (customServiceNames != null && customServiceNames.Count > 0)
				{
					array = new object[extendedProtectionPolicy.CustomServiceNames.Count];
					((ICollection)extendedProtectionPolicy.CustomServiceNames).CopyTo((Array)array, 0);
				}
				arguments = new object[3] { extendedProtectionPolicy.PolicyEnforcement, extendedProtectionPolicy.ProtectionScenario, array };
			}
			ConstructorInfo constructor = typeof(ExtendedProtectionPolicy).GetConstructor(types);
			return new InstanceDescriptor(constructor, arguments);
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}
}
