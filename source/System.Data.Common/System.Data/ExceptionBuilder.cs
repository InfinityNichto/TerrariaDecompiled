using System.ComponentModel;
using System.Data.Common;
using System.Globalization;

namespace System.Data;

internal static class ExceptionBuilder
{
	private static void TraceException(string trace, Exception e)
	{
		if (e != null)
		{
			DataCommonEventSource.Log.Trace(trace, e);
		}
	}

	internal static Exception TraceExceptionAsReturnValue(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|THROW> '{0}'", e);
		return e;
	}

	internal static Exception TraceExceptionForCapture(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
		return e;
	}

	internal static Exception TraceExceptionWithoutRethrow(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
		return e;
	}

	internal static Exception _Argument(string error)
	{
		return TraceExceptionAsReturnValue(new ArgumentException(error));
	}

	internal static Exception _Argument(string paramName, string error)
	{
		return TraceExceptionAsReturnValue(new ArgumentException(error));
	}

	internal static Exception _Argument(string error, Exception innerException)
	{
		return TraceExceptionAsReturnValue(new ArgumentException(error, innerException));
	}

	private static Exception _ArgumentNull(string paramName, string msg)
	{
		return TraceExceptionAsReturnValue(new ArgumentNullException(paramName, msg));
	}

	internal static Exception _ArgumentOutOfRange(string paramName, string msg)
	{
		return TraceExceptionAsReturnValue(new ArgumentOutOfRangeException(paramName, msg));
	}

	private static Exception _IndexOutOfRange(string error)
	{
		return TraceExceptionAsReturnValue(new IndexOutOfRangeException(error));
	}

	private static Exception _InvalidOperation(string error)
	{
		return TraceExceptionAsReturnValue(new InvalidOperationException(error));
	}

	private static Exception _InvalidEnumArgumentException(string error)
	{
		return TraceExceptionAsReturnValue(new InvalidEnumArgumentException(error));
	}

	private static Exception _InvalidEnumArgumentException<T>(T value) where T : Enum
	{
		return _InvalidEnumArgumentException(System.SR.Format(System.SR.ADP_InvalidEnumerationValue, typeof(T).Name, value.ToString()));
	}

	private static void ThrowDataException(string error, Exception innerException)
	{
		throw TraceExceptionAsReturnValue(new DataException(error, innerException));
	}

	private static Exception _Data(string error)
	{
		return TraceExceptionAsReturnValue(new DataException(error));
	}

	private static Exception _Constraint(string error)
	{
		return TraceExceptionAsReturnValue(new ConstraintException(error));
	}

	private static Exception _InvalidConstraint(string error)
	{
		return TraceExceptionAsReturnValue(new InvalidConstraintException(error));
	}

	private static Exception _DeletedRowInaccessible(string error)
	{
		return TraceExceptionAsReturnValue(new DeletedRowInaccessibleException(error));
	}

	private static Exception _DuplicateName(string error)
	{
		return TraceExceptionAsReturnValue(new DuplicateNameException(error));
	}

	private static Exception _InRowChangingEvent(string error)
	{
		return TraceExceptionAsReturnValue(new InRowChangingEventException(error));
	}

	private static Exception _MissingPrimaryKey(string error)
	{
		return TraceExceptionAsReturnValue(new MissingPrimaryKeyException(error));
	}

	private static Exception _NoNullAllowed(string error)
	{
		return TraceExceptionAsReturnValue(new NoNullAllowedException(error));
	}

	private static Exception _ReadOnly(string error)
	{
		return TraceExceptionAsReturnValue(new ReadOnlyException(error));
	}

	private static Exception _RowNotInTable(string error)
	{
		return TraceExceptionAsReturnValue(new RowNotInTableException(error));
	}

	private static Exception _VersionNotFound(string error)
	{
		return TraceExceptionAsReturnValue(new VersionNotFoundException(error));
	}

	public static Exception ArgumentNull(string paramName)
	{
		return _ArgumentNull(paramName, System.SR.Format(System.SR.Data_ArgumentNull, paramName));
	}

	public static Exception ArgumentOutOfRange(string paramName)
	{
		return _ArgumentOutOfRange(paramName, System.SR.Format(System.SR.Data_ArgumentOutOfRange, paramName));
	}

