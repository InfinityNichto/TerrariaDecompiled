using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Data.Common;

[TypeConverter(typeof(DataColumnMappingConverter))]
public sealed class DataColumnMapping : MarshalByRefObject, IColumnMapping, ICloneable
{
	internal sealed class DataColumnMappingConverter : ExpandableObjectConverter
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
			if (typeof(InstanceDescriptor) == destinationType && value is DataColumnMapping)
			{
				DataColumnMapping dataColumnMapping = (DataColumnMapping)value;
				object[] arguments = new object[2] { dataColumnMapping.SourceColumn, dataColumnMapping.DataSetColumn };
				Type[] types = new Type[2]
				{
					typeof(string),
					typeof(string)
				};
				ConstructorInfo constructor = typeof(DataColumnMapping).GetConstructor(types);
				return new InstanceDescriptor(constructor, arguments);
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private DataColumnMappingCollection _parent;

	private string _dataSetColumnName;

	private string _sourceColumnName;

	[DefaultValue("")]
	public string DataSetColumn
	{
		get
		{
			return _dataSetColumnName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			_dataSetColumnName = value;
		}
	}

	internal DataColumnMappingCollection? Parent
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
	public string SourceColumn
	{
		get
		{
			return _sourceColumnName ?? string.Empty;
		}
		[param: AllowNull]
		set
		{
			if (Parent != null && ADP.SrcCompare(_sourceColumnName, value) != 0)
			{
				Parent.ValidateSourceColumn(-1, value);
			}
			_sourceColumnName = value;
		}
	}

	public DataColumnMapping()
	{
	}

	public DataColumnMapping(string? sourceColumn, string? dataSetColumn)
	{
		SourceColumn = sourceColumn;
		DataSetColumn = dataSetColumn;
	}

	object ICloneable.Clone()
	{
		DataColumnMapping dataColumnMapping = new DataColumnMapping();
		dataColumnMapping._sourceColumnName = _sourceColumnName;
		dataColumnMapping._dataSetColumnName = _dataSetColumnName;
		return dataColumnMapping;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public DataColumn? GetDataColumnBySchemaAction(DataTable dataTable, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type? dataType, MissingSchemaAction schemaAction)
	{
		return GetDataColumnBySchemaAction(SourceColumn, DataSetColumn, dataTable, dataType, schemaAction);
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static DataColumn? GetDataColumnBySchemaAction(string? sourceColumn, string? dataSetColumn, DataTable dataTable, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type? dataType, MissingSchemaAction schemaAction)
	{
		if (dataTable == null)
		{
			throw ADP.ArgumentNull("dataTable");
		}
		if (string.IsNullOrEmpty(dataSetColumn))
		{
			return null;
		}
		DataColumnCollection columns = dataTable.Columns;
		int num = columns.IndexOf(dataSetColumn);
		if (0 <= num && num < columns.Count)
		{
			DataColumn dataColumn = columns[num];
			if (!string.IsNullOrEmpty(dataColumn.Expression))
			{
				throw ADP.ColumnSchemaExpression(sourceColumn, dataSetColumn);
			}
			if (null == dataType || dataType.IsArray == dataColumn.DataType.IsArray)
			{
				return dataColumn;
			}
			throw ADP.ColumnSchemaMismatch(sourceColumn, dataType, dataColumn);
		}
		return CreateDataColumnBySchemaAction(sourceColumn, dataSetColumn, dataTable, dataType, schemaAction);
	}

	internal static DataColumn CreateDataColumnBySchemaAction(string sourceColumn, string dataSetColumn, DataTable dataTable, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type dataType, MissingSchemaAction schemaAction)
	{
		if (string.IsNullOrEmpty(dataSetColumn))
		{
			return null;
		}
		switch (schemaAction)
		{
		case MissingSchemaAction.Add:
		case MissingSchemaAction.AddWithKey:
			return new DataColumn(dataSetColumn, dataType);
		case MissingSchemaAction.Ignore:
			return null;
		case MissingSchemaAction.Error:
			throw ADP.ColumnSchemaMissing(dataSetColumn, dataTable.TableName, sourceColumn);
		default:
			throw ADP.InvalidMissingSchemaAction(schemaAction);
		}
	}

	public override string ToString()
	{
		return SourceColumn;
	}
}
