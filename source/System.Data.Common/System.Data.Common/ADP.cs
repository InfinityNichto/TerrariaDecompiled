using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common;

internal static class ADP
{
	internal enum InternalErrorCode
	{
		UnpooledObjectHasOwner = 0,
		UnpooledObjectHasWrongOwner = 1,
		PushingObjectSecondTime = 2,
		PooledObjectHasOwner = 3,
		PooledObjectInPoolMoreThanOnce = 4,
		CreateObjectReturnedNull = 5,
		NewObjectCannotBePooled = 6,
		NonPooledObjectUsedMoreThanOnce = 7,
		AttemptingToPoolOnRestrictedToken = 8,
		ConvertSidToStringSidWReturnedNull = 10,
		AttemptingToConstructReferenceCollectionOnStaticObject = 12,
		AttemptingToEnlistTwice = 13,
		CreateReferenceCollectionReturnedNull = 14,
		PooledObjectWithoutPool = 15,
		UnexpectedWaitAnyResult = 16,
		SynchronousConnectReturnedPending = 17,
		CompletedConnectReturnedPending = 18,
		NameValuePairNext = 20,
		InvalidParserState1 = 21,
		InvalidParserState2 = 22,
		InvalidParserState3 = 23,
		InvalidBuffer = 30,
		UnimplementedSMIMethod = 40,
		InvalidSmiCall = 41,
		SqlDependencyObtainProcessDispatcherFailureObjectHandle = 50,
		SqlDependencyProcessDispatcherFailureCreateInstance = 51,
		SqlDependencyProcessDispatcherFailureAppDomain = 52,
		SqlDependencyCommandHashIsNotAssociatedWithNotification = 53,
		UnknownTransactionFailure = 60
	}

	private static Task<bool> _trueTask;

	private static Task<bool> _falseTask;

	private static readonly Type s_stackOverflowType = typeof(StackOverflowException);

	private static readonly Type s_outOfMemoryType = typeof(OutOfMemoryException);

	private static readonly Type s_threadAbortType = typeof(ThreadAbortException);

	private static readonly Type s_nullReferenceType = typeof(NullReferenceException);

	private static readonly Type s_accessViolationType = typeof(AccessViolationException);

	private static readonly Type s_securityType = typeof(SecurityException);

	internal static Task<bool> TrueTask => _trueTask ?? (_trueTask = Task.FromResult(result: true));

	internal static Task<bool> FalseTask => _falseTask ?? (_falseTask = Task.FromResult(result: false));

	internal static Task<T> CreatedTaskWithCancellation<T>()
	{
		return Task.FromCanceled<T>(new CancellationToken(canceled: true));
	}

	internal static void TraceExceptionForCapture(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
	}

