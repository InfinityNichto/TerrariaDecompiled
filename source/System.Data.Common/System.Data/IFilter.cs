namespace System.Data;

internal interface IFilter
{
	bool Invoke(DataRow row, DataRowVersion version);
}
