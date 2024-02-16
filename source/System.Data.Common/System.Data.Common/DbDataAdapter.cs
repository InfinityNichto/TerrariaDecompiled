using System.Collections.Generic;
using System.ComponentModel;
using System.Data.ProviderBase;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common;

public abstract class DbDataAdapter : DataAdapter, IDbDataAdapter, IDataAdapter, ICloneable
{
	private struct BatchCommandInfo
	{
		internal int _commandIdentifier;

		internal int _parameterCount;

		internal DataRow _row;

		internal StatementType _statementType;

		internal UpdateRowSource _updatedRowSource;

		internal int? _recordsAffected;

		internal Exception _errors;
	}

	public const string DefaultSourceTableName = "Table";

	internal static readonly object s_parameterValueNonNullValue = 0;

	internal static readonly object s_parameterValueNullValue = 1;

	private IDbCommand _deleteCommand;

	private IDbCommand _insertCommand;

	private IDbCommand _selectCommand;

	private IDbCommand _updateCommand;

	private CommandBehavior _fillCommandBehavior;

	private IDbDataAdapter _IDbDataAdapter => this;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbCommand? DeleteCommand
	{
		get
		{
			return (DbCommand)_IDbDataAdapter.DeleteCommand;
		}
		set
		{
			_IDbDataAdapter.DeleteCommand = value;
		}
	}

	IDbCommand? IDbDataAdapter.DeleteCommand
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

