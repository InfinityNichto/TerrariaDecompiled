using System.Resources;
using FxResources.System.ComponentModel.TypeConverter;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Array => GetResourceString("Array");

	internal static string Collection => GetResourceString("Collection");

	internal static string ConvertFromException => GetResourceString("ConvertFromException");

	internal static string ConvertInvalidPrimitive => GetResourceString("ConvertInvalidPrimitive");

	internal static string ConvertToException => GetResourceString("ConvertToException");

	internal static string EnumConverterInvalidValue => GetResourceString("EnumConverterInvalidValue");

	internal static string ErrorInvalidEventHandler => GetResourceString("ErrorInvalidEventHandler");

	internal static string ErrorInvalidEventType => GetResourceString("ErrorInvalidEventType");

	internal static string ErrorInvalidPropertyType => GetResourceString("ErrorInvalidPropertyType");

	internal static string ErrorMissingEventAccessors => GetResourceString("ErrorMissingEventAccessors");

	internal static string ErrorMissingPropertyAccessors => GetResourceString("ErrorMissingPropertyAccessors");

	internal static string InvalidMemberName => GetResourceString("InvalidMemberName");

	internal static string none => GetResourceString("none");

	internal static string Null => GetResourceString("Null");

	internal static string NullableConverterBadCtorArg => GetResourceString("NullableConverterBadCtorArg");

	internal static string Text => GetResourceString("Text");

	internal static string TypeDescriptorAlreadyAssociated => GetResourceString("TypeDescriptorAlreadyAssociated");

	internal static string TypeDescriptorArgsCountMismatch => GetResourceString("TypeDescriptorArgsCountMismatch");

	internal static string TypeDescriptorProviderError => GetResourceString("TypeDescriptorProviderError");

	internal static string TypeDescriptorExpectedElementType => GetResourceString("TypeDescriptorExpectedElementType");

	internal static string TypeDescriptorSameAssociation => GetResourceString("TypeDescriptorSameAssociation");

	internal static string InvalidColor => GetResourceString("InvalidColor");

	internal static string TextParseFailedFormat => GetResourceString("TextParseFailedFormat");

	internal static string PropertyValueInvalidEntry => GetResourceString("PropertyValueInvalidEntry");

	internal static string InvalidParameter => GetResourceString("InvalidParameter");

	internal static string TimerInvalidInterval => GetResourceString("TimerInvalidInterval");

	internal static string ToolboxItemAttributeFailedGetType => GetResourceString("ToolboxItemAttributeFailedGetType");

	internal static string PropertyTabAttributeBadPropertyTabScope => GetResourceString("PropertyTabAttributeBadPropertyTabScope");

	internal static string PropertyTabAttributeTypeLoadException => GetResourceString("PropertyTabAttributeTypeLoadException");

	internal static string PropertyTabAttributeArrayLengthMismatch => GetResourceString("PropertyTabAttributeArrayLengthMismatch");

	internal static string PropertyTabAttributeParamsBothNull => GetResourceString("PropertyTabAttributeParamsBothNull");

	internal static string CultureInfoConverterDefaultCultureString => GetResourceString("CultureInfoConverterDefaultCultureString");

	internal static string CultureInfoConverterInvalidCulture => GetResourceString("CultureInfoConverterInvalidCulture");

	internal static string ErrorInvalidServiceInstance => GetResourceString("ErrorInvalidServiceInstance");

	internal static string ErrorServiceExists => GetResourceString("ErrorServiceExists");

	internal static string InvalidArgumentValue => GetResourceString("InvalidArgumentValue");

	internal static string InvalidNullArgument => GetResourceString("InvalidNullArgument");

	internal static string DuplicateComponentName => GetResourceString("DuplicateComponentName");

	internal static string MaskedTextProviderPasswordAndPromptCharError => GetResourceString("MaskedTextProviderPasswordAndPromptCharError");

	internal static string MaskedTextProviderInvalidCharError => GetResourceString("MaskedTextProviderInvalidCharError");

	internal static string MaskedTextProviderMaskNullOrEmpty => GetResourceString("MaskedTextProviderMaskNullOrEmpty");

	internal static string MaskedTextProviderMaskInvalidChar => GetResourceString("MaskedTextProviderMaskInvalidChar");

	internal static string InstanceDescriptorCannotBeStatic => GetResourceString("InstanceDescriptorCannotBeStatic");

	internal static string InstanceDescriptorMustBeStatic => GetResourceString("InstanceDescriptorMustBeStatic");

	internal static string InstanceDescriptorMustBeReadable => GetResourceString("InstanceDescriptorMustBeReadable");

	internal static string InstanceDescriptorLengthMismatch => GetResourceString("InstanceDescriptorLengthMismatch");

	internal static string MetaExtenderName => GetResourceString("MetaExtenderName");

	internal static string CantModifyListSortDescriptionCollection => GetResourceString("CantModifyListSortDescriptionCollection");

	internal static string LicExceptionTypeOnly => GetResourceString("LicExceptionTypeOnly");

	internal static string LicExceptionTypeAndInstance => GetResourceString("LicExceptionTypeAndInstance");

	internal static string LicMgrContextCannotBeChanged => GetResourceString("LicMgrContextCannotBeChanged");

	internal static string LicMgrAlreadyLocked => GetResourceString("LicMgrAlreadyLocked");

	internal static string LicMgrDifferentUser => GetResourceString("LicMgrDifferentUser");

	internal static string CollectionConverterText => GetResourceString("CollectionConverterText");

	internal static string InstanceCreationEditorDefaultText => GetResourceString("InstanceCreationEditorDefaultText");

	internal static string ErrorPropertyAccessorException => GetResourceString("ErrorPropertyAccessorException");

	internal static string CHECKOUTCanceled => GetResourceString("CHECKOUTCanceled");

	internal static string toStringNone => GetResourceString("toStringNone");

	internal static string MemberRelationshipService_RelationshipNotSupported => GetResourceString("MemberRelationshipService_RelationshipNotSupported");

	internal static string BinaryFormatterMessage => GetResourceString("BinaryFormatterMessage");

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
