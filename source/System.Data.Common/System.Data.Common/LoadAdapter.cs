namespace System.Data.Common;

internal sealed class LoadAdapter : DataAdapter
{
	internal LoadAdapter()
	{
	}

	internal int FillFromReader(DataTable[] dataTables, IDataReader dataReader, int startRecord, int maxRecords)
	{
		return Fill(dataTables, dataReader, startRecord, maxRecords);
	}
}
