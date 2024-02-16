using System.Resources;
using FxResources.System.Data.Common;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ADP_CollectionIndexString => GetResourceString("ADP_CollectionIndexString");

	internal static string ADP_CollectionInvalidType => GetResourceString("ADP_CollectionInvalidType");

	internal static string ADP_CollectionIsNotParent => GetResourceString("ADP_CollectionIsNotParent");

	internal static string ADP_CollectionNullValue => GetResourceString("ADP_CollectionNullValue");

	internal static string ADP_CollectionRemoveInvalidObject => GetResourceString("ADP_CollectionRemoveInvalidObject");

	internal static string ADP_CollectionUniqueValue => GetResourceString("ADP_CollectionUniqueValue");

	internal static string ADP_ConnectionStateMsg_Closed => GetResourceString("ADP_ConnectionStateMsg_Closed");

	internal static string ADP_ConnectionStateMsg_Connecting => GetResourceString("ADP_ConnectionStateMsg_Connecting");

	internal static string ADP_ConnectionStateMsg_Open => GetResourceString("ADP_ConnectionStateMsg_Open");

	internal static string ADP_ConnectionStateMsg_OpenExecuting => GetResourceString("ADP_ConnectionStateMsg_OpenExecuting");

	internal static string ADP_ConnectionStateMsg_OpenFetching => GetResourceString("ADP_ConnectionStateMsg_OpenFetching");

	internal static string ADP_ConnectionStateMsg => GetResourceString("ADP_ConnectionStateMsg");

	internal static string ADP_ConnectionStringSyntax => GetResourceString("ADP_ConnectionStringSyntax");

	internal static string ADP_DataReaderClosed => GetResourceString("ADP_DataReaderClosed");

	internal static string ADP_EmptyString => GetResourceString("ADP_EmptyString");

	internal static string ADP_InvalidEnumerationValue => GetResourceString("ADP_InvalidEnumerationValue");

	internal static string ADP_InvalidKey => GetResourceString("ADP_InvalidKey");

	internal static string ADP_InvalidValue => GetResourceString("ADP_InvalidValue");

	internal static string Xml_SimpleTypeNotSupported => GetResourceString("Xml_SimpleTypeNotSupported");

	internal static string Xml_MissingAttribute => GetResourceString("Xml_MissingAttribute");

	internal static string Xml_ValueOutOfRange => GetResourceString("Xml_ValueOutOfRange");

	internal static string Xml_AttributeValues => GetResourceString("Xml_AttributeValues");

	internal static string Xml_RelationParentNameMissing => GetResourceString("Xml_RelationParentNameMissing");

	internal static string Xml_RelationChildNameMissing => GetResourceString("Xml_RelationChildNameMissing");

	internal static string Xml_RelationTableKeyMissing => GetResourceString("Xml_RelationTableKeyMissing");

	internal static string Xml_RelationChildKeyMissing => GetResourceString("Xml_RelationChildKeyMissing");

	internal static string Xml_UndefinedDatatype => GetResourceString("Xml_UndefinedDatatype");

	internal static string Xml_DatatypeNotDefined => GetResourceString("Xml_DatatypeNotDefined");

	internal static string Xml_InvalidField => GetResourceString("Xml_InvalidField");

	internal static string Xml_InvalidSelector => GetResourceString("Xml_InvalidSelector");

	internal static string Xml_InvalidKey => GetResourceString("Xml_InvalidKey");

	internal static string Xml_DuplicateConstraint => GetResourceString("Xml_DuplicateConstraint");

	internal static string Xml_CannotConvert => GetResourceString("Xml_CannotConvert");

	internal static string Xml_MissingRefer => GetResourceString("Xml_MissingRefer");

	internal static string Xml_MismatchKeyLength => GetResourceString("Xml_MismatchKeyLength");

	internal static string Xml_CircularComplexType => GetResourceString("Xml_CircularComplexType");

	internal static string Xml_CannotInstantiateAbstract => GetResourceString("Xml_CannotInstantiateAbstract");

	internal static string Xml_MultipleTargetConverterError => GetResourceString("Xml_MultipleTargetConverterError");

	internal static string Xml_MultipleTargetConverterEmpty => GetResourceString("Xml_MultipleTargetConverterEmpty");

	internal static string Xml_MergeDuplicateDeclaration => GetResourceString("Xml_MergeDuplicateDeclaration");

	internal static string Xml_MissingSQL => GetResourceString("Xml_MissingSQL");

	internal static string Xml_ColumnConflict => GetResourceString("Xml_ColumnConflict");

	internal static string Xml_InvalidPrefix_SpecialCharacters => GetResourceString("Xml_InvalidPrefix_SpecialCharacters");

	internal static string Xml_NestedCircular => GetResourceString("Xml_NestedCircular");

	internal static string Xml_FoundEntity => GetResourceString("Xml_FoundEntity");

	internal static string Xml_PolymorphismNotSupported => GetResourceString("Xml_PolymorphismNotSupported");

	internal static string Xml_CanNotDeserializeObjectType => GetResourceString("Xml_CanNotDeserializeObjectType");

	internal static string Xml_DataTableInferenceNotSupported => GetResourceString("Xml_DataTableInferenceNotSupported");

	internal static string Xml_MultipleParentRows => GetResourceString("Xml_MultipleParentRows");

	internal static string Xml_IsDataSetAttributeMissingInSchema => GetResourceString("Xml_IsDataSetAttributeMissingInSchema");

	internal static string Xml_TooManyIsDataSetAttributesInSchema => GetResourceString("Xml_TooManyIsDataSetAttributesInSchema");

	internal static string Xml_DynamicWithoutXmlSerializable => GetResourceString("Xml_DynamicWithoutXmlSerializable");

	internal static string Expr_NYI => GetResourceString("Expr_NYI");

	internal static string Expr_MissingOperand => GetResourceString("Expr_MissingOperand");

	internal static string Expr_TypeMismatch => GetResourceString("Expr_TypeMismatch");

	internal static string Expr_ExpressionTooComplex => GetResourceString("Expr_ExpressionTooComplex");

	internal static string Expr_UnboundName => GetResourceString("Expr_UnboundName");

	internal static string Expr_InvalidString => GetResourceString("Expr_InvalidString");

	internal static string Expr_UndefinedFunction => GetResourceString("Expr_UndefinedFunction");

	internal static string Expr_Syntax => GetResourceString("Expr_Syntax");

	internal static string Expr_FunctionArgumentCount => GetResourceString("Expr_FunctionArgumentCount");

	internal static string Expr_MissingRightParen => GetResourceString("Expr_MissingRightParen");

	internal static string Expr_UnknownToken => GetResourceString("Expr_UnknownToken");

	internal static string Expr_UnknownToken1 => GetResourceString("Expr_UnknownToken1");

	internal static string Expr_DatatypeConvertion => GetResourceString("Expr_DatatypeConvertion");

	internal static string Expr_DatavalueConvertion => GetResourceString("Expr_DatavalueConvertion");

	internal static string Expr_InvalidName => GetResourceString("Expr_InvalidName");

	internal static string Expr_InvalidDate => GetResourceString("Expr_InvalidDate");

	internal static string Expr_NonConstantArgument => GetResourceString("Expr_NonConstantArgument");

	internal static string Expr_InvalidPattern => GetResourceString("Expr_InvalidPattern");

	internal static string Expr_InWithoutParentheses => GetResourceString("Expr_InWithoutParentheses");

	internal static string Expr_ArgumentType => GetResourceString("Expr_ArgumentType");

	internal static string Expr_ArgumentTypeInteger => GetResourceString("Expr_ArgumentTypeInteger");

	internal static string Expr_TypeMismatchInBinop => GetResourceString("Expr_TypeMismatchInBinop");

	internal static string Expr_AmbiguousBinop => GetResourceString("Expr_AmbiguousBinop");

	internal static string Expr_InWithoutList => GetResourceString("Expr_InWithoutList");

	internal static string Expr_UnsupportedOperator => GetResourceString("Expr_UnsupportedOperator");

	internal static string Expr_InvalidNameBracketing => GetResourceString("Expr_InvalidNameBracketing");

	internal static string Expr_MissingOperandBefore => GetResourceString("Expr_MissingOperandBefore");

	internal static string Expr_TooManyRightParentheses => GetResourceString("Expr_TooManyRightParentheses");

	internal static string Expr_UnresolvedRelation => GetResourceString("Expr_UnresolvedRelation");

	internal static string Expr_AggregateArgument => GetResourceString("Expr_AggregateArgument");

	internal static string Expr_AggregateUnbound => GetResourceString("Expr_AggregateUnbound");

	internal static string Expr_EvalNoContext => GetResourceString("Expr_EvalNoContext");

	internal static string Expr_ExpressionUnbound => GetResourceString("Expr_ExpressionUnbound");

	internal static string Expr_ComputeNotAggregate => GetResourceString("Expr_ComputeNotAggregate");

	internal static string Expr_FilterConvertion => GetResourceString("Expr_FilterConvertion");

	internal static string Expr_InvalidType => GetResourceString("Expr_InvalidType");

	internal static string Expr_LookupArgument => GetResourceString("Expr_LookupArgument");

	internal static string Expr_InvokeArgument => GetResourceString("Expr_InvokeArgument");

	internal static string Expr_ArgumentOutofRange => GetResourceString("Expr_ArgumentOutofRange");

	internal static string Expr_IsSyntax => GetResourceString("Expr_IsSyntax");

	internal static string Expr_Overflow => GetResourceString("Expr_Overflow");

	internal static string Expr_BindFailure => GetResourceString("Expr_BindFailure");

	internal static string Expr_InvalidHoursArgument => GetResourceString("Expr_InvalidHoursArgument");

	internal static string Expr_InvalidMinutesArgument => GetResourceString("Expr_InvalidMinutesArgument");

	internal static string Expr_InvalidTimeZoneRange => GetResourceString("Expr_InvalidTimeZoneRange");

	internal static string Expr_MismatchKindandTimeSpan => GetResourceString("Expr_MismatchKindandTimeSpan");

	internal static string Expr_UnsupportedType => GetResourceString("Expr_UnsupportedType");

	internal static string Data_EnforceConstraints => GetResourceString("Data_EnforceConstraints");

	internal static string Data_CannotModifyCollection => GetResourceString("Data_CannotModifyCollection");

	internal static string Data_CaseInsensitiveNameConflict => GetResourceString("Data_CaseInsensitiveNameConflict");

	internal static string Data_NamespaceNameConflict => GetResourceString("Data_NamespaceNameConflict");

	internal static string Data_InvalidOffsetLength => GetResourceString("Data_InvalidOffsetLength");

	internal static string Data_ArgumentOutOfRange => GetResourceString("Data_ArgumentOutOfRange");

	internal static string Data_ArgumentNull => GetResourceString("Data_ArgumentNull");

	internal static string Data_ArgumentContainsNull => GetResourceString("Data_ArgumentContainsNull");

	internal static string Data_TypeNotAllowed => GetResourceString("Data_TypeNotAllowed");

	internal static string DataColumns_OutOfRange => GetResourceString("DataColumns_OutOfRange");

	internal static string DataColumns_Add1 => GetResourceString("DataColumns_Add1");

	internal static string DataColumns_Add2 => GetResourceString("DataColumns_Add2");

	internal static string DataColumns_Add3 => GetResourceString("DataColumns_Add3");

	internal static string DataColumns_Add4 => GetResourceString("DataColumns_Add4");

	internal static string DataColumns_AddDuplicate => GetResourceString("DataColumns_AddDuplicate");

	internal static string DataColumns_AddDuplicate2 => GetResourceString("DataColumns_AddDuplicate2");

	internal static string DataColumns_AddDuplicate3 => GetResourceString("DataColumns_AddDuplicate3");

	internal static string DataColumns_Remove => GetResourceString("DataColumns_Remove");

	internal static string DataColumns_RemovePrimaryKey => GetResourceString("DataColumns_RemovePrimaryKey");

	internal static string DataColumns_RemoveChildKey => GetResourceString("DataColumns_RemoveChildKey");

	internal static string DataColumns_RemoveConstraint => GetResourceString("DataColumns_RemoveConstraint");

	internal static string DataColumn_AutoIncrementAndExpression => GetResourceString("DataColumn_AutoIncrementAndExpression");

	internal static string DataColumn_AutoIncrementAndDefaultValue => GetResourceString("DataColumn_AutoIncrementAndDefaultValue");

	internal static string DataColumn_DefaultValueAndAutoIncrement => GetResourceString("DataColumn_DefaultValueAndAutoIncrement");

	internal static string DataColumn_AutoIncrementSeed => GetResourceString("DataColumn_AutoIncrementSeed");

	internal static string DataColumn_NameRequired => GetResourceString("DataColumn_NameRequired");

	internal static string DataColumn_ChangeDataType => GetResourceString("DataColumn_ChangeDataType");

	internal static string DataColumn_NullDataType => GetResourceString("DataColumn_NullDataType");

	internal static string DataColumn_DefaultValueDataType => GetResourceString("DataColumn_DefaultValueDataType");

	internal static string DataColumn_DefaultValueDataType1 => GetResourceString("DataColumn_DefaultValueDataType1");

	internal static string DataColumn_DefaultValueColumnDataType => GetResourceString("DataColumn_DefaultValueColumnDataType");

	internal static string DataColumn_ReadOnlyAndExpression => GetResourceString("DataColumn_ReadOnlyAndExpression");

	internal static string DataColumn_UniqueAndExpression => GetResourceString("DataColumn_UniqueAndExpression");

	internal static string DataColumn_ExpressionAndUnique => GetResourceString("DataColumn_ExpressionAndUnique");

	internal static string DataColumn_ExpressionAndReadOnly => GetResourceString("DataColumn_ExpressionAndReadOnly");

	internal static string DataColumn_ExpressionAndConstraint => GetResourceString("DataColumn_ExpressionAndConstraint");

	internal static string DataColumn_ExpressionInConstraint => GetResourceString("DataColumn_ExpressionInConstraint");

	internal static string DataColumn_ExpressionCircular => GetResourceString("DataColumn_ExpressionCircular");

	internal static string DataColumn_NullKeyValues => GetResourceString("DataColumn_NullKeyValues");

	internal static string DataColumn_NullValues => GetResourceString("DataColumn_NullValues");

	internal static string DataColumn_ReadOnly => GetResourceString("DataColumn_ReadOnly");

	internal static string DataColumn_NonUniqueValues => GetResourceString("DataColumn_NonUniqueValues");

	internal static string DataColumn_NotInTheTable => GetResourceString("DataColumn_NotInTheTable");

	internal static string DataColumn_NotInAnyTable => GetResourceString("DataColumn_NotInAnyTable");

	internal static string DataColumn_SetFailed => GetResourceString("DataColumn_SetFailed");

	internal static string DataColumn_CannotSetToNull => GetResourceString("DataColumn_CannotSetToNull");

	internal static string DataColumn_LongerThanMaxLength => GetResourceString("DataColumn_LongerThanMaxLength");

	internal static string DataColumn_HasToBeStringType => GetResourceString("DataColumn_HasToBeStringType");

	internal static string DataColumn_CannotSetMaxLength => GetResourceString("DataColumn_CannotSetMaxLength");

	internal static string DataColumn_CannotSetMaxLength2 => GetResourceString("DataColumn_CannotSetMaxLength2");

	internal static string DataColumn_CannotSimpleContentType => GetResourceString("DataColumn_CannotSimpleContentType");

	internal static string DataColumn_CannotSimpleContent => GetResourceString("DataColumn_CannotSimpleContent");

	internal static string DataColumn_ExceedMaxLength => GetResourceString("DataColumn_ExceedMaxLength");

	internal static string DataColumn_NotAllowDBNull => GetResourceString("DataColumn_NotAllowDBNull");

	internal static string DataColumn_CannotChangeNamespace => GetResourceString("DataColumn_CannotChangeNamespace");

	internal static string DataColumn_AutoIncrementCannotSetIfHasData => GetResourceString("DataColumn_AutoIncrementCannotSetIfHasData");

	internal static string DataColumn_NotInTheUnderlyingTable => GetResourceString("DataColumn_NotInTheUnderlyingTable");

	internal static string DataColumn_InvalidDataColumnMapping => GetResourceString("DataColumn_InvalidDataColumnMapping");

	internal static string DataColumn_CannotSetDateTimeModeForNonDateTimeColumns => GetResourceString("DataColumn_CannotSetDateTimeModeForNonDateTimeColumns");

	internal static string DataColumn_DateTimeMode => GetResourceString("DataColumn_DateTimeMode");

	internal static string DataColumn_INullableUDTwithoutStaticNull => GetResourceString("DataColumn_INullableUDTwithoutStaticNull");

	internal static string DataColumn_UDTImplementsIChangeTrackingButnotIRevertible => GetResourceString("DataColumn_UDTImplementsIChangeTrackingButnotIRevertible");

	internal static string DataColumn_SetAddedAndModifiedCalledOnNonUnchanged => GetResourceString("DataColumn_SetAddedAndModifiedCalledOnNonUnchanged");

	internal static string DataColumn_OrdinalExceedMaximun => GetResourceString("DataColumn_OrdinalExceedMaximun");

	internal static string DataColumn_NullableTypesNotSupported => GetResourceString("DataColumn_NullableTypesNotSupported");

	internal static string DataConstraint_NoName => GetResourceString("DataConstraint_NoName");

	internal static string DataConstraint_Violation => GetResourceString("DataConstraint_Violation");

	internal static string DataConstraint_ViolationValue => GetResourceString("DataConstraint_ViolationValue");

	internal static string DataConstraint_NotInTheTable => GetResourceString("DataConstraint_NotInTheTable");

	internal static string DataConstraint_OutOfRange => GetResourceString("DataConstraint_OutOfRange");

	internal static string DataConstraint_Duplicate => GetResourceString("DataConstraint_Duplicate");

	internal static string DataConstraint_DuplicateName => GetResourceString("DataConstraint_DuplicateName");

	internal static string DataConstraint_UniqueViolation => GetResourceString("DataConstraint_UniqueViolation");

	internal static string DataConstraint_ForeignTable => GetResourceString("DataConstraint_ForeignTable");

	internal static string DataConstraint_ParentValues => GetResourceString("DataConstraint_ParentValues");

	internal static string DataConstraint_AddFailed => GetResourceString("DataConstraint_AddFailed");

	internal static string DataConstraint_RemoveFailed => GetResourceString("DataConstraint_RemoveFailed");

	internal static string DataConstraint_NeededForForeignKeyConstraint => GetResourceString("DataConstraint_NeededForForeignKeyConstraint");

	internal static string DataConstraint_CascadeDelete => GetResourceString("DataConstraint_CascadeDelete");

	internal static string DataConstraint_CascadeUpdate => GetResourceString("DataConstraint_CascadeUpdate");

	internal static string DataConstraint_ClearParentTable => GetResourceString("DataConstraint_ClearParentTable");

	internal static string DataConstraint_ForeignKeyViolation => GetResourceString("DataConstraint_ForeignKeyViolation");

	internal static string DataConstraint_BadObjectPropertyAccess => GetResourceString("DataConstraint_BadObjectPropertyAccess");

	internal static string DataConstraint_RemoveParentRow => GetResourceString("DataConstraint_RemoveParentRow");

	internal static string DataConstraint_AddPrimaryKeyConstraint => GetResourceString("DataConstraint_AddPrimaryKeyConstraint");

	internal static string DataConstraint_CantAddConstraintToMultipleNestedTable => GetResourceString("DataConstraint_CantAddConstraintToMultipleNestedTable");

	internal static string DataKey_TableMismatch => GetResourceString("DataKey_TableMismatch");

	internal static string DataKey_NoColumns => GetResourceString("DataKey_NoColumns");

	internal static string DataKey_TooManyColumns => GetResourceString("DataKey_TooManyColumns");

	internal static string DataKey_DuplicateColumns => GetResourceString("DataKey_DuplicateColumns");

	internal static string DataKey_RemovePrimaryKey => GetResourceString("DataKey_RemovePrimaryKey");

	internal static string DataKey_RemovePrimaryKey1 => GetResourceString("DataKey_RemovePrimaryKey1");

	internal static string DataRelation_ColumnsTypeMismatch => GetResourceString("DataRelation_ColumnsTypeMismatch");

	internal static string DataRelation_KeyColumnsIdentical => GetResourceString("DataRelation_KeyColumnsIdentical");

	internal static string DataRelation_KeyLengthMismatch => GetResourceString("DataRelation_KeyLengthMismatch");

	internal static string DataRelation_KeyZeroLength => GetResourceString("DataRelation_KeyZeroLength");

	internal static string DataRelation_ForeignRow => GetResourceString("DataRelation_ForeignRow");

	internal static string DataRelation_NoName => GetResourceString("DataRelation_NoName");

	internal static string DataRelation_ForeignTable => GetResourceString("DataRelation_ForeignTable");

	internal static string DataRelation_ForeignDataSet => GetResourceString("DataRelation_ForeignDataSet");

	internal static string DataRelation_GetParentRowTableMismatch => GetResourceString("DataRelation_GetParentRowTableMismatch");

	internal static string DataRelation_SetParentRowTableMismatch => GetResourceString("DataRelation_SetParentRowTableMismatch");

	internal static string DataRelation_DataSetMismatch => GetResourceString("DataRelation_DataSetMismatch");

	internal static string DataRelation_TablesInDifferentSets => GetResourceString("DataRelation_TablesInDifferentSets");

	internal static string DataRelation_AlreadyExists => GetResourceString("DataRelation_AlreadyExists");

	internal static string DataRelation_DoesNotExist => GetResourceString("DataRelation_DoesNotExist");

	internal static string DataRelation_AlreadyInOtherDataSet => GetResourceString("DataRelation_AlreadyInOtherDataSet");

	internal static string DataRelation_AlreadyInTheDataSet => GetResourceString("DataRelation_AlreadyInTheDataSet");

	internal static string DataRelation_DuplicateName => GetResourceString("DataRelation_DuplicateName");

	internal static string DataRelation_NotInTheDataSet => GetResourceString("DataRelation_NotInTheDataSet");

	internal static string DataRelation_OutOfRange => GetResourceString("DataRelation_OutOfRange");

	internal static string DataRelation_TableNull => GetResourceString("DataRelation_TableNull");

	internal static string DataRelation_TableWasRemoved => GetResourceString("DataRelation_TableWasRemoved");

	internal static string DataRelation_ChildTableMismatch => GetResourceString("DataRelation_ChildTableMismatch");

	internal static string DataRelation_ParentTableMismatch => GetResourceString("DataRelation_ParentTableMismatch");

	internal static string DataRelation_RelationNestedReadOnly => GetResourceString("DataRelation_RelationNestedReadOnly");

	internal static string DataRelation_TableCantBeNestedInTwoTables => GetResourceString("DataRelation_TableCantBeNestedInTwoTables");

	internal static string DataRelation_LoopInNestedRelations => GetResourceString("DataRelation_LoopInNestedRelations");

	internal static string DataRelation_CaseLocaleMismatch => GetResourceString("DataRelation_CaseLocaleMismatch");

	internal static string DataRelation_ParentOrChildColumnsDoNotHaveDataSet => GetResourceString("DataRelation_ParentOrChildColumnsDoNotHaveDataSet");

	internal static string DataRelation_InValidNestedRelation => GetResourceString("DataRelation_InValidNestedRelation");

	internal static string DataRelation_InValidNamespaceInNestedRelation => GetResourceString("DataRelation_InValidNamespaceInNestedRelation");

	internal static string DataRow_NotInTheDataSet => GetResourceString("DataRow_NotInTheDataSet");

	internal static string DataRow_NotInTheTable => GetResourceString("DataRow_NotInTheTable");

	internal static string DataRow_ParentRowNotInTheDataSet => GetResourceString("DataRow_ParentRowNotInTheDataSet");

	internal static string DataRow_EditInRowChanging => GetResourceString("DataRow_EditInRowChanging");

	internal static string DataRow_EndEditInRowChanging => GetResourceString("DataRow_EndEditInRowChanging");

	internal static string DataRow_BeginEditInRowChanging => GetResourceString("DataRow_BeginEditInRowChanging");

	internal static string DataRow_CancelEditInRowChanging => GetResourceString("DataRow_CancelEditInRowChanging");

	internal static string DataRow_DeleteInRowDeleting => GetResourceString("DataRow_DeleteInRowDeleting");

	internal static string DataRow_ValuesArrayLength => GetResourceString("DataRow_ValuesArrayLength");

	internal static string DataRow_NoCurrentData => GetResourceString("DataRow_NoCurrentData");

	internal static string DataRow_NoOriginalData => GetResourceString("DataRow_NoOriginalData");

	internal static string DataRow_NoProposedData => GetResourceString("DataRow_NoProposedData");

	internal static string DataRow_RemovedFromTheTable => GetResourceString("DataRow_RemovedFromTheTable");

	internal static string DataRow_DeletedRowInaccessible => GetResourceString("DataRow_DeletedRowInaccessible");

	internal static string DataRow_InvalidVersion => GetResourceString("DataRow_InvalidVersion");

	internal static string DataRow_OutOfRange => GetResourceString("DataRow_OutOfRange");

	internal static string DataRow_RowInsertOutOfRange => GetResourceString("DataRow_RowInsertOutOfRange");

	internal static string DataRow_RowInsertMissing => GetResourceString("DataRow_RowInsertMissing");

	internal static string DataRow_RowOutOfRange => GetResourceString("DataRow_RowOutOfRange");

	internal static string DataRow_AlreadyInOtherCollection => GetResourceString("DataRow_AlreadyInOtherCollection");

	internal static string DataRow_AlreadyInTheCollection => GetResourceString("DataRow_AlreadyInTheCollection");

	internal static string DataRow_AlreadyDeleted => GetResourceString("DataRow_AlreadyDeleted");

	internal static string DataRow_Empty => GetResourceString("DataRow_Empty");

	internal static string DataRow_AlreadyRemoved => GetResourceString("DataRow_AlreadyRemoved");

	internal static string DataRow_MultipleParents => GetResourceString("DataRow_MultipleParents");

	internal static string DataRow_InvalidRowBitPattern => GetResourceString("DataRow_InvalidRowBitPattern");

	internal static string DataSet_SetNameToEmpty => GetResourceString("DataSet_SetNameToEmpty");

	internal static string DataSet_SetDataSetNameConflicting => GetResourceString("DataSet_SetDataSetNameConflicting");

	internal static string DataSet_UnsupportedSchema => GetResourceString("DataSet_UnsupportedSchema");

	internal static string DataSet_CannotChangeCaseLocale => GetResourceString("DataSet_CannotChangeCaseLocale");

	internal static string DataSet_CannotChangeSchemaSerializationMode => GetResourceString("DataSet_CannotChangeSchemaSerializationMode");

	internal static string DataTable_ForeignPrimaryKey => GetResourceString("DataTable_ForeignPrimaryKey");

	internal static string DataTable_CannotAddToSimpleContent => GetResourceString("DataTable_CannotAddToSimpleContent");

	internal static string DataTable_NoName => GetResourceString("DataTable_NoName");

	internal static string DataTable_MultipleSimpleContentColumns => GetResourceString("DataTable_MultipleSimpleContentColumns");

	internal static string DataTable_MissingPrimaryKey => GetResourceString("DataTable_MissingPrimaryKey");

	internal static string DataTable_InvalidSortString => GetResourceString("DataTable_InvalidSortString");

	internal static string DataTable_CanNotSerializeDataTableHierarchy => GetResourceString("DataTable_CanNotSerializeDataTableHierarchy");

	internal static string DataTable_CanNotRemoteDataTable => GetResourceString("DataTable_CanNotRemoteDataTable");

	internal static string DataTable_CanNotSetRemotingFormat => GetResourceString("DataTable_CanNotSetRemotingFormat");

	internal static string DataTable_CanNotSerializeDataTableWithEmptyName => GetResourceString("DataTable_CanNotSerializeDataTableWithEmptyName");

	internal static string DataTable_DuplicateName => GetResourceString("DataTable_DuplicateName");

	internal static string DataTable_DuplicateName2 => GetResourceString("DataTable_DuplicateName2");

	internal static string DataTable_SelfnestedDatasetConflictingName => GetResourceString("DataTable_SelfnestedDatasetConflictingName");

	internal static string DataTable_DatasetConflictingName => GetResourceString("DataTable_DatasetConflictingName");

	internal static string DataTable_AlreadyInOtherDataSet => GetResourceString("DataTable_AlreadyInOtherDataSet");

	internal static string DataTable_AlreadyInTheDataSet => GetResourceString("DataTable_AlreadyInTheDataSet");

	internal static string DataTable_NotInTheDataSet => GetResourceString("DataTable_NotInTheDataSet");

	internal static string DataTable_OutOfRange => GetResourceString("DataTable_OutOfRange");

	internal static string DataTable_InRelation => GetResourceString("DataTable_InRelation");

	internal static string DataTable_InConstraint => GetResourceString("DataTable_InConstraint");

	internal static string DataTable_TableNotFound => GetResourceString("DataTable_TableNotFound");

	internal static string DataMerge_MissingDefinition => GetResourceString("DataMerge_MissingDefinition");

	internal static string DataMerge_MissingConstraint => GetResourceString("DataMerge_MissingConstraint");

	internal static string DataMerge_DataTypeMismatch => GetResourceString("DataMerge_DataTypeMismatch");

	internal static string DataMerge_PrimaryKeyMismatch => GetResourceString("DataMerge_PrimaryKeyMismatch");

	internal static string DataMerge_PrimaryKeyColumnsMismatch => GetResourceString("DataMerge_PrimaryKeyColumnsMismatch");

	internal static string DataMerge_ReltionKeyColumnsMismatch => GetResourceString("DataMerge_ReltionKeyColumnsMismatch");

	internal static string DataMerge_MissingColumnDefinition => GetResourceString("DataMerge_MissingColumnDefinition");

	internal static string DataIndex_RecordStateRange => GetResourceString("DataIndex_RecordStateRange");

	internal static string DataIndex_FindWithoutSortOrder => GetResourceString("DataIndex_FindWithoutSortOrder");

	internal static string DataIndex_KeyLength => GetResourceString("DataIndex_KeyLength");

	internal static string DataStorage_AggregateException => GetResourceString("DataStorage_AggregateException");

	internal static string DataStorage_InvalidStorageType => GetResourceString("DataStorage_InvalidStorageType");

	internal static string DataStorage_ProblematicChars => GetResourceString("DataStorage_ProblematicChars");

	internal static string DataStorage_SetInvalidDataType => GetResourceString("DataStorage_SetInvalidDataType");

	internal static string DataStorage_IComparableNotDefined => GetResourceString("DataStorage_IComparableNotDefined");

	internal static string DataView_SetFailed => GetResourceString("DataView_SetFailed");

	internal static string DataView_SetDataSetFailed => GetResourceString("DataView_SetDataSetFailed");

	internal static string DataView_SetRowStateFilter => GetResourceString("DataView_SetRowStateFilter");

	internal static string DataView_SetTable => GetResourceString("DataView_SetTable");

	internal static string DataView_CanNotSetDataSet => GetResourceString("DataView_CanNotSetDataSet");

	internal static string DataView_CanNotUseDataViewManager => GetResourceString("DataView_CanNotUseDataViewManager");

	internal static string DataView_CanNotSetTable => GetResourceString("DataView_CanNotSetTable");

	internal static string DataView_CanNotUse => GetResourceString("DataView_CanNotUse");

	internal static string DataView_CanNotBindTable => GetResourceString("DataView_CanNotBindTable");

	internal static string DataView_SetIListObject => GetResourceString("DataView_SetIListObject");

	internal static string DataView_AddNewNotAllowNull => GetResourceString("DataView_AddNewNotAllowNull");

	internal static string DataView_NotOpen => GetResourceString("DataView_NotOpen");

	internal static string DataView_CreateChildView => GetResourceString("DataView_CreateChildView");

	internal static string DataView_CanNotDelete => GetResourceString("DataView_CanNotDelete");

	internal static string DataView_CanNotEdit => GetResourceString("DataView_CanNotEdit");

	internal static string DataView_GetElementIndex => GetResourceString("DataView_GetElementIndex");

	internal static string DataView_AddExternalObject => GetResourceString("DataView_AddExternalObject");

	internal static string DataView_CanNotClear => GetResourceString("DataView_CanNotClear");

	internal static string DataView_InsertExternalObject => GetResourceString("DataView_InsertExternalObject");

	internal static string DataView_RemoveExternalObject => GetResourceString("DataView_RemoveExternalObject");

	internal static string DataROWView_PropertyNotFound => GetResourceString("DataROWView_PropertyNotFound");

	internal static string Range_Argument => GetResourceString("Range_Argument");

	internal static string Range_NullRange => GetResourceString("Range_NullRange");

	internal static string RecordManager_MinimumCapacity => GetResourceString("RecordManager_MinimumCapacity");

	internal static string SqlConvert_ConvertFailed => GetResourceString("SqlConvert_ConvertFailed");

	internal static string DataSet_DefaultDataException => GetResourceString("DataSet_DefaultDataException");

	internal static string DataSet_DefaultConstraintException => GetResourceString("DataSet_DefaultConstraintException");

	internal static string DataSet_DefaultDeletedRowInaccessibleException => GetResourceString("DataSet_DefaultDeletedRowInaccessibleException");

	internal static string DataSet_DefaultDuplicateNameException => GetResourceString("DataSet_DefaultDuplicateNameException");

	internal static string DataSet_DefaultInRowChangingEventException => GetResourceString("DataSet_DefaultInRowChangingEventException");

	internal static string DataSet_DefaultInvalidConstraintException => GetResourceString("DataSet_DefaultInvalidConstraintException");

	internal static string DataSet_DefaultMissingPrimaryKeyException => GetResourceString("DataSet_DefaultMissingPrimaryKeyException");

	internal static string DataSet_DefaultNoNullAllowedException => GetResourceString("DataSet_DefaultNoNullAllowedException");

	internal static string DataSet_DefaultReadOnlyException => GetResourceString("DataSet_DefaultReadOnlyException");

	internal static string DataSet_DefaultRowNotInTableException => GetResourceString("DataSet_DefaultRowNotInTableException");

	internal static string DataSet_DefaultVersionNotFoundException => GetResourceString("DataSet_DefaultVersionNotFoundException");

	internal static string Load_ReadOnlyDataModified => GetResourceString("Load_ReadOnlyDataModified");

	internal static string DataTableReader_InvalidDataTableReader => GetResourceString("DataTableReader_InvalidDataTableReader");

	internal static string DataTableReader_SchemaInvalidDataTableReader => GetResourceString("DataTableReader_SchemaInvalidDataTableReader");

	internal static string DataTableReader_CannotCreateDataReaderOnEmptyDataSet => GetResourceString("DataTableReader_CannotCreateDataReaderOnEmptyDataSet");

	internal static string DataTableReader_DataTableReaderArgumentIsEmpty => GetResourceString("DataTableReader_DataTableReaderArgumentIsEmpty");

	internal static string DataTableReader_ArgumentContainsNullValue => GetResourceString("DataTableReader_ArgumentContainsNullValue");

	internal static string DataTableReader_InvalidRowInDataTableReader => GetResourceString("DataTableReader_InvalidRowInDataTableReader");

	internal static string DataTableReader_DataTableCleared => GetResourceString("DataTableReader_DataTableCleared");

	internal static string RbTree_InvalidState => GetResourceString("RbTree_InvalidState");

	internal static string RbTree_EnumerationBroken => GetResourceString("RbTree_EnumerationBroken");

	internal static string NamedSimpleType_InvalidDuplicateNamedSimpleTypeDelaration => GetResourceString("NamedSimpleType_InvalidDuplicateNamedSimpleTypeDelaration");

	internal static string DataDom_Foliation => GetResourceString("DataDom_Foliation");

	internal static string DataDom_TableNameChange => GetResourceString("DataDom_TableNameChange");

	internal static string DataDom_TableNamespaceChange => GetResourceString("DataDom_TableNamespaceChange");

	internal static string DataDom_ColumnNameChange => GetResourceString("DataDom_ColumnNameChange");

	internal static string DataDom_ColumnNamespaceChange => GetResourceString("DataDom_ColumnNamespaceChange");

	internal static string DataDom_ColumnMappingChange => GetResourceString("DataDom_ColumnMappingChange");

	internal static string DataDom_TableColumnsChange => GetResourceString("DataDom_TableColumnsChange");

	internal static string DataDom_DataSetTablesChange => GetResourceString("DataDom_DataSetTablesChange");

	internal static string DataDom_DataSetNestedRelationsChange => GetResourceString("DataDom_DataSetNestedRelationsChange");

	internal static string DataDom_DataSetNull => GetResourceString("DataDom_DataSetNull");

	internal static string DataDom_DataSetNameChange => GetResourceString("DataDom_DataSetNameChange");

	internal static string DataDom_CloneNode => GetResourceString("DataDom_CloneNode");

	internal static string DataDom_MultipleLoad => GetResourceString("DataDom_MultipleLoad");

	internal static string DataDom_MultipleDataSet => GetResourceString("DataDom_MultipleDataSet");

	internal static string DataDom_NotSupport_GetElementById => GetResourceString("DataDom_NotSupport_GetElementById");

	internal static string DataDom_NotSupport_EntRef => GetResourceString("DataDom_NotSupport_EntRef");

	internal static string DataDom_NotSupport_Clear => GetResourceString("DataDom_NotSupport_Clear");

	internal static string ADP_EmptyArray => GetResourceString("ADP_EmptyArray");

	internal static string SQL_WrongType => GetResourceString("SQL_WrongType");

	internal static string ADP_KeywordNotSupported => GetResourceString("ADP_KeywordNotSupported");

	internal static string ADP_InternalProviderError => GetResourceString("ADP_InternalProviderError");

	internal static string ADP_NoQuoteChange => GetResourceString("ADP_NoQuoteChange");

	internal static string ADP_MissingSourceCommand => GetResourceString("ADP_MissingSourceCommand");

	internal static string ADP_MissingSourceCommandConnection => GetResourceString("ADP_MissingSourceCommandConnection");

	internal static string ADP_ColumnSchemaExpression => GetResourceString("ADP_ColumnSchemaExpression");

	internal static string ADP_ColumnSchemaMismatch => GetResourceString("ADP_ColumnSchemaMismatch");

	internal static string ADP_ColumnSchemaMissing1 => GetResourceString("ADP_ColumnSchemaMissing1");

	internal static string ADP_ColumnSchemaMissing2 => GetResourceString("ADP_ColumnSchemaMissing2");

	internal static string ADP_InvalidSourceColumn => GetResourceString("ADP_InvalidSourceColumn");

	internal static string ADP_MissingColumnMapping => GetResourceString("ADP_MissingColumnMapping");

	internal static string ADP_NotSupportedEnumerationValue => GetResourceString("ADP_NotSupportedEnumerationValue");

	internal static string ADP_MissingTableSchema => GetResourceString("ADP_MissingTableSchema");

	internal static string ADP_InvalidSourceTable => GetResourceString("ADP_InvalidSourceTable");

	internal static string ADP_MissingTableMapping => GetResourceString("ADP_MissingTableMapping");

	internal static string ADP_ConnectionRequired_Insert => GetResourceString("ADP_ConnectionRequired_Insert");

	internal static string ADP_ConnectionRequired_Update => GetResourceString("ADP_ConnectionRequired_Update");

	internal static string ADP_ConnectionRequired_Delete => GetResourceString("ADP_ConnectionRequired_Delete");

	internal static string ADP_ConnectionRequired_Batch => GetResourceString("ADP_ConnectionRequired_Batch");

	internal static string ADP_ConnectionRequired_Clone => GetResourceString("ADP_ConnectionRequired_Clone");

	internal static string ADP_OpenConnectionRequired_Insert => GetResourceString("ADP_OpenConnectionRequired_Insert");

	internal static string ADP_OpenConnectionRequired_Update => GetResourceString("ADP_OpenConnectionRequired_Update");

	internal static string ADP_OpenConnectionRequired_Delete => GetResourceString("ADP_OpenConnectionRequired_Delete");

	internal static string ADP_OpenConnectionRequired_Clone => GetResourceString("ADP_OpenConnectionRequired_Clone");

	internal static string ADP_MissingSelectCommand => GetResourceString("ADP_MissingSelectCommand");

	internal static string ADP_UnwantedStatementType => GetResourceString("ADP_UnwantedStatementType");

	internal static string ADP_FillSchemaRequiresSourceTableName => GetResourceString("ADP_FillSchemaRequiresSourceTableName");

	internal static string ADP_FillRequiresSourceTableName => GetResourceString("ADP_FillRequiresSourceTableName");

	internal static string ADP_FillChapterAutoIncrement => GetResourceString("ADP_FillChapterAutoIncrement");

	internal static string ADP_MissingDataReaderFieldType => GetResourceString("ADP_MissingDataReaderFieldType");

	internal static string ADP_OnlyOneTableForStartRecordOrMaxRecords => GetResourceString("ADP_OnlyOneTableForStartRecordOrMaxRecords");

	internal static string ADP_UpdateRequiresSourceTable => GetResourceString("ADP_UpdateRequiresSourceTable");

	internal static string ADP_UpdateRequiresSourceTableName => GetResourceString("ADP_UpdateRequiresSourceTableName");

	internal static string ADP_UpdateRequiresCommandClone => GetResourceString("ADP_UpdateRequiresCommandClone");

	internal static string ADP_UpdateRequiresCommandSelect => GetResourceString("ADP_UpdateRequiresCommandSelect");

	internal static string ADP_UpdateRequiresCommandInsert => GetResourceString("ADP_UpdateRequiresCommandInsert");

	internal static string ADP_UpdateRequiresCommandUpdate => GetResourceString("ADP_UpdateRequiresCommandUpdate");

	internal static string ADP_UpdateRequiresCommandDelete => GetResourceString("ADP_UpdateRequiresCommandDelete");

	internal static string ADP_UpdateMismatchRowTable => GetResourceString("ADP_UpdateMismatchRowTable");

	internal static string ADP_RowUpdatedErrors => GetResourceString("ADP_RowUpdatedErrors");

	internal static string ADP_RowUpdatingErrors => GetResourceString("ADP_RowUpdatingErrors");

	internal static string ADP_ResultsNotAllowedDuringBatch => GetResourceString("ADP_ResultsNotAllowedDuringBatch");

	internal static string ADP_UpdateConcurrencyViolation_Update => GetResourceString("ADP_UpdateConcurrencyViolation_Update");

	internal static string ADP_UpdateConcurrencyViolation_Delete => GetResourceString("ADP_UpdateConcurrencyViolation_Delete");

	internal static string ADP_UpdateConcurrencyViolation_Batch => GetResourceString("ADP_UpdateConcurrencyViolation_Batch");

	internal static string ADP_InvalidSourceBufferIndex => GetResourceString("ADP_InvalidSourceBufferIndex");

	internal static string ADP_InvalidDestinationBufferIndex => GetResourceString("ADP_InvalidDestinationBufferIndex");

	internal static string ADP_StreamClosed => GetResourceString("ADP_StreamClosed");

	internal static string ADP_InvalidSeekOrigin => GetResourceString("ADP_InvalidSeekOrigin");

	internal static string ADP_DynamicSQLJoinUnsupported => GetResourceString("ADP_DynamicSQLJoinUnsupported");

	internal static string ADP_DynamicSQLNoTableInfo => GetResourceString("ADP_DynamicSQLNoTableInfo");

	internal static string ADP_DynamicSQLNoKeyInfoDelete => GetResourceString("ADP_DynamicSQLNoKeyInfoDelete");

	internal static string ADP_DynamicSQLNoKeyInfoUpdate => GetResourceString("ADP_DynamicSQLNoKeyInfoUpdate");

	internal static string ADP_DynamicSQLNoKeyInfoRowVersionDelete => GetResourceString("ADP_DynamicSQLNoKeyInfoRowVersionDelete");

	internal static string ADP_DynamicSQLNoKeyInfoRowVersionUpdate => GetResourceString("ADP_DynamicSQLNoKeyInfoRowVersionUpdate");

	internal static string ADP_DynamicSQLNestedQuote => GetResourceString("ADP_DynamicSQLNestedQuote");

	internal static string SQL_InvalidBufferSizeOrIndex => GetResourceString("SQL_InvalidBufferSizeOrIndex");

	internal static string SQL_InvalidDataLength => GetResourceString("SQL_InvalidDataLength");

	internal static string SqlMisc_NullString => GetResourceString("SqlMisc_NullString");

	internal static string SqlMisc_ArithOverflowMessage => GetResourceString("SqlMisc_ArithOverflowMessage");

	internal static string SqlMisc_DivideByZeroMessage => GetResourceString("SqlMisc_DivideByZeroMessage");

	internal static string SqlMisc_NullValueMessage => GetResourceString("SqlMisc_NullValueMessage");

	internal static string SqlMisc_TruncationMessage => GetResourceString("SqlMisc_TruncationMessage");

	internal static string SqlMisc_DateTimeOverflowMessage => GetResourceString("SqlMisc_DateTimeOverflowMessage");

	internal static string SqlMisc_ConcatDiffCollationMessage => GetResourceString("SqlMisc_ConcatDiffCollationMessage");

	internal static string SqlMisc_CompareDiffCollationMessage => GetResourceString("SqlMisc_CompareDiffCollationMessage");

	internal static string SqlMisc_ConversionOverflowMessage => GetResourceString("SqlMisc_ConversionOverflowMessage");

	internal static string SqlMisc_InvalidDateTimeMessage => GetResourceString("SqlMisc_InvalidDateTimeMessage");

	internal static string SqlMisc_TimeZoneSpecifiedMessage => GetResourceString("SqlMisc_TimeZoneSpecifiedMessage");

	internal static string SqlMisc_InvalidArraySizeMessage => GetResourceString("SqlMisc_InvalidArraySizeMessage");

	internal static string SqlMisc_InvalidPrecScaleMessage => GetResourceString("SqlMisc_InvalidPrecScaleMessage");

	internal static string SqlMisc_FormatMessage => GetResourceString("SqlMisc_FormatMessage");

	internal static string SqlMisc_SqlTypeMessage => GetResourceString("SqlMisc_SqlTypeMessage");

	internal static string SqlMisc_NoBufferMessage => GetResourceString("SqlMisc_NoBufferMessage");

	internal static string SqlMisc_BufferInsufficientMessage => GetResourceString("SqlMisc_BufferInsufficientMessage");

	internal static string SqlMisc_WriteNonZeroOffsetOnNullMessage => GetResourceString("SqlMisc_WriteNonZeroOffsetOnNullMessage");

	internal static string SqlMisc_WriteOffsetLargerThanLenMessage => GetResourceString("SqlMisc_WriteOffsetLargerThanLenMessage");

	internal static string SqlMisc_NotFilledMessage => GetResourceString("SqlMisc_NotFilledMessage");

	internal static string SqlMisc_AlreadyFilledMessage => GetResourceString("SqlMisc_AlreadyFilledMessage");

	internal static string SqlMisc_ClosedXmlReaderMessage => GetResourceString("SqlMisc_ClosedXmlReaderMessage");

	internal static string SqlMisc_InvalidOpStreamClosed => GetResourceString("SqlMisc_InvalidOpStreamClosed");

	internal static string SqlMisc_InvalidOpStreamNonWritable => GetResourceString("SqlMisc_InvalidOpStreamNonWritable");

	internal static string SqlMisc_InvalidOpStreamNonReadable => GetResourceString("SqlMisc_InvalidOpStreamNonReadable");

	internal static string SqlMisc_InvalidOpStreamNonSeekable => GetResourceString("SqlMisc_InvalidOpStreamNonSeekable");

	internal static string ADP_DBConcurrencyExceptionMessage => GetResourceString("ADP_DBConcurrencyExceptionMessage");

	internal static string ADP_InvalidMaxRecords => GetResourceString("ADP_InvalidMaxRecords");

	internal static string ADP_CollectionIndexInt32 => GetResourceString("ADP_CollectionIndexInt32");

	internal static string ADP_MissingTableMappingDestination => GetResourceString("ADP_MissingTableMappingDestination");

	internal static string ADP_InvalidStartRecord => GetResourceString("ADP_InvalidStartRecord");

	internal static string DataDom_EnforceConstraintsShouldBeOff => GetResourceString("DataDom_EnforceConstraintsShouldBeOff");

	internal static string DataColumns_RemoveExpression => GetResourceString("DataColumns_RemoveExpression");

	internal static string DataRow_RowInsertTwice => GetResourceString("DataRow_RowInsertTwice");

	internal static string Xml_ElementTypeNotFound => GetResourceString("Xml_ElementTypeNotFound");

	internal static string ADP_DbProviderFactories_InvariantNameNotFound => GetResourceString("ADP_DbProviderFactories_InvariantNameNotFound");

	internal static string ADP_DbProviderFactories_NoInstance => GetResourceString("ADP_DbProviderFactories_NoInstance");

	internal static string ADP_DbProviderFactories_FactoryNotLoadable => GetResourceString("ADP_DbProviderFactories_FactoryNotLoadable");

	internal static string ADP_DbProviderFactories_NoAssemblyQualifiedName => GetResourceString("ADP_DbProviderFactories_NoAssemblyQualifiedName");

	internal static string ADP_DbProviderFactories_NotAFactoryType => GetResourceString("ADP_DbProviderFactories_NotAFactoryType");

	internal static string DataSetLinq_InvalidEnumerationValue => GetResourceString("DataSetLinq_InvalidEnumerationValue");

	internal static string LDV_InvalidNumOfKeys => GetResourceString("LDV_InvalidNumOfKeys");

	internal static string LDVRowStateError => GetResourceString("LDVRowStateError");

	internal static string ToLDVUnsupported => GetResourceString("ToLDVUnsupported");

	internal static string DataSetLinq_EmptyDataRowSource => GetResourceString("DataSetLinq_EmptyDataRowSource");

	internal static string DataSetLinq_NullDataRow => GetResourceString("DataSetLinq_NullDataRow");

	internal static string DataSetLinq_CannotLoadDetachedRow => GetResourceString("DataSetLinq_CannotLoadDetachedRow");

	internal static string DataSetLinq_CannotCompareDeletedRow => GetResourceString("DataSetLinq_CannotCompareDeletedRow");

	internal static string DataSetLinq_CannotLoadDeletedRow => GetResourceString("DataSetLinq_CannotLoadDeletedRow");

	internal static string DataSetLinq_NonNullableCast => GetResourceString("DataSetLinq_NonNullableCast");

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
