namespace System.Data.Common;

internal sealed class DbSchemaTable
{
	private enum ColumnEnum
	{
		ColumnName,
		ColumnOrdinal,
		ColumnSize,
		BaseServerName,
		BaseCatalogName,
		BaseColumnName,
		BaseSchemaName,
		BaseTableName,
		IsAutoIncrement,
		IsUnique,
		IsKey,
		IsRowVersion,
		DataType,
		ProviderSpecificDataType,
		AllowDBNull,
		ProviderType,
		IsExpression,
		IsHidden,
		IsLong,
		IsReadOnly,
		SchemaMappingUnsortedIndex
	}

	private static readonly string[] s_DBCOLUMN_NAME = new string[21]
	{
		SchemaTableColumn.ColumnName,
		SchemaTableColumn.ColumnOrdinal,
		SchemaTableColumn.ColumnSize,
		SchemaTableOptionalColumn.BaseServerName,
		SchemaTableOptionalColumn.BaseCatalogName,
		SchemaTableColumn.BaseColumnName,
		SchemaTableColumn.BaseSchemaName,
		SchemaTableColumn.BaseTableName,
		SchemaTableOptionalColumn.IsAutoIncrement,
		SchemaTableColumn.IsUnique,
		SchemaTableColumn.IsKey,
		SchemaTableOptionalColumn.IsRowVersion,
		SchemaTableColumn.DataType,
		SchemaTableOptionalColumn.ProviderSpecificDataType,
		SchemaTableColumn.AllowDBNull,
		SchemaTableColumn.ProviderType,
		SchemaTableColumn.IsExpression,
		SchemaTableOptionalColumn.IsHidden,
		SchemaTableColumn.IsLong,
		SchemaTableOptionalColumn.IsReadOnly,
		"SchemaMapping Unsorted Index"
	};

	internal DataTable _dataTable;

	private readonly DataColumnCollection _columns;

	private readonly DataColumn[] _columnCache = new DataColumn[s_DBCOLUMN_NAME.Length];

	private readonly bool _returnProviderSpecificTypes;

	internal DataColumn ColumnName => CachedDataColumn(ColumnEnum.ColumnName);

	internal DataColumn Size => CachedDataColumn(ColumnEnum.ColumnSize);

	internal DataColumn BaseServerName => CachedDataColumn(ColumnEnum.BaseServerName);

	internal DataColumn BaseColumnName => CachedDataColumn(ColumnEnum.BaseColumnName);

	internal DataColumn BaseTableName => CachedDataColumn(ColumnEnum.BaseTableName);

	internal DataColumn BaseCatalogName => CachedDataColumn(ColumnEnum.BaseCatalogName);

	internal DataColumn BaseSchemaName => CachedDataColumn(ColumnEnum.BaseSchemaName);

	internal DataColumn IsAutoIncrement => CachedDataColumn(ColumnEnum.IsAutoIncrement);

	internal DataColumn IsUnique => CachedDataColumn(ColumnEnum.IsUnique);

	internal DataColumn IsKey => CachedDataColumn(ColumnEnum.IsKey);

	internal DataColumn IsRowVersion => CachedDataColumn(ColumnEnum.IsRowVersion);

	internal DataColumn AllowDBNull => CachedDataColumn(ColumnEnum.AllowDBNull);

	internal DataColumn IsExpression => CachedDataColumn(ColumnEnum.IsExpression);

	internal DataColumn IsHidden => CachedDataColumn(ColumnEnum.IsHidden);

	internal DataColumn IsLong => CachedDataColumn(ColumnEnum.IsLong);

	internal DataColumn IsReadOnly => CachedDataColumn(ColumnEnum.IsReadOnly);

	internal DataColumn UnsortedIndex => CachedDataColumn(ColumnEnum.SchemaMappingUnsortedIndex);

	internal DataColumn DataType
	{
		get
		{
			if (_returnProviderSpecificTypes)
			{
				return CachedDataColumn(ColumnEnum.ProviderSpecificDataType, ColumnEnum.DataType);
			}
			return CachedDataColumn(ColumnEnum.DataType);
		}
	}

	internal DbSchemaTable(DataTable dataTable, bool returnProviderSpecificTypes)
	{
		_dataTable = dataTable;
		_columns = dataTable.Columns;
		_returnProviderSpecificTypes = returnProviderSpecificTypes;
	}

	private DataColumn CachedDataColumn(ColumnEnum column)
	{
		return CachedDataColumn(column, column);
	}

	private DataColumn CachedDataColumn(ColumnEnum column, ColumnEnum column2)
	{
		DataColumn dataColumn = _columnCache[(int)column];
		if (dataColumn == null)
		{
			int num = _columns.IndexOf(s_DBCOLUMN_NAME[(int)column]);
			if (-1 == num && column != column2)
			{
				num = _columns.IndexOf(s_DBCOLUMN_NAME[(int)column2]);
			}
			if (-1 != num)
			{
				dataColumn = _columns[num];
				_columnCache[(int)column] = dataColumn;
			}
		}
		return dataColumn;
	}
}
