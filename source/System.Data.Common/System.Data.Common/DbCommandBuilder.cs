using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Data.Common;

public abstract class DbCommandBuilder : Component
{
	private sealed class ParameterNames
	{
		private string _originalPrefix;

		private string _isNullPrefix;

		private readonly Regex _parameterNameParser;

		private readonly DbCommandBuilder _dbCommandBuilder;

		private readonly string[] _baseParameterNames;

		private readonly string[] _originalParameterNames;

		private readonly string[] _nullParameterNames;

		private readonly bool[] _isMutatedName;

		private readonly int _count;

		private int _genericParameterCount;

		private readonly int _adjustedParameterNameMaxLength;

		internal ParameterNames(DbCommandBuilder dbCommandBuilder, DbSchemaRow[] schemaRows)
		{
			_dbCommandBuilder = dbCommandBuilder;
			_baseParameterNames = new string[schemaRows.Length];
			_originalParameterNames = new string[schemaRows.Length];
			_nullParameterNames = new string[schemaRows.Length];
			_isMutatedName = new bool[schemaRows.Length];
			_count = schemaRows.Length;
			_parameterNameParser = new Regex(_dbCommandBuilder.ParameterNamePattern, RegexOptions.ExplicitCapture | RegexOptions.Singleline);
			SetAndValidateNamePrefixes();
			_adjustedParameterNameMaxLength = GetAdjustedParameterNameMaxLength();
			for (int i = 0; i < schemaRows.Length; i++)
			{
				DbSchemaRow dbSchemaRow = schemaRows[i];
				if (dbSchemaRow == null)
				{
					continue;
				}
				bool flag = false;
				string text = dbSchemaRow.ColumnName;
				if ((_originalPrefix == null || !text.StartsWith(_originalPrefix, StringComparison.OrdinalIgnoreCase)) && (_isNullPrefix == null || !text.StartsWith(_isNullPrefix, StringComparison.OrdinalIgnoreCase)))
				{
					if (text.Contains(' '))
					{
						text = text.Replace(' ', '_');
						flag = true;
					}
					if (_parameterNameParser.IsMatch(text) && text.Length <= _adjustedParameterNameMaxLength)
					{
						_baseParameterNames[i] = text;
						_isMutatedName[i] = flag;
					}
				}
			}
			EliminateConflictingNames();
			for (int j = 0; j < schemaRows.Length; j++)
			{
				if (_baseParameterNames[j] != null)
				{
					if (_originalPrefix != null)
					{
						_originalParameterNames[j] = _originalPrefix + _baseParameterNames[j];
					}
					if (_isNullPrefix != null && schemaRows[j].AllowDBNull)
					{
						_nullParameterNames[j] = _isNullPrefix + _baseParameterNames[j];
					}
				}
			}
			ApplyProviderSpecificFormat();
			GenerateMissingNames(schemaRows);
		}

		private void SetAndValidateNamePrefixes()
		{
			if (_parameterNameParser.IsMatch("IsNull_"))
			{
				_isNullPrefix = "IsNull_";
			}
			else if (_parameterNameParser.IsMatch("isnull"))
			{
				_isNullPrefix = "isnull";
			}
			else if (_parameterNameParser.IsMatch("ISNULL"))
			{
				_isNullPrefix = "ISNULL";
			}
			else
			{
				_isNullPrefix = null;
			}
			if (_parameterNameParser.IsMatch("Original_"))
			{
				_originalPrefix = "Original_";
			}
			else if (_parameterNameParser.IsMatch("original"))
			{
				_originalPrefix = "original";
			}
			else if (_parameterNameParser.IsMatch("ORIGINAL"))
			{
				_originalPrefix = "ORIGINAL";
			}
			else
			{
				_originalPrefix = null;
			}
		}

		private void ApplyProviderSpecificFormat()
		{
			for (int i = 0; i < _baseParameterNames.Length; i++)
			{
				string text = _baseParameterNames[i];
				if (text != null)
				{
					_baseParameterNames[i] = _dbCommandBuilder.GetParameterName(text);
				}
				string text2 = _originalParameterNames[i];
				if (text2 != null)
				{
					_originalParameterNames[i] = _dbCommandBuilder.GetParameterName(text2);
				}
				string text3 = _nullParameterNames[i];
				if (text3 != null)
				{
					_nullParameterNames[i] = _dbCommandBuilder.GetParameterName(text3);
				}
			}
		}

		private void EliminateConflictingNames()
		{
			for (int i = 0; i < _count - 1; i++)
			{
				string text = _baseParameterNames[i];
				if (text == null)
				{
					continue;
				}
				for (int j = i + 1; j < _count; j++)
				{
					if (ADP.CompareInsensitiveInvariant(text, _baseParameterNames[j]))
					{
						int num = (_isMutatedName[j] ? j : i);
						_baseParameterNames[num] = null;
					}
				}
			}
		}