	internal static DataException Data(string message)
	{
		DataException ex = new DataException(message);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static void CheckArgumentLength(string value, string parameterName)
	{
		CheckArgumentNull(value, parameterName);
		if (value.Length == 0)
		{
			throw Argument(System.SR.Format(System.SR.ADP_EmptyString, parameterName));
		}
	}

	internal static void CheckArgumentLength(Array value, string parameterName)
	{
		CheckArgumentNull(value, parameterName);
		if (value.Length == 0)
		{
			throw Argument(System.SR.Format(System.SR.ADP_EmptyArray, parameterName));
		}
	}

	internal static ArgumentOutOfRangeException InvalidAcceptRejectRule(AcceptRejectRule value)
	{
		return InvalidEnumerationValue(typeof(AcceptRejectRule), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidCatalogLocation(CatalogLocation value)
	{
		return InvalidEnumerationValue(typeof(CatalogLocation), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidConflictOptions(ConflictOption value)
	{
		return InvalidEnumerationValue(typeof(ConflictOption), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidDataRowState(DataRowState value)
	{
		return InvalidEnumerationValue(typeof(DataRowState), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidLoadOption(LoadOption value)
	{
		return InvalidEnumerationValue(typeof(LoadOption), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidMissingMappingAction(MissingMappingAction value)
	{
		return InvalidEnumerationValue(typeof(MissingMappingAction), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidMissingSchemaAction(MissingSchemaAction value)
	{
		return InvalidEnumerationValue(typeof(MissingSchemaAction), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidRule(Rule value)
	{
		return InvalidEnumerationValue(typeof(Rule), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidSchemaType(SchemaType value)
	{
		return InvalidEnumerationValue(typeof(SchemaType), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidStatementType(StatementType value)
	{
		return InvalidEnumerationValue(typeof(StatementType), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidUpdateStatus(UpdateStatus value)
	{
		return InvalidEnumerationValue(typeof(UpdateStatus), (int)value);
	}

	internal static ArgumentOutOfRangeException NotSupportedStatementType(StatementType value, string method)
	{
		return NotSupportedEnumerationValue(typeof(StatementType), value.ToString(), method);
	}

	internal static ArgumentException InvalidKeyname(string parameterName)
	{
		return Argument(System.SR.ADP_InvalidKey, parameterName);
	}

	internal static ArgumentException InvalidValue(string parameterName)
	{
		return Argument(System.SR.ADP_InvalidValue, parameterName);
	}

	internal static Exception WrongType(Type got, Type expected)
	{
		return Argument(System.SR.Format(System.SR.SQL_WrongType, got, expected));
	}

	internal static Exception CollectionUniqueValue(Type itemType, string propertyName, string propertyValue)
	{
		return Argument(System.SR.Format(System.SR.ADP_CollectionUniqueValue, itemType.Name, propertyName, propertyValue));
	}

	internal static InvalidOperationException MissingSelectCommand(string method)
	{
		return Provider(System.SR.Format(System.SR.ADP_MissingSelectCommand, method));
	}

	private static InvalidOperationException DataMapping(string error)
	{
		return InvalidOperation(error);
	}

	internal static InvalidOperationException ColumnSchemaExpression(string srcColumn, string cacheColumn)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_ColumnSchemaExpression, srcColumn, cacheColumn));
	}

	internal static InvalidOperationException ColumnSchemaMismatch(string srcColumn, Type srcType, DataColumn column)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_ColumnSchemaMismatch, srcColumn, srcType.Name, column.ColumnName, column.DataType.Name));
	}

	internal static InvalidOperationException ColumnSchemaMissing(string cacheColumn, string tableName, string srcColumn)
	{
		if (string.IsNullOrEmpty(tableName))
		{
			return InvalidOperation(System.SR.Format(System.SR.ADP_ColumnSchemaMissing1, cacheColumn, tableName, srcColumn));
		}
		return DataMapping(System.SR.Format(System.SR.ADP_ColumnSchemaMissing2, cacheColumn, tableName, srcColumn));
	}

	internal static InvalidOperationException MissingColumnMapping(string srcColumn)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_MissingColumnMapping, srcColumn));
	}

	internal static InvalidOperationException MissingTableSchema(string cacheTable, string srcTable)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_MissingTableSchema, cacheTable, srcTable));
	}

	internal static InvalidOperationException MissingTableMapping(string srcTable)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_MissingTableMapping, srcTable));
	}

	internal static InvalidOperationException MissingTableMappingDestination(string dstTable)
	{
		return DataMapping(System.SR.Format(System.SR.ADP_MissingTableMappingDestination, dstTable));
	}

	internal static Exception InvalidSourceColumn(string parameter)
	{
		return Argument(System.SR.ADP_InvalidSourceColumn, parameter);
	}

	internal static Exception ColumnsAddNullAttempt(string parameter)
	{
		return CollectionNullValue(parameter, typeof(DataColumnMappingCollection), typeof(DataColumnMapping));
	}

	internal static Exception ColumnsDataSetColumn(string cacheColumn)
	{
		return CollectionIndexString(typeof(DataColumnMapping), "DataSetColumn", cacheColumn, typeof(DataColumnMappingCollection));
	}

	internal static Exception ColumnsIndexInt32(int index, IColumnMappingCollection collection)
	{
		return CollectionIndexInt32(index, collection.GetType(), collection.Count);
	}

	internal static Exception ColumnsIndexSource(string srcColumn)
	{
		return CollectionIndexString(typeof(DataColumnMapping), "SourceColumn", srcColumn, typeof(DataColumnMappingCollection));
	}

	internal static Exception ColumnsIsNotParent(ICollection collection)
	{
		return ParametersIsNotParent(typeof(DataColumnMapping), collection);
	}

	internal static Exception ColumnsIsParent(ICollection collection)
	{
		return ParametersIsParent(typeof(DataColumnMapping), collection);
	}

	internal static Exception ColumnsUniqueSourceColumn(string srcColumn)
	{
		return CollectionUniqueValue(typeof(DataColumnMapping), "SourceColumn", srcColumn);
	}

	internal static Exception NotADataColumnMapping(object value)
	{
		return CollectionInvalidType(typeof(DataColumnMappingCollection), typeof(DataColumnMapping), value);
	}

	internal static Exception InvalidSourceTable(string parameter)
	{
		return Argument(System.SR.ADP_InvalidSourceTable, parameter);
	}

	internal static Exception TablesAddNullAttempt(string parameter)
	{
		return CollectionNullValue(parameter, typeof(DataTableMappingCollection), typeof(DataTableMapping));
	}

	internal static Exception TablesDataSetTable(string cacheTable)
	{
		return CollectionIndexString(typeof(DataTableMapping), "DataSetTable", cacheTable, typeof(DataTableMappingCollection));
	}

	internal static Exception TablesIndexInt32(int index, ITableMappingCollection collection)
	{
		return CollectionIndexInt32(index, collection.GetType(), collection.Count);
	}

	internal static Exception TablesIsNotParent(ICollection collection)
	{
		return ParametersIsNotParent(typeof(DataTableMapping), collection);
	}

	internal static Exception TablesIsParent(ICollection collection)
	{
		return ParametersIsParent(typeof(DataTableMapping), collection);
	}

	internal static Exception TablesSourceIndex(string srcTable)
	{
		return CollectionIndexString(typeof(DataTableMapping), "SourceTable", srcTable, typeof(DataTableMappingCollection));
	}

	internal static Exception TablesUniqueSourceTable(string srcTable)
	{
		return CollectionUniqueValue(typeof(DataTableMapping), "SourceTable", srcTable);
	}

	internal static Exception NotADataTableMapping(object value)
	{
		return CollectionInvalidType(typeof(DataTableMappingCollection), typeof(DataTableMapping), value);
	}

	internal static InvalidOperationException UpdateConnectionRequired(StatementType statementType, bool isRowUpdatingCommand)
	{
		string error;
		if (isRowUpdatingCommand)
		{
			error = System.SR.ADP_ConnectionRequired_Clone;
		}
		else
		{
			switch (statementType)
			{
			case StatementType.Insert:
				error = System.SR.ADP_ConnectionRequired_Insert;
				break;
			case StatementType.Update:
				error = System.SR.ADP_ConnectionRequired_Update;
				break;
			case StatementType.Delete:
				error = System.SR.ADP_ConnectionRequired_Delete;
				break;
			case StatementType.Batch:
				error = System.SR.ADP_ConnectionRequired_Batch;
				goto default;
			default:
				throw InvalidStatementType(statementType);
			}
		}
		return InvalidOperation(error);
	}

	internal static InvalidOperationException ConnectionRequired_Res(string method)
	{
		return InvalidOperation("ADP_ConnectionRequired_" + method);
	}

	internal static InvalidOperationException UpdateOpenConnectionRequired(StatementType statementType, bool isRowUpdatingCommand, ConnectionState state)
	{
		string resourceFormat = (isRowUpdatingCommand ? System.SR.ADP_OpenConnectionRequired_Clone : (statementType switch
		{
			StatementType.Insert => System.SR.ADP_OpenConnectionRequired_Insert, 
			StatementType.Update => System.SR.ADP_OpenConnectionRequired_Update, 
			StatementType.Delete => System.SR.ADP_OpenConnectionRequired_Delete, 
			_ => throw InvalidStatementType(statementType), 
		}));
		return InvalidOperation(System.SR.Format(resourceFormat, ConnectionStateMsg(state)));
	}

	internal static ArgumentException UnwantedStatementType(StatementType statementType)
	{
		return Argument(System.SR.Format(System.SR.ADP_UnwantedStatementType, statementType.ToString()));
	}

	internal static Exception FillSchemaRequiresSourceTableName(string parameter)
	{
		return Argument(System.SR.ADP_FillSchemaRequiresSourceTableName, parameter);
	}

	internal static Exception InvalidMaxRecords(string parameter, int max)
	{
		return Argument(System.SR.Format(System.SR.ADP_InvalidMaxRecords, max.ToString(CultureInfo.InvariantCulture)), parameter);
	}

	internal static Exception InvalidStartRecord(string parameter, int start)
	{
		return Argument(System.SR.Format(System.SR.ADP_InvalidStartRecord, start.ToString(CultureInfo.InvariantCulture)), parameter);
	}

	internal static Exception FillRequires(string parameter)
	{
		return ArgumentNull(parameter);
	}

	internal static Exception FillRequiresSourceTableName(string parameter)
	{
		return Argument(System.SR.ADP_FillRequiresSourceTableName, parameter);
	}

	internal static Exception FillChapterAutoIncrement()
	{
		return InvalidOperation(System.SR.ADP_FillChapterAutoIncrement);
	}

	internal static InvalidOperationException MissingDataReaderFieldType(int index)
	{
		return DataAdapter(System.SR.Format(System.SR.ADP_MissingDataReaderFieldType, index));
	}

	internal static InvalidOperationException OnlyOneTableForStartRecordOrMaxRecords()
	{
		return DataAdapter(System.SR.ADP_OnlyOneTableForStartRecordOrMaxRecords);
	}

	internal static ArgumentNullException UpdateRequiresNonNullDataSet(string parameter)
	{
		return ArgumentNull(parameter);
	}

	internal static InvalidOperationException UpdateRequiresSourceTable(string defaultSrcTableName)
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_UpdateRequiresSourceTable, defaultSrcTableName));
	}

	internal static InvalidOperationException UpdateRequiresSourceTableName(string srcTable)
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_UpdateRequiresSourceTableName, srcTable));
	}

