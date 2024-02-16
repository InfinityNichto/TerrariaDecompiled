using System.Collections.Generic;
using System.IO;
using System.Private.CoreLib;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System;

internal static class SR
{
	private static readonly object _lock = new object();

	private static List<string> _currentlyLoading;

	private static int _infinitelyRecursingCount;

	private static bool _resourceManagerInited;

	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(Strings)));

	internal static string Acc_CreateAbstEx => GetResourceString("Acc_CreateAbstEx");

	internal static string Acc_CreateArgIterator => GetResourceString("Acc_CreateArgIterator");

	internal static string Acc_CreateGenericEx => GetResourceString("Acc_CreateGenericEx");

	internal static string Acc_CreateInterfaceEx => GetResourceString("Acc_CreateInterfaceEx");

	internal static string Acc_CreateVoid => GetResourceString("Acc_CreateVoid");

	internal static string Acc_NotClassInit => GetResourceString("Acc_NotClassInit");

	internal static string Acc_ReadOnly => GetResourceString("Acc_ReadOnly");

	internal static string Access_Void => GetResourceString("Access_Void");

	internal static string AggregateException_ctor_DefaultMessage => GetResourceString("AggregateException_ctor_DefaultMessage");

	internal static string AggregateException_ctor_InnerExceptionNull => GetResourceString("AggregateException_ctor_InnerExceptionNull");

	internal static string AggregateException_DeserializationFailure => GetResourceString("AggregateException_DeserializationFailure");

	internal static string AggregateException_InnerException => GetResourceString("AggregateException_InnerException");

	internal static string AppDomain_Name => GetResourceString("AppDomain_Name");

	internal static string AppDomain_NoContextPolicies => GetResourceString("AppDomain_NoContextPolicies");

	internal static string AppDomain_Policy_PrincipalTwice => GetResourceString("AppDomain_Policy_PrincipalTwice");

	internal static string AmbiguousImplementationException_NullMessage => GetResourceString("AmbiguousImplementationException_NullMessage");

	internal static string Arg_AccessException => GetResourceString("Arg_AccessException");

	internal static string Arg_AccessViolationException => GetResourceString("Arg_AccessViolationException");

	internal static string Arg_AmbiguousMatchException => GetResourceString("Arg_AmbiguousMatchException");

	internal static string Arg_ApplicationException => GetResourceString("Arg_ApplicationException");

	internal static string Arg_ArgumentException => GetResourceString("Arg_ArgumentException");

	internal static string Arg_ArgumentOutOfRangeException => GetResourceString("Arg_ArgumentOutOfRangeException");

	internal static string Arg_ArithmeticException => GetResourceString("Arg_ArithmeticException");

	internal static string Arg_ArrayLengthsDiffer => GetResourceString("Arg_ArrayLengthsDiffer");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string Arg_ArrayTypeMismatchException => GetResourceString("Arg_ArrayTypeMismatchException");

	internal static string Arg_ArrayZeroError => GetResourceString("Arg_ArrayZeroError");

	internal static string Arg_BadDecimal => GetResourceString("Arg_BadDecimal");

	internal static string Arg_BadImageFormatException => GetResourceString("Arg_BadImageFormatException");

	internal static string Arg_BadLiteralFormat => GetResourceString("Arg_BadLiteralFormat");

	internal static string Arg_BogusIComparer => GetResourceString("Arg_BogusIComparer");

	internal static string Arg_BufferTooSmall => GetResourceString("Arg_BufferTooSmall");

	internal static string Arg_CannotBeNaN => GetResourceString("Arg_CannotBeNaN");

	internal static string Arg_CannotHaveNegativeValue => GetResourceString("Arg_CannotHaveNegativeValue");

	internal static string Arg_CannotMixComparisonInfrastructure => GetResourceString("Arg_CannotMixComparisonInfrastructure");

	internal static string Arg_CannotUnloadAppDomainException => GetResourceString("Arg_CannotUnloadAppDomainException");

	internal static string Arg_CATypeResolutionFailed => GetResourceString("Arg_CATypeResolutionFailed");

	internal static string Arg_COMAccess => GetResourceString("Arg_COMAccess");

	internal static string Arg_COMException => GetResourceString("Arg_COMException");

	internal static string Arg_COMPropSetPut => GetResourceString("Arg_COMPropSetPut");

	internal static string Arg_CreatInstAccess => GetResourceString("Arg_CreatInstAccess");

	internal static string Arg_CryptographyException => GetResourceString("Arg_CryptographyException");

	internal static string Arg_CustomAttributeFormatException => GetResourceString("Arg_CustomAttributeFormatException");

	internal static string Arg_DataMisalignedException => GetResourceString("Arg_DataMisalignedException");

	internal static string Arg_DecBitCtor => GetResourceString("Arg_DecBitCtor");

	internal static string Arg_DirectoryNotFoundException => GetResourceString("Arg_DirectoryNotFoundException");

	internal static string Arg_DivideByZero => GetResourceString("Arg_DivideByZero");

	internal static string Arg_DlgtNullInst => GetResourceString("Arg_DlgtNullInst");

	internal static string Arg_DlgtTargMeth => GetResourceString("Arg_DlgtTargMeth");

	internal static string Arg_DlgtTypeMis => GetResourceString("Arg_DlgtTypeMis");

	internal static string Arg_DllNotFoundException => GetResourceString("Arg_DllNotFoundException");

	internal static string Arg_DuplicateWaitObjectException => GetResourceString("Arg_DuplicateWaitObjectException");

	internal static string Arg_EHClauseNotClause => GetResourceString("Arg_EHClauseNotClause");

	internal static string Arg_EHClauseNotFilter => GetResourceString("Arg_EHClauseNotFilter");

	internal static string Arg_EmptyArray => GetResourceString("Arg_EmptyArray");

	internal static string Arg_EndOfStreamException => GetResourceString("Arg_EndOfStreamException");

	internal static string Arg_EntryPointNotFoundException => GetResourceString("Arg_EntryPointNotFoundException");

	internal static string Arg_EnumAndObjectMustBeSameType => GetResourceString("Arg_EnumAndObjectMustBeSameType");

	internal static string Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType => GetResourceString("Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType");

	internal static string Arg_EnumIllegalVal => GetResourceString("Arg_EnumIllegalVal");

	internal static string Arg_EnumLitValueNotFound => GetResourceString("Arg_EnumLitValueNotFound");

	internal static string Arg_EnumUnderlyingTypeAndObjectMustBeSameType => GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType");

	internal static string Arg_EnumValueNotFound => GetResourceString("Arg_EnumValueNotFound");

	internal static string Arg_ExecutionEngineException => GetResourceString("Arg_ExecutionEngineException");

	internal static string Arg_ExternalException => GetResourceString("Arg_ExternalException");

	internal static string Arg_FieldAccessException => GetResourceString("Arg_FieldAccessException");

	internal static string Arg_FieldDeclTarget => GetResourceString("Arg_FieldDeclTarget");

	internal static string Arg_FldGetArgErr => GetResourceString("Arg_FldGetArgErr");

	internal static string Arg_FldGetPropSet => GetResourceString("Arg_FldGetPropSet");

	internal static string Arg_FldSetArgErr => GetResourceString("Arg_FldSetArgErr");

	internal static string Arg_FldSetGet => GetResourceString("Arg_FldSetGet");

	internal static string Arg_FldSetInvoke => GetResourceString("Arg_FldSetInvoke");

	internal static string Arg_FldSetPropGet => GetResourceString("Arg_FldSetPropGet");

	internal static string Arg_FormatException => GetResourceString("Arg_FormatException");

	internal static string Arg_GenericParameter => GetResourceString("Arg_GenericParameter");

	internal static string Arg_GetMethNotFnd => GetResourceString("Arg_GetMethNotFnd");

	internal static string Arg_GuidArrayCtor => GetResourceString("Arg_GuidArrayCtor");

	internal static string Arg_HandleNotAsync => GetResourceString("Arg_HandleNotAsync");

	internal static string Arg_HandleNotSync => GetResourceString("Arg_HandleNotSync");

	internal static string Arg_HexStyleNotSupported => GetResourceString("Arg_HexStyleNotSupported");

	internal static string Arg_HTCapacityOverflow => GetResourceString("Arg_HTCapacityOverflow");

	internal static string Arg_IndexMustBeInt => GetResourceString("Arg_IndexMustBeInt");

	internal static string Arg_IndexOutOfRangeException => GetResourceString("Arg_IndexOutOfRangeException");

	internal static string Arg_InsufficientExecutionStackException => GetResourceString("Arg_InsufficientExecutionStackException");

	internal static string Arg_InvalidANSIString => GetResourceString("Arg_InvalidANSIString");

	internal static string Arg_InvalidBase => GetResourceString("Arg_InvalidBase");

	internal static string Arg_InvalidCastException => GetResourceString("Arg_InvalidCastException");

	internal static string Arg_InvalidComObjectException => GetResourceString("Arg_InvalidComObjectException");

	internal static string Arg_InvalidFilterCriteriaException => GetResourceString("Arg_InvalidFilterCriteriaException");

	internal static string Arg_InvalidHandle => GetResourceString("Arg_InvalidHandle");

	internal static string Arg_InvalidHexStyle => GetResourceString("Arg_InvalidHexStyle");

	internal static string Arg_InvalidNeutralResourcesLanguage_Asm_Culture => GetResourceString("Arg_InvalidNeutralResourcesLanguage_Asm_Culture");

	internal static string Arg_InvalidNeutralResourcesLanguage_FallbackLoc => GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc");

	internal static string Arg_InvalidSatelliteContract_Asm_Ver => GetResourceString("Arg_InvalidSatelliteContract_Asm_Ver");

	internal static string Arg_InvalidOleVariantTypeException => GetResourceString("Arg_InvalidOleVariantTypeException");

	internal static string Arg_InvalidOperationException => GetResourceString("Arg_InvalidOperationException");

	internal static string Arg_InvalidTypeInRetType => GetResourceString("Arg_InvalidTypeInRetType");

	internal static string Arg_InvalidTypeInSignature => GetResourceString("Arg_InvalidTypeInSignature");

	internal static string Arg_IOException => GetResourceString("Arg_IOException");

	internal static string Arg_KeyNotFound => GetResourceString("Arg_KeyNotFound");

	internal static string Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey");

	internal static string Arg_LongerThanDestArray => GetResourceString("Arg_LongerThanDestArray");

	internal static string Arg_LongerThanSrcArray => GetResourceString("Arg_LongerThanSrcArray");

	internal static string Arg_LongerThanSrcString => GetResourceString("Arg_LongerThanSrcString");

	internal static string Arg_LowerBoundsMustMatch => GetResourceString("Arg_LowerBoundsMustMatch");

	internal static string Arg_MarshalAsAnyRestriction => GetResourceString("Arg_MarshalAsAnyRestriction");

	internal static string Arg_MarshalDirectiveException => GetResourceString("Arg_MarshalDirectiveException");

	internal static string Arg_MethodAccessException => GetResourceString("Arg_MethodAccessException");

	internal static string Arg_MissingFieldException => GetResourceString("Arg_MissingFieldException");

	internal static string Arg_MissingManifestResourceException => GetResourceString("Arg_MissingManifestResourceException");

	internal static string Arg_MissingMemberException => GetResourceString("Arg_MissingMemberException");

	internal static string Arg_MissingMethodException => GetResourceString("Arg_MissingMethodException");

	internal static string Arg_MulticastNotSupportedException => GetResourceString("Arg_MulticastNotSupportedException");

	internal static string Arg_MustBeBoolean => GetResourceString("Arg_MustBeBoolean");

	internal static string Arg_MustBeByte => GetResourceString("Arg_MustBeByte");

	internal static string Arg_MustBeChar => GetResourceString("Arg_MustBeChar");

	internal static string Arg_MustBeDateOnly => GetResourceString("Arg_MustBeDateOnly");

	internal static string Arg_MustBeTimeOnly => GetResourceString("Arg_MustBeTimeOnly");

	internal static string Arg_MustBeDateTime => GetResourceString("Arg_MustBeDateTime");

	internal static string Arg_MustBeDateTimeOffset => GetResourceString("Arg_MustBeDateTimeOffset");

	internal static string Arg_MustBeDecimal => GetResourceString("Arg_MustBeDecimal");

	internal static string Arg_MustBeDelegate => GetResourceString("Arg_MustBeDelegate");

	internal static string Arg_MustBeDouble => GetResourceString("Arg_MustBeDouble");

	internal static string Arg_MustBeDriveLetterOrRootDir => GetResourceString("Arg_MustBeDriveLetterOrRootDir");

	internal static string Arg_MustBeEnum => GetResourceString("Arg_MustBeEnum");

	internal static string Arg_MustBeEnumBaseTypeOrEnum => GetResourceString("Arg_MustBeEnumBaseTypeOrEnum");

	internal static string Arg_MustBeGuid => GetResourceString("Arg_MustBeGuid");

	internal static string Arg_MustBeInt16 => GetResourceString("Arg_MustBeInt16");

	internal static string Arg_MustBeInt32 => GetResourceString("Arg_MustBeInt32");

	internal static string Arg_MustBeInt64 => GetResourceString("Arg_MustBeInt64");

	internal static string Arg_MustBeIntPtr => GetResourceString("Arg_MustBeIntPtr");

	internal static string Arg_MustBePointer => GetResourceString("Arg_MustBePointer");

	internal static string Arg_MustBePrimArray => GetResourceString("Arg_MustBePrimArray");

	internal static string Arg_MustBeRuntimeAssembly => GetResourceString("Arg_MustBeRuntimeAssembly");

	internal static string Arg_MustBeSByte => GetResourceString("Arg_MustBeSByte");

	internal static string Arg_MustBeSingle => GetResourceString("Arg_MustBeSingle");

	internal static string Arg_MustBeString => GetResourceString("Arg_MustBeString");

	internal static string Arg_MustBeTimeSpan => GetResourceString("Arg_MustBeTimeSpan");

	internal static string Arg_MustBeType => GetResourceString("Arg_MustBeType");

	internal static string Arg_MustBeTrue => GetResourceString("Arg_MustBeTrue");

	internal static string Arg_MustBeUInt16 => GetResourceString("Arg_MustBeUInt16");

	internal static string Arg_MustBeUInt32 => GetResourceString("Arg_MustBeUInt32");

	internal static string Arg_MustBeUInt64 => GetResourceString("Arg_MustBeUInt64");

	internal static string Arg_MustBeUIntPtr => GetResourceString("Arg_MustBeUIntPtr");

	internal static string Arg_MustBeVersion => GetResourceString("Arg_MustBeVersion");

	internal static string Arg_MustContainEnumInfo => GetResourceString("Arg_MustContainEnumInfo");

	internal static string Arg_NamedParamNull => GetResourceString("Arg_NamedParamNull");

	internal static string Arg_NamedParamTooBig => GetResourceString("Arg_NamedParamTooBig");

	internal static string Arg_NDirectBadObject => GetResourceString("Arg_NDirectBadObject");

	internal static string Arg_Need1DArray => GetResourceString("Arg_Need1DArray");

	internal static string Arg_Need2DArray => GetResourceString("Arg_Need2DArray");

	internal static string Arg_Need3DArray => GetResourceString("Arg_Need3DArray");

	internal static string Arg_NeedAtLeast1Rank => GetResourceString("Arg_NeedAtLeast1Rank");

	internal static string Arg_NegativeArgCount => GetResourceString("Arg_NegativeArgCount");

	internal static string Arg_NoAccessSpec => GetResourceString("Arg_NoAccessSpec");

	internal static string Arg_NoDefCTor => GetResourceString("Arg_NoDefCTor");

	internal static string Arg_NonZeroLowerBound => GetResourceString("Arg_NonZeroLowerBound");

	internal static string Arg_NoStaticVirtual => GetResourceString("Arg_NoStaticVirtual");

	internal static string Arg_NotFiniteNumberException => GetResourceString("Arg_NotFiniteNumberException");

	internal static string Arg_NotGenericMethodDefinition => GetResourceString("Arg_NotGenericMethodDefinition");

	internal static string Arg_NotGenericParameter => GetResourceString("Arg_NotGenericParameter");

	internal static string Arg_NotGenericTypeDefinition => GetResourceString("Arg_NotGenericTypeDefinition");

	internal static string Arg_NotImplementedException => GetResourceString("Arg_NotImplementedException");

	internal static string Arg_NotSupportedException => GetResourceString("Arg_NotSupportedException");

	internal static string Arg_NullReferenceException => GetResourceString("Arg_NullReferenceException");

	internal static string Arg_ObjObjEx => GetResourceString("Arg_ObjObjEx");

	internal static string Arg_OleAutDateInvalid => GetResourceString("Arg_OleAutDateInvalid");

	internal static string Arg_OleAutDateScale => GetResourceString("Arg_OleAutDateScale");

	internal static string Arg_OverflowException => GetResourceString("Arg_OverflowException");

	internal static string Arg_ParamName_Name => GetResourceString("Arg_ParamName_Name");

	internal static string Arg_ParmArraySize => GetResourceString("Arg_ParmArraySize");

	internal static string Arg_ParmCnt => GetResourceString("Arg_ParmCnt");

	internal static string Arg_PathEmpty => GetResourceString("Arg_PathEmpty");

	internal static string Arg_PlatformNotSupported => GetResourceString("Arg_PlatformNotSupported");

	internal static string Arg_PropSetGet => GetResourceString("Arg_PropSetGet");

	internal static string Arg_PropSetInvoke => GetResourceString("Arg_PropSetInvoke");

	internal static string Arg_RankException => GetResourceString("Arg_RankException");

	internal static string Arg_RankIndices => GetResourceString("Arg_RankIndices");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Arg_RanksAndBounds => GetResourceString("Arg_RanksAndBounds");

	internal static string Arg_RegGetOverflowBug => GetResourceString("Arg_RegGetOverflowBug");

	internal static string Arg_RegKeyNotFound => GetResourceString("Arg_RegKeyNotFound");

	internal static string Arg_RegSubKeyValueAbsent => GetResourceString("Arg_RegSubKeyValueAbsent");

	internal static string Arg_RegValStrLenBug => GetResourceString("Arg_RegValStrLenBug");

	internal static string Arg_ResMgrNotResSet => GetResourceString("Arg_ResMgrNotResSet");

	internal static string Arg_ResourceFileUnsupportedVersion => GetResourceString("Arg_ResourceFileUnsupportedVersion");

	internal static string Arg_ResourceNameNotExist => GetResourceString("Arg_ResourceNameNotExist");

	internal static string Arg_SafeArrayRankMismatchException => GetResourceString("Arg_SafeArrayRankMismatchException");

	internal static string Arg_SafeArrayTypeMismatchException => GetResourceString("Arg_SafeArrayTypeMismatchException");

	internal static string Arg_SecurityException => GetResourceString("Arg_SecurityException");

	internal static string SerializationException => GetResourceString("SerializationException");

	internal static string Arg_SetMethNotFnd => GetResourceString("Arg_SetMethNotFnd");

	internal static string Arg_StackOverflowException => GetResourceString("Arg_StackOverflowException");

	internal static string Arg_SurrogatesNotAllowedAsSingleChar => GetResourceString("Arg_SurrogatesNotAllowedAsSingleChar");

	internal static string Arg_SynchronizationLockException => GetResourceString("Arg_SynchronizationLockException");

	internal static string Arg_SystemException => GetResourceString("Arg_SystemException");

	internal static string Arg_TargetInvocationException => GetResourceString("Arg_TargetInvocationException");

	internal static string Arg_TargetParameterCountException => GetResourceString("Arg_TargetParameterCountException");

	internal static string Arg_ThreadStartException => GetResourceString("Arg_ThreadStartException");

	internal static string Arg_ThreadStateException => GetResourceString("Arg_ThreadStateException");

	internal static string Arg_TimeoutException => GetResourceString("Arg_TimeoutException");

	internal static string Arg_TypeAccessException => GetResourceString("Arg_TypeAccessException");

	internal static string Arg_TypedReference_Null => GetResourceString("Arg_TypedReference_Null");

	internal static string Arg_TypeLoadException => GetResourceString("Arg_TypeLoadException");

	internal static string Arg_TypeLoadNullStr => GetResourceString("Arg_TypeLoadNullStr");

	internal static string Arg_TypeRefPrimitve => GetResourceString("Arg_TypeRefPrimitve");

	internal static string Arg_TypeUnloadedException => GetResourceString("Arg_TypeUnloadedException");

	internal static string Arg_UnauthorizedAccessException => GetResourceString("Arg_UnauthorizedAccessException");

	internal static string Arg_UnboundGenField => GetResourceString("Arg_UnboundGenField");

	internal static string Arg_UnboundGenParam => GetResourceString("Arg_UnboundGenParam");

	internal static string Arg_UnknownTypeCode => GetResourceString("Arg_UnknownTypeCode");

	internal static string Arg_VarMissNull => GetResourceString("Arg_VarMissNull");

	internal static string Arg_VersionString => GetResourceString("Arg_VersionString");

	internal static string Arg_WrongType => GetResourceString("Arg_WrongType");

	internal static string Argument_AbsolutePathRequired => GetResourceString("Argument_AbsolutePathRequired");

	internal static string Argument_AddingDuplicate => GetResourceString("Argument_AddingDuplicate");

	internal static string Argument_AddingDuplicate__ => GetResourceString("Argument_AddingDuplicate__");

	internal static string Argument_AddingDuplicateWithKey => GetResourceString("Argument_AddingDuplicateWithKey");

	internal static string Argument_AdjustmentRulesNoNulls => GetResourceString("Argument_AdjustmentRulesNoNulls");

	internal static string Argument_AdjustmentRulesOutOfOrder => GetResourceString("Argument_AdjustmentRulesOutOfOrder");

	internal static string Argument_AlignmentMustBePow2 => GetResourceString("Argument_AlignmentMustBePow2");

	internal static string Argument_AlreadyBoundOrSyncHandle => GetResourceString("Argument_AlreadyBoundOrSyncHandle");

	internal static string Argument_ArrayGetInterfaceMap => GetResourceString("Argument_ArrayGetInterfaceMap");

	internal static string Argument_ArraysInvalid => GetResourceString("Argument_ArraysInvalid");

	internal static string Argument_AttributeNamesMustBeUnique => GetResourceString("Argument_AttributeNamesMustBeUnique");

	internal static string Argument_BadConstructor => GetResourceString("Argument_BadConstructor");

	internal static string Argument_BadConstructorCallConv => GetResourceString("Argument_BadConstructorCallConv");

	internal static string Argument_BadExceptionCodeGen => GetResourceString("Argument_BadExceptionCodeGen");

	internal static string Argument_BadFieldForConstructorBuilder => GetResourceString("Argument_BadFieldForConstructorBuilder");

	internal static string Argument_BadFieldSig => GetResourceString("Argument_BadFieldSig");

	internal static string Argument_BadFieldType => GetResourceString("Argument_BadFieldType");

	internal static string Argument_BadFormatSpecifier => GetResourceString("Argument_BadFormatSpecifier");

	internal static string Argument_BadImageFormatExceptionResolve => GetResourceString("Argument_BadImageFormatExceptionResolve");

	internal static string Argument_BadLabel => GetResourceString("Argument_BadLabel");

	internal static string Argument_BadLabelContent => GetResourceString("Argument_BadLabelContent");

	internal static string Argument_BadNestedTypeFlags => GetResourceString("Argument_BadNestedTypeFlags");

	internal static string Argument_BadParameterCountsForConstructor => GetResourceString("Argument_BadParameterCountsForConstructor");

	internal static string Argument_BadParameterTypeForCAB => GetResourceString("Argument_BadParameterTypeForCAB");

	internal static string Argument_BadPropertyForConstructorBuilder => GetResourceString("Argument_BadPropertyForConstructorBuilder");

	internal static string Argument_BadSigFormat => GetResourceString("Argument_BadSigFormat");

	internal static string Argument_BadSizeForData => GetResourceString("Argument_BadSizeForData");

	internal static string Argument_BadTypeAttrInvalidLayout => GetResourceString("Argument_BadTypeAttrInvalidLayout");

	internal static string Argument_BadTypeAttrNestedVisibilityOnNonNestedType => GetResourceString("Argument_BadTypeAttrNestedVisibilityOnNonNestedType");

	internal static string Argument_BadTypeAttrNonNestedVisibilityNestedType => GetResourceString("Argument_BadTypeAttrNonNestedVisibilityNestedType");

	internal static string Argument_BadTypeAttrReservedBitsSet => GetResourceString("Argument_BadTypeAttrReservedBitsSet");

	internal static string Argument_BadTypeInCustomAttribute => GetResourceString("Argument_BadTypeInCustomAttribute");

	internal static string Argument_CannotGetTypeTokenForByRef => GetResourceString("Argument_CannotGetTypeTokenForByRef");

	internal static string Argument_CannotSetParentToInterface => GetResourceString("Argument_CannotSetParentToInterface");

	internal static string Argument_CodepageNotSupported => GetResourceString("Argument_CodepageNotSupported");

	internal static string Argument_CompareOptionOrdinal => GetResourceString("Argument_CompareOptionOrdinal");

	internal static string Argument_ConflictingDateTimeRoundtripStyles => GetResourceString("Argument_ConflictingDateTimeRoundtripStyles");

	internal static string Argument_ConflictingDateTimeStyles => GetResourceString("Argument_ConflictingDateTimeStyles");

	internal static string Argument_ConstantDoesntMatch => GetResourceString("Argument_ConstantDoesntMatch");

	internal static string Argument_ConstantNotSupported => GetResourceString("Argument_ConstantNotSupported");

	internal static string Argument_ConstantNull => GetResourceString("Argument_ConstantNull");

	internal static string Argument_ConstructorNeedGenericDeclaringType => GetResourceString("Argument_ConstructorNeedGenericDeclaringType");

	internal static string Argument_ConversionOverflow => GetResourceString("Argument_ConversionOverflow");

	internal static string Argument_ConvertMismatch => GetResourceString("Argument_ConvertMismatch");

	internal static string Argument_CultureIetfNotSupported => GetResourceString("Argument_CultureIetfNotSupported");

	internal static string Argument_CultureInvalidIdentifier => GetResourceString("Argument_CultureInvalidIdentifier");

	internal static string Argument_CultureIsNeutral => GetResourceString("Argument_CultureIsNeutral");

	internal static string Argument_CultureNotSupported => GetResourceString("Argument_CultureNotSupported");

	internal static string Argument_CultureNotSupportedInInvariantMode => GetResourceString("Argument_CultureNotSupportedInInvariantMode");

	internal static string Argument_CustomAssemblyLoadContextRequestedNameMismatch => GetResourceString("Argument_CustomAssemblyLoadContextRequestedNameMismatch");

	internal static string Argument_CustomCultureCannotBePassedByNumber => GetResourceString("Argument_CustomCultureCannotBePassedByNumber");

	internal static string Argument_DateTimeBadBinaryData => GetResourceString("Argument_DateTimeBadBinaryData");

	internal static string Argument_DateTimeHasTicks => GetResourceString("Argument_DateTimeHasTicks");

	internal static string Argument_DateTimeHasTimeOfDay => GetResourceString("Argument_DateTimeHasTimeOfDay");

	internal static string Argument_DateTimeIsInvalid => GetResourceString("Argument_DateTimeIsInvalid");

	internal static string Argument_DateTimeIsNotAmbiguous => GetResourceString("Argument_DateTimeIsNotAmbiguous");

	internal static string Argument_DateTimeKindMustBeUnspecified => GetResourceString("Argument_DateTimeKindMustBeUnspecified");

	internal static string Argument_DateTimeKindMustBeUnspecifiedOrUtc => GetResourceString("Argument_DateTimeKindMustBeUnspecifiedOrUtc");

	internal static string Argument_DateTimeOffsetInvalidDateTimeStyles => GetResourceString("Argument_DateTimeOffsetInvalidDateTimeStyles");

	internal static string Argument_DateTimeOffsetIsNotAmbiguous => GetResourceString("Argument_DateTimeOffsetIsNotAmbiguous");

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

	internal static string Argument_DuplicateTypeName => GetResourceString("Argument_DuplicateTypeName");

	internal static string Argument_EmitWriteLineType => GetResourceString("Argument_EmitWriteLineType");

	internal static string Argument_EmptyDecString => GetResourceString("Argument_EmptyDecString");

	internal static string Argument_EmptyFileName => GetResourceString("Argument_EmptyFileName");

	internal static string Argument_EmptyName => GetResourceString("Argument_EmptyName");

	internal static string Argument_EmptyPath => GetResourceString("Argument_EmptyPath");

	internal static string Argument_EmptyWaithandleArray => GetResourceString("Argument_EmptyWaithandleArray");

	internal static string Argument_EncoderFallbackNotEmpty => GetResourceString("Argument_EncoderFallbackNotEmpty");

	internal static string Argument_EncodingConversionOverflowBytes => GetResourceString("Argument_EncodingConversionOverflowBytes");

	internal static string Argument_EncodingConversionOverflowChars => GetResourceString("Argument_EncodingConversionOverflowChars");

	internal static string Argument_EncodingNotSupported => GetResourceString("Argument_EncodingNotSupported");

	internal static string Argument_EnumTypeDoesNotMatch => GetResourceString("Argument_EnumTypeDoesNotMatch");

	internal static string Argument_FallbackBufferNotEmpty => GetResourceString("Argument_FallbackBufferNotEmpty");

	internal static string Argument_FieldDeclaringTypeGeneric => GetResourceString("Argument_FieldDeclaringTypeGeneric");

	internal static string Argument_FieldNeedGenericDeclaringType => GetResourceString("Argument_FieldNeedGenericDeclaringType");

	internal static string Argument_GenConstraintViolation => GetResourceString("Argument_GenConstraintViolation");

	internal static string Argument_GenericArgsCount => GetResourceString("Argument_GenericArgsCount");

	internal static string Argument_GenericsInvalid => GetResourceString("Argument_GenericsInvalid");

	internal static string Argument_GlobalFunctionHasToBeStatic => GetResourceString("Argument_GlobalFunctionHasToBeStatic");

	internal static string Argument_HasToBeArrayClass => GetResourceString("Argument_HasToBeArrayClass");

	internal static string Argument_IdnBadBidi => GetResourceString("Argument_IdnBadBidi");

	internal static string Argument_IdnBadLabelSize => GetResourceString("Argument_IdnBadLabelSize");

	internal static string Argument_IdnBadNameSize => GetResourceString("Argument_IdnBadNameSize");

	internal static string Argument_IdnBadPunycode => GetResourceString("Argument_IdnBadPunycode");

	internal static string Argument_IdnBadStd3 => GetResourceString("Argument_IdnBadStd3");

	internal static string Argument_IdnIllegalName => GetResourceString("Argument_IdnIllegalName");

	internal static string Argument_IllegalEnvVarName => GetResourceString("Argument_IllegalEnvVarName");

	internal static string Argument_IllegalName => GetResourceString("Argument_IllegalName");

	internal static string Argument_ImplementIComparable => GetResourceString("Argument_ImplementIComparable");

	internal static string Argument_InvalidAppendMode => GetResourceString("Argument_InvalidAppendMode");

	internal static string Argument_InvalidPreallocateAccess => GetResourceString("Argument_InvalidPreallocateAccess");

	internal static string Argument_InvalidPreallocateMode => GetResourceString("Argument_InvalidPreallocateMode");

	internal static string Argument_InvalidArgumentForComparison => GetResourceString("Argument_InvalidArgumentForComparison");

	internal static string Argument_InvalidArrayLength => GetResourceString("Argument_InvalidArrayLength");

	internal static string Argument_InvalidArrayType => GetResourceString("Argument_InvalidArrayType");

	internal static string Argument_InvalidCalendar => GetResourceString("Argument_InvalidCalendar");

	internal static string Argument_InvalidCharSequence => GetResourceString("Argument_InvalidCharSequence");

	internal static string Argument_InvalidCharSequenceNoIndex => GetResourceString("Argument_InvalidCharSequenceNoIndex");

	internal static string Argument_InvalidCodePageBytesIndex => GetResourceString("Argument_InvalidCodePageBytesIndex");

	internal static string Argument_InvalidCodePageConversionIndex => GetResourceString("Argument_InvalidCodePageConversionIndex");

	internal static string Argument_InvalidConstructorDeclaringType => GetResourceString("Argument_InvalidConstructorDeclaringType");

	internal static string Argument_InvalidConstructorInfo => GetResourceString("Argument_InvalidConstructorInfo");

	internal static string Argument_InvalidCultureName => GetResourceString("Argument_InvalidCultureName");

	internal static string Argument_InvalidPredefinedCultureName => GetResourceString("Argument_InvalidPredefinedCultureName");

	internal static string Argument_InvalidDateTimeKind => GetResourceString("Argument_InvalidDateTimeKind");

	internal static string Argument_InvalidDateTimeStyles => GetResourceString("Argument_InvalidDateTimeStyles");

	internal static string Argument_InvalidDateStyles => GetResourceString("Argument_InvalidDateStyles");

	internal static string Argument_InvalidDigitSubstitution => GetResourceString("Argument_InvalidDigitSubstitution");

	internal static string Argument_InvalidElementName => GetResourceString("Argument_InvalidElementName");

	internal static string Argument_InvalidElementTag => GetResourceString("Argument_InvalidElementTag");

	internal static string Argument_InvalidElementText => GetResourceString("Argument_InvalidElementText");

	internal static string Argument_InvalidElementValue => GetResourceString("Argument_InvalidElementValue");

	internal static string Argument_InvalidEnum => GetResourceString("Argument_InvalidEnum");

	internal static string Argument_InvalidEnumValue => GetResourceString("Argument_InvalidEnumValue");

	internal static string Argument_InvalidFieldDeclaringType => GetResourceString("Argument_InvalidFieldDeclaringType");

	internal static string Argument_InvalidFileModeAndAccessCombo => GetResourceString("Argument_InvalidFileModeAndAccessCombo");

	internal static string Argument_InvalidFlag => GetResourceString("Argument_InvalidFlag");

	internal static string Argument_InvalidGenericInstArray => GetResourceString("Argument_InvalidGenericInstArray");

	internal static string Argument_InvalidGroupSize => GetResourceString("Argument_InvalidGroupSize");

	internal static string Argument_InvalidHandle => GetResourceString("Argument_InvalidHandle");

	internal static string Argument_InvalidHighSurrogate => GetResourceString("Argument_InvalidHighSurrogate");

	internal static string Argument_InvalidId => GetResourceString("Argument_InvalidId");

	internal static string Argument_InvalidKindOfTypeForCA => GetResourceString("Argument_InvalidKindOfTypeForCA");

	internal static string Argument_InvalidLabel => GetResourceString("Argument_InvalidLabel");

	internal static string Argument_InvalidLowSurrogate => GetResourceString("Argument_InvalidLowSurrogate");

	internal static string Argument_InvalidMemberForNamedArgument => GetResourceString("Argument_InvalidMemberForNamedArgument");

	internal static string Argument_InvalidMethodDeclaringType => GetResourceString("Argument_InvalidMethodDeclaringType");

	internal static string Argument_InvalidName => GetResourceString("Argument_InvalidName");

	internal static string Argument_InvalidNativeDigitCount => GetResourceString("Argument_InvalidNativeDigitCount");

	internal static string Argument_InvalidNativeDigitValue => GetResourceString("Argument_InvalidNativeDigitValue");

	internal static string Argument_InvalidNeutralRegionName => GetResourceString("Argument_InvalidNeutralRegionName");

	internal static string Argument_InvalidNormalizationForm => GetResourceString("Argument_InvalidNormalizationForm");

	internal static string Argument_InvalidNumberStyles => GetResourceString("Argument_InvalidNumberStyles");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_InvalidOpCodeOnDynamicMethod => GetResourceString("Argument_InvalidOpCodeOnDynamicMethod");

	internal static string Argument_InvalidParameterInfo => GetResourceString("Argument_InvalidParameterInfo");

	internal static string Argument_InvalidParamInfo => GetResourceString("Argument_InvalidParamInfo");

	internal static string Argument_InvalidPathChars => GetResourceString("Argument_InvalidPathChars");

	internal static string Argument_InvalidResourceCultureName => GetResourceString("Argument_InvalidResourceCultureName");

	internal static string Argument_InvalidSafeBufferOffLen => GetResourceString("Argument_InvalidSafeBufferOffLen");

	internal static string Argument_InvalidSeekOrigin => GetResourceString("Argument_InvalidSeekOrigin");

	internal static string Argument_InvalidSerializedString => GetResourceString("Argument_InvalidSerializedString");

	internal static string Argument_InvalidStartupHookSignature => GetResourceString("Argument_InvalidStartupHookSignature");

	internal static string Argument_InvalidTimeSpanStyles => GetResourceString("Argument_InvalidTimeSpanStyles");

	internal static string Argument_InvalidToken => GetResourceString("Argument_InvalidToken");

	internal static string Argument_InvalidTypeForCA => GetResourceString("Argument_InvalidTypeForCA");

	internal static string Argument_InvalidTypeForDynamicMethod => GetResourceString("Argument_InvalidTypeForDynamicMethod");

	internal static string Argument_InvalidTypeName => GetResourceString("Argument_InvalidTypeName");

	internal static string Argument_InvalidTypeWithPointersNotSupported => GetResourceString("Argument_InvalidTypeWithPointersNotSupported");

	internal static string Argument_InvalidUnity => GetResourceString("Argument_InvalidUnity");

	internal static string Argument_LargeInteger => GetResourceString("Argument_LargeInteger");

	internal static string Argument_LongEnvVarValue => GetResourceString("Argument_LongEnvVarValue");

	internal static string Argument_MethodDeclaringTypeGeneric => GetResourceString("Argument_MethodDeclaringTypeGeneric");

	internal static string Argument_MethodDeclaringTypeGenericLcg => GetResourceString("Argument_MethodDeclaringTypeGenericLcg");

	internal static string Argument_MethodNeedGenericDeclaringType => GetResourceString("Argument_MethodNeedGenericDeclaringType");

	internal static string Argument_MinMaxValue => GetResourceString("Argument_MinMaxValue");

	internal static string Argument_MismatchedArrays => GetResourceString("Argument_MismatchedArrays");

	internal static string Argument_MissingDefaultConstructor => GetResourceString("Argument_MissingDefaultConstructor");

	internal static string Argument_MustBeFalse => GetResourceString("Argument_MustBeFalse");

	internal static string Argument_MustBeRuntimeAssembly => GetResourceString("Argument_MustBeRuntimeAssembly");

	internal static string Argument_MustBeRuntimeFieldInfo => GetResourceString("Argument_MustBeRuntimeFieldInfo");

	internal static string Argument_MustBeRuntimeMethodInfo => GetResourceString("Argument_MustBeRuntimeMethodInfo");

	internal static string Argument_MustBeRuntimeReflectionObject => GetResourceString("Argument_MustBeRuntimeReflectionObject");

	internal static string Argument_MustBeRuntimeType => GetResourceString("Argument_MustBeRuntimeType");

	internal static string Argument_MustBeTypeBuilder => GetResourceString("Argument_MustBeTypeBuilder");

	internal static string Argument_MustHaveAttributeBaseClass => GetResourceString("Argument_MustHaveAttributeBaseClass");

	internal static string Argument_NativeOverlappedAlreadyFree => GetResourceString("Argument_NativeOverlappedAlreadyFree");

	internal static string Argument_NativeOverlappedWrongBoundHandle => GetResourceString("Argument_NativeOverlappedWrongBoundHandle");

	internal static string Argument_NeedGenericMethodDefinition => GetResourceString("Argument_NeedGenericMethodDefinition");

	internal static string Argument_NeedNonGenericType => GetResourceString("Argument_NeedNonGenericType");

	internal static string Argument_NeedStructWithNoRefs => GetResourceString("Argument_NeedStructWithNoRefs");

	internal static string Argument_NeverValidGenericArgument => GetResourceString("Argument_NeverValidGenericArgument");

	internal static string Argument_NoEra => GetResourceString("Argument_NoEra");

	internal static string Argument_NoRegionInvariantCulture => GetResourceString("Argument_NoRegionInvariantCulture");

	internal static string Argument_NotAWritableProperty => GetResourceString("Argument_NotAWritableProperty");

	internal static string Argument_NotEnoughBytesToRead => GetResourceString("Argument_NotEnoughBytesToRead");

	internal static string Argument_NotEnoughBytesToWrite => GetResourceString("Argument_NotEnoughBytesToWrite");

	internal static string Argument_NotEnoughGenArguments => GetResourceString("Argument_NotEnoughGenArguments");

	internal static string Argument_NotExceptionType => GetResourceString("Argument_NotExceptionType");

	internal static string Argument_NotInExceptionBlock => GetResourceString("Argument_NotInExceptionBlock");

	internal static string Argument_NotMethodCallOpcode => GetResourceString("Argument_NotMethodCallOpcode");

	internal static string Argument_NotSerializable => GetResourceString("Argument_NotSerializable");

	internal static string Argument_ObjNotComObject => GetResourceString("Argument_ObjNotComObject");

	internal static string Argument_OffsetAndCapacityOutOfBounds => GetResourceString("Argument_OffsetAndCapacityOutOfBounds");

	internal static string Argument_OffsetLocalMismatch => GetResourceString("Argument_OffsetLocalMismatch");

	internal static string Argument_OffsetOfFieldNotFound => GetResourceString("Argument_OffsetOfFieldNotFound");

	internal static string Argument_OffsetOutOfRange => GetResourceString("Argument_OffsetOutOfRange");

	internal static string Argument_OffsetPrecision => GetResourceString("Argument_OffsetPrecision");

	internal static string Argument_OffsetUtcMismatch => GetResourceString("Argument_OffsetUtcMismatch");

	internal static string Argument_OneOfCulturesNotSupported => GetResourceString("Argument_OneOfCulturesNotSupported");

	internal static string Argument_OnlyMscorlib => GetResourceString("Argument_OnlyMscorlib");

	internal static string Argument_OutOfOrderDateTimes => GetResourceString("Argument_OutOfOrderDateTimes");

	internal static string Argument_PathEmpty => GetResourceString("Argument_PathEmpty");

	internal static string Argument_PreAllocatedAlreadyAllocated => GetResourceString("Argument_PreAllocatedAlreadyAllocated");

	internal static string Argument_RecursiveFallback => GetResourceString("Argument_RecursiveFallback");

	internal static string Argument_RecursiveFallbackBytes => GetResourceString("Argument_RecursiveFallbackBytes");

	internal static string Argument_RedefinedLabel => GetResourceString("Argument_RedefinedLabel");

	internal static string Argument_ResolveField => GetResourceString("Argument_ResolveField");

	internal static string Argument_ResolveFieldHandle => GetResourceString("Argument_ResolveFieldHandle");

	internal static string Argument_ResolveMember => GetResourceString("Argument_ResolveMember");

	internal static string Argument_ResolveMethod => GetResourceString("Argument_ResolveMethod");

	internal static string Argument_ResolveMethodHandle => GetResourceString("Argument_ResolveMethodHandle");

	internal static string Argument_ResolveModuleType => GetResourceString("Argument_ResolveModuleType");

	internal static string Argument_ResolveString => GetResourceString("Argument_ResolveString");

	internal static string Argument_ResolveType => GetResourceString("Argument_ResolveType");

	internal static string Argument_ResultCalendarRange => GetResourceString("Argument_ResultCalendarRange");

	internal static string Argument_SemaphoreInitialMaximum => GetResourceString("Argument_SemaphoreInitialMaximum");

	internal static string Argument_ShouldNotSpecifyExceptionType => GetResourceString("Argument_ShouldNotSpecifyExceptionType");

	internal static string Argument_ShouldOnlySetVisibilityFlags => GetResourceString("Argument_ShouldOnlySetVisibilityFlags");

	internal static string Argument_SigIsFinalized => GetResourceString("Argument_SigIsFinalized");

	internal static string Argument_StreamNotReadable => GetResourceString("Argument_StreamNotReadable");

	internal static string Argument_StreamNotWritable => GetResourceString("Argument_StreamNotWritable");

	internal static string Argument_StringFirstCharIsZero => GetResourceString("Argument_StringFirstCharIsZero");

	internal static string Argument_StringZeroLength => GetResourceString("Argument_StringZeroLength");

	internal static string Argument_TimeSpanHasSeconds => GetResourceString("Argument_TimeSpanHasSeconds");

	internal static string Argument_ToExclusiveLessThanFromExclusive => GetResourceString("Argument_ToExclusiveLessThanFromExclusive");

	internal static string Argument_TooManyFinallyClause => GetResourceString("Argument_TooManyFinallyClause");

	internal static string Argument_TransitionTimesAreIdentical => GetResourceString("Argument_TransitionTimesAreIdentical");

	internal static string Argument_TypedReferenceInvalidField => GetResourceString("Argument_TypedReferenceInvalidField");

	internal static string Argument_TypeMustNotBeComImport => GetResourceString("Argument_TypeMustNotBeComImport");

	internal static string Argument_TypeNameTooLong => GetResourceString("Argument_TypeNameTooLong");

	internal static string Argument_TypeNotComObject => GetResourceString("Argument_TypeNotComObject");

	internal static string Argument_TypeNotValid => GetResourceString("Argument_TypeNotValid");

	internal static string Argument_UnclosedExceptionBlock => GetResourceString("Argument_UnclosedExceptionBlock");

	internal static string Argument_UnknownUnmanagedCallConv => GetResourceString("Argument_UnknownUnmanagedCallConv");

	internal static string Argument_UnmanagedMemAccessorWrapAround => GetResourceString("Argument_UnmanagedMemAccessorWrapAround");

	internal static string Argument_UnmatchedMethodForLocal => GetResourceString("Argument_UnmatchedMethodForLocal");

	internal static string Argument_UnmatchingSymScope => GetResourceString("Argument_UnmatchingSymScope");

	internal static string Argument_UTCOutOfRange => GetResourceString("Argument_UTCOutOfRange");

	internal static string ArgumentException_BadMethodImplBody => GetResourceString("ArgumentException_BadMethodImplBody");

	internal static string ArgumentException_BufferNotFromPool => GetResourceString("ArgumentException_BufferNotFromPool");

	internal static string ArgumentException_OtherNotArrayOfCorrectLength => GetResourceString("ArgumentException_OtherNotArrayOfCorrectLength");

	internal static string ArgumentException_NotIsomorphic => GetResourceString("ArgumentException_NotIsomorphic");

	internal static string ArgumentException_TupleIncorrectType => GetResourceString("ArgumentException_TupleIncorrectType");

	internal static string ArgumentException_TupleLastArgumentNotATuple => GetResourceString("ArgumentException_TupleLastArgumentNotATuple");

	internal static string ArgumentException_ValueTupleIncorrectType => GetResourceString("ArgumentException_ValueTupleIncorrectType");

	internal static string ArgumentException_ValueTupleLastArgumentNotAValueTuple => GetResourceString("ArgumentException_ValueTupleLastArgumentNotAValueTuple");

	internal static string ArgumentNull_Array => GetResourceString("ArgumentNull_Array");

	internal static string ArgumentNull_ArrayElement => GetResourceString("ArgumentNull_ArrayElement");

	internal static string ArgumentNull_ArrayValue => GetResourceString("ArgumentNull_ArrayValue");

	internal static string ArgumentNull_Assembly => GetResourceString("ArgumentNull_Assembly");

	internal static string ArgumentNull_AssemblyNameName => GetResourceString("ArgumentNull_AssemblyNameName");

	internal static string ArgumentNull_Buffer => GetResourceString("ArgumentNull_Buffer");

	internal static string ArgumentNull_Child => GetResourceString("ArgumentNull_Child");

	internal static string ArgumentNull_Collection => GetResourceString("ArgumentNull_Collection");

	internal static string ArgumentNull_Dictionary => GetResourceString("ArgumentNull_Dictionary");

	internal static string ArgumentNull_FileName => GetResourceString("ArgumentNull_FileName");

	internal static string ArgumentNull_Generic => GetResourceString("ArgumentNull_Generic");

	internal static string ArgumentNull_Key => GetResourceString("ArgumentNull_Key");

	internal static string ArgumentNull_Path => GetResourceString("ArgumentNull_Path");

	internal static string ArgumentNull_SafeHandle => GetResourceString("ArgumentNull_SafeHandle");

	internal static string ArgumentNull_Stream => GetResourceString("ArgumentNull_Stream");

	internal static string ArgumentNull_String => GetResourceString("ArgumentNull_String");

	internal static string ArgumentNull_Type => GetResourceString("ArgumentNull_Type");

	internal static string ArgumentNull_Waithandles => GetResourceString("ArgumentNull_Waithandles");

	internal static string ArgumentOutOfRange_ActualValue => GetResourceString("ArgumentOutOfRange_ActualValue");

	internal static string ArgumentOutOfRange_AddValue => GetResourceString("ArgumentOutOfRange_AddValue");

	internal static string ArgumentOutOfRange_ArrayLB => GetResourceString("ArgumentOutOfRange_ArrayLB");

	internal static string ArgumentOutOfRange_BadHourMinuteSecond => GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond");

	internal static string ArgumentOutOfRange_BadYearMonthDay => GetResourceString("ArgumentOutOfRange_BadYearMonthDay");

	internal static string ArgumentOutOfRange_BiggerThanCollection => GetResourceString("ArgumentOutOfRange_BiggerThanCollection");

	internal static string ArgumentOutOfRange_BinaryReaderFillBuffer => GetResourceString("ArgumentOutOfRange_BinaryReaderFillBuffer");

	internal static string ArgumentOutOfRange_Bounds_Lower_Upper => GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper");

	internal static string ArgumentOutOfRange_CalendarRange => GetResourceString("ArgumentOutOfRange_CalendarRange");

	internal static string ArgumentOutOfRange_Capacity => GetResourceString("ArgumentOutOfRange_Capacity");

	internal static string ArgumentOutOfRange_Count => GetResourceString("ArgumentOutOfRange_Count");

	internal static string ArgumentOutOfRange_DateArithmetic => GetResourceString("ArgumentOutOfRange_DateArithmetic");

	internal static string ArgumentOutOfRange_DateTimeBadMonths => GetResourceString("ArgumentOutOfRange_DateTimeBadMonths");

	internal static string ArgumentOutOfRange_DateTimeBadTicks => GetResourceString("ArgumentOutOfRange_DateTimeBadTicks");

	internal static string ArgumentOutOfRange_TimeOnlyBadTicks => GetResourceString("ArgumentOutOfRange_TimeOnlyBadTicks");

	internal static string ArgumentOutOfRange_DateTimeBadYears => GetResourceString("ArgumentOutOfRange_DateTimeBadYears");

	internal static string ArgumentOutOfRange_Day => GetResourceString("ArgumentOutOfRange_Day");

	internal static string ArgumentOutOfRange_DayOfWeek => GetResourceString("ArgumentOutOfRange_DayOfWeek");

	internal static string ArgumentOutOfRange_DayParam => GetResourceString("ArgumentOutOfRange_DayParam");

	internal static string ArgumentOutOfRange_DecimalRound => GetResourceString("ArgumentOutOfRange_DecimalRound");

	internal static string ArgumentOutOfRange_DecimalScale => GetResourceString("ArgumentOutOfRange_DecimalScale");

	internal static string ArgumentOutOfRange_EndIndexStartIndex => GetResourceString("ArgumentOutOfRange_EndIndexStartIndex");

	internal static string ArgumentOutOfRange_Enum => GetResourceString("ArgumentOutOfRange_Enum");

	internal static string ArgumentOutOfRange_Era => GetResourceString("ArgumentOutOfRange_Era");

	internal static string ArgumentOutOfRange_FileLengthTooBig => GetResourceString("ArgumentOutOfRange_FileLengthTooBig");

	internal static string ArgumentOutOfRange_FileTimeInvalid => GetResourceString("ArgumentOutOfRange_FileTimeInvalid");

	internal static string ArgumentOutOfRange_GenericPositive => GetResourceString("ArgumentOutOfRange_GenericPositive");

	internal static string ArgumentOutOfRange_GetByteCountOverflow => GetResourceString("ArgumentOutOfRange_GetByteCountOverflow");

	internal static string ArgumentOutOfRange_GetCharCountOverflow => GetResourceString("ArgumentOutOfRange_GetCharCountOverflow");

	internal static string ArgumentOutOfRange_HashtableLoadFactor => GetResourceString("ArgumentOutOfRange_HashtableLoadFactor");

	internal static string ArgumentOutOfRange_HugeArrayNotSupported => GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string ArgumentOutOfRange_IndexCount => GetResourceString("ArgumentOutOfRange_IndexCount");

	internal static string ArgumentOutOfRange_IndexCountBuffer => GetResourceString("ArgumentOutOfRange_IndexCountBuffer");

	internal static string ArgumentOutOfRange_IndexLength => GetResourceString("ArgumentOutOfRange_IndexLength");

	internal static string ArgumentOutOfRange_IndexString => GetResourceString("ArgumentOutOfRange_IndexString");

	internal static string ArgumentOutOfRange_InputTooLarge => GetResourceString("ArgumentOutOfRange_InputTooLarge");

	internal static string ArgumentOutOfRange_InvalidEraValue => GetResourceString("ArgumentOutOfRange_InvalidEraValue");

	internal static string ArgumentOutOfRange_InvalidHighSurrogate => GetResourceString("ArgumentOutOfRange_InvalidHighSurrogate");

	internal static string ArgumentOutOfRange_InvalidLowSurrogate => GetResourceString("ArgumentOutOfRange_InvalidLowSurrogate");

	internal static string ArgumentOutOfRange_InvalidUTF32 => GetResourceString("ArgumentOutOfRange_InvalidUTF32");

	internal static string ArgumentOutOfRange_Length => GetResourceString("ArgumentOutOfRange_Length");

	internal static string ArgumentOutOfRange_LengthGreaterThanCapacity => GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity");

	internal static string ArgumentOutOfRange_LengthTooLarge => GetResourceString("ArgumentOutOfRange_LengthTooLarge");

	internal static string ArgumentOutOfRange_LessEqualToIntegerMaxVal => GetResourceString("ArgumentOutOfRange_LessEqualToIntegerMaxVal");

	internal static string ArgumentOutOfRange_ListInsert => GetResourceString("ArgumentOutOfRange_ListInsert");

	internal static string ArgumentOutOfRange_Month => GetResourceString("ArgumentOutOfRange_Month");

	internal static string ArgumentOutOfRange_DayNumber => GetResourceString("ArgumentOutOfRange_DayNumber");

	internal static string ArgumentOutOfRange_MonthParam => GetResourceString("ArgumentOutOfRange_MonthParam");

	internal static string ArgumentOutOfRange_MustBeNonNegInt32 => GetResourceString("ArgumentOutOfRange_MustBeNonNegInt32");

	internal static string ArgumentOutOfRange_MustBeNonNegNum => GetResourceString("ArgumentOutOfRange_MustBeNonNegNum");

	internal static string ArgumentOutOfRange_MustBePositive => GetResourceString("ArgumentOutOfRange_MustBePositive");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_NeedNonNegOrNegative1 => GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1");

	internal static string ArgumentOutOfRange_NeedPosNum => GetResourceString("ArgumentOutOfRange_NeedPosNum");

	internal static string ArgumentOutOfRange_NeedValidId => GetResourceString("ArgumentOutOfRange_NeedValidId");

	internal static string ArgumentOutOfRange_NegativeCapacity => GetResourceString("ArgumentOutOfRange_NegativeCapacity");

	internal static string ArgumentOutOfRange_NegativeCount => GetResourceString("ArgumentOutOfRange_NegativeCount");

	internal static string ArgumentOutOfRange_NegativeLength => GetResourceString("ArgumentOutOfRange_NegativeLength");

	internal static string ArgumentOutOfRange_OffsetLength => GetResourceString("ArgumentOutOfRange_OffsetLength");

	internal static string ArgumentOutOfRange_OffsetOut => GetResourceString("ArgumentOutOfRange_OffsetOut");

	internal static string ArgumentOutOfRange_ParamSequence => GetResourceString("ArgumentOutOfRange_ParamSequence");

	internal static string ArgumentOutOfRange_PartialWCHAR => GetResourceString("ArgumentOutOfRange_PartialWCHAR");

	internal static string ArgumentOutOfRange_PeriodTooLarge => GetResourceString("ArgumentOutOfRange_PeriodTooLarge");

	internal static string ArgumentOutOfRange_PositionLessThanCapacityRequired => GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired");

	internal static string ArgumentOutOfRange_Range => GetResourceString("ArgumentOutOfRange_Range");

	internal static string ArgumentOutOfRange_RoundingDigits => GetResourceString("ArgumentOutOfRange_RoundingDigits");

	internal static string ArgumentOutOfRange_RoundingDigits_MathF => GetResourceString("ArgumentOutOfRange_RoundingDigits_MathF");

	internal static string ArgumentOutOfRange_SmallCapacity => GetResourceString("ArgumentOutOfRange_SmallCapacity");

	internal static string ArgumentOutOfRange_SmallMaxCapacity => GetResourceString("ArgumentOutOfRange_SmallMaxCapacity");

	internal static string ArgumentOutOfRange_StartIndex => GetResourceString("ArgumentOutOfRange_StartIndex");

	internal static string ArgumentOutOfRange_StartIndexLargerThanLength => GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength");

	internal static string ArgumentOutOfRange_StreamLength => GetResourceString("ArgumentOutOfRange_StreamLength");

	internal static string ArgumentOutOfRange_TimeoutTooLarge => GetResourceString("ArgumentOutOfRange_TimeoutTooLarge");

	internal static string ArgumentOutOfRange_UIntPtrMax => GetResourceString("ArgumentOutOfRange_UIntPtrMax");

	internal static string ArgumentOutOfRange_UnmanagedMemStreamLength => GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength");

	internal static string ArgumentOutOfRange_UnmanagedMemStreamWrapAround => GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround");

	internal static string ArgumentOutOfRange_UtcOffset => GetResourceString("ArgumentOutOfRange_UtcOffset");

	internal static string ArgumentOutOfRange_UtcOffsetAndDaylightDelta => GetResourceString("ArgumentOutOfRange_UtcOffsetAndDaylightDelta");

	internal static string ArgumentOutOfRange_Version => GetResourceString("ArgumentOutOfRange_Version");

	internal static string ArgumentOutOfRange_Week => GetResourceString("ArgumentOutOfRange_Week");

	internal static string ArgumentOutOfRange_Year => GetResourceString("ArgumentOutOfRange_Year");

	internal static string Arithmetic_NaN => GetResourceString("Arithmetic_NaN");

	internal static string ArrayTypeMismatch_ConstrainedCopy => GetResourceString("ArrayTypeMismatch_ConstrainedCopy");

	internal static string AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible => GetResourceString("AssemblyLoadContext_Unload_CannotUnloadIfNotCollectible");

	internal static string AssemblyLoadContext_Verify_NotUnloading => GetResourceString("AssemblyLoadContext_Verify_NotUnloading");

	internal static string AssertionFailed => GetResourceString("AssertionFailed");

	internal static string AssertionFailed_Cnd => GetResourceString("AssertionFailed_Cnd");

	internal static string AssumptionFailed => GetResourceString("AssumptionFailed");

	internal static string AssumptionFailed_Cnd => GetResourceString("AssumptionFailed_Cnd");

	internal static string AsyncMethodBuilder_InstanceNotInitialized => GetResourceString("AsyncMethodBuilder_InstanceNotInitialized");

	internal static string BadImageFormat_BadILFormat => GetResourceString("BadImageFormat_BadILFormat");

	internal static string BadImageFormat_InvalidType => GetResourceString("BadImageFormat_InvalidType");

	internal static string BadImageFormat_NegativeStringLength => GetResourceString("BadImageFormat_NegativeStringLength");

	internal static string BadImageFormat_ParameterSignatureMismatch => GetResourceString("BadImageFormat_ParameterSignatureMismatch");

	internal static string BadImageFormat_ResType_SerBlobMismatch => GetResourceString("BadImageFormat_ResType_SerBlobMismatch");

	internal static string BadImageFormat_ResourceDataLengthInvalid => GetResourceString("BadImageFormat_ResourceDataLengthInvalid");

	internal static string BadImageFormat_ResourceNameCorrupted => GetResourceString("BadImageFormat_ResourceNameCorrupted");

	internal static string BadImageFormat_ResourceNameCorrupted_NameIndex => GetResourceString("BadImageFormat_ResourceNameCorrupted_NameIndex");

	internal static string BadImageFormat_ResourcesDataInvalidOffset => GetResourceString("BadImageFormat_ResourcesDataInvalidOffset");

	internal static string BadImageFormat_ResourcesHeaderCorrupted => GetResourceString("BadImageFormat_ResourcesHeaderCorrupted");

	internal static string BadImageFormat_ResourcesIndexTooLong => GetResourceString("BadImageFormat_ResourcesIndexTooLong");

	internal static string BadImageFormat_ResourcesNameInvalidOffset => GetResourceString("BadImageFormat_ResourcesNameInvalidOffset");

	internal static string BadImageFormat_ResourcesNameTooLong => GetResourceString("BadImageFormat_ResourcesNameTooLong");

	internal static string BadImageFormat_TypeMismatch => GetResourceString("BadImageFormat_TypeMismatch");

	internal static string CancellationToken_CreateLinkedToken_TokensIsEmpty => GetResourceString("CancellationToken_CreateLinkedToken_TokensIsEmpty");

	internal static string CancellationTokenSource_Disposed => GetResourceString("CancellationTokenSource_Disposed");

	internal static string ConcurrentCollection_SyncRoot_NotSupported => GetResourceString("ConcurrentCollection_SyncRoot_NotSupported");

	internal static string EventSource_AbstractMustNotDeclareEventMethods => GetResourceString("EventSource_AbstractMustNotDeclareEventMethods");

	internal static string EventSource_AbstractMustNotDeclareKTOC => GetResourceString("EventSource_AbstractMustNotDeclareKTOC");

	internal static string EventSource_AddScalarOutOfRange => GetResourceString("EventSource_AddScalarOutOfRange");

	internal static string EventSource_BadHexDigit => GetResourceString("EventSource_BadHexDigit");

	internal static string EventSource_ChannelTypeDoesNotMatchEventChannelValue => GetResourceString("EventSource_ChannelTypeDoesNotMatchEventChannelValue");

	internal static string EventSource_DataDescriptorsOutOfRange => GetResourceString("EventSource_DataDescriptorsOutOfRange");

	internal static string EventSource_DuplicateStringKey => GetResourceString("EventSource_DuplicateStringKey");

	internal static string EventSource_EnumKindMismatch => GetResourceString("EventSource_EnumKindMismatch");

	internal static string EventSource_EvenHexDigits => GetResourceString("EventSource_EvenHexDigits");

	internal static string EventSource_EventChannelOutOfRange => GetResourceString("EventSource_EventChannelOutOfRange");

	internal static string EventSource_EventIdReused => GetResourceString("EventSource_EventIdReused");

	internal static string EventSource_EventMustHaveTaskIfNonDefaultOpcode => GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode");

	internal static string EventSource_EventMustNotBeExplicitImplementation => GetResourceString("EventSource_EventMustNotBeExplicitImplementation");

	internal static string EventSource_EventNameReused => GetResourceString("EventSource_EventNameReused");

	internal static string EventSource_EventParametersMismatch => GetResourceString("EventSource_EventParametersMismatch");

	internal static string EventSource_EventSourceGuidInUse => GetResourceString("EventSource_EventSourceGuidInUse");

	internal static string EventSource_EventTooBig => GetResourceString("EventSource_EventTooBig");

	internal static string EventSource_EventWithAdminChannelMustHaveMessage => GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage");

	internal static string EventSource_IllegalKeywordsValue => GetResourceString("EventSource_IllegalKeywordsValue");

	internal static string EventSource_IllegalOpcodeValue => GetResourceString("EventSource_IllegalOpcodeValue");

	internal static string EventSource_IllegalTaskValue => GetResourceString("EventSource_IllegalTaskValue");

	internal static string EventSource_IllegalValue => GetResourceString("EventSource_IllegalValue");

	internal static string EventSource_IncorrentlyAuthoredTypeInfo => GetResourceString("EventSource_IncorrentlyAuthoredTypeInfo");

	internal static string EventSource_InvalidCommand => GetResourceString("EventSource_InvalidCommand");

	internal static string EventSource_InvalidEventFormat => GetResourceString("EventSource_InvalidEventFormat");

	internal static string EventSource_KeywordCollision => GetResourceString("EventSource_KeywordCollision");

	internal static string EventSource_KeywordNeedPowerOfTwo => GetResourceString("EventSource_KeywordNeedPowerOfTwo");

	internal static string EventSource_ListenerCreatedInsideCallback => GetResourceString("EventSource_ListenerCreatedInsideCallback");

	internal static string EventSource_ListenerNotFound => GetResourceString("EventSource_ListenerNotFound");

	internal static string EventSource_ListenerWriteFailure => GetResourceString("EventSource_ListenerWriteFailure");

	internal static string EventSource_MaxChannelExceeded => GetResourceString("EventSource_MaxChannelExceeded");

	internal static string EventSource_MismatchIdToWriteEvent => GetResourceString("EventSource_MismatchIdToWriteEvent");

	internal static string EventSource_NeedGuid => GetResourceString("EventSource_NeedGuid");

	internal static string EventSource_NeedName => GetResourceString("EventSource_NeedName");

	internal static string EventSource_NeedPositiveId => GetResourceString("EventSource_NeedPositiveId");

	internal static string EventSource_NoFreeBuffers => GetResourceString("EventSource_NoFreeBuffers");

	internal static string EventSource_NonCompliantTypeError => GetResourceString("EventSource_NonCompliantTypeError");

	internal static string EventSource_NoRelatedActivityId => GetResourceString("EventSource_NoRelatedActivityId");

	internal static string EventSource_NotSupportedArrayOfBinary => GetResourceString("EventSource_NotSupportedArrayOfBinary");

	internal static string EventSource_NotSupportedArrayOfNil => GetResourceString("EventSource_NotSupportedArrayOfNil");

	internal static string EventSource_NotSupportedArrayOfNullTerminatedString => GetResourceString("EventSource_NotSupportedArrayOfNullTerminatedString");

	internal static string EventSource_NotSupportedNestedArraysEnums => GetResourceString("EventSource_NotSupportedNestedArraysEnums");

	internal static string EventSource_NullInput => GetResourceString("EventSource_NullInput");

	internal static string EventSource_OpcodeCollision => GetResourceString("EventSource_OpcodeCollision");

	internal static string EventSource_PinArrayOutOfRange => GetResourceString("EventSource_PinArrayOutOfRange");

	internal static string EventSource_RecursiveTypeDefinition => GetResourceString("EventSource_RecursiveTypeDefinition");

	internal static string EventSource_StopsFollowStarts => GetResourceString("EventSource_StopsFollowStarts");

	internal static string EventSource_TaskCollision => GetResourceString("EventSource_TaskCollision");

	internal static string EventSource_TaskOpcodePairReused => GetResourceString("EventSource_TaskOpcodePairReused");

	internal static string EventSource_TooManyArgs => GetResourceString("EventSource_TooManyArgs");

	internal static string EventSource_TooManyFields => GetResourceString("EventSource_TooManyFields");

	internal static string EventSource_ToString => GetResourceString("EventSource_ToString");

	internal static string EventSource_TraitEven => GetResourceString("EventSource_TraitEven");

	internal static string EventSource_TypeMustBeSealedOrAbstract => GetResourceString("EventSource_TypeMustBeSealedOrAbstract");

	internal static string EventSource_TypeMustDeriveFromEventSource => GetResourceString("EventSource_TypeMustDeriveFromEventSource");

	internal static string EventSource_UndefinedChannel => GetResourceString("EventSource_UndefinedChannel");

	internal static string EventSource_UndefinedKeyword => GetResourceString("EventSource_UndefinedKeyword");

	internal static string EventSource_UndefinedOpcode => GetResourceString("EventSource_UndefinedOpcode");

	internal static string EventSource_UnknownEtwTrait => GetResourceString("EventSource_UnknownEtwTrait");

	internal static string EventSource_UnsupportedEventTypeInManifest => GetResourceString("EventSource_UnsupportedEventTypeInManifest");

	internal static string EventSource_UnsupportedMessageProperty => GetResourceString("EventSource_UnsupportedMessageProperty");

	internal static string EventSource_VarArgsParameterMismatch => GetResourceString("EventSource_VarArgsParameterMismatch");

	internal static string Exception_EndOfInnerExceptionStack => GetResourceString("Exception_EndOfInnerExceptionStack");

	internal static string Exception_EndStackTraceFromPreviousThrow => GetResourceString("Exception_EndStackTraceFromPreviousThrow");

	internal static string Exception_WasThrown => GetResourceString("Exception_WasThrown");

	internal static string ExecutionContext_ExceptionInAsyncLocalNotification => GetResourceString("ExecutionContext_ExceptionInAsyncLocalNotification");

	internal static string FileNotFound_ResolveAssembly => GetResourceString("FileNotFound_ResolveAssembly");

	internal static string Format_AttributeUsage => GetResourceString("Format_AttributeUsage");

	internal static string Format_Bad7BitInt => GetResourceString("Format_Bad7BitInt");

	internal static string Format_BadBase64Char => GetResourceString("Format_BadBase64Char");

	internal static string Format_BadBoolean => GetResourceString("Format_BadBoolean");

	internal static string Format_BadDateOnly => GetResourceString("Format_BadDateOnly");

	internal static string Format_BadTimeOnly => GetResourceString("Format_BadTimeOnly");

	internal static string Format_DateTimeOnlyContainsNoneDateParts => GetResourceString("Format_DateTimeOnlyContainsNoneDateParts");

	internal static string Format_BadFormatSpecifier => GetResourceString("Format_BadFormatSpecifier");

	internal static string Format_NoFormatSpecifier => GetResourceString("Format_NoFormatSpecifier");

	internal static string Format_BadHexChar => GetResourceString("Format_BadHexChar");

	internal static string Format_BadHexLength => GetResourceString("Format_BadHexLength");

	internal static string Format_BadQuote => GetResourceString("Format_BadQuote");

	internal static string Format_BadTimeSpan => GetResourceString("Format_BadTimeSpan");

	internal static string Format_EmptyInputString => GetResourceString("Format_EmptyInputString");

	internal static string Format_ExtraJunkAtEnd => GetResourceString("Format_ExtraJunkAtEnd");

	internal static string Format_GuidUnrecognized => GetResourceString("Format_GuidUnrecognized");

	internal static string Format_IndexOutOfRange => GetResourceString("Format_IndexOutOfRange");

	internal static string Format_InvalidEnumFormatSpecification => GetResourceString("Format_InvalidEnumFormatSpecification");

	internal static string Format_InvalidGuidFormatSpecification => GetResourceString("Format_InvalidGuidFormatSpecification");

	internal static string Format_InvalidString => GetResourceString("Format_InvalidString");

	internal static string Format_NeedSingleChar => GetResourceString("Format_NeedSingleChar");

	internal static string Format_NoParsibleDigits => GetResourceString("Format_NoParsibleDigits");

	internal static string Format_StringZeroLength => GetResourceString("Format_StringZeroLength");

	internal static string IndexOutOfRange_ArrayRankIndex => GetResourceString("IndexOutOfRange_ArrayRankIndex");

	internal static string IndexOutOfRange_UMSPosition => GetResourceString("IndexOutOfRange_UMSPosition");

	internal static string InsufficientMemory_MemFailPoint => GetResourceString("InsufficientMemory_MemFailPoint");

	internal static string InsufficientMemory_MemFailPoint_TooBig => GetResourceString("InsufficientMemory_MemFailPoint_TooBig");

	internal static string InsufficientMemory_MemFailPoint_VAFrag => GetResourceString("InsufficientMemory_MemFailPoint_VAFrag");

	internal static string Interop_COM_TypeMismatch => GetResourceString("Interop_COM_TypeMismatch");

	internal static string Interop_Marshal_Unmappable_Char => GetResourceString("Interop_Marshal_Unmappable_Char");

	internal static string Interop_Marshal_SafeHandle_InvalidOperation => GetResourceString("Interop_Marshal_SafeHandle_InvalidOperation");

	internal static string Interop_Marshal_CannotCreateSafeHandleField => GetResourceString("Interop_Marshal_CannotCreateSafeHandleField");

	internal static string Interop_Marshal_CannotCreateCriticalHandleField => GetResourceString("Interop_Marshal_CannotCreateCriticalHandleField");

	internal static string InvalidCast_CannotCastNullToValueType => GetResourceString("InvalidCast_CannotCastNullToValueType");

	internal static string InvalidCast_CannotCoerceByRefVariant => GetResourceString("InvalidCast_CannotCoerceByRefVariant");

	internal static string InvalidCast_DBNull => GetResourceString("InvalidCast_DBNull");

	internal static string InvalidCast_Empty => GetResourceString("InvalidCast_Empty");

	internal static string InvalidCast_FromDBNull => GetResourceString("InvalidCast_FromDBNull");

	internal static string InvalidCast_FromTo => GetResourceString("InvalidCast_FromTo");

	internal static string InvalidCast_IConvertible => GetResourceString("InvalidCast_IConvertible");

	internal static string InvalidOperation_AsyncFlowCtrlCtxMismatch => GetResourceString("InvalidOperation_AsyncFlowCtrlCtxMismatch");

	internal static string InvalidOperation_AsyncIOInProgress => GetResourceString("InvalidOperation_AsyncIOInProgress");

	internal static string InvalidOperation_BadEmptyMethodBody => GetResourceString("InvalidOperation_BadEmptyMethodBody");

	internal static string InvalidOperation_BadILGeneratorUsage => GetResourceString("InvalidOperation_BadILGeneratorUsage");

	internal static string InvalidOperation_BadInstructionOrIndexOutOfBound => GetResourceString("InvalidOperation_BadInstructionOrIndexOutOfBound");

	internal static string InvalidOperation_BadInterfaceNotAbstract => GetResourceString("InvalidOperation_BadInterfaceNotAbstract");

	internal static string InvalidOperation_BadMethodBody => GetResourceString("InvalidOperation_BadMethodBody");

	internal static string InvalidOperation_BadTypeAttributesNotAbstract => GetResourceString("InvalidOperation_BadTypeAttributesNotAbstract");

	internal static string InvalidOperation_CalledTwice => GetResourceString("InvalidOperation_CalledTwice");

	internal static string InvalidOperation_CannotImportGlobalFromDifferentModule => GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule");

	internal static string InvalidOperation_CannotRegisterSecondResolver => GetResourceString("InvalidOperation_CannotRegisterSecondResolver");

	internal static string InvalidOperation_CannotRestoreUnsupressedFlow => GetResourceString("InvalidOperation_CannotRestoreUnsupressedFlow");

	internal static string InvalidOperation_CannotSupressFlowMultipleTimes => GetResourceString("InvalidOperation_CannotSupressFlowMultipleTimes");

	internal static string InvalidOperation_CannotUseAFCMultiple => GetResourceString("InvalidOperation_CannotUseAFCMultiple");

	internal static string InvalidOperation_CannotUseAFCOtherThread => GetResourceString("InvalidOperation_CannotUseAFCOtherThread");

	internal static string InvalidOperation_CollectionCorrupted => GetResourceString("InvalidOperation_CollectionCorrupted");

	internal static string InvalidOperation_ComputerName => GetResourceString("InvalidOperation_ComputerName");

	internal static string InvalidOperation_ConcurrentOperationsNotSupported => GetResourceString("InvalidOperation_ConcurrentOperationsNotSupported");

	internal static string InvalidOperation_ConstructorNotAllowedOnInterface => GetResourceString("InvalidOperation_ConstructorNotAllowedOnInterface");

	internal static string InvalidOperation_DateTimeParsing => GetResourceString("InvalidOperation_DateTimeParsing");

	internal static string InvalidOperation_DefaultConstructorILGen => GetResourceString("InvalidOperation_DefaultConstructorILGen");

	internal static string InvalidOperation_EnumEnded => GetResourceString("InvalidOperation_EnumEnded");

	internal static string InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion");

	internal static string InvalidOperation_EnumNotStarted => GetResourceString("InvalidOperation_EnumNotStarted");

	internal static string InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen");

	internal static string InvalidOperation_EventInfoNotAvailable => GetResourceString("InvalidOperation_EventInfoNotAvailable");

	internal static string InvalidOperation_GenericParametersAlreadySet => GetResourceString("InvalidOperation_GenericParametersAlreadySet");

	internal static string InvalidOperation_GetVersion => GetResourceString("InvalidOperation_GetVersion");

	internal static string InvalidOperation_GlobalsHaveBeenCreated => GetResourceString("InvalidOperation_GlobalsHaveBeenCreated");

	internal static string InvalidOperation_HandleIsNotInitialized => GetResourceString("InvalidOperation_HandleIsNotInitialized");

	internal static string InvalidOperation_HandleIsNotPinned => GetResourceString("InvalidOperation_HandleIsNotPinned");

	internal static string InvalidOperation_HashInsertFailed => GetResourceString("InvalidOperation_HashInsertFailed");

	internal static string InvalidOperation_IComparerFailed => GetResourceString("InvalidOperation_IComparerFailed");

	internal static string InvalidOperation_MethodBaked => GetResourceString("InvalidOperation_MethodBaked");

	internal static string InvalidOperation_MethodBuilderBaked => GetResourceString("InvalidOperation_MethodBuilderBaked");

	internal static string InvalidOperation_MethodHasBody => GetResourceString("InvalidOperation_MethodHasBody");

	internal static string InvalidOperation_MustCallInitialize => GetResourceString("InvalidOperation_MustCallInitialize");

	internal static string InvalidOperation_NativeOverlappedReused => GetResourceString("InvalidOperation_NativeOverlappedReused");

	internal static string InvalidOperation_NoMultiModuleAssembly => GetResourceString("InvalidOperation_NoMultiModuleAssembly");

	internal static string InvalidOperation_NoPublicAddMethod => GetResourceString("InvalidOperation_NoPublicAddMethod");

	internal static string InvalidOperation_NoPublicRemoveMethod => GetResourceString("InvalidOperation_NoPublicRemoveMethod");

	internal static string InvalidOperation_NotADebugModule => GetResourceString("InvalidOperation_NotADebugModule");

	internal static string InvalidOperation_NotAllowedInDynamicMethod => GetResourceString("InvalidOperation_NotAllowedInDynamicMethod");

	internal static string InvalidOperation_NotAVarArgCallingConvention => GetResourceString("InvalidOperation_NotAVarArgCallingConvention");

	internal static string InvalidOperation_NotGenericType => GetResourceString("InvalidOperation_NotGenericType");

	internal static string InvalidOperation_NotWithConcurrentGC => GetResourceString("InvalidOperation_NotWithConcurrentGC");

	internal static string InvalidOperation_NoUnderlyingTypeOnEnum => GetResourceString("InvalidOperation_NoUnderlyingTypeOnEnum");

	internal static string InvalidOperation_NoValue => GetResourceString("InvalidOperation_NoValue");

	internal static string InvalidOperation_NullArray => GetResourceString("InvalidOperation_NullArray");

	internal static string InvalidOperation_NullContext => GetResourceString("InvalidOperation_NullContext");

	internal static string InvalidOperation_NullModuleHandle => GetResourceString("InvalidOperation_NullModuleHandle");

	internal static string InvalidOperation_OpenLocalVariableScope => GetResourceString("InvalidOperation_OpenLocalVariableScope");

	internal static string InvalidOperation_Overlapped_Pack => GetResourceString("InvalidOperation_Overlapped_Pack");

	internal static string InvalidOperation_PropertyInfoNotAvailable => GetResourceString("InvalidOperation_PropertyInfoNotAvailable");

	internal static string InvalidOperation_ReadOnly => GetResourceString("InvalidOperation_ReadOnly");

	internal static string InvalidOperation_ResMgrBadResSet_Type => GetResourceString("InvalidOperation_ResMgrBadResSet_Type");

	internal static string InvalidOperation_ResourceNotStream_Name => GetResourceString("InvalidOperation_ResourceNotStream_Name");

	internal static string InvalidOperation_ResourceNotString_Name => GetResourceString("InvalidOperation_ResourceNotString_Name");

	internal static string InvalidOperation_ResourceNotString_Type => GetResourceString("InvalidOperation_ResourceNotString_Type");

	internal static string InvalidOperation_SetLatencyModeNoGC => GetResourceString("InvalidOperation_SetLatencyModeNoGC");

	internal static string InvalidOperation_ShouldNotHaveMethodBody => GetResourceString("InvalidOperation_ShouldNotHaveMethodBody");

	internal static string InvalidOperation_ThreadWrongThreadStart => GetResourceString("InvalidOperation_ThreadWrongThreadStart");

	internal static string InvalidOperation_TimeoutsNotSupported => GetResourceString("InvalidOperation_TimeoutsNotSupported");

	internal static string InvalidOperation_TimerAlreadyClosed => GetResourceString("InvalidOperation_TimerAlreadyClosed");

	internal static string InvalidOperation_TypeHasBeenCreated => GetResourceString("InvalidOperation_TypeHasBeenCreated");

	internal static string InvalidOperation_TypeNotCreated => GetResourceString("InvalidOperation_TypeNotCreated");

	internal static string InvalidOperation_UnderlyingArrayListChanged => GetResourceString("InvalidOperation_UnderlyingArrayListChanged");

	internal static string InvalidOperation_UnknownEnumType => GetResourceString("InvalidOperation_UnknownEnumType");

	internal static string InvalidOperation_WrongAsyncResultOrEndCalledMultiple => GetResourceString("InvalidOperation_WrongAsyncResultOrEndCalledMultiple");

	internal static string InvalidProgram_Default => GetResourceString("InvalidProgram_Default");

	internal static string InvalidTimeZone_InvalidRegistryData => GetResourceString("InvalidTimeZone_InvalidRegistryData");

	internal static string InvariantFailed => GetResourceString("InvariantFailed");

	internal static string InvariantFailed_Cnd => GetResourceString("InvariantFailed_Cnd");

	internal static string IO_NoFileTableInInMemoryAssemblies => GetResourceString("IO_NoFileTableInInMemoryAssemblies");

	internal static string IO_EOF_ReadBeyondEOF => GetResourceString("IO_EOF_ReadBeyondEOF");

	internal static string IO_FileLoad => GetResourceString("IO_FileLoad");

	internal static string IO_FileName_Name => GetResourceString("IO_FileName_Name");

	internal static string IO_FileNotFound => GetResourceString("IO_FileNotFound");

	internal static string IO_FileNotFound_FileName => GetResourceString("IO_FileNotFound_FileName");

	internal static string IO_AlreadyExists_Name => GetResourceString("IO_AlreadyExists_Name");

	internal static string IO_DiskFull_Path_AllocationSize => GetResourceString("IO_DiskFull_Path_AllocationSize");

	internal static string IO_FileTooLarge_Path_AllocationSize => GetResourceString("IO_FileTooLarge_Path_AllocationSize");

	internal static string IO_BindHandleFailed => GetResourceString("IO_BindHandleFailed");

	internal static string IO_FileExists_Name => GetResourceString("IO_FileExists_Name");

	internal static string IO_FileStreamHandlePosition => GetResourceString("IO_FileStreamHandlePosition");

	internal static string IO_FileTooLong2GB => GetResourceString("IO_FileTooLong2GB");

	internal static string IO_FileTooLong => GetResourceString("IO_FileTooLong");

	internal static string IO_FileTooLongOrHandleNotSync => GetResourceString("IO_FileTooLongOrHandleNotSync");

	internal static string IO_FixedCapacity => GetResourceString("IO_FixedCapacity");

	internal static string IO_InvalidStringLen_Len => GetResourceString("IO_InvalidStringLen_Len");

	internal static string IO_SeekAppendOverwrite => GetResourceString("IO_SeekAppendOverwrite");

	internal static string IO_SeekBeforeBegin => GetResourceString("IO_SeekBeforeBegin");

	internal static string IO_SetLengthAppendTruncate => GetResourceString("IO_SetLengthAppendTruncate");

	internal static string IO_SharingViolation_File => GetResourceString("IO_SharingViolation_File");

	internal static string IO_SharingViolation_NoFileName => GetResourceString("IO_SharingViolation_NoFileName");

	internal static string IO_StreamTooLong => GetResourceString("IO_StreamTooLong");

	internal static string IO_PathNotFound_NoPathName => GetResourceString("IO_PathNotFound_NoPathName");

	internal static string IO_PathNotFound_Path => GetResourceString("IO_PathNotFound_Path");

	internal static string IO_PathTooLong => GetResourceString("IO_PathTooLong");

	internal static string IO_PathTooLong_Path => GetResourceString("IO_PathTooLong_Path");

	internal static string IO_UnknownFileName => GetResourceString("IO_UnknownFileName");

	internal static string Lazy_CreateValue_NoParameterlessCtorForT => GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT");

	internal static string Lazy_ctor_ModeInvalid => GetResourceString("Lazy_ctor_ModeInvalid");

	internal static string Lazy_StaticInit_InvalidOperation => GetResourceString("Lazy_StaticInit_InvalidOperation");

	internal static string Lazy_ToString_ValueNotCreated => GetResourceString("Lazy_ToString_ValueNotCreated");

	internal static string Lazy_Value_RecursiveCallsToValue => GetResourceString("Lazy_Value_RecursiveCallsToValue");

	internal static string ManualResetEventSlim_ctor_SpinCountOutOfRange => GetResourceString("ManualResetEventSlim_ctor_SpinCountOutOfRange");

	internal static string ManualResetEventSlim_ctor_TooManyWaiters => GetResourceString("ManualResetEventSlim_ctor_TooManyWaiters");

	internal static string ManualResetEventSlim_Disposed => GetResourceString("ManualResetEventSlim_Disposed");

	internal static string Marshaler_StringTooLong => GetResourceString("Marshaler_StringTooLong");

	internal static string MissingConstructor_Name => GetResourceString("MissingConstructor_Name");

	internal static string MissingField => GetResourceString("MissingField");

	internal static string MissingField_Name => GetResourceString("MissingField_Name");

	internal static string MissingManifestResource_MultipleBlobs => GetResourceString("MissingManifestResource_MultipleBlobs");

	internal static string MissingManifestResource_NoNeutralAsm => GetResourceString("MissingManifestResource_NoNeutralAsm");

	internal static string MissingManifestResource_NoNeutralDisk => GetResourceString("MissingManifestResource_NoNeutralDisk");

	internal static string MissingMember => GetResourceString("MissingMember");

	internal static string MissingMember_Name => GetResourceString("MissingMember_Name");

	internal static string MissingMemberNestErr => GetResourceString("MissingMemberNestErr");

	internal static string MissingMemberTypeRef => GetResourceString("MissingMemberTypeRef");

	internal static string MissingMethod_Name => GetResourceString("MissingMethod_Name");

	internal static string MissingSatelliteAssembly_Culture_Name => GetResourceString("MissingSatelliteAssembly_Culture_Name");

	internal static string MissingSatelliteAssembly_Default => GetResourceString("MissingSatelliteAssembly_Default");

	internal static string Multicast_Combine => GetResourceString("Multicast_Combine");

	internal static string MustUseCCRewrite => GetResourceString("MustUseCCRewrite");

	internal static string NotSupported_AbstractNonCLS => GetResourceString("NotSupported_AbstractNonCLS");

	internal static string NotSupported_ActivAttr => GetResourceString("NotSupported_ActivAttr");

	internal static string NotSupported_AssemblyLoadFromHash => GetResourceString("NotSupported_AssemblyLoadFromHash");

	internal static string NotSupported_ByRefToByRefLikeReturn => GetResourceString("NotSupported_ByRefToByRefLikeReturn");

	internal static string NotSupported_ByRefToVoidReturn => GetResourceString("NotSupported_ByRefToVoidReturn");

	internal static string NotSupported_CallToVarArg => GetResourceString("NotSupported_CallToVarArg");

	internal static string NotSupported_CannotCallEqualsOnSpan => GetResourceString("NotSupported_CannotCallEqualsOnSpan");

	internal static string NotSupported_CannotCallGetHashCodeOnSpan => GetResourceString("NotSupported_CannotCallGetHashCodeOnSpan");

	internal static string NotSupported_ChangeType => GetResourceString("NotSupported_ChangeType");

	internal static string NotSupported_CreateInstanceWithTypeBuilder => GetResourceString("NotSupported_CreateInstanceWithTypeBuilder");

	internal static string NotSupported_DBNullSerial => GetResourceString("NotSupported_DBNullSerial");

	internal static string NotSupported_DynamicAssembly => GetResourceString("NotSupported_DynamicAssembly");

	internal static string NotSupported_DynamicMethodFlags => GetResourceString("NotSupported_DynamicMethodFlags");

	internal static string NotSupported_DynamicModule => GetResourceString("NotSupported_DynamicModule");

	internal static string NotSupported_FixedSizeCollection => GetResourceString("NotSupported_FixedSizeCollection");

	internal static string InvalidOperation_SpanOverlappedOperation => GetResourceString("InvalidOperation_SpanOverlappedOperation");

	internal static string NotSupported_IllegalOneByteBranch => GetResourceString("NotSupported_IllegalOneByteBranch");

	internal static string NotSupported_KeyCollectionSet => GetResourceString("NotSupported_KeyCollectionSet");

	internal static string NotSupported_MaxWaitHandles => GetResourceString("NotSupported_MaxWaitHandles");

	internal static string NotSupported_MemStreamNotExpandable => GetResourceString("NotSupported_MemStreamNotExpandable");

	internal static string NotSupported_MustBeModuleBuilder => GetResourceString("NotSupported_MustBeModuleBuilder");

	internal static string NotSupported_NoCodepageData => GetResourceString("NotSupported_NoCodepageData");

	internal static string InvalidOperation_FunctionMissingUnmanagedCallersOnly => GetResourceString("InvalidOperation_FunctionMissingUnmanagedCallersOnly");

	internal static string NotSupported_NonReflectedType => GetResourceString("NotSupported_NonReflectedType");

	internal static string NotSupported_NoParentDefaultConstructor => GetResourceString("NotSupported_NoParentDefaultConstructor");

	internal static string NotSupported_NoTypeInfo => GetResourceString("NotSupported_NoTypeInfo");

	internal static string NotSupported_NYI => GetResourceString("NotSupported_NYI");

	internal static string NotSupported_ObsoleteResourcesFile => GetResourceString("NotSupported_ObsoleteResourcesFile");

	internal static string NotSupported_OutputStreamUsingTypeBuilder => GetResourceString("NotSupported_OutputStreamUsingTypeBuilder");

	internal static string NotSupported_RangeCollection => GetResourceString("NotSupported_RangeCollection");

	internal static string NotSupported_Reading => GetResourceString("NotSupported_Reading");

	internal static string NotSupported_ReadOnlyCollection => GetResourceString("NotSupported_ReadOnlyCollection");

	internal static string NotSupported_ResourceObjectSerialization => GetResourceString("NotSupported_ResourceObjectSerialization");

	internal static string NotSupported_StringComparison => GetResourceString("NotSupported_StringComparison");

	internal static string NotSupported_SubclassOverride => GetResourceString("NotSupported_SubclassOverride");

	internal static string NotSupported_SymbolMethod => GetResourceString("NotSupported_SymbolMethod");

	internal static string NotSupported_Type => GetResourceString("NotSupported_Type");

	internal static string NotSupported_TypeNotYetCreated => GetResourceString("NotSupported_TypeNotYetCreated");

	internal static string NotSupported_UmsSafeBuffer => GetResourceString("NotSupported_UmsSafeBuffer");

	internal static string NotSupported_UnitySerHolder => GetResourceString("NotSupported_UnitySerHolder");

	internal static string NotSupported_UnknownTypeCode => GetResourceString("NotSupported_UnknownTypeCode");

	internal static string NotSupported_UnreadableStream => GetResourceString("NotSupported_UnreadableStream");

	internal static string NotSupported_UnseekableStream => GetResourceString("NotSupported_UnseekableStream");

	internal static string NotSupported_UnwritableStream => GetResourceString("NotSupported_UnwritableStream");

	internal static string NotSupported_ValueCollectionSet => GetResourceString("NotSupported_ValueCollectionSet");

	internal static string NotSupported_Writing => GetResourceString("NotSupported_Writing");

	internal static string NotSupported_WrongResourceReader_Type => GetResourceString("NotSupported_WrongResourceReader_Type");

	internal static string ObjectDisposed_FileClosed => GetResourceString("ObjectDisposed_FileClosed");

	internal static string ObjectDisposed_Generic => GetResourceString("ObjectDisposed_Generic");

	internal static string ObjectDisposed_ObjectName_Name => GetResourceString("ObjectDisposed_ObjectName_Name");

	internal static string ObjectDisposed_WriterClosed => GetResourceString("ObjectDisposed_WriterClosed");

	internal static string ObjectDisposed_ReaderClosed => GetResourceString("ObjectDisposed_ReaderClosed");

	internal static string ObjectDisposed_ResourceSet => GetResourceString("ObjectDisposed_ResourceSet");

	internal static string ObjectDisposed_StreamClosed => GetResourceString("ObjectDisposed_StreamClosed");

	internal static string ObjectDisposed_ViewAccessorClosed => GetResourceString("ObjectDisposed_ViewAccessorClosed");

	internal static string ObjectDisposed_SafeHandleClosed => GetResourceString("ObjectDisposed_SafeHandleClosed");

	internal static string OperationCanceled => GetResourceString("OperationCanceled");

	internal static string Overflow_Byte => GetResourceString("Overflow_Byte");

	internal static string Overflow_Char => GetResourceString("Overflow_Char");

	internal static string Overflow_Currency => GetResourceString("Overflow_Currency");

	internal static string Overflow_Decimal => GetResourceString("Overflow_Decimal");

	internal static string Overflow_Duration => GetResourceString("Overflow_Duration");

	internal static string Overflow_Int16 => GetResourceString("Overflow_Int16");

	internal static string Overflow_Int32 => GetResourceString("Overflow_Int32");

	internal static string Overflow_Int64 => GetResourceString("Overflow_Int64");

	internal static string Overflow_NegateTwosCompNum => GetResourceString("Overflow_NegateTwosCompNum");

	internal static string Overflow_NegativeUnsigned => GetResourceString("Overflow_NegativeUnsigned");

	internal static string Overflow_SByte => GetResourceString("Overflow_SByte");

	internal static string Overflow_TimeSpanElementTooLarge => GetResourceString("Overflow_TimeSpanElementTooLarge");

	internal static string Overflow_TimeSpanTooLong => GetResourceString("Overflow_TimeSpanTooLong");

	internal static string Overflow_UInt16 => GetResourceString("Overflow_UInt16");

	internal static string Overflow_UInt32 => GetResourceString("Overflow_UInt32");

	internal static string Overflow_UInt64 => GetResourceString("Overflow_UInt64");

	internal static string PlatformNotSupported_ReflectionOnly => GetResourceString("PlatformNotSupported_ReflectionOnly");

	internal static string PlatformNotSupported_Remoting => GetResourceString("PlatformNotSupported_Remoting");

	internal static string PlatformNotSupported_SecureBinarySerialization => GetResourceString("PlatformNotSupported_SecureBinarySerialization");

	internal static string PlatformNotSupported_StrongNameSigning => GetResourceString("PlatformNotSupported_StrongNameSigning");

	internal static string PlatformNotSupported_ITypeInfo => GetResourceString("PlatformNotSupported_ITypeInfo");

	internal static string PlatformNotSupported_IExpando => GetResourceString("PlatformNotSupported_IExpando");

	internal static string PlatformNotSupported_AppDomains => GetResourceString("PlatformNotSupported_AppDomains");

	internal static string PlatformNotSupported_CAS => GetResourceString("PlatformNotSupported_CAS");

	internal static string PlatformNotSupported_Principal => GetResourceString("PlatformNotSupported_Principal");

	internal static string PlatformNotSupported_ThreadAbort => GetResourceString("PlatformNotSupported_ThreadAbort");

	internal static string PlatformNotSupported_ThreadSuspend => GetResourceString("PlatformNotSupported_ThreadSuspend");

	internal static string PostconditionFailed => GetResourceString("PostconditionFailed");

	internal static string PostconditionFailed_Cnd => GetResourceString("PostconditionFailed_Cnd");

	internal static string PostconditionOnExceptionFailed => GetResourceString("PostconditionOnExceptionFailed");

	internal static string PostconditionOnExceptionFailed_Cnd => GetResourceString("PostconditionOnExceptionFailed_Cnd");

	internal static string PreconditionFailed => GetResourceString("PreconditionFailed");

	internal static string PreconditionFailed_Cnd => GetResourceString("PreconditionFailed_Cnd");

	internal static string Rank_MultiDimNotSupported => GetResourceString("Rank_MultiDimNotSupported");

	internal static string Rank_MustMatch => GetResourceString("Rank_MustMatch");

	internal static string ResourceReaderIsClosed => GetResourceString("ResourceReaderIsClosed");

	internal static string Resources_StreamNotValid => GetResourceString("Resources_StreamNotValid");

	internal static string RFLCT_AmbigCust => GetResourceString("RFLCT_AmbigCust");

	internal static string RFLCT_Ambiguous => GetResourceString("RFLCT_Ambiguous");

	internal static string InvalidFilterCriteriaException_CritInt => GetResourceString("InvalidFilterCriteriaException_CritInt");

	internal static string InvalidFilterCriteriaException_CritString => GetResourceString("InvalidFilterCriteriaException_CritString");

	internal static string RFLCT_InvalidFieldFail => GetResourceString("RFLCT_InvalidFieldFail");

	internal static string RFLCT_InvalidPropFail => GetResourceString("RFLCT_InvalidPropFail");

	internal static string RFLCT_Targ_ITargMismatch => GetResourceString("RFLCT_Targ_ITargMismatch");

	internal static string RFLCT_Targ_StatFldReqTarg => GetResourceString("RFLCT_Targ_StatFldReqTarg");

	internal static string RFLCT_Targ_StatMethReqTarg => GetResourceString("RFLCT_Targ_StatMethReqTarg");

	internal static string RuntimeWrappedException => GetResourceString("RuntimeWrappedException");

	internal static string StandardOleMarshalObjectGetMarshalerFailed => GetResourceString("StandardOleMarshalObjectGetMarshalerFailed");

	internal static string Security_CannotReadRegistryData => GetResourceString("Security_CannotReadRegistryData");

	internal static string Security_RegistryPermission => GetResourceString("Security_RegistryPermission");

	internal static string SemaphoreSlim_ctor_InitialCountWrong => GetResourceString("SemaphoreSlim_ctor_InitialCountWrong");

	internal static string SemaphoreSlim_ctor_MaxCountWrong => GetResourceString("SemaphoreSlim_ctor_MaxCountWrong");

	internal static string SemaphoreSlim_Disposed => GetResourceString("SemaphoreSlim_Disposed");

	internal static string SemaphoreSlim_Release_CountWrong => GetResourceString("SemaphoreSlim_Release_CountWrong");

	internal static string SemaphoreSlim_Wait_TimeoutWrong => GetResourceString("SemaphoreSlim_Wait_TimeoutWrong");

	internal static string Serialization_BadParameterInfo => GetResourceString("Serialization_BadParameterInfo");

	internal static string Serialization_CorruptField => GetResourceString("Serialization_CorruptField");

	internal static string Serialization_DateTimeTicksOutOfRange => GetResourceString("Serialization_DateTimeTicksOutOfRange");

	internal static string Serialization_DelegatesNotSupported => GetResourceString("Serialization_DelegatesNotSupported");

	internal static string Serialization_InsufficientState => GetResourceString("Serialization_InsufficientState");

	internal static string Serialization_InvalidData => GetResourceString("Serialization_InvalidData");

	internal static string Serialization_InvalidEscapeSequence => GetResourceString("Serialization_InvalidEscapeSequence");

	internal static string Serialization_InvalidOnDeser => GetResourceString("Serialization_InvalidOnDeser");

	internal static string Serialization_InvalidType => GetResourceString("Serialization_InvalidType");

	internal static string Serialization_KeyValueDifferentSizes => GetResourceString("Serialization_KeyValueDifferentSizes");

	internal static string Serialization_MissingDateTimeData => GetResourceString("Serialization_MissingDateTimeData");

	internal static string Serialization_MissingKeys => GetResourceString("Serialization_MissingKeys");

	internal static string Serialization_MissingValues => GetResourceString("Serialization_MissingValues");

	internal static string Serialization_NoParameterInfo => GetResourceString("Serialization_NoParameterInfo");

	internal static string Serialization_NotFound => GetResourceString("Serialization_NotFound");

	internal static string Serialization_NullKey => GetResourceString("Serialization_NullKey");

	internal static string Serialization_OptionalFieldVersionValue => GetResourceString("Serialization_OptionalFieldVersionValue");

	internal static string Serialization_SameNameTwice => GetResourceString("Serialization_SameNameTwice");

	internal static string Serialization_StringBuilderCapacity => GetResourceString("Serialization_StringBuilderCapacity");

	internal static string Serialization_StringBuilderMaxCapacity => GetResourceString("Serialization_StringBuilderMaxCapacity");

	internal static string SpinLock_Exit_SynchronizationLockException => GetResourceString("SpinLock_Exit_SynchronizationLockException");

	internal static string SpinLock_IsHeldByCurrentThread => GetResourceString("SpinLock_IsHeldByCurrentThread");

	internal static string SpinLock_TryEnter_ArgumentOutOfRange => GetResourceString("SpinLock_TryEnter_ArgumentOutOfRange");

	internal static string SpinLock_TryEnter_LockRecursionException => GetResourceString("SpinLock_TryEnter_LockRecursionException");

	internal static string SpinLock_TryReliableEnter_ArgumentException => GetResourceString("SpinLock_TryReliableEnter_ArgumentException");

	internal static string SpinWait_SpinUntil_ArgumentNull => GetResourceString("SpinWait_SpinUntil_ArgumentNull");

	internal static string SpinWait_SpinUntil_TimeoutWrong => GetResourceString("SpinWait_SpinUntil_TimeoutWrong");

	internal static string Task_ContinueWith_ESandLR => GetResourceString("Task_ContinueWith_ESandLR");

	internal static string Task_ContinueWith_NotOnAnything => GetResourceString("Task_ContinueWith_NotOnAnything");

	internal static string Task_InvalidTimerTimeSpan => GetResourceString("Task_InvalidTimerTimeSpan");

	internal static string Task_Delay_InvalidMillisecondsDelay => GetResourceString("Task_Delay_InvalidMillisecondsDelay");

	internal static string Task_Dispose_NotCompleted => GetResourceString("Task_Dispose_NotCompleted");

	internal static string Task_FromAsync_LongRunning => GetResourceString("Task_FromAsync_LongRunning");

	internal static string Task_FromAsync_PreferFairness => GetResourceString("Task_FromAsync_PreferFairness");

	internal static string Task_MultiTaskContinuation_EmptyTaskList => GetResourceString("Task_MultiTaskContinuation_EmptyTaskList");

	internal static string Task_MultiTaskContinuation_FireOptions => GetResourceString("Task_MultiTaskContinuation_FireOptions");

	internal static string Task_MultiTaskContinuation_NullTask => GetResourceString("Task_MultiTaskContinuation_NullTask");

	internal static string Task_RunSynchronously_AlreadyStarted => GetResourceString("Task_RunSynchronously_AlreadyStarted");

	internal static string Task_RunSynchronously_Continuation => GetResourceString("Task_RunSynchronously_Continuation");

	internal static string Task_RunSynchronously_Promise => GetResourceString("Task_RunSynchronously_Promise");

	internal static string Task_RunSynchronously_TaskCompleted => GetResourceString("Task_RunSynchronously_TaskCompleted");

	internal static string Task_Start_AlreadyStarted => GetResourceString("Task_Start_AlreadyStarted");

	internal static string Task_Start_ContinuationTask => GetResourceString("Task_Start_ContinuationTask");

	internal static string Task_Start_Promise => GetResourceString("Task_Start_Promise");

	internal static string Task_Start_TaskCompleted => GetResourceString("Task_Start_TaskCompleted");

	internal static string Task_ThrowIfDisposed => GetResourceString("Task_ThrowIfDisposed");

	internal static string Task_WaitMulti_NullTask => GetResourceString("Task_WaitMulti_NullTask");

	internal static string TaskCanceledException_ctor_DefaultMessage => GetResourceString("TaskCanceledException_ctor_DefaultMessage");

	internal static string TaskCompletionSourceT_TrySetException_NoExceptions => GetResourceString("TaskCompletionSourceT_TrySetException_NoExceptions");

	internal static string TaskCompletionSourceT_TrySetException_NullException => GetResourceString("TaskCompletionSourceT_TrySetException_NullException");

	internal static string TaskExceptionHolder_UnhandledException => GetResourceString("TaskExceptionHolder_UnhandledException");

	internal static string TaskExceptionHolder_UnknownExceptionType => GetResourceString("TaskExceptionHolder_UnknownExceptionType");

	internal static string TaskScheduler_ExecuteTask_WrongTaskScheduler => GetResourceString("TaskScheduler_ExecuteTask_WrongTaskScheduler");

	internal static string TaskScheduler_FromCurrentSynchronizationContext_NoCurrent => GetResourceString("TaskScheduler_FromCurrentSynchronizationContext_NoCurrent");

	internal static string TaskScheduler_InconsistentStateAfterTryExecuteTaskInline => GetResourceString("TaskScheduler_InconsistentStateAfterTryExecuteTaskInline");

	internal static string TaskSchedulerException_ctor_DefaultMessage => GetResourceString("TaskSchedulerException_ctor_DefaultMessage");

	internal static string TaskT_DebuggerNoResult => GetResourceString("TaskT_DebuggerNoResult");

	internal static string TaskT_TransitionToFinal_AlreadyCompleted => GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted");

	internal static string Thread_ApartmentState_ChangeFailed => GetResourceString("Thread_ApartmentState_ChangeFailed");

	internal static string Thread_GetSetCompressedStack_NotSupported => GetResourceString("Thread_GetSetCompressedStack_NotSupported");

	internal static string Thread_Operation_RequiresCurrentThread => GetResourceString("Thread_Operation_RequiresCurrentThread");

	internal static string Threading_AbandonedMutexException => GetResourceString("Threading_AbandonedMutexException");

	internal static string Threading_WaitHandleCannotBeOpenedException => GetResourceString("Threading_WaitHandleCannotBeOpenedException");

	internal static string Threading_WaitHandleCannotBeOpenedException_InvalidHandle => GetResourceString("Threading_WaitHandleCannotBeOpenedException_InvalidHandle");

	internal static string Threading_WaitHandleTooManyPosts => GetResourceString("Threading_WaitHandleTooManyPosts");

	internal static string Threading_SemaphoreFullException => GetResourceString("Threading_SemaphoreFullException");

	internal static string ThreadLocal_Disposed => GetResourceString("ThreadLocal_Disposed");

	internal static string ThreadLocal_Value_RecursiveCallsToValue => GetResourceString("ThreadLocal_Value_RecursiveCallsToValue");

	internal static string ThreadLocal_ValuesNotAvailable => GetResourceString("ThreadLocal_ValuesNotAvailable");

	internal static string TimeZoneNotFound_MissingData => GetResourceString("TimeZoneNotFound_MissingData");

	internal static string TypeInitialization_Default => GetResourceString("TypeInitialization_Default");

	internal static string TypeInitialization_Type => GetResourceString("TypeInitialization_Type");

	internal static string TypeLoad_ResolveNestedType => GetResourceString("TypeLoad_ResolveNestedType");

	internal static string TypeLoad_ResolveType => GetResourceString("TypeLoad_ResolveType");

	internal static string TypeLoad_ResolveTypeFromAssembly => GetResourceString("TypeLoad_ResolveTypeFromAssembly");

	internal static string UnauthorizedAccess_IODenied_NoPathName => GetResourceString("UnauthorizedAccess_IODenied_NoPathName");

	internal static string UnauthorizedAccess_IODenied_Path => GetResourceString("UnauthorizedAccess_IODenied_Path");

	internal static string UnauthorizedAccess_MemStreamBuffer => GetResourceString("UnauthorizedAccess_MemStreamBuffer");

	internal static string UnauthorizedAccess_RegistryKeyGeneric_Key => GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key");

	internal static string UnknownError_Num => GetResourceString("UnknownError_Num");

	internal static string Verification_Exception => GetResourceString("Verification_Exception");

	internal static string DebugAssertBanner => GetResourceString("DebugAssertBanner");

	internal static string DebugAssertLongMessage => GetResourceString("DebugAssertLongMessage");

	internal static string DebugAssertShortMessage => GetResourceString("DebugAssertShortMessage");

	internal static string LockRecursionException_ReadAfterWriteNotAllowed => GetResourceString("LockRecursionException_ReadAfterWriteNotAllowed");

	internal static string LockRecursionException_RecursiveReadNotAllowed => GetResourceString("LockRecursionException_RecursiveReadNotAllowed");

	internal static string LockRecursionException_RecursiveWriteNotAllowed => GetResourceString("LockRecursionException_RecursiveWriteNotAllowed");

	internal static string LockRecursionException_RecursiveUpgradeNotAllowed => GetResourceString("LockRecursionException_RecursiveUpgradeNotAllowed");

	internal static string LockRecursionException_WriteAfterReadNotAllowed => GetResourceString("LockRecursionException_WriteAfterReadNotAllowed");

	internal static string SynchronizationLockException_MisMatchedUpgrade => GetResourceString("SynchronizationLockException_MisMatchedUpgrade");

	internal static string SynchronizationLockException_MisMatchedRead => GetResourceString("SynchronizationLockException_MisMatchedRead");

	internal static string SynchronizationLockException_IncorrectDispose => GetResourceString("SynchronizationLockException_IncorrectDispose");

	internal static string LockRecursionException_UpgradeAfterReadNotAllowed => GetResourceString("LockRecursionException_UpgradeAfterReadNotAllowed");

	internal static string LockRecursionException_UpgradeAfterWriteNotAllowed => GetResourceString("LockRecursionException_UpgradeAfterWriteNotAllowed");

	internal static string SynchronizationLockException_MisMatchedWrite => GetResourceString("SynchronizationLockException_MisMatchedWrite");

	internal static string NotSupported_SignatureType => GetResourceString("NotSupported_SignatureType");

	internal static string HashCode_HashCodeNotSupported => GetResourceString("HashCode_HashCodeNotSupported");

	internal static string HashCode_EqualityNotSupported => GetResourceString("HashCode_EqualityNotSupported");

	internal static string Arg_TypeNotSupported => GetResourceString("Arg_TypeNotSupported");

	internal static string IO_InvalidReadLength => GetResourceString("IO_InvalidReadLength");

	internal static string Arg_BasePathNotFullyQualified => GetResourceString("Arg_BasePathNotFullyQualified");

	internal static string Arg_ElementsInSourceIsGreaterThanDestination => GetResourceString("Arg_ElementsInSourceIsGreaterThanDestination");

	internal static string Arg_NullArgumentNullRef => GetResourceString("Arg_NullArgumentNullRef");

	internal static string Argument_OverlapAlignmentMismatch => GetResourceString("Argument_OverlapAlignmentMismatch");

	internal static string Arg_InsufficientNumberOfElements => GetResourceString("Arg_InsufficientNumberOfElements");

	internal static string Arg_MustBeNullTerminatedString => GetResourceString("Arg_MustBeNullTerminatedString");

	internal static string ArgumentOutOfRange_Week_ISO => GetResourceString("ArgumentOutOfRange_Week_ISO");

	internal static string Argument_BadPInvokeMethod => GetResourceString("Argument_BadPInvokeMethod");

	internal static string Argument_BadPInvokeOnInterface => GetResourceString("Argument_BadPInvokeOnInterface");

	internal static string Argument_MethodRedefined => GetResourceString("Argument_MethodRedefined");

	internal static string Argument_CannotExtractScalar => GetResourceString("Argument_CannotExtractScalar");

	internal static string Argument_CannotParsePrecision => GetResourceString("Argument_CannotParsePrecision");

	internal static string Argument_GWithPrecisionNotSupported => GetResourceString("Argument_GWithPrecisionNotSupported");

	internal static string Argument_PrecisionTooLarge => GetResourceString("Argument_PrecisionTooLarge");

	internal static string AssemblyDependencyResolver_FailedToLoadHostpolicy => GetResourceString("AssemblyDependencyResolver_FailedToLoadHostpolicy");

	internal static string AssemblyDependencyResolver_FailedToResolveDependencies => GetResourceString("AssemblyDependencyResolver_FailedToResolveDependencies");

	internal static string Arg_EnumNotCloneable => GetResourceString("Arg_EnumNotCloneable");

	internal static string InvalidOp_InvalidNewEnumVariant => GetResourceString("InvalidOp_InvalidNewEnumVariant");

	internal static string Argument_StructArrayTooLarge => GetResourceString("Argument_StructArrayTooLarge");

	internal static string IndexOutOfRange_ArrayWithOffset => GetResourceString("IndexOutOfRange_ArrayWithOffset");

	internal static string Serialization_DangerousDeserialization => GetResourceString("Serialization_DangerousDeserialization");

	internal static string Serialization_DangerousDeserialization_Switch => GetResourceString("Serialization_DangerousDeserialization_Switch");

	internal static string Argument_InvalidStartupHookSimpleAssemblyName => GetResourceString("Argument_InvalidStartupHookSimpleAssemblyName");

	internal static string Argument_StartupHookAssemblyLoadFailed => GetResourceString("Argument_StartupHookAssemblyLoadFailed");

	internal static string InvalidOperation_NonStaticComRegFunction => GetResourceString("InvalidOperation_NonStaticComRegFunction");

	internal static string InvalidOperation_NonStaticComUnRegFunction => GetResourceString("InvalidOperation_NonStaticComUnRegFunction");

	internal static string InvalidOperation_InvalidComRegFunctionSig => GetResourceString("InvalidOperation_InvalidComRegFunctionSig");

	internal static string InvalidOperation_InvalidComUnRegFunctionSig => GetResourceString("InvalidOperation_InvalidComUnRegFunctionSig");

	internal static string InvalidOperation_MultipleComRegFunctions => GetResourceString("InvalidOperation_MultipleComRegFunctions");

	internal static string InvalidOperation_MultipleComUnRegFunctions => GetResourceString("InvalidOperation_MultipleComUnRegFunctions");

	internal static string InvalidOperation_ResetGlobalComWrappersInstance => GetResourceString("InvalidOperation_ResetGlobalComWrappersInstance");

	internal static string InvalidOperation_SuppliedInnerMustBeMarkedAggregation => GetResourceString("InvalidOperation_SuppliedInnerMustBeMarkedAggregation");

	internal static string Argument_SpansMustHaveSameLength => GetResourceString("Argument_SpansMustHaveSameLength");

	internal static string NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed => GetResourceString("NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed");

	internal static string GenericInvalidData => GetResourceString("GenericInvalidData");

	internal static string Argument_ResourceScopeWrongDirection => GetResourceString("Argument_ResourceScopeWrongDirection");

	internal static string ArgumentNull_TypeRequiredByResourceScope => GetResourceString("ArgumentNull_TypeRequiredByResourceScope");

	internal static string Argument_BadResourceScopeTypeBits => GetResourceString("Argument_BadResourceScopeTypeBits");

	internal static string Argument_BadResourceScopeVisibilityBits => GetResourceString("Argument_BadResourceScopeVisibilityBits");

	internal static string net_emptystringcall => GetResourceString("net_emptystringcall");

	internal static string Argument_EmptyApplicationName => GetResourceString("Argument_EmptyApplicationName");

	internal static string Argument_FrameworkNameInvalid => GetResourceString("Argument_FrameworkNameInvalid");

	internal static string Argument_FrameworkNameInvalidVersion => GetResourceString("Argument_FrameworkNameInvalidVersion");

	internal static string Argument_FrameworkNameMissingVersion => GetResourceString("Argument_FrameworkNameMissingVersion");

	internal static string Argument_FrameworkNameTooShort => GetResourceString("Argument_FrameworkNameTooShort");

	internal static string Arg_SwitchExpressionException => GetResourceString("Arg_SwitchExpressionException");

	internal static string Arg_ContextMarshalException => GetResourceString("Arg_ContextMarshalException");

	internal static string Arg_AppDomainUnloadedException => GetResourceString("Arg_AppDomainUnloadedException");

	internal static string SwitchExpressionException_UnmatchedValue => GetResourceString("SwitchExpressionException_UnmatchedValue");

	internal static string Encoding_UTF7_Disabled => GetResourceString("Encoding_UTF7_Disabled");

	internal static string IDynamicInterfaceCastable_DoesNotImplementRequested => GetResourceString("IDynamicInterfaceCastable_DoesNotImplementRequested");

	internal static string IDynamicInterfaceCastable_MissingImplementationAttribute => GetResourceString("IDynamicInterfaceCastable_MissingImplementationAttribute");

	internal static string IDynamicInterfaceCastable_NotInterface => GetResourceString("IDynamicInterfaceCastable_NotInterface");

	internal static string Arg_MustBeHalf => GetResourceString("Arg_MustBeHalf");

	internal static string Arg_MustBeRune => GetResourceString("Arg_MustBeRune");

	internal static string BinaryFormatter_SerializationDisallowed => GetResourceString("BinaryFormatter_SerializationDisallowed");

	internal static string NotSupported_CodeBase => GetResourceString("NotSupported_CodeBase");

	internal static string Activator_CannotCreateInstance => GetResourceString("Activator_CannotCreateInstance");

	internal static string Argv_IncludeDoubleQuote => GetResourceString("Argv_IncludeDoubleQuote");

	internal static string ResourceManager_ReflectionNotAllowed => GetResourceString("ResourceManager_ReflectionNotAllowed");

	internal static string NotSupported_COM => GetResourceString("NotSupported_COM");

	internal static string InvalidOperation_EmptyQueue => GetResourceString("InvalidOperation_EmptyQueue");

	internal static string Arg_FileIsDirectory_Name => GetResourceString("Arg_FileIsDirectory_Name");

	internal static string Arg_InvalidFileAttrs => GetResourceString("Arg_InvalidFileAttrs");

	internal static string Arg_Path2IsRooted => GetResourceString("Arg_Path2IsRooted");

	internal static string Arg_PathIsVolume => GetResourceString("Arg_PathIsVolume");

	internal static string Argument_InvalidSubPath => GetResourceString("Argument_InvalidSubPath");

	internal static string IO_SourceDestMustBeDifferent => GetResourceString("IO_SourceDestMustBeDifferent");

	internal static string IO_SourceDestMustHaveSameRoot => GetResourceString("IO_SourceDestMustHaveSameRoot");

	internal static string PlatformNotSupported_FileEncryption => GetResourceString("PlatformNotSupported_FileEncryption");

	internal static string Arg_MemberInfoNotFound => GetResourceString("Arg_MemberInfoNotFound");

	internal static string NullabilityInfoContext_NotSupported => GetResourceString("NullabilityInfoContext_NotSupported");

	private static string InternalGetResourceString(string key)
	{
		if (key.Length == 0)
		{
			return key;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(_lock, ref lockTaken);
			if (_currentlyLoading != null && _currentlyLoading.Count > 0 && _currentlyLoading.LastIndexOf(key) != -1)
			{
				if (_infinitelyRecursingCount > 0)
				{
					return key;
				}
				_infinitelyRecursingCount++;
				string message = $"Infinite recursion during resource lookup within {"System.Private.CoreLib"}.  This may be a bug in {"System.Private.CoreLib"}, or potentially in certain extensibility points such as assembly resolve events or CultureInfo names.  Resource name: {key}";
				Environment.FailFast(message);
			}
			if (_currentlyLoading == null)
			{
				_currentlyLoading = new List<string>();
			}
			if (!_resourceManagerInited)
			{
				RuntimeHelpers.RunClassConstructor(typeof(ResourceManager).TypeHandle);
				RuntimeHelpers.RunClassConstructor(typeof(ResourceReader).TypeHandle);
				RuntimeHelpers.RunClassConstructor(typeof(RuntimeResourceSet).TypeHandle);
				RuntimeHelpers.RunClassConstructor(typeof(BinaryReader).TypeHandle);
				_resourceManagerInited = true;
			}
			_currentlyLoading.Add(key);
			string @string = ResourceManager.GetString(key, null);
			_currentlyLoading.RemoveAt(_currentlyLoading.Count - 1);
			return @string ?? key;
		}
		catch
		{
			if (lockTaken)
			{
				s_resourceManager = null;
				_currentlyLoading = null;
			}
			throw;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(_lock);
			}
		}
	}

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
			result = InternalGetResourceString(resourceKey);
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
}
