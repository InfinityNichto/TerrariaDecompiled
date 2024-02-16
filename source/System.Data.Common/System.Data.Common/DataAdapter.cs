using System.ComponentModel;
using System.Data.ProviderBase;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Data.Common;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
public class DataAdapter : Component, IDataAdapter
{
	private static readonly object s_eventFillError = new object();

	private bool _acceptChangesDuringUpdate = true;

	private bool _acceptChangesDuringUpdateAfterInsert = true;

	private bool _continueUpdateOnError;

	private bool _hasFillErrorHandler;

	private bool _returnProviderSpecificTypes;

	private bool _acceptChangesDuringFill = true;

	private LoadOption _fillLoadOption;

	private MissingMappingAction _missingMappingAction = MissingMappingAction.Passthrough;

	private MissingSchemaAction _missingSchemaAction = MissingSchemaAction.Add;

	private DataTableMappingCollection _tableMappings;

	private static int s_objectTypeCount;

	internal readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	[DefaultValue(true)]
	public bool AcceptChangesDuringFill
	{
		get
		{
			return _acceptChangesDuringFill;
		}
		set
		{
			_acceptChangesDuringFill = value;
		}
	}

	[DefaultValue(true)]
	public bool AcceptChangesDuringUpdate
	{
		get
		{
			return _acceptChangesDuringUpdate;
		}
		set
		{
			_acceptChangesDuringUpdate = value;
		}
	}

	[DefaultValue(false)]
	public bool ContinueUpdateOnError
	{
		get
		{
			return _continueUpdateOnError;
		}
		set
		{
			_continueUpdateOnError = value;
		}
	}

