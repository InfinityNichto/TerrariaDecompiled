using System.Resources;
using FxResources.System.ComponentModel.Annotations;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentIsNullOrWhitespace => GetResourceString("ArgumentIsNullOrWhitespace", "The argument '{0}' cannot be null, empty or contain only whitespace.");

	internal static string AssociatedMetadataTypeTypeDescriptor_MetadataTypeContainsUnknownProperties => GetResourceString("AssociatedMetadataTypeTypeDescriptor_MetadataTypeContainsUnknownProperties", "The associated metadata type for type '{0}' contains the following unknown properties or fields: {1}. Please make sure that the names of these members match the names of the properties on the main type.");

	internal static string AttributeStore_Unknown_Property => GetResourceString("AttributeStore_Unknown_Property", "The type '{0}' does not contain a public property named '{1}'.");

	internal static string Common_PropertyNotFound => GetResourceString("Common_PropertyNotFound", "The property {0}.{1} could not be found.");

	internal static string CompareAttribute_MustMatch => GetResourceString("CompareAttribute_MustMatch", "'{0}' and '{1}' do not match.");

	internal static string CompareAttribute_UnknownProperty => GetResourceString("CompareAttribute_UnknownProperty", "Could not find a property named {0}.");

	internal static string CreditCardAttribute_Invalid => GetResourceString("CreditCardAttribute_Invalid", "The {0} field is not a valid credit card number.");

	internal static string CustomValidationAttribute_Method_Must_Return_ValidationResult => GetResourceString("CustomValidationAttribute_Method_Must_Return_ValidationResult", "The CustomValidationAttribute method '{0}' in type '{1}' must return System.ComponentModel.DataAnnotations.ValidationResult.  Use System.ComponentModel.DataAnnotations.ValidationResult.Success to represent success.");

	internal static string CustomValidationAttribute_Method_Not_Found => GetResourceString("CustomValidationAttribute_Method_Not_Found", "The CustomValidationAttribute method '{0}' does not exist in type '{1}' or is not public and static.");

	internal static string CustomValidationAttribute_Method_Required => GetResourceString("CustomValidationAttribute_Method_Required", "The CustomValidationAttribute.Method was not specified.");

	internal static string CustomValidationAttribute_Method_Signature => GetResourceString("CustomValidationAttribute_Method_Signature", "The CustomValidationAttribute method '{0}' in type '{1}' must match the expected signature: public static ValidationResult {0}(object value, ValidationContext context).  The value can be strongly typed.  The ValidationContext parameter is optional.");

	internal static string CustomValidationAttribute_Type_Conversion_Failed => GetResourceString("CustomValidationAttribute_Type_Conversion_Failed", "Could not convert the value of type '{0}' to '{1}' as expected by method {2}.{3}.");

	internal static string CustomValidationAttribute_Type_Must_Be_Public => GetResourceString("CustomValidationAttribute_Type_Must_Be_Public", "The custom validation type '{0}' must be public.");

	internal static string CustomValidationAttribute_ValidationError => GetResourceString("CustomValidationAttribute_ValidationError", "{0} is not valid.");

	internal static string CustomValidationAttribute_ValidatorType_Required => GetResourceString("CustomValidationAttribute_ValidatorType_Required", "The CustomValidationAttribute.ValidatorType was not specified.");

	internal static string DataTypeAttribute_EmptyDataTypeString => GetResourceString("DataTypeAttribute_EmptyDataTypeString", "The custom DataType string cannot be null or empty.");

	internal static string DisplayAttribute_PropertyNotSet => GetResourceString("DisplayAttribute_PropertyNotSet", "The {0} property has not been set.  Use the {1} method to get the value.");

	internal static string EmailAddressAttribute_Invalid => GetResourceString("EmailAddressAttribute_Invalid", "The {0} field is not a valid e-mail address.");

	internal static string EnumDataTypeAttribute_TypeCannotBeNull => GetResourceString("EnumDataTypeAttribute_TypeCannotBeNull", "The type provided for EnumDataTypeAttribute cannot be null.");

	internal static string EnumDataTypeAttribute_TypeNeedsToBeAnEnum => GetResourceString("EnumDataTypeAttribute_TypeNeedsToBeAnEnum", "The type '{0}' needs to represent an enumeration type.");

	internal static string FileExtensionsAttribute_Invalid => GetResourceString("FileExtensionsAttribute_Invalid", "The {0} field only accepts files with the following extensions: {1}");

	internal static string LocalizableString_LocalizationFailed => GetResourceString("LocalizableString_LocalizationFailed", "Cannot retrieve property '{0}' because localization failed.  Type '{1}' is not public or does not contain a public static string property with the name '{2}'.");

	internal static string MaxLengthAttribute_InvalidMaxLength => GetResourceString("MaxLengthAttribute_InvalidMaxLength", "MaxLengthAttribute must have a Length value that is greater than zero. Use MaxLength() without parameters to indicate that the string or array can have the maximum allowable length.");

	internal static string MaxLengthAttribute_ValidationError => GetResourceString("MaxLengthAttribute_ValidationError", "The field {0} must be a string or array type with a maximum length of '{1}'.");

	internal static string MetadataTypeAttribute_TypeCannotBeNull => GetResourceString("MetadataTypeAttribute_TypeCannotBeNull", "MetadataClassType cannot be null.");

	internal static string MinLengthAttribute_InvalidMinLength => GetResourceString("MinLengthAttribute_InvalidMinLength", "MinLengthAttribute must have a Length value that is zero or greater.");

	internal static string MinLengthAttribute_ValidationError => GetResourceString("MinLengthAttribute_ValidationError", "The field {0} must be a string or array type with a minimum length of '{1}'.");

	internal static string LengthAttribute_InvalidValueType => GetResourceString("LengthAttribute_InvalidValueType", "The field of type {0} must be a string, array or ICollection type.");

	internal static string PhoneAttribute_Invalid => GetResourceString("PhoneAttribute_Invalid", "The {0} field is not a valid phone number.");

	internal static string RangeAttribute_ArbitraryTypeNotIComparable => GetResourceString("RangeAttribute_ArbitraryTypeNotIComparable", "The type {0} must implement {1}.");

	internal static string RangeAttribute_MinGreaterThanMax => GetResourceString("RangeAttribute_MinGreaterThanMax", "The maximum value '{0}' must be greater than or equal to the minimum value '{1}'.");

	internal static string RangeAttribute_Must_Set_Min_And_Max => GetResourceString("RangeAttribute_Must_Set_Min_And_Max", "The minimum and maximum values must be set.");

	internal static string RangeAttribute_Must_Set_Operand_Type => GetResourceString("RangeAttribute_Must_Set_Operand_Type", "The OperandType must be set when strings are used for minimum and maximum values.");

	internal static string RangeAttribute_ValidationError => GetResourceString("RangeAttribute_ValidationError", "The field {0} must be between {1} and {2}.");

	internal static string RegexAttribute_ValidationError => GetResourceString("RegexAttribute_ValidationError", "The field {0} must match the regular expression '{1}'.");

	internal static string RegularExpressionAttribute_Empty_Pattern => GetResourceString("RegularExpressionAttribute_Empty_Pattern", "The pattern must be set to a valid regular expression.");

	internal static string RequiredAttribute_ValidationError => GetResourceString("RequiredAttribute_ValidationError", "The {0} field is required.");

	internal static string StringLengthAttribute_InvalidMaxLength => GetResourceString("StringLengthAttribute_InvalidMaxLength", "The maximum length must be a nonnegative integer.");

	internal static string StringLengthAttribute_ValidationError => GetResourceString("StringLengthAttribute_ValidationError", "The field {0} must be a string with a maximum length of {1}.");

	internal static string StringLengthAttribute_ValidationErrorIncludingMinimum => GetResourceString("StringLengthAttribute_ValidationErrorIncludingMinimum", "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.");

	internal static string UIHintImplementation_ControlParameterKeyIsNotAString => GetResourceString("UIHintImplementation_ControlParameterKeyIsNotAString", "The key parameter at position {0} with value '{1}' is not a string. Every key control parameter must be a string.");

	internal static string UIHintImplementation_ControlParameterKeyIsNull => GetResourceString("UIHintImplementation_ControlParameterKeyIsNull", "The key parameter at position {0} is null. Every key control parameter must be a string.");

	internal static string UIHintImplementation_ControlParameterKeyOccursMoreThanOnce => GetResourceString("UIHintImplementation_ControlParameterKeyOccursMoreThanOnce", "The key parameter at position {0} with value '{1}' occurs more than once.");

	internal static string UIHintImplementation_NeedEvenNumberOfControlParameters => GetResourceString("UIHintImplementation_NeedEvenNumberOfControlParameters", "The number of control parameters must be even.");

	internal static string UrlAttribute_Invalid => GetResourceString("UrlAttribute_Invalid", "The {0} field is not a valid fully-qualified http, https, or ftp URL.");

	internal static string ValidationAttribute_Cannot_Set_ErrorMessage_And_Resource => GetResourceString("ValidationAttribute_Cannot_Set_ErrorMessage_And_Resource", "Either ErrorMessageString or ErrorMessageResourceName must be set, but not both.");

	internal static string ValidationAttribute_IsValid_NotImplemented => GetResourceString("ValidationAttribute_IsValid_NotImplemented", "IsValid(object value) has not been implemented by this class.  The preferred entry point is GetValidationResult() and classes should override IsValid(object value, ValidationContext context).");

	internal static string ValidationAttribute_NeedBothResourceTypeAndResourceName => GetResourceString("ValidationAttribute_NeedBothResourceTypeAndResourceName", "Both ErrorMessageResourceType and ErrorMessageResourceName need to be set on this attribute.");

	internal static string ValidationAttribute_ResourcePropertyNotStringType => GetResourceString("ValidationAttribute_ResourcePropertyNotStringType", "The property '{0}' on resource type '{1}' is not a string type.");

	internal static string ValidationAttribute_ResourceTypeDoesNotHaveProperty => GetResourceString("ValidationAttribute_ResourceTypeDoesNotHaveProperty", "The resource type '{0}' does not have an accessible static property named '{1}'.");

	internal static string ValidationAttribute_ValidationError => GetResourceString("ValidationAttribute_ValidationError", "The field {0} is invalid.");

	internal static string Validator_InstanceMustMatchValidationContextInstance => GetResourceString("Validator_InstanceMustMatchValidationContextInstance", "The instance provided must match the ObjectInstance on the ValidationContext supplied.");

	internal static string Validator_Property_Value_Wrong_Type => GetResourceString("Validator_Property_Value_Wrong_Type", "The value for property '{0}' must be of type '{1}'.");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}
}
