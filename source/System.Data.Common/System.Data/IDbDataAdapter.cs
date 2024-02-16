namespace System.Data;

public interface IDbDataAdapter : IDataAdapter
{
	IDbCommand? SelectCommand { get; set; }

	IDbCommand? InsertCommand { get; set; }

	IDbCommand? UpdateCommand { get; set; }

	IDbCommand? DeleteCommand { get; set; }
}
