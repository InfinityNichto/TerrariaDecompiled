namespace System.Data;

public sealed class DataRowBuilder
{
	internal readonly DataTable _table;

	internal int _record;

	internal DataRowBuilder(DataTable table, int record)
	{
		_table = table;
		_record = record;
	}
}