		internal void GenerateMissingNames(DbSchemaRow[] schemaRows)
		{
			for (int i = 0; i < _baseParameterNames.Length; i++)
			{
				string text = _baseParameterNames[i];
				if (text == null)
				{
					_baseParameterNames[i] = GetNextGenericParameterName();
					_originalParameterNames[i] = GetNextGenericParameterName();
					DbSchemaRow dbSchemaRow = schemaRows[i];
					if (dbSchemaRow != null && dbSchemaRow.AllowDBNull)
					{
						_nullParameterNames[i] = GetNextGenericParameterName();
					}
				}
			}
		}

		private int GetAdjustedParameterNameMaxLength()
		{
			int num = Math.Max((_isNullPrefix != null) ? _isNullPrefix.Length : 0, (_originalPrefix != null) ? _originalPrefix.Length : 0) + _dbCommandBuilder.GetParameterName("").Length;
			return _dbCommandBuilder.ParameterNameMaxLength - num;
		}

		private string GetNextGenericParameterName()
		{
			bool flag;
			string parameterName;
			do
			{
				flag = false;
				_genericParameterCount++;
				parameterName = _dbCommandBuilder.GetParameterName(_genericParameterCount);
				for (int i = 0; i < _baseParameterNames.Length; i++)
				{
					if (ADP.CompareInsensitiveInvariant(_baseParameterNames[i], parameterName))
					{
						flag = true;
						break;
					}
				}
			}
			while (flag);
			return parameterName;
		}

		internal string GetBaseParameterName(int index)
		{
			return _baseParameterNames[index];
		}

		internal string GetOriginalParameterName(int index)
		{
			return _originalParameterNames[index];
		}

		internal string GetNullParameterName(int index)
		{
			return _nullParameterNames[index];
		}
	}

	private DbDataAdapter _dataAdapter;

	private DbCommand _insertCommand;

	private DbCommand _updateCommand;

	private DbCommand _deleteCommand;

	private MissingMappingAction _missingMappingAction;

	private ConflictOption _conflictDetection = ConflictOption.CompareAllSearchableValues;

	private bool _setAllValues;

	private bool _hasPartialPrimaryKey;

	private DataTable _dbSchemaTable;

	private DbSchemaRow[] _dbSchemaRows;

	private string[] _sourceColumnNames;

	private ParameterNames _parameterNames;

	private string _quotedBaseTableName;

	private CatalogLocation _catalogLocation = CatalogLocation.Start;

	private string _catalogSeparator = ".";

	private string _schemaSeparator = ".";

	private string _quotePrefix = string.Empty;

	private string _quoteSuffix = string.Empty;

	private string _parameterNamePattern;

	private string _parameterMarkerFormat;

	private int _parameterNameMaxLength;

	[DefaultValue(ConflictOption.CompareAllSearchableValues)]
	public virtual ConflictOption ConflictOption
	{
		get
		{
			return _conflictDetection;
		}
		set
		{
			if ((uint)(value - 1) <= 2u)
			{
				_conflictDetection = value;
				return;
			}
			throw ADP.InvalidConflictOptions(value);
		}
	}

	[DefaultValue(CatalogLocation.Start)]
	public virtual CatalogLocation CatalogLocation
	{
		get
		{
			return _catalogLocation;
		}
		set
		{
			if (_dbSchemaTable != null)
			{
				throw ADP.NoQuoteChange();
			}
			if ((uint)(value - 1) <= 1u)
			{
				_catalogLocation = value;
				return;
			}
			throw ADP.InvalidCatalogLocation(value);
		}
	}

