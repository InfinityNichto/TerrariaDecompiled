using System.Resources;
using FxResources.System.Private.DataContractSerialization;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArrayExceededSize => GetResourceString("ArrayExceededSize");

	internal static string ArrayExceededSizeAttribute => GetResourceString("ArrayExceededSizeAttribute");

	internal static string ArrayTypeIsNotSupported_GeneratingCode => GetResourceString("ArrayTypeIsNotSupported_GeneratingCode");

	internal static string CannotDeserializeRefAtTopLevel => GetResourceString("CannotDeserializeRefAtTopLevel");

	internal static string CannotLoadMemberType => GetResourceString("CannotLoadMemberType");

	internal static string CannotSerializeObjectWithCycles => GetResourceString("CannotSerializeObjectWithCycles");

	internal static string CanOnlyStoreIntoArgOrLocGot0 => GetResourceString("CanOnlyStoreIntoArgOrLocGot0");

	internal static string CharIsInvalidPrimitive => GetResourceString("CharIsInvalidPrimitive");

	internal static string CallbackMustReturnVoid => GetResourceString("CallbackMustReturnVoid");

	internal static string CallbackParameterInvalid => GetResourceString("CallbackParameterInvalid");

	internal static string CallbacksCannotBeVirtualMethods => GetResourceString("CallbacksCannotBeVirtualMethods");

	internal static string CollectionMustHaveAddMethod => GetResourceString("CollectionMustHaveAddMethod");

	internal static string CollectionMustHaveGetEnumeratorMethod => GetResourceString("CollectionMustHaveGetEnumeratorMethod");

	internal static string CollectionMustHaveItemType => GetResourceString("CollectionMustHaveItemType");

	internal static string CollectionTypeCannotBeBuiltIn => GetResourceString("CollectionTypeCannotBeBuiltIn");

	internal static string CollectionTypeCannotHaveDataContract => GetResourceString("CollectionTypeCannotHaveDataContract");

	internal static string CollectionTypeDoesNotHaveAddMethod => GetResourceString("CollectionTypeDoesNotHaveAddMethod");

	internal static string CollectionTypeDoesNotHaveDefaultCtor => GetResourceString("CollectionTypeDoesNotHaveDefaultCtor");

	internal static string CollectionTypeHasMultipleDefinitionsOfInterface => GetResourceString("CollectionTypeHasMultipleDefinitionsOfInterface");

	internal static string CollectionTypeIsNotIEnumerable => GetResourceString("CollectionTypeIsNotIEnumerable");

	internal static string DataContractCacheOverflow => GetResourceString("DataContractCacheOverflow");

	internal static string DataContractNamespaceAlreadySet => GetResourceString("DataContractNamespaceAlreadySet");

	internal static string DataContractNamespaceIsNotValid => GetResourceString("DataContractNamespaceIsNotValid");

	internal static string DataContractNamespaceReserved => GetResourceString("DataContractNamespaceReserved");

	internal static string DataMemberOnEnumField => GetResourceString("DataMemberOnEnumField");

	internal static string DcTypeNotFoundOnDeserialize => GetResourceString("DcTypeNotFoundOnDeserialize");

	internal static string DcTypeNotFoundOnSerialize => GetResourceString("DcTypeNotFoundOnSerialize");

	internal static string DcTypeNotResolvedOnDeserialize => GetResourceString("DcTypeNotResolvedOnDeserialize");

	internal static string DeserializedObjectWithIdNotFound => GetResourceString("DeserializedObjectWithIdNotFound");

	internal static string DupContractInKnownTypes => GetResourceString("DupContractInKnownTypes");

	internal static string DupKeyValueName => GetResourceString("DupKeyValueName");

	internal static string DupEnumMemberValue => GetResourceString("DupEnumMemberValue");

	internal static string DupMemberName => GetResourceString("DupMemberName");

	internal static string DuplicateAttribute => GetResourceString("DuplicateAttribute");

	internal static string DuplicateCallback => GetResourceString("DuplicateCallback");

	internal static string EncounteredWithNameNamespace => GetResourceString("EncounteredWithNameNamespace");

	internal static string EnumTypeCannotHaveIsReference => GetResourceString("EnumTypeCannotHaveIsReference");

	internal static string ErrorDeserializing => GetResourceString("ErrorDeserializing");

	internal static string ErrorInLine => GetResourceString("ErrorInLine");

	internal static string ErrorIsStartObject => GetResourceString("ErrorIsStartObject");

	internal static string ErrorSerializing => GetResourceString("ErrorSerializing");

	internal static string ErrorTypeInfo => GetResourceString("ErrorTypeInfo");

	internal static string ErrorWriteEndObject => GetResourceString("ErrorWriteEndObject");

	internal static string ErrorWriteStartObject => GetResourceString("ErrorWriteStartObject");

	internal static string ExceededMaxItemsQuota => GetResourceString("ExceededMaxItemsQuota");

	internal static string ExpectingElement => GetResourceString("ExpectingElement");

	internal static string ExpectingElementAtDeserialize => GetResourceString("ExpectingElementAtDeserialize");

	internal static string ExpectingEnd => GetResourceString("ExpectingEnd");

	internal static string ExpectingState => GetResourceString("ExpectingState");

	internal static string GenericNameBraceMismatch => GetResourceString("GenericNameBraceMismatch");

	internal static string GenericParameterNotValid => GetResourceString("GenericParameterNotValid");

	internal static string InconsistentIsReference => GetResourceString("InconsistentIsReference");

	internal static string IndexedPropertyCannotBeSerialized => GetResourceString("IndexedPropertyCannotBeSerialized");

	internal static string InvalidCollectionContractItemName => GetResourceString("InvalidCollectionContractItemName");

	internal static string InvalidCollectionContractKeyName => GetResourceString("InvalidCollectionContractKeyName");

	internal static string InvalidCollectionContractKeyNoDictionary => GetResourceString("InvalidCollectionContractKeyNoDictionary");

	internal static string InvalidCollectionContractName => GetResourceString("InvalidCollectionContractName");

	internal static string InvalidCollectionContractNamespace => GetResourceString("InvalidCollectionContractNamespace");

	internal static string InvalidCollectionContractValueName => GetResourceString("InvalidCollectionContractValueName");

	internal static string InvalidCollectionContractValueNoDictionary => GetResourceString("InvalidCollectionContractValueNoDictionary");

	internal static string InvalidCollectionDataContract => GetResourceString("InvalidCollectionDataContract");

	internal static string InvalidCollectionType => GetResourceString("InvalidCollectionType");

	internal static string InvalidDataContractName => GetResourceString("InvalidDataContractName");

	internal static string InvalidDataContractNamespace => GetResourceString("InvalidDataContractNamespace");

	internal static string InvalidDataMemberName => GetResourceString("InvalidDataMemberName");

	internal static string InvalidEnumMemberValue => GetResourceString("InvalidEnumMemberValue");

	internal static string InvalidEnumValueOnRead => GetResourceString("InvalidEnumValueOnRead");

	internal static string InvalidEnumValueOnWrite => GetResourceString("InvalidEnumValueOnWrite");

	internal static string InvalidGetSchemaMethod => GetResourceString("InvalidGetSchemaMethod");

	internal static string InvalidGlobalDataContractNamespace => GetResourceString("InvalidGlobalDataContractNamespace");

	internal static string InvalidMember => GetResourceString("InvalidMember");

	internal static string InvalidNonNullReturnValueByIsAny => GetResourceString("InvalidNonNullReturnValueByIsAny");

	internal static string InvalidPrimitiveType_Serialization => GetResourceString("InvalidPrimitiveType_Serialization");

	internal static string InvalidReturnTypeOnGetSchemaMethod => GetResourceString("InvalidReturnTypeOnGetSchemaMethod");

	internal static string InvalidSizeDefinition => GetResourceString("InvalidSizeDefinition");

	internal static string InvalidXmlDataContractName => GetResourceString("InvalidXmlDataContractName");

	internal static string InvalidXsIdDefinition => GetResourceString("InvalidXsIdDefinition");

	internal static string InvalidXsRefDefinition => GetResourceString("InvalidXsRefDefinition");

	internal static string IsAnyCannotBeNull => GetResourceString("IsAnyCannotBeNull");

	internal static string IsAnyCannotBeSerializedAsDerivedType => GetResourceString("IsAnyCannotBeSerializedAsDerivedType");

	internal static string IsAnyCannotHaveXmlRoot => GetResourceString("IsAnyCannotHaveXmlRoot");

	internal static string IsNotAssignableFrom => GetResourceString("IsNotAssignableFrom");

	internal static string IsRequiredDataMemberOnIsReferenceDataContractType => GetResourceString("IsRequiredDataMemberOnIsReferenceDataContractType");

	internal static string IXmlSerializableCannotHaveCollectionDataContract => GetResourceString("IXmlSerializableCannotHaveCollectionDataContract");

	internal static string IXmlSerializableCannotHaveDataContract => GetResourceString("IXmlSerializableCannotHaveDataContract");

	internal static string IXmlSerializableIllegalOperation => GetResourceString("IXmlSerializableIllegalOperation");

	internal static string IXmlSerializableMissingEndElements => GetResourceString("IXmlSerializableMissingEndElements");

	internal static string IXmlSerializableMustHaveDefaultConstructor => GetResourceString("IXmlSerializableMustHaveDefaultConstructor");

	internal static string IXmlSerializableWritePastSubTree => GetResourceString("IXmlSerializableWritePastSubTree");

	internal static string KnownTypeAttributeEmptyString => GetResourceString("KnownTypeAttributeEmptyString");

	internal static string KnownTypeAttributeUnknownMethod => GetResourceString("KnownTypeAttributeUnknownMethod");

	internal static string KnownTypeAttributeReturnType => GetResourceString("KnownTypeAttributeReturnType");

	internal static string KnownTypeAttributeOneScheme => GetResourceString("KnownTypeAttributeOneScheme");

	internal static string KnownTypeAttributeValidMethodTypes => GetResourceString("KnownTypeAttributeValidMethodTypes");

	internal static string KnownTypeAttributeNoData => GetResourceString("KnownTypeAttributeNoData");

	internal static string KnownTypeAttributeMethodNull => GetResourceString("KnownTypeAttributeMethodNull");

	internal static string MaxArrayLengthExceeded => GetResourceString("MaxArrayLengthExceeded");

	internal static string MissingGetSchemaMethod => GetResourceString("MissingGetSchemaMethod");

	internal static string MultipleIdDefinition => GetResourceString("MultipleIdDefinition");

	internal static string NoConversionPossibleTo => GetResourceString("NoConversionPossibleTo");

	internal static string NoDefaultConstructorForCollection => GetResourceString("NoDefaultConstructorForCollection");

	internal static string NoGetMethodForProperty => GetResourceString("NoGetMethodForProperty");

	internal static string NoSetMethodForProperty => GetResourceString("NoSetMethodForProperty");

	internal static string NullKnownType => GetResourceString("NullKnownType");

	internal static string NullValueReturnedForGetOnlyCollection => GetResourceString("NullValueReturnedForGetOnlyCollection");

	internal static string ObjectTableOverflow => GetResourceString("ObjectTableOverflow");

	internal static string ParameterCountMismatch => GetResourceString("ParameterCountMismatch");

	internal static string PartialTrustCollectionContractAddMethodNotPublic => GetResourceString("PartialTrustCollectionContractAddMethodNotPublic");

	internal static string PartialTrustCollectionContractNoPublicConstructor => GetResourceString("PartialTrustCollectionContractNoPublicConstructor");

	internal static string PartialTrustCollectionContractTypeNotPublic => GetResourceString("PartialTrustCollectionContractTypeNotPublic");

	internal static string PartialTrustDataContractOnSerializingNotPublic => GetResourceString("PartialTrustDataContractOnSerializingNotPublic");

	internal static string PartialTrustDataContractOnSerializedNotPublic => GetResourceString("PartialTrustDataContractOnSerializedNotPublic");

	internal static string PartialTrustDataContractOnDeserializingNotPublic => GetResourceString("PartialTrustDataContractOnDeserializingNotPublic");

	internal static string PartialTrustDataContractOnDeserializedNotPublic => GetResourceString("PartialTrustDataContractOnDeserializedNotPublic");

	internal static string PartialTrustDataContractFieldGetNotPublic => GetResourceString("PartialTrustDataContractFieldGetNotPublic");

	internal static string PartialTrustDataContractFieldSetNotPublic => GetResourceString("PartialTrustDataContractFieldSetNotPublic");

	internal static string PartialTrustDataContractPropertyGetNotPublic => GetResourceString("PartialTrustDataContractPropertyGetNotPublic");

	internal static string PartialTrustDataContractPropertySetNotPublic => GetResourceString("PartialTrustDataContractPropertySetNotPublic");

	internal static string PartialTrustDataContractTypeNotPublic => GetResourceString("PartialTrustDataContractTypeNotPublic");

	internal static string PartialTrustNonAttributedSerializableTypeNoPublicConstructor => GetResourceString("PartialTrustNonAttributedSerializableTypeNoPublicConstructor");

	internal static string PartialTrustIXmlSerializableTypeNotPublic => GetResourceString("PartialTrustIXmlSerializableTypeNotPublic");

	internal static string PartialTrustIXmlSerialzableNoPublicConstructor => GetResourceString("PartialTrustIXmlSerialzableNoPublicConstructor");

	internal static string NonAttributedSerializableTypesMustHaveDefaultConstructor => GetResourceString("NonAttributedSerializableTypesMustHaveDefaultConstructor");

	internal static string AttributedTypesCannotInheritFromNonAttributedSerializableTypes => GetResourceString("AttributedTypesCannotInheritFromNonAttributedSerializableTypes");

	internal static string QuotaMustBePositive => GetResourceString("QuotaMustBePositive");

	internal static string QuotaIsReadOnly => GetResourceString("QuotaIsReadOnly");

	internal static string QuotaCopyReadOnly => GetResourceString("QuotaCopyReadOnly");

	internal static string RequiredMemberMustBeEmitted => GetResourceString("RequiredMemberMustBeEmitted");

	internal static string ResolveTypeReturnedFalse => GetResourceString("ResolveTypeReturnedFalse");

	internal static string ResolveTypeReturnedNull => GetResourceString("ResolveTypeReturnedNull");

	internal static string SupportForMultidimensionalArraysNotPresent => GetResourceString("SupportForMultidimensionalArraysNotPresent");

	internal static string TooManyCollectionContracts => GetResourceString("TooManyCollectionContracts");

	internal static string TooManyDataContracts => GetResourceString("TooManyDataContracts");

	internal static string TooManyDataMembers => GetResourceString("TooManyDataMembers");

	internal static string TooManyEnumMembers => GetResourceString("TooManyEnumMembers");

	internal static string TooManyIgnoreDataMemberAttributes => GetResourceString("TooManyIgnoreDataMemberAttributes");

	internal static string TypeNotSerializable => GetResourceString("TypeNotSerializable");

	internal static string UnexpectedContractType => GetResourceString("UnexpectedContractType");

	internal static string UnexpectedElementExpectingElements => GetResourceString("UnexpectedElementExpectingElements");

	internal static string UnexpectedEndOfFile => GetResourceString("UnexpectedEndOfFile");

	internal static string UnknownConstantType => GetResourceString("UnknownConstantType");

	internal static string ValueMustBeNonNegative => GetResourceString("ValueMustBeNonNegative");

	internal static string ValueTypeCannotBeNull => GetResourceString("ValueTypeCannotBeNull");

	internal static string ValueTypeCannotHaveBaseType => GetResourceString("ValueTypeCannotHaveBaseType");

	internal static string ValueTypeCannotHaveId => GetResourceString("ValueTypeCannotHaveId");

	internal static string ValueTypeCannotHaveIsReference => GetResourceString("ValueTypeCannotHaveIsReference");

	internal static string ValueTypeCannotHaveRef => GetResourceString("ValueTypeCannotHaveRef");

	internal static string XmlElementAttributes => GetResourceString("XmlElementAttributes");

	internal static string XmlForObjectCannotHaveContent => GetResourceString("XmlForObjectCannotHaveContent");

	internal static string XmlInvalidConversion => GetResourceString("XmlInvalidConversion");

	internal static string XmlInvalidConversionWithoutValue => GetResourceString("XmlInvalidConversionWithoutValue");

	internal static string XmlStartElementExpected => GetResourceString("XmlStartElementExpected");

	internal static string XmlWriterMustBeInElement => GetResourceString("XmlWriterMustBeInElement");

	internal static string NonOptionalFieldMemberOnIsReferenceSerializableType => GetResourceString("NonOptionalFieldMemberOnIsReferenceSerializableType");

	internal static string OffsetExceedsBufferSize => GetResourceString("OffsetExceedsBufferSize");

	internal static string SizeExceedsRemainingBufferSpace => GetResourceString("SizeExceedsRemainingBufferSpace");

	internal static string ValueMustBeInRange => GetResourceString("ValueMustBeInRange");

	internal static string XmlArrayTooSmallOutput => GetResourceString("XmlArrayTooSmallOutput");

	internal static string XmlAsyncIsRunningException => GetResourceString("XmlAsyncIsRunningException");

	internal static string XmlInvalidBase64Length => GetResourceString("XmlInvalidBase64Length");

	internal static string XmlInvalidBase64Sequence => GetResourceString("XmlInvalidBase64Sequence");

	internal static string XmlInvalidBinHexLength => GetResourceString("XmlInvalidBinHexLength");

	internal static string XmlInvalidBinHexSequence => GetResourceString("XmlInvalidBinHexSequence");

	internal static string XmlInvalidHighSurrogate => GetResourceString("XmlInvalidHighSurrogate");

	internal static string XmlInvalidLowSurrogate => GetResourceString("XmlInvalidLowSurrogate");

	internal static string XmlInvalidSurrogate => GetResourceString("XmlInvalidSurrogate");

	internal static string InvalidLocalNameEmpty => GetResourceString("InvalidLocalNameEmpty");

	internal static string XmlArrayTooSmall => GetResourceString("XmlArrayTooSmall");

	internal static string XmlArrayTooSmallInput => GetResourceString("XmlArrayTooSmallInput");

	internal static string XmlBadBOM => GetResourceString("XmlBadBOM");

	internal static string XmlBase64DataExpected => GetResourceString("XmlBase64DataExpected");

	internal static string XmlCDATAInvalidAtTopLevel => GetResourceString("XmlCDATAInvalidAtTopLevel");

	internal static string XmlCloseCData => GetResourceString("XmlCloseCData");

	internal static string XmlConversionOverflow => GetResourceString("XmlConversionOverflow");

	internal static string XmlDeclarationRequired => GetResourceString("XmlDeclarationRequired");

	internal static string XmlDeclMissingVersion => GetResourceString("XmlDeclMissingVersion");

	internal static string XmlDeclMissing => GetResourceString("XmlDeclMissing");

	internal static string XmlDeclNotFirst => GetResourceString("XmlDeclNotFirst");

	internal static string XmlDictionaryStringIDRange => GetResourceString("XmlDictionaryStringIDRange");

	internal static string XmlDictionaryStringIDUndefinedSession => GetResourceString("XmlDictionaryStringIDUndefinedSession");

	internal static string XmlDictionaryStringIDUndefinedStatic => GetResourceString("XmlDictionaryStringIDUndefinedStatic");

	internal static string XmlDuplicateAttribute => GetResourceString("XmlDuplicateAttribute");

	internal static string XmlEmptyNamespaceRequiresNullPrefix => GetResourceString("XmlEmptyNamespaceRequiresNullPrefix");

	internal static string XmlEncodingMismatch => GetResourceString("XmlEncodingMismatch");

	internal static string XmlEncodingNotSupported => GetResourceString("XmlEncodingNotSupported");

	internal static string XmlEndElementExpected => GetResourceString("XmlEndElementExpected");

	internal static string XmlEndElementNoOpenNodes => GetResourceString("XmlEndElementNoOpenNodes");

	internal static string XmlExpectedEncoding => GetResourceString("XmlExpectedEncoding");

	internal static string XmlFoundCData => GetResourceString("XmlFoundCData");

	internal static string XmlFoundComment => GetResourceString("XmlFoundComment");

	internal static string XmlFoundElement => GetResourceString("XmlFoundElement");

	internal static string XmlFoundEndElement => GetResourceString("XmlFoundEndElement");

	internal static string XmlFoundEndOfFile => GetResourceString("XmlFoundEndOfFile");

	internal static string XmlFoundNodeType => GetResourceString("XmlFoundNodeType");

	internal static string XmlFoundText => GetResourceString("XmlFoundText");

	internal static string XmlFullStartElementExpected => GetResourceString("XmlFullStartElementExpected");

	internal static string XmlFullStartElementLocalNameNsExpected => GetResourceString("XmlFullStartElementLocalNameNsExpected");

	internal static string XmlFullStartElementNameExpected => GetResourceString("XmlFullStartElementNameExpected");

	internal static string XmlIDDefined => GetResourceString("XmlIDDefined");

	internal static string XmlKeyAlreadyExists => GetResourceString("XmlKeyAlreadyExists");

	internal static string XmlIllegalOutsideRoot => GetResourceString("XmlIllegalOutsideRoot");

	internal static string XmlInvalidBytes => GetResourceString("XmlInvalidBytes");

	internal static string XmlInvalidCharRef => GetResourceString("XmlInvalidCharRef");

	internal static string XmlInvalidCommentChars => GetResourceString("XmlInvalidCommentChars");

	internal static string XmlInvalidDeclaration => GetResourceString("XmlInvalidDeclaration");

	internal static string XmlInvalidDepth => GetResourceString("XmlInvalidDepth");

	internal static string XmlInvalidEncoding_UTF8 => GetResourceString("XmlInvalidEncoding_UTF8");

	internal static string XmlInvalidFFFE => GetResourceString("XmlInvalidFFFE");

	internal static string XmlInvalidFormat => GetResourceString("XmlInvalidFormat");

	internal static string XmlInvalidID => GetResourceString("XmlInvalidID");

	internal static string XmlInvalidOperation => GetResourceString("XmlInvalidOperation");

	internal static string XmlInvalidPrefixState => GetResourceString("XmlInvalidPrefixState");

	internal static string XmlInvalidQualifiedName => GetResourceString("XmlInvalidQualifiedName");

	internal static string XmlInvalidRootData => GetResourceString("XmlInvalidRootData");

	internal static string XmlInvalidStandalone => GetResourceString("XmlInvalidStandalone");

	internal static string XmlInvalidUniqueId => GetResourceString("XmlInvalidUniqueId");

	internal static string XmlInvalidUTF8Bytes => GetResourceString("XmlInvalidUTF8Bytes");

	internal static string XmlInvalidVersion => GetResourceString("XmlInvalidVersion");

	internal static string XmlInvalidWriteState => GetResourceString("XmlInvalidWriteState");

	internal static string XmlInvalidXmlByte => GetResourceString("XmlInvalidXmlByte");

	internal static string XmlInvalidXmlSpace => GetResourceString("XmlInvalidXmlSpace");

	internal static string XmlLineInfo => GetResourceString("XmlLineInfo");

	internal static string XmlMalformedDecl => GetResourceString("XmlMalformedDecl");

	internal static string XmlMaxArrayLengthExceeded => GetResourceString("XmlMaxArrayLengthExceeded");

	internal static string XmlMaxBytesPerReadExceeded => GetResourceString("XmlMaxBytesPerReadExceeded");

	internal static string XmlMaxDepthExceeded => GetResourceString("XmlMaxDepthExceeded");

	internal static string XmlMaxStringContentLengthExceeded => GetResourceString("XmlMaxStringContentLengthExceeded");

	internal static string XmlMethodNotSupported => GetResourceString("XmlMethodNotSupported");

	internal static string XmlMissingLowSurrogate => GetResourceString("XmlMissingLowSurrogate");

	internal static string XmlMultipleRootElements => GetResourceString("XmlMultipleRootElements");

	internal static string XmlNamespaceNotFound => GetResourceString("XmlNamespaceNotFound");

	internal static string XmlNestedArraysNotSupported => GetResourceString("XmlNestedArraysNotSupported");

	internal static string XmlNoRootElement => GetResourceString("XmlNoRootElement");

	internal static string XmlOnlyOneRoot => GetResourceString("XmlOnlyOneRoot");

	internal static string XmlOnlyWhitespace => GetResourceString("XmlOnlyWhitespace");

	internal static string XmlOnlySingleValue => GetResourceString("XmlOnlySingleValue");

	internal static string XmlPrefixBoundToNamespace => GetResourceString("XmlPrefixBoundToNamespace");

	internal static string XmlProcessingInstructionNotSupported => GetResourceString("XmlProcessingInstructionNotSupported");

	internal static string XmlReservedPrefix => GetResourceString("XmlReservedPrefix");

	internal static string XmlSpaceBetweenAttributes => GetResourceString("XmlSpaceBetweenAttributes");

	internal static string XmlSpecificBindingNamespace => GetResourceString("XmlSpecificBindingNamespace");

	internal static string XmlSpecificBindingPrefix => GetResourceString("XmlSpecificBindingPrefix");

	internal static string XmlStartElementLocalNameNsExpected => GetResourceString("XmlStartElementLocalNameNsExpected");

	internal static string XmlStartElementNameExpected => GetResourceString("XmlStartElementNameExpected");

	internal static string XmlTagMismatch => GetResourceString("XmlTagMismatch");

	internal static string XmlTokenExpected => GetResourceString("XmlTokenExpected");

	internal static string XmlUndefinedPrefix => GetResourceString("XmlUndefinedPrefix");

	internal static string XmlUnexpectedEndElement => GetResourceString("XmlUnexpectedEndElement");

	internal static string XmlUnexpectedEndOfFile => GetResourceString("XmlUnexpectedEndOfFile");

	internal static string XmlWriterClosed => GetResourceString("XmlWriterClosed");

	internal static string XmlObjectAssignedToIncompatibleInterface => GetResourceString("XmlObjectAssignedToIncompatibleInterface");

	internal static string CollectionAssignedToIncompatibleInterface => GetResourceString("CollectionAssignedToIncompatibleInterface");

	internal static string JsonInvalidBytes => GetResourceString("JsonInvalidBytes");

	internal static string JsonDuplicateMemberNames => GetResourceString("JsonDuplicateMemberNames");

	internal static string JsonUnsupportedForIsReference => GetResourceString("JsonUnsupportedForIsReference");

	internal static string SerializationCodeIsMissingForType => GetResourceString("SerializationCodeIsMissingForType");

	internal static string InvalidXmlDeserializingExtensionData => GetResourceString("InvalidXmlDeserializingExtensionData");

	internal static string InvalidStateInExtensionDataReader => GetResourceString("InvalidStateInExtensionDataReader");

	internal static string JsonTypeNotSupportedByDataContractJsonSerializer => GetResourceString("JsonTypeNotSupportedByDataContractJsonSerializer");

	internal static string GetOnlyCollectionMustHaveAddMethod => GetResourceString("GetOnlyCollectionMustHaveAddMethod");

	internal static string JsonUnexpectedAttributeValue => GetResourceString("JsonUnexpectedAttributeValue");

	internal static string JsonInvalidDateTimeString => GetResourceString("JsonInvalidDateTimeString");

	internal static string GenericCallbackException => GetResourceString("GenericCallbackException");

	internal static string JsonEncounteredUnexpectedCharacter => GetResourceString("JsonEncounteredUnexpectedCharacter");

	internal static string JsonOffsetExceedsBufferSize => GetResourceString("JsonOffsetExceedsBufferSize");

	internal static string JsonSizeExceedsRemainingBufferSpace => GetResourceString("JsonSizeExceedsRemainingBufferSpace");

	internal static string InvalidCharacterEncountered => GetResourceString("InvalidCharacterEncountered");

	internal static string JsonInvalidFFFE => GetResourceString("JsonInvalidFFFE");

	internal static string JsonDateTimeOutOfRange => GetResourceString("JsonDateTimeOutOfRange");

	internal static string JsonWriteArrayNotSupported => GetResourceString("JsonWriteArrayNotSupported");

	internal static string JsonMethodNotSupported => GetResourceString("JsonMethodNotSupported");

	internal static string JsonNoMatchingStartAttribute => GetResourceString("JsonNoMatchingStartAttribute");

	internal static string JsonNamespaceMustBeEmpty => GetResourceString("JsonNamespaceMustBeEmpty");

	internal static string JsonEndElementNoOpenNodes => GetResourceString("JsonEndElementNoOpenNodes");

	internal static string JsonOpenAttributeMustBeClosedFirst => GetResourceString("JsonOpenAttributeMustBeClosedFirst");

	internal static string JsonMustSpecifyDataType => GetResourceString("JsonMustSpecifyDataType");

	internal static string JsonXmlProcessingInstructionNotSupported => GetResourceString("JsonXmlProcessingInstructionNotSupported");

	internal static string JsonXmlInvalidDeclaration => GetResourceString("JsonXmlInvalidDeclaration");

	internal static string JsonInvalidLocalNameEmpty => GetResourceString("JsonInvalidLocalNameEmpty");

	internal static string JsonPrefixMustBeNullOrEmpty => GetResourceString("JsonPrefixMustBeNullOrEmpty");

	internal static string JsonAttributeMustHaveElement => GetResourceString("JsonAttributeMustHaveElement");

	internal static string JsonAttributeAlreadyWritten => GetResourceString("JsonAttributeAlreadyWritten");

	internal static string JsonServerTypeSpecifiedForInvalidDataType => GetResourceString("JsonServerTypeSpecifiedForInvalidDataType");

	internal static string JsonUnexpectedAttributeLocalName => GetResourceString("JsonUnexpectedAttributeLocalName");

	internal static string JsonInvalidWriteState => GetResourceString("JsonInvalidWriteState");

	internal static string JsonMultipleRootElementsNotAllowedOnWriter => GetResourceString("JsonMultipleRootElementsNotAllowedOnWriter");

	internal static string JsonInvalidRootElementName => GetResourceString("JsonInvalidRootElementName");

	internal static string JsonNodeTypeArrayOrObjectNotSpecified => GetResourceString("JsonNodeTypeArrayOrObjectNotSpecified");

	internal static string JsonInvalidItemNameForArrayElement => GetResourceString("JsonInvalidItemNameForArrayElement");

	internal static string JsonInvalidStartElementCall => GetResourceString("JsonInvalidStartElementCall");

	internal static string JsonOnlyWhitespace => GetResourceString("JsonOnlyWhitespace");

	internal static string JsonWriterClosed => GetResourceString("JsonWriterClosed");

	internal static string JsonCannotWriteStandaloneTextAfterQuotedText => GetResourceString("JsonCannotWriteStandaloneTextAfterQuotedText");

	internal static string JsonMustUseWriteStringForWritingAttributeValues => GetResourceString("JsonMustUseWriteStringForWritingAttributeValues");

	internal static string JsonInvalidDataTypeSpecifiedForServerType => GetResourceString("JsonInvalidDataTypeSpecifiedForServerType");

	internal static string JsonInvalidMethodBetweenStartEndAttribute => GetResourceString("JsonInvalidMethodBetweenStartEndAttribute");

	internal static string JsonCannotWriteTextAfterNonTextAttribute => GetResourceString("JsonCannotWriteTextAfterNonTextAttribute");

	internal static string JsonNestedArraysNotSupported => GetResourceString("JsonNestedArraysNotSupported");

	internal static string JsonEncodingNotSupported => GetResourceString("JsonEncodingNotSupported");

	internal static string JsonExpectedEncoding => GetResourceString("JsonExpectedEncoding");

	internal static string JsonUnexpectedEndOfFile => GetResourceString("JsonUnexpectedEndOfFile");

	internal static string AssemblyNotFound => GetResourceString("AssemblyNotFound");

	internal static string ClrTypeNotFound => GetResourceString("ClrTypeNotFound");

	internal static string AttributeNotFound => GetResourceString("AttributeNotFound");

	internal static string JsonDuplicateMemberInInput => GetResourceString("JsonDuplicateMemberInInput");

	internal static string JsonRequiredMembersNotFound => GetResourceString("JsonRequiredMembersNotFound");

	internal static string JsonOneRequiredMemberNotFound => GetResourceString("JsonOneRequiredMemberNotFound");

	internal static string EnumTypeNotSupportedByDataContractJsonSerializer => GetResourceString("EnumTypeNotSupportedByDataContractJsonSerializer");

	internal static string KeyTypeCannotBeParsedInSimpleDictionary => GetResourceString("KeyTypeCannotBeParsedInSimpleDictionary");

	internal static string SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser => GetResourceString("SurrogatesWithGetOnlyCollectionsNotSupportedSerDeser");

	internal static string FactoryObjectContainsSelfReference => GetResourceString("FactoryObjectContainsSelfReference");

	internal static string RecursiveCollectionType => GetResourceString("RecursiveCollectionType");

	internal static string UnknownXmlType => GetResourceString("UnknownXmlType");

	internal static string DupContractInDataContractSet => GetResourceString("DupContractInDataContractSet");

	internal static string DupTypeContractInDataContractSet => GetResourceString("DupTypeContractInDataContractSet");

	internal static string GenericTypeNotExportable => GetResourceString("GenericTypeNotExportable");

	internal static string CannotExportNullAssembly => GetResourceString("CannotExportNullAssembly");

	internal static string CannotExportNullKnownType => GetResourceString("CannotExportNullKnownType");

	internal static string CannotExportNullType => GetResourceString("CannotExportNullType");

	internal static string QueryGeneratorPathToMemberNotFound => GetResourceString("QueryGeneratorPathToMemberNotFound");

	internal static string XmlInvalidStream => GetResourceString("XmlInvalidStream");

	internal static string ISerializableAssemblyNameSetToZero => GetResourceString("ISerializableAssemblyNameSetToZero");

	internal static string RequiresClassDataContractToSetIsISerializable => GetResourceString("RequiresClassDataContractToSetIsISerializable");

	internal static string ISerializableCannotHaveDataContract => GetResourceString("ISerializableCannotHaveDataContract");

	internal static string SerializationInfo_ConstructorNotFound => GetResourceString("SerializationInfo_ConstructorNotFound");

	internal static string ChangingFullTypeNameNotSupported => GetResourceString("ChangingFullTypeNameNotSupported");

	internal static string InterfaceTypeCannotBeCreated => GetResourceString("InterfaceTypeCannotBeCreated");

	internal static string ArraySizeAttributeIncorrect => GetResourceString("ArraySizeAttributeIncorrect");

	internal static string DuplicateExtensionDataSetMethod => GetResourceString("DuplicateExtensionDataSetMethod");

	internal static string ExtensionDataSetMustReturnVoid => GetResourceString("ExtensionDataSetMustReturnVoid");

	internal static string ExtensionDataSetParameterInvalid => GetResourceString("ExtensionDataSetParameterInvalid");

	internal static string OnlyDataContractTypesCanHaveExtensionData => GetResourceString("OnlyDataContractTypesCanHaveExtensionData");

	internal static string ParseJsonNumberReturnInvalidNumber => GetResourceString("ParseJsonNumberReturnInvalidNumber");

	internal static string CouldNotReadSerializationSchema => GetResourceString("CouldNotReadSerializationSchema");

	internal static string MissingSchemaType => GetResourceString("MissingSchemaType");

	internal static string InvalidReturnSchemaOnGetSchemaMethod => GetResourceString("InvalidReturnSchemaOnGetSchemaMethod");

	internal static string PlatformNotSupported_MtomEncoding => GetResourceString("PlatformNotSupported_MtomEncoding");

	internal static string PlatformNotSupported_NetDataContractSerializer => GetResourceString("PlatformNotSupported_NetDataContractSerializer");

	internal static string PlatformNotSupported_IDataContractSurrogate => GetResourceString("PlatformNotSupported_IDataContractSurrogate");

	internal static string PlatformNotSupported_SchemaImporter => GetResourceString("PlatformNotSupported_SchemaImporter");

	internal static string PlatformNotSupported_Canonicalization => GetResourceString("PlatformNotSupported_Canonicalization");

	internal static string FactoryTypeNotISerializable => GetResourceString("FactoryTypeNotISerializable");

	internal static string XmlCanonicalizationStarted => GetResourceString("XmlCanonicalizationStarted");

	internal static string XmlCanonicalizationNotStarted => GetResourceString("XmlCanonicalizationNotStarted");

	internal static string CombinedPrefixNSLength => GetResourceString("CombinedPrefixNSLength");

	internal static string InvalidInclusivePrefixListCollection => GetResourceString("InvalidInclusivePrefixListCollection");

	internal static string FailedToCreateMethodDelegate => GetResourceString("FailedToCreateMethodDelegate");

	internal static string CannotSetTwice => GetResourceString("CannotSetTwice");

	internal static string MustBeGreaterThanZero => GetResourceString("MustBeGreaterThanZero");

	internal static string ReadOnlyCollectionDeserialization => GetResourceString("ReadOnlyCollectionDeserialization");

	internal static string UnknownNullType => GetResourceString("UnknownNullType");

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

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(provider, resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(provider, resourceFormat, args);
		}
		return resourceFormat;
	}
}
