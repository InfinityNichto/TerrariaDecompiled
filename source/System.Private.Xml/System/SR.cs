using System.Resources;
using FxResources.System.Private.Xml;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Xml_UserException => GetResourceString("Xml_UserException");

	internal static string Xml_DefaultException => GetResourceString("Xml_DefaultException");

	internal static string Xml_InvalidOperation => GetResourceString("Xml_InvalidOperation");

	internal static string Xml_ErrorFilePosition => GetResourceString("Xml_ErrorFilePosition");

	internal static string Xml_StackOverflow => GetResourceString("Xml_StackOverflow");

	internal static string Xslt_NoStylesheetLoaded => GetResourceString("Xslt_NoStylesheetLoaded");

	internal static string Xslt_NotCompiledStylesheet => GetResourceString("Xslt_NotCompiledStylesheet");

	internal static string Xslt_IncompatibleCompiledStylesheetVersion => GetResourceString("Xslt_IncompatibleCompiledStylesheetVersion");

	internal static string Xml_AsyncIsRunningException => GetResourceString("Xml_AsyncIsRunningException");

	internal static string Xml_ReaderAsyncNotSetException => GetResourceString("Xml_ReaderAsyncNotSetException");

	internal static string Xml_UnclosedQuote => GetResourceString("Xml_UnclosedQuote");

	internal static string Xml_UnexpectedEOF => GetResourceString("Xml_UnexpectedEOF");

	internal static string Xml_UnexpectedEOF1 => GetResourceString("Xml_UnexpectedEOF1");

	internal static string Xml_UnexpectedEOFInElementContent => GetResourceString("Xml_UnexpectedEOFInElementContent");

	internal static string Xml_BadStartNameChar => GetResourceString("Xml_BadStartNameChar");

	internal static string Xml_BadNameChar => GetResourceString("Xml_BadNameChar");

	internal static string Xml_BadDecimalEntity => GetResourceString("Xml_BadDecimalEntity");

	internal static string Xml_BadHexEntity => GetResourceString("Xml_BadHexEntity");

	internal static string Xml_MissingByteOrderMark => GetResourceString("Xml_MissingByteOrderMark");

	internal static string Xml_UnknownEncoding => GetResourceString("Xml_UnknownEncoding");

	internal static string Xml_InternalError => GetResourceString("Xml_InternalError");

	internal static string Xml_InvalidCharInThisEncoding => GetResourceString("Xml_InvalidCharInThisEncoding");

	internal static string Xml_ErrorPosition => GetResourceString("Xml_ErrorPosition");

	internal static string Xml_MessageWithErrorPosition => GetResourceString("Xml_MessageWithErrorPosition");

	internal static string Xml_UnexpectedTokenEx => GetResourceString("Xml_UnexpectedTokenEx");

	internal static string Xml_UnexpectedTokens2 => GetResourceString("Xml_UnexpectedTokens2");

	internal static string Xml_ExpectingWhiteSpace => GetResourceString("Xml_ExpectingWhiteSpace");

	internal static string Xml_TagMismatchEx => GetResourceString("Xml_TagMismatchEx");

	internal static string Xml_UnexpectedEndTag => GetResourceString("Xml_UnexpectedEndTag");

	internal static string Xml_UnknownNs => GetResourceString("Xml_UnknownNs");

	internal static string Xml_BadAttributeChar => GetResourceString("Xml_BadAttributeChar");

	internal static string Xml_ExpectExternalOrClose => GetResourceString("Xml_ExpectExternalOrClose");

	internal static string Xml_MissingRoot => GetResourceString("Xml_MissingRoot");

	internal static string Xml_MultipleRoots => GetResourceString("Xml_MultipleRoots");

	internal static string Xml_InvalidRootData => GetResourceString("Xml_InvalidRootData");

	internal static string Xml_XmlDeclNotFirst => GetResourceString("Xml_XmlDeclNotFirst");

	internal static string Xml_InvalidXmlDecl => GetResourceString("Xml_InvalidXmlDecl");

	internal static string Xml_InvalidNodeType => GetResourceString("Xml_InvalidNodeType");

	internal static string Xml_InvalidPIName => GetResourceString("Xml_InvalidPIName");

	internal static string Xml_InvalidXmlSpace => GetResourceString("Xml_InvalidXmlSpace");

	internal static string Xml_InvalidVersionNumber => GetResourceString("Xml_InvalidVersionNumber");

	internal static string Xml_DupAttributeName => GetResourceString("Xml_DupAttributeName");

	internal static string Xml_BadDTDLocation => GetResourceString("Xml_BadDTDLocation");

	internal static string Xml_ElementNotFound => GetResourceString("Xml_ElementNotFound");

	internal static string Xml_ElementNotFoundNs => GetResourceString("Xml_ElementNotFoundNs");

	internal static string Xml_PartialContentNodeTypeNotSupportedEx => GetResourceString("Xml_PartialContentNodeTypeNotSupportedEx");

	internal static string Xml_MultipleDTDsProvided => GetResourceString("Xml_MultipleDTDsProvided");

	internal static string Xml_CanNotBindToReservedNamespace => GetResourceString("Xml_CanNotBindToReservedNamespace");

	internal static string Xml_InvalidCharacter => GetResourceString("Xml_InvalidCharacter");

	internal static string Xml_InvalidBinHexValue => GetResourceString("Xml_InvalidBinHexValue");

	internal static string Xml_InvalidBinHexValueOddCount => GetResourceString("Xml_InvalidBinHexValueOddCount");

	internal static string Xml_InvalidTextDecl => GetResourceString("Xml_InvalidTextDecl");

	internal static string Xml_InvalidBase64Value => GetResourceString("Xml_InvalidBase64Value");

	internal static string Xml_UndeclaredEntity => GetResourceString("Xml_UndeclaredEntity");

	internal static string Xml_RecursiveParEntity => GetResourceString("Xml_RecursiveParEntity");

	internal static string Xml_RecursiveGenEntity => GetResourceString("Xml_RecursiveGenEntity");

	internal static string Xml_ExternalEntityInAttValue => GetResourceString("Xml_ExternalEntityInAttValue");

	internal static string Xml_UnparsedEntityRef => GetResourceString("Xml_UnparsedEntityRef");

	internal static string Xml_NotSameNametable => GetResourceString("Xml_NotSameNametable");

	internal static string Xml_NametableMismatch => GetResourceString("Xml_NametableMismatch");

	internal static string Xml_BadNamespaceDecl => GetResourceString("Xml_BadNamespaceDecl");

	internal static string Xml_ErrorParsingEntityName => GetResourceString("Xml_ErrorParsingEntityName");

	internal static string Xml_InvalidNmToken => GetResourceString("Xml_InvalidNmToken");

	internal static string Xml_EntityRefNesting => GetResourceString("Xml_EntityRefNesting");

	internal static string Xml_CannotResolveEntity => GetResourceString("Xml_CannotResolveEntity");

	internal static string Xml_CannotResolveEntityDtdIgnored => GetResourceString("Xml_CannotResolveEntityDtdIgnored");

	internal static string Xml_CannotResolveExternalSubset => GetResourceString("Xml_CannotResolveExternalSubset");

	internal static string Xml_CannotResolveUrl => GetResourceString("Xml_CannotResolveUrl");

	internal static string Xml_CDATAEndInText => GetResourceString("Xml_CDATAEndInText");

	internal static string Xml_ExternalEntityInStandAloneDocument => GetResourceString("Xml_ExternalEntityInStandAloneDocument");

	internal static string Xml_DtdAfterRootElement => GetResourceString("Xml_DtdAfterRootElement");

	internal static string Xml_ReadOnlyProperty => GetResourceString("Xml_ReadOnlyProperty");

	internal static string Xml_DtdIsProhibited => GetResourceString("Xml_DtdIsProhibited");

	internal static string Xml_DtdIsProhibitedEx => GetResourceString("Xml_DtdIsProhibitedEx");

	internal static string Xml_ReadSubtreeNotOnElement => GetResourceString("Xml_ReadSubtreeNotOnElement");

	internal static string Xml_DtdNotAllowedInFragment => GetResourceString("Xml_DtdNotAllowedInFragment");

	internal static string Xml_CannotStartDocumentOnFragment => GetResourceString("Xml_CannotStartDocumentOnFragment");

	internal static string Xml_ErrorOpeningExternalDtd => GetResourceString("Xml_ErrorOpeningExternalDtd");

	internal static string Xml_ErrorOpeningExternalEntity => GetResourceString("Xml_ErrorOpeningExternalEntity");

	internal static string Xml_ReadBinaryContentNotSupported => GetResourceString("Xml_ReadBinaryContentNotSupported");

	internal static string Xml_ReadValueChunkNotSupported => GetResourceString("Xml_ReadValueChunkNotSupported");

	internal static string Xml_InvalidReadContentAs => GetResourceString("Xml_InvalidReadContentAs");

	internal static string Xml_InvalidReadElementContentAs => GetResourceString("Xml_InvalidReadElementContentAs");

	internal static string Xml_MixedReadElementContentAs => GetResourceString("Xml_MixedReadElementContentAs");

	internal static string Xml_MixingReadValueChunkWithBinary => GetResourceString("Xml_MixingReadValueChunkWithBinary");

	internal static string Xml_MixingBinaryContentMethods => GetResourceString("Xml_MixingBinaryContentMethods");

	internal static string Xml_MixingV1StreamingWithV2Binary => GetResourceString("Xml_MixingV1StreamingWithV2Binary");

	internal static string Xml_InvalidReadValueChunk => GetResourceString("Xml_InvalidReadValueChunk");

	internal static string Xml_ReadContentAsFormatException => GetResourceString("Xml_ReadContentAsFormatException");

	internal static string Xml_DoubleBaseUri => GetResourceString("Xml_DoubleBaseUri");

	internal static string Xml_NotEnoughSpaceForSurrogatePair => GetResourceString("Xml_NotEnoughSpaceForSurrogatePair");

	internal static string Xml_EmptyUrl => GetResourceString("Xml_EmptyUrl");

	internal static string Xml_UnexpectedNodeInSimpleContent => GetResourceString("Xml_UnexpectedNodeInSimpleContent");

	internal static string Xml_InvalidWhitespaceCharacter => GetResourceString("Xml_InvalidWhitespaceCharacter");

	internal static string Xml_IncompatibleConformanceLevel => GetResourceString("Xml_IncompatibleConformanceLevel");

	internal static string Xml_LimitExceeded => GetResourceString("Xml_LimitExceeded");

	internal static string Xml_ClosedOrErrorReader => GetResourceString("Xml_ClosedOrErrorReader");

	internal static string Xml_CharEntityOverflow => GetResourceString("Xml_CharEntityOverflow");

	internal static string Xml_BadNameCharWithPos => GetResourceString("Xml_BadNameCharWithPos");

	internal static string Xml_XmlnsBelongsToReservedNs => GetResourceString("Xml_XmlnsBelongsToReservedNs");

	internal static string Xml_UndeclaredParEntity => GetResourceString("Xml_UndeclaredParEntity");

	internal static string Xml_InvalidXmlDocument => GetResourceString("Xml_InvalidXmlDocument");

	internal static string Xml_NoDTDPresent => GetResourceString("Xml_NoDTDPresent");

	internal static string Xml_MultipleValidaitonTypes => GetResourceString("Xml_MultipleValidaitonTypes");

	internal static string Xml_NoValidation => GetResourceString("Xml_NoValidation");

	internal static string Xml_WhitespaceHandling => GetResourceString("Xml_WhitespaceHandling");

	internal static string Xml_InvalidResetStateCall => GetResourceString("Xml_InvalidResetStateCall");

	internal static string Xml_EntityHandling => GetResourceString("Xml_EntityHandling");

	internal static string Xml_AttlistDuplEnumValue => GetResourceString("Xml_AttlistDuplEnumValue");

	internal static string Xml_AttlistDuplNotationValue => GetResourceString("Xml_AttlistDuplNotationValue");

	internal static string Xml_EncodingSwitchAfterResetState => GetResourceString("Xml_EncodingSwitchAfterResetState");

	internal static string Xml_UnexpectedNodeType => GetResourceString("Xml_UnexpectedNodeType");

	internal static string Xml_InvalidConditionalSection => GetResourceString("Xml_InvalidConditionalSection");

	internal static string Xml_UnexpectedCDataEnd => GetResourceString("Xml_UnexpectedCDataEnd");

	internal static string Xml_UnclosedConditionalSection => GetResourceString("Xml_UnclosedConditionalSection");

	internal static string Xml_ExpectDtdMarkup => GetResourceString("Xml_ExpectDtdMarkup");

	internal static string Xml_IncompleteDtdContent => GetResourceString("Xml_IncompleteDtdContent");

	internal static string Xml_EnumerationRequired => GetResourceString("Xml_EnumerationRequired");

	internal static string Xml_InvalidContentModel => GetResourceString("Xml_InvalidContentModel");

	internal static string Xml_FragmentId => GetResourceString("Xml_FragmentId");

	internal static string Xml_ExpectPcData => GetResourceString("Xml_ExpectPcData");

	internal static string Xml_ExpectNoWhitespace => GetResourceString("Xml_ExpectNoWhitespace");

	internal static string Xml_ExpectOp => GetResourceString("Xml_ExpectOp");

	internal static string Xml_InvalidAttributeType => GetResourceString("Xml_InvalidAttributeType");

	internal static string Xml_InvalidAttributeType1 => GetResourceString("Xml_InvalidAttributeType1");

	internal static string Xml_ExpectAttType => GetResourceString("Xml_ExpectAttType");

	internal static string Xml_ColonInLocalName => GetResourceString("Xml_ColonInLocalName");

	internal static string Xml_InvalidParEntityRef => GetResourceString("Xml_InvalidParEntityRef");

	internal static string Xml_ExpectSubOrClose => GetResourceString("Xml_ExpectSubOrClose");

	internal static string Xml_ExpectExternalOrPublicId => GetResourceString("Xml_ExpectExternalOrPublicId");

	internal static string Xml_ExpectExternalIdOrEntityValue => GetResourceString("Xml_ExpectExternalIdOrEntityValue");

	internal static string Xml_ExpectIgnoreOrInclude => GetResourceString("Xml_ExpectIgnoreOrInclude");

	internal static string Xml_UnsupportedClass => GetResourceString("Xml_UnsupportedClass");

	internal static string Xml_NullResolver => GetResourceString("Xml_NullResolver");

	internal static string Xml_RelativeUriNotSupported => GetResourceString("Xml_RelativeUriNotSupported");

	internal static string Xml_WriterAsyncNotSetException => GetResourceString("Xml_WriterAsyncNotSetException");

	internal static string Xml_PrefixForEmptyNs => GetResourceString("Xml_PrefixForEmptyNs");

	internal static string Xml_InvalidCommentChars => GetResourceString("Xml_InvalidCommentChars");

	internal static string Xml_UndefNamespace => GetResourceString("Xml_UndefNamespace");

	internal static string Xml_EmptyName => GetResourceString("Xml_EmptyName");

	internal static string Xml_EmptyLocalName => GetResourceString("Xml_EmptyLocalName");

	internal static string Xml_InvalidNameCharsDetail => GetResourceString("Xml_InvalidNameCharsDetail");

	internal static string Xml_NoStartTag => GetResourceString("Xml_NoStartTag");

	internal static string Xml_ClosedOrError => GetResourceString("Xml_ClosedOrError");

	internal static string Xml_WrongToken => GetResourceString("Xml_WrongToken");

	internal static string Xml_XmlPrefix => GetResourceString("Xml_XmlPrefix");

	internal static string Xml_XmlnsPrefix => GetResourceString("Xml_XmlnsPrefix");

	internal static string Xml_NamespaceDeclXmlXmlns => GetResourceString("Xml_NamespaceDeclXmlXmlns");

	internal static string Xml_NonWhitespace => GetResourceString("Xml_NonWhitespace");

	internal static string Xml_DupXmlDecl => GetResourceString("Xml_DupXmlDecl");

	internal static string Xml_CannotWriteXmlDecl => GetResourceString("Xml_CannotWriteXmlDecl");

	internal static string Xml_NoRoot => GetResourceString("Xml_NoRoot");

	internal static string Xml_InvalidPosition => GetResourceString("Xml_InvalidPosition");

	internal static string Xml_IncompleteEntity => GetResourceString("Xml_IncompleteEntity");

	internal static string Xml_InvalidSurrogateHighChar => GetResourceString("Xml_InvalidSurrogateHighChar");

	internal static string Xml_InvalidSurrogateMissingLowChar => GetResourceString("Xml_InvalidSurrogateMissingLowChar");

	internal static string Xml_InvalidSurrogatePairWithArgs => GetResourceString("Xml_InvalidSurrogatePairWithArgs");

	internal static string Xml_RedefinePrefix => GetResourceString("Xml_RedefinePrefix");

	internal static string Xml_DtdAlreadyWritten => GetResourceString("Xml_DtdAlreadyWritten");

	internal static string Xml_InvalidCharsInIndent => GetResourceString("Xml_InvalidCharsInIndent");

	internal static string Xml_IndentCharsNotWhitespace => GetResourceString("Xml_IndentCharsNotWhitespace");

	internal static string Xml_ConformanceLevelFragment => GetResourceString("Xml_ConformanceLevelFragment");

	internal static string Xml_InvalidQuote => GetResourceString("Xml_InvalidQuote");

	internal static string Xml_UndefPrefix => GetResourceString("Xml_UndefPrefix");

	internal static string Xml_NoNamespaces => GetResourceString("Xml_NoNamespaces");

	internal static string Xml_InvalidCDataChars => GetResourceString("Xml_InvalidCDataChars");

	internal static string Xml_NotTheFirst => GetResourceString("Xml_NotTheFirst");

	internal static string Xml_InvalidPiChars => GetResourceString("Xml_InvalidPiChars");

	internal static string Xml_InvalidNameChars => GetResourceString("Xml_InvalidNameChars");

	internal static string Xml_Closed => GetResourceString("Xml_Closed");

	internal static string Xml_InvalidPrefix => GetResourceString("Xml_InvalidPrefix");

	internal static string Xml_InvalidIndentation => GetResourceString("Xml_InvalidIndentation");

	internal static string Xml_NotInWriteState => GetResourceString("Xml_NotInWriteState");

	internal static string Xml_SurrogatePairSplit => GetResourceString("Xml_SurrogatePairSplit");

	internal static string Xml_NoMultipleRoots => GetResourceString("Xml_NoMultipleRoots");

	internal static string XmlBadName => GetResourceString("XmlBadName");

	internal static string XmlNoNameAllowed => GetResourceString("XmlNoNameAllowed");

	internal static string XmlConvert_BadUri => GetResourceString("XmlConvert_BadUri");

	internal static string XmlConvert_BadFormat => GetResourceString("XmlConvert_BadFormat");

	internal static string XmlConvert_Overflow => GetResourceString("XmlConvert_Overflow");

	internal static string XmlConvert_TypeBadMapping => GetResourceString("XmlConvert_TypeBadMapping");

	internal static string XmlConvert_TypeBadMapping2 => GetResourceString("XmlConvert_TypeBadMapping2");

	internal static string XmlConvert_TypeListBadMapping => GetResourceString("XmlConvert_TypeListBadMapping");

	internal static string XmlConvert_TypeListBadMapping2 => GetResourceString("XmlConvert_TypeListBadMapping2");

	internal static string XmlConvert_TypeToString => GetResourceString("XmlConvert_TypeToString");

	internal static string XmlConvert_TypeFromString => GetResourceString("XmlConvert_TypeFromString");

	internal static string XmlConvert_TypeNoPrefix => GetResourceString("XmlConvert_TypeNoPrefix");

	internal static string XmlConvert_TypeNoNamespace => GetResourceString("XmlConvert_TypeNoNamespace");

	internal static string XmlConvert_NotOneCharString => GetResourceString("XmlConvert_NotOneCharString");

	internal static string Sch_ParEntityRefNesting => GetResourceString("Sch_ParEntityRefNesting");

	internal static string Sch_NotTokenString => GetResourceString("Sch_NotTokenString");

	internal static string Sch_InvalidDateTimeOption => GetResourceString("Sch_InvalidDateTimeOption");

	internal static string Sch_StandAloneNormalization => GetResourceString("Sch_StandAloneNormalization");

	internal static string Sch_UnSpecifiedDefaultAttributeInExternalStandalone => GetResourceString("Sch_UnSpecifiedDefaultAttributeInExternalStandalone");

	internal static string Sch_DefaultException => GetResourceString("Sch_DefaultException");

	internal static string Sch_DupElementDecl => GetResourceString("Sch_DupElementDecl");

	internal static string Sch_IdAttrDeclared => GetResourceString("Sch_IdAttrDeclared");

	internal static string Sch_RootMatchDocType => GetResourceString("Sch_RootMatchDocType");

	internal static string Sch_DupId => GetResourceString("Sch_DupId");

	internal static string Sch_UndeclaredElement => GetResourceString("Sch_UndeclaredElement");

	internal static string Sch_UndeclaredAttribute => GetResourceString("Sch_UndeclaredAttribute");

	internal static string Sch_UndeclaredNotation => GetResourceString("Sch_UndeclaredNotation");

	internal static string Sch_UndeclaredId => GetResourceString("Sch_UndeclaredId");

	internal static string Sch_SchemaRootExpected => GetResourceString("Sch_SchemaRootExpected");

	internal static string Sch_XSDSchemaRootExpected => GetResourceString("Sch_XSDSchemaRootExpected");

	internal static string Sch_UnsupportedAttribute => GetResourceString("Sch_UnsupportedAttribute");

	internal static string Sch_UnsupportedElement => GetResourceString("Sch_UnsupportedElement");

	internal static string Sch_MissAttribute => GetResourceString("Sch_MissAttribute");

	internal static string Sch_AnnotationLocation => GetResourceString("Sch_AnnotationLocation");

	internal static string Sch_DataTypeTextOnly => GetResourceString("Sch_DataTypeTextOnly");

	internal static string Sch_UnknownModel => GetResourceString("Sch_UnknownModel");

	internal static string Sch_UnknownOrder => GetResourceString("Sch_UnknownOrder");

	internal static string Sch_UnknownContent => GetResourceString("Sch_UnknownContent");

	internal static string Sch_UnknownRequired => GetResourceString("Sch_UnknownRequired");

	internal static string Sch_UnknownDtType => GetResourceString("Sch_UnknownDtType");

	internal static string Sch_MixedMany => GetResourceString("Sch_MixedMany");

	internal static string Sch_GroupDisabled => GetResourceString("Sch_GroupDisabled");

	internal static string Sch_MissDtvalue => GetResourceString("Sch_MissDtvalue");

	internal static string Sch_MissDtvaluesAttribute => GetResourceString("Sch_MissDtvaluesAttribute");

	internal static string Sch_DupDtType => GetResourceString("Sch_DupDtType");

	internal static string Sch_DupAttribute => GetResourceString("Sch_DupAttribute");

	internal static string Sch_RequireEnumeration => GetResourceString("Sch_RequireEnumeration");

	internal static string Sch_DefaultIdValue => GetResourceString("Sch_DefaultIdValue");

	internal static string Sch_ElementNotAllowed => GetResourceString("Sch_ElementNotAllowed");

	internal static string Sch_ElementMissing => GetResourceString("Sch_ElementMissing");

	internal static string Sch_ManyMaxOccurs => GetResourceString("Sch_ManyMaxOccurs");

	internal static string Sch_MaxOccursInvalid => GetResourceString("Sch_MaxOccursInvalid");

	internal static string Sch_MinOccursInvalid => GetResourceString("Sch_MinOccursInvalid");

	internal static string Sch_DtMaxLengthInvalid => GetResourceString("Sch_DtMaxLengthInvalid");

	internal static string Sch_DtMinLengthInvalid => GetResourceString("Sch_DtMinLengthInvalid");

	internal static string Sch_DupDtMaxLength => GetResourceString("Sch_DupDtMaxLength");

	internal static string Sch_DupDtMinLength => GetResourceString("Sch_DupDtMinLength");

	internal static string Sch_DtMinMaxLength => GetResourceString("Sch_DtMinMaxLength");

	internal static string Sch_DupElement => GetResourceString("Sch_DupElement");

	internal static string Sch_DupGroupParticle => GetResourceString("Sch_DupGroupParticle");

	internal static string Sch_InvalidValue => GetResourceString("Sch_InvalidValue");

	internal static string Sch_InvalidValueDetailed => GetResourceString("Sch_InvalidValueDetailed");

	internal static string Sch_InvalidValueDetailedAttribute => GetResourceString("Sch_InvalidValueDetailedAttribute");

	internal static string Sch_MissRequiredAttribute => GetResourceString("Sch_MissRequiredAttribute");

	internal static string Sch_FixedAttributeValue => GetResourceString("Sch_FixedAttributeValue");

	internal static string Sch_FixedElementValue => GetResourceString("Sch_FixedElementValue");

	internal static string Sch_AttributeValueDataTypeDetailed => GetResourceString("Sch_AttributeValueDataTypeDetailed");

	internal static string Sch_AttributeDefaultDataType => GetResourceString("Sch_AttributeDefaultDataType");

	internal static string Sch_IncludeLocation => GetResourceString("Sch_IncludeLocation");

	internal static string Sch_ImportLocation => GetResourceString("Sch_ImportLocation");

	internal static string Sch_RedefineLocation => GetResourceString("Sch_RedefineLocation");

	internal static string Sch_InvalidBlockDefaultValue => GetResourceString("Sch_InvalidBlockDefaultValue");

	internal static string Sch_InvalidFinalDefaultValue => GetResourceString("Sch_InvalidFinalDefaultValue");

	internal static string Sch_InvalidElementBlockValue => GetResourceString("Sch_InvalidElementBlockValue");

	internal static string Sch_InvalidElementFinalValue => GetResourceString("Sch_InvalidElementFinalValue");

	internal static string Sch_InvalidSimpleTypeFinalValue => GetResourceString("Sch_InvalidSimpleTypeFinalValue");

	internal static string Sch_InvalidComplexTypeBlockValue => GetResourceString("Sch_InvalidComplexTypeBlockValue");

	internal static string Sch_InvalidComplexTypeFinalValue => GetResourceString("Sch_InvalidComplexTypeFinalValue");

	internal static string Sch_DupIdentityConstraint => GetResourceString("Sch_DupIdentityConstraint");

	internal static string Sch_DupGlobalElement => GetResourceString("Sch_DupGlobalElement");

	internal static string Sch_DupGlobalAttribute => GetResourceString("Sch_DupGlobalAttribute");

	internal static string Sch_DupSimpleType => GetResourceString("Sch_DupSimpleType");

	internal static string Sch_DupComplexType => GetResourceString("Sch_DupComplexType");

	internal static string Sch_DupGroup => GetResourceString("Sch_DupGroup");

	internal static string Sch_DupAttributeGroup => GetResourceString("Sch_DupAttributeGroup");

	internal static string Sch_DupNotation => GetResourceString("Sch_DupNotation");

	internal static string Sch_DefaultFixedAttributes => GetResourceString("Sch_DefaultFixedAttributes");

	internal static string Sch_FixedInRef => GetResourceString("Sch_FixedInRef");

	internal static string Sch_FixedDefaultInRef => GetResourceString("Sch_FixedDefaultInRef");

	internal static string Sch_DupXsdElement => GetResourceString("Sch_DupXsdElement");

	internal static string Sch_ForbiddenAttribute => GetResourceString("Sch_ForbiddenAttribute");

	internal static string Sch_AttributeIgnored => GetResourceString("Sch_AttributeIgnored");

	internal static string Sch_ElementRef => GetResourceString("Sch_ElementRef");

	internal static string Sch_TypeMutualExclusive => GetResourceString("Sch_TypeMutualExclusive");

	internal static string Sch_ElementNameRef => GetResourceString("Sch_ElementNameRef");

	internal static string Sch_AttributeNameRef => GetResourceString("Sch_AttributeNameRef");

	internal static string Sch_TextNotAllowed => GetResourceString("Sch_TextNotAllowed");

	internal static string Sch_UndeclaredType => GetResourceString("Sch_UndeclaredType");

	internal static string Sch_UndeclaredSimpleType => GetResourceString("Sch_UndeclaredSimpleType");

	internal static string Sch_UndeclaredEquivClass => GetResourceString("Sch_UndeclaredEquivClass");

	internal static string Sch_AttListPresence => GetResourceString("Sch_AttListPresence");

	internal static string Sch_NotationValue => GetResourceString("Sch_NotationValue");

	internal static string Sch_EnumerationValue => GetResourceString("Sch_EnumerationValue");

	internal static string Sch_EmptyAttributeValue => GetResourceString("Sch_EmptyAttributeValue");

	internal static string Sch_InvalidLanguageId => GetResourceString("Sch_InvalidLanguageId");

	internal static string Sch_XmlSpace => GetResourceString("Sch_XmlSpace");

	internal static string Sch_InvalidXsdAttributeValue => GetResourceString("Sch_InvalidXsdAttributeValue");

	internal static string Sch_InvalidXsdAttributeDatatypeValue => GetResourceString("Sch_InvalidXsdAttributeDatatypeValue");

	internal static string Sch_ElementValueDataTypeDetailed => GetResourceString("Sch_ElementValueDataTypeDetailed");

	internal static string Sch_InvalidElementDefaultValue => GetResourceString("Sch_InvalidElementDefaultValue");

	internal static string Sch_NonDeterministic => GetResourceString("Sch_NonDeterministic");

	internal static string Sch_NonDeterministicAnyEx => GetResourceString("Sch_NonDeterministicAnyEx");

	internal static string Sch_NonDeterministicAnyAny => GetResourceString("Sch_NonDeterministicAnyAny");

	internal static string Sch_StandAlone => GetResourceString("Sch_StandAlone");

	internal static string Sch_XmlNsAttribute => GetResourceString("Sch_XmlNsAttribute");

	internal static string Sch_AllElement => GetResourceString("Sch_AllElement");

	internal static string Sch_MismatchTargetNamespaceInclude => GetResourceString("Sch_MismatchTargetNamespaceInclude");

	internal static string Sch_MismatchTargetNamespaceImport => GetResourceString("Sch_MismatchTargetNamespaceImport");

	internal static string Sch_MismatchTargetNamespaceEx => GetResourceString("Sch_MismatchTargetNamespaceEx");

	internal static string Sch_XsiTypeNotFound => GetResourceString("Sch_XsiTypeNotFound");

	internal static string Sch_XsiTypeAbstract => GetResourceString("Sch_XsiTypeAbstract");

	internal static string Sch_ListFromNonatomic => GetResourceString("Sch_ListFromNonatomic");

	internal static string Sch_UnionFromUnion => GetResourceString("Sch_UnionFromUnion");

	internal static string Sch_DupLengthFacet => GetResourceString("Sch_DupLengthFacet");

	internal static string Sch_DupMinLengthFacet => GetResourceString("Sch_DupMinLengthFacet");

	internal static string Sch_DupMaxLengthFacet => GetResourceString("Sch_DupMaxLengthFacet");

	internal static string Sch_DupWhiteSpaceFacet => GetResourceString("Sch_DupWhiteSpaceFacet");

	internal static string Sch_DupMaxInclusiveFacet => GetResourceString("Sch_DupMaxInclusiveFacet");

	internal static string Sch_DupMaxExclusiveFacet => GetResourceString("Sch_DupMaxExclusiveFacet");

	internal static string Sch_DupMinInclusiveFacet => GetResourceString("Sch_DupMinInclusiveFacet");

	internal static string Sch_DupMinExclusiveFacet => GetResourceString("Sch_DupMinExclusiveFacet");

	internal static string Sch_DupTotalDigitsFacet => GetResourceString("Sch_DupTotalDigitsFacet");

	internal static string Sch_DupFractionDigitsFacet => GetResourceString("Sch_DupFractionDigitsFacet");

	internal static string Sch_LengthFacetProhibited => GetResourceString("Sch_LengthFacetProhibited");

	internal static string Sch_MinLengthFacetProhibited => GetResourceString("Sch_MinLengthFacetProhibited");

	internal static string Sch_MaxLengthFacetProhibited => GetResourceString("Sch_MaxLengthFacetProhibited");

	internal static string Sch_PatternFacetProhibited => GetResourceString("Sch_PatternFacetProhibited");

	internal static string Sch_EnumerationFacetProhibited => GetResourceString("Sch_EnumerationFacetProhibited");

	internal static string Sch_WhiteSpaceFacetProhibited => GetResourceString("Sch_WhiteSpaceFacetProhibited");

	internal static string Sch_MaxInclusiveFacetProhibited => GetResourceString("Sch_MaxInclusiveFacetProhibited");

	internal static string Sch_MaxExclusiveFacetProhibited => GetResourceString("Sch_MaxExclusiveFacetProhibited");

	internal static string Sch_MinInclusiveFacetProhibited => GetResourceString("Sch_MinInclusiveFacetProhibited");

	internal static string Sch_MinExclusiveFacetProhibited => GetResourceString("Sch_MinExclusiveFacetProhibited");

	internal static string Sch_TotalDigitsFacetProhibited => GetResourceString("Sch_TotalDigitsFacetProhibited");

	internal static string Sch_FractionDigitsFacetProhibited => GetResourceString("Sch_FractionDigitsFacetProhibited");

	internal static string Sch_LengthFacetInvalid => GetResourceString("Sch_LengthFacetInvalid");

	internal static string Sch_MinLengthFacetInvalid => GetResourceString("Sch_MinLengthFacetInvalid");

	internal static string Sch_MaxLengthFacetInvalid => GetResourceString("Sch_MaxLengthFacetInvalid");

	internal static string Sch_MaxInclusiveFacetInvalid => GetResourceString("Sch_MaxInclusiveFacetInvalid");

	internal static string Sch_MaxExclusiveFacetInvalid => GetResourceString("Sch_MaxExclusiveFacetInvalid");

	internal static string Sch_MinInclusiveFacetInvalid => GetResourceString("Sch_MinInclusiveFacetInvalid");

	internal static string Sch_MinExclusiveFacetInvalid => GetResourceString("Sch_MinExclusiveFacetInvalid");

	internal static string Sch_TotalDigitsFacetInvalid => GetResourceString("Sch_TotalDigitsFacetInvalid");

	internal static string Sch_FractionDigitsFacetInvalid => GetResourceString("Sch_FractionDigitsFacetInvalid");

	internal static string Sch_PatternFacetInvalid => GetResourceString("Sch_PatternFacetInvalid");

	internal static string Sch_EnumerationFacetInvalid => GetResourceString("Sch_EnumerationFacetInvalid");

	internal static string Sch_InvalidWhiteSpace => GetResourceString("Sch_InvalidWhiteSpace");

	internal static string Sch_UnknownFacet => GetResourceString("Sch_UnknownFacet");

	internal static string Sch_LengthAndMinMax => GetResourceString("Sch_LengthAndMinMax");

	internal static string Sch_MinLengthGtMaxLength => GetResourceString("Sch_MinLengthGtMaxLength");

	internal static string Sch_FractionDigitsGtTotalDigits => GetResourceString("Sch_FractionDigitsGtTotalDigits");

	internal static string Sch_LengthConstraintFailed => GetResourceString("Sch_LengthConstraintFailed");

	internal static string Sch_MinLengthConstraintFailed => GetResourceString("Sch_MinLengthConstraintFailed");

	internal static string Sch_MaxLengthConstraintFailed => GetResourceString("Sch_MaxLengthConstraintFailed");

	internal static string Sch_PatternConstraintFailed => GetResourceString("Sch_PatternConstraintFailed");

	internal static string Sch_EnumerationConstraintFailed => GetResourceString("Sch_EnumerationConstraintFailed");

	internal static string Sch_MaxInclusiveConstraintFailed => GetResourceString("Sch_MaxInclusiveConstraintFailed");

	internal static string Sch_MaxExclusiveConstraintFailed => GetResourceString("Sch_MaxExclusiveConstraintFailed");

	internal static string Sch_MinInclusiveConstraintFailed => GetResourceString("Sch_MinInclusiveConstraintFailed");

	internal static string Sch_MinExclusiveConstraintFailed => GetResourceString("Sch_MinExclusiveConstraintFailed");

	internal static string Sch_TotalDigitsConstraintFailed => GetResourceString("Sch_TotalDigitsConstraintFailed");

	internal static string Sch_FractionDigitsConstraintFailed => GetResourceString("Sch_FractionDigitsConstraintFailed");

	internal static string Sch_UnionFailedEx => GetResourceString("Sch_UnionFailedEx");

	internal static string Sch_NotationRequired => GetResourceString("Sch_NotationRequired");

	internal static string Sch_DupNotationAttribute => GetResourceString("Sch_DupNotationAttribute");

	internal static string Sch_MissingPublicSystemAttribute => GetResourceString("Sch_MissingPublicSystemAttribute");

	internal static string Sch_NotationAttributeOnEmptyElement => GetResourceString("Sch_NotationAttributeOnEmptyElement");

	internal static string Sch_RefNotInScope => GetResourceString("Sch_RefNotInScope");

	internal static string Sch_UndeclaredIdentityConstraint => GetResourceString("Sch_UndeclaredIdentityConstraint");

	internal static string Sch_RefInvalidIdentityConstraint => GetResourceString("Sch_RefInvalidIdentityConstraint");

	internal static string Sch_RefInvalidCardin => GetResourceString("Sch_RefInvalidCardin");

	internal static string Sch_ReftoKeyref => GetResourceString("Sch_ReftoKeyref");

	internal static string Sch_EmptyXPath => GetResourceString("Sch_EmptyXPath");

	internal static string Sch_UnresolvedPrefix => GetResourceString("Sch_UnresolvedPrefix");

	internal static string Sch_UnresolvedKeyref => GetResourceString("Sch_UnresolvedKeyref");

	internal static string Sch_ICXpathError => GetResourceString("Sch_ICXpathError");

	internal static string Sch_SelectorAttr => GetResourceString("Sch_SelectorAttr");

	internal static string Sch_FieldSimpleTypeExpected => GetResourceString("Sch_FieldSimpleTypeExpected");

	internal static string Sch_FieldSingleValueExpected => GetResourceString("Sch_FieldSingleValueExpected");

	internal static string Sch_MissingKey => GetResourceString("Sch_MissingKey");

	internal static string Sch_DuplicateKey => GetResourceString("Sch_DuplicateKey");

	internal static string Sch_TargetNamespaceXsi => GetResourceString("Sch_TargetNamespaceXsi");

	internal static string Sch_UndeclaredEntity => GetResourceString("Sch_UndeclaredEntity");

	internal static string Sch_UnparsedEntityRef => GetResourceString("Sch_UnparsedEntityRef");

	internal static string Sch_MaxOccursInvalidXsd => GetResourceString("Sch_MaxOccursInvalidXsd");

	internal static string Sch_MinOccursInvalidXsd => GetResourceString("Sch_MinOccursInvalidXsd");

	internal static string Sch_MaxInclusiveExclusive => GetResourceString("Sch_MaxInclusiveExclusive");

	internal static string Sch_MinInclusiveExclusive => GetResourceString("Sch_MinInclusiveExclusive");

	internal static string Sch_MinInclusiveGtMaxInclusive => GetResourceString("Sch_MinInclusiveGtMaxInclusive");

	internal static string Sch_MinExclusiveGtMaxExclusive => GetResourceString("Sch_MinExclusiveGtMaxExclusive");

	internal static string Sch_MinInclusiveGtMaxExclusive => GetResourceString("Sch_MinInclusiveGtMaxExclusive");

	internal static string Sch_MinExclusiveGtMaxInclusive => GetResourceString("Sch_MinExclusiveGtMaxInclusive");

	internal static string Sch_SimpleTypeRestriction => GetResourceString("Sch_SimpleTypeRestriction");

	internal static string Sch_InvalidFacetPosition => GetResourceString("Sch_InvalidFacetPosition");

	internal static string Sch_AttributeMutuallyExclusive => GetResourceString("Sch_AttributeMutuallyExclusive");

	internal static string Sch_AnyAttributeLastChild => GetResourceString("Sch_AnyAttributeLastChild");

	internal static string Sch_ComplexTypeContentModel => GetResourceString("Sch_ComplexTypeContentModel");

	internal static string Sch_ComplexContentContentModel => GetResourceString("Sch_ComplexContentContentModel");

	internal static string Sch_NotNormalizedString => GetResourceString("Sch_NotNormalizedString");

	internal static string Sch_FractionDigitsNotOnDecimal => GetResourceString("Sch_FractionDigitsNotOnDecimal");

	internal static string Sch_ContentInNill => GetResourceString("Sch_ContentInNill");

	internal static string Sch_NoElementSchemaFound => GetResourceString("Sch_NoElementSchemaFound");

	internal static string Sch_NoAttributeSchemaFound => GetResourceString("Sch_NoAttributeSchemaFound");

	internal static string Sch_InvalidNamespace => GetResourceString("Sch_InvalidNamespace");

	internal static string Sch_InvalidTargetNamespaceAttribute => GetResourceString("Sch_InvalidTargetNamespaceAttribute");

	internal static string Sch_InvalidNamespaceAttribute => GetResourceString("Sch_InvalidNamespaceAttribute");

	internal static string Sch_InvalidSchemaLocation => GetResourceString("Sch_InvalidSchemaLocation");

	internal static string Sch_ImportTargetNamespace => GetResourceString("Sch_ImportTargetNamespace");

	internal static string Sch_ImportTargetNamespaceNull => GetResourceString("Sch_ImportTargetNamespaceNull");

	internal static string Sch_GroupDoubleRedefine => GetResourceString("Sch_GroupDoubleRedefine");

	internal static string Sch_ComponentRedefineNotFound => GetResourceString("Sch_ComponentRedefineNotFound");

	internal static string Sch_GroupRedefineNotFound => GetResourceString("Sch_GroupRedefineNotFound");

	internal static string Sch_AttrGroupDoubleRedefine => GetResourceString("Sch_AttrGroupDoubleRedefine");

	internal static string Sch_AttrGroupRedefineNotFound => GetResourceString("Sch_AttrGroupRedefineNotFound");

	internal static string Sch_ComplexTypeDoubleRedefine => GetResourceString("Sch_ComplexTypeDoubleRedefine");

	internal static string Sch_ComplexTypeRedefineNotFound => GetResourceString("Sch_ComplexTypeRedefineNotFound");

	internal static string Sch_SimpleToComplexTypeRedefine => GetResourceString("Sch_SimpleToComplexTypeRedefine");

	internal static string Sch_SimpleTypeDoubleRedefine => GetResourceString("Sch_SimpleTypeDoubleRedefine");

	internal static string Sch_ComplexToSimpleTypeRedefine => GetResourceString("Sch_ComplexToSimpleTypeRedefine");

	internal static string Sch_SimpleTypeRedefineNotFound => GetResourceString("Sch_SimpleTypeRedefineNotFound");

	internal static string Sch_MinMaxGroupRedefine => GetResourceString("Sch_MinMaxGroupRedefine");

	internal static string Sch_MultipleGroupSelfRef => GetResourceString("Sch_MultipleGroupSelfRef");

	internal static string Sch_MultipleAttrGroupSelfRef => GetResourceString("Sch_MultipleAttrGroupSelfRef");

	internal static string Sch_InvalidTypeRedefine => GetResourceString("Sch_InvalidTypeRedefine");

	internal static string Sch_InvalidElementRef => GetResourceString("Sch_InvalidElementRef");

	internal static string Sch_MinGtMax => GetResourceString("Sch_MinGtMax");

	internal static string Sch_DupSelector => GetResourceString("Sch_DupSelector");

	internal static string Sch_IdConstraintNoSelector => GetResourceString("Sch_IdConstraintNoSelector");

	internal static string Sch_IdConstraintNoFields => GetResourceString("Sch_IdConstraintNoFields");

	internal static string Sch_IdConstraintNoRefer => GetResourceString("Sch_IdConstraintNoRefer");

	internal static string Sch_SelectorBeforeFields => GetResourceString("Sch_SelectorBeforeFields");

	internal static string Sch_NoSimpleTypeContent => GetResourceString("Sch_NoSimpleTypeContent");

	internal static string Sch_SimpleTypeRestRefBase => GetResourceString("Sch_SimpleTypeRestRefBase");

	internal static string Sch_SimpleTypeRestRefBaseNone => GetResourceString("Sch_SimpleTypeRestRefBaseNone");

	internal static string Sch_SimpleTypeListRefBase => GetResourceString("Sch_SimpleTypeListRefBase");

	internal static string Sch_SimpleTypeListRefBaseNone => GetResourceString("Sch_SimpleTypeListRefBaseNone");

	internal static string Sch_SimpleTypeUnionNoBase => GetResourceString("Sch_SimpleTypeUnionNoBase");

	internal static string Sch_NoRestOrExtQName => GetResourceString("Sch_NoRestOrExtQName");

	internal static string Sch_NoRestOrExt => GetResourceString("Sch_NoRestOrExt");

	internal static string Sch_NoGroupParticle => GetResourceString("Sch_NoGroupParticle");

	internal static string Sch_InvalidAllMin => GetResourceString("Sch_InvalidAllMin");

	internal static string Sch_InvalidAllMax => GetResourceString("Sch_InvalidAllMax");

	internal static string Sch_InvalidFacet => GetResourceString("Sch_InvalidFacet");

	internal static string Sch_AbstractElement => GetResourceString("Sch_AbstractElement");

	internal static string Sch_XsiTypeBlockedEx => GetResourceString("Sch_XsiTypeBlockedEx");

	internal static string Sch_InvalidXsiNill => GetResourceString("Sch_InvalidXsiNill");

	internal static string Sch_SubstitutionNotAllowed => GetResourceString("Sch_SubstitutionNotAllowed");

	internal static string Sch_SubstitutionBlocked => GetResourceString("Sch_SubstitutionBlocked");

	internal static string Sch_InvalidElementInEmptyEx => GetResourceString("Sch_InvalidElementInEmptyEx");

	internal static string Sch_InvalidElementInTextOnlyEx => GetResourceString("Sch_InvalidElementInTextOnlyEx");

	internal static string Sch_InvalidTextInElement => GetResourceString("Sch_InvalidTextInElement");

	internal static string Sch_InvalidElementContent => GetResourceString("Sch_InvalidElementContent");

	internal static string Sch_InvalidElementContentComplex => GetResourceString("Sch_InvalidElementContentComplex");

	internal static string Sch_IncompleteContent => GetResourceString("Sch_IncompleteContent");

	internal static string Sch_IncompleteContentComplex => GetResourceString("Sch_IncompleteContentComplex");

	internal static string Sch_InvalidTextInElementExpecting => GetResourceString("Sch_InvalidTextInElementExpecting");

	internal static string Sch_InvalidElementContentExpecting => GetResourceString("Sch_InvalidElementContentExpecting");

	internal static string Sch_InvalidElementContentExpectingComplex => GetResourceString("Sch_InvalidElementContentExpectingComplex");

	internal static string Sch_IncompleteContentExpecting => GetResourceString("Sch_IncompleteContentExpecting");

	internal static string Sch_IncompleteContentExpectingComplex => GetResourceString("Sch_IncompleteContentExpectingComplex");

	internal static string Sch_InvalidElementSubstitution => GetResourceString("Sch_InvalidElementSubstitution");

	internal static string Sch_ElementNameAndNamespace => GetResourceString("Sch_ElementNameAndNamespace");

	internal static string Sch_ElementName => GetResourceString("Sch_ElementName");

	internal static string Sch_ContinuationString => GetResourceString("Sch_ContinuationString");

	internal static string Sch_AnyElementNS => GetResourceString("Sch_AnyElementNS");

	internal static string Sch_AnyElement => GetResourceString("Sch_AnyElement");

	internal static string Sch_InvalidTextInEmpty => GetResourceString("Sch_InvalidTextInEmpty");

	internal static string Sch_InvalidWhitespaceInEmpty => GetResourceString("Sch_InvalidWhitespaceInEmpty");

	internal static string Sch_InvalidPIComment => GetResourceString("Sch_InvalidPIComment");

	internal static string Sch_InvalidAttributeRef => GetResourceString("Sch_InvalidAttributeRef");

	internal static string Sch_OptionalDefaultAttribute => GetResourceString("Sch_OptionalDefaultAttribute");

	internal static string Sch_AttributeCircularRef => GetResourceString("Sch_AttributeCircularRef");

	internal static string Sch_IdentityConstraintCircularRef => GetResourceString("Sch_IdentityConstraintCircularRef");

	internal static string Sch_SubstitutionCircularRef => GetResourceString("Sch_SubstitutionCircularRef");

	internal static string Sch_InvalidAnyAttribute => GetResourceString("Sch_InvalidAnyAttribute");

	internal static string Sch_DupIdAttribute => GetResourceString("Sch_DupIdAttribute");

	internal static string Sch_InvalidAllElementMax => GetResourceString("Sch_InvalidAllElementMax");

	internal static string Sch_InvalidAny => GetResourceString("Sch_InvalidAny");

	internal static string Sch_InvalidAnyDetailed => GetResourceString("Sch_InvalidAnyDetailed");

	internal static string Sch_InvalidExamplar => GetResourceString("Sch_InvalidExamplar");

	internal static string Sch_NoExamplar => GetResourceString("Sch_NoExamplar");

	internal static string Sch_InvalidSubstitutionMember => GetResourceString("Sch_InvalidSubstitutionMember");

	internal static string Sch_RedefineNoSchema => GetResourceString("Sch_RedefineNoSchema");

	internal static string Sch_ProhibitedAttribute => GetResourceString("Sch_ProhibitedAttribute");

	internal static string Sch_TypeCircularRef => GetResourceString("Sch_TypeCircularRef");

	internal static string Sch_TwoIdAttrUses => GetResourceString("Sch_TwoIdAttrUses");

	internal static string Sch_AttrUseAndWildId => GetResourceString("Sch_AttrUseAndWildId");

	internal static string Sch_MoreThanOneWildId => GetResourceString("Sch_MoreThanOneWildId");

	internal static string Sch_BaseFinalExtension => GetResourceString("Sch_BaseFinalExtension");

	internal static string Sch_NotSimpleContent => GetResourceString("Sch_NotSimpleContent");

	internal static string Sch_NotComplexContent => GetResourceString("Sch_NotComplexContent");

	internal static string Sch_BaseFinalRestriction => GetResourceString("Sch_BaseFinalRestriction");

	internal static string Sch_BaseFinalList => GetResourceString("Sch_BaseFinalList");

	internal static string Sch_BaseFinalUnion => GetResourceString("Sch_BaseFinalUnion");

	internal static string Sch_UndefBaseRestriction => GetResourceString("Sch_UndefBaseRestriction");

	internal static string Sch_UndefBaseExtension => GetResourceString("Sch_UndefBaseExtension");

	internal static string Sch_DifContentType => GetResourceString("Sch_DifContentType");

	internal static string Sch_InvalidContentRestriction => GetResourceString("Sch_InvalidContentRestriction");

	internal static string Sch_InvalidContentRestrictionDetailed => GetResourceString("Sch_InvalidContentRestrictionDetailed");

	internal static string Sch_InvalidBaseToEmpty => GetResourceString("Sch_InvalidBaseToEmpty");

	internal static string Sch_InvalidBaseToMixed => GetResourceString("Sch_InvalidBaseToMixed");

	internal static string Sch_DupAttributeUse => GetResourceString("Sch_DupAttributeUse");

	internal static string Sch_InvalidParticleRestriction => GetResourceString("Sch_InvalidParticleRestriction");

	internal static string Sch_InvalidParticleRestrictionDetailed => GetResourceString("Sch_InvalidParticleRestrictionDetailed");

	internal static string Sch_ForbiddenDerivedParticleForAll => GetResourceString("Sch_ForbiddenDerivedParticleForAll");

	internal static string Sch_ForbiddenDerivedParticleForElem => GetResourceString("Sch_ForbiddenDerivedParticleForElem");

	internal static string Sch_ForbiddenDerivedParticleForChoice => GetResourceString("Sch_ForbiddenDerivedParticleForChoice");

	internal static string Sch_ForbiddenDerivedParticleForSeq => GetResourceString("Sch_ForbiddenDerivedParticleForSeq");

	internal static string Sch_ElementFromElement => GetResourceString("Sch_ElementFromElement");

	internal static string Sch_ElementFromAnyRule1 => GetResourceString("Sch_ElementFromAnyRule1");

	internal static string Sch_ElementFromAnyRule2 => GetResourceString("Sch_ElementFromAnyRule2");

	internal static string Sch_AnyFromAnyRule1 => GetResourceString("Sch_AnyFromAnyRule1");

	internal static string Sch_AnyFromAnyRule2 => GetResourceString("Sch_AnyFromAnyRule2");

	internal static string Sch_AnyFromAnyRule3 => GetResourceString("Sch_AnyFromAnyRule3");

	internal static string Sch_GroupBaseFromAny1 => GetResourceString("Sch_GroupBaseFromAny1");

	internal static string Sch_GroupBaseFromAny2 => GetResourceString("Sch_GroupBaseFromAny2");

	internal static string Sch_ElementFromGroupBase1 => GetResourceString("Sch_ElementFromGroupBase1");

	internal static string Sch_ElementFromGroupBase2 => GetResourceString("Sch_ElementFromGroupBase2");

	internal static string Sch_ElementFromGroupBase3 => GetResourceString("Sch_ElementFromGroupBase3");

	internal static string Sch_GroupBaseRestRangeInvalid => GetResourceString("Sch_GroupBaseRestRangeInvalid");

	internal static string Sch_GroupBaseRestNoMap => GetResourceString("Sch_GroupBaseRestNoMap");

	internal static string Sch_GroupBaseRestNotEmptiable => GetResourceString("Sch_GroupBaseRestNotEmptiable");

	internal static string Sch_SeqFromAll => GetResourceString("Sch_SeqFromAll");

	internal static string Sch_SeqFromChoice => GetResourceString("Sch_SeqFromChoice");

	internal static string Sch_UndefGroupRef => GetResourceString("Sch_UndefGroupRef");

	internal static string Sch_GroupCircularRef => GetResourceString("Sch_GroupCircularRef");

	internal static string Sch_AllRefNotRoot => GetResourceString("Sch_AllRefNotRoot");

	internal static string Sch_AllRefMinMax => GetResourceString("Sch_AllRefMinMax");

	internal static string Sch_NotAllAlone => GetResourceString("Sch_NotAllAlone");

	internal static string Sch_AttributeGroupCircularRef => GetResourceString("Sch_AttributeGroupCircularRef");

	internal static string Sch_UndefAttributeGroupRef => GetResourceString("Sch_UndefAttributeGroupRef");

	internal static string Sch_InvalidAttributeExtension => GetResourceString("Sch_InvalidAttributeExtension");

	internal static string Sch_InvalidAnyAttributeRestriction => GetResourceString("Sch_InvalidAnyAttributeRestriction");

	internal static string Sch_AttributeRestrictionProhibited => GetResourceString("Sch_AttributeRestrictionProhibited");

	internal static string Sch_AttributeRestrictionInvalid => GetResourceString("Sch_AttributeRestrictionInvalid");

	internal static string Sch_AttributeFixedInvalid => GetResourceString("Sch_AttributeFixedInvalid");

	internal static string Sch_AttributeUseInvalid => GetResourceString("Sch_AttributeUseInvalid");

	internal static string Sch_AttributeRestrictionInvalidFromWildcard => GetResourceString("Sch_AttributeRestrictionInvalidFromWildcard");

	internal static string Sch_NoDerivedAttribute => GetResourceString("Sch_NoDerivedAttribute");

	internal static string Sch_UnexpressibleAnyAttribute => GetResourceString("Sch_UnexpressibleAnyAttribute");

	internal static string Sch_RefInvalidAttribute => GetResourceString("Sch_RefInvalidAttribute");

	internal static string Sch_ElementCircularRef => GetResourceString("Sch_ElementCircularRef");

	internal static string Sch_RefInvalidElement => GetResourceString("Sch_RefInvalidElement");

	internal static string Sch_ElementCannotHaveValue => GetResourceString("Sch_ElementCannotHaveValue");

	internal static string Sch_ElementInMixedWithFixed => GetResourceString("Sch_ElementInMixedWithFixed");

	internal static string Sch_ElementTypeCollision => GetResourceString("Sch_ElementTypeCollision");

	internal static string Sch_InvalidIncludeLocation => GetResourceString("Sch_InvalidIncludeLocation");

	internal static string Sch_CannotLoadSchema => GetResourceString("Sch_CannotLoadSchema");

	internal static string Sch_CannotLoadSchemaLocation => GetResourceString("Sch_CannotLoadSchemaLocation");

	internal static string Sch_LengthGtBaseLength => GetResourceString("Sch_LengthGtBaseLength");

	internal static string Sch_MinLengthGtBaseMinLength => GetResourceString("Sch_MinLengthGtBaseMinLength");

	internal static string Sch_MaxLengthGtBaseMaxLength => GetResourceString("Sch_MaxLengthGtBaseMaxLength");

	internal static string Sch_MaxMinLengthBaseLength => GetResourceString("Sch_MaxMinLengthBaseLength");

	internal static string Sch_MaxInclusiveMismatch => GetResourceString("Sch_MaxInclusiveMismatch");

	internal static string Sch_MaxExclusiveMismatch => GetResourceString("Sch_MaxExclusiveMismatch");

	internal static string Sch_MinInclusiveMismatch => GetResourceString("Sch_MinInclusiveMismatch");

	internal static string Sch_MinExclusiveMismatch => GetResourceString("Sch_MinExclusiveMismatch");

	internal static string Sch_MinExlIncMismatch => GetResourceString("Sch_MinExlIncMismatch");

	internal static string Sch_MinExlMaxExlMismatch => GetResourceString("Sch_MinExlMaxExlMismatch");

	internal static string Sch_MinIncMaxExlMismatch => GetResourceString("Sch_MinIncMaxExlMismatch");

	internal static string Sch_MinIncExlMismatch => GetResourceString("Sch_MinIncExlMismatch");

	internal static string Sch_MaxIncExlMismatch => GetResourceString("Sch_MaxIncExlMismatch");

	internal static string Sch_MaxExlIncMismatch => GetResourceString("Sch_MaxExlIncMismatch");

	internal static string Sch_TotalDigitsMismatch => GetResourceString("Sch_TotalDigitsMismatch");

	internal static string Sch_FractionDigitsMismatch => GetResourceString("Sch_FractionDigitsMismatch");

	internal static string Sch_FacetBaseFixed => GetResourceString("Sch_FacetBaseFixed");

	internal static string Sch_WhiteSpaceRestriction1 => GetResourceString("Sch_WhiteSpaceRestriction1");

	internal static string Sch_WhiteSpaceRestriction2 => GetResourceString("Sch_WhiteSpaceRestriction2");

	internal static string Sch_XsiNilAndFixed => GetResourceString("Sch_XsiNilAndFixed");

	internal static string Sch_MixSchemaTypes => GetResourceString("Sch_MixSchemaTypes");

	internal static string Sch_XSDSchemaOnly => GetResourceString("Sch_XSDSchemaOnly");

	internal static string Sch_InvalidPublicAttribute => GetResourceString("Sch_InvalidPublicAttribute");

	internal static string Sch_InvalidSystemAttribute => GetResourceString("Sch_InvalidSystemAttribute");

	internal static string Sch_TypeAfterConstraints => GetResourceString("Sch_TypeAfterConstraints");

	internal static string Sch_XsiNilAndType => GetResourceString("Sch_XsiNilAndType");

	internal static string Sch_DupSimpleTypeChild => GetResourceString("Sch_DupSimpleTypeChild");

	internal static string Sch_InvalidIdAttribute => GetResourceString("Sch_InvalidIdAttribute");

	internal static string Sch_InvalidNameAttributeEx => GetResourceString("Sch_InvalidNameAttributeEx");

	internal static string Sch_InvalidAttribute => GetResourceString("Sch_InvalidAttribute");

	internal static string Sch_EmptyChoice => GetResourceString("Sch_EmptyChoice");

	internal static string Sch_DerivedNotFromBase => GetResourceString("Sch_DerivedNotFromBase");

	internal static string Sch_NeedSimpleTypeChild => GetResourceString("Sch_NeedSimpleTypeChild");

	internal static string Sch_InvalidCollection => GetResourceString("Sch_InvalidCollection");

	internal static string Sch_UnrefNS => GetResourceString("Sch_UnrefNS");

	internal static string Sch_InvalidSimpleTypeRestriction => GetResourceString("Sch_InvalidSimpleTypeRestriction");

	internal static string Sch_MultipleRedefine => GetResourceString("Sch_MultipleRedefine");

	internal static string Sch_NullValue => GetResourceString("Sch_NullValue");

	internal static string Sch_ComplexContentModel => GetResourceString("Sch_ComplexContentModel");

	internal static string Sch_SchemaNotPreprocessed => GetResourceString("Sch_SchemaNotPreprocessed");

	internal static string Sch_SchemaNotRemoved => GetResourceString("Sch_SchemaNotRemoved");

	internal static string Sch_ComponentAlreadySeenForNS => GetResourceString("Sch_ComponentAlreadySeenForNS");

	internal static string Sch_DefaultAttributeNotApplied => GetResourceString("Sch_DefaultAttributeNotApplied");

	internal static string Sch_NotXsiAttribute => GetResourceString("Sch_NotXsiAttribute");

	internal static string Sch_SchemaDoesNotExist => GetResourceString("Sch_SchemaDoesNotExist");

	internal static string XmlDocument_ValidateInvalidNodeType => GetResourceString("XmlDocument_ValidateInvalidNodeType");

	internal static string XmlDocument_NodeNotFromDocument => GetResourceString("XmlDocument_NodeNotFromDocument");

	internal static string XmlDocument_NoNodeSchemaInfo => GetResourceString("XmlDocument_NoNodeSchemaInfo");

	internal static string XmlDocument_NoSchemaInfo => GetResourceString("XmlDocument_NoSchemaInfo");

	internal static string Sch_InvalidStartTransition => GetResourceString("Sch_InvalidStartTransition");

	internal static string Sch_InvalidStateTransition => GetResourceString("Sch_InvalidStateTransition");

	internal static string Sch_InvalidEndValidation => GetResourceString("Sch_InvalidEndValidation");

	internal static string Sch_InvalidEndElementCall => GetResourceString("Sch_InvalidEndElementCall");

	internal static string Sch_InvalidEndElementCallTyped => GetResourceString("Sch_InvalidEndElementCallTyped");

	internal static string Sch_InvalidEndElementMultiple => GetResourceString("Sch_InvalidEndElementMultiple");

	internal static string Sch_DuplicateAttribute => GetResourceString("Sch_DuplicateAttribute");

	internal static string Sch_InvalidPartialValidationType => GetResourceString("Sch_InvalidPartialValidationType");

	internal static string Sch_SchemaElementNameMismatch => GetResourceString("Sch_SchemaElementNameMismatch");

	internal static string Sch_SchemaAttributeNameMismatch => GetResourceString("Sch_SchemaAttributeNameMismatch");

	internal static string Sch_ValidateAttributeInvalidCall => GetResourceString("Sch_ValidateAttributeInvalidCall");

	internal static string Sch_ValidateElementInvalidCall => GetResourceString("Sch_ValidateElementInvalidCall");

	internal static string Sch_EnumNotStarted => GetResourceString("Sch_EnumNotStarted");

	internal static string Sch_EnumFinished => GetResourceString("Sch_EnumFinished");

	internal static string SchInf_schema => GetResourceString("SchInf_schema");

	internal static string SchInf_entity => GetResourceString("SchInf_entity");

	internal static string SchInf_simplecontent => GetResourceString("SchInf_simplecontent");

	internal static string SchInf_extension => GetResourceString("SchInf_extension");

	internal static string SchInf_particle => GetResourceString("SchInf_particle");

	internal static string SchInf_ct => GetResourceString("SchInf_ct");

	internal static string SchInf_seq => GetResourceString("SchInf_seq");

	internal static string SchInf_noseq => GetResourceString("SchInf_noseq");

	internal static string SchInf_noct => GetResourceString("SchInf_noct");

	internal static string SchInf_UnknownParticle => GetResourceString("SchInf_UnknownParticle");

	internal static string SchInf_schematype => GetResourceString("SchInf_schematype");

	internal static string SchInf_NoElement => GetResourceString("SchInf_NoElement");

	internal static string Xp_UnclosedString => GetResourceString("Xp_UnclosedString");

	internal static string Xp_ExprExpected => GetResourceString("Xp_ExprExpected");

	internal static string Xp_InvalidArgumentType => GetResourceString("Xp_InvalidArgumentType");

	internal static string Xp_InvalidNumArgs => GetResourceString("Xp_InvalidNumArgs");

	internal static string Xp_InvalidName => GetResourceString("Xp_InvalidName");

	internal static string Xp_InvalidToken => GetResourceString("Xp_InvalidToken");

	internal static string Xp_NodeSetExpected => GetResourceString("Xp_NodeSetExpected");

	internal static string Xp_NotSupported => GetResourceString("Xp_NotSupported");

	internal static string Xp_InvalidPattern => GetResourceString("Xp_InvalidPattern");

	internal static string Xp_InvalidKeyPattern => GetResourceString("Xp_InvalidKeyPattern");

	internal static string Xp_BadQueryObject => GetResourceString("Xp_BadQueryObject");

	internal static string Xp_UndefinedXsltContext => GetResourceString("Xp_UndefinedXsltContext");

	internal static string Xp_NoContext => GetResourceString("Xp_NoContext");

	internal static string Xp_UndefVar => GetResourceString("Xp_UndefVar");

	internal static string Xp_UndefFunc => GetResourceString("Xp_UndefFunc");

	internal static string Xp_FunctionFailed => GetResourceString("Xp_FunctionFailed");

	internal static string Xp_CurrentNotAllowed => GetResourceString("Xp_CurrentNotAllowed");

	internal static string Xp_QueryTooComplex => GetResourceString("Xp_QueryTooComplex");

	internal static string Xdom_DualDocumentTypeNode => GetResourceString("Xdom_DualDocumentTypeNode");

	internal static string Xdom_DualDocumentElementNode => GetResourceString("Xdom_DualDocumentElementNode");

	internal static string Xdom_DualDeclarationNode => GetResourceString("Xdom_DualDeclarationNode");

	internal static string Xdom_Import => GetResourceString("Xdom_Import");

	internal static string Xdom_Import_NullNode => GetResourceString("Xdom_Import_NullNode");

	internal static string Xdom_NoRootEle => GetResourceString("Xdom_NoRootEle");

	internal static string Xdom_Attr_Name => GetResourceString("Xdom_Attr_Name");

	internal static string Xdom_AttrCol_Object => GetResourceString("Xdom_AttrCol_Object");

	internal static string Xdom_AttrCol_Insert => GetResourceString("Xdom_AttrCol_Insert");

	internal static string Xdom_NamedNode_Context => GetResourceString("Xdom_NamedNode_Context");

	internal static string Xdom_Version => GetResourceString("Xdom_Version");

	internal static string Xdom_standalone => GetResourceString("Xdom_standalone");

	internal static string Xdom_Ent_Innertext => GetResourceString("Xdom_Ent_Innertext");

	internal static string Xdom_EntRef_SetVal => GetResourceString("Xdom_EntRef_SetVal");

	internal static string Xdom_WS_Char => GetResourceString("Xdom_WS_Char");

	internal static string Xdom_Node_SetVal => GetResourceString("Xdom_Node_SetVal");

	internal static string Xdom_Empty_LocalName => GetResourceString("Xdom_Empty_LocalName");

	internal static string Xdom_Set_InnerXml => GetResourceString("Xdom_Set_InnerXml");

	internal static string Xdom_Attr_InUse => GetResourceString("Xdom_Attr_InUse");

	internal static string Xdom_Enum_ElementList => GetResourceString("Xdom_Enum_ElementList");

	internal static string Xdom_Invalid_NT_String => GetResourceString("Xdom_Invalid_NT_String");

	internal static string Xdom_InvalidCharacter_EntityReference => GetResourceString("Xdom_InvalidCharacter_EntityReference");

	internal static string Xdom_IndexOutOfRange => GetResourceString("Xdom_IndexOutOfRange");

	internal static string Xdom_Document_Innertext => GetResourceString("Xdom_Document_Innertext");

	internal static string Xpn_BadPosition => GetResourceString("Xpn_BadPosition");

	internal static string Xpn_MissingParent => GetResourceString("Xpn_MissingParent");

	internal static string Xpn_NoContent => GetResourceString("Xpn_NoContent");

	internal static string Xdom_Load_NoDocument => GetResourceString("Xdom_Load_NoDocument");

	internal static string Xdom_Load_NoReader => GetResourceString("Xdom_Load_NoReader");

	internal static string Xdom_Node_Null_Doc => GetResourceString("Xdom_Node_Null_Doc");

	internal static string Xdom_Node_Insert_Child => GetResourceString("Xdom_Node_Insert_Child");

	internal static string Xdom_Node_Insert_Contain => GetResourceString("Xdom_Node_Insert_Contain");

	internal static string Xdom_Node_Insert_Path => GetResourceString("Xdom_Node_Insert_Path");

	internal static string Xdom_Node_Insert_Context => GetResourceString("Xdom_Node_Insert_Context");

	internal static string Xdom_Node_Insert_Location => GetResourceString("Xdom_Node_Insert_Location");

	internal static string Xdom_Node_Insert_TypeConflict => GetResourceString("Xdom_Node_Insert_TypeConflict");

	internal static string Xdom_Node_Remove_Contain => GetResourceString("Xdom_Node_Remove_Contain");

	internal static string Xdom_Node_Remove_Child => GetResourceString("Xdom_Node_Remove_Child");

	internal static string Xdom_Node_Modify_ReadOnly => GetResourceString("Xdom_Node_Modify_ReadOnly");

	internal static string Xdom_TextNode_SplitText => GetResourceString("Xdom_TextNode_SplitText");

	internal static string Xdom_Attr_Reserved_XmlNS => GetResourceString("Xdom_Attr_Reserved_XmlNS");

	internal static string Xdom_Node_Cloning => GetResourceString("Xdom_Node_Cloning");

	internal static string Xnr_ResolveEntity => GetResourceString("Xnr_ResolveEntity");

	internal static string XPathDocument_MissingSchemas => GetResourceString("XPathDocument_MissingSchemas");

	internal static string XPathDocument_NotEnoughSchemaInfo => GetResourceString("XPathDocument_NotEnoughSchemaInfo");

	internal static string XPathDocument_ValidateInvalidNodeType => GetResourceString("XPathDocument_ValidateInvalidNodeType");

	internal static string XPathDocument_SchemaSetNotAllowed => GetResourceString("XPathDocument_SchemaSetNotAllowed");

	internal static string XmlBin_MissingEndCDATA => GetResourceString("XmlBin_MissingEndCDATA");

	internal static string XmlBin_InvalidQNameID => GetResourceString("XmlBin_InvalidQNameID");

	internal static string XmlBinary_UnexpectedToken => GetResourceString("XmlBinary_UnexpectedToken");

	internal static string XmlBinary_InvalidSqlDecimal => GetResourceString("XmlBinary_InvalidSqlDecimal");

	internal static string XmlBinary_InvalidSignature => GetResourceString("XmlBinary_InvalidSignature");

	internal static string XmlBinary_InvalidProtocolVersion => GetResourceString("XmlBinary_InvalidProtocolVersion");

	internal static string XmlBinary_UnsupportedCodePage => GetResourceString("XmlBinary_UnsupportedCodePage");

	internal static string XmlBinary_InvalidStandalone => GetResourceString("XmlBinary_InvalidStandalone");

	internal static string XmlBinary_NoParserContext => GetResourceString("XmlBinary_NoParserContext");

	internal static string XmlBinary_ListsOfValuesNotSupported => GetResourceString("XmlBinary_ListsOfValuesNotSupported");

	internal static string XmlBinary_CastNotSupported => GetResourceString("XmlBinary_CastNotSupported");

	internal static string XmlBinary_NoRemapPrefix => GetResourceString("XmlBinary_NoRemapPrefix");

	internal static string XmlBinary_AttrWithNsNoPrefix => GetResourceString("XmlBinary_AttrWithNsNoPrefix");

	internal static string XmlBinary_ValueTooBig => GetResourceString("XmlBinary_ValueTooBig");

	internal static string SqlTypes_ArithOverflow => GetResourceString("SqlTypes_ArithOverflow");

	internal static string XmlMissingType => GetResourceString("XmlMissingType");

	internal static string XmlSerializerUnsupportedType => GetResourceString("XmlSerializerUnsupportedType");

	internal static string XmlSerializerUnsupportedMember => GetResourceString("XmlSerializerUnsupportedMember");

	internal static string XmlUnsupportedTypeKind => GetResourceString("XmlUnsupportedTypeKind");

	internal static string XmlUnsupportedSoapTypeKind => GetResourceString("XmlUnsupportedSoapTypeKind");

	internal static string XmlUnsupportedIDictionary => GetResourceString("XmlUnsupportedIDictionary");

	internal static string XmlUnsupportedIDictionaryDetails => GetResourceString("XmlUnsupportedIDictionaryDetails");

	internal static string XmlDuplicateTypeName => GetResourceString("XmlDuplicateTypeName");

	internal static string XmlSerializableNameMissing1 => GetResourceString("XmlSerializableNameMissing1");

	internal static string XmlConstructorInaccessible => GetResourceString("XmlConstructorInaccessible");

	internal static string XmlTypeInaccessible => GetResourceString("XmlTypeInaccessible");

	internal static string XmlTypeStatic => GetResourceString("XmlTypeStatic");

	internal static string XmlNoDefaultAccessors => GetResourceString("XmlNoDefaultAccessors");

	internal static string XmlNoAddMethod => GetResourceString("XmlNoAddMethod");

	internal static string XmlReadOnlyPropertyError => GetResourceString("XmlReadOnlyPropertyError");

	internal static string XmlAttributeSetAgain => GetResourceString("XmlAttributeSetAgain");

	internal static string XmlIllegalWildcard => GetResourceString("XmlIllegalWildcard");

	internal static string XmlIllegalArrayElement => GetResourceString("XmlIllegalArrayElement");

	internal static string XmlIllegalForm => GetResourceString("XmlIllegalForm");

	internal static string XmlBareTextMember => GetResourceString("XmlBareTextMember");

	internal static string XmlBareAttributeMember => GetResourceString("XmlBareAttributeMember");

	internal static string XmlReflectionError => GetResourceString("XmlReflectionError");

	internal static string XmlTypeReflectionError => GetResourceString("XmlTypeReflectionError");

	internal static string XmlPropertyReflectionError => GetResourceString("XmlPropertyReflectionError");

	internal static string XmlFieldReflectionError => GetResourceString("XmlFieldReflectionError");

	internal static string XmlInvalidDataTypeUsage => GetResourceString("XmlInvalidDataTypeUsage");

	internal static string XmlInvalidXsdDataType => GetResourceString("XmlInvalidXsdDataType");

	internal static string XmlDataTypeMismatch => GetResourceString("XmlDataTypeMismatch");

	internal static string XmlIllegalTypeContext => GetResourceString("XmlIllegalTypeContext");

	internal static string XmlUdeclaredXsdType => GetResourceString("XmlUdeclaredXsdType");

	internal static string XmlInvalidConstantAttribute => GetResourceString("XmlInvalidConstantAttribute");

	internal static string XmlIllegalAttributesArrayAttribute => GetResourceString("XmlIllegalAttributesArrayAttribute");

	internal static string XmlIllegalElementsArrayAttribute => GetResourceString("XmlIllegalElementsArrayAttribute");

	internal static string XmlIllegalArrayArrayAttribute => GetResourceString("XmlIllegalArrayArrayAttribute");

	internal static string XmlIllegalAttribute => GetResourceString("XmlIllegalAttribute");

	internal static string XmlIllegalType => GetResourceString("XmlIllegalType");

	internal static string XmlIllegalAttrOrText => GetResourceString("XmlIllegalAttrOrText");

	internal static string XmlIllegalSoapAttribute => GetResourceString("XmlIllegalSoapAttribute");

	internal static string XmlIllegalAttrOrTextInterface => GetResourceString("XmlIllegalAttrOrTextInterface");

	internal static string XmlIllegalAttributeFlagsArray => GetResourceString("XmlIllegalAttributeFlagsArray");

	internal static string XmlIllegalAnyElement => GetResourceString("XmlIllegalAnyElement");

	internal static string XmlInvalidIsNullable => GetResourceString("XmlInvalidIsNullable");

	internal static string XmlInvalidNotNullable => GetResourceString("XmlInvalidNotNullable");

	internal static string XmlInvalidFormUnqualified => GetResourceString("XmlInvalidFormUnqualified");

	internal static string XmlDuplicateNamespace => GetResourceString("XmlDuplicateNamespace");

	internal static string XmlElementHasNoName => GetResourceString("XmlElementHasNoName");

	internal static string XmlAttributeHasNoName => GetResourceString("XmlAttributeHasNoName");

	internal static string XmlElementImportedTwice => GetResourceString("XmlElementImportedTwice");

	internal static string XmlHiddenMember => GetResourceString("XmlHiddenMember");

	internal static string XmlInvalidXmlOverride => GetResourceString("XmlInvalidXmlOverride");

	internal static string XmlMembersDeriveError => GetResourceString("XmlMembersDeriveError");

	internal static string XmlTypeUsedTwice => GetResourceString("XmlTypeUsedTwice");

	internal static string XmlMissingGroup => GetResourceString("XmlMissingGroup");

	internal static string XmlMissingAttributeGroup => GetResourceString("XmlMissingAttributeGroup");

	internal static string XmlMissingDataType => GetResourceString("XmlMissingDataType");

	internal static string XmlInvalidEncoding => GetResourceString("XmlInvalidEncoding");

	internal static string XmlMissingElement => GetResourceString("XmlMissingElement");

	internal static string XmlMissingAttribute => GetResourceString("XmlMissingAttribute");

	internal static string XmlMissingMethodEnum => GetResourceString("XmlMissingMethodEnum");

	internal static string XmlNoAttributeHere => GetResourceString("XmlNoAttributeHere");

	internal static string XmlNeedAttributeHere => GetResourceString("XmlNeedAttributeHere");

	internal static string XmlElementNameMismatch => GetResourceString("XmlElementNameMismatch");

	internal static string XmlUnsupportedDefaultType => GetResourceString("XmlUnsupportedDefaultType");

	internal static string XmlUnsupportedDefaultValue => GetResourceString("XmlUnsupportedDefaultValue");

	internal static string XmlInvalidDefaultValue => GetResourceString("XmlInvalidDefaultValue");

	internal static string XmlInvalidDefaultEnumValue => GetResourceString("XmlInvalidDefaultEnumValue");

	internal static string XmlUnknownNode => GetResourceString("XmlUnknownNode");

	internal static string XmlUnknownConstant => GetResourceString("XmlUnknownConstant");

	internal static string XmlSerializeError => GetResourceString("XmlSerializeError");

	internal static string XmlSerializeErrorDetails => GetResourceString("XmlSerializeErrorDetails");

	internal static string XmlSchemaDuplicateNamespace => GetResourceString("XmlSchemaDuplicateNamespace");

	internal static string XmlSchemaCompiled => GetResourceString("XmlSchemaCompiled");

	internal static string XmlInvalidArrayDimentions => GetResourceString("XmlInvalidArrayDimentions");

	internal static string XmlInvalidArrayTypeName => GetResourceString("XmlInvalidArrayTypeName");

	internal static string XmlInvalidArrayTypeNamespace => GetResourceString("XmlInvalidArrayTypeNamespace");

	internal static string XmlMissingArrayType => GetResourceString("XmlMissingArrayType");

	internal static string XmlEmptyArrayType => GetResourceString("XmlEmptyArrayType");

	internal static string XmlInvalidArraySyntax => GetResourceString("XmlInvalidArraySyntax");

	internal static string XmlInvalidArrayTypeSyntax => GetResourceString("XmlInvalidArrayTypeSyntax");

	internal static string XmlMismatchedArrayBrackets => GetResourceString("XmlMismatchedArrayBrackets");

	internal static string XmlInvalidArrayLength => GetResourceString("XmlInvalidArrayLength");

	internal static string XmlMissingHref => GetResourceString("XmlMissingHref");

	internal static string XmlInvalidHref => GetResourceString("XmlInvalidHref");

	internal static string XmlUnknownType => GetResourceString("XmlUnknownType");

	internal static string XmlAbstractType => GetResourceString("XmlAbstractType");

	internal static string XmlMappingsScopeMismatch => GetResourceString("XmlMappingsScopeMismatch");

	internal static string XmlMethodTypeNameConflict => GetResourceString("XmlMethodTypeNameConflict");

	internal static string XmlCannotReconcileAccessor => GetResourceString("XmlCannotReconcileAccessor");

	internal static string XmlCannotReconcileAttributeAccessor => GetResourceString("XmlCannotReconcileAttributeAccessor");

	internal static string XmlCannotReconcileAccessorDefault => GetResourceString("XmlCannotReconcileAccessorDefault");

	internal static string XmlInvalidTypeAttributes => GetResourceString("XmlInvalidTypeAttributes");

	internal static string XmlInvalidAttributeUse => GetResourceString("XmlInvalidAttributeUse");

	internal static string XmlTypesDuplicate => GetResourceString("XmlTypesDuplicate");

	internal static string XmlInvalidSoapArray => GetResourceString("XmlInvalidSoapArray");

	internal static string XmlCannotIncludeInSchema => GetResourceString("XmlCannotIncludeInSchema");

	internal static string XmlInvalidSerializable => GetResourceString("XmlInvalidSerializable");

	internal static string XmlInvalidUseOfType => GetResourceString("XmlInvalidUseOfType");

	internal static string XmlUnxpectedType => GetResourceString("XmlUnxpectedType");

	internal static string XmlUnknownAnyElement => GetResourceString("XmlUnknownAnyElement");

	internal static string XmlMultipleAttributeOverrides => GetResourceString("XmlMultipleAttributeOverrides");

	internal static string XmlInvalidEnumAttribute => GetResourceString("XmlInvalidEnumAttribute");

	internal static string XmlInvalidReturnPosition => GetResourceString("XmlInvalidReturnPosition");

	internal static string XmlInvalidElementAttribute => GetResourceString("XmlInvalidElementAttribute");

	internal static string XmlInvalidVoid => GetResourceString("XmlInvalidVoid");

	internal static string XmlInvalidContent => GetResourceString("XmlInvalidContent");

	internal static string XmlInvalidAttributeType => GetResourceString("XmlInvalidAttributeType");

	internal static string XmlInvalidBaseType => GetResourceString("XmlInvalidBaseType");

	internal static string XmlInvalidIdentifier => GetResourceString("XmlInvalidIdentifier");

	internal static string XmlGenError => GetResourceString("XmlGenError");

	internal static string XmlInvalidXmlns => GetResourceString("XmlInvalidXmlns");

	internal static string XmlCircularReference => GetResourceString("XmlCircularReference");

	internal static string XmlCircularReference2 => GetResourceString("XmlCircularReference2");

	internal static string XmlAnonymousBaseType => GetResourceString("XmlAnonymousBaseType");

	internal static string XmlMissingSchema => GetResourceString("XmlMissingSchema");

	internal static string XmlNoSerializableMembers => GetResourceString("XmlNoSerializableMembers");

	internal static string XmlIllegalOverride => GetResourceString("XmlIllegalOverride");

	internal static string XmlReadOnlyCollection => GetResourceString("XmlReadOnlyCollection");

	internal static string XmlRpcNestedValueType => GetResourceString("XmlRpcNestedValueType");

	internal static string XmlRpcRefsInValueType => GetResourceString("XmlRpcRefsInValueType");

	internal static string XmlRpcArrayOfValueTypes => GetResourceString("XmlRpcArrayOfValueTypes");

	internal static string XmlDuplicateElementName => GetResourceString("XmlDuplicateElementName");

	internal static string XmlDuplicateAttributeName => GetResourceString("XmlDuplicateAttributeName");

	internal static string XmlBadBaseElement => GetResourceString("XmlBadBaseElement");

	internal static string XmlBadBaseType => GetResourceString("XmlBadBaseType");

	internal static string XmlUndefinedAlias => GetResourceString("XmlUndefinedAlias");

	internal static string XmlChoiceIdentifierType => GetResourceString("XmlChoiceIdentifierType");

	internal static string XmlChoiceIdentifierArrayType => GetResourceString("XmlChoiceIdentifierArrayType");

	internal static string XmlChoiceIdentifierTypeEnum => GetResourceString("XmlChoiceIdentifierTypeEnum");

	internal static string XmlChoiceIdentiferMemberMissing => GetResourceString("XmlChoiceIdentiferMemberMissing");

	internal static string XmlChoiceIdentiferAmbiguous => GetResourceString("XmlChoiceIdentiferAmbiguous");

	internal static string XmlChoiceIdentiferMissing => GetResourceString("XmlChoiceIdentiferMissing");

	internal static string XmlChoiceMissingValue => GetResourceString("XmlChoiceMissingValue");

	internal static string XmlChoiceMissingAnyValue => GetResourceString("XmlChoiceMissingAnyValue");

	internal static string XmlChoiceMismatchChoiceException => GetResourceString("XmlChoiceMismatchChoiceException");

	internal static string XmlArrayItemAmbiguousTypes => GetResourceString("XmlArrayItemAmbiguousTypes");

	internal static string XmlUnsupportedInterface => GetResourceString("XmlUnsupportedInterface");

	internal static string XmlUnsupportedInterfaceDetails => GetResourceString("XmlUnsupportedInterfaceDetails");

	internal static string XmlUnsupportedRank => GetResourceString("XmlUnsupportedRank");

	internal static string XmlUnsupportedInheritance => GetResourceString("XmlUnsupportedInheritance");

	internal static string XmlIllegalMultipleText => GetResourceString("XmlIllegalMultipleText");

	internal static string XmlIllegalMultipleTextMembers => GetResourceString("XmlIllegalMultipleTextMembers");

	internal static string XmlIllegalArrayTextAttribute => GetResourceString("XmlIllegalArrayTextAttribute");

	internal static string XmlIllegalTypedTextAttribute => GetResourceString("XmlIllegalTypedTextAttribute");

	internal static string XmlIllegalSimpleContentExtension => GetResourceString("XmlIllegalSimpleContentExtension");

	internal static string XmlInvalidCast => GetResourceString("XmlInvalidCast");

	internal static string XmlInvalidCastWithId => GetResourceString("XmlInvalidCastWithId");

	internal static string XmlInvalidArrayRef => GetResourceString("XmlInvalidArrayRef");

	internal static string XmlInvalidNullCast => GetResourceString("XmlInvalidNullCast");

	internal static string XmlMultipleXmlns => GetResourceString("XmlMultipleXmlns");

	internal static string XmlMultipleXmlnsMembers => GetResourceString("XmlMultipleXmlnsMembers");

	internal static string XmlXmlnsInvalidType => GetResourceString("XmlXmlnsInvalidType");

	internal static string XmlSoleXmlnsAttribute => GetResourceString("XmlSoleXmlnsAttribute");

	internal static string XmlConstructorHasSecurityAttributes => GetResourceString("XmlConstructorHasSecurityAttributes");

	internal static string XmlInvalidChoiceIdentifierValue => GetResourceString("XmlInvalidChoiceIdentifierValue");

	internal static string XmlAnyElementDuplicate => GetResourceString("XmlAnyElementDuplicate");

	internal static string XmlChoiceIdDuplicate => GetResourceString("XmlChoiceIdDuplicate");

	internal static string XmlChoiceIdentifierMismatch => GetResourceString("XmlChoiceIdentifierMismatch");

	internal static string XmlUnsupportedRedefine => GetResourceString("XmlUnsupportedRedefine");

	internal static string XmlDuplicateElementInScope => GetResourceString("XmlDuplicateElementInScope");

	internal static string XmlDuplicateElementInScope1 => GetResourceString("XmlDuplicateElementInScope1");

	internal static string XmlNoPartialTrust => GetResourceString("XmlNoPartialTrust");

	internal static string XmlInvalidEncodingNotEncoded1 => GetResourceString("XmlInvalidEncodingNotEncoded1");

	internal static string XmlInvalidEncoding3 => GetResourceString("XmlInvalidEncoding3");

	internal static string XmlInvalidSpecifiedType => GetResourceString("XmlInvalidSpecifiedType");

	internal static string XmlUnsupportedOpenGenericType => GetResourceString("XmlUnsupportedOpenGenericType");

	internal static string XmlMismatchSchemaObjects => GetResourceString("XmlMismatchSchemaObjects");

	internal static string XmlCircularTypeReference => GetResourceString("XmlCircularTypeReference");

	internal static string XmlCircularGroupReference => GetResourceString("XmlCircularGroupReference");

	internal static string XmlRpcLitElementNamespace => GetResourceString("XmlRpcLitElementNamespace");

	internal static string XmlRpcLitElementNullable => GetResourceString("XmlRpcLitElementNullable");

	internal static string XmlRpcLitElements => GetResourceString("XmlRpcLitElements");

	internal static string XmlRpcLitArrayElement => GetResourceString("XmlRpcLitArrayElement");

	internal static string XmlRpcLitAttributeAttributes => GetResourceString("XmlRpcLitAttributeAttributes");

	internal static string XmlRpcLitAttributes => GetResourceString("XmlRpcLitAttributes");

	internal static string XmlSequenceMembers => GetResourceString("XmlSequenceMembers");

	internal static string XmlRpcLitXmlns => GetResourceString("XmlRpcLitXmlns");

	internal static string XmlDuplicateNs => GetResourceString("XmlDuplicateNs");

	internal static string XmlAnonymousInclude => GetResourceString("XmlAnonymousInclude");

	internal static string XmlSchemaIncludeLocation => GetResourceString("XmlSchemaIncludeLocation");

	internal static string XmlSerializableSchemaError => GetResourceString("XmlSerializableSchemaError");

	internal static string XmlGetSchemaMethodName => GetResourceString("XmlGetSchemaMethodName");

	internal static string XmlGetSchemaMethodMissing => GetResourceString("XmlGetSchemaMethodMissing");

	internal static string XmlGetSchemaMethodReturnType => GetResourceString("XmlGetSchemaMethodReturnType");

	internal static string XmlGetSchemaEmptyTypeName => GetResourceString("XmlGetSchemaEmptyTypeName");

	internal static string XmlGetSchemaTypeMissing => GetResourceString("XmlGetSchemaTypeMissing");

	internal static string XmlGetSchemaInclude => GetResourceString("XmlGetSchemaInclude");

	internal static string XmlSerializableAttributes => GetResourceString("XmlSerializableAttributes");

	internal static string XmlSerializableMergeItem => GetResourceString("XmlSerializableMergeItem");

	internal static string XmlSerializableBadDerivation => GetResourceString("XmlSerializableBadDerivation");

	internal static string XmlSerializableMissingClrType => GetResourceString("XmlSerializableMissingClrType");

	internal static string XmlCircularDerivation => GetResourceString("XmlCircularDerivation");

	internal static string XmlMelformMapping => GetResourceString("XmlMelformMapping");

	internal static string XmlSchemaSyntaxErrorDetails => GetResourceString("XmlSchemaSyntaxErrorDetails");

	internal static string XmlSchemaElementReference => GetResourceString("XmlSchemaElementReference");

	internal static string XmlSchemaAttributeReference => GetResourceString("XmlSchemaAttributeReference");

	internal static string XmlSchemaItem => GetResourceString("XmlSchemaItem");

	internal static string XmlSchemaNamedItem => GetResourceString("XmlSchemaNamedItem");

	internal static string XmlSchemaContentDef => GetResourceString("XmlSchemaContentDef");

	internal static string XmlSchema => GetResourceString("XmlSchema");

	internal static string XmlSerializableRootDupName => GetResourceString("XmlSerializableRootDupName");

	internal static string XmlNotSerializable => GetResourceString("XmlNotSerializable");

	internal static string XmlPregenInvalidXmlSerializerAssemblyAttribute => GetResourceString("XmlPregenInvalidXmlSerializerAssemblyAttribute");

	internal static string XmlSequenceInconsistent => GetResourceString("XmlSequenceInconsistent");

	internal static string XmlSequenceUnique => GetResourceString("XmlSequenceUnique");

	internal static string XmlSequenceHierarchy => GetResourceString("XmlSequenceHierarchy");

	internal static string XmlSequenceMatch => GetResourceString("XmlSequenceMatch");

	internal static string XmlDisallowNegativeValues => GetResourceString("XmlDisallowNegativeValues");

	internal static string Xml_UnexpectedToken => GetResourceString("Xml_UnexpectedToken");

	internal static string Sch_AttributeValueDataType => GetResourceString("Sch_AttributeValueDataType");

	internal static string Sch_ElementValueDataType => GetResourceString("Sch_ElementValueDataType");

	internal static string XmlInternalError => GetResourceString("XmlInternalError");

	internal static string XmlInternalErrorDetails => GetResourceString("XmlInternalErrorDetails");

	internal static string Arg_NeverValueType => GetResourceString("Arg_NeverValueType");

	internal static string Enc_InvalidByteInEncoding => GetResourceString("Enc_InvalidByteInEncoding");

	internal static string Arg_ExpectingXmlTextReader => GetResourceString("Arg_ExpectingXmlTextReader");

	internal static string Arg_CannotCreateNode => GetResourceString("Arg_CannotCreateNode");

	internal static string Arg_IncompatibleParamType => GetResourceString("Arg_IncompatibleParamType");

	internal static string Xml_EndOfInnerExceptionStack => GetResourceString("Xml_EndOfInnerExceptionStack");

	internal static string XPath_UnclosedString => GetResourceString("XPath_UnclosedString");

	internal static string XPath_ScientificNotation => GetResourceString("XPath_ScientificNotation");

	internal static string XPath_UnexpectedToken => GetResourceString("XPath_UnexpectedToken");

	internal static string XPath_NodeTestExpected => GetResourceString("XPath_NodeTestExpected");

	internal static string XPath_EofExpected => GetResourceString("XPath_EofExpected");

	internal static string XPath_TokenExpected => GetResourceString("XPath_TokenExpected");

	internal static string XPath_InvalidAxisInPattern => GetResourceString("XPath_InvalidAxisInPattern");

	internal static string XPath_PredicateAfterDot => GetResourceString("XPath_PredicateAfterDot");

	internal static string XPath_PredicateAfterDotDot => GetResourceString("XPath_PredicateAfterDotDot");

	internal static string XPath_NArgsExpected => GetResourceString("XPath_NArgsExpected");

	internal static string XPath_NOrMArgsExpected => GetResourceString("XPath_NOrMArgsExpected");

	internal static string XPath_AtLeastNArgsExpected => GetResourceString("XPath_AtLeastNArgsExpected");

	internal static string XPath_AtMostMArgsExpected => GetResourceString("XPath_AtMostMArgsExpected");

	internal static string XPath_NodeSetArgumentExpected => GetResourceString("XPath_NodeSetArgumentExpected");

	internal static string XPath_NodeSetExpected => GetResourceString("XPath_NodeSetExpected");

	internal static string XPath_RtfInPathExpr => GetResourceString("XPath_RtfInPathExpr");

	internal static string Xslt_WarningAsError => GetResourceString("Xslt_WarningAsError");

	internal static string Xslt_InputTooComplex => GetResourceString("Xslt_InputTooComplex");

	internal static string Xslt_CannotLoadStylesheet => GetResourceString("Xslt_CannotLoadStylesheet");

	internal static string Xslt_WrongStylesheetElement => GetResourceString("Xslt_WrongStylesheetElement");

	internal static string Xslt_WdXslNamespace => GetResourceString("Xslt_WdXslNamespace");

	internal static string Xslt_NotAtTop => GetResourceString("Xslt_NotAtTop");

	internal static string Xslt_UnexpectedElement => GetResourceString("Xslt_UnexpectedElement");

	internal static string Xslt_NullNsAtTopLevel => GetResourceString("Xslt_NullNsAtTopLevel");

	internal static string Xslt_TextNodesNotAllowed => GetResourceString("Xslt_TextNodesNotAllowed");

	internal static string Xslt_NotEmptyContents => GetResourceString("Xslt_NotEmptyContents");

	internal static string Xslt_InvalidAttribute => GetResourceString("Xslt_InvalidAttribute");

	internal static string Xslt_MissingAttribute => GetResourceString("Xslt_MissingAttribute");

	internal static string Xslt_InvalidAttrValue => GetResourceString("Xslt_InvalidAttrValue");

	internal static string Xslt_BistateAttribute => GetResourceString("Xslt_BistateAttribute");

	internal static string Xslt_CharAttribute => GetResourceString("Xslt_CharAttribute");

	internal static string Xslt_CircularInclude => GetResourceString("Xslt_CircularInclude");

	internal static string Xslt_SingleRightBraceInAvt => GetResourceString("Xslt_SingleRightBraceInAvt");

	internal static string Xslt_VariableCntSel2 => GetResourceString("Xslt_VariableCntSel2");

	internal static string Xslt_KeyCntUse => GetResourceString("Xslt_KeyCntUse");

	internal static string Xslt_DupTemplateName => GetResourceString("Xslt_DupTemplateName");

	internal static string Xslt_BothMatchNameAbsent => GetResourceString("Xslt_BothMatchNameAbsent");

	internal static string Xslt_InvalidVariable => GetResourceString("Xslt_InvalidVariable");

	internal static string Xslt_DupGlobalVariable => GetResourceString("Xslt_DupGlobalVariable");

	internal static string Xslt_DupLocalVariable => GetResourceString("Xslt_DupLocalVariable");

	internal static string Xslt_DupNsAlias => GetResourceString("Xslt_DupNsAlias");

	internal static string Xslt_EmptyAttrValue => GetResourceString("Xslt_EmptyAttrValue");

	internal static string Xslt_EmptyNsAlias => GetResourceString("Xslt_EmptyNsAlias");

	internal static string Xslt_UnknownXsltFunction => GetResourceString("Xslt_UnknownXsltFunction");

	internal static string Xslt_UnsupportedXsltFunction => GetResourceString("Xslt_UnsupportedXsltFunction");

	internal static string Xslt_NoAttributeSet => GetResourceString("Xslt_NoAttributeSet");

	internal static string Xslt_UndefinedKey => GetResourceString("Xslt_UndefinedKey");

	internal static string Xslt_CircularAttributeSet => GetResourceString("Xslt_CircularAttributeSet");

	internal static string Xslt_InvalidCallTemplate => GetResourceString("Xslt_InvalidCallTemplate");

	internal static string Xslt_InvalidPrefix => GetResourceString("Xslt_InvalidPrefix");

	internal static string Xslt_ScriptXsltNamespace => GetResourceString("Xslt_ScriptXsltNamespace");

	internal static string Xslt_ScriptInvalidLanguage => GetResourceString("Xslt_ScriptInvalidLanguage");

	internal static string Xslt_ScriptMixedLanguages => GetResourceString("Xslt_ScriptMixedLanguages");

	internal static string Xslt_ScriptAndExtensionClash => GetResourceString("Xslt_ScriptAndExtensionClash");

	internal static string Xslt_NoDecimalFormat => GetResourceString("Xslt_NoDecimalFormat");

	internal static string Xslt_DecimalFormatSignsNotDistinct => GetResourceString("Xslt_DecimalFormatSignsNotDistinct");

	internal static string Xslt_DecimalFormatRedefined => GetResourceString("Xslt_DecimalFormatRedefined");

	internal static string Xslt_UnknownExtensionElement => GetResourceString("Xslt_UnknownExtensionElement");

	internal static string Xslt_ModeWithoutMatch => GetResourceString("Xslt_ModeWithoutMatch");

	internal static string Xslt_ModeListEmpty => GetResourceString("Xslt_ModeListEmpty");

	internal static string Xslt_ModeListDup => GetResourceString("Xslt_ModeListDup");

	internal static string Xslt_ModeListAll => GetResourceString("Xslt_ModeListAll");

	internal static string Xslt_PriorityWithoutMatch => GetResourceString("Xslt_PriorityWithoutMatch");

	internal static string Xslt_InvalidApplyImports => GetResourceString("Xslt_InvalidApplyImports");

	internal static string Xslt_DuplicateWithParam => GetResourceString("Xslt_DuplicateWithParam");

	internal static string Xslt_ReservedNS => GetResourceString("Xslt_ReservedNS");

	internal static string Xslt_XmlnsAttr => GetResourceString("Xslt_XmlnsAttr");

	internal static string Xslt_NoWhen => GetResourceString("Xslt_NoWhen");

	internal static string Xslt_WhenAfterOtherwise => GetResourceString("Xslt_WhenAfterOtherwise");

	internal static string Xslt_DupOtherwise => GetResourceString("Xslt_DupOtherwise");

	internal static string Xslt_AttributeRedefinition => GetResourceString("Xslt_AttributeRedefinition");

	internal static string Xslt_InvalidMethod => GetResourceString("Xslt_InvalidMethod");

	internal static string Xslt_InvalidEncoding => GetResourceString("Xslt_InvalidEncoding");

	internal static string Xslt_InvalidLanguage => GetResourceString("Xslt_InvalidLanguage");

	internal static string Xslt_InvalidCompareOption => GetResourceString("Xslt_InvalidCompareOption");

	internal static string Xslt_KeyNotAllowed => GetResourceString("Xslt_KeyNotAllowed");

	internal static string Xslt_VariablesNotAllowed => GetResourceString("Xslt_VariablesNotAllowed");

	internal static string Xslt_CurrentNotAllowed => GetResourceString("Xslt_CurrentNotAllowed");

	internal static string Xslt_DocumentFuncProhibited => GetResourceString("Xslt_DocumentFuncProhibited");

	internal static string Xslt_ScriptsProhibited => GetResourceString("Xslt_ScriptsProhibited");

	internal static string Xslt_ItemNull => GetResourceString("Xslt_ItemNull");

	internal static string Xslt_NodeSetNotNode => GetResourceString("Xslt_NodeSetNotNode");

	internal static string Xslt_UnsupportedClrType => GetResourceString("Xslt_UnsupportedClrType");

	internal static string Xslt_NotYetImplemented => GetResourceString("Xslt_NotYetImplemented");

	internal static string Xslt_SchemaAttribute => GetResourceString("Xslt_SchemaAttribute");

	internal static string Xslt_SchemaAttributeValue => GetResourceString("Xslt_SchemaAttributeValue");

	internal static string Xslt_ElementCntSel => GetResourceString("Xslt_ElementCntSel");

	internal static string Xslt_RequiredAndSelect => GetResourceString("Xslt_RequiredAndSelect");

	internal static string Xslt_NoSelectNoContent => GetResourceString("Xslt_NoSelectNoContent");

	internal static string Xslt_NonTemplateTunnel => GetResourceString("Xslt_NonTemplateTunnel");

	internal static string Xslt_ExcludeDefault => GetResourceString("Xslt_ExcludeDefault");

	internal static string Xslt_CollationSyntax => GetResourceString("Xslt_CollationSyntax");

	internal static string Xslt_SortStable => GetResourceString("Xslt_SortStable");

	internal static string Xslt_InputTypeAnnotations => GetResourceString("Xslt_InputTypeAnnotations");

	internal static string Coll_BadOptFormat => GetResourceString("Coll_BadOptFormat");

	internal static string Coll_Unsupported => GetResourceString("Coll_Unsupported");

	internal static string Coll_UnsupportedLanguage => GetResourceString("Coll_UnsupportedLanguage");

	internal static string Coll_UnsupportedOpt => GetResourceString("Coll_UnsupportedOpt");

	internal static string Coll_UnsupportedOptVal => GetResourceString("Coll_UnsupportedOptVal");

	internal static string Coll_UnsupportedSortOpt => GetResourceString("Coll_UnsupportedSortOpt");

	internal static string XmlIl_TooManyParameters => GetResourceString("XmlIl_TooManyParameters");

	internal static string XmlIl_BadXmlState => GetResourceString("XmlIl_BadXmlState");

	internal static string XmlIl_BadXmlStateAttr => GetResourceString("XmlIl_BadXmlStateAttr");

	internal static string XmlIl_NmspAfterAttr => GetResourceString("XmlIl_NmspAfterAttr");

	internal static string XmlIl_NmspConflict => GetResourceString("XmlIl_NmspConflict");

	internal static string XmlIl_CantResolveEntity => GetResourceString("XmlIl_CantResolveEntity");

	internal static string XmlIl_NoDefaultDocument => GetResourceString("XmlIl_NoDefaultDocument");

	internal static string XmlIl_UnknownDocument => GetResourceString("XmlIl_UnknownDocument");

	internal static string XmlIl_UnknownParam => GetResourceString("XmlIl_UnknownParam");

	internal static string XmlIl_UnknownExtObj => GetResourceString("XmlIl_UnknownExtObj");

	internal static string XmlIl_CantStripNav => GetResourceString("XmlIl_CantStripNav");

	internal static string XmlIl_ExtensionError => GetResourceString("XmlIl_ExtensionError");

	internal static string XmlIl_TopLevelAttrNmsp => GetResourceString("XmlIl_TopLevelAttrNmsp");

	internal static string XmlIl_NoExtensionMethod => GetResourceString("XmlIl_NoExtensionMethod");

	internal static string XmlIl_AmbiguousExtensionMethod => GetResourceString("XmlIl_AmbiguousExtensionMethod");

	internal static string XmlIl_NonPublicExtensionMethod => GetResourceString("XmlIl_NonPublicExtensionMethod");

	internal static string XmlIl_GenericExtensionMethod => GetResourceString("XmlIl_GenericExtensionMethod");

	internal static string XmlIl_ByRefType => GetResourceString("XmlIl_ByRefType");

	internal static string XmlIl_DocumentLoadError => GetResourceString("XmlIl_DocumentLoadError");

	internal static string Xslt_CompileError => GetResourceString("Xslt_CompileError");

	internal static string Xslt_CompileError2 => GetResourceString("Xslt_CompileError2");

	internal static string Xslt_UnsuppFunction => GetResourceString("Xslt_UnsuppFunction");

	internal static string Xslt_NotFirstImport => GetResourceString("Xslt_NotFirstImport");

	internal static string Xslt_UnexpectedKeyword => GetResourceString("Xslt_UnexpectedKeyword");

	internal static string Xslt_InvalidContents => GetResourceString("Xslt_InvalidContents");

	internal static string Xslt_CantResolve => GetResourceString("Xslt_CantResolve");

	internal static string Xslt_SingleRightAvt => GetResourceString("Xslt_SingleRightAvt");

	internal static string Xslt_OpenBracesAvt => GetResourceString("Xslt_OpenBracesAvt");

	internal static string Xslt_OpenLiteralAvt => GetResourceString("Xslt_OpenLiteralAvt");

	internal static string Xslt_NestedAvt => GetResourceString("Xslt_NestedAvt");

	internal static string Xslt_EmptyAvtExpr => GetResourceString("Xslt_EmptyAvtExpr");

	internal static string Xslt_InvalidXPath => GetResourceString("Xslt_InvalidXPath");

	internal static string Xslt_InvalidQName => GetResourceString("Xslt_InvalidQName");

	internal static string Xslt_TemplateNoAttrib => GetResourceString("Xslt_TemplateNoAttrib");

	internal static string Xslt_DupVarName => GetResourceString("Xslt_DupVarName");

	internal static string Xslt_WrongNumberArgs => GetResourceString("Xslt_WrongNumberArgs");

	internal static string Xslt_NoNodeSetConversion => GetResourceString("Xslt_NoNodeSetConversion");

	internal static string Xslt_NoNavigatorConversion => GetResourceString("Xslt_NoNavigatorConversion");

	internal static string Xslt_InvalidFormat => GetResourceString("Xslt_InvalidFormat");

	internal static string Xslt_InvalidFormat1 => GetResourceString("Xslt_InvalidFormat1");

	internal static string Xslt_InvalidFormat2 => GetResourceString("Xslt_InvalidFormat2");

	internal static string Xslt_InvalidFormat3 => GetResourceString("Xslt_InvalidFormat3");

	internal static string Xslt_InvalidFormat5 => GetResourceString("Xslt_InvalidFormat5");

	internal static string Xslt_InvalidFormat8 => GetResourceString("Xslt_InvalidFormat8");

	internal static string Xslt_ScriptInvalidPrefix => GetResourceString("Xslt_ScriptInvalidPrefix");

	internal static string Xslt_ScriptEmpty => GetResourceString("Xslt_ScriptEmpty");

	internal static string Xslt_DupDecimalFormat => GetResourceString("Xslt_DupDecimalFormat");

	internal static string Xslt_CircularReference => GetResourceString("Xslt_CircularReference");

	internal static string Xslt_InvalidExtensionNamespace => GetResourceString("Xslt_InvalidExtensionNamespace");

	internal static string Xslt_InvalidModeAttribute => GetResourceString("Xslt_InvalidModeAttribute");

	internal static string Xslt_MultipleRoots => GetResourceString("Xslt_MultipleRoots");

	internal static string Xslt_ApplyImports => GetResourceString("Xslt_ApplyImports");

	internal static string Xslt_Terminate => GetResourceString("Xslt_Terminate");

	internal static string Xslt_InvalidPattern => GetResourceString("Xslt_InvalidPattern");

	internal static string XmlInvalidCharSchemaPrimitive => GetResourceString("XmlInvalidCharSchemaPrimitive");

	internal static string UnknownConstantType => GetResourceString("UnknownConstantType");

	internal static string ArrayTypeIsNotSupported => GetResourceString("ArrayTypeIsNotSupported");

	internal static string XmlPregenTypeDynamic => GetResourceString("XmlPregenTypeDynamic");

	internal static string XmlPregenOrphanType => GetResourceString("XmlPregenOrphanType");

	internal static string FailLoadAssemblyUnderPregenMode => GetResourceString("FailLoadAssemblyUnderPregenMode");

	internal static string CompilingScriptsNotSupported => GetResourceString("CompilingScriptsNotSupported");

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

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}
}