	[DefaultValue(".")]
	public virtual string CatalogSeparator
	{
		get
		{
			string catalogSeparator = _catalogSeparator;
			if (catalogSeparator == null || 0 >= catalogSeparator.Length)
			{
				return ".";
			}
			return catalogSeparator;
		}
		[param: AllowNull]
		set
		{
			if (_dbSchemaTable != null)
			{
				throw ADP.NoQuoteChange();
			}
			_catalogSeparator = value;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbDataAdapter? DataAdapter
	{
		get
		{
			return _dataAdapter;
		}
		set
		{
			if (_dataAdapter != value)
			{
				RefreshSchema();
				if (_dataAdapter != null)
				{
					SetRowUpdatingHandler(_dataAdapter);
					_dataAdapter = null;
				}
				if (value != null)
				{
					SetRowUpdatingHandler(value);
					_dataAdapter = value;
				}
			}
		}
	}

	internal int ParameterNameMaxLength => _parameterNameMaxLength;

	internal string? ParameterNamePattern => _parameterNamePattern;

	private string? QuotedBaseTableName => _quotedBaseTableName;

	[DefaultValue("")]
	public virtual string QuotePrefix
	{
		get
		{
			return _quotePrefix ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			if (_dbSchemaTable != null)
			{
				throw ADP.NoQuoteChange();
			}
			_quotePrefix = value;
		}
	}

	[DefaultValue("")]
	public virtual string QuoteSuffix
	{
		get
		{
			string quoteSuffix = _quoteSuffix;
			if (quoteSuffix == null)
			{
				return string.Empty;
			}
			return quoteSuffix;
		}
		[param: AllowNull]
		set
		{
			if (_dbSchemaTable != null)
			{
				throw ADP.NoQuoteChange();
			}
			_quoteSuffix = value;
		}
	}

	[DefaultValue(".")]
	public virtual string SchemaSeparator
	{
		get
		{
			string schemaSeparator = _schemaSeparator;
			if (schemaSeparator == null || 0 >= schemaSeparator.Length)
			{
				return ".";
			}
			return schemaSeparator;
		}
		[param: AllowNull]
		set
		{
			if (_dbSchemaTable != null)
			{
				throw ADP.NoQuoteChange();
			}
			_schemaSeparator = value;
		}
	}

	[DefaultValue(false)]
	public bool SetAllValues
	{
		get
		{
			return _setAllValues;
		}
		set
		{
			_setAllValues = value;
		}
	}

	private DbCommand? InsertCommand
	{
		get
		{
			return _insertCommand;
		}
		set
		{
			_insertCommand = value;
		}
	}

	private DbCommand? UpdateCommand
	{
		get
		{
			return _updateCommand;
		}
		set
		{
			_updateCommand = value;
		}
	}

	private DbCommand? DeleteCommand
	{
		get
		{
			return _deleteCommand;
		}
		set
		{
			_deleteCommand = value;
		}
	}

	private void BuildCache(bool closeConnection, DataRow dataRow, bool useColumnsForParameterNames)
	{
		if (_dbSchemaTable != null && (!useColumnsForParameterNames || _parameterNames != null))
		{
			return;
		}
		DataTable dataTable = null;
		DbCommand selectCommand = GetSelectCommand();
		DbConnection connection = selectCommand.Connection;
		if (connection == null)
		{
			throw ADP.MissingSourceCommandConnection();
		}
		try
		{
			if ((ConnectionState.Open & connection.State) == 0)
			{
				connection.Open();
			}
			else
			{
				closeConnection = false;
			}
			if (useColumnsForParameterNames)
			{
				DataTable schema = connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
				if (schema.Rows.Count == 1)
				{
					_parameterNamePattern = schema.Rows[0][DbMetaDataColumnNames.ParameterNamePattern] as string;
					_parameterMarkerFormat = schema.Rows[0][DbMetaDataColumnNames.ParameterMarkerFormat] as string;
					object obj = schema.Rows[0][DbMetaDataColumnNames.ParameterNameMaxLength];
					_parameterNameMaxLength = ((obj is int) ? ((int)obj) : 0);
					if (_parameterNameMaxLength == 0 || _parameterNamePattern == null || _parameterMarkerFormat == null)
					{
						useColumnsForParameterNames = false;
					}
				}
				else
				{
					useColumnsForParameterNames = false;
				}
			}
			dataTable = GetSchemaTable(selectCommand);
		}
		finally
		{
			if (closeConnection)
			{
				connection.Close();
			}
		}
		if (dataTable == null)
		{
			throw ADP.DynamicSQLNoTableInfo();
		}
		BuildInformation(dataTable);
		_dbSchemaTable = dataTable;
		DbSchemaRow[] dbSchemaRows = _dbSchemaRows;
		string[] array = new string[dbSchemaRows.Length];
		for (int i = 0; i < dbSchemaRows.Length; i++)
		{
			DbSchemaRow dbSchemaRow = dbSchemaRows[i];
			if (dbSchemaRow != null)
			{
				array[i] = dbSchemaRow.ColumnName;
			}
		}
		_sourceColumnNames = array;
		if (useColumnsForParameterNames)
		{
			_parameterNames = new ParameterNames(this, dbSchemaRows);
		}
		ADP.BuildSchemaTableInfoTableNames(array);
	}

	protected virtual DataTable? GetSchemaTable(DbCommand sourceCommand)
	{
		using IDataReader dataReader = sourceCommand.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
		return dataReader.GetSchemaTable();
	}

	private void BuildInformation(DataTable schemaTable)
	{
		DbSchemaRow[] sortedSchemaRows = DbSchemaRow.GetSortedSchemaRows(schemaTable, returnProviderSpecificTypes: false);
		if (sortedSchemaRows == null || sortedSchemaRows.Length == 0)
		{
			throw ADP.DynamicSQLNoTableInfo();
		}
		string text = string.Empty;
		string text2 = string.Empty;
		string text3 = string.Empty;
		string text4 = null;
		for (int i = 0; i < sortedSchemaRows.Length; i++)
		{
			DbSchemaRow dbSchemaRow = sortedSchemaRows[i];
			string baseTableName = dbSchemaRow.BaseTableName;
			if (baseTableName == null || baseTableName.Length == 0)
			{
				sortedSchemaRows[i] = null;
				continue;
			}
			string text5 = dbSchemaRow.BaseServerName;
			string text6 = dbSchemaRow.BaseCatalogName;
			string text7 = dbSchemaRow.BaseSchemaName;
			if (text5 == null)
			{
				text5 = string.Empty;
			}
			if (text6 == null)
			{
				text6 = string.Empty;
			}
			if (text7 == null)
			{
				text7 = string.Empty;
			}
			if (text4 == null)
			{
				text = text5;
				text2 = text6;
				text3 = text7;
				text4 = baseTableName;
			}
			else if (ADP.SrcCompare(text4, baseTableName) != 0 || ADP.SrcCompare(text3, text7) != 0 || ADP.SrcCompare(text2, text6) != 0 || ADP.SrcCompare(text, text5) != 0)
			{
				throw ADP.DynamicSQLJoinUnsupported();
			}
		}
		if (text.Length == 0)
		{
			text = null;
		}
		if (text2.Length == 0)
		{
			text = null;
			text2 = null;
		}
		if (text3.Length == 0)
		{
			text = null;
			text2 = null;
			text3 = null;
		}
		if (text4 == null || text4.Length == 0)
		{
			throw ADP.DynamicSQLNoTableInfo();
		}
		CatalogLocation catalogLocation = CatalogLocation;
		string catalogSeparator = CatalogSeparator;
		string schemaSeparator = SchemaSeparator;
		string quotePrefix = QuotePrefix;
		string quoteSuffix = QuoteSuffix;
		if (!string.IsNullOrEmpty(quotePrefix) && -1 != text4.IndexOf(quotePrefix, StringComparison.Ordinal))
		{
			throw ADP.DynamicSQLNestedQuote(text4, quotePrefix);
		}
		if (!string.IsNullOrEmpty(quoteSuffix) && -1 != text4.IndexOf(quoteSuffix, StringComparison.Ordinal))
		{
			throw ADP.DynamicSQLNestedQuote(text4, quoteSuffix);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (CatalogLocation.Start == catalogLocation)
		{
			if (text != null)
			{
				stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text));
				stringBuilder.Append(catalogSeparator);
			}
			if (text2 != null)
			{
				stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text2));
				stringBuilder.Append(catalogSeparator);
			}
		}
		if (text3 != null)
		{
			stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text3));
			stringBuilder.Append(schemaSeparator);
		}
		stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text4));
		if (CatalogLocation.End == catalogLocation)
		{
			if (text != null)
			{
				stringBuilder.Append(catalogSeparator);
				stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text));
			}
			if (text2 != null)
			{
				stringBuilder.Append(catalogSeparator);
				stringBuilder.Append(ADP.BuildQuotedString(quotePrefix, quoteSuffix, text2));
			}
		}
		_quotedBaseTableName = stringBuilder.ToString();
		_hasPartialPrimaryKey = false;
		DbSchemaRow[] array = sortedSchemaRows;
		foreach (DbSchemaRow dbSchemaRow2 in array)
		{
			if (dbSchemaRow2 != null && (dbSchemaRow2.IsKey || dbSchemaRow2.IsUnique) && !dbSchemaRow2.IsLong && !dbSchemaRow2.IsRowVersion && dbSchemaRow2.IsHidden)
			{
				_hasPartialPrimaryKey = true;
				break;
			}
		}
		_dbSchemaRows = sortedSchemaRows;
	}

	private DbCommand BuildDeleteCommand(DataTableMapping mappings, DataRow dataRow)
	{
		DbCommand dbCommand = InitializeCommand(DeleteCommand);
		StringBuilder stringBuilder = new StringBuilder();
		int parameterCount = 0;
		stringBuilder.Append("DELETE FROM ");
		stringBuilder.Append(QuotedBaseTableName);
		parameterCount = BuildWhereClause(mappings, dataRow, stringBuilder, dbCommand, parameterCount, isUpdate: false);
		dbCommand.CommandText = stringBuilder.ToString();
		RemoveExtraParameters(dbCommand, parameterCount);
		DeleteCommand = dbCommand;
		return dbCommand;
	}

	private DbCommand BuildInsertCommand(DataTableMapping mappings, DataRow dataRow)
	{
		DbCommand dbCommand = InitializeCommand(InsertCommand);
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		string value = " (";
		stringBuilder.Append("INSERT INTO ");
		stringBuilder.Append(QuotedBaseTableName);
		DbSchemaRow[] dbSchemaRows = _dbSchemaRows;
		string[] array = new string[dbSchemaRows.Length];
		for (int i = 0; i < dbSchemaRows.Length; i++)
		{
			DbSchemaRow dbSchemaRow = dbSchemaRows[i];
			if (dbSchemaRow == null || dbSchemaRow.BaseColumnName.Length == 0 || !IncludeInInsertValues(dbSchemaRow))
			{
				continue;
			}
			object obj = null;
			string text = _sourceColumnNames[i];
			if (mappings != null && dataRow != null)
			{
				DataColumn dataColumn = GetDataColumn(text, mappings, dataRow);
				if (dataColumn == null || (dbSchemaRow.IsReadOnly && dataColumn.ReadOnly))
				{
					continue;
				}
				obj = GetColumnValue(dataRow, dataColumn, DataRowVersion.Current);
				if (!dbSchemaRow.AllowDBNull && (obj == null || Convert.IsDBNull(obj)))
				{
					continue;
				}
			}
			stringBuilder.Append(value);
			value = ", ";
			stringBuilder.Append(QuotedColumn(dbSchemaRow.BaseColumnName));
			array[num] = CreateParameterForValue(dbCommand, GetBaseParameterName(i), text, DataRowVersion.Current, num, obj, dbSchemaRow, StatementType.Insert, whereClause: false);
			num++;
		}
		if (num == 0)
		{
			stringBuilder.Append(" DEFAULT VALUES");
		}
		else
		{
			stringBuilder.Append(')');
			stringBuilder.Append(" VALUES ");
			stringBuilder.Append('(');
			stringBuilder.Append(array[0]);
			for (int j = 1; j < num; j++)
			{
				stringBuilder.Append(", ");
				stringBuilder.Append(array[j]);
			}
			stringBuilder.Append(')');
		}
		dbCommand.CommandText = stringBuilder.ToString();
		RemoveExtraParameters(dbCommand, num);
		InsertCommand = dbCommand;
		return dbCommand;
	}

	private DbCommand BuildUpdateCommand(DataTableMapping mappings, DataRow dataRow)
	{
		DbCommand dbCommand = InitializeCommand(UpdateCommand);
		StringBuilder stringBuilder = new StringBuilder();
		string value = " SET ";
		int num = 0;
		stringBuilder.Append("UPDATE ");
		stringBuilder.Append(QuotedBaseTableName);
		DbSchemaRow[] dbSchemaRows = _dbSchemaRows;
		for (int i = 0; i < dbSchemaRows.Length; i++)
		{
			DbSchemaRow dbSchemaRow = dbSchemaRows[i];
			if (dbSchemaRow == null || dbSchemaRow.BaseColumnName.Length == 0 || !IncludeInUpdateSet(dbSchemaRow))
			{
				continue;
			}
			object obj = null;
			string text = _sourceColumnNames[i];
			if (mappings != null && dataRow != null)
			{
				DataColumn dataColumn = GetDataColumn(text, mappings, dataRow);
				if (dataColumn == null || (dbSchemaRow.IsReadOnly && dataColumn.ReadOnly))
				{
					continue;
				}
				obj = GetColumnValue(dataRow, dataColumn, DataRowVersion.Current);
				if (!SetAllValues)
				{
					object columnValue = GetColumnValue(dataRow, dataColumn, DataRowVersion.Original);
					if (columnValue == obj || (columnValue != null && columnValue.Equals(obj)))
					{
						continue;
					}
				}
			}
			stringBuilder.Append(value);
			value = ", ";
			stringBuilder.Append(QuotedColumn(dbSchemaRow.BaseColumnName));
			stringBuilder.Append(" = ");
			stringBuilder.Append(CreateParameterForValue(dbCommand, GetBaseParameterName(i), text, DataRowVersion.Current, num, obj, dbSchemaRow, StatementType.Update, whereClause: false));
			num++;
		}
		bool flag = num == 0;
		num = BuildWhereClause(mappings, dataRow, stringBuilder, dbCommand, num, isUpdate: true);
		dbCommand.CommandText = stringBuilder.ToString();
		RemoveExtraParameters(dbCommand, num);
		UpdateCommand = dbCommand;
		if (!flag)
		{
			return dbCommand;
		}
		return null;
	}

	private int BuildWhereClause(DataTableMapping mappings, DataRow dataRow, StringBuilder builder, DbCommand command, int parameterCount, bool isUpdate)
	{
		string value = string.Empty;
		int num = 0;
		builder.Append(" WHERE ");
		builder.Append('(');
		DbSchemaRow[] dbSchemaRows = _dbSchemaRows;
		for (int i = 0; i < dbSchemaRows.Length; i++)
		{
			DbSchemaRow dbSchemaRow = dbSchemaRows[i];
			if (dbSchemaRow != null && dbSchemaRow.BaseColumnName.Length != 0 && IncludeInWhereClause(dbSchemaRow, isUpdate))
			{
				builder.Append(value);
				value = " AND ";
				object value2 = null;
				string text = _sourceColumnNames[i];
				string value3 = QuotedColumn(dbSchemaRow.BaseColumnName);
				if (mappings != null && dataRow != null)
				{
					value2 = GetColumnValue(dataRow, text, mappings, DataRowVersion.Original);
				}
				if (!dbSchemaRow.AllowDBNull)
				{
					builder.Append('(');
					builder.Append(value3);
					builder.Append(" = ");
					builder.Append(CreateParameterForValue(command, GetOriginalParameterName(i), text, DataRowVersion.Original, parameterCount, value2, dbSchemaRow, isUpdate ? StatementType.Update : StatementType.Delete, whereClause: true));
					parameterCount++;
					builder.Append(')');
				}
				else
				{
					builder.Append('(');
					builder.Append('(');
					builder.Append(CreateParameterForNullTest(command, GetNullParameterName(i), text, DataRowVersion.Original, parameterCount, value2, dbSchemaRow, isUpdate ? StatementType.Update : StatementType.Delete, whereClause: true));
					parameterCount++;
					builder.Append(" = 1");
					builder.Append(" AND ");
					builder.Append(value3);
					builder.Append(" IS NULL");
					builder.Append(')');
					builder.Append(" OR ");
					builder.Append('(');
					builder.Append(value3);
					builder.Append(" = ");
					builder.Append(CreateParameterForValue(command, GetOriginalParameterName(i), text, DataRowVersion.Original, parameterCount, value2, dbSchemaRow, isUpdate ? StatementType.Update : StatementType.Delete, whereClause: true));
					parameterCount++;
					builder.Append(')');
					builder.Append(')');
				}
				if (IncrementWhereCount(dbSchemaRow))
				{
					num++;
				}
			}
		}
		builder.Append(')');
		if (num == 0)
		{
			if (isUpdate)
			{
				if (ConflictOption.CompareRowVersion == ConflictOption)
				{
					throw ADP.DynamicSQLNoKeyInfoRowVersionUpdate();
				}
				throw ADP.DynamicSQLNoKeyInfoUpdate();
			}
			if (ConflictOption.CompareRowVersion == ConflictOption)
			{
				throw ADP.DynamicSQLNoKeyInfoRowVersionDelete();
			}
			throw ADP.DynamicSQLNoKeyInfoDelete();
		}
		return parameterCount;
	}

	private string CreateParameterForNullTest(DbCommand command, string parameterName, string sourceColumn, DataRowVersion version, int parameterCount, object value, DbSchemaRow row, StatementType statementType, bool whereClause)
	{
		DbParameter nextParameter = GetNextParameter(command, parameterCount);
		if (parameterName == null)
		{
			nextParameter.ParameterName = GetParameterName(1 + parameterCount);
		}
		else
		{
			nextParameter.ParameterName = parameterName;
		}
		nextParameter.Direction = ParameterDirection.Input;
		nextParameter.SourceColumn = sourceColumn;
		nextParameter.SourceVersion = version;
		nextParameter.SourceColumnNullMapping = true;
		nextParameter.Value = value;
		nextParameter.Size = 0;
		ApplyParameterInfo(nextParameter, row.DataRow, statementType, whereClause);
		nextParameter.DbType = DbType.Int32;
		nextParameter.Value = (ADP.IsNull(value) ? DbDataAdapter.s_parameterValueNullValue : DbDataAdapter.s_parameterValueNonNullValue);
		if (!command.Parameters.Contains(nextParameter))
		{
			command.Parameters.Add(nextParameter);
		}
		if (parameterName == null)
		{
			return GetParameterPlaceholder(1 + parameterCount);
		}
		return string.Format(CultureInfo.InvariantCulture, _parameterMarkerFormat, parameterName);
	}

	private string CreateParameterForValue(DbCommand command, string parameterName, string sourceColumn, DataRowVersion version, int parameterCount, object value, DbSchemaRow row, StatementType statementType, bool whereClause)
	{
		DbParameter nextParameter = GetNextParameter(command, parameterCount);
		if (parameterName == null)
		{
			nextParameter.ParameterName = GetParameterName(1 + parameterCount);
		}
		else
		{
			nextParameter.ParameterName = parameterName;
		}
		nextParameter.Direction = ParameterDirection.Input;
		nextParameter.SourceColumn = sourceColumn;
		nextParameter.SourceVersion = version;
		nextParameter.SourceColumnNullMapping = false;
		nextParameter.Value = value;
		nextParameter.Size = 0;
		ApplyParameterInfo(nextParameter, row.DataRow, statementType, whereClause);
		if (!command.Parameters.Contains(nextParameter))
		{
			command.Parameters.Add(nextParameter);
		}
		if (parameterName == null)
		{
			return GetParameterPlaceholder(1 + parameterCount);
		}
		return string.Format(CultureInfo.InvariantCulture, _parameterMarkerFormat, parameterName);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			DataAdapter = null;
		}
		base.Dispose(disposing);
	}

	private DataTableMapping GetTableMapping(DataRow dataRow)
	{
		DataTableMapping result = null;
		if (dataRow != null)
		{
			DataTable table = dataRow.Table;
			if (table != null)
			{
				DbDataAdapter dataAdapter = DataAdapter;
				if (dataAdapter != null)
				{
					result = dataAdapter.GetTableMapping(table);
				}
				else
				{
					string tableName = table.TableName;
					result = new DataTableMapping(tableName, tableName);
				}
			}
		}
		return result;
	}

	private string GetBaseParameterName(int index)
	{
		if (_parameterNames != null)
		{
			return _parameterNames.GetBaseParameterName(index);
		}
		return null;
	}

	private string GetOriginalParameterName(int index)
	{
		if (_parameterNames != null)
		{
			return _parameterNames.GetOriginalParameterName(index);
		}
		return null;
	}

	private string GetNullParameterName(int index)
	{
		if (_parameterNames != null)
		{
			return _parameterNames.GetNullParameterName(index);
		}
		return null;
	}

	private DbCommand GetSelectCommand()
	{
		DbCommand dbCommand = null;
		DbDataAdapter dataAdapter = DataAdapter;
		if (dataAdapter != null)
		{
			if (_missingMappingAction == (MissingMappingAction)0)
			{
				_missingMappingAction = dataAdapter.MissingMappingAction;
			}
			dbCommand = dataAdapter.SelectCommand;
		}
		if (dbCommand == null)
		{
			throw ADP.MissingSourceCommand();
		}
		return dbCommand;
	}

	public DbCommand GetInsertCommand()
	{
		return GetInsertCommand(null, useColumnsForParameterNames: false);
	}

	public DbCommand GetInsertCommand(bool useColumnsForParameterNames)
	{
		return GetInsertCommand(null, useColumnsForParameterNames);
	}

	internal DbCommand GetInsertCommand(DataRow dataRow, bool useColumnsForParameterNames)
	{
		BuildCache(closeConnection: true, dataRow, useColumnsForParameterNames);
		BuildInsertCommand(GetTableMapping(dataRow), dataRow);
		return InsertCommand;
	}

	public DbCommand GetUpdateCommand()
	{
		return GetUpdateCommand(null, useColumnsForParameterNames: false);
	}

	public DbCommand GetUpdateCommand(bool useColumnsForParameterNames)
	{
		return GetUpdateCommand(null, useColumnsForParameterNames);
	}

	internal DbCommand GetUpdateCommand(DataRow dataRow, bool useColumnsForParameterNames)
	{
		BuildCache(closeConnection: true, dataRow, useColumnsForParameterNames);
		BuildUpdateCommand(GetTableMapping(dataRow), dataRow);
		return UpdateCommand;
	}

	public DbCommand GetDeleteCommand()
	{
		return GetDeleteCommand(null, useColumnsForParameterNames: false);
	}

	public DbCommand GetDeleteCommand(bool useColumnsForParameterNames)
	{
		return GetDeleteCommand(null, useColumnsForParameterNames);
	}

	internal DbCommand GetDeleteCommand(DataRow dataRow, bool useColumnsForParameterNames)
	{
		BuildCache(closeConnection: true, dataRow, useColumnsForParameterNames);
		BuildDeleteCommand(GetTableMapping(dataRow), dataRow);
		return DeleteCommand;
	}

	private object GetColumnValue(DataRow row, string columnName, DataTableMapping mappings, DataRowVersion version)
	{
		return GetColumnValue(row, GetDataColumn(columnName, mappings, row), version);
	}

	[return: NotNullIfNotNull("column")]
	private object GetColumnValue(DataRow row, DataColumn column, DataRowVersion version)
	{
		object result = null;
		if (column != null)
		{
			result = row[column, version];
		}
		return result;
	}

	private DataColumn GetDataColumn(string columnName, DataTableMapping tablemapping, DataRow row)
	{
		DataColumn result = null;
		if (!string.IsNullOrEmpty(columnName))
		{
			result = tablemapping.GetDataColumn(columnName, null, row.Table, _missingMappingAction, MissingSchemaAction.Error);
		}
		return result;
	}

	private static DbParameter GetNextParameter(DbCommand command, int pcount)
	{
		if (pcount < command.Parameters.Count)
		{
			return command.Parameters[pcount];
		}
		return command.CreateParameter();
	}

	private bool IncludeInInsertValues(DbSchemaRow row)
	{
		if (!row.IsAutoIncrement && !row.IsHidden && !row.IsExpression && !row.IsRowVersion)
		{
			return !row.IsReadOnly;
		}
		return false;
	}

	private bool IncludeInUpdateSet(DbSchemaRow row)
	{
		if (!row.IsAutoIncrement && !row.IsRowVersion && !row.IsHidden)
		{
			return !row.IsReadOnly;
		}
		return false;
	}

	private bool IncludeInWhereClause(DbSchemaRow row, bool isUpdate)
	{
		bool flag = IncrementWhereCount(row);
		if (flag && row.IsHidden)
		{
			if (ConflictOption.CompareRowVersion == ConflictOption)
			{
				throw ADP.DynamicSQLNoKeyInfoRowVersionUpdate();
			}
			throw ADP.DynamicSQLNoKeyInfoUpdate();
		}
		if (!flag && ConflictOption.CompareAllSearchableValues == ConflictOption)
		{
			flag = !row.IsLong && !row.IsRowVersion && !row.IsHidden;
		}
		return flag;
	}

	private bool IncrementWhereCount(DbSchemaRow row)
	{
		ConflictOption conflictOption = ConflictOption;
		switch (conflictOption)
		{
		case ConflictOption.CompareAllSearchableValues:
		case ConflictOption.OverwriteChanges:
			if ((row.IsKey || row.IsUnique) && !row.IsLong)
			{
				return !row.IsRowVersion;
			}
			return false;
		case ConflictOption.CompareRowVersion:
			if (((row.IsKey || row.IsUnique) && !_hasPartialPrimaryKey) || row.IsRowVersion)
			{
				return !row.IsLong;
			}
			return false;
		default:
			throw ADP.InvalidConflictOptions(conflictOption);
		}
	}

	protected virtual DbCommand InitializeCommand(DbCommand? command)
	{
		if (command == null)
		{
			DbCommand selectCommand = GetSelectCommand();
			command = selectCommand.Connection.CreateCommand();
			command.CommandTimeout = selectCommand.CommandTimeout;
			command.Transaction = selectCommand.Transaction;
		}
		command.CommandType = CommandType.Text;
		command.UpdatedRowSource = UpdateRowSource.None;
		return command;
	}

	private string QuotedColumn(string column)
	{
		return ADP.BuildQuotedString(QuotePrefix, QuoteSuffix, column);
	}

	public virtual string QuoteIdentifier(string unquotedIdentifier)
	{
		throw ADP.NotSupported();
	}

	public virtual void RefreshSchema()
	{
		_dbSchemaTable = null;
		_dbSchemaRows = null;
		_sourceColumnNames = null;
		_quotedBaseTableName = null;
		DbDataAdapter dataAdapter = DataAdapter;
		if (dataAdapter != null)
		{
			if (InsertCommand == dataAdapter.InsertCommand)
			{
				dataAdapter.InsertCommand = null;
			}
			if (UpdateCommand == dataAdapter.UpdateCommand)
			{
				dataAdapter.UpdateCommand = null;
			}
			if (DeleteCommand == dataAdapter.DeleteCommand)
			{
				dataAdapter.DeleteCommand = null;
			}
		}
		DbCommand insertCommand;
		if ((insertCommand = InsertCommand) != null)
		{
			insertCommand.Dispose();
		}
		if ((insertCommand = UpdateCommand) != null)
		{
			insertCommand.Dispose();
		}
		if ((insertCommand = DeleteCommand) != null)
		{
			insertCommand.Dispose();
		}
		InsertCommand = null;
		UpdateCommand = null;
		DeleteCommand = null;
	}

	private static void RemoveExtraParameters(DbCommand command, int usedParameterCount)
	{
		for (int num = command.Parameters.Count - 1; num >= usedParameterCount; num--)
		{
			command.Parameters.RemoveAt(num);
		}
	}

	protected void RowUpdatingHandler(RowUpdatingEventArgs rowUpdatingEvent)
	{
		if (rowUpdatingEvent == null)
		{
			throw ADP.ArgumentNull("rowUpdatingEvent");
		}
		try
		{
			if (rowUpdatingEvent.Status != 0)
			{
				return;
			}
			StatementType statementType = rowUpdatingEvent.StatementType;
			DbCommand dbCommand = (DbCommand)rowUpdatingEvent.Command;
			if (dbCommand != null)
			{
				switch (statementType)
				{
				case StatementType.Select:
					return;
				case StatementType.Insert:
					dbCommand = InsertCommand;
					break;
				case StatementType.Update:
					dbCommand = UpdateCommand;
					break;
				case StatementType.Delete:
					dbCommand = DeleteCommand;
					break;
				default:
					throw ADP.InvalidStatementType(statementType);
				}
				if (dbCommand != rowUpdatingEvent.Command)
				{
					dbCommand = (DbCommand)rowUpdatingEvent.Command;
					if (dbCommand != null && dbCommand.Connection == null)
					{
						DbCommand dbCommand2 = DataAdapter?.SelectCommand;
						if (dbCommand2 != null)
						{
							dbCommand.Connection = dbCommand2.Connection;
						}
					}
				}
				else
				{
					dbCommand = null;
				}
			}
			if (dbCommand == null)
			{
				RowUpdatingHandlerBuilder(rowUpdatingEvent);
			}
		}
		catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
		{
			ADP.TraceExceptionForCapture(ex);
			rowUpdatingEvent.Status = UpdateStatus.ErrorsOccurred;
			rowUpdatingEvent.Errors = ex;
		}
	}

	private void RowUpdatingHandlerBuilder(RowUpdatingEventArgs rowUpdatingEvent)
	{
		DataRow row = rowUpdatingEvent.Row;
		BuildCache(closeConnection: false, row, useColumnsForParameterNames: false);
		DbCommand dbCommand = rowUpdatingEvent.StatementType switch
		{
			StatementType.Insert => BuildInsertCommand(rowUpdatingEvent.TableMapping, row), 
			StatementType.Update => BuildUpdateCommand(rowUpdatingEvent.TableMapping, row), 
			StatementType.Delete => BuildDeleteCommand(rowUpdatingEvent.TableMapping, row), 
			_ => throw ADP.InvalidStatementType(rowUpdatingEvent.StatementType), 
		};
		if (dbCommand == null)
		{
			row?.AcceptChanges();
			rowUpdatingEvent.Status = UpdateStatus.SkipCurrentRow;
		}
		rowUpdatingEvent.Command = dbCommand;
	}

	public virtual string UnquoteIdentifier(string quotedIdentifier)
	{
		throw ADP.NotSupported();
	}

	protected abstract void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause);

	protected abstract string GetParameterName(int parameterOrdinal);

	protected abstract string GetParameterName(string parameterName);

	protected abstract string GetParameterPlaceholder(int parameterOrdinal);

	protected abstract void SetRowUpdatingHandler(DbDataAdapter adapter);
}
