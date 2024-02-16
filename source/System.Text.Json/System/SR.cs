using System.Resources;
using FxResources.System.Text.Json;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArrayDepthTooLarge => GetResourceString("ArrayDepthTooLarge");

	internal static string CannotReadIncompleteUTF16 => GetResourceString("CannotReadIncompleteUTF16");

	internal static string CannotReadInvalidUTF16 => GetResourceString("CannotReadInvalidUTF16");

	internal static string CannotStartObjectArrayAfterPrimitiveOrClose => GetResourceString("CannotStartObjectArrayAfterPrimitiveOrClose");

	internal static string CannotStartObjectArrayWithoutProperty => GetResourceString("CannotStartObjectArrayWithoutProperty");

	internal static string CannotTranscodeInvalidUtf8 => GetResourceString("CannotTranscodeInvalidUtf8");

	internal static string CannotDecodeInvalidBase64 => GetResourceString("CannotDecodeInvalidBase64");

	internal static string CannotTranscodeInvalidUtf16 => GetResourceString("CannotTranscodeInvalidUtf16");

	internal static string CannotEncodeInvalidUTF16 => GetResourceString("CannotEncodeInvalidUTF16");

	internal static string CannotEncodeInvalidUTF8 => GetResourceString("CannotEncodeInvalidUTF8");

	internal static string CannotWritePropertyWithinArray => GetResourceString("CannotWritePropertyWithinArray");

	internal static string CannotWritePropertyAfterProperty => GetResourceString("CannotWritePropertyAfterProperty");

	internal static string CannotWriteValueAfterPrimitiveOrClose => GetResourceString("CannotWriteValueAfterPrimitiveOrClose");

	internal static string CannotWriteValueWithinObject => GetResourceString("CannotWriteValueWithinObject");

	internal static string DepthTooLarge => GetResourceString("DepthTooLarge");

	internal static string EndOfCommentNotFound => GetResourceString("EndOfCommentNotFound");

	internal static string EndOfStringNotFound => GetResourceString("EndOfStringNotFound");

	internal static string ExpectedEndAfterSingleJson => GetResourceString("ExpectedEndAfterSingleJson");

	internal static string ExpectedEndOfDigitNotFound => GetResourceString("ExpectedEndOfDigitNotFound");

	internal static string ExpectedFalse => GetResourceString("ExpectedFalse");

	internal static string ExpectedJsonTokens => GetResourceString("ExpectedJsonTokens");

	internal static string ExpectedOneCompleteToken => GetResourceString("ExpectedOneCompleteToken");

	internal static string ExpectedNextDigitEValueNotFound => GetResourceString("ExpectedNextDigitEValueNotFound");

	internal static string ExpectedNull => GetResourceString("ExpectedNull");

	internal static string ExpectedSeparatorAfterPropertyNameNotFound => GetResourceString("ExpectedSeparatorAfterPropertyNameNotFound");

	internal static string ExpectedStartOfPropertyNotFound => GetResourceString("ExpectedStartOfPropertyNotFound");

	internal static string ExpectedStartOfPropertyOrValueNotFound => GetResourceString("ExpectedStartOfPropertyOrValueNotFound");

	internal static string ExpectedStartOfValueNotFound => GetResourceString("ExpectedStartOfValueNotFound");

	internal static string ExpectedTrue => GetResourceString("ExpectedTrue");

	internal static string ExpectedValueAfterPropertyNameNotFound => GetResourceString("ExpectedValueAfterPropertyNameNotFound");

	internal static string FailedToGetLargerSpan => GetResourceString("FailedToGetLargerSpan");

	internal static string FoundInvalidCharacter => GetResourceString("FoundInvalidCharacter");

	internal static string InvalidCast => GetResourceString("InvalidCast");

	internal static string InvalidCharacterAfterEscapeWithinString => GetResourceString("InvalidCharacterAfterEscapeWithinString");

	internal static string InvalidCharacterWithinString => GetResourceString("InvalidCharacterWithinString");

	internal static string InvalidEndOfJsonNonPrimitive => GetResourceString("InvalidEndOfJsonNonPrimitive");

	internal static string InvalidHexCharacterWithinString => GetResourceString("InvalidHexCharacterWithinString");

	internal static string JsonDocumentDoesNotSupportComments => GetResourceString("JsonDocumentDoesNotSupportComments");

	internal static string JsonElementHasWrongType => GetResourceString("JsonElementHasWrongType");

	internal static string MaxDepthMustBePositive => GetResourceString("MaxDepthMustBePositive");

	internal static string CommentHandlingMustBeValid => GetResourceString("CommentHandlingMustBeValid");

	internal static string MismatchedObjectArray => GetResourceString("MismatchedObjectArray");

	internal static string CannotWriteEndAfterProperty => GetResourceString("CannotWriteEndAfterProperty");

	internal static string ObjectDepthTooLarge => GetResourceString("ObjectDepthTooLarge");

	internal static string PropertyNameTooLarge => GetResourceString("PropertyNameTooLarge");

	internal static string FormatDecimal => GetResourceString("FormatDecimal");

	internal static string FormatDouble => GetResourceString("FormatDouble");

	internal static string FormatInt32 => GetResourceString("FormatInt32");

	internal static string FormatInt64 => GetResourceString("FormatInt64");

	internal static string FormatSingle => GetResourceString("FormatSingle");

	internal static string FormatUInt32 => GetResourceString("FormatUInt32");

	internal static string FormatUInt64 => GetResourceString("FormatUInt64");

	internal static string RequiredDigitNotFoundAfterDecimal => GetResourceString("RequiredDigitNotFoundAfterDecimal");

	internal static string RequiredDigitNotFoundAfterSign => GetResourceString("RequiredDigitNotFoundAfterSign");

	internal static string RequiredDigitNotFoundEndOfData => GetResourceString("RequiredDigitNotFoundEndOfData");

	internal static string SpecialNumberValuesNotSupported => GetResourceString("SpecialNumberValuesNotSupported");

	internal static string ValueTooLarge => GetResourceString("ValueTooLarge");

	internal static string ZeroDepthAtEnd => GetResourceString("ZeroDepthAtEnd");

	internal static string DeserializeUnableToConvertValue => GetResourceString("DeserializeUnableToConvertValue");

	internal static string DeserializeWrongType => GetResourceString("DeserializeWrongType");

	internal static string SerializationInvalidBufferSize => GetResourceString("SerializationInvalidBufferSize");

	internal static string InvalidComparison => GetResourceString("InvalidComparison");

	internal static string FormatDateTime => GetResourceString("FormatDateTime");

	internal static string FormatDateTimeOffset => GetResourceString("FormatDateTimeOffset");

	internal static string FormatTimeSpan => GetResourceString("FormatTimeSpan");

	internal static string FormatGuid => GetResourceString("FormatGuid");

	internal static string ExpectedStartOfPropertyOrValueAfterComment => GetResourceString("ExpectedStartOfPropertyOrValueAfterComment");

	internal static string TrailingCommaNotAllowedBeforeArrayEnd => GetResourceString("TrailingCommaNotAllowedBeforeArrayEnd");

	internal static string TrailingCommaNotAllowedBeforeObjectEnd => GetResourceString("TrailingCommaNotAllowedBeforeObjectEnd");

	internal static string SerializerOptionsImmutable => GetResourceString("SerializerOptionsImmutable");

	internal static string StreamNotWritable => GetResourceString("StreamNotWritable");

	internal static string CannotWriteCommentWithEmbeddedDelimiter => GetResourceString("CannotWriteCommentWithEmbeddedDelimiter");

	internal static string SerializerPropertyNameConflict => GetResourceString("SerializerPropertyNameConflict");

	internal static string SerializerPropertyNameNull => GetResourceString("SerializerPropertyNameNull");

	internal static string SerializationDataExtensionPropertyInvalid => GetResourceString("SerializationDataExtensionPropertyInvalid");

	internal static string SerializationDuplicateTypeAttribute => GetResourceString("SerializationDuplicateTypeAttribute");

	internal static string SerializationNotSupportedType => GetResourceString("SerializationNotSupportedType");

	internal static string TypeRequiresAsyncSerialization => GetResourceString("TypeRequiresAsyncSerialization");

	internal static string InvalidCharacterAtStartOfComment => GetResourceString("InvalidCharacterAtStartOfComment");

	internal static string UnexpectedEndOfDataWhileReadingComment => GetResourceString("UnexpectedEndOfDataWhileReadingComment");

	internal static string CannotSkip => GetResourceString("CannotSkip");

	internal static string NotEnoughData => GetResourceString("NotEnoughData");

	internal static string UnexpectedEndOfLineSeparator => GetResourceString("UnexpectedEndOfLineSeparator");

	internal static string JsonSerializerDoesNotSupportComments => GetResourceString("JsonSerializerDoesNotSupportComments");

	internal static string DeserializeNoConstructor => GetResourceString("DeserializeNoConstructor");

	internal static string DeserializePolymorphicInterface => GetResourceString("DeserializePolymorphicInterface");

	internal static string SerializationConverterOnAttributeNotCompatible => GetResourceString("SerializationConverterOnAttributeNotCompatible");

	internal static string SerializationConverterOnAttributeInvalid => GetResourceString("SerializationConverterOnAttributeInvalid");

	internal static string SerializationConverterRead => GetResourceString("SerializationConverterRead");

	internal static string SerializationConverterNotCompatible => GetResourceString("SerializationConverterNotCompatible");

	internal static string SerializationConverterWrite => GetResourceString("SerializationConverterWrite");

	internal static string NamingPolicyReturnNull => GetResourceString("NamingPolicyReturnNull");

	internal static string SerializationDuplicateAttribute => GetResourceString("SerializationDuplicateAttribute");

	internal static string SerializeUnableToSerialize => GetResourceString("SerializeUnableToSerialize");

	internal static string FormatByte => GetResourceString("FormatByte");

	internal static string FormatInt16 => GetResourceString("FormatInt16");

	internal static string FormatSByte => GetResourceString("FormatSByte");

	internal static string FormatUInt16 => GetResourceString("FormatUInt16");

	internal static string SerializerCycleDetected => GetResourceString("SerializerCycleDetected");

	internal static string InvalidLeadingZeroInNumber => GetResourceString("InvalidLeadingZeroInNumber");

	internal static string MetadataCannotParsePreservedObjectToImmutable => GetResourceString("MetadataCannotParsePreservedObjectToImmutable");

	internal static string MetadataDuplicateIdFound => GetResourceString("MetadataDuplicateIdFound");

	internal static string MetadataIdIsNotFirstProperty => GetResourceString("MetadataIdIsNotFirstProperty");

	internal static string MetadataInvalidReferenceToValueType => GetResourceString("MetadataInvalidReferenceToValueType");

	internal static string MetadataInvalidTokenAfterValues => GetResourceString("MetadataInvalidTokenAfterValues");

	internal static string MetadataPreservedArrayFailed => GetResourceString("MetadataPreservedArrayFailed");

	internal static string MetadataPreservedArrayInvalidProperty => GetResourceString("MetadataPreservedArrayInvalidProperty");

	internal static string MetadataPreservedArrayPropertyNotFound => GetResourceString("MetadataPreservedArrayPropertyNotFound");

	internal static string MetadataReferenceCannotContainOtherProperties => GetResourceString("MetadataReferenceCannotContainOtherProperties");

	internal static string MetadataReferenceNotFound => GetResourceString("MetadataReferenceNotFound");

	internal static string MetadataValueWasNotString => GetResourceString("MetadataValueWasNotString");

	internal static string MetadataInvalidPropertyWithLeadingDollarSign => GetResourceString("MetadataInvalidPropertyWithLeadingDollarSign");

	internal static string MultipleMembersBindWithConstructorParameter => GetResourceString("MultipleMembersBindWithConstructorParameter");

	internal static string ConstructorParamIncompleteBinding => GetResourceString("ConstructorParamIncompleteBinding");

	internal static string ConstructorMaxOf64Parameters => GetResourceString("ConstructorMaxOf64Parameters");

	internal static string ObjectWithParameterizedCtorRefMetadataNotHonored => GetResourceString("ObjectWithParameterizedCtorRefMetadataNotHonored");

	internal static string SerializerConverterFactoryReturnsNull => GetResourceString("SerializerConverterFactoryReturnsNull");

	internal static string SerializationNotSupportedParentType => GetResourceString("SerializationNotSupportedParentType");

	internal static string ExtensionDataCannotBindToCtorParam => GetResourceString("ExtensionDataCannotBindToCtorParam");

	internal static string BufferMaximumSizeExceeded => GetResourceString("BufferMaximumSizeExceeded");

	internal static string CannotSerializeInvalidType => GetResourceString("CannotSerializeInvalidType");

	internal static string SerializeTypeInstanceNotSupported => GetResourceString("SerializeTypeInstanceNotSupported");

	internal static string JsonIncludeOnNonPublicInvalid => GetResourceString("JsonIncludeOnNonPublicInvalid");

	internal static string CannotSerializeInvalidMember => GetResourceString("CannotSerializeInvalidMember");

	internal static string CannotPopulateCollection => GetResourceString("CannotPopulateCollection");

	internal static string DefaultIgnoreConditionAlreadySpecified => GetResourceString("DefaultIgnoreConditionAlreadySpecified");

	internal static string DefaultIgnoreConditionInvalid => GetResourceString("DefaultIgnoreConditionInvalid");

	internal static string FormatBoolean => GetResourceString("FormatBoolean");

	internal static string DictionaryKeyTypeNotSupported => GetResourceString("DictionaryKeyTypeNotSupported");

	internal static string IgnoreConditionOnValueTypeInvalid => GetResourceString("IgnoreConditionOnValueTypeInvalid");

	internal static string NumberHandlingOnPropertyInvalid => GetResourceString("NumberHandlingOnPropertyInvalid");

	internal static string ConverterCanConvertMultipleTypes => GetResourceString("ConverterCanConvertMultipleTypes");

	internal static string MetadataReferenceOfTypeCannotBeAssignedToType => GetResourceString("MetadataReferenceOfTypeCannotBeAssignedToType");

	internal static string DeserializeUnableToAssignValue => GetResourceString("DeserializeUnableToAssignValue");

	internal static string DeserializeUnableToAssignNull => GetResourceString("DeserializeUnableToAssignNull");

	internal static string SerializerConverterFactoryReturnsJsonConverterFactory => GetResourceString("SerializerConverterFactoryReturnsJsonConverterFactory");

	internal static string NodeElementWrongType => GetResourceString("NodeElementWrongType");

	internal static string NodeElementCannotBeObjectOrArray => GetResourceString("NodeElementCannotBeObjectOrArray");

	internal static string NodeAlreadyHasParent => GetResourceString("NodeAlreadyHasParent");

	internal static string NodeCycleDetected => GetResourceString("NodeCycleDetected");

	internal static string NodeUnableToConvert => GetResourceString("NodeUnableToConvert");

	internal static string NodeUnableToConvertElement => GetResourceString("NodeUnableToConvertElement");

	internal static string NodeValueNotAllowed => GetResourceString("NodeValueNotAllowed");

	internal static string NodeWrongType => GetResourceString("NodeWrongType");

	internal static string NodeDuplicateKey => GetResourceString("NodeDuplicateKey");

	internal static string SerializerContextOptionsImmutable => GetResourceString("SerializerContextOptionsImmutable");

	internal static string OptionsAlreadyBoundToContext => GetResourceString("OptionsAlreadyBoundToContext");

	internal static string ConverterForPropertyMustBeValid => GetResourceString("ConverterForPropertyMustBeValid");

	internal static string BuiltInConvertersNotRooted => GetResourceString("BuiltInConvertersNotRooted");

	internal static string NoMetadataForType => GetResourceString("NoMetadataForType");

	internal static string NodeCollectionIsReadOnly => GetResourceString("NodeCollectionIsReadOnly");

	internal static string NodeArrayIndexNegative => GetResourceString("NodeArrayIndexNegative");

	internal static string NodeArrayTooSmall => GetResourceString("NodeArrayTooSmall");

	internal static string NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty => GetResourceString("NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty");

	internal static string NoMetadataForTypeProperties => GetResourceString("NoMetadataForTypeProperties");

	internal static string FieldCannotBeVirtual => GetResourceString("FieldCannotBeVirtual");

	internal static string MissingFSharpCoreMember => GetResourceString("MissingFSharpCoreMember");

	internal static string FSharpDiscriminatedUnionsNotSupported => GetResourceString("FSharpDiscriminatedUnionsNotSupported");

	internal static string NoMetadataForTypeCtorParams => GetResourceString("NoMetadataForTypeCtorParams");

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
