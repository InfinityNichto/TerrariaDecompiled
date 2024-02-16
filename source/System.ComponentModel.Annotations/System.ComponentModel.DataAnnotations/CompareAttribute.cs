using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CompareAttribute : ValidationAttribute
{
	public string OtherProperty { get; }

	public string? OtherPropertyDisplayName { get; internal set; }

	public override bool RequiresValidationContext => true;

	[RequiresUnreferencedCode("The property referenced by 'otherProperty' may be trimmed. Ensure it is preserved.")]
	public CompareAttribute(string otherProperty)
		: base(System.SR.CompareAttribute_MustMatch)
	{
		OtherProperty = otherProperty ?? throw new ArgumentNullException("otherProperty");
	}

	public override string FormatErrorMessage(string name)
	{
		return string.Format(CultureInfo.CurrentCulture, base.ErrorMessageString, name, OtherPropertyDisplayName ?? OtherProperty);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "The ctor is marked with RequiresUnreferencedCode informing the caller to preserve the other property.")]
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		PropertyInfo runtimeProperty = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
		if (runtimeProperty == null)
		{
			return new ValidationResult(System.SR.Format(System.SR.CompareAttribute_UnknownProperty, OtherProperty));
		}
		if (runtimeProperty.GetIndexParameters().Length != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Common_PropertyNotFound, validationContext.ObjectType.FullName, OtherProperty));
		}
		object value2 = runtimeProperty.GetValue(validationContext.ObjectInstance, null);
		if (!object.Equals(value, value2))
		{
			if (OtherPropertyDisplayName == null)
			{
				OtherPropertyDisplayName = GetDisplayNameForProperty(runtimeProperty);
			}
			string[] memberNames = ((validationContext.MemberName == null) ? null : new string[1] { validationContext.MemberName });
			return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
		}
		return null;
	}

	private string GetDisplayNameForProperty(PropertyInfo property)
	{
		IEnumerable<Attribute> customAttributes = CustomAttributeExtensions.GetCustomAttributes(property, inherit: true);
		foreach (Attribute item in customAttributes)
		{
			if (item is DisplayAttribute displayAttribute)
			{
				return displayAttribute.GetName();
			}
		}
		return OtherProperty;
	}
}
