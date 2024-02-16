using System.Collections;
using System.ComponentModel;
using System.Data.ProviderBase;

namespace System.Data.Common;

public class DbEnumerator : IEnumerator
{
	private sealed class DbColumnDescriptor : PropertyDescriptor
	{
		private readonly int _ordinal;

		private readonly Type _type;

		public override Type ComponentType => typeof(IDataRecord);

		public override bool IsReadOnly => true;

		public override Type PropertyType => _type;

		internal DbColumnDescriptor(int ordinal, string name, Type type)
			: base(name, null)
		{
			_ordinal = ordinal;
			_type = type;
		}

		public override bool CanResetValue(object component)
		{
			return false;
		}

		public override object GetValue(object component)
		{
			return ((IDataRecord)component)[_ordinal];
		}

		public override void ResetValue(object component)
		{
			throw ADP.NotSupported();
		}

		public override void SetValue(object component, object value)
		{
			throw ADP.NotSupported();
		}

		public override bool ShouldSerializeValue(object component)
		{
			return false;
		}
	}

	internal IDataReader _reader;

	internal DbDataRecord _current;

	internal SchemaInfo[] _schemaInfo;

	internal PropertyDescriptorCollection _descriptors;

	private FieldNameLookup _fieldNameLookup;

	private readonly bool _closeReader;

	public object Current => _current;

	public DbEnumerator(IDataReader reader)
	{
		if (reader == null)
		{
			throw ADP.ArgumentNull("reader");
		}
		_reader = reader;
	}

	public DbEnumerator(IDataReader reader, bool closeReader)
	{
		if (reader == null)
		{
			throw ADP.ArgumentNull("reader");
		}
		_reader = reader;
		_closeReader = closeReader;
	}

	public DbEnumerator(DbDataReader reader)
		: this((IDataReader)reader)
	{
	}

	public DbEnumerator(DbDataReader reader, bool closeReader)
		: this((IDataReader)reader, closeReader)
	{
	}

	public bool MoveNext()
	{
		if (_schemaInfo == null)
		{
			BuildSchemaInfo();
		}
		_current = null;
		if (_reader.Read())
		{
			object[] values = new object[_schemaInfo.Length];
			_reader.GetValues(values);
			_current = new DataRecordInternal(_schemaInfo, values, _descriptors, _fieldNameLookup);
			return true;
		}
		if (_closeReader)
		{
			_reader.Close();
		}
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Reset()
	{
		throw ADP.NotSupported();
	}

	private void BuildSchemaInfo()
	{
		int fieldCount = _reader.FieldCount;
		string[] array = new string[fieldCount];
		for (int i = 0; i < fieldCount; i++)
		{
			array[i] = _reader.GetName(i);
		}
		ADP.BuildSchemaTableInfoTableNames(array);
		SchemaInfo[] array2 = new SchemaInfo[fieldCount];
		PropertyDescriptor[] array3 = new PropertyDescriptor[_reader.FieldCount];
		for (int j = 0; j < array2.Length; j++)
		{
			SchemaInfo schemaInfo = default(SchemaInfo);
			schemaInfo.name = _reader.GetName(j);
			schemaInfo.type = _reader.GetFieldType(j);
			schemaInfo.typeName = _reader.GetDataTypeName(j);
			array3[j] = new DbColumnDescriptor(j, array[j], schemaInfo.type);
			array2[j] = schemaInfo;
		}
		_schemaInfo = array2;
		_fieldNameLookup = new FieldNameLookup(_reader, -1);
		_descriptors = new PropertyDescriptorCollection(array3);
	}
}