	[RefreshProperties(RefreshProperties.All)]
	public LoadOption FillLoadOption
	{
		get
		{
			if (_fillLoadOption == (LoadOption)0)
			{
				return LoadOption.OverwriteChanges;
			}
			return _fillLoadOption;
		}
		[RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
		set
		{
			if ((uint)value <= 3u)
			{
				_fillLoadOption = value;
				return;
			}
			throw ADP.InvalidLoadOption(value);
		}
	}

	[DefaultValue(MissingMappingAction.Passthrough)]
	public MissingMappingAction MissingMappingAction
	{
		get
		{
			return _missingMappingAction;
		}
		set
		{
			if ((uint)(value - 1) <= 2u)
			{
				_missingMappingAction = value;
				return;
			}
			throw ADP.InvalidMissingMappingAction(value);
		}
	}

	[DefaultValue(MissingSchemaAction.Add)]
	public MissingSchemaAction MissingSchemaAction
	{
		get
		{
			return _missingSchemaAction;
		}
		set
		{
			if ((uint)(value - 1) <= 3u)
			{
				_missingSchemaAction = value;
				return;
			}
			throw ADP.InvalidMissingSchemaAction(value);
		}
	}

	internal int ObjectID => _objectID;

	[DefaultValue(false)]
	public virtual bool ReturnProviderSpecificTypes
	{
		get
		{
			return _returnProviderSpecificTypes;
		}
		set
		{
			_returnProviderSpecificTypes = value;
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public DataTableMappingCollection TableMappings
	{
		get
		{
			DataTableMappingCollection dataTableMappingCollection = _tableMappings;
			if (dataTableMappingCollection == null)
			{
				dataTableMappingCollection = CreateTableMappings();
				if (dataTableMappingCollection == null)
				{
					dataTableMappingCollection = new DataTableMappingCollection();
				}
				_tableMappings = dataTableMappingCollection;
			}
			return dataTableMappingCollection;
		}
	}

	ITableMappingCollection IDataAdapter.TableMappings => TableMappings;

	public event FillErrorEventHandler? FillError
	{
		add
		{
			_hasFillErrorHandler = true;
			base.Events.AddHandler(s_eventFillError, value);
		}
		remove
		{
			base.Events.RemoveHandler(s_eventFillError, value);
		}
	}

	protected DataAdapter()
	{
		GC.SuppressFinalize(this);
	}

	protected DataAdapter(DataAdapter from)
	{
		CloneFrom(from);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual bool ShouldSerializeAcceptChangesDuringFill()
	{
		return _fillLoadOption == (LoadOption)0;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void ResetFillLoadOption()
	{
		_fillLoadOption = (LoadOption)0;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual bool ShouldSerializeFillLoadOption()
	{
		return _fillLoadOption != (LoadOption)0;
	}

	protected virtual bool ShouldSerializeTableMappings()
	{
		return true;
	}

	protected bool HasTableMappings()
	{
		if (_tableMappings != null)
		{
			return 0 < TableMappings.Count;
		}
		return false;
	}

	[Obsolete("CloneInternals() has been deprecated. Use the DataAdapter(DataAdapter from) constructor instead.")]
	protected virtual DataAdapter CloneInternals()
	{
		DataAdapter dataAdapter = (DataAdapter)Activator.CreateInstance(GetType());
		dataAdapter.CloneFrom(this);
		return dataAdapter;
	}

	private void CloneFrom(DataAdapter from)
	{
		_acceptChangesDuringUpdate = from._acceptChangesDuringUpdate;
		_acceptChangesDuringUpdateAfterInsert = from._acceptChangesDuringUpdateAfterInsert;
		_continueUpdateOnError = from._continueUpdateOnError;
		_returnProviderSpecificTypes = from._returnProviderSpecificTypes;
		_acceptChangesDuringFill = from._acceptChangesDuringFill;
		_fillLoadOption = from._fillLoadOption;
		_missingMappingAction = from._missingMappingAction;
		_missingSchemaAction = from._missingSchemaAction;
		if (from._tableMappings == null || 0 >= from.TableMappings.Count)
		{
			return;
		}
		DataTableMappingCollection tableMappings = TableMappings;
		foreach (object tableMapping in from.TableMappings)
		{
			tableMappings.Add((tableMapping is ICloneable) ? ((ICloneable)tableMapping).Clone() : tableMapping);
		}
	}

	protected virtual DataTableMappingCollection CreateTableMappings()
	{
		DataCommonEventSource.Log.Trace("<comm.DataAdapter.CreateTableMappings|API> {0}", ObjectID);
		return new DataTableMappingCollection();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_tableMappings = null;
		}
		base.Dispose(disposing);
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType)
	{
		throw ADP.NotSupported();
	}

	[RequiresUnreferencedCode("dataReader's schema table types cannot be statically analyzed.")]
	protected virtual DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType, string srcTable, IDataReader dataReader)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.FillSchema|API> {0}, dataSet, schemaType={1}, srcTable, dataReader", ObjectID, schemaType);
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
			if (dataReader == null || dataReader.IsClosed)
			{
				throw ADP.FillRequires("dataReader");
			}
			object obj = FillSchemaFromReader(dataSet, null, schemaType, srcTable, dataReader);
			return (DataTable[])obj;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("dataReader's schema table types cannot be statically analyzed.")]
	protected virtual DataTable? FillSchema(DataTable dataTable, SchemaType schemaType, IDataReader dataReader)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.FillSchema|API> {0}, dataTable, schemaType, dataReader", ObjectID);
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
			if (dataReader == null || dataReader.IsClosed)
			{
				throw ADP.FillRequires("dataReader");
			}
			object obj = FillSchemaFromReader(null, dataTable, schemaType, null, dataReader);
			return (DataTable)obj;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("dataReader's schema table types cannot be statically analyzed.")]
	internal object FillSchemaFromReader(DataSet dataset, DataTable datatable, SchemaType schemaType, string srcTable, IDataReader dataReader)
	{
		DataTable[] array = null;
		int num = 0;
		do
		{
			DataReaderContainer dataReaderContainer = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
			if (0 < dataReaderContainer.FieldCount)
			{
				string sourceTableName = null;
				if (dataset != null)
				{
					sourceTableName = GetSourceTableName(srcTable, num);
					num++;
				}
				SchemaMapping schemaMapping = new SchemaMapping(this, dataset, datatable, dataReaderContainer, keyInfo: true, schemaType, sourceTableName, gettingData: false, null, null);
				if (datatable != null)
				{
					return schemaMapping.DataTable;
				}
				if (schemaMapping.DataTable != null)
				{
					array = ((array != null) ? AddDataTableToArray(array, schemaMapping.DataTable) : new DataTable[1] { schemaMapping.DataTable });
				}
			}
		}
		while (dataReader.NextResult());
		object obj = array;
		if (obj == null && datatable == null)
		{
			obj = Array.Empty<DataTable>();
		}
		return obj;
	}

	public virtual int Fill(DataSet dataSet)
	{
		throw ADP.NotSupported();
	}

	protected virtual int Fill(DataSet dataSet, string srcTable, IDataReader dataReader, int startRecord, int maxRecords)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.Fill|API> {0}, dataSet, srcTable, dataReader, startRecord, maxRecords", ObjectID);
		try
		{
			if (dataSet == null)
			{
				throw ADP.FillRequires("dataSet");
			}
			if (string.IsNullOrEmpty(srcTable))
			{
				throw ADP.FillRequiresSourceTableName("srcTable");
			}
			if (dataReader == null)
			{
				throw ADP.FillRequires("dataReader");
			}
			if (startRecord < 0)
			{
				throw ADP.InvalidStartRecord("startRecord", startRecord);
			}
			if (maxRecords < 0)
			{
				throw ADP.InvalidMaxRecords("maxRecords", maxRecords);
			}
			if (dataReader.IsClosed)
			{
				return 0;
			}
			DataReaderContainer dataReader2 = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
			return FillFromReader(dataSet, null, srcTable, dataReader2, startRecord, maxRecords);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected virtual int Fill(DataTable dataTable, IDataReader dataReader)
	{
		DataTable[] dataTables = new DataTable[1] { dataTable };
		return Fill(dataTables, dataReader, 0, 0);
	}

	protected virtual int Fill(DataTable[] dataTables, IDataReader dataReader, int startRecord, int maxRecords)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<comm.DataAdapter.Fill|API> {0}, dataTables[], dataReader, startRecord, maxRecords", ObjectID);
		try
		{
			ADP.CheckArgumentLength(dataTables, "dataTables");
			if (dataTables == null || dataTables.Length == 0 || dataTables[0] == null)
			{
				throw ADP.FillRequires("dataTable");
			}
			if (dataReader == null)
			{
				throw ADP.FillRequires("dataReader");
			}
			if (1 < dataTables.Length && (startRecord != 0 || maxRecords != 0))
			{
				throw ADP.NotSupported();
			}
			int result = 0;
			bool flag = false;
			DataSet dataSet = dataTables[0].DataSet;
			try
			{
				if (dataSet != null)
				{
					flag = dataSet.EnforceConstraints;
					dataSet.EnforceConstraints = false;
				}
				for (int i = 0; i < dataTables.Length && !dataReader.IsClosed; i++)
				{
					DataReaderContainer dataReaderContainer = DataReaderContainer.Create(dataReader, ReturnProviderSpecificTypes);
					if (dataReaderContainer.FieldCount <= 0)
					{
						if (i != 0)
						{
							continue;
						}
						bool flag2;
						do
						{
							flag2 = FillNextResult(dataReaderContainer);
						}
						while (flag2 && dataReaderContainer.FieldCount <= 0);
						if (!flag2)
						{
							break;
						}
					}
					if (0 >= i || FillNextResult(dataReaderContainer))
					{
						int num = FillFromReader(null, dataTables[i], null, dataReaderContainer, startRecord, maxRecords);
						if (i == 0)
						{
							result = num;
						}
						continue;
					}
					break;
				}
			}
			catch (ConstraintException)
			{
				flag = false;
				throw;
			}
			finally
			{
				if (flag)
				{
					dataSet.EnforceConstraints = true;
				}
			}
			return result;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "parentChapterValue is not used here")]
	internal int FillFromReader(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int startRecord, int maxRecords)
	{
		return FillFromReader(dataset, datatable, srcTable, dataReader, startRecord, maxRecords, null, null);
	}

	[RequiresUnreferencedCode("parentChapterValue's type cannot be statically analyzed")]
	internal int FillFromReader(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int startRecord, int maxRecords, DataColumn parentChapterColumn, object parentChapterValue)
	{
		int result = 0;
		int num = 0;
		do
		{
			if (0 >= dataReader.FieldCount)
			{
				continue;
			}
			SchemaMapping schemaMapping = FillMapping(dataset, datatable, srcTable, dataReader, num, parentChapterColumn, parentChapterValue);
			num++;
			if (schemaMapping == null || schemaMapping.DataValues == null || schemaMapping.DataTable == null)
			{
				continue;
			}
			schemaMapping.DataTable.BeginLoadData();
			try
			{
				if (1 == num && (0 < startRecord || 0 < maxRecords))
				{
					result = FillLoadDataRowChunk(schemaMapping, startRecord, maxRecords);
				}
				else
				{
					int num2 = FillLoadDataRow(schemaMapping);
					if (1 == num)
					{
						result = num2;
					}
				}
			}
			finally
			{
				schemaMapping.DataTable.EndLoadData();
			}
			if (datatable != null)
			{
				break;
			}
		}
		while (FillNextResult(dataReader));
		return result;
	}

	[RequiresUnreferencedCode("Row chapter column types cannot be statically analyzed")]
	private int FillLoadDataRowChunk(SchemaMapping mapping, int startRecord, int maxRecords)
	{
		DataReaderContainer dataReader = mapping.DataReader;
		while (0 < startRecord)
		{
			if (!dataReader.Read())
			{
				return 0;
			}
			startRecord--;
		}
		int num = 0;
		if (0 < maxRecords)
		{
			while (num < maxRecords && dataReader.Read())
			{
				if (_hasFillErrorHandler)
				{
					try
					{
						mapping.LoadDataRowWithClear();
						num++;
					}
					catch (Exception e) when (ADP.IsCatchableExceptionType(e))
					{
						ADP.TraceExceptionForCapture(e);
						OnFillErrorHandler(e, mapping.DataTable, mapping.DataValues);
					}
				}
				else
				{
					mapping.LoadDataRow();
					num++;
				}
			}
		}
		else
		{
			num = FillLoadDataRow(mapping);
		}
		return num;
	}

	[RequiresUnreferencedCode("Row chapter column types cannot be statically analyzed")]
	private int FillLoadDataRow(SchemaMapping mapping)
	{
		int num = 0;
		DataReaderContainer dataReader = mapping.DataReader;
		if (_hasFillErrorHandler)
		{
			while (dataReader.Read())
			{
				try
				{
					mapping.LoadDataRowWithClear();
					num++;
				}
				catch (Exception e) when (ADP.IsCatchableExceptionType(e))
				{
					ADP.TraceExceptionForCapture(e);
					OnFillErrorHandler(e, mapping.DataTable, mapping.DataValues);
				}
			}
		}
		else
		{
			while (dataReader.Read())
			{
				mapping.LoadDataRow();
				num++;
			}
		}
		return num;
	}

	[RequiresUnreferencedCode("parentChapterValue's type cannot be statically analyzed")]
	private SchemaMapping FillMappingInternal(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int schemaCount, DataColumn parentChapterColumn, object parentChapterValue)
	{
		bool keyInfo = MissingSchemaAction.AddWithKey == MissingSchemaAction;
		string sourceTableName = null;
		if (dataset != null)
		{
			sourceTableName = GetSourceTableName(srcTable, schemaCount);
		}
		return new SchemaMapping(this, dataset, datatable, dataReader, keyInfo, SchemaType.Mapped, sourceTableName, gettingData: true, parentChapterColumn, parentChapterValue);
	}

	[RequiresUnreferencedCode("parentChapterValue's type cannot be statically analyzed")]
	private SchemaMapping FillMapping(DataSet dataset, DataTable datatable, string srcTable, DataReaderContainer dataReader, int schemaCount, DataColumn parentChapterColumn, object parentChapterValue)
	{
		SchemaMapping result = null;
		if (_hasFillErrorHandler)
		{
			try
			{
				result = FillMappingInternal(dataset, datatable, srcTable, dataReader, schemaCount, parentChapterColumn, parentChapterValue);
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
				ADP.TraceExceptionForCapture(e);
				OnFillErrorHandler(e, null, null);
			}
		}
		else
		{
			result = FillMappingInternal(dataset, datatable, srcTable, dataReader, schemaCount, parentChapterColumn, parentChapterValue);
		}
		return result;
	}

	private bool FillNextResult(DataReaderContainer dataReader)
	{
		bool result = true;
		if (_hasFillErrorHandler)
		{
			try
			{
				result = dataReader.NextResult();
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
				ADP.TraceExceptionForCapture(e);
				OnFillErrorHandler(e, null, null);
			}
		}
		else
		{
			result = dataReader.NextResult();
		}
		return result;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public virtual IDataParameter[] GetFillParameters()
	{
		return Array.Empty<IDataParameter>();
	}

	internal DataTableMapping GetTableMappingBySchemaAction(string sourceTableName, string dataSetTableName, MissingMappingAction mappingAction)
	{
		return DataTableMappingCollection.GetTableMappingBySchemaAction(_tableMappings, sourceTableName, dataSetTableName, mappingAction);
	}

	internal int IndexOfDataSetTable(string dataSetTable)
	{
		if (_tableMappings != null)
		{
			return TableMappings.IndexOfDataSetTable(dataSetTable);
		}
		return -1;
	}

	protected virtual void OnFillError(FillErrorEventArgs value)
	{
		((FillErrorEventHandler)base.Events[s_eventFillError])?.Invoke(this, value);
	}

	private void OnFillErrorHandler(Exception e, DataTable dataTable, object[] dataValues)
	{
		FillErrorEventArgs fillErrorEventArgs = new FillErrorEventArgs(dataTable, dataValues);
		fillErrorEventArgs.Errors = e;
		OnFillError(fillErrorEventArgs);
		if (!fillErrorEventArgs.Continue)
		{
			if (fillErrorEventArgs.Errors != null)
			{
				throw fillErrorEventArgs.Errors;
			}
			throw e;
		}
	}

	[RequiresUnreferencedCode("IDataReader's (built from adapter commands) schema table types cannot be statically analyzed.")]
	public virtual int Update(DataSet dataSet)
	{
		throw ADP.NotSupported();
	}

	private static DataTable[] AddDataTableToArray(DataTable[] tables, DataTable newTable)
	{
		for (int i = 0; i < tables.Length; i++)
		{
			if (tables[i] == newTable)
			{
				return tables;
			}
		}
		DataTable[] array = new DataTable[tables.Length + 1];
		for (int j = 0; j < tables.Length; j++)
		{
			array[j] = tables[j];
		}
		array[tables.Length] = newTable;
		return array;
	}

	private static string GetSourceTableName(string srcTable, int index)
	{
		if (index == 0)
		{
			return srcTable;
		}
		return srcTable + index.ToString(CultureInfo.InvariantCulture);
	}
}
