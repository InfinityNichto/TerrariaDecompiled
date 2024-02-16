using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Data.Common;

[TypeConverter(typeof(DataTableMappingConverter))]
public sealed class DataTableMapping : MarshalByRefObject, ITableMapping, ICloneable
{
	internal sealed class DataTableMappingConverter : ExpandableObjectConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (typeof(InstanceDescriptor) == destinationType)
			{
				return true;
			}
			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (null == destinationType)
			{
				throw ADP.ArgumentNull("destinationType");
			}
			if (typeof(InstanceDescriptor) == destinationType && value is DataTableMapping)
			{
				DataTableMapping dataTableMapping = (DataTableMapping)value;
				DataColumnMapping[] array = new DataColumnMapping[dataTableMapping.ColumnMappings.Count];
				dataTableMapping.ColumnMappings.CopyTo(array, 0);
				object[] arguments = new object[3] { dataTableMapping.SourceTable, dataTableMapping.DataSetTable, array };
				Type[] types = new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(DataColumnMapping[])
				};
				ConstructorInfo constructor = typeof(DataTableMapping).GetConstructor(types);
				return new InstanceDescriptor(constructor, arguments);
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private DataTableMappingCollection _parent;

	private DataColumnMappingCollection _columnMappings;

	private string _dataSetTableName;

	private string _sourceTableName;

	IColumnMappingCollection ITableMapping.ColumnMappings => ColumnMappings;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public DataColumnMappingCollection ColumnMappings
	{
		get
		{
			DataColumnMappingCollection dataColumnMappingCollection = _columnMappings;
			if (dataColumnMappingCollection == null)
			{
				dataColumnMappingCollection = (_columnMappings = new DataColumnMappingCollection());
			}
			return dataColumnMappingCollection;
		}
	}

	[DefaultValue("")]
	public string DataSetTable
	{
		get
		{
			return _dataSetTableName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_dataSetTableName = value;
		}
	}

	internal DataTableMappingCollection? Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			_parent = value;
		}
	}

	[DefaultValue("")]
	public string SourceTable
	{
		get
		{
			return _sourceTableName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			if (Parent != null && ADP.SrcCompare(_sourceTableName, value) != 0)
			{
				Parent.ValidateSourceTable(-1, value);
			}
			_sourceTableName = value;
		}
	}

	public DataTableMapping()
	{
	}

	public DataTableMapping(string? sourceTable, string? dataSetTable)
	{
		SourceTable = sourceTable;
		DataSetTable = dataSetTable;
	}

	public DataTableMapping(string? sourceTable, string? dataSetTable, DataColumnMapping[]? columnMappings)
	{
		SourceTable = sourceTable;
		DataSetTable = dataSetTable;
		if (columnMappings != null && columnMappings.Length != 0)
		{
			ColumnMappings.AddRange(columnMappings);
		}
	}

	object ICloneable.Clone()
	{
		DataTableMapping dataTableMapping = new DataTableMapping();
		dataTableMapping._dataSetTableName = _dataSetTableName;
		dataTableMapping._sourceTableName = _sourceTableName;
		if (_columnMappings != null && 0 < ColumnMappings.Count)
		{
			DataColumnMappingCollection columnMappings = dataTableMapping.ColumnMappings;
			foreach (ICloneable columnMapping in ColumnMappings)
			{
				columnMappings.Add(columnMapping.Clone());
			}
		}
		return dataTableMapping;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public DataColumn? GetDataColumn(string sourceColumn, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type? dataType, DataTable dataTable, MissingMappingAction mappingAction, MissingSchemaAction schemaAction)
	{
		return DataColumnMappingCollection.GetDataColumn(_columnMappings, sourceColumn, dataType, dataTable, mappingAction, schemaAction);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public DataColumnMapping? GetColumnMappingBySchemaAction(string sourceColumn, MissingMappingAction mappingAction)
	{
		return DataColumnMappingCollection.GetColumnMappingBySchemaAction(_columnMappings, sourceColumn, mappingAction);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public DataTable? GetDataTableBySchemaAction(DataSet dataSet, MissingSchemaAction schemaAction)
	{
		if (dataSet == null)
		{
			throw ADP.ArgumentNull("dataSet");
		}
		string dataSetTable = DataSetTable;
		if (string.IsNullOrEmpty(dataSetTable))
		{
			return null;
		}
		DataTableCollection tables = dataSet.Tables;
		int num = tables.IndexOf(dataSetTable);
		if (0 <= num && num < tables.Count)
		{
			return tables[num];
		}
		switch (schemaAction)
		{
		case MissingSchemaAction.Add:
		case MissingSchemaAction.AddWithKey:
			return new DataTable(dataSetTable);
		case MissingSchemaAction.Ignore:
			return null;
		case MissingSchemaAction.Error:
			throw ADP.MissingTableSchema(dataSetTable, SourceTable);
		default:
			throw ADP.InvalidMissingSchemaAction(schemaAction);
		}
	}

	public override string ToString()
	{
		return SourceTable;
	}
}