	public static Exception BadObjectPropertyAccess(string error)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataConstraint_BadObjectPropertyAccess, error));
	}

	public static Exception ArgumentContainsNull(string paramName)
	{
		return _Argument(paramName, System.SR.Format(System.SR.Data_ArgumentContainsNull, paramName));
	}

	public static Exception TypeNotAllowed(Type type)
	{
		return _InvalidOperation(System.SR.Format(System.SR.Data_TypeNotAllowed, type.AssemblyQualifiedName));
	}

	public static Exception CannotModifyCollection()
	{
		return _Argument(System.SR.Data_CannotModifyCollection);
	}

	public static Exception CaseInsensitiveNameConflict(string name)
	{
		return _Argument(System.SR.Format(System.SR.Data_CaseInsensitiveNameConflict, name));
	}

	public static Exception NamespaceNameConflict(string name)
	{
		return _Argument(System.SR.Format(System.SR.Data_NamespaceNameConflict, name));
	}

	public static Exception InvalidOffsetLength()
	{
		return _Argument(System.SR.Data_InvalidOffsetLength);
	}

	public static Exception ColumnNotInTheTable(string column, string table)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_NotInTheTable, column, table));
	}

	public static Exception ColumnNotInAnyTable()
	{
		return _Argument(System.SR.DataColumn_NotInAnyTable);
	}

	public static Exception ColumnOutOfRange(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataColumns_OutOfRange, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception ColumnOutOfRange(string column)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataColumns_OutOfRange, column));
	}

	public static Exception CannotAddColumn1(string column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_Add1, column));
	}

	public static Exception CannotAddColumn2(string column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_Add2, column));
	}

	public static Exception CannotAddColumn3()
	{
		return _Argument(System.SR.DataColumns_Add3);
	}

	public static Exception CannotAddColumn4(string column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_Add4, column));
	}

	public static Exception CannotAddDuplicate(string column)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataColumns_AddDuplicate, column));
	}

	public static Exception CannotAddDuplicate2(string table)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataColumns_AddDuplicate2, table));
	}

	public static Exception CannotAddDuplicate3(string table)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataColumns_AddDuplicate3, table));
	}

	public static Exception CannotRemoveColumn()
	{
		return _Argument(System.SR.DataColumns_Remove);
	}

	public static Exception CannotRemovePrimaryKey()
	{
		return _Argument(System.SR.DataColumns_RemovePrimaryKey);
	}

	public static Exception CannotRemoveChildKey(string relation)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_RemoveChildKey, relation));
	}

	public static Exception CannotRemoveConstraint(string constraint, string table)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_RemoveConstraint, constraint, table));
	}

	public static Exception CannotRemoveExpression(string column, string expression)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_RemoveExpression, column, expression));
	}

	public static Exception ColumnNotInTheUnderlyingTable(string column, string table)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_NotInTheUnderlyingTable, column, table));
	}

	public static Exception InvalidOrdinal(string name, int ordinal)
	{
		return _ArgumentOutOfRange(name, System.SR.Format(System.SR.DataColumn_OrdinalExceedMaximun, ordinal.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception AddPrimaryKeyConstraint()
	{
		return _Argument(System.SR.DataConstraint_AddPrimaryKeyConstraint);
	}

	public static Exception NoConstraintName()
	{
		return _Argument(System.SR.DataConstraint_NoName);
	}

	public static Exception ConstraintViolation(string constraint)
	{
		return _Constraint(System.SR.Format(System.SR.DataConstraint_Violation, constraint));
	}

	public static Exception ConstraintNotInTheTable(string constraint)
	{
		return _Argument(System.SR.Format(System.SR.DataConstraint_NotInTheTable, constraint));
	}

	public static string KeysToString(object[] keys)
	{
		string text = string.Empty;
		for (int i = 0; i < keys.Length; i++)
		{
			text = text + Convert.ToString(keys[i], null) + ((i < keys.Length - 1) ? ", " : string.Empty);
		}
		return text;
	}

	public static string UniqueConstraintViolationText(DataColumn[] columns, object[] values)
	{
		if (columns.Length > 1)
		{
			string text = string.Empty;
			for (int i = 0; i < columns.Length; i++)
			{
				text = text + columns[i].ColumnName + ((i < columns.Length - 1) ? ", " : "");
			}
			return System.SR.Format(System.SR.DataConstraint_ViolationValue, text, KeysToString(values));
		}
		return System.SR.Format(System.SR.DataConstraint_ViolationValue, columns[0].ColumnName, Convert.ToString(values[0], null));
	}

	public static Exception ConstraintViolation(DataColumn[] columns, object[] values)
	{
		return _Constraint(UniqueConstraintViolationText(columns, values));
	}

	public static Exception ConstraintOutOfRange(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataConstraint_OutOfRange, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception DuplicateConstraint(string constraint)
	{
		return _Data(System.SR.Format(System.SR.DataConstraint_Duplicate, constraint));
	}

	public static Exception DuplicateConstraintName(string constraint)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataConstraint_DuplicateName, constraint));
	}

	public static Exception NeededForForeignKeyConstraint(UniqueConstraint key, ForeignKeyConstraint fk)
	{
		return _Argument(System.SR.Format(System.SR.DataConstraint_NeededForForeignKeyConstraint, key.ConstraintName, fk.ConstraintName));
	}

	public static Exception UniqueConstraintViolation()
	{
		return _Argument(System.SR.DataConstraint_UniqueViolation);
	}

	public static Exception ConstraintForeignTable()
	{
		return _Argument(System.SR.DataConstraint_ForeignTable);
	}

	public static Exception ConstraintParentValues()
	{
		return _Argument(System.SR.DataConstraint_ParentValues);
	}

	public static Exception ConstraintAddFailed(DataTable table)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_AddFailed, table.TableName));
	}

	public static Exception ConstraintRemoveFailed()
	{
		return _Argument(System.SR.DataConstraint_RemoveFailed);
	}

	public static Exception FailedCascadeDelete(string constraint)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_CascadeDelete, constraint));
	}

	public static Exception FailedCascadeUpdate(string constraint)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_CascadeUpdate, constraint));
	}

	public static Exception FailedClearParentTable(string table, string constraint, string childTable)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_ClearParentTable, table, constraint, childTable));
	}

	public static Exception ForeignKeyViolation(string constraint, object[] keys)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_ForeignKeyViolation, constraint, KeysToString(keys)));
	}

	public static Exception RemoveParentRow(ForeignKeyConstraint constraint)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataConstraint_RemoveParentRow, constraint.ConstraintName));
	}

	public static string MaxLengthViolationText(string columnName)
	{
		return System.SR.Format(System.SR.DataColumn_ExceedMaxLength, columnName);
	}

	public static string NotAllowDBNullViolationText(string columnName)
	{
		return System.SR.Format(System.SR.DataColumn_NotAllowDBNull, columnName);
	}

	public static Exception CantAddConstraintToMultipleNestedTable(string tableName)
	{
		return _Argument(System.SR.Format(System.SR.DataConstraint_CantAddConstraintToMultipleNestedTable, tableName));
	}

	public static Exception AutoIncrementAndExpression()
	{
		return _Argument(System.SR.DataColumn_AutoIncrementAndExpression);
	}

	public static Exception AutoIncrementAndDefaultValue()
	{
		return _Argument(System.SR.DataColumn_AutoIncrementAndDefaultValue);
	}

	public static Exception AutoIncrementSeed()
	{
		return _Argument(System.SR.DataColumn_AutoIncrementSeed);
	}

	public static Exception CantChangeDataType()
	{
		return _Argument(System.SR.DataColumn_ChangeDataType);
	}

	public static Exception NullDataType()
	{
		return _Argument(System.SR.DataColumn_NullDataType);
	}

	public static Exception ColumnNameRequired()
	{
		return _Argument(System.SR.DataColumn_NameRequired);
	}

	public static Exception DefaultValueAndAutoIncrement()
	{
		return _Argument(System.SR.DataColumn_DefaultValueAndAutoIncrement);
	}

	public static Exception DefaultValueDataType(string column, Type defaultType, Type columnType, Exception inner)
	{
		if (column.Length != 0)
		{
			return _Argument(System.SR.Format(System.SR.DataColumn_DefaultValueDataType, column, defaultType.FullName, columnType.FullName), inner);
		}
		return _Argument(System.SR.Format(System.SR.DataColumn_DefaultValueDataType1, defaultType.FullName, columnType.FullName), inner);
	}

	public static Exception DefaultValueColumnDataType(string column, Type defaultType, Type columnType, Exception inner)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_DefaultValueColumnDataType, column, defaultType.FullName, columnType.FullName), inner);
	}

	public static Exception ExpressionAndUnique()
	{
		return _Argument(System.SR.DataColumn_ExpressionAndUnique);
	}

	public static Exception ExpressionAndReadOnly()
	{
		return _Argument(System.SR.DataColumn_ExpressionAndReadOnly);
	}

	public static Exception ExpressionAndConstraint(DataColumn column, Constraint constraint)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_ExpressionAndConstraint, column.ColumnName, constraint.ConstraintName));
	}

	public static Exception ExpressionInConstraint(DataColumn column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_ExpressionInConstraint, column.ColumnName));
	}

	public static Exception ExpressionCircular()
	{
		return _Argument(System.SR.DataColumn_ExpressionCircular);
	}

	public static Exception NonUniqueValues(string column)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataColumn_NonUniqueValues, column));
	}

	public static Exception NullKeyValues(string column)
	{
		return _Data(System.SR.Format(System.SR.DataColumn_NullKeyValues, column));
	}

	public static Exception NullValues(string column)
	{
		return _NoNullAllowed(System.SR.Format(System.SR.DataColumn_NullValues, column));
	}

	public static Exception ReadOnlyAndExpression()
	{
		return _ReadOnly(System.SR.DataColumn_ReadOnlyAndExpression);
	}

	public static Exception ReadOnly(string column)
	{
		return _ReadOnly(System.SR.Format(System.SR.DataColumn_ReadOnly, column));
	}

	public static Exception UniqueAndExpression()
	{
		return _Argument(System.SR.DataColumn_UniqueAndExpression);
	}

	public static Exception SetFailed(object value, DataColumn column, Type type, Exception innerException)
	{
		return _Argument(innerException.Message + System.SR.Format(System.SR.DataColumn_SetFailed, value?.ToString(), column.ColumnName, type.Name), innerException);
	}

	public static Exception CannotSetToNull(DataColumn column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotSetToNull, column.ColumnName));
	}

	public static Exception LongerThanMaxLength(DataColumn column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_LongerThanMaxLength, column.ColumnName));
	}

	public static Exception CannotSetMaxLength(DataColumn column, int value)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotSetMaxLength, column.ColumnName, value.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception CannotSetMaxLength2(DataColumn column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotSetMaxLength2, column.ColumnName));
	}

	public static Exception CannotSetSimpleContentType(string columnName, Type type)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotSimpleContentType, columnName, type));
	}

	public static Exception CannotSetSimpleContent(string columnName, Type type)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotSimpleContent, columnName, type));
	}

	public static Exception CannotChangeNamespace(string columnName)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_CannotChangeNamespace, columnName));
	}

	public static Exception HasToBeStringType(DataColumn column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_HasToBeStringType, column.ColumnName));
	}

	public static Exception AutoIncrementCannotSetIfHasData(string typeName)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_AutoIncrementCannotSetIfHasData, typeName));
	}

	public static Exception INullableUDTwithoutStaticNull(string typeName)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_INullableUDTwithoutStaticNull, typeName));
	}

	public static Exception IComparableNotImplemented(string typeName)
	{
		return _Data(System.SR.Format(System.SR.DataStorage_IComparableNotDefined, typeName));
	}

	public static Exception UDTImplementsIChangeTrackingButnotIRevertible(string typeName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataColumn_UDTImplementsIChangeTrackingButnotIRevertible, typeName));
	}

	public static Exception SetAddedAndModifiedCalledOnnonUnchanged()
	{
		return _InvalidOperation(System.SR.DataColumn_SetAddedAndModifiedCalledOnNonUnchanged);
	}

	public static Exception InvalidDataColumnMapping(Type type)
	{
		return _Argument(System.SR.Format(System.SR.DataColumn_InvalidDataColumnMapping, type.AssemblyQualifiedName));
	}

	public static Exception CannotSetDateTimeModeForNonDateTimeColumns()
	{
		return _InvalidOperation(System.SR.DataColumn_CannotSetDateTimeModeForNonDateTimeColumns);
	}

	public static Exception InvalidDateTimeMode(DataSetDateTime mode)
	{
		return _InvalidEnumArgumentException(mode);
	}

	public static Exception CantChangeDateTimeMode(DataSetDateTime oldValue, DataSetDateTime newValue)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataColumn_DateTimeMode, oldValue.ToString(), newValue.ToString()));
	}

	public static Exception ColumnTypeNotSupported()
	{
		return ADP.NotSupported(System.SR.DataColumn_NullableTypesNotSupported);
	}

	public static Exception SetFailed(string name)
	{
		return _Data(System.SR.Format(System.SR.DataView_SetFailed, name));
	}

	public static Exception SetDataSetFailed()
	{
		return _Data(System.SR.DataView_SetDataSetFailed);
	}

	public static Exception SetRowStateFilter()
	{
		return _Data(System.SR.DataView_SetRowStateFilter);
	}

	public static Exception CanNotSetDataSet()
	{
		return _Data(System.SR.DataView_CanNotSetDataSet);
	}

	public static Exception CanNotUseDataViewManager()
	{
		return _Data(System.SR.DataView_CanNotUseDataViewManager);
	}

	public static Exception CanNotSetTable()
	{
		return _Data(System.SR.DataView_CanNotSetTable);
	}

	public static Exception CanNotUse()
	{
		return _Data(System.SR.DataView_CanNotUse);
	}

	public static Exception CanNotBindTable()
	{
		return _Data(System.SR.DataView_CanNotBindTable);
	}

	public static Exception SetTable()
	{
		return _Data(System.SR.DataView_SetTable);
	}

	public static Exception SetIListObject()
	{
		return _Argument(System.SR.DataView_SetIListObject);
	}

	public static Exception AddNewNotAllowNull()
	{
		return _Data(System.SR.DataView_AddNewNotAllowNull);
	}

	public static Exception NotOpen()
	{
		return _Data(System.SR.DataView_NotOpen);
	}

	public static Exception CreateChildView()
	{
		return _Argument(System.SR.DataView_CreateChildView);
	}

	public static Exception CanNotDelete()
	{
		return _Data(System.SR.DataView_CanNotDelete);
	}

	public static Exception CanNotEdit()
	{
		return _Data(System.SR.DataView_CanNotEdit);
	}

	public static Exception GetElementIndex(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataView_GetElementIndex, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception AddExternalObject()
	{
		return _Argument(System.SR.DataView_AddExternalObject);
	}

	public static Exception CanNotClear()
	{
		return _Argument(System.SR.DataView_CanNotClear);
	}

	public static Exception InsertExternalObject()
	{
		return _Argument(System.SR.DataView_InsertExternalObject);
	}

	public static Exception RemoveExternalObject()
	{
		return _Argument(System.SR.DataView_RemoveExternalObject);
	}

	public static Exception PropertyNotFound(string property, string table)
	{
		return _Argument(System.SR.Format(System.SR.DataROWView_PropertyNotFound, property, table));
	}

	public static Exception ColumnToSortIsOutOfRange(string column)
	{
		return _Argument(System.SR.Format(System.SR.DataColumns_OutOfRange, column));
	}

	public static Exception KeyTableMismatch()
	{
		return _InvalidConstraint(System.SR.DataKey_TableMismatch);
	}

	public static Exception KeyNoColumns()
	{
		return _InvalidConstraint(System.SR.DataKey_NoColumns);
	}

	public static Exception KeyTooManyColumns(int cols)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataKey_TooManyColumns, cols.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception KeyDuplicateColumns(string columnName)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataKey_DuplicateColumns, columnName));
	}

	public static Exception RelationDataSetMismatch()
	{
		return _InvalidConstraint(System.SR.DataRelation_DataSetMismatch);
	}

	public static Exception NoRelationName()
	{
		return _Argument(System.SR.DataRelation_NoName);
	}

	public static Exception ColumnsTypeMismatch()
	{
		return _InvalidConstraint(System.SR.DataRelation_ColumnsTypeMismatch);
	}

	public static Exception KeyLengthMismatch()
	{
		return _Argument(System.SR.DataRelation_KeyLengthMismatch);
	}

	public static Exception KeyLengthZero()
	{
		return _Argument(System.SR.DataRelation_KeyZeroLength);
	}

	public static Exception ForeignRelation()
	{
		return _Argument(System.SR.DataRelation_ForeignDataSet);
	}

	public static Exception KeyColumnsIdentical()
	{
		return _InvalidConstraint(System.SR.DataRelation_KeyColumnsIdentical);
	}

	public static Exception RelationForeignTable(string t1, string t2)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataRelation_ForeignTable, t1, t2));
	}

	public static Exception GetParentRowTableMismatch(string t1, string t2)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataRelation_GetParentRowTableMismatch, t1, t2));
	}

	public static Exception SetParentRowTableMismatch(string t1, string t2)
	{
		return _InvalidConstraint(System.SR.Format(System.SR.DataRelation_SetParentRowTableMismatch, t1, t2));
	}

	public static Exception RelationForeignRow()
	{
		return _Argument(System.SR.DataRelation_ForeignRow);
	}

	public static Exception RelationNestedReadOnly()
	{
		return _Argument(System.SR.DataRelation_RelationNestedReadOnly);
	}

	public static Exception TableCantBeNestedInTwoTables(string tableName)
	{
		return _Argument(System.SR.Format(System.SR.DataRelation_TableCantBeNestedInTwoTables, tableName));
	}

	public static Exception LoopInNestedRelations(string tableName)
	{
		return _Argument(System.SR.Format(System.SR.DataRelation_LoopInNestedRelations, tableName));
	}

	public static Exception RelationDoesNotExist()
	{
		return _Argument(System.SR.DataRelation_DoesNotExist);
	}

	public static Exception ParentRowNotInTheDataSet()
	{
		return _Argument(System.SR.DataRow_ParentRowNotInTheDataSet);
	}

	public static Exception ParentOrChildColumnsDoNotHaveDataSet()
	{
		return _InvalidConstraint(System.SR.DataRelation_ParentOrChildColumnsDoNotHaveDataSet);
	}

	public static Exception InValidNestedRelation(string childTableName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataRelation_InValidNestedRelation, childTableName));
	}

	public static Exception InvalidParentNamespaceinNestedRelation(string childTableName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataRelation_InValidNamespaceInNestedRelation, childTableName));
	}

	public static Exception RowNotInTheDataSet()
	{
		return _Argument(System.SR.DataRow_NotInTheDataSet);
	}

	public static Exception RowNotInTheTable()
	{
		return _RowNotInTable(System.SR.DataRow_NotInTheTable);
	}

	public static Exception EditInRowChanging()
	{
		return _InRowChangingEvent(System.SR.DataRow_EditInRowChanging);
	}

	public static Exception EndEditInRowChanging()
	{
		return _InRowChangingEvent(System.SR.DataRow_EndEditInRowChanging);
	}

	public static Exception BeginEditInRowChanging()
	{
		return _InRowChangingEvent(System.SR.DataRow_BeginEditInRowChanging);
	}

	public static Exception CancelEditInRowChanging()
	{
		return _InRowChangingEvent(System.SR.DataRow_CancelEditInRowChanging);
	}

	public static Exception DeleteInRowDeleting()
	{
		return _InRowChangingEvent(System.SR.DataRow_DeleteInRowDeleting);
	}

	public static Exception ValueArrayLength()
	{
		return _Argument(System.SR.DataRow_ValuesArrayLength);
	}

	public static Exception NoCurrentData()
	{
		return _VersionNotFound(System.SR.DataRow_NoCurrentData);
	}

	public static Exception NoOriginalData()
	{
		return _VersionNotFound(System.SR.DataRow_NoOriginalData);
	}

	public static Exception NoProposedData()
	{
		return _VersionNotFound(System.SR.DataRow_NoProposedData);
	}

	public static Exception RowRemovedFromTheTable()
	{
		return _RowNotInTable(System.SR.DataRow_RemovedFromTheTable);
	}

	public static Exception DeletedRowInaccessible()
	{
		return _DeletedRowInaccessible(System.SR.DataRow_DeletedRowInaccessible);
	}

	public static Exception RowAlreadyDeleted()
	{
		return _DeletedRowInaccessible(System.SR.DataRow_AlreadyDeleted);
	}

	public static Exception RowEmpty()
	{
		return _Argument(System.SR.DataRow_Empty);
	}

	public static Exception InvalidRowVersion()
	{
		return _Data(System.SR.DataRow_InvalidVersion);
	}

	public static Exception RowOutOfRange()
	{
		return _IndexOutOfRange(System.SR.DataRow_RowOutOfRange);
	}

	public static Exception RowOutOfRange(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataRow_OutOfRange, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception RowInsertOutOfRange(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataRow_RowInsertOutOfRange, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception RowInsertTwice(int index, string tableName)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataRow_RowInsertTwice, index.ToString(CultureInfo.InvariantCulture), tableName));
	}

	public static Exception RowInsertMissing(string tableName)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataRow_RowInsertMissing, tableName));
	}

	public static Exception RowAlreadyRemoved()
	{
		return _Data(System.SR.DataRow_AlreadyRemoved);
	}

	public static Exception MultipleParents()
	{
		return _Data(System.SR.DataRow_MultipleParents);
	}

	public static Exception InvalidRowState(DataRowState state)
	{
		return _InvalidEnumArgumentException(state);
	}

	public static Exception InvalidRowBitPattern()
	{
		return _Argument(System.SR.DataRow_InvalidRowBitPattern);
	}

	internal static Exception SetDataSetNameToEmpty()
	{
		return _Argument(System.SR.DataSet_SetNameToEmpty);
	}

	internal static Exception SetDataSetNameConflicting(string name)
	{
		return _Argument(System.SR.Format(System.SR.DataSet_SetDataSetNameConflicting, name));
	}

	public static Exception DataSetUnsupportedSchema(string ns)
	{
		return _Argument(System.SR.Format(System.SR.DataSet_UnsupportedSchema, ns));
	}

	public static Exception MergeMissingDefinition(string obj)
	{
		return _Argument(System.SR.Format(System.SR.DataMerge_MissingDefinition, obj));
	}

	public static Exception TablesInDifferentSets()
	{
		return _Argument(System.SR.DataRelation_TablesInDifferentSets);
	}

	public static Exception RelationAlreadyExists()
	{
		return _Argument(System.SR.DataRelation_AlreadyExists);
	}

	public static Exception RowAlreadyInOtherCollection()
	{
		return _Argument(System.SR.DataRow_AlreadyInOtherCollection);
	}

	public static Exception RowAlreadyInTheCollection()
	{
		return _Argument(System.SR.DataRow_AlreadyInTheCollection);
	}

	public static Exception TableMissingPrimaryKey()
	{
		return _MissingPrimaryKey(System.SR.DataTable_MissingPrimaryKey);
	}

	public static Exception RecordStateRange()
	{
		return _Argument(System.SR.DataIndex_RecordStateRange);
	}

	public static Exception IndexKeyLength(int length, int keyLength)
	{
		if (length != 0)
		{
			return _Argument(System.SR.Format(System.SR.DataIndex_KeyLength, length.ToString(CultureInfo.InvariantCulture), keyLength.ToString(CultureInfo.InvariantCulture)));
		}
		return _Argument(System.SR.DataIndex_FindWithoutSortOrder);
	}

	public static Exception RemovePrimaryKey(DataTable table)
	{
		if (table.TableName.Length != 0)
		{
			return _Argument(System.SR.Format(System.SR.DataKey_RemovePrimaryKey1, table.TableName));
		}
		return _Argument(System.SR.DataKey_RemovePrimaryKey);
	}

	public static Exception RelationAlreadyInOtherDataSet()
	{
		return _Argument(System.SR.DataRelation_AlreadyInOtherDataSet);
	}

	public static Exception RelationAlreadyInTheDataSet()
	{
		return _Argument(System.SR.DataRelation_AlreadyInTheDataSet);
	}

	public static Exception RelationNotInTheDataSet(string relation)
	{
		return _Argument(System.SR.Format(System.SR.DataRelation_NotInTheDataSet, relation));
	}

	public static Exception RelationOutOfRange(object index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataRelation_OutOfRange, Convert.ToString(index, null)));
	}

	public static Exception DuplicateRelation(string relation)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataRelation_DuplicateName, relation));
	}

	public static Exception RelationTableNull()
	{
		return _Argument(System.SR.DataRelation_TableNull);
	}

	public static Exception RelationDataSetNull()
	{
		return _Argument(System.SR.DataRelation_TableNull);
	}

	public static Exception RelationTableWasRemoved()
	{
		return _Argument(System.SR.DataRelation_TableWasRemoved);
	}

	public static Exception ParentTableMismatch()
	{
		return _Argument(System.SR.DataRelation_ParentTableMismatch);
	}

	public static Exception ChildTableMismatch()
	{
		return _Argument(System.SR.DataRelation_ChildTableMismatch);
	}

	public static Exception EnforceConstraint()
	{
		return _Constraint(System.SR.Data_EnforceConstraints);
	}

	public static Exception CaseLocaleMismatch()
	{
		return _Argument(System.SR.DataRelation_CaseLocaleMismatch);
	}

	public static Exception CannotChangeCaseLocale()
	{
		return CannotChangeCaseLocale(null);
	}

	public static Exception CannotChangeCaseLocale(Exception innerException)
	{
		return _Argument(System.SR.DataSet_CannotChangeCaseLocale, innerException);
	}

	public static Exception CannotChangeSchemaSerializationMode()
	{
		return _InvalidOperation(System.SR.DataSet_CannotChangeSchemaSerializationMode);
	}

	public static Exception InvalidSchemaSerializationMode(Type enumType, string mode)
	{
		return _InvalidEnumArgumentException(System.SR.Format(System.SR.ADP_InvalidEnumerationValue, enumType.Name, mode));
	}

	public static Exception InvalidRemotingFormat(SerializationFormat mode)
	{
		return _InvalidEnumArgumentException(mode);
	}

	public static Exception TableForeignPrimaryKey()
	{
		return _Argument(System.SR.DataTable_ForeignPrimaryKey);
	}

	public static Exception TableCannotAddToSimpleContent()
	{
		return _Argument(System.SR.DataTable_CannotAddToSimpleContent);
	}

	public static Exception NoTableName()
	{
		return _Argument(System.SR.DataTable_NoName);
	}

	public static Exception MultipleTextOnlyColumns()
	{
		return _Argument(System.SR.DataTable_MultipleSimpleContentColumns);
	}

	public static Exception InvalidSortString(string sort)
	{
		return _Argument(System.SR.Format(System.SR.DataTable_InvalidSortString, sort));
	}

	public static Exception DuplicateTableName(string table)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataTable_DuplicateName, table));
	}

	public static Exception DuplicateTableName2(string table, string ns)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataTable_DuplicateName2, table, ns));
	}

	public static Exception SelfnestedDatasetConflictingName(string table)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataTable_SelfnestedDatasetConflictingName, table));
	}

	public static Exception DatasetConflictingName(string table)
	{
		return _DuplicateName(System.SR.Format(System.SR.DataTable_DatasetConflictingName, table));
	}

	public static Exception TableAlreadyInOtherDataSet()
	{
		return _Argument(System.SR.DataTable_AlreadyInOtherDataSet);
	}

	public static Exception TableAlreadyInTheDataSet()
	{
		return _Argument(System.SR.DataTable_AlreadyInTheDataSet);
	}

	public static Exception TableOutOfRange(int index)
	{
		return _IndexOutOfRange(System.SR.Format(System.SR.DataTable_OutOfRange, index.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception TableNotInTheDataSet(string table)
	{
		return _Argument(System.SR.Format(System.SR.DataTable_NotInTheDataSet, table));
	}

	public static Exception TableInRelation()
	{
		return _Argument(System.SR.DataTable_InRelation);
	}

	public static Exception TableInConstraint(DataTable table, Constraint constraint)
	{
		return _Argument(System.SR.Format(System.SR.DataTable_InConstraint, table.TableName, constraint.ConstraintName));
	}

	public static Exception CanNotSerializeDataTableHierarchy()
	{
		return _InvalidOperation(System.SR.DataTable_CanNotSerializeDataTableHierarchy);
	}

	public static Exception CanNotRemoteDataTable()
	{
		return _InvalidOperation(System.SR.DataTable_CanNotRemoteDataTable);
	}

	public static Exception CanNotSetRemotingFormat()
	{
		return _Argument(System.SR.DataTable_CanNotSetRemotingFormat);
	}

	public static Exception CanNotSerializeDataTableWithEmptyName()
	{
		return _InvalidOperation(System.SR.DataTable_CanNotSerializeDataTableWithEmptyName);
	}

	public static Exception TableNotFound(string tableName)
	{
		return _Argument(System.SR.Format(System.SR.DataTable_TableNotFound, tableName));
	}

	public static Exception AggregateException(AggregateType aggregateType, Type type)
	{
		return _Data(System.SR.Format(System.SR.DataStorage_AggregateException, aggregateType, type.Name));
	}

	public static Exception InvalidStorageType(TypeCode typecode)
	{
		return _Data(System.SR.Format(System.SR.DataStorage_InvalidStorageType, typecode.ToString()));
	}

	public static Exception RangeArgument(int min, int max)
	{
		return _Argument(System.SR.Format(System.SR.Range_Argument, min.ToString(CultureInfo.InvariantCulture), max.ToString(CultureInfo.InvariantCulture)));
	}

	public static Exception NullRange()
	{
		return _Data(System.SR.Range_NullRange);
	}

	public static Exception NegativeMinimumCapacity()
	{
		return _Argument(System.SR.RecordManager_MinimumCapacity);
	}

	public static Exception ProblematicChars(char charValue)
	{
		return _Argument(System.SR.Format(System.SR.DataStorage_ProblematicChars, $"0x{charValue:X}"));
	}

	public static Exception StorageSetFailed()
	{
		return _Argument(System.SR.DataStorage_SetInvalidDataType);
	}

	public static Exception SimpleTypeNotSupported()
	{
		return _Data(System.SR.Xml_SimpleTypeNotSupported);
	}

	public static Exception MissingAttribute(string attribute)
	{
		return MissingAttribute(string.Empty, attribute);
	}

	public static Exception MissingAttribute(string element, string attribute)
	{
		return _Data(System.SR.Format(System.SR.Xml_MissingAttribute, element, attribute));
	}

	public static Exception InvalidAttributeValue(string name, string value)
	{
		return _Data(System.SR.Format(System.SR.Xml_ValueOutOfRange, name, value));
	}

	public static Exception AttributeValues(string name, string value1, string value2)
	{
		return _Data(System.SR.Format(System.SR.Xml_AttributeValues, name, value1, value2));
	}

	public static Exception ElementTypeNotFound(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_ElementTypeNotFound, name));
	}

	public static Exception RelationParentNameMissing(string rel)
	{
		return _Data(System.SR.Format(System.SR.Xml_RelationParentNameMissing, rel));
	}

	public static Exception RelationChildNameMissing(string rel)
	{
		return _Data(System.SR.Format(System.SR.Xml_RelationChildNameMissing, rel));
	}

	public static Exception RelationTableKeyMissing(string rel)
	{
		return _Data(System.SR.Format(System.SR.Xml_RelationTableKeyMissing, rel));
	}

	public static Exception RelationChildKeyMissing(string rel)
	{
		return _Data(System.SR.Format(System.SR.Xml_RelationChildKeyMissing, rel));
	}

	public static Exception UndefinedDatatype(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_UndefinedDatatype, name));
	}

	public static Exception DatatypeNotDefined()
	{
		return _Data(System.SR.Xml_DatatypeNotDefined);
	}

	public static Exception MismatchKeyLength()
	{
		return _Data(System.SR.Xml_MismatchKeyLength);
	}

	public static Exception InvalidField(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_InvalidField, name));
	}

	public static Exception InvalidSelector(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_InvalidSelector, name));
	}

	public static Exception CircularComplexType(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_CircularComplexType, name));
	}

	public static Exception CannotInstantiateAbstract(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_CannotInstantiateAbstract, name));
	}

	public static Exception InvalidKey(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_InvalidKey, name));
	}

	public static Exception DiffgramMissingSQL()
	{
		return _Data(System.SR.Xml_MissingSQL);
	}

	public static Exception DuplicateConstraintRead(string str)
	{
		return _Data(System.SR.Format(System.SR.Xml_DuplicateConstraint, str));
	}

	public static Exception ColumnTypeConflict(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_ColumnConflict, name));
	}

	public static Exception CannotConvert(string name, string type)
	{
		return _Data(System.SR.Format(System.SR.Xml_CannotConvert, name, type));
	}

	public static Exception MissingRefer(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_MissingRefer, "refer", "keyref", name));
	}

	public static Exception InvalidPrefix(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_InvalidPrefix_SpecialCharacters, name));
	}

	public static Exception CanNotDeserializeObjectType()
	{
		return _InvalidOperation(System.SR.Xml_CanNotDeserializeObjectType);
	}

	public static Exception IsDataSetAttributeMissingInSchema()
	{
		return _Data(System.SR.Xml_IsDataSetAttributeMissingInSchema);
	}

	public static Exception TooManyIsDataSetAttributesInSchema()
	{
		return _Data(System.SR.Xml_TooManyIsDataSetAttributesInSchema);
	}

	public static Exception NestedCircular(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_NestedCircular, name));
	}

	public static Exception MultipleParentRows(string tableQName)
	{
		return _Data(System.SR.Format(System.SR.Xml_MultipleParentRows, tableQName));
	}

	public static Exception PolymorphismNotSupported(string typeName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.Xml_PolymorphismNotSupported, typeName));
	}

	public static Exception DataTableInferenceNotSupported()
	{
		return _InvalidOperation(System.SR.Xml_DataTableInferenceNotSupported);
	}

	internal static void ThrowMultipleTargetConverter(Exception innerException)
	{
		string error = ((innerException != null) ? System.SR.Xml_MultipleTargetConverterError : System.SR.Xml_MultipleTargetConverterEmpty);
		ThrowDataException(error, innerException);
	}

	public static Exception DuplicateDeclaration(string name)
	{
		return _Data(System.SR.Format(System.SR.Xml_MergeDuplicateDeclaration, name));
	}

	public static Exception FoundEntity()
	{
		return _Data(System.SR.Xml_FoundEntity);
	}

	public static Exception MergeFailed(string name)
	{
		return _Data(name);
	}

	public static Exception ConvertFailed(Type type1, Type type2)
	{
		return _Data(System.SR.Format(System.SR.SqlConvert_ConvertFailed, type1.FullName, type2.FullName));
	}

	public static Exception InvalidDataTableReader(string tableName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataTableReader_InvalidDataTableReader, tableName));
	}

	public static Exception DataTableReaderSchemaIsInvalid(string tableName)
	{
		return _InvalidOperation(System.SR.Format(System.SR.DataTableReader_SchemaInvalidDataTableReader, tableName));
	}

	public static Exception CannotCreateDataReaderOnEmptyDataSet()
	{
		return _Argument(System.SR.DataTableReader_CannotCreateDataReaderOnEmptyDataSet);
	}

	public static Exception DataTableReaderArgumentIsEmpty()
	{
		return _Argument(System.SR.DataTableReader_DataTableReaderArgumentIsEmpty);
	}

	public static Exception ArgumentContainsNullValue()
	{
		return _Argument(System.SR.DataTableReader_ArgumentContainsNullValue);
	}

	public static Exception InvalidCurrentRowInDataTableReader()
	{
		return _DeletedRowInaccessible(System.SR.DataTableReader_InvalidRowInDataTableReader);
	}

	public static Exception EmptyDataTableReader(string tableName)
	{
		return _DeletedRowInaccessible(System.SR.Format(System.SR.DataTableReader_DataTableCleared, tableName));
	}

	internal static Exception InvalidDuplicateNamedSimpleTypeDelaration(string stName, string errorStr)
	{
		return _Argument(System.SR.Format(System.SR.NamedSimpleType_InvalidDuplicateNamedSimpleTypeDelaration, stName, errorStr));
	}

	internal static Exception InternalRBTreeError(RBTreeError internalError)
	{
		return _InvalidOperation(System.SR.Format(System.SR.RbTree_InvalidState, (int)internalError));
	}

	public static Exception EnumeratorModified()
	{
		return _InvalidOperation(System.SR.RbTree_EnumerationBroken);
	}
}
