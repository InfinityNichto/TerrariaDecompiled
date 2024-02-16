using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Data.Common;

public static class DbDataReaderExtensions
{
	private sealed class DataRowDbColumn : DbColumn
	{
		private readonly DataColumnCollection _schemaColumns;

		private readonly DataRow _schemaRow;

		public DataRowDbColumn(DataRow readerSchemaRow, DataColumnCollection readerSchemaColumns)
		{
			_schemaRow = readerSchemaRow;
			_schemaColumns = readerSchemaColumns;
			PopulateFields();
		}

		private void PopulateFields()
		{
			base.AllowDBNull = GetDbColumnValue<bool?>(SchemaTableColumn.AllowDBNull);
			base.BaseCatalogName = GetDbColumnValue<string>(SchemaTableOptionalColumn.BaseCatalogName);
			base.BaseColumnName = GetDbColumnValue<string>(SchemaTableColumn.BaseColumnName);
			base.BaseSchemaName = GetDbColumnValue<string>(SchemaTableColumn.BaseSchemaName);
			base.BaseServerName = GetDbColumnValue<string>(SchemaTableOptionalColumn.BaseServerName);
			base.BaseTableName = GetDbColumnValue<string>(SchemaTableColumn.BaseTableName);
			base.ColumnName = GetDbColumnValue<string>(SchemaTableColumn.ColumnName);
			base.ColumnOrdinal = GetDbColumnValue<int?>(SchemaTableColumn.ColumnOrdinal);
			base.ColumnSize = GetDbColumnValue<int?>(SchemaTableColumn.ColumnSize);
			base.IsAliased = GetDbColumnValue<bool?>(SchemaTableColumn.IsAliased);
			base.IsAutoIncrement = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsAutoIncrement);
			base.IsExpression = GetDbColumnValue<bool>(SchemaTableColumn.IsExpression);
			base.IsHidden = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsHidden);
			base.IsIdentity = GetDbColumnValue<bool?>("IsIdentity");
			base.IsKey = GetDbColumnValue<bool?>(SchemaTableColumn.IsKey);
			base.IsLong = GetDbColumnValue<bool?>(SchemaTableColumn.IsLong);
			base.IsReadOnly = GetDbColumnValue<bool?>(SchemaTableOptionalColumn.IsReadOnly);
			base.IsUnique = GetDbColumnValue<bool?>(SchemaTableColumn.IsUnique);
			base.NumericPrecision = GetDbColumnValue<int?>(SchemaTableColumn.NumericPrecision);
			base.NumericScale = GetDbColumnValue<int?>(SchemaTableColumn.NumericScale);
			base.UdtAssemblyQualifiedName = GetDbColumnValue<string>("UdtAssemblyQualifiedName");
			base.DataType = GetDbColumnValue<Type>(SchemaTableColumn.DataType);
			base.DataTypeName = GetDbColumnValue<string>("DataTypeName");
		}

		private T GetDbColumnValue<T>(string columnName)
		{
			if (_schemaColumns.Contains(columnName))
			{
				object obj = _schemaRow[columnName];
				if (obj is T)
				{
					return (T)obj;
				}
			}
			return default(T);
		}
	}

	public static ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (reader is IDbColumnSchemaGenerator dbColumnSchemaGenerator)
		{
			return dbColumnSchemaGenerator.GetColumnSchema();
		}
		return GetColumnSchemaCompatibility(reader);
	}

	public static bool CanGetColumnSchema(this DbDataReader reader)
	{
		return true;
	}

	private static ReadOnlyCollection<DbColumn> GetColumnSchemaCompatibility(DbDataReader reader)
	{
		List<DbColumn> list = new List<DbColumn>();
		DataTable schemaTable = reader.GetSchemaTable();
		if (schemaTable != null)
		{
			DataColumnCollection columns = schemaTable.Columns;
			foreach (DataRow row in schemaTable.Rows)
			{
				list.Add(new DataRowDbColumn(row, columns));
			}
		}
		return new ReadOnlyCollection<DbColumn>(list);
	}
}
