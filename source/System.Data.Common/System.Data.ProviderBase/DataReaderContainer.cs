using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.ProviderBase;

internal abstract class DataReaderContainer
{
	private sealed class ProviderSpecificDataReader : DataReaderContainer
	{
		private readonly DbDataReader _providerSpecificDataReader;

		internal override bool ReturnProviderSpecificTypes => true;

		protected override int VisibleFieldCount
		{
			get
			{
				int visibleFieldCount = _providerSpecificDataReader.VisibleFieldCount;
				if (0 > visibleFieldCount)
				{
					return 0;
				}
				return visibleFieldCount;
			}
		}

		internal ProviderSpecificDataReader(IDataReader dataReader, DbDataReader dbDataReader)
			: base(dataReader)
		{
			_providerSpecificDataReader = dbDataReader;
			_fieldCount = VisibleFieldCount;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		internal override Type GetFieldType(int ordinal)
		{
			return _providerSpecificDataReader.GetProviderSpecificFieldType(ordinal);
		}

		internal override int GetValues(object[] values)
		{
			return _providerSpecificDataReader.GetProviderSpecificValues(values);
		}
	}

	private sealed class CommonLanguageSubsetDataReader : DataReaderContainer
	{
		internal override bool ReturnProviderSpecificTypes => false;

		protected override int VisibleFieldCount
		{
			get
			{
				int fieldCount = _dataReader.FieldCount;
				if (0 > fieldCount)
				{
					return 0;
				}
				return fieldCount;
			}
		}

		internal CommonLanguageSubsetDataReader(IDataReader dataReader)
			: base(dataReader)
		{
			_fieldCount = VisibleFieldCount;
		}

		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		internal override Type GetFieldType(int ordinal)
		{
			return _dataReader.GetFieldType(ordinal);
		}

		internal override int GetValues(object[] values)
		{
			return _dataReader.GetValues(values);
		}
	}

	protected readonly IDataReader _dataReader;

	protected int _fieldCount;

	internal int FieldCount => _fieldCount;

	internal abstract bool ReturnProviderSpecificTypes { get; }

	protected abstract int VisibleFieldCount { get; }

	internal static DataReaderContainer Create(IDataReader dataReader, bool returnProviderSpecificTypes)
	{
		if (returnProviderSpecificTypes && dataReader is DbDataReader dbDataReader)
		{
			return new ProviderSpecificDataReader(dataReader, dbDataReader);
		}
		return new CommonLanguageSubsetDataReader(dataReader);
	}

	protected DataReaderContainer(IDataReader dataReader)
	{
		_dataReader = dataReader;
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	internal abstract Type GetFieldType(int ordinal);

	internal abstract int GetValues(object[] values);

	internal string GetName(int ordinal)
	{
		string name = _dataReader.GetName(ordinal);
		if (name == null)
		{
			return "";
		}
		return name;
	}

	internal DataTable GetSchemaTable()
	{
		return _dataReader.GetSchemaTable();
	}

	internal bool NextResult()
	{
		_fieldCount = 0;
		if (_dataReader.NextResult())
		{
			_fieldCount = VisibleFieldCount;
			return true;
		}
		return false;
	}

	internal bool Read()
	{
		return _dataReader.Read();
	}
}
