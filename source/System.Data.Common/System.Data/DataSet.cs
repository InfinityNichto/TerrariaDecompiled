using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data;

[Serializable]
[Designer("Microsoft.VSDesigner.Data.VS.DataSetDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[DefaultProperty("DataSetName")]
[ToolboxItem("Microsoft.VSDesigner.Data.VS.DataSetToolboxItem, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[XmlSchemaProvider("GetDataSetSchema")]
[XmlRoot("DataSet")]
[TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
public class DataSet : MarshalByValueComponent, IListSource, IXmlSerializable, ISupportInitializeNotification, ISupportInitialize, ISerializable
{
	private struct TableChanges
	{
		private readonly BitArray _rowChanges;

		internal int HasChanges { get; set; }

		internal bool this[int index]
		{
			get
			{
				return _rowChanges[index];
			}
			set
			{
				_rowChanges[index] = value;
				HasChanges++;
			}
		}

		internal TableChanges(int rowCount)
		{
			_rowChanges = new BitArray(rowCount);
			HasChanges = 0;
		}
	}

	private DataViewManager _defaultViewManager;

	private readonly DataTableCollection _tableCollection;

	private readonly DataRelationCollection _relationCollection;

	internal PropertyCollection _extendedProperties;

	private string _dataSetName = "NewDataSet";

	private string _datasetPrefix = string.Empty;

	internal string _namespaceURI = string.Empty;

	private bool _enforceConstraints = true;

	private bool _caseSensitive;

	private CultureInfo _culture;

	private bool _cultureUserSet;

	internal bool _fInReadXml;

	internal bool _fInLoadDiffgram;

	internal bool _fTopLevelTable;

	internal bool _fInitInProgress;

	internal bool _fEnableCascading = true;

	internal bool _fIsSchemaLoading;

	private bool _fBoundToDocument;

	internal string _mainTableName = string.Empty;

	private SerializationFormat _remotingFormat;

	private readonly object _defaultViewManagerLock = new object();

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	private static XmlSchemaComplexType s_schemaTypeForWSDL;

	internal bool _useDataSetSchemaOnly;

	internal bool _udtIsWrapped;

	[DefaultValue(SerializationFormat.Xml)]
	public SerializationFormat RemotingFormat
	{
		get
		{
			return _remotingFormat;
		}
		set
		{
			if (value != SerializationFormat.Binary && value != 0)
			{
				throw ExceptionBuilder.InvalidRemotingFormat(value);
			}
			_remotingFormat = value;
			for (int i = 0; i < Tables.Count; i++)
			{
				Tables[i].RemotingFormat = value;
			}
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual SchemaSerializationMode SchemaSerializationMode
	{
		get
		{
			return SchemaSerializationMode.IncludeSchema;
		}
		set
		{
			if (value != SchemaSerializationMode.IncludeSchema)
			{
				throw ExceptionBuilder.CannotChangeSchemaSerializationMode();
			}
		}
	}

	[DefaultValue(false)]
	public bool CaseSensitive
	{
		get
		{
			return _caseSensitive;
		}
		set
		{
			if (_caseSensitive == value)
			{
				return;
			}
			bool caseSensitive = _caseSensitive;
			_caseSensitive = value;
			if (!ValidateCaseConstraint())
			{
				_caseSensitive = caseSensitive;
				throw ExceptionBuilder.CannotChangeCaseLocale();
			}
			foreach (DataTable table in Tables)
			{
				table.SetCaseSensitiveValue(value, userSet: false, resetIndexes: true);
			}
		}
	}

	bool IListSource.ContainsListCollection => true;

	[Browsable(false)]
	public DataViewManager DefaultViewManager
	{
		get
		{
			if (_defaultViewManager == null)
			{
				lock (_defaultViewManagerLock)
				{
					if (_defaultViewManager == null)
					{
						_defaultViewManager = new DataViewManager(this, locked: true);
					}
				}
			}
			return _defaultViewManager;
		}
	}

	[DefaultValue(true)]
	public bool EnforceConstraints
	{
		get
		{
			return _enforceConstraints;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.set_EnforceConstraints|API> {0}, {1}", ObjectID, value);
			try
			{
				if (_enforceConstraints != value)
				{
					if (value)
					{
						EnableConstraints();
					}
					_enforceConstraints = value;
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	[DefaultValue("")]
	public string DataSetName
	{
		get
		{
			return _dataSetName;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataSet.set_DataSetName|API> {0}, '{1}'", ObjectID, value);
			if (value != _dataSetName)
			{
				if (value == null || value.Length == 0)
				{
					throw ExceptionBuilder.SetDataSetNameToEmpty();
				}
				DataTable dataTable = Tables[value, Namespace];
				if (dataTable != null && !dataTable._fNestedInDataset)
				{
					throw ExceptionBuilder.SetDataSetNameConflicting(value);
				}
				RaisePropertyChanging("DataSetName");
				_dataSetName = value;
			}
		}
	}

	[DefaultValue("")]
	public string Namespace
	{
		get
		{
			return _namespaceURI;
		}
		[param: AllowNull]
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataSet.set_Namespace|API> {0}, '{1}'", ObjectID, value);
			if (value == null)
			{
				value = string.Empty;
			}
			if (!(value != _namespaceURI))
			{
				return;
			}
			RaisePropertyChanging("Namespace");
			foreach (DataTable table in Tables)
			{
				if (table._tableNamespace == null && (table.NestedParentRelations.Length == 0 || (table.NestedParentRelations.Length == 1 && table.NestedParentRelations[0].ChildTable == table)))
				{
					if (Tables.Contains(table.TableName, value, checkProperty: false, caseSensitive: true))
					{
						throw ExceptionBuilder.DuplicateTableName2(table.TableName, value);
					}
					table.CheckCascadingNamespaceConflict(value);
					table.DoRaiseNamespaceChange();
				}
			}
			_namespaceURI = value;
			if (string.IsNullOrEmpty(value))
			{
				_datasetPrefix = string.Empty;
			}
		}
	}

	[DefaultValue("")]
	public string Prefix
	{
		get
		{
			return _datasetPrefix;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (XmlConvert.DecodeName(value) == value && XmlConvert.EncodeName(value) != value)
			{
				throw ExceptionBuilder.InvalidPrefix(value);
			}
			if (value != _datasetPrefix)
			{
				RaisePropertyChanging("Prefix");
				_datasetPrefix = value;
			}
		}
	}

	[Browsable(false)]
	public PropertyCollection ExtendedProperties => _extendedProperties ?? (_extendedProperties = new PropertyCollection());

	[Browsable(false)]
	public bool HasErrors
	{
		get
		{
			for (int i = 0; i < Tables.Count; i++)
			{
				if (Tables[i].HasErrors)
				{
					return true;
				}
			}
			return false;
		}
	}

	[Browsable(false)]
	public bool IsInitialized => !_fInitInProgress;

	public CultureInfo Locale
	{
		get
		{
			return _culture;
		}
		set
		{
			long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.set_Locale|API> {0}", ObjectID);
			try
			{
				if (value != null)
				{
					if (!_culture.Equals(value))
					{
						SetLocaleValue(value, userSet: true);
					}
					_cultureUserSet = true;
				}
			}
			finally
			{
				DataCommonEventSource.Log.ExitScope(scopeId);
			}
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ISite? Site
	{
		get
		{
			return base.Site;
		}
		set
		{
			ISite site = Site;
			if (value == null && site != null)
			{
				IContainer container = site.Container;
				if (container != null)
				{
					for (int i = 0; i < Tables.Count; i++)
					{
						if (Tables[i].Site != null)
						{
							container.Remove(Tables[i]);
						}
					}
				}
			}
			base.Site = value;
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public DataRelationCollection Relations => _relationCollection;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public DataTableCollection Tables => _tableCollection;

	internal bool FBoundToDocument
	{
		get
		{
			return _fBoundToDocument;
		}
		set
		{
			_fBoundToDocument = value;
		}
	}

	internal string MainTableName
	{
		get
		{
			return _mainTableName;
		}
		set
		{
			_mainTableName = value;
		}
	}

	internal int ObjectID => _objectID;

	internal event PropertyChangedEventHandler? PropertyChanging;

	public event MergeFailedEventHandler? MergeFailed;

	internal event DataRowCreatedEventHandler? DataRowCreated;

	internal event DataSetClearEventhandler? ClearFunctionCalled;

	public event EventHandler? Initialized;

	public DataSet()
	{
		GC.SuppressFinalize(this);
		DataCommonEventSource.Log.Trace("<ds.DataSet.DataSet|API> {0}", ObjectID);
		_tableCollection = new DataTableCollection(this);
		_relationCollection = new DataRelationCollection.DataSetRelationCollection(this);
		_culture = CultureInfo.CurrentCulture;
	}

	public DataSet(string dataSetName)
		: this()
	{
		DataSetName = dataSetName;
	}

	protected bool IsBinarySerialized(SerializationInfo info, StreamingContext context)
	{
		SerializationFormat serializationFormat = SerializationFormat.Xml;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Name == "DataSet.RemotingFormat")
			{
				serializationFormat = (SerializationFormat)enumerator.Value;
				break;
			}
		}
		return serializationFormat == SerializationFormat.Binary;
	}

	protected SchemaSerializationMode DetermineSchemaSerializationMode(SerializationInfo info, StreamingContext context)
	{
		SchemaSerializationMode result = SchemaSerializationMode.IncludeSchema;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Name == "SchemaSerializationMode.DataSet")
			{
				result = (SchemaSerializationMode)enumerator.Value;
				break;
			}
		}
		return result;
	}

	protected SchemaSerializationMode DetermineSchemaSerializationMode(XmlReader reader)
	{
		SchemaSerializationMode result = SchemaSerializationMode.IncludeSchema;
		reader.MoveToContent();
		if (reader.NodeType == XmlNodeType.Element && reader.HasAttributes)
		{
			string attribute = reader.GetAttribute("SchemaSerializationMode", "urn:schemas-microsoft-com:xml-msdata");
			if (string.Equals(attribute, "ExcludeSchema", StringComparison.OrdinalIgnoreCase))
			{
				result = SchemaSerializationMode.ExcludeSchema;
			}
			else if (string.Equals(attribute, "IncludeSchema", StringComparison.OrdinalIgnoreCase))
			{
				result = SchemaSerializationMode.IncludeSchema;
			}
			else if (attribute != null)
			{
				throw ExceptionBuilder.InvalidSchemaSerializationMode(typeof(SchemaSerializationMode), attribute);
			}
		}
		return result;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	protected void GetSerializationData(SerializationInfo info, StreamingContext context)
	{
		SerializationFormat remotingFormat = SerializationFormat.Xml;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (enumerator.Name == "DataSet.RemotingFormat")
			{
				remotingFormat = (SerializationFormat)enumerator.Value;
				break;
			}
		}
		DeserializeDataSetData(info, context, remotingFormat);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "CreateInstanceOfThisType's use of GetType uses only the parameterless constructor, but the annotations preserve all non-public constructors causing a warning for the serialization constructors. Those constructors won't be used here.")]
	protected DataSet(SerializationInfo info, StreamingContext context)
		: this(info, context, ConstructSchema: true)
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode", Justification = "CreateInstanceOfThisType's use of GetType uses only the parameterless constructor, but the annotations preserve all non-public constructors causing a warning for the serialization constructors. Those constructors won't be used here.")]
	protected DataSet(SerializationInfo info, StreamingContext context, bool ConstructSchema)
		: this()
	{
		SerializationFormat serializationFormat = SerializationFormat.Xml;
		SchemaSerializationMode schemaSerializationMode = SchemaSerializationMode.IncludeSchema;
		SerializationInfoEnumerator enumerator = info.GetEnumerator();
		while (enumerator.MoveNext())
		{
			string name = enumerator.Name;
			if (!(name == "DataSet.RemotingFormat"))
			{
				if (name == "SchemaSerializationMode.DataSet")
				{
					schemaSerializationMode = (SchemaSerializationMode)enumerator.Value;
				}
			}
			else
			{
				serializationFormat = (SerializationFormat)enumerator.Value;
			}
		}
		if (schemaSerializationMode == SchemaSerializationMode.ExcludeSchema)
		{
			InitializeDerivedDataSet();
		}
		if (serializationFormat != 0 || ConstructSchema)
		{
			DeserializeDataSet(info, context, serializationFormat, schemaSerializationMode);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Binary serialization is unsafe in general and is planned to be obsoleted. We do not want to mark interface or ctors of this class as unsafe as that would show many unnecessary warnings elsewhere.")]
	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		SerializationFormat remotingFormat = RemotingFormat;
		SerializeDataSet(info, context, remotingFormat);
	}

	protected virtual void InitializeDerivedDataSet()
	{
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SerializeDataSet(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat)
	{
		info.AddValue("DataSet.RemotingVersion", new Version(2, 0));
		if (remotingFormat != 0)
		{
			info.AddValue("DataSet.RemotingFormat", remotingFormat);
		}
		if (SchemaSerializationMode.IncludeSchema != SchemaSerializationMode)
		{
			info.AddValue("SchemaSerializationMode.DataSet", SchemaSerializationMode);
		}
		if (remotingFormat != 0)
		{
			if (SchemaSerializationMode == SchemaSerializationMode.IncludeSchema)
			{
				SerializeDataSetProperties(info, context);
				info.AddValue("DataSet.Tables.Count", Tables.Count);
				for (int i = 0; i < Tables.Count; i++)
				{
					BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(context.State, false));
					MemoryStream memoryStream = new MemoryStream();
					binaryFormatter.Serialize(memoryStream, Tables[i]);
					memoryStream.Position = 0L;
					info.AddValue(string.Format(CultureInfo.InvariantCulture, "DataSet.Tables_{0}", i), memoryStream.GetBuffer());
				}
				for (int j = 0; j < Tables.Count; j++)
				{
					Tables[j].SerializeConstraints(info, context, j, allConstraints: true);
				}
				SerializeRelations(info, context);
				for (int k = 0; k < Tables.Count; k++)
				{
					Tables[k].SerializeExpressionColumns(info, context, k);
				}
			}
			else
			{
				SerializeDataSetProperties(info, context);
			}
			for (int l = 0; l < Tables.Count; l++)
			{
				Tables[l].SerializeTableData(info, context, l);
			}
		}
		else
		{
			string xmlSchemaForRemoting = GetXmlSchemaForRemoting(null);
			string text = null;
			info.AddValue("XmlSchema", xmlSchemaForRemoting);
			StringBuilder sb = new StringBuilder(EstimatedXmlStringSize() * 2);
			StringWriter stringWriter = new StringWriter(sb, CultureInfo.InvariantCulture);
			XmlTextWriter writer = new XmlTextWriter(stringWriter);
			WriteXml(writer, XmlWriteMode.DiffGram);
			text = stringWriter.ToString();
			info.AddValue("XmlDiffGram", text);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void DeserializeDataSet(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat, SchemaSerializationMode schemaSerializationMode)
	{
		DeserializeDataSetSchema(info, context, remotingFormat, schemaSerializationMode);
		DeserializeDataSetData(info, context, remotingFormat);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void DeserializeDataSetSchema(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat, SchemaSerializationMode schemaSerializationMode)
	{
		if (remotingFormat != 0)
		{
			if (schemaSerializationMode == SchemaSerializationMode.IncludeSchema)
			{
				DeserializeDataSetProperties(info, context);
				int @int = info.GetInt32("DataSet.Tables.Count");
				for (int i = 0; i < @int; i++)
				{
					byte[] buffer = (byte[])info.GetValue(string.Format(CultureInfo.InvariantCulture, "DataSet.Tables_{0}", i), typeof(byte[]));
					MemoryStream memoryStream = new MemoryStream(buffer);
					memoryStream.Position = 0L;
					BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(context.State, false));
					DataTable table = (DataTable)binaryFormatter.Deserialize(memoryStream);
					Tables.Add(table);
				}
				for (int j = 0; j < @int; j++)
				{
					Tables[j].DeserializeConstraints(info, context, j, allConstraints: true);
				}
				DeserializeRelations(info, context);
				for (int k = 0; k < @int; k++)
				{
					Tables[k].DeserializeExpressionColumns(info, context, k);
				}
			}
			else
			{
				DeserializeDataSetProperties(info, context);
			}
		}
		else
		{
			string text = (string)info.GetValue("XmlSchema", typeof(string));
			if (text != null)
			{
				ReadXmlSchema(new XmlTextReader(new StringReader(text)), denyResolving: true);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void DeserializeDataSetData(SerializationInfo info, StreamingContext context, SerializationFormat remotingFormat)
	{
		if (remotingFormat != 0)
		{
			for (int i = 0; i < Tables.Count; i++)
			{
				Tables[i].DeserializeTableData(info, context, i);
			}
			return;
		}
		string text = (string)info.GetValue("XmlDiffGram", typeof(string));
		if (text != null)
		{
			ReadXml(new XmlTextReader(new StringReader(text)), XmlReadMode.DiffGram);
		}
	}

	private void SerializeDataSetProperties(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("DataSet.DataSetName", DataSetName);
		info.AddValue("DataSet.Namespace", Namespace);
		info.AddValue("DataSet.Prefix", Prefix);
		info.AddValue("DataSet.CaseSensitive", CaseSensitive);
		info.AddValue("DataSet.LocaleLCID", Locale.LCID);
		info.AddValue("DataSet.EnforceConstraints", EnforceConstraints);
		info.AddValue("DataSet.ExtendedProperties", ExtendedProperties);
	}

	private void DeserializeDataSetProperties(SerializationInfo info, StreamingContext context)
	{
		_dataSetName = info.GetString("DataSet.DataSetName");
		_namespaceURI = info.GetString("DataSet.Namespace");
		_datasetPrefix = info.GetString("DataSet.Prefix");
		_caseSensitive = info.GetBoolean("DataSet.CaseSensitive");
		int culture = (int)info.GetValue("DataSet.LocaleLCID", typeof(int));
		_culture = new CultureInfo(culture);
		_cultureUserSet = true;
		_enforceConstraints = info.GetBoolean("DataSet.EnforceConstraints");
		_extendedProperties = (PropertyCollection)info.GetValue("DataSet.ExtendedProperties", typeof(PropertyCollection));
	}

	private void SerializeRelations(SerializationInfo info, StreamingContext context)
	{
		ArrayList arrayList = new ArrayList();
		foreach (DataRelation relation in Relations)
		{
			int[] array = new int[relation.ParentColumns.Length + 1];
			array[0] = Tables.IndexOf(relation.ParentTable);
			for (int i = 1; i < array.Length; i++)
			{
				array[i] = relation.ParentColumns[i - 1].Ordinal;
			}
			int[] array2 = new int[relation.ChildColumns.Length + 1];
			array2[0] = Tables.IndexOf(relation.ChildTable);
			for (int j = 1; j < array2.Length; j++)
			{
				array2[j] = relation.ChildColumns[j - 1].Ordinal;
			}
			ArrayList arrayList2 = new ArrayList();
			arrayList2.Add(relation.RelationName);
			arrayList2.Add(array);
			arrayList2.Add(array2);
			arrayList2.Add(relation.Nested);
			arrayList2.Add(relation._extendedProperties);
			arrayList.Add(arrayList2);
		}
		info.AddValue("DataSet.Relations", arrayList);
	}

	private void DeserializeRelations(SerializationInfo info, StreamingContext context)
	{
		ArrayList arrayList = (ArrayList)info.GetValue("DataSet.Relations", typeof(ArrayList));
		foreach (ArrayList item in arrayList)
		{
			string relationName = (string)item[0];
			int[] array = (int[])item[1];
			int[] array2 = (int[])item[2];
			bool nested = (bool)item[3];
			PropertyCollection extendedProperties = (PropertyCollection)item[4];
			DataColumn[] array3 = new DataColumn[array.Length - 1];
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i] = Tables[array[0]].Columns[array[i + 1]];
			}
			DataColumn[] array4 = new DataColumn[array2.Length - 1];
			for (int j = 0; j < array4.Length; j++)
			{
				array4[j] = Tables[array2[0]].Columns[array2[j + 1]];
			}
			DataRelation dataRelation = new DataRelation(relationName, array3, array4, createConstraints: false);
			dataRelation.CheckMultipleNested = false;
			dataRelation.Nested = nested;
			dataRelation._extendedProperties = extendedProperties;
			Relations.Add(dataRelation);
			dataRelation.CheckMultipleNested = true;
		}
	}

	internal void FailedEnableConstraints()
	{
		EnforceConstraints = false;
		throw ExceptionBuilder.EnforceConstraint();
	}

	internal void RestoreEnforceConstraints(bool value)
	{
		_enforceConstraints = value;
	}

	internal void EnableConstraints()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.EnableConstraints|INFO> {0}", ObjectID);
		try
		{
			bool flag = false;
			ConstraintEnumerator constraintEnumerator = new ConstraintEnumerator(this);
			while (constraintEnumerator.GetNext())
			{
				Constraint constraint = constraintEnumerator.GetConstraint();
				flag |= constraint.IsConstraintViolated();
			}
			foreach (DataTable table in Tables)
			{
				foreach (DataColumn column in table.Columns)
				{
					if (!column.AllowDBNull)
					{
						flag |= column.IsNotAllowDBNullViolated();
					}
					if (column.MaxLength >= 0)
					{
						flag |= column.IsMaxLengthViolated();
					}
				}
			}
			if (flag)
			{
				FailedEnableConstraints();
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal void SetLocaleValue(CultureInfo value, bool userSet)
	{
		bool flag = false;
		bool flag2 = false;
		int num = 0;
		CultureInfo culture = _culture;
		bool cultureUserSet = _cultureUserSet;
		try
		{
			_culture = value;
			_cultureUserSet = userSet;
			foreach (DataTable table in Tables)
			{
				if (!table.ShouldSerializeLocale())
				{
					table.SetLocaleValue(value, userSet: false, resetIndexes: false);
				}
			}
			flag = ValidateLocaleConstraint();
			if (!flag)
			{
				return;
			}
			flag = false;
			foreach (DataTable table2 in Tables)
			{
				num++;
				if (!table2.ShouldSerializeLocale())
				{
					table2.SetLocaleValue(value, userSet: false, resetIndexes: true);
				}
			}
			flag = true;
		}
		catch
		{
			flag2 = true;
			throw;
		}
		finally
		{
			if (!flag)
			{
				_culture = culture;
				_cultureUserSet = cultureUserSet;
				foreach (DataTable table3 in Tables)
				{
					if (!table3.ShouldSerializeLocale())
					{
						table3.SetLocaleValue(culture, userSet: false, resetIndexes: false);
					}
				}
				try
				{
					for (int i = 0; i < num; i++)
					{
						if (!Tables[i].ShouldSerializeLocale())
						{
							Tables[i].SetLocaleValue(culture, userSet: false, resetIndexes: true);
						}
					}
				}
				catch (Exception e) when (ADP.IsCatchableExceptionType(e))
				{
					ADP.TraceExceptionWithoutRethrow(e);
				}
				if (!flag2)
				{
					throw ExceptionBuilder.CannotChangeCaseLocale(null);
				}
			}
		}
	}

	internal bool ShouldSerializeLocale()
	{
		return _cultureUserSet;
	}

	protected virtual bool ShouldSerializeRelations()
	{
		return true;
	}

	protected virtual bool ShouldSerializeTables()
	{
		return true;
	}

	public void AcceptChanges()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.AcceptChanges|API> {0}", ObjectID);
		try
		{
			for (int i = 0; i < Tables.Count; i++)
			{
				Tables[i].AcceptChanges();
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void BeginInit()
	{
		_fInitInProgress = true;
	}

	public void EndInit()
	{
		Tables.FinishInitCollection();
		for (int i = 0; i < Tables.Count; i++)
		{
			Tables[i].Columns.FinishInitCollection();
		}
		for (int j = 0; j < Tables.Count; j++)
		{
			Tables[j].Constraints.FinishInitConstraints();
		}
		((DataRelationCollection.DataSetRelationCollection)Relations).FinishInitRelations();
		_fInitInProgress = false;
		OnInitialized();
	}

	public void Clear()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Clear|API> {0}", ObjectID);
		try
		{
			OnClearFunctionCalled(null);
			bool enforceConstraints = EnforceConstraints;
			EnforceConstraints = false;
			for (int i = 0; i < Tables.Count; i++)
			{
				Tables[i].Clear();
			}
			EnforceConstraints = enforceConstraints;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	private DataSet CreateInstanceOfThisType()
	{
		return (DataSet)Activator.CreateInstance(GetType(), nonPublic: true);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public virtual DataSet Clone()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Clone|API> {0}", ObjectID);
		try
		{
			DataSet dataSet = CreateInstanceOfThisType();
			if (dataSet.Tables.Count > 0)
			{
				dataSet.Reset();
			}
			dataSet.DataSetName = DataSetName;
			dataSet.CaseSensitive = CaseSensitive;
			dataSet._culture = _culture;
			dataSet._cultureUserSet = _cultureUserSet;
			dataSet.EnforceConstraints = EnforceConstraints;
			dataSet.Namespace = Namespace;
			dataSet.Prefix = Prefix;
			dataSet.RemotingFormat = RemotingFormat;
			dataSet._fIsSchemaLoading = true;
			DataTableCollection tables = Tables;
			for (int i = 0; i < tables.Count; i++)
			{
				DataTable dataTable = tables[i].Clone(dataSet);
				dataTable._tableNamespace = tables[i].Namespace;
				dataSet.Tables.Add(dataTable);
			}
			for (int j = 0; j < tables.Count; j++)
			{
				ConstraintCollection constraints = tables[j].Constraints;
				for (int k = 0; k < constraints.Count; k++)
				{
					if (!(constraints[k] is UniqueConstraint))
					{
						ForeignKeyConstraint foreignKeyConstraint = (ForeignKeyConstraint)constraints[k];
						if (foreignKeyConstraint.Table != foreignKeyConstraint.RelatedTable)
						{
							dataSet.Tables[j].Constraints.Add(constraints[k].Clone(dataSet));
						}
					}
				}
			}
			DataRelationCollection relations = Relations;
			for (int l = 0; l < relations.Count; l++)
			{
				DataRelation dataRelation = relations[l].Clone(dataSet);
				dataRelation.CheckMultipleNested = false;
				dataSet.Relations.Add(dataRelation);
				dataRelation.CheckMultipleNested = true;
			}
			if (_extendedProperties != null)
			{
				foreach (object key in _extendedProperties.Keys)
				{
					dataSet.ExtendedProperties[key] = _extendedProperties[key];
				}
			}
			foreach (DataTable table in Tables)
			{
				foreach (DataColumn column in table.Columns)
				{
					if (column.Expression.Length != 0)
					{
						dataSet.Tables[table.TableName, table.Namespace].Columns[column.ColumnName].CopyExpressionFrom(column);
					}
				}
			}
			for (int m = 0; m < tables.Count; m++)
			{
				dataSet.Tables[m]._tableNamespace = tables[m]._tableNamespace;
			}
			dataSet._fIsSchemaLoading = false;
			return dataSet;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public DataSet Copy()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Copy|API> {0}", ObjectID);
		try
		{
			DataSet dataSet = Clone();
			bool enforceConstraints = dataSet.EnforceConstraints;
			dataSet.EnforceConstraints = false;
			foreach (DataTable table2 in Tables)
			{
				DataTable table = dataSet.Tables[table2.TableName, table2.Namespace];
				foreach (DataRow row in table2.Rows)
				{
					table2.CopyRow(table, row);
				}
			}
			dataSet.EnforceConstraints = enforceConstraints;
			return dataSet;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal int EstimatedXmlStringSize()
	{
		int num = 100;
		for (int i = 0; i < Tables.Count; i++)
		{
			int num2 = Tables[i].TableName.Length + 4 << 2;
			DataTable dataTable = Tables[i];
			for (int j = 0; j < dataTable.Columns.Count; j++)
			{
				num2 += dataTable.Columns[j].ColumnName.Length + 4 << 2;
				num2 += 20;
			}
			num += dataTable.Rows.Count * num2;
		}
		return num;
	}

	public DataSet? GetChanges()
	{
		return GetChanges(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified);
	}

	public DataSet? GetChanges(DataRowState rowStates)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetChanges|API> {0}, rowStates={1}", ObjectID, rowStates);
		try
		{
			DataSet dataSet = null;
			bool enforceConstraints = false;
			if (((uint)rowStates & 0xFFFFFFE1u) != 0)
			{
				throw ExceptionBuilder.InvalidRowState(rowStates);
			}
			TableChanges[] array = new TableChanges[Tables.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new TableChanges(Tables[i].Rows.Count);
			}
			MarkModifiedRows(array, rowStates);
			for (int j = 0; j < array.Length; j++)
			{
				if (0 >= array[j].HasChanges)
				{
					continue;
				}
				if (dataSet == null)
				{
					dataSet = Clone();
					enforceConstraints = dataSet.EnforceConstraints;
					dataSet.EnforceConstraints = false;
				}
				DataTable dataTable = Tables[j];
				DataTable table = dataSet.Tables[dataTable.TableName, dataTable.Namespace];
				int num = 0;
				while (0 < array[j].HasChanges)
				{
					if (array[j][num])
					{
						dataTable.CopyRow(table, dataTable.Rows[num]);
						array[j].HasChanges--;
					}
					num++;
				}
			}
			if (dataSet != null)
			{
				dataSet.EnforceConstraints = enforceConstraints;
			}
			return dataSet;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	private void MarkModifiedRows(TableChanges[] bitMatrix, DataRowState rowStates)
	{
		for (int i = 0; i < bitMatrix.Length; i++)
		{
			DataRowCollection rows = Tables[i].Rows;
			int count = rows.Count;
			for (int j = 0; j < count; j++)
			{
				DataRow dataRow = rows[j];
				DataRowState rowState = dataRow.RowState;
				if ((rowStates & rowState) != 0 && !bitMatrix[i][j])
				{
					bitMatrix[i][j] = true;
					if (DataRowState.Deleted != rowState)
					{
						MarkRelatedRowsAsModified(bitMatrix, dataRow);
					}
				}
			}
		}
	}

	private void MarkRelatedRowsAsModified(TableChanges[] bitMatrix, DataRow row)
	{
		DataRelationCollection parentRelations = row.Table.ParentRelations;
		int count = parentRelations.Count;
		for (int i = 0; i < count; i++)
		{
			DataRow[] parentRows = row.GetParentRows(parentRelations[i], DataRowVersion.Current);
			DataRow[] array = parentRows;
			foreach (DataRow dataRow in array)
			{
				int num = Tables.IndexOf(dataRow.Table);
				int index = dataRow.Table.Rows.IndexOf(dataRow);
				if (!bitMatrix[num][index])
				{
					bitMatrix[num][index] = true;
					if (DataRowState.Deleted != dataRow.RowState)
					{
						MarkRelatedRowsAsModified(bitMatrix, dataRow);
					}
				}
			}
		}
	}

	IList IListSource.GetList()
	{
		return DefaultViewManager;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal string GetRemotingDiffGram(DataTable table)
	{
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
		xmlTextWriter.Formatting = Formatting.Indented;
		new NewDiffgramGen(table, writeHierarchy: false).Save(xmlTextWriter, table);
		return stringWriter.ToString();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public string GetXml()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetXml|API> {0}", ObjectID);
		try
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
			xmlTextWriter.Formatting = Formatting.Indented;
			new XmlDataTreeWriter(this).Save(xmlTextWriter, writeSchema: false);
			return stringWriter.ToString();
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public string GetXmlSchema()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetXmlSchema|API> {0}", ObjectID);
		try
		{
			StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
			xmlTextWriter.Formatting = Formatting.Indented;
			new XmlTreeGen(SchemaFormat.Public).Save(this, xmlTextWriter);
			return stringWriter.ToString();
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal string GetXmlSchemaForRemoting(DataTable table)
	{
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
		xmlTextWriter.Formatting = Formatting.Indented;
		if (table == null)
		{
			if (SchemaSerializationMode == SchemaSerializationMode.ExcludeSchema)
			{
				new XmlTreeGen(SchemaFormat.RemotingSkipSchema).Save(this, xmlTextWriter);
			}
			else
			{
				new XmlTreeGen(SchemaFormat.Remoting).Save(this, xmlTextWriter);
			}
		}
		else
		{
			new XmlTreeGen(SchemaFormat.Remoting).Save(table, xmlTextWriter);
		}
		return stringWriter.ToString();
	}

	public bool HasChanges()
	{
		return HasChanges(DataRowState.Added | DataRowState.Deleted | DataRowState.Modified);
	}

	public bool HasChanges(DataRowState rowStates)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.HasChanges|API> {0}, rowStates={1}", ObjectID, (int)rowStates);
		try
		{
			if (((uint)rowStates & 0xFFFFFFE0u) != 0)
			{
				throw ExceptionBuilder.ArgumentOutOfRange("rowState");
			}
			for (int i = 0; i < Tables.Count; i++)
			{
				DataTable dataTable = Tables[i];
				for (int j = 0; j < dataTable.Rows.Count; j++)
				{
					DataRow dataRow = dataTable.Rows[j];
					if ((dataRow.RowState & rowStates) != 0)
					{
						return true;
					}
				}
			}
			return false;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void InferXmlSchema(XmlReader? reader, string[]? nsArray)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.InferXmlSchema|API> {0}", ObjectID);
		try
		{
			if (reader != null)
			{
				XmlDocument xmlDocument = new XmlDocument();
				if (reader.NodeType == XmlNodeType.Element)
				{
					XmlNode newChild = xmlDocument.ReadNode(reader);
					xmlDocument.AppendChild(newChild);
				}
				else
				{
					xmlDocument.Load(reader);
				}
				if (xmlDocument.DocumentElement != null)
				{
					InferSchema(xmlDocument, nsArray, XmlReadMode.InferSchema);
				}
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void InferXmlSchema(Stream? stream, string[]? nsArray)
	{
		if (stream != null)
		{
			InferXmlSchema(new XmlTextReader(stream), nsArray);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void InferXmlSchema(TextReader? reader, string[]? nsArray)
	{
		if (reader != null)
		{
			InferXmlSchema(new XmlTextReader(reader), nsArray);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void InferXmlSchema(string fileName, string[]? nsArray)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(fileName);
		try
		{
			InferXmlSchema(xmlTextReader, nsArray);
		}
		finally
		{
			xmlTextReader.Close();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void ReadXmlSchema(XmlReader? reader)
	{
		ReadXmlSchema(reader, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ReadXmlSchema(XmlReader reader, bool denyResolving)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXmlSchema|INFO> {0}, reader, denyResolving={1}", ObjectID, denyResolving);
		try
		{
			int depth = -1;
			if (reader == null)
			{
				return;
			}
			if (reader is XmlTextReader)
			{
				((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.None;
			}
			XmlDocument xmlDocument = new XmlDocument();
			if (reader.NodeType == XmlNodeType.Element)
			{
				depth = reader.Depth;
			}
			reader.MoveToContent();
			if (reader.NodeType != XmlNodeType.Element)
			{
				return;
			}
			if (reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
			{
				ReadXDRSchema(reader);
				return;
			}
			if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
			{
				ReadXSDSchema(reader, denyResolving);
				return;
			}
			if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
			{
				throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
			}
			XmlElement xmlElement = xmlDocument.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
			if (reader.HasAttributes)
			{
				int attributeCount = reader.AttributeCount;
				for (int i = 0; i < attributeCount; i++)
				{
					reader.MoveToAttribute(i);
					if (reader.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/"))
					{
						xmlElement.SetAttribute(reader.Name, reader.GetAttribute(i));
						continue;
					}
					XmlAttribute xmlAttribute = xmlElement.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
					xmlAttribute.Prefix = reader.Prefix;
					xmlAttribute.Value = reader.GetAttribute(i);
				}
			}
			reader.Read();
			while (MoveToElement(reader, depth))
			{
				if (reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
				{
					ReadXDRSchema(reader);
					return;
				}
				if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
				{
					ReadXSDSchema(reader, denyResolving);
					return;
				}
				if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
				{
					throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
				}
				XmlNode newChild = xmlDocument.ReadNode(reader);
				xmlElement.AppendChild(newChild);
			}
			ReadEndElement(reader);
			xmlDocument.AppendChild(xmlElement);
			InferSchema(xmlDocument, null, XmlReadMode.Auto);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal bool MoveToElement(XmlReader reader, int depth)
	{
		while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element && reader.Depth > depth)
		{
			reader.Read();
		}
		return reader.NodeType == XmlNodeType.Element;
	}

	private static void MoveToElement(XmlReader reader)
	{
		while (!reader.EOF && reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element)
		{
			reader.Read();
		}
	}

	internal void ReadEndElement(XmlReader reader)
	{
		while (reader.NodeType == XmlNodeType.Whitespace)
		{
			reader.Skip();
		}
		if (reader.NodeType == XmlNodeType.None)
		{
			reader.Skip();
		}
		else if (reader.NodeType == XmlNodeType.EndElement)
		{
			reader.ReadEndElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ReadXSDSchema(XmlReader reader, bool denyResolving)
	{
		XmlSchemaSet xmlSchemaSet = new XmlSchemaSet();
		int num = 1;
		if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema" && reader.HasAttributes)
		{
			string attribute = reader.GetAttribute("schemafragmentcount", "urn:schemas-microsoft-com:xml-msdata");
			if (!string.IsNullOrEmpty(attribute))
			{
				num = int.Parse(attribute, null);
			}
		}
		while (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
		{
			XmlSchema schema = XmlSchema.Read(reader, null);
			xmlSchemaSet.Add(schema);
			ReadEndElement(reader);
			if (--num > 0)
			{
				MoveToElement(reader);
			}
			while (reader.NodeType == XmlNodeType.Whitespace)
			{
				reader.Skip();
			}
		}
		xmlSchemaSet.Compile();
		XSDSchema xSDSchema = new XSDSchema();
		xSDSchema.LoadSchema(xmlSchemaSet, this);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ReadXDRSchema(XmlReader reader)
	{
		XmlDocument xmlDocument = new XmlDocument();
		XmlNode xmlNode = xmlDocument.ReadNode(reader);
		xmlDocument.AppendChild(xmlNode);
		XDRSchema xDRSchema = new XDRSchema(this, fInline: false);
		DataSetName = xmlDocument.DocumentElement.LocalName;
		xDRSchema.LoadSchema((XmlElement)xmlNode, this);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void ReadXmlSchema(Stream? stream)
	{
		if (stream != null)
		{
			ReadXmlSchema(new XmlTextReader(stream), denyResolving: false);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void ReadXmlSchema(TextReader? reader)
	{
		if (reader != null)
		{
			ReadXmlSchema(new XmlTextReader(reader), denyResolving: false);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void ReadXmlSchema(string fileName)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(fileName);
		try
		{
			ReadXmlSchema(xmlTextReader, denyResolving: false);
		}
		finally
		{
			xmlTextReader.Close();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(Stream? stream)
	{
		WriteXmlSchema(stream, SchemaFormat.Public, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(Stream? stream, Converter<Type, string> multipleTargetConverter)
	{
		ADP.CheckArgumentNull(multipleTargetConverter, "multipleTargetConverter");
		WriteXmlSchema(stream, SchemaFormat.Public, multipleTargetConverter);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(string fileName)
	{
		WriteXmlSchema(fileName, SchemaFormat.Public, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(string fileName, Converter<Type, string> multipleTargetConverter)
	{
		ADP.CheckArgumentNull(multipleTargetConverter, "multipleTargetConverter");
		WriteXmlSchema(fileName, SchemaFormat.Public, multipleTargetConverter);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(TextWriter? writer)
	{
		WriteXmlSchema(writer, SchemaFormat.Public, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(TextWriter? writer, Converter<Type, string> multipleTargetConverter)
	{
		ADP.CheckArgumentNull(multipleTargetConverter, "multipleTargetConverter");
		WriteXmlSchema(writer, SchemaFormat.Public, multipleTargetConverter);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(XmlWriter? writer)
	{
		WriteXmlSchema(writer, SchemaFormat.Public, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXmlSchema(XmlWriter? writer, Converter<Type, string> multipleTargetConverter)
	{
		ADP.CheckArgumentNull(multipleTargetConverter, "multipleTargetConverter");
		WriteXmlSchema(writer, SchemaFormat.Public, multipleTargetConverter);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteXmlSchema(string fileName, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
	{
		XmlTextWriter xmlTextWriter = new XmlTextWriter(fileName, null);
		try
		{
			xmlTextWriter.Formatting = Formatting.Indented;
			xmlTextWriter.WriteStartDocument(standalone: true);
			WriteXmlSchema(xmlTextWriter, schemaFormat, multipleTargetConverter);
			xmlTextWriter.WriteEndDocument();
		}
		finally
		{
			xmlTextWriter.Close();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteXmlSchema(Stream stream, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
	{
		if (stream != null)
		{
			XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, null);
			xmlTextWriter.Formatting = Formatting.Indented;
			WriteXmlSchema(xmlTextWriter, schemaFormat, multipleTargetConverter);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteXmlSchema(TextWriter writer, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
	{
		if (writer != null)
		{
			XmlTextWriter xmlTextWriter = new XmlTextWriter(writer);
			xmlTextWriter.Formatting = Formatting.Indented;
			WriteXmlSchema(xmlTextWriter, schemaFormat, multipleTargetConverter);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteXmlSchema(XmlWriter writer, SchemaFormat schemaFormat, Converter<Type, string> multipleTargetConverter)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXmlSchema|INFO> {0}, schemaFormat={1}", ObjectID, schemaFormat);
		try
		{
			if (writer != null)
			{
				XmlTreeGen xmlTreeGen = null;
				xmlTreeGen = ((schemaFormat != SchemaFormat.WebService || SchemaSerializationMode != SchemaSerializationMode.ExcludeSchema || writer.WriteState != WriteState.Element) ? new XmlTreeGen(schemaFormat) : new XmlTreeGen(SchemaFormat.WebServiceSkipSchema));
				xmlTreeGen.Save(this, null, writer, writeHierarchy: false, multipleTargetConverter);
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(XmlReader? reader)
	{
		return ReadXml(reader, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlReadMode ReadXml(XmlReader reader, bool denyResolving)
	{
		IDisposable disposable = null;
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXml|INFO> {0}, denyResolving={1}", ObjectID, denyResolving);
		try
		{
			disposable = TypeLimiter.EnterRestrictedScope(this);
			DataTable.DSRowDiffIdUsageSection dSRowDiffIdUsageSection = default(DataTable.DSRowDiffIdUsageSection);
			try
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool isXdr = false;
				int depth = -1;
				XmlReadMode result = XmlReadMode.Auto;
				bool flag4 = false;
				bool flag5 = false;
				dSRowDiffIdUsageSection.Prepare(this);
				if (reader == null)
				{
					return result;
				}
				if (Tables.Count == 0)
				{
					flag4 = true;
				}
				if (reader is XmlTextReader)
				{
					((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;
				}
				XmlDocument xmlDocument = new XmlDocument();
				XmlDataLoader xmlDataLoader = null;
				reader.MoveToContent();
				if (reader.NodeType == XmlNodeType.Element)
				{
					depth = reader.Depth;
				}
				if (reader.NodeType == XmlNodeType.Element)
				{
					if (reader.LocalName == "diffgram" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
					{
						ReadXmlDiffgram(reader);
						ReadEndElement(reader);
						return XmlReadMode.DiffGram;
					}
					if (reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
					{
						ReadXDRSchema(reader);
						return XmlReadMode.ReadSchema;
					}
					if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
					{
						ReadXSDSchema(reader, denyResolving);
						return XmlReadMode.ReadSchema;
					}
					if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
					{
						throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
					}
					XmlElement xmlElement = xmlDocument.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
					if (reader.HasAttributes)
					{
						int attributeCount = reader.AttributeCount;
						for (int i = 0; i < attributeCount; i++)
						{
							reader.MoveToAttribute(i);
							if (reader.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/"))
							{
								xmlElement.SetAttribute(reader.Name, reader.GetAttribute(i));
								continue;
							}
							XmlAttribute xmlAttribute = xmlElement.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
							xmlAttribute.Prefix = reader.Prefix;
							xmlAttribute.Value = reader.GetAttribute(i);
						}
					}
					reader.Read();
					string value = reader.Value;
					while (MoveToElement(reader, depth))
					{
						if (reader.LocalName == "diffgram" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
						{
							ReadXmlDiffgram(reader);
							result = XmlReadMode.DiffGram;
						}
						if (!flag2 && !flag && reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
						{
							ReadXDRSchema(reader);
							flag2 = true;
							isXdr = true;
							continue;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
						{
							ReadXSDSchema(reader, denyResolving);
							flag2 = true;
							continue;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
						{
							throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
						}
						if (reader.LocalName == "diffgram" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
						{
							ReadXmlDiffgram(reader);
							flag3 = true;
							result = XmlReadMode.DiffGram;
							continue;
						}
						while (!reader.EOF && reader.NodeType == XmlNodeType.Whitespace)
						{
							reader.Read();
						}
						if (reader.NodeType != XmlNodeType.Element)
						{
							continue;
						}
						flag = true;
						if (!flag2 && Tables.Count == 0)
						{
							XmlNode newChild = xmlDocument.ReadNode(reader);
							xmlElement.AppendChild(newChild);
							continue;
						}
						if (xmlDataLoader == null)
						{
							xmlDataLoader = new XmlDataLoader(this, isXdr, xmlElement, ignoreSchema: false);
						}
						xmlDataLoader.LoadData(reader);
						flag5 = true;
						result = (flag2 ? XmlReadMode.ReadSchema : XmlReadMode.IgnoreSchema);
					}
					ReadEndElement(reader);
					bool flag6 = false;
					bool fTopLevelTable = _fTopLevelTable;
					if (!flag2 && Tables.Count == 0 && !xmlElement.HasChildNodes)
					{
						_fTopLevelTable = true;
						flag6 = true;
						if (value != null && value.Length > 0)
						{
							xmlElement.InnerText = value;
						}
					}
					if (!flag4 && value != null && value.Length > 0)
					{
						xmlElement.InnerText = value;
					}
					xmlDocument.AppendChild(xmlElement);
					if (xmlDataLoader == null)
					{
						xmlDataLoader = new XmlDataLoader(this, isXdr, xmlElement, ignoreSchema: false);
					}
					if (!flag4 && !flag5)
					{
						XmlElement documentElement = xmlDocument.DocumentElement;
						if (documentElement.ChildNodes.Count == 0 || (documentElement.ChildNodes.Count == 1 && documentElement.FirstChild.GetType() == typeof(XmlText)))
						{
							bool fTopLevelTable2 = _fTopLevelTable;
							if (DataSetName != documentElement.Name && _namespaceURI != documentElement.NamespaceURI && Tables.Contains(documentElement.Name, (documentElement.NamespaceURI.Length == 0) ? null : documentElement.NamespaceURI, checkProperty: false, caseSensitive: true))
							{
								_fTopLevelTable = true;
							}
							try
							{
								xmlDataLoader.LoadData(xmlDocument);
							}
							finally
							{
								_fTopLevelTable = fTopLevelTable2;
							}
						}
					}
					if (!flag3)
					{
						if (!flag2 && Tables.Count == 0)
						{
							InferSchema(xmlDocument, null, XmlReadMode.Auto);
							result = XmlReadMode.InferSchema;
							xmlDataLoader.FromInference = true;
							try
							{
								xmlDataLoader.LoadData(xmlDocument);
							}
							finally
							{
								xmlDataLoader.FromInference = false;
							}
						}
						if (flag6)
						{
							_fTopLevelTable = fTopLevelTable;
						}
					}
				}
				return result;
			}
			finally
			{
			}
		}
		finally
		{
			disposable?.Dispose();
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(Stream? stream)
	{
		if (stream == null)
		{
			return XmlReadMode.Auto;
		}
		XmlTextReader xmlTextReader = new XmlTextReader(stream);
		xmlTextReader.XmlResolver = null;
		return ReadXml(xmlTextReader, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(TextReader? reader)
	{
		if (reader == null)
		{
			return XmlReadMode.Auto;
		}
		XmlTextReader xmlTextReader = new XmlTextReader(reader);
		xmlTextReader.XmlResolver = null;
		return ReadXml(xmlTextReader, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(string fileName)
	{
		XmlTextReader xmlTextReader = new XmlTextReader(fileName);
		xmlTextReader.XmlResolver = null;
		try
		{
			return ReadXml(xmlTextReader, denyResolving: false);
		}
		finally
		{
			xmlTextReader.Close();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void InferSchema(XmlDocument xdoc, string[] excludedNamespaces, XmlReadMode mode)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.InferSchema|INFO> {0}, mode={1}", ObjectID, mode);
		try
		{
			if (excludedNamespaces == null)
			{
				excludedNamespaces = Array.Empty<string>();
			}
			XmlNodeReader instanceDocument = new XmlIgnoreNamespaceReader(xdoc, excludedNamespaces);
			XmlSchemaInference xmlSchemaInference = new XmlSchemaInference();
			xmlSchemaInference.Occurrence = XmlSchemaInference.InferenceOption.Relaxed;
			xmlSchemaInference.TypeInference = ((mode != XmlReadMode.InferTypedSchema) ? XmlSchemaInference.InferenceOption.Relaxed : XmlSchemaInference.InferenceOption.Restricted);
			XmlSchemaSet xmlSchemaSet = xmlSchemaInference.InferSchema(instanceDocument);
			xmlSchemaSet.Compile();
			XSDSchema xSDSchema = new XSDSchema();
			xSDSchema.FromInference = true;
			try
			{
				xSDSchema.LoadSchema(xmlSchemaSet, this);
			}
			finally
			{
				xSDSchema.FromInference = false;
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	private bool IsEmpty()
	{
		foreach (DataTable table in Tables)
		{
			if (table.Rows.Count > 0)
			{
				return false;
			}
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void ReadXmlDiffgram(XmlReader reader)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXmlDiffgram|INFO> {0}", ObjectID);
		try
		{
			int depth = reader.Depth;
			bool enforceConstraints = EnforceConstraints;
			EnforceConstraints = false;
			bool flag = IsEmpty();
			DataSet dataSet;
			if (flag)
			{
				dataSet = this;
			}
			else
			{
				dataSet = Clone();
				dataSet.EnforceConstraints = false;
			}
			foreach (DataTable table in dataSet.Tables)
			{
				table.Rows._nullInList = 0;
			}
			reader.MoveToContent();
			if (reader.LocalName != "diffgram" && reader.NamespaceURI != "urn:schemas-microsoft-com:xml-diffgram-v1")
			{
				return;
			}
			reader.Read();
			if (reader.NodeType == XmlNodeType.Whitespace)
			{
				MoveToElement(reader, reader.Depth - 1);
			}
			dataSet._fInLoadDiffgram = true;
			if (reader.Depth > depth)
			{
				if (reader.NamespaceURI != "urn:schemas-microsoft-com:xml-diffgram-v1" && reader.NamespaceURI != "urn:schemas-microsoft-com:xml-msdata")
				{
					XmlDocument xmlDocument = new XmlDocument();
					XmlElement topNode = xmlDocument.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
					reader.Read();
					if (reader.NodeType == XmlNodeType.Whitespace)
					{
						MoveToElement(reader, reader.Depth - 1);
					}
					if (reader.Depth - 1 > depth)
					{
						XmlDataLoader xmlDataLoader = new XmlDataLoader(dataSet, IsXdr: false, topNode, ignoreSchema: false);
						xmlDataLoader._isDiffgram = true;
						xmlDataLoader.LoadData(reader);
					}
					ReadEndElement(reader);
					if (reader.NodeType == XmlNodeType.Whitespace)
					{
						MoveToElement(reader, reader.Depth - 1);
					}
				}
				if ((reader.LocalName == "before" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1") || (reader.LocalName == "errors" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1"))
				{
					XMLDiffLoader xMLDiffLoader = new XMLDiffLoader();
					xMLDiffLoader.LoadDiffGram(dataSet, reader);
				}
				while (reader.Depth > depth)
				{
					reader.Read();
				}
				ReadEndElement(reader);
			}
			foreach (DataTable table2 in dataSet.Tables)
			{
				if (table2.Rows._nullInList > 0)
				{
					throw ExceptionBuilder.RowInsertMissing(table2.TableName);
				}
			}
			dataSet._fInLoadDiffgram = false;
			foreach (DataTable table3 in dataSet.Tables)
			{
				DataRelation[] nestedParentRelations = table3.NestedParentRelations;
				DataRelation[] array = nestedParentRelations;
				foreach (DataRelation dataRelation in array)
				{
					if (dataRelation.ParentTable != table3)
					{
						continue;
					}
					foreach (DataRow row in table3.Rows)
					{
						DataRelation[] array2 = nestedParentRelations;
						foreach (DataRelation rel in array2)
						{
							row.CheckForLoops(rel);
						}
					}
				}
			}
			if (!flag)
			{
				Merge(dataSet);
				if (_dataSetName == "NewDataSet")
				{
					_dataSetName = dataSet._dataSetName;
				}
				dataSet.EnforceConstraints = enforceConstraints;
			}
			EnforceConstraints = enforceConstraints;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(XmlReader? reader, XmlReadMode mode)
	{
		return ReadXml(reader, mode, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlReadMode ReadXml(XmlReader reader, XmlReadMode mode, bool denyResolving)
	{
		IDisposable disposable = null;
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ReadXml|INFO> {0}, mode={1}, denyResolving={2}", ObjectID, mode, denyResolving);
		try
		{
			disposable = TypeLimiter.EnterRestrictedScope(this);
			XmlReadMode result = mode;
			if (reader == null)
			{
				return result;
			}
			if (mode == XmlReadMode.Auto)
			{
				return ReadXml(reader);
			}
			DataTable.DSRowDiffIdUsageSection dSRowDiffIdUsageSection = default(DataTable.DSRowDiffIdUsageSection);
			try
			{
				bool flag = false;
				bool flag2 = false;
				bool isXdr = false;
				int depth = -1;
				dSRowDiffIdUsageSection.Prepare(this);
				if (reader is XmlTextReader)
				{
					((XmlTextReader)reader).WhitespaceHandling = WhitespaceHandling.Significant;
				}
				XmlDocument xmlDocument = new XmlDocument();
				if (mode != XmlReadMode.Fragment && reader.NodeType == XmlNodeType.Element)
				{
					depth = reader.Depth;
				}
				reader.MoveToContent();
				XmlDataLoader xmlDataLoader = null;
				if (reader.NodeType == XmlNodeType.Element)
				{
					XmlElement xmlElement = null;
					if (mode == XmlReadMode.Fragment)
					{
						xmlDocument.AppendChild(xmlDocument.CreateElement("ds_sqlXmlWraPPeR"));
						xmlElement = xmlDocument.DocumentElement;
					}
					else
					{
						if (reader.LocalName == "diffgram" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
						{
							if (mode == XmlReadMode.DiffGram || mode == XmlReadMode.IgnoreSchema)
							{
								ReadXmlDiffgram(reader);
								ReadEndElement(reader);
							}
							else
							{
								reader.Skip();
							}
							return result;
						}
						if (reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
						{
							if (mode != XmlReadMode.IgnoreSchema && mode != XmlReadMode.InferSchema && mode != XmlReadMode.InferTypedSchema)
							{
								ReadXDRSchema(reader);
							}
							else
							{
								reader.Skip();
							}
							return result;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
						{
							if (mode != XmlReadMode.IgnoreSchema && mode != XmlReadMode.InferSchema && mode != XmlReadMode.InferTypedSchema)
							{
								ReadXSDSchema(reader, denyResolving);
							}
							else
							{
								reader.Skip();
							}
							return result;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
						{
							throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
						}
						xmlElement = xmlDocument.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
						if (reader.HasAttributes)
						{
							int attributeCount = reader.AttributeCount;
							for (int i = 0; i < attributeCount; i++)
							{
								reader.MoveToAttribute(i);
								if (reader.NamespaceURI.Equals("http://www.w3.org/2000/xmlns/"))
								{
									xmlElement.SetAttribute(reader.Name, reader.GetAttribute(i));
									continue;
								}
								XmlAttribute xmlAttribute = xmlElement.SetAttributeNode(reader.LocalName, reader.NamespaceURI);
								xmlAttribute.Prefix = reader.Prefix;
								xmlAttribute.Value = reader.GetAttribute(i);
							}
						}
						reader.Read();
					}
					while (MoveToElement(reader, depth))
					{
						if (reader.LocalName == "Schema" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-data")
						{
							if (!flag && !flag2 && mode != XmlReadMode.IgnoreSchema && mode != XmlReadMode.InferSchema && mode != XmlReadMode.InferTypedSchema)
							{
								ReadXDRSchema(reader);
								flag = true;
								isXdr = true;
							}
							else
							{
								reader.Skip();
							}
							continue;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema")
						{
							if (mode != XmlReadMode.IgnoreSchema && mode != XmlReadMode.InferSchema && mode != XmlReadMode.InferTypedSchema)
							{
								ReadXSDSchema(reader, denyResolving);
								flag = true;
							}
							else
							{
								reader.Skip();
							}
							continue;
						}
						if (reader.LocalName == "diffgram" && reader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
						{
							if (mode == XmlReadMode.DiffGram || mode == XmlReadMode.IgnoreSchema)
							{
								ReadXmlDiffgram(reader);
								result = XmlReadMode.DiffGram;
							}
							else
							{
								reader.Skip();
							}
							continue;
						}
						if (reader.LocalName == "schema" && reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
						{
							throw ExceptionBuilder.DataSetUnsupportedSchema("http://www.w3.org/2001/XMLSchema");
						}
						if (mode == XmlReadMode.DiffGram)
						{
							reader.Skip();
							continue;
						}
						flag2 = true;
						if (mode == XmlReadMode.InferSchema || mode == XmlReadMode.InferTypedSchema)
						{
							XmlNode newChild = xmlDocument.ReadNode(reader);
							xmlElement.AppendChild(newChild);
							continue;
						}
						if (xmlDataLoader == null)
						{
							xmlDataLoader = new XmlDataLoader(this, isXdr, xmlElement, mode == XmlReadMode.IgnoreSchema);
						}
						xmlDataLoader.LoadData(reader);
					}
					ReadEndElement(reader);
					xmlDocument.AppendChild(xmlElement);
					if (xmlDataLoader == null)
					{
						xmlDataLoader = new XmlDataLoader(this, isXdr, mode == XmlReadMode.IgnoreSchema);
					}
					switch (mode)
					{
					case XmlReadMode.DiffGram:
						return result;
					case XmlReadMode.InferSchema:
					case XmlReadMode.InferTypedSchema:
						InferSchema(xmlDocument, null, mode);
						result = XmlReadMode.InferSchema;
						xmlDataLoader.FromInference = true;
						try
						{
							xmlDataLoader.LoadData(xmlDocument);
						}
						finally
						{
							xmlDataLoader.FromInference = false;
						}
						break;
					}
				}
				return result;
			}
			finally
			{
			}
		}
		finally
		{
			disposable?.Dispose();
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(Stream? stream, XmlReadMode mode)
	{
		if (stream == null)
		{
			return XmlReadMode.Auto;
		}
		XmlTextReader xmlTextReader = ((mode == XmlReadMode.Fragment) ? new XmlTextReader(stream, XmlNodeType.Element, null) : new XmlTextReader(stream));
		xmlTextReader.XmlResolver = null;
		return ReadXml(xmlTextReader, mode, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(TextReader? reader, XmlReadMode mode)
	{
		if (reader == null)
		{
			return XmlReadMode.Auto;
		}
		XmlTextReader xmlTextReader = ((mode == XmlReadMode.Fragment) ? new XmlTextReader(reader.ReadToEnd(), XmlNodeType.Element, null) : new XmlTextReader(reader));
		xmlTextReader.XmlResolver = null;
		return ReadXml(xmlTextReader, mode, denyResolving: false);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public XmlReadMode ReadXml(string fileName, XmlReadMode mode)
	{
		XmlTextReader xmlTextReader = null;
		if (mode == XmlReadMode.Fragment)
		{
			FileStream xmlFragment = new FileStream(fileName, FileMode.Open);
			xmlTextReader = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);
		}
		else
		{
			xmlTextReader = new XmlTextReader(fileName);
		}
		xmlTextReader.XmlResolver = null;
		try
		{
			return ReadXml(xmlTextReader, mode, denyResolving: false);
		}
		finally
		{
			xmlTextReader.Close();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(Stream? stream)
	{
		WriteXml(stream, XmlWriteMode.IgnoreSchema);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(TextWriter? writer)
	{
		WriteXml(writer, XmlWriteMode.IgnoreSchema);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(XmlWriter? writer)
	{
		WriteXml(writer, XmlWriteMode.IgnoreSchema);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(string fileName)
	{
		WriteXml(fileName, XmlWriteMode.IgnoreSchema);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(Stream? stream, XmlWriteMode mode)
	{
		if (stream != null)
		{
			XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, null);
			xmlTextWriter.Formatting = Formatting.Indented;
			WriteXml(xmlTextWriter, mode);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(TextWriter? writer, XmlWriteMode mode)
	{
		if (writer != null)
		{
			XmlTextWriter xmlTextWriter = new XmlTextWriter(writer);
			xmlTextWriter.Formatting = Formatting.Indented;
			WriteXml(xmlTextWriter, mode);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(XmlWriter? writer, XmlWriteMode mode)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXml|API> {0}, mode={1}", ObjectID, mode);
		try
		{
			if (writer != null)
			{
				if (mode == XmlWriteMode.DiffGram)
				{
					new NewDiffgramGen(this).Save(writer);
				}
				else
				{
					new XmlDataTreeWriter(this).Save(writer, mode == XmlWriteMode.WriteSchema);
				}
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void WriteXml(string fileName, XmlWriteMode mode)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.WriteXml|API> {0}, fileName='{1}', mode={2}", ObjectID, fileName, (int)mode);
		XmlTextWriter xmlTextWriter = new XmlTextWriter(fileName, null);
		try
		{
			xmlTextWriter.Formatting = Formatting.Indented;
			xmlTextWriter.WriteStartDocument(standalone: true);
			if (mode == XmlWriteMode.DiffGram)
			{
				new NewDiffgramGen(this).Save(xmlTextWriter);
			}
			else
			{
				new XmlDataTreeWriter(this).Save(xmlTextWriter, mode == XmlWriteMode.WriteSchema);
			}
			xmlTextWriter.WriteEndDocument();
		}
		finally
		{
			xmlTextWriter.Close();
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataSet dataSet)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}", ObjectID, dataSet?.ObjectID ?? 0);
		try
		{
			Merge(dataSet, preserveChanges: false, MissingSchemaAction.Add);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataSet dataSet, bool preserveChanges)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}, preserveChanges={2}", ObjectID, dataSet?.ObjectID ?? 0, preserveChanges);
		try
		{
			Merge(dataSet, preserveChanges, MissingSchemaAction.Add);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataSet dataSet, bool preserveChanges, MissingSchemaAction missingSchemaAction)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, dataSet={1}, preserveChanges={2}, missingSchemaAction={3}", ObjectID, dataSet?.ObjectID ?? 0, preserveChanges, missingSchemaAction);
		try
		{
			if (dataSet == null)
			{
				throw ExceptionBuilder.ArgumentNull("dataSet");
			}
			if ((uint)(missingSchemaAction - 1) <= 3u)
			{
				Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
				merger.MergeDataSet(dataSet);
				return;
			}
			throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataTable table)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, table={1}", ObjectID, table?.ObjectID ?? 0);
		try
		{
			Merge(table, preserveChanges: false, MissingSchemaAction.Add);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataTable table, bool preserveChanges, MissingSchemaAction missingSchemaAction)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, table={1}, preserveChanges={2}, missingSchemaAction={3}", ObjectID, table?.ObjectID ?? 0, preserveChanges, missingSchemaAction);
		try
		{
			if (table == null)
			{
				throw ExceptionBuilder.ArgumentNull("table");
			}
			if ((uint)(missingSchemaAction - 1) <= 3u)
			{
				Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
				merger.MergeTable(table);
				return;
			}
			throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataRow[] rows)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, rows", ObjectID);
		try
		{
			Merge(rows, preserveChanges: false, MissingSchemaAction.Add);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Merge(DataRow[] rows, bool preserveChanges, MissingSchemaAction missingSchemaAction)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Merge|API> {0}, preserveChanges={1}, missingSchemaAction={2}", ObjectID, preserveChanges, missingSchemaAction);
		try
		{
			if (rows == null)
			{
				throw ExceptionBuilder.ArgumentNull("rows");
			}
			if ((uint)(missingSchemaAction - 1) <= 3u)
			{
				Merger merger = new Merger(this, preserveChanges, missingSchemaAction);
				merger.MergeRows(rows);
				return;
			}
			throw ADP.InvalidMissingSchemaAction(missingSchemaAction);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected virtual void OnPropertyChanging(PropertyChangedEventArgs pcevent)
	{
		this.PropertyChanging?.Invoke(this, pcevent);
	}

	internal void OnMergeFailed(MergeFailedEventArgs mfevent)
	{
		if (this.MergeFailed != null)
		{
			this.MergeFailed(this, mfevent);
			return;
		}
		throw ExceptionBuilder.MergeFailed(mfevent.Conflict);
	}

	internal void RaiseMergeFailed(DataTable table, string conflict, MissingSchemaAction missingSchemaAction)
	{
		if (MissingSchemaAction.Error == missingSchemaAction)
		{
			throw ExceptionBuilder.MergeFailed(conflict);
		}
		OnMergeFailed(new MergeFailedEventArgs(table, conflict));
	}

	internal void OnDataRowCreated(DataRow row)
	{
		this.DataRowCreated?.Invoke(this, row);
	}

	internal void OnClearFunctionCalled(DataTable table)
	{
		this.ClearFunctionCalled?.Invoke(this, table);
	}

	private void OnInitialized()
	{
		this.Initialized?.Invoke(this, EventArgs.Empty);
	}

	protected internal virtual void OnRemoveTable(DataTable table)
	{
	}

	internal void OnRemovedTable(DataTable table)
	{
		_defaultViewManager?.DataViewSettings.Remove(table);
	}

	protected virtual void OnRemoveRelation(DataRelation relation)
	{
	}

	internal void OnRemoveRelationHack(DataRelation relation)
	{
		OnRemoveRelation(relation);
	}

	protected internal void RaisePropertyChanging(string name)
	{
		OnPropertyChanging(new PropertyChangedEventArgs(name));
	}

	internal DataTable[] TopLevelTables()
	{
		return TopLevelTables(forSchema: false);
	}

	internal DataTable[] TopLevelTables(bool forSchema)
	{
		List<DataTable> list = new List<DataTable>();
		if (forSchema)
		{
			for (int i = 0; i < Tables.Count; i++)
			{
				DataTable dataTable = Tables[i];
				if (dataTable.NestedParentsCount > 1 || dataTable.SelfNested)
				{
					list.Add(dataTable);
				}
			}
		}
		for (int j = 0; j < Tables.Count; j++)
		{
			DataTable dataTable2 = Tables[j];
			if (dataTable2.NestedParentsCount == 0 && !list.Contains(dataTable2))
			{
				list.Add(dataTable2);
			}
		}
		if (list.Count != 0)
		{
			return list.ToArray();
		}
		return Array.Empty<DataTable>();
	}

	public virtual void RejectChanges()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.RejectChanges|API> {0}", ObjectID);
		try
		{
			bool enforceConstraints = EnforceConstraints;
			EnforceConstraints = false;
			for (int i = 0; i < Tables.Count; i++)
			{
				Tables[i].RejectChanges();
			}
			EnforceConstraints = enforceConstraints;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public virtual void Reset()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Reset|API> {0}", ObjectID);
		try
		{
			for (int i = 0; i < Tables.Count; i++)
			{
				ConstraintCollection constraints = Tables[i].Constraints;
				int num = 0;
				while (num < constraints.Count)
				{
					if (constraints[num] is ForeignKeyConstraint)
					{
						constraints.Remove(constraints[num]);
					}
					else
					{
						num++;
					}
				}
			}
			Clear();
			Relations.Clear();
			Tables.Clear();
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal bool ValidateCaseConstraint()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ValidateCaseConstraint|INFO> {0}", ObjectID);
		try
		{
			DataRelation dataRelation = null;
			for (int i = 0; i < Relations.Count; i++)
			{
				dataRelation = Relations[i];
				if (dataRelation.ChildTable.CaseSensitive != dataRelation.ParentTable.CaseSensitive)
				{
					return false;
				}
			}
			ForeignKeyConstraint foreignKeyConstraint = null;
			ConstraintCollection constraintCollection = null;
			for (int j = 0; j < Tables.Count; j++)
			{
				constraintCollection = Tables[j].Constraints;
				for (int k = 0; k < constraintCollection.Count; k++)
				{
					if (constraintCollection[k] is ForeignKeyConstraint)
					{
						foreignKeyConstraint = (ForeignKeyConstraint)constraintCollection[k];
						if (foreignKeyConstraint.Table.CaseSensitive != foreignKeyConstraint.RelatedTable.CaseSensitive)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal bool ValidateLocaleConstraint()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.ValidateLocaleConstraint|INFO> {0}", ObjectID);
		try
		{
			DataRelation dataRelation = null;
			for (int i = 0; i < Relations.Count; i++)
			{
				dataRelation = Relations[i];
				if (dataRelation.ChildTable.Locale.LCID != dataRelation.ParentTable.Locale.LCID)
				{
					return false;
				}
			}
			ForeignKeyConstraint foreignKeyConstraint = null;
			ConstraintCollection constraintCollection = null;
			for (int j = 0; j < Tables.Count; j++)
			{
				constraintCollection = Tables[j].Constraints;
				for (int k = 0; k < constraintCollection.Count; k++)
				{
					if (constraintCollection[k] is ForeignKeyConstraint)
					{
						foreignKeyConstraint = (ForeignKeyConstraint)constraintCollection[k];
						if (foreignKeyConstraint.Table.Locale.LCID != foreignKeyConstraint.RelatedTable.Locale.LCID)
						{
							return false;
						}
					}
				}
			}
			return true;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal DataTable FindTable(DataTable baseTable, PropertyDescriptor[] props, int propStart)
	{
		if (props.Length < propStart + 1)
		{
			return baseTable;
		}
		PropertyDescriptor propertyDescriptor = props[propStart];
		if (baseTable == null)
		{
			if (propertyDescriptor is DataTablePropertyDescriptor)
			{
				return FindTable(((DataTablePropertyDescriptor)propertyDescriptor).Table, props, propStart + 1);
			}
			return null;
		}
		if (propertyDescriptor is DataRelationPropertyDescriptor)
		{
			return FindTable(((DataRelationPropertyDescriptor)propertyDescriptor).Relation.ChildTable, props, propStart + 1);
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	protected virtual void ReadXmlSerializable(XmlReader reader)
	{
		_useDataSetSchemaOnly = false;
		_udtIsWrapped = false;
		if (reader.HasAttributes)
		{
			if (reader.MoveToAttribute("xsi:nil"))
			{
				string attribute = reader.GetAttribute("xsi:nil");
				if (string.Equals(attribute, "true", StringComparison.Ordinal))
				{
					MoveToElement(reader, 1);
					return;
				}
			}
			if (reader.MoveToAttribute("msdata:UseDataSetSchemaOnly"))
			{
				string attribute2 = reader.GetAttribute("msdata:UseDataSetSchemaOnly");
				if (string.Equals(attribute2, "true", StringComparison.Ordinal) || string.Equals(attribute2, "1", StringComparison.Ordinal))
				{
					_useDataSetSchemaOnly = true;
				}
				else if (!string.Equals(attribute2, "false", StringComparison.Ordinal) && !string.Equals(attribute2, "0", StringComparison.Ordinal))
				{
					throw ExceptionBuilder.InvalidAttributeValue("UseDataSetSchemaOnly", attribute2);
				}
			}
			if (reader.MoveToAttribute("msdata:UDTColumnValueWrapped"))
			{
				string attribute3 = reader.GetAttribute("msdata:UDTColumnValueWrapped");
				if (string.Equals(attribute3, "true", StringComparison.Ordinal) || string.Equals(attribute3, "1", StringComparison.Ordinal))
				{
					_udtIsWrapped = true;
				}
				else if (!string.Equals(attribute3, "false", StringComparison.Ordinal) && !string.Equals(attribute3, "0", StringComparison.Ordinal))
				{
					throw ExceptionBuilder.InvalidAttributeValue("UDTColumnValueWrapped", attribute3);
				}
			}
		}
		ReadXml(reader, XmlReadMode.DiffGram, denyResolving: true);
	}

	protected virtual XmlSchema? GetSchemaSerializable()
	{
		return null;
	}

	public static XmlSchemaComplexType GetDataSetSchema(XmlSchemaSet? schemaSet)
	{
		if (s_schemaTypeForWSDL == null)
		{
			XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
			XmlSchemaSequence xmlSchemaSequence = new XmlSchemaSequence();
			XmlSchemaAny xmlSchemaAny = new XmlSchemaAny();
			xmlSchemaAny.Namespace = "http://www.w3.org/2001/XMLSchema";
			xmlSchemaAny.MinOccurs = 0m;
			xmlSchemaAny.ProcessContents = XmlSchemaContentProcessing.Lax;
			xmlSchemaSequence.Items.Add(xmlSchemaAny);
			xmlSchemaAny = new XmlSchemaAny();
			xmlSchemaAny.Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1";
			xmlSchemaAny.MinOccurs = 0m;
			xmlSchemaAny.ProcessContents = XmlSchemaContentProcessing.Lax;
			xmlSchemaSequence.Items.Add(xmlSchemaAny);
			xmlSchemaSequence.MaxOccurs = decimal.MaxValue;
			xmlSchemaComplexType.Particle = xmlSchemaSequence;
			s_schemaTypeForWSDL = xmlSchemaComplexType;
		}
		return s_schemaTypeForWSDL;
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		if (GetType() == typeof(DataSet))
		{
			return null;
		}
		MemoryStream memoryStream = new MemoryStream();
		XmlWriter xmlWriter = new XmlTextWriter(memoryStream, null);
		if (xmlWriter != null)
		{
			WriteXmlSchema(this, xmlWriter);
		}
		memoryStream.Position = 0L;
		return XmlSchema.Read(new XmlTextReader(memoryStream), null);
	}

	[RequiresUnreferencedCode("DataSet.GetSchema uses TypeDescriptor and XmlSerialization underneath which are not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
	private static void WriteXmlSchema(DataSet ds, XmlWriter writer)
	{
		new XmlTreeGen(SchemaFormat.WebService).Save(ds, writer);
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		bool flag = true;
		XmlTextReader xmlTextReader = null;
		IXmlTextParser xmlTextParser = reader as IXmlTextParser;
		if (xmlTextParser != null)
		{
			flag = xmlTextParser.Normalized;
			xmlTextParser.Normalized = false;
		}
		else
		{
			xmlTextReader = reader as XmlTextReader;
			if (xmlTextReader != null)
			{
				flag = xmlTextReader.Normalization;
				xmlTextReader.Normalization = false;
			}
		}
		ReadXmlSerializableInternal(reader);
		if (xmlTextParser != null)
		{
			xmlTextParser.Normalized = flag;
		}
		else if (xmlTextReader != null)
		{
			xmlTextReader.Normalization = flag;
		}
	}

	[RequiresUnreferencedCode("DataSet.ReadXml uses XmlSerialization underneath which is not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
	private void ReadXmlSerializableInternal(XmlReader reader)
	{
		ReadXmlSerializable(reader);
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		WriteXmlInternal(writer);
	}

	[RequiresUnreferencedCode("DataSet.WriteXml uses XmlSerialization underneath which is not trimming safe. Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteXmlInternal(XmlWriter writer)
	{
		WriteXmlSchema(writer, SchemaFormat.WebService, null);
		WriteXml(writer, XmlWriteMode.DiffGram);
	}

	[RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
	public virtual void Load(IDataReader reader, LoadOption loadOption, FillErrorEventHandler? errorHandler, params DataTable[] tables)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.Load|API> reader, loadOption={0}", loadOption);
		try
		{
			foreach (DataTable dataTable in tables)
			{
				ADP.CheckArgumentNull(dataTable, "tables");
				if (dataTable.DataSet != this)
				{
					throw ExceptionBuilder.TableNotInTheDataSet(dataTable.TableName);
				}
			}
			LoadAdapter loadAdapter = new LoadAdapter();
			loadAdapter.FillLoadOption = loadOption;
			loadAdapter.MissingSchemaAction = MissingSchemaAction.AddWithKey;
			if (errorHandler != null)
			{
				loadAdapter.FillError += errorHandler;
			}
			loadAdapter.FillFromReader(tables, reader, 0, 0);
			if (!reader.IsClosed && !reader.NextResult())
			{
				reader.Close();
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
	public void Load(IDataReader reader, LoadOption loadOption, params DataTable[] tables)
	{
		Load(reader, loadOption, null, tables);
	}

	[RequiresUnreferencedCode("Using LoadOption may cause members from types used in the expression column to be trimmed if not referenced directly.")]
	public void Load(IDataReader reader, LoadOption loadOption, params string[] tables)
	{
		ADP.CheckArgumentNull(tables, "tables");
		DataTable[] array = new DataTable[tables.Length];
		for (int i = 0; i < tables.Length; i++)
		{
			DataTable dataTable = Tables[tables[i]];
			if (dataTable == null)
			{
				dataTable = new DataTable(tables[i]);
				Tables.Add(dataTable);
			}
			array[i] = dataTable;
		}
		Load(reader, loadOption, null, array);
	}

	public DataTableReader CreateDataReader()
	{
		if (Tables.Count == 0)
		{
			throw ExceptionBuilder.CannotCreateDataReaderOnEmptyDataSet();
		}
		DataTable[] array = new DataTable[Tables.Count];
		for (int i = 0; i < Tables.Count; i++)
		{
			array[i] = Tables[i];
		}
		return CreateDataReader(array);
	}

	public DataTableReader CreateDataReader(params DataTable[] dataTables)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataSet.GetDataReader|API> {0}", ObjectID);
		try
		{
			if (dataTables.Length == 0)
			{
				throw ExceptionBuilder.DataTableReaderArgumentIsEmpty();
			}
			for (int i = 0; i < dataTables.Length; i++)
			{
				if (dataTables[i] == null)
				{
					throw ExceptionBuilder.ArgumentContainsNullValue();
				}
			}
			return new DataTableReader(dataTables);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}
}