	protected internal CommandBehavior FillCommandBehavior
	{
		get
		{
			return _fillCommandBehavior | CommandBehavior.SequentialAccess;
		}
		set
		{
			_fillCommandBehavior = value | CommandBehavior.SequentialAccess;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbCommand? InsertCommand
	{
		get
		{
			return (DbCommand)_IDbDataAdapter.InsertCommand;
		}
		set
		{
			_IDbDataAdapter.InsertCommand = value;
		}
	}

	IDbCommand? IDbDataAdapter.InsertCommand
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

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbCommand? SelectCommand
	{
		get
		{
			return (DbCommand)_IDbDataAdapter.SelectCommand;
		}
		set
		{
			_IDbDataAdapter.SelectCommand = value;
		}
	}

	IDbCommand? IDbDataAdapter.SelectCommand
	{
		get
		{
			return _selectCommand;
		}
		set
		{
			_selectCommand = value;
		}
	}

	[DefaultValue(1)]
	public virtual int UpdateBatchSize
	{
		get
		{
			return 1;
		}
		set
		{
			if (1 != value)
			{
				throw ADP.NotSupported();
			}
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbCommand? UpdateCommand
	{
		get
		{
			return (DbCommand)_IDbDataAdapter.UpdateCommand;
		}
		set
		{
			_IDbDataAdapter.UpdateCommand = value;
		}
	}

	IDbCommand? IDbDataAdapter.UpdateCommand
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

	private MissingMappingAction UpdateMappingAction
	{
		get
		{
			if (MissingMappingAction.Passthrough == base.MissingMappingAction)
			{
				return MissingMappingAction.Passthrough;
			}
			return MissingMappingAction.Error;
		}
	}

	private MissingSchemaAction UpdateSchemaAction
	{
		get
		{
			MissingSchemaAction missingSchemaAction = base.MissingSchemaAction;
			if (MissingSchemaAction.Add == missingSchemaAction || MissingSchemaAction.AddWithKey == missingSchemaAction)
			{
				return MissingSchemaAction.Ignore;
			}
			return MissingSchemaAction.Error;
		}
	}

	protected DbDataAdapter()
	{
	}

	protected DbDataAdapter(DbDataAdapter adapter)
		: base(adapter)
	{
		CloneFrom(adapter);
	}

	protected virtual int AddToBatch(IDbCommand command)
	{
		throw ADP.NotSupported();
	}

	protected virtual void ClearBatch()
	{
		throw ADP.NotSupported();
	}

	object ICloneable.Clone()
	{
		DbDataAdapter dbDataAdapter = (DbDataAdapter)CloneInternals();
		dbDataAdapter.CloneFrom(this);
		return dbDataAdapter;
	}

	private void CloneFrom(DbDataAdapter from)
	{
		IDbDataAdapter iDbDataAdapter = from._IDbDataAdapter;
		_IDbDataAdapter.SelectCommand = CloneCommand(iDbDataAdapter.SelectCommand);
		_IDbDataAdapter.InsertCommand = CloneCommand(iDbDataAdapter.InsertCommand);
		_IDbDataAdapter.UpdateCommand = CloneCommand(iDbDataAdapter.UpdateCommand);
		_IDbDataAdapter.DeleteCommand = CloneCommand(iDbDataAdapter.DeleteCommand);
	}

	private IDbCommand CloneCommand(IDbCommand command)
	{
		return (IDbCommand)((command is ICloneable) ? ((ICloneable)command).Clone() : null);
	}

	protected virtual RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
	{
		return new RowUpdatedEventArgs(dataRow, command, statementType, tableMapping);
	}

	protected virtual RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
	{
		return new RowUpdatingEventArgs(dataRow, command, statementType, tableMapping);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			((IDbDataAdapter)this).SelectCommand = null;
			((IDbDataAdapter)this).InsertCommand = null;
			((IDbDataAdapter)this).UpdateCommand = null;
			((IDbDataAdapter)this).DeleteCommand = null;
		}
		base.Dispose(disposing);
	}

	protected virtual int ExecuteBatch()
	{
		throw ADP.NotSupported();
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public DataTable? FillSchema(DataTable dataTable, SchemaType schemaType)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataTable, schemaType={1}", base.ObjectID, schemaType);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return FillSchema(dataTable, schemaType, selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public override DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}", base.ObjectID, schemaType);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			if (base.DesignMode && (selectCommand == null || selectCommand.Connection == null || string.IsNullOrEmpty(selectCommand.CommandText)))
			{
				return Array.Empty<DataTable>();
			}
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return FillSchema(dataSet, schemaType, selectCommand, "Table", fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, string srcTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}, srcTable={2}", base.ObjectID, (int)schemaType, srcTable);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return FillSchema(dataSet, schemaType, selectCommand, srcTable, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from command) schema table types cannot be statically analyzed.")]
	protected virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataSet, schemaType, command, srcTable, behavior={1}", base.ObjectID, behavior);
		try
		{
			if (dataSet == null)
			{
				throw ADP.ArgumentNull("dataSet");
			}
			if (SchemaType.Source != schemaType && SchemaType.Mapped != schemaType)
			{
				throw ADP.InvalidSchemaType(schemaType);
			}
			if (string.IsNullOrEmpty(srcTable))
			{
				throw ADP.FillSchemaRequiresSourceTableName("srcTable");
			}
			if (command == null)
			{
				throw ADP.MissingSelectCommand("FillSchema");
			}
			return (DataTable[])FillSchemaInternal(dataSet, null, schemaType, command, srcTable, behavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from command) schema table types cannot be statically analyzed.")]
	protected virtual DataTable? FillSchema(DataTable dataTable, SchemaType schemaType, IDbCommand command, CommandBehavior behavior)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.FillSchema|API> {0}, dataTable, schemaType, command, behavior={1}", base.ObjectID, behavior);
		try
		{
			if (dataTable == null)
			{
				throw ADP.ArgumentNull("dataTable");
			}
			if (SchemaType.Source != schemaType && SchemaType.Mapped != schemaType)
			{
				throw ADP.InvalidSchemaType(schemaType);
			}
			if (command == null)
			{
				throw ADP.MissingSelectCommand("FillSchema");
			}
			string text = dataTable.TableName;
			int num = IndexOfDataSetTable(text);
			if (-1 != num)
			{
				text = base.TableMappings[num].SourceTable;
			}
			return (DataTable)FillSchemaInternal(null, dataTable, schemaType, command, text, behavior | CommandBehavior.SingleResult);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from command) schema table types cannot be statically analyzed.")]
	private object FillSchemaInternal(DataSet dataset, DataTable datatable, SchemaType schemaType, IDbCommand command, string srcTable, CommandBehavior behavior)
	{
		object result = null;
		bool flag = command.Connection == null;
		try
		{
			IDbConnection connection = GetConnection3(this, command, "FillSchema");
			ConnectionState originalState = ConnectionState.Open;
			try
			{
				QuietOpen(connection, out originalState);
				using IDataReader dataReader = command.ExecuteReader(behavior | CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo);
				result = ((datatable == null) ? ((object)FillSchema(dataset, schemaType, srcTable, dataReader)) : ((object)FillSchema(datatable, schemaType, dataReader)));
			}
			finally
			{
				QuietClose(connection, originalState);
			}
		}
		finally
		{
			if (flag)
			{
				command.Transaction = null;
				command.Connection = null;
			}
		}
		return result;
	}

	public override int Fill(DataSet dataSet)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet", base.ObjectID);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return Fill(dataSet, 0, 0, "Table", selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public int Fill(DataSet dataSet, string srcTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, srcTable='{1}'", base.ObjectID, srcTable);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return Fill(dataSet, 0, 0, srcTable, selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public int Fill(DataSet dataSet, int startRecord, int maxRecords, string srcTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, startRecord={1}, maxRecords={2}, srcTable='{3}'", base.ObjectID, startRecord, maxRecords, srcTable);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return Fill(dataSet, startRecord, maxRecords, srcTable, selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected virtual int Fill(DataSet dataSet, int startRecord, int maxRecords, string srcTable, IDbCommand command, CommandBehavior behavior)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataSet, startRecord, maxRecords, srcTable, command, behavior={1}", base.ObjectID, behavior);
		try
		{
			if (dataSet == null)
			{
				throw ADP.FillRequires("dataSet");
			}
			if (startRecord < 0)
			{
				throw ADP.InvalidStartRecord("startRecord", startRecord);
			}
			if (maxRecords < 0)
			{
				throw ADP.InvalidMaxRecords("maxRecords", maxRecords);
			}
			if (string.IsNullOrEmpty(srcTable))
			{
				throw ADP.FillRequiresSourceTableName("srcTable");
			}
			if (command == null)
			{
				throw ADP.MissingSelectCommand("Fill");
			}
			return FillInternal(dataSet, null, startRecord, maxRecords, srcTable, command, behavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public int Fill(DataTable dataTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTable", base.ObjectID);
		try
		{
			DataTable[] dataTables = new DataTable[1] { dataTable };
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return Fill(dataTables, 0, 0, selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public int Fill(int startRecord, int maxRecords, params DataTable[] dataTables)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, startRecord={1}, maxRecords={2}, dataTable[]", base.ObjectID, startRecord, maxRecords);
		try
		{
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			CommandBehavior fillCommandBehavior = FillCommandBehavior;
			return Fill(dataTables, startRecord, maxRecords, selectCommand, fillCommandBehavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected virtual int Fill(DataTable dataTable, IDbCommand command, CommandBehavior behavior)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTable, command, behavior={1}", base.ObjectID, behavior);
		try
		{
			DataTable[] dataTables = new DataTable[1] { dataTable };
			return Fill(dataTables, 0, 0, command, behavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected virtual int Fill(DataTable[] dataTables, int startRecord, int maxRecords, IDbCommand command, CommandBehavior behavior)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Fill|API> {0}, dataTables[], startRecord, maxRecords, command, behavior={1}", base.ObjectID, behavior);
		try
		{
			if (dataTables == null || dataTables.Length == 0 || dataTables[0] == null)
			{
				throw ADP.FillRequires("dataTable");
			}
			if (startRecord < 0)
			{
				throw ADP.InvalidStartRecord("startRecord", startRecord);
			}
			if (maxRecords < 0)
			{
				throw ADP.InvalidMaxRecords("maxRecords", maxRecords);
			}
			if (1 < dataTables.Length && (startRecord != 0 || maxRecords != 0))
			{
				throw ADP.OnlyOneTableForStartRecordOrMaxRecords();
			}
			if (command == null)
			{
				throw ADP.MissingSelectCommand("Fill");
			}
			if (1 == dataTables.Length)
			{
				behavior |= CommandBehavior.SingleResult;
			}
			return FillInternal(null, dataTables, startRecord, maxRecords, null, command, behavior);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	private int FillInternal(DataSet dataset, DataTable[] datatables, int startRecord, int maxRecords, string srcTable, IDbCommand command, CommandBehavior behavior)
	{
		int result = 0;
		bool flag = command.Connection == null;
		try
		{
			IDbConnection connection = GetConnection3(this, command, "Fill");
			ConnectionState originalState = ConnectionState.Open;
			if (MissingSchemaAction.AddWithKey == base.MissingSchemaAction)
			{
				behavior |= CommandBehavior.KeyInfo;
			}
			try
			{
				QuietOpen(connection, out originalState);
				behavior |= CommandBehavior.SequentialAccess;
				IDataReader dataReader = null;
				try
				{
					dataReader = command.ExecuteReader(behavior);
					result = ((datatables == null) ? Fill(dataset, srcTable, dataReader, startRecord, maxRecords) : Fill(datatables, dataReader, startRecord, maxRecords));
				}
				finally
				{
					dataReader?.Dispose();
				}
			}
			finally
			{
				QuietClose(connection, originalState);
			}
		}
		finally
		{
			if (flag)
			{
				command.Transaction = null;
				command.Connection = null;
			}
		}
		return result;
	}

	protected virtual IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex)
	{
		throw ADP.NotSupported();
	}

	protected virtual bool GetBatchedRecordsAffected(int commandIdentifier, out int recordsAffected, out Exception? error)
	{
		recordsAffected = 1;
		error = null;
		return true;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public override IDataParameter[] GetFillParameters()
	{
		IDataParameter[] array = null;
		IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
		if (selectCommand != null)
		{
			IDataParameterCollection parameters = selectCommand.Parameters;
			if (parameters != null)
			{
				array = new IDataParameter[parameters.Count];
				parameters.CopyTo(array, 0);
			}
		}
		if (array == null)
		{
			array = Array.Empty<IDataParameter>();
		}
		return array;
	}

	internal DataTableMapping GetTableMapping(DataTable dataTable)
	{
		DataTableMapping dataTableMapping = null;
		int num = IndexOfDataSetTable(dataTable.TableName);
		if (-1 != num)
		{
			dataTableMapping = base.TableMappings[num];
		}
		if (dataTableMapping == null)
		{
			if (MissingMappingAction.Error == base.MissingMappingAction)
			{
				throw ADP.MissingTableMappingDestination(dataTable.TableName);
			}
			dataTableMapping = new DataTableMapping(dataTable.TableName, dataTable.TableName);
		}
		return dataTableMapping;
	}

	protected virtual void InitializeBatching()
	{
		throw ADP.NotSupported();
	}

	protected virtual void OnRowUpdated(RowUpdatedEventArgs value)
	{
	}

	protected virtual void OnRowUpdating(RowUpdatingEventArgs value)
	{
	}

	private void ParameterInput(IDataParameterCollection parameters, StatementType typeIndex, DataRow row, DataTableMapping mappings)
	{
		MissingMappingAction updateMappingAction = UpdateMappingAction;
		MissingSchemaAction updateSchemaAction = UpdateSchemaAction;
		foreach (IDataParameter parameter in parameters)
		{
			if (parameter == null || (ParameterDirection.Input & parameter.Direction) == 0)
			{
				continue;
			}
			string sourceColumn = parameter.SourceColumn;
			if (!string.IsNullOrEmpty(sourceColumn))
			{
				DataColumn dataColumn = mappings.GetDataColumn(sourceColumn, null, row.Table, updateMappingAction, updateSchemaAction);
				if (dataColumn != null)
				{
					DataRowVersion parameterSourceVersion = GetParameterSourceVersion(typeIndex, parameter);
					parameter.Value = row[dataColumn, parameterSourceVersion];
				}
				else
				{
					parameter.Value = null;
				}
				if (parameter is DbParameter { SourceColumnNullMapping: not false })
				{
					parameter.Value = (ADP.IsNull(parameter.Value) ? s_parameterValueNullValue : s_parameterValueNonNullValue);
				}
			}
		}
	}

	private void ParameterOutput(IDataParameter parameter, DataRow row, DataTableMapping mappings, MissingMappingAction missingMapping, MissingSchemaAction missingSchema)
	{
		if ((ParameterDirection.Output & parameter.Direction) == 0)
		{
			return;
		}
		object value = parameter.Value;
		if (value == null)
		{
			return;
		}
		string sourceColumn = parameter.SourceColumn;
		if (string.IsNullOrEmpty(sourceColumn))
		{
			return;
		}
		DataColumn dataColumn = mappings.GetDataColumn(sourceColumn, null, row.Table, missingMapping, missingSchema);
		if (dataColumn == null)
		{
			return;
		}
		if (dataColumn.ReadOnly)
		{
			try
			{
				dataColumn.ReadOnly = false;
				row[dataColumn] = value;
				return;
			}
			finally
			{
				dataColumn.ReadOnly = true;
			}
		}
		row[dataColumn] = value;
	}

	private void ParameterOutput(IDataParameterCollection parameters, DataRow row, DataTableMapping mappings)
	{
		MissingMappingAction updateMappingAction = UpdateMappingAction;
		MissingSchemaAction updateSchemaAction = UpdateSchemaAction;
		foreach (IDataParameter parameter in parameters)
		{
			if (parameter != null)
			{
				ParameterOutput(parameter, row, mappings, updateMappingAction, updateSchemaAction);
			}
		}
	}

	protected virtual void TerminateBatching()
	{
		throw ADP.NotSupported();
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public override int Update(DataSet dataSet)
	{
		return Update(dataSet, "Table");
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public int Update(DataRow[] dataRows)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataRows[]", base.ObjectID);
		try
		{
			int result = 0;
			if (dataRows == null)
			{
				throw ADP.ArgumentNull("dataRows");
			}
			if (dataRows.Length != 0)
			{
				DataTable dataTable = null;
				for (int i = 0; i < dataRows.Length; i++)
				{
					if (dataRows[i] != null && dataTable != dataRows[i].Table)
					{
						if (dataTable != null)
						{
							throw ADP.UpdateMismatchRowTable(i);
						}
						dataTable = dataRows[i].Table;
					}
				}
				if (dataTable != null)
				{
					DataTableMapping tableMapping = GetTableMapping(dataTable);
					result = Update(dataRows, tableMapping);
				}
			}
			return result;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public int Update(DataTable dataTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataTable", base.ObjectID);
		try
		{
			if (dataTable == null)
			{
				throw ADP.UpdateRequiresDataTable("dataTable");
			}
			DataTableMapping dataTableMapping = null;
			int num = IndexOfDataSetTable(dataTable.TableName);
			if (-1 != num)
			{
				dataTableMapping = base.TableMappings[num];
			}
			if (dataTableMapping == null)
			{
				if (MissingMappingAction.Error == base.MissingMappingAction)
				{
					throw ADP.MissingTableMappingDestination(dataTable.TableName);
				}
				dataTableMapping = new DataTableMapping("Table", dataTable.TableName);
			}
			return UpdateFromDataTable(dataTable, dataTableMapping);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public int Update(DataSet dataSet, string srcTable)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataSet, srcTable='{1}'", base.ObjectID, srcTable);
		try
		{
			if (dataSet == null)
			{
				throw ADP.UpdateRequiresNonNullDataSet("dataSet");
			}
			if (string.IsNullOrEmpty(srcTable))
			{
				throw ADP.UpdateRequiresSourceTableName("srcTable");
			}
			int result = 0;
			DataTableMapping tableMappingBySchemaAction = GetTableMappingBySchemaAction(srcTable, srcTable, UpdateMappingAction);
			MissingSchemaAction updateSchemaAction = UpdateSchemaAction;
			DataTable dataTableBySchemaAction = tableMappingBySchemaAction.GetDataTableBySchemaAction(dataSet, updateSchemaAction);
			if (dataTableBySchemaAction != null)
			{
				result = UpdateFromDataTable(dataTableBySchemaAction, tableMappingBySchemaAction);
			}
			else if (!HasTableMappings() || -1 == base.TableMappings.IndexOf(tableMappingBySchemaAction))
			{
				throw ADP.UpdateRequiresSourceTable(srcTable);
			}
			return result;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	protected virtual int Update(DataRow[] dataRows, DataTableMapping tableMapping)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DbDataAdapter.Update|API> {0}, dataRows[], tableMapping", base.ObjectID);
		try
		{
			int num = 0;
			IDbConnection[] array = new IDbConnection[5];
			ConnectionState[] array2 = new ConnectionState[5];
			bool useSelectConnectionState = false;
			IDbCommand selectCommand = _IDbDataAdapter.SelectCommand;
			if (selectCommand != null)
			{
				array[0] = selectCommand.Connection;
				if (array[0] != null)
				{
					array2[0] = array[0].State;
					useSelectConnectionState = true;
				}
			}
			int num2 = Math.Min(UpdateBatchSize, dataRows.Length);
			if (num2 < 1)
			{
				num2 = dataRows.Length;
			}
			BatchCommandInfo[] array3 = new BatchCommandInfo[num2];
			DataRow[] array4 = new DataRow[num2];
			int num3 = 0;
			try
			{
				try
				{
					if (1 != num2)
					{
						InitializeBatching();
					}
					StatementType statementType = StatementType.Select;
					IDbCommand dbCommand = null;
					foreach (DataRow dataRow in dataRows)
					{
						if (dataRow == null)
						{
							continue;
						}
						bool flag = false;
						DataRowState rowState = dataRow.RowState;
						if (rowState <= DataRowState.Added)
						{
							if ((uint)(rowState - 1) <= 1u)
							{
								continue;
							}
							if (rowState != DataRowState.Added)
							{
								goto IL_0115;
							}
							statementType = StatementType.Insert;
							dbCommand = _IDbDataAdapter.InsertCommand;
						}
						else if (rowState != DataRowState.Deleted)
						{
							if (rowState != DataRowState.Modified)
							{
								goto IL_0115;
							}
							statementType = StatementType.Update;
							dbCommand = _IDbDataAdapter.UpdateCommand;
						}
						else
						{
							statementType = StatementType.Delete;
							dbCommand = _IDbDataAdapter.DeleteCommand;
						}
						RowUpdatingEventArgs rowUpdatingEventArgs = CreateRowUpdatingEvent(dataRow, dbCommand, statementType, tableMapping);
						try
						{
							dataRow.RowError = null;
							if (dbCommand != null)
							{
								ParameterInput(dbCommand.Parameters, statementType, dataRow, tableMapping);
							}
						}
						catch (Exception ex) when (ADP.IsCatchableExceptionType(ex))
						{
							ADP.TraceExceptionForCapture(ex);
							rowUpdatingEventArgs.Errors = ex;
							rowUpdatingEventArgs.Status = UpdateStatus.ErrorsOccurred;
						}
						OnRowUpdating(rowUpdatingEventArgs);
						IDbCommand command = rowUpdatingEventArgs.Command;
						flag = dbCommand != command;
						dbCommand = command;
						command = null;
						UpdateStatus status = rowUpdatingEventArgs.Status;
						if (status != 0)
						{
							if (UpdateStatus.ErrorsOccurred == status)
							{
								UpdatingRowStatusErrors(rowUpdatingEventArgs, dataRow);
								continue;
							}
							if (UpdateStatus.SkipCurrentRow == status)
							{
								if (DataRowState.Unchanged == dataRow.RowState)
								{
									num++;
								}
								continue;
							}
							if (UpdateStatus.SkipAllRemainingRows == status)
							{
								if (DataRowState.Unchanged == dataRow.RowState)
								{
									num++;
								}
								break;
							}
							throw ADP.InvalidUpdateStatus(status);
						}
						rowUpdatingEventArgs = null;
						RowUpdatedEventArgs rowUpdatedEventArgs = null;
						if (1 == num2)
						{
							if (dbCommand != null)
							{
								array3[0]._commandIdentifier = 0;
								array3[0]._parameterCount = dbCommand.Parameters.Count;
								array3[0]._statementType = statementType;
								array3[0]._updatedRowSource = dbCommand.UpdatedRowSource;
							}
							array3[0]._row = dataRow;
							array4[0] = dataRow;
							num3 = 1;
						}
						else
						{
							Exception ex2 = null;
							try
							{
								if (dbCommand != null)
								{
									if ((UpdateRowSource.FirstReturnedRecord & dbCommand.UpdatedRowSource) == 0)
									{
										array3[num3]._commandIdentifier = AddToBatch(dbCommand);
										array3[num3]._parameterCount = dbCommand.Parameters.Count;
										array3[num3]._row = dataRow;
										array3[num3]._statementType = statementType;
										array3[num3]._updatedRowSource = dbCommand.UpdatedRowSource;
										array4[num3] = dataRow;
										num3++;
										if (num3 < num2)
										{
											continue;
										}
									}
									else
									{
										ex2 = ADP.ResultsNotAllowedDuringBatch();
									}
								}
								else
								{
									ex2 = ADP.UpdateRequiresCommand(statementType, flag);
								}
							}
							catch (Exception ex3) when (ADP.IsCatchableExceptionType(ex3))
							{
								ADP.TraceExceptionForCapture(ex3);
								ex2 = ex3;
							}
							if (ex2 != null)
							{
								rowUpdatedEventArgs = CreateRowUpdatedEvent(dataRow, dbCommand, StatementType.Batch, tableMapping);
								rowUpdatedEventArgs.Errors = ex2;
								rowUpdatedEventArgs.Status = UpdateStatus.ErrorsOccurred;
								OnRowUpdated(rowUpdatedEventArgs);
								if (ex2 != rowUpdatedEventArgs.Errors)
								{
									for (int j = 0; j < array3.Length; j++)
									{
										array3[j]._errors = null;
									}
								}
								num += UpdatedRowStatus(rowUpdatedEventArgs, array3, num3);
								if (UpdateStatus.SkipAllRemainingRows == rowUpdatedEventArgs.Status)
								{
									break;
								}
								continue;
							}
						}
						rowUpdatedEventArgs = CreateRowUpdatedEvent(dataRow, dbCommand, statementType, tableMapping);
						try
						{
							if (1 != num2)
							{
								IDbConnection connection = GetConnection1(this);
								ConnectionState connectionState = UpdateConnectionOpen(connection, StatementType.Batch, array, array2, useSelectConnectionState);
								rowUpdatedEventArgs.AdapterInit(array4);
								if (ConnectionState.Open == connectionState)
								{
									UpdateBatchExecute(array3, num3, rowUpdatedEventArgs);
								}
								else
								{
									rowUpdatedEventArgs.Errors = ADP.UpdateOpenConnectionRequired(StatementType.Batch, isRowUpdatingCommand: false, connectionState);
									rowUpdatedEventArgs.Status = UpdateStatus.ErrorsOccurred;
								}
							}
							else if (dbCommand != null)
							{
								IDbConnection connection2 = GetConnection4(this, dbCommand, statementType, flag);
								ConnectionState connectionState2 = UpdateConnectionOpen(connection2, statementType, array, array2, useSelectConnectionState);
								if (ConnectionState.Open == connectionState2)
								{
									UpdateRowExecute(rowUpdatedEventArgs, dbCommand, statementType);
									array3[0]._recordsAffected = rowUpdatedEventArgs.RecordsAffected;
									array3[0]._errors = null;
								}
								else
								{
									rowUpdatedEventArgs.Errors = ADP.UpdateOpenConnectionRequired(statementType, flag, connectionState2);
									rowUpdatedEventArgs.Status = UpdateStatus.ErrorsOccurred;
								}
							}
							else
							{
								rowUpdatedEventArgs.Errors = ADP.UpdateRequiresCommand(statementType, flag);
								rowUpdatedEventArgs.Status = UpdateStatus.ErrorsOccurred;
							}
						}
						catch (Exception ex4) when (ADP.IsCatchableExceptionType(ex4))
						{
							ADP.TraceExceptionForCapture(ex4);
							rowUpdatedEventArgs.Errors = ex4;
							rowUpdatedEventArgs.Status = UpdateStatus.ErrorsOccurred;
						}
						bool flag2 = UpdateStatus.ErrorsOccurred == rowUpdatedEventArgs.Status;
						Exception errors = rowUpdatedEventArgs.Errors;
						OnRowUpdated(rowUpdatedEventArgs);
						if (errors != rowUpdatedEventArgs.Errors)
						{
							for (int k = 0; k < array3.Length; k++)
							{
								array3[k]._errors = null;
							}
						}
						num += UpdatedRowStatus(rowUpdatedEventArgs, array3, num3);
						if (UpdateStatus.SkipAllRemainingRows == rowUpdatedEventArgs.Status)
						{
							if (flag2 && 1 != num2)
							{
								ClearBatch();
								num3 = 0;
							}
							break;
						}
						if (1 != num2)
						{
							ClearBatch();
							num3 = 0;
						}
						for (int l = 0; l < array3.Length; l++)
						{
							array3[l] = default(BatchCommandInfo);
						}
						num3 = 0;
						continue;
						IL_0115:
						throw ADP.InvalidDataRowState(dataRow.RowState);
					}
					if (1 != num2 && 0 < num3)
					{
						RowUpdatedEventArgs rowUpdatedEventArgs2 = CreateRowUpdatedEvent(null, dbCommand, statementType, tableMapping);
						try
						{
							IDbConnection connection3 = GetConnection1(this);
							ConnectionState connectionState3 = UpdateConnectionOpen(connection3, StatementType.Batch, array, array2, useSelectConnectionState);
							DataRow[] array5 = array4;
							if (num3 < array4.Length)
							{
								array5 = new DataRow[num3];
								Array.Copy(array4, array5, num3);
							}
							rowUpdatedEventArgs2.AdapterInit(array5);
							if (ConnectionState.Open == connectionState3)
							{
								UpdateBatchExecute(array3, num3, rowUpdatedEventArgs2);
							}
							else
							{
								rowUpdatedEventArgs2.Errors = ADP.UpdateOpenConnectionRequired(StatementType.Batch, isRowUpdatingCommand: false, connectionState3);
								rowUpdatedEventArgs2.Status = UpdateStatus.ErrorsOccurred;
							}
						}
						catch (Exception ex5) when (ADP.IsCatchableExceptionType(ex5))
						{
							ADP.TraceExceptionForCapture(ex5);
							rowUpdatedEventArgs2.Errors = ex5;
							rowUpdatedEventArgs2.Status = UpdateStatus.ErrorsOccurred;
						}
						Exception errors2 = rowUpdatedEventArgs2.Errors;
						OnRowUpdated(rowUpdatedEventArgs2);
						if (errors2 != rowUpdatedEventArgs2.Errors)
						{
							for (int m = 0; m < array3.Length; m++)
							{
								array3[m]._errors = null;
							}
						}
						num += UpdatedRowStatus(rowUpdatedEventArgs2, array3, num3);
					}
				}
				finally
				{
					if (1 != num2)
					{
						TerminateBatching();
					}
				}
			}
			finally
			{
				for (int n = 0; n < array.Length; n++)
				{
					QuietClose(array[n], array2[n]);
				}
			}
			return num;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	private void UpdateBatchExecute(BatchCommandInfo[] batchCommands, int commandCount, RowUpdatedEventArgs rowUpdatedEvent)
	{
		try
		{
			int recordsAffected = ExecuteBatch();
			rowUpdatedEvent.AdapterInit(recordsAffected);
		}
		catch (DbException ex)
		{
			ADP.TraceExceptionForCapture(ex);
			rowUpdatedEvent.Errors = ex;
			rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
		}
		MissingMappingAction updateMappingAction = UpdateMappingAction;
		MissingSchemaAction updateSchemaAction = UpdateSchemaAction;
		int num = 0;
		bool flag = false;
		List<DataRow> list = null;
		for (int i = 0; i < commandCount; i++)
		{
			BatchCommandInfo batchCommandInfo = batchCommands[i];
			StatementType statementType = batchCommandInfo._statementType;
			if (GetBatchedRecordsAffected(batchCommandInfo._commandIdentifier, out int recordsAffected2, out batchCommands[i]._errors))
			{
				batchCommands[i]._recordsAffected = recordsAffected2;
			}
			if (batchCommands[i]._errors != null || !batchCommands[i]._recordsAffected.HasValue)
			{
				continue;
			}
			if (StatementType.Update == statementType || StatementType.Delete == statementType)
			{
				num++;
				if (recordsAffected2 == 0)
				{
					if (list == null)
					{
						list = new List<DataRow>();
					}
					batchCommands[i]._errors = ADP.UpdateConcurrencyViolation(batchCommands[i]._statementType, 0, 1, new DataRow[1] { rowUpdatedEvent.Rows[i] });
					flag = true;
					list.Add(rowUpdatedEvent.Rows[i]);
				}
			}
			if ((StatementType.Insert == statementType || StatementType.Update == statementType) && (UpdateRowSource.OutputParameters & batchCommandInfo._updatedRowSource) != 0 && recordsAffected2 != 0)
			{
				if (StatementType.Insert == statementType)
				{
					rowUpdatedEvent.Rows[i].AcceptChanges();
				}
				for (int j = 0; j < batchCommandInfo._parameterCount; j++)
				{
					IDataParameter batchedParameter = GetBatchedParameter(batchCommandInfo._commandIdentifier, j);
					ParameterOutput(batchedParameter, batchCommandInfo._row, rowUpdatedEvent.TableMapping, updateMappingAction, updateSchemaAction);
				}
			}
		}
		if (rowUpdatedEvent.Errors == null && rowUpdatedEvent.Status == UpdateStatus.Continue && 0 < num && (rowUpdatedEvent.RecordsAffected == 0 || flag))
		{
			DataRow[] array = ((list != null) ? list.ToArray() : rowUpdatedEvent.Rows);
			rowUpdatedEvent.Errors = ADP.UpdateConcurrencyViolation(StatementType.Batch, commandCount - array.Length, commandCount, array);
			rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
		}
	}

	private ConnectionState UpdateConnectionOpen(IDbConnection connection, StatementType statementType, IDbConnection[] connections, ConnectionState[] connectionStates, bool useSelectConnectionState)
	{
		if (connection != connections[(int)statementType])
		{
			QuietClose(connections[(int)statementType], connectionStates[(int)statementType]);
			connections[(int)statementType] = connection;
			connectionStates[(int)statementType] = ConnectionState.Closed;
			QuietOpen(connection, out connectionStates[(int)statementType]);
			if (useSelectConnectionState && connections[0] == connection)
			{
				connectionStates[(int)statementType] = connections[0].State;
			}
		}
		return connection.State;
	}

	[RequiresUnreferencedCode("IDataReader (built from _IDbDataAdapter command) schema table rows DataTypes cannot be statically analyzed.")]
	private int UpdateFromDataTable(DataTable dataTable, DataTableMapping tableMapping)
	{
		int result = 0;
		DataRow[] array = ADP.SelectAdapterRows(dataTable, sorted: false);
		if (array != null && array.Length != 0)
		{
			result = Update(array, tableMapping);
		}
		return result;
	}

	[RequiresUnreferencedCode("IDataReader (built from dataCommand) schema table rows DataTypes cannot be statically analyzed.")]
	private void UpdateRowExecute(RowUpdatedEventArgs rowUpdatedEvent, IDbCommand dataCommand, StatementType cmdIndex)
	{
		bool flag = true;
		UpdateRowSource updatedRowSource = dataCommand.UpdatedRowSource;
		if (StatementType.Delete == cmdIndex || (UpdateRowSource.FirstReturnedRecord & updatedRowSource) == 0)
		{
			int recordsAffected = dataCommand.ExecuteNonQuery();
			rowUpdatedEvent.AdapterInit(recordsAffected);
		}
		else if (StatementType.Insert == cmdIndex || StatementType.Update == cmdIndex)
		{
			using IDataReader dataReader = dataCommand.ExecuteReader(CommandBehavior.SequentialAccess);
			DataReaderContainer dataReaderContainer = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
			try
			{
				bool flag2 = false;
				do
				{
					if (0 < dataReaderContainer.FieldCount)
					{
						flag2 = true;
						break;
					}
				}
				while (dataReader.NextResult());
				if (flag2 && dataReader.RecordsAffected != 0)
				{
					SchemaMapping schemaMapping = new SchemaMapping(this, null, rowUpdatedEvent.Row.Table, dataReaderContainer, keyInfo: false, SchemaType.Mapped, rowUpdatedEvent.TableMapping.SourceTable, gettingData: true, null, null);
					if (schemaMapping.DataTable != null && schemaMapping.DataValues != null && dataReader.Read())
					{
						if (StatementType.Insert == cmdIndex && flag)
						{
							rowUpdatedEvent.Row.AcceptChanges();
							flag = false;
						}
						schemaMapping.ApplyToDataRow(rowUpdatedEvent.Row);
					}
				}
			}
			finally
			{
				dataReader.Close();
				int recordsAffected2 = dataReader.RecordsAffected;
				rowUpdatedEvent.AdapterInit(recordsAffected2);
			}
		}
		if ((StatementType.Insert == cmdIndex || StatementType.Update == cmdIndex) && (UpdateRowSource.OutputParameters & updatedRowSource) != 0 && rowUpdatedEvent.RecordsAffected != 0)
		{
			if (StatementType.Insert == cmdIndex && flag)
			{
				rowUpdatedEvent.Row.AcceptChanges();
			}
			ParameterOutput(dataCommand.Parameters, rowUpdatedEvent.Row, rowUpdatedEvent.TableMapping);
		}
		if (rowUpdatedEvent.Status == UpdateStatus.Continue && (uint)(cmdIndex - 2) <= 1u && rowUpdatedEvent.RecordsAffected == 0)
		{
			rowUpdatedEvent.Errors = ADP.UpdateConcurrencyViolation(cmdIndex, rowUpdatedEvent.RecordsAffected, 1, new DataRow[1] { rowUpdatedEvent.Row });
			rowUpdatedEvent.Status = UpdateStatus.ErrorsOccurred;
		}
	}

	private int UpdatedRowStatus(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
	{
		int num = 0;
		switch (rowUpdatedEvent.Status)
		{
		case UpdateStatus.Continue:
			return UpdatedRowStatusContinue(rowUpdatedEvent, batchCommands, commandCount);
		case UpdateStatus.ErrorsOccurred:
			return UpdatedRowStatusErrors(rowUpdatedEvent, batchCommands, commandCount);
		case UpdateStatus.SkipCurrentRow:
		case UpdateStatus.SkipAllRemainingRows:
			return UpdatedRowStatusSkip(batchCommands, commandCount);
		default:
			throw ADP.InvalidUpdateStatus(rowUpdatedEvent.Status);
		}
	}

	private int UpdatedRowStatusContinue(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
	{
		int num = 0;
		bool acceptChangesDuringUpdate = base.AcceptChangesDuringUpdate;
		for (int i = 0; i < commandCount; i++)
		{
			BatchCommandInfo batchCommandInfo = batchCommands[i];
			DataRow row = batchCommandInfo._row;
			if (batchCommandInfo._errors == null && batchCommandInfo._recordsAffected.HasValue && batchCommandInfo._recordsAffected.Value != 0)
			{
				if (acceptChangesDuringUpdate && ((DataRowState.Added | DataRowState.Deleted | DataRowState.Modified) & row.RowState) != 0)
				{
					row.AcceptChanges();
				}
				num++;
			}
		}
		return num;
	}

	private int UpdatedRowStatusErrors(RowUpdatedEventArgs rowUpdatedEvent, BatchCommandInfo[] batchCommands, int commandCount)
	{
		Exception ex = rowUpdatedEvent.Errors;
		if (ex == null)
		{
			ex = (rowUpdatedEvent.Errors = ADP.RowUpdatedErrors());
		}
		int result = 0;
		bool flag = false;
		string message = ex.Message;
		for (int i = 0; i < commandCount; i++)
		{
			DataRow row = batchCommands[i]._row;
			Exception errors = batchCommands[i]._errors;
			if (errors != null)
			{
				string text = errors.Message;
				if (string.IsNullOrEmpty(text))
				{
					text = message;
				}
				row.RowError += text;
				flag = true;
			}
		}
		if (!flag)
		{
			for (int j = 0; j < commandCount; j++)
			{
				batchCommands[j]._row.RowError += message;
			}
		}
		else
		{
			result = UpdatedRowStatusContinue(rowUpdatedEvent, batchCommands, commandCount);
		}
		if (!base.ContinueUpdateOnError)
		{
			throw ex;
		}
		return result;
	}

	private int UpdatedRowStatusSkip(BatchCommandInfo[] batchCommands, int commandCount)
	{
		int num = 0;
		for (int i = 0; i < commandCount; i++)
		{
			DataRow row = batchCommands[i]._row;
			if (((DataRowState.Detached | DataRowState.Unchanged) & row.RowState) != 0)
			{
				num++;
			}
		}
		return num;
	}

	private void UpdatingRowStatusErrors(RowUpdatingEventArgs rowUpdatedEvent, DataRow dataRow)
	{
		Exception ex = rowUpdatedEvent.Errors;
		if (ex == null)
		{
			ex = (rowUpdatedEvent.Errors = ADP.RowUpdatingErrors());
		}
		string message = ex.Message;
		dataRow.RowError += message;
		if (!base.ContinueUpdateOnError)
		{
			throw ex;
		}
	}

	private static IDbConnection GetConnection1(DbDataAdapter adapter)
	{
		IDbCommand dbCommand = adapter._IDbDataAdapter.SelectCommand;
		if (dbCommand == null)
		{
			dbCommand = adapter._IDbDataAdapter.InsertCommand;
			if (dbCommand == null)
			{
				dbCommand = adapter._IDbDataAdapter.UpdateCommand;
				if (dbCommand == null)
				{
					dbCommand = adapter._IDbDataAdapter.DeleteCommand;
				}
			}
		}
		IDbConnection dbConnection = null;
		if (dbCommand != null)
		{
			dbConnection = dbCommand.Connection;
		}
		if (dbConnection == null)
		{
			throw ADP.UpdateConnectionRequired(StatementType.Batch, isRowUpdatingCommand: false);
		}
		return dbConnection;
	}

	private static IDbConnection GetConnection3(DbDataAdapter adapter, IDbCommand command, string method)
	{
		IDbConnection connection = command.Connection;
		if (connection == null)
		{
			throw ADP.ConnectionRequired_Res(method);
		}
		return connection;
	}

	private static IDbConnection GetConnection4(DbDataAdapter adapter, IDbCommand command, StatementType statementType, bool isCommandFromRowUpdating)
	{
		IDbConnection connection = command.Connection;
		if (connection == null)
		{
			throw ADP.UpdateConnectionRequired(statementType, isCommandFromRowUpdating);
		}
		return connection;
	}

	private static DataRowVersion GetParameterSourceVersion(StatementType statementType, IDataParameter parameter)
	{
		switch (statementType)
		{
		case StatementType.Insert:
			return DataRowVersion.Current;
		case StatementType.Update:
			return parameter.SourceVersion;
		case StatementType.Delete:
			return DataRowVersion.Original;
		case StatementType.Select:
		case StatementType.Batch:
			throw ADP.UnwantedStatementType(statementType);
		default:
			throw ADP.InvalidStatementType(statementType);
		}
	}

	private static void QuietClose(IDbConnection connection, ConnectionState originalState)
	{
		if (connection != null && originalState == ConnectionState.Closed)
		{
			connection.Close();
		}
	}

	private static void QuietOpen(IDbConnection connection, out ConnectionState originalState)
	{
		originalState = connection.State;
		if (originalState == ConnectionState.Closed)
		{
			connection.Open();
		}
	}
}
