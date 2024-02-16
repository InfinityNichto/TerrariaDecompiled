using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.Common;

internal sealed class DbSchemaRow
{
	private readonly DbSchemaTable _schemaTable;

	private readonly DataRow _dataRow;

	internal DataRow DataRow => _dataRow;

	internal string ColumnName
	{
		get
		{
			object obj = _dataRow[_schemaTable.ColumnName, DataRowVersion.Default];
			if (!Convert.IsDBNull(obj))
			{
				return Convert.ToString(obj, CultureInfo.InvariantCulture);
			}
			return string.Empty;
		}
	}

	internal int Size
	{
		get
		{
			object value = _dataRow[_schemaTable.Size, DataRowVersion.Default];
			if (!Convert.IsDBNull(value))
			{
				return Convert.ToInt32(value, CultureInfo.InvariantCulture);
			}
			return 0;
		}
	}

	internal string BaseColumnName
	{
		get
		{
			if (_schemaTable.BaseColumnName != null)
			{
				object obj = _dataRow[_schemaTable.BaseColumnName, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return Convert.ToString(obj, CultureInfo.InvariantCulture);
				}
			}
			return string.Empty;
		}
	}

	internal string BaseServerName
	{
		get
		{
			if (_schemaTable.BaseServerName != null)
			{
				object obj = _dataRow[_schemaTable.BaseServerName, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return Convert.ToString(obj, CultureInfo.InvariantCulture);
				}
			}
			return string.Empty;
		}
	}

	internal string BaseCatalogName
	{
		get
		{
			if (_schemaTable.BaseCatalogName != null)
			{
				object obj = _dataRow[_schemaTable.BaseCatalogName, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return Convert.ToString(obj, CultureInfo.InvariantCulture);
				}
			}
			return string.Empty;
		}
	}

	internal string BaseSchemaName
	{
		get
		{
			if (_schemaTable.BaseSchemaName != null)
			{
				object obj = _dataRow[_schemaTable.BaseSchemaName, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return Convert.ToString(obj, CultureInfo.InvariantCulture);
				}
			}
			return string.Empty;
		}
	}

	internal string BaseTableName
	{
		get
		{
			if (_schemaTable.BaseTableName != null)
			{
				object obj = _dataRow[_schemaTable.BaseTableName, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return Convert.ToString(obj, CultureInfo.InvariantCulture);
				}
			}
			return string.Empty;
		}
	}

	internal bool IsAutoIncrement
	{
		get
		{
			if (_schemaTable.IsAutoIncrement != null)
			{
				object value = _dataRow[_schemaTable.IsAutoIncrement, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsUnique
	{
		get
		{
			if (_schemaTable.IsUnique != null)
			{
				object value = _dataRow[_schemaTable.IsUnique, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsRowVersion
	{
		get
		{
			if (_schemaTable.IsRowVersion != null)
			{
				object value = _dataRow[_schemaTable.IsRowVersion, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsKey
	{
		get
		{
			if (_schemaTable.IsKey != null)
			{
				object value = _dataRow[_schemaTable.IsKey, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsExpression
	{
		get
		{
			if (_schemaTable.IsExpression != null)
			{
				object value = _dataRow[_schemaTable.IsExpression, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsHidden
	{
		get
		{
			if (_schemaTable.IsHidden != null)
			{
				object value = _dataRow[_schemaTable.IsHidden, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsLong
	{
		get
		{
			if (_schemaTable.IsLong != null)
			{
				object value = _dataRow[_schemaTable.IsLong, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal bool IsReadOnly
	{
		get
		{
			if (_schemaTable.IsReadOnly != null)
			{
				object value = _dataRow[_schemaTable.IsReadOnly, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return false;
		}
	}

	internal Type DataType
	{
		[RequiresUnreferencedCode("DataRow's DataType cannot be statically analyzed")]
		get
		{
			if (_schemaTable.DataType != null)
			{
				object obj = _dataRow[_schemaTable.DataType, DataRowVersion.Default];
				if (!Convert.IsDBNull(obj))
				{
					return (Type)obj;
				}
			}
			return null;
		}
	}

	internal bool AllowDBNull
	{
		get
		{
			if (_schemaTable.AllowDBNull != null)
			{
				object value = _dataRow[_schemaTable.AllowDBNull, DataRowVersion.Default];
				if (!Convert.IsDBNull(value))
				{
					return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
				}
			}
			return true;
		}
	}

	internal int UnsortedIndex => (int)_dataRow[_schemaTable.UnsortedIndex, DataRowVersion.Default];

	internal static DbSchemaRow[] GetSortedSchemaRows(DataTable dataTable, bool returnProviderSpecificTypes)
	{
		DataColumn dataColumn = dataTable.Columns["SchemaMapping Unsorted Index"];
		if (dataColumn == null)
		{
			dataColumn = new DataColumn("SchemaMapping Unsorted Index", typeof(int));
			dataTable.Columns.Add(dataColumn);
		}
		int count = dataTable.Rows.Count;
		for (int i = 0; i < count; i++)
		{
			dataTable.Rows[i][dataColumn] = i;
		}
		DbSchemaTable schemaTable = new DbSchemaTable(dataTable, returnProviderSpecificTypes);
		DataRow[] array = SelectRows(dataTable, returnProviderSpecificTypes);
		DbSchemaRow[] array2 = new DbSchemaRow[array.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array2[j] = new DbSchemaRow(schemaTable, array[j]);
		}
		return array2;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Filter expression is null.")]
	private static DataRow[] SelectRows(DataTable dataTable, bool returnProviderSpecificTypes)
	{
		return dataTable.Select(null, "ColumnOrdinal ASC", DataViewRowState.CurrentRows);
	}

	internal DbSchemaRow(DbSchemaTable schemaTable, DataRow dataRow)
	{
		_schemaTable = schemaTable;
		_dataRow = dataRow;
	}
}