	internal static ArgumentNullException UpdateRequiresDataTable(string parameter)
	{
		return ArgumentNull(parameter);
	}

	internal static Exception UpdateConcurrencyViolation(StatementType statementType, int affected, int expected, DataRow[] dataRows)
	{
		DBConcurrencyException ex = new DBConcurrencyException(System.SR.Format(statementType switch
		{
			StatementType.Update => System.SR.ADP_UpdateConcurrencyViolation_Update, 
			StatementType.Delete => System.SR.ADP_UpdateConcurrencyViolation_Delete, 
			StatementType.Batch => System.SR.ADP_UpdateConcurrencyViolation_Batch, 
			_ => throw InvalidStatementType(statementType), 
		}, affected.ToString(CultureInfo.InvariantCulture), expected.ToString(CultureInfo.InvariantCulture)), null, dataRows);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static InvalidOperationException UpdateRequiresCommand(StatementType statementType, bool isRowUpdatingCommand)
	{
		string error = (isRowUpdatingCommand ? System.SR.ADP_UpdateRequiresCommandClone : (statementType switch
		{
			StatementType.Select => System.SR.ADP_UpdateRequiresCommandSelect, 
			StatementType.Insert => System.SR.ADP_UpdateRequiresCommandInsert, 
			StatementType.Update => System.SR.ADP_UpdateRequiresCommandUpdate, 
			StatementType.Delete => System.SR.ADP_UpdateRequiresCommandDelete, 
			_ => throw InvalidStatementType(statementType), 
		}));
		return InvalidOperation(error);
	}

	internal static ArgumentException UpdateMismatchRowTable(int i)
	{
		return Argument(System.SR.Format(System.SR.ADP_UpdateMismatchRowTable, i.ToString(CultureInfo.InvariantCulture)));
	}

	internal static DataException RowUpdatedErrors()
	{
		return Data(System.SR.ADP_RowUpdatedErrors);
	}

	internal static DataException RowUpdatingErrors()
	{
		return Data(System.SR.ADP_RowUpdatingErrors);
	}

	internal static InvalidOperationException ResultsNotAllowedDuringBatch()
	{
		return DataAdapter(System.SR.ADP_ResultsNotAllowedDuringBatch);
	}

	internal static InvalidOperationException DynamicSQLJoinUnsupported()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLJoinUnsupported);
	}

	internal static InvalidOperationException DynamicSQLNoTableInfo()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLNoTableInfo);
	}

	internal static InvalidOperationException DynamicSQLNoKeyInfoDelete()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLNoKeyInfoDelete);
	}

	internal static InvalidOperationException DynamicSQLNoKeyInfoUpdate()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLNoKeyInfoUpdate);
	}

	internal static InvalidOperationException DynamicSQLNoKeyInfoRowVersionDelete()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLNoKeyInfoRowVersionDelete);
	}

	internal static InvalidOperationException DynamicSQLNoKeyInfoRowVersionUpdate()
	{
		return InvalidOperation(System.SR.ADP_DynamicSQLNoKeyInfoRowVersionUpdate);
	}

	internal static InvalidOperationException DynamicSQLNestedQuote(string name, string quote)
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_DynamicSQLNestedQuote, name, quote));
	}

	internal static InvalidOperationException NoQuoteChange()
	{
		return InvalidOperation(System.SR.ADP_NoQuoteChange);
	}

	internal static InvalidOperationException MissingSourceCommand()
	{
		return InvalidOperation(System.SR.ADP_MissingSourceCommand);
	}

	internal static InvalidOperationException MissingSourceCommandConnection()
	{
		return InvalidOperation(System.SR.ADP_MissingSourceCommandConnection);
	}

	internal static DataRow[] SelectAdapterRows(DataTable dataTable, bool sorted)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		DataRowCollection rows = dataTable.Rows;
		foreach (DataRow item in rows)
		{
			switch (item.RowState)
			{
			case DataRowState.Added:
				num++;
				break;
			case DataRowState.Deleted:
				num2++;
				break;
			case DataRowState.Modified:
				num3++;
				break;
			}
		}
		DataRow[] array = new DataRow[num + num2 + num3];
		if (sorted)
		{
			num3 = num + num2;
			num2 = num;
			num = 0;
			foreach (DataRow item2 in rows)
			{
				switch (item2.RowState)
				{
				case DataRowState.Added:
					array[num++] = item2;
					break;
				case DataRowState.Deleted:
					array[num2++] = item2;
					break;
				case DataRowState.Modified:
					array[num3++] = item2;
					break;
				}
			}
		}
		else
		{
			int num4 = 0;
			foreach (DataRow item3 in rows)
			{
				if ((item3.RowState & (DataRowState.Added | DataRowState.Deleted | DataRowState.Modified)) != 0)
				{
					array[num4++] = item3;
					if (num4 == array.Length)
					{
						break;
					}
				}
			}
		}
		return array;
	}

	internal static void BuildSchemaTableInfoTableNames(string[] columnNameArray)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(columnNameArray.Length);
		int num = columnNameArray.Length;
		int num2 = columnNameArray.Length - 1;
		while (0 <= num2)
		{
			string text = columnNameArray[num2];
			if (text != null && 0 < text.Length)
			{
				text = text.ToLowerInvariant();
				if (dictionary.TryGetValue(text, out var value))
				{
					num = Math.Min(num, value);
				}
				dictionary[text] = num2;
			}
			else
			{
				columnNameArray[num2] = string.Empty;
				num = num2;
			}
			num2--;
		}
		int uniqueIndex = 1;
		for (int i = num; i < columnNameArray.Length; i++)
		{
			string text2 = columnNameArray[i];
			if (text2.Length == 0)
			{
				columnNameArray[i] = "Column";
				uniqueIndex = GenerateUniqueName(dictionary, ref columnNameArray[i], i, uniqueIndex);
				continue;
			}
			text2 = text2.ToLowerInvariant();
			if (i != dictionary[text2])
			{
				GenerateUniqueName(dictionary, ref columnNameArray[i], i, 1);
			}
		}
	}

	private static int GenerateUniqueName(Dictionary<string, int> hash, ref string columnName, int index, int uniqueIndex)
	{
		string text;
		while (true)
		{
			text = columnName + uniqueIndex.ToString(CultureInfo.InvariantCulture);
			string key = text.ToLowerInvariant();
			if (hash.TryAdd(key, index))
			{
				break;
			}
			uniqueIndex++;
		}
		columnName = text;
		return uniqueIndex;
	}

	internal static int SrcCompare(string strA, string strB)
	{
		if (!(strA == strB))
		{
			return 1;
		}
		return 0;
	}

	private static void TraceException(string trace, Exception e)
	{
		if (e != null)
		{
			DataCommonEventSource.Log.Trace(trace, e);
		}
	}

	internal static void TraceExceptionAsReturnValue(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|THROW> '{0}'", e);
	}

	internal static void TraceExceptionWithoutRethrow(Exception e)
	{
		TraceException("<comm.ADP.TraceException|ERR|CATCH> '%ls'\n", e);
	}

	internal static ArgumentException Argument(string error)
	{
		ArgumentException ex = new ArgumentException(error);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentException Argument(string error, Exception inner)
	{
		ArgumentException ex = new ArgumentException(error, inner);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentException Argument(string error, string parameter)
	{
		ArgumentException ex = new ArgumentException(error, parameter);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentNullException ArgumentNull(string parameter)
	{
		ArgumentNullException ex = new ArgumentNullException(parameter);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentNullException ArgumentNull(string parameter, string error)
	{
		ArgumentNullException ex = new ArgumentNullException(parameter, error);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName)
	{
		ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(parameterName);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
	{
		ArgumentOutOfRangeException ex = new ArgumentOutOfRangeException(parameterName, message);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static IndexOutOfRangeException IndexOutOfRange(string error)
	{
		IndexOutOfRangeException ex = new IndexOutOfRangeException(error);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static InvalidCastException InvalidCast(string error)
	{
		return InvalidCast(error, null);
	}

	internal static InvalidCastException InvalidCast(string error, Exception inner)
	{
		InvalidCastException ex = new InvalidCastException(error, inner);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static InvalidOperationException InvalidOperation(string error)
	{
		InvalidOperationException ex = new InvalidOperationException(error);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static NotSupportedException NotSupported()
	{
		NotSupportedException ex = new NotSupportedException();
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static NotSupportedException NotSupported(string error)
	{
		NotSupportedException ex = new NotSupportedException(error);
		TraceExceptionAsReturnValue(ex);
		return ex;
	}

	internal static ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, string value, string method)
	{
		return ArgumentOutOfRange(System.SR.Format(System.SR.ADP_NotSupportedEnumerationValue, type.Name, value, method), type.Name);
	}

	internal static InvalidOperationException DataAdapter(string error)
	{
		return InvalidOperation(error);
	}

	private static InvalidOperationException Provider(string error)
	{
		return InvalidOperation(error);
	}

	internal static void CheckArgumentNull([NotNull] object value, string parameterName)
	{
		if (value == null)
		{
			throw ArgumentNull(parameterName);
		}
	}

	internal static bool IsCatchableExceptionType(Exception e)
	{
		Type type = e.GetType();
		if (type != s_stackOverflowType && type != s_outOfMemoryType && type != s_threadAbortType && type != s_nullReferenceType && type != s_accessViolationType)
		{
			return !s_securityType.IsAssignableFrom(type);
		}
		return false;
	}

	internal static bool IsCatchableOrSecurityExceptionType(Exception e)
	{
		Type type = e.GetType();
		if (type != s_stackOverflowType && type != s_outOfMemoryType && type != s_threadAbortType && type != s_nullReferenceType)
		{
			return type != s_accessViolationType;
		}
		return false;
	}

	internal static ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
	{
		return ArgumentOutOfRange(System.SR.Format(System.SR.ADP_InvalidEnumerationValue, type.Name, value.ToString(CultureInfo.InvariantCulture)), type.Name);
	}

	internal static ArgumentException ConnectionStringSyntax(int index)
	{
		return Argument(System.SR.Format(System.SR.ADP_ConnectionStringSyntax, index));
	}

	internal static ArgumentException KeywordNotSupported(string keyword)
	{
		return Argument(System.SR.Format(System.SR.ADP_KeywordNotSupported, keyword));
	}

	internal static ArgumentException ConvertFailed(Type fromType, Type toType, Exception innerException)
	{
		return Argument(System.SR.Format(System.SR.SqlConvert_ConvertFailed, fromType.FullName, toType.FullName), innerException);
	}

	internal static ArgumentException CollectionRemoveInvalidObject(Type itemType, ICollection collection)
	{
		return Argument(System.SR.Format(System.SR.ADP_CollectionRemoveInvalidObject, itemType.Name, collection.GetType().Name));
	}

	internal static ArgumentNullException CollectionNullValue(string parameter, Type collection, Type itemType)
	{
		return ArgumentNull(parameter, System.SR.Format(System.SR.ADP_CollectionNullValue, collection.Name, itemType.Name));
	}

	internal static IndexOutOfRangeException CollectionIndexInt32(int index, Type collection, int count)
	{
		return IndexOutOfRange(System.SR.Format(System.SR.ADP_CollectionIndexInt32, index.ToString(CultureInfo.InvariantCulture), collection.Name, count.ToString(CultureInfo.InvariantCulture)));
	}

	internal static IndexOutOfRangeException CollectionIndexString(Type itemType, string propertyName, string propertyValue, Type collection)
	{
		return IndexOutOfRange(System.SR.Format(System.SR.ADP_CollectionIndexString, itemType.Name, propertyName, propertyValue, collection.Name));
	}

	internal static InvalidCastException CollectionInvalidType(Type collection, Type itemType, object invalidValue)
	{
		return InvalidCast(System.SR.Format(System.SR.ADP_CollectionInvalidType, collection.Name, itemType.Name, invalidValue.GetType().Name));
	}

	private static string ConnectionStateMsg(ConnectionState state)
	{
		switch (state)
		{
		case ConnectionState.Closed:
		case ConnectionState.Connecting | ConnectionState.Broken:
			return System.SR.ADP_ConnectionStateMsg_Closed;
		case ConnectionState.Connecting:
			return System.SR.ADP_ConnectionStateMsg_Connecting;
		case ConnectionState.Open:
			return System.SR.ADP_ConnectionStateMsg_Open;
		case ConnectionState.Open | ConnectionState.Executing:
			return System.SR.ADP_ConnectionStateMsg_OpenExecuting;
		case ConnectionState.Open | ConnectionState.Fetching:
			return System.SR.ADP_ConnectionStateMsg_OpenFetching;
		default:
			return System.SR.Format(System.SR.ADP_ConnectionStateMsg, state.ToString());
		}
	}

	internal static Exception StreamClosed([CallerMemberName] string method = "")
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_StreamClosed, method));
	}

	internal static string BuildQuotedString(string quotePrefix, string quoteSuffix, string unQuotedString)
	{
		StringBuilder stringBuilder = new StringBuilder(unQuotedString.Length + quoteSuffix.Length + quoteSuffix.Length);
		AppendQuotedString(stringBuilder, quotePrefix, quoteSuffix, unQuotedString);
		return stringBuilder.ToString();
	}

	internal static string AppendQuotedString(StringBuilder buffer, string quotePrefix, string quoteSuffix, string unQuotedString)
	{
		if (!string.IsNullOrEmpty(quotePrefix))
		{
			buffer.Append(quotePrefix);
		}
		if (!string.IsNullOrEmpty(quoteSuffix))
		{
			int length = buffer.Length;
			buffer.Append(unQuotedString);
			buffer.Replace(quoteSuffix, quoteSuffix + quoteSuffix, length, unQuotedString.Length);
			buffer.Append(quoteSuffix);
		}
		else
		{
			buffer.Append(unQuotedString);
		}
		return buffer.ToString();
	}

	internal static ArgumentException ParametersIsNotParent(Type parameterType, ICollection collection)
	{
		return Argument(System.SR.Format(System.SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
	}

	internal static ArgumentException ParametersIsParent(Type parameterType, ICollection collection)
	{
		return Argument(System.SR.Format(System.SR.ADP_CollectionIsNotParent, parameterType.Name, collection.GetType().Name));
	}

	internal static Exception InternalError(InternalErrorCode internalError)
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_InternalProviderError, (int)internalError));
	}

	internal static Exception DataReaderClosed([CallerMemberName] string method = "")
	{
		return InvalidOperation(System.SR.Format(System.SR.ADP_DataReaderClosed, method));
	}

	internal static ArgumentOutOfRangeException InvalidSourceBufferIndex(int maxLen, long srcOffset, string parameterName)
	{
		return ArgumentOutOfRange(System.SR.Format(System.SR.ADP_InvalidSourceBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), srcOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
	}

	internal static ArgumentOutOfRangeException InvalidDestinationBufferIndex(int maxLen, int dstOffset, string parameterName)
	{
		return ArgumentOutOfRange(System.SR.Format(System.SR.ADP_InvalidDestinationBufferIndex, maxLen.ToString(CultureInfo.InvariantCulture), dstOffset.ToString(CultureInfo.InvariantCulture)), parameterName);
	}

	internal static IndexOutOfRangeException InvalidBufferSizeOrIndex(int numBytes, int bufferIndex)
	{
		return IndexOutOfRange(System.SR.Format(System.SR.SQL_InvalidBufferSizeOrIndex, numBytes.ToString(CultureInfo.InvariantCulture), bufferIndex.ToString(CultureInfo.InvariantCulture)));
	}

	internal static Exception InvalidDataLength(long length)
	{
		return IndexOutOfRange(System.SR.Format(System.SR.SQL_InvalidDataLength, length.ToString(CultureInfo.InvariantCulture)));
	}

	internal static bool CompareInsensitiveInvariant(string strvalue, string strconst)
	{
		return CultureInfo.InvariantCulture.CompareInfo.Compare(strvalue, strconst, CompareOptions.IgnoreCase) == 0;
	}

	internal static int DstCompare(string strA, string strB)
	{
		return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
	}

	internal static bool IsNull(object value)
	{
		if (value == null || DBNull.Value == value)
		{
			return true;
		}
		if (value is INullable nullable)
		{
			return nullable.IsNull;
		}
		return false;
	}

	internal static Exception InvalidSeekOrigin(string parameterName)
	{
		return ArgumentOutOfRange(System.SR.ADP_InvalidSeekOrigin, parameterName);
	}
}
