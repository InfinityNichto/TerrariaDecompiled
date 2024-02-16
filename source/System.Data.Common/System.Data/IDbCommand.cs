using System.Diagnostics.CodeAnalysis;

namespace System.Data;

public interface IDbCommand : IDisposable
{
	IDbConnection? Connection { get; set; }

	IDbTransaction? Transaction { get; set; }

	string CommandText
	{
		get; [param: AllowNull]
		set;
	}

	int CommandTimeout { get; set; }

	CommandType CommandType { get; set; }

	IDataParameterCollection Parameters { get; }

	UpdateRowSource UpdatedRowSource { get; set; }

	void Prepare();

	void Cancel();

	IDbDataParameter CreateParameter();

	int ExecuteNonQuery();

	IDataReader ExecuteReader();

	IDataReader ExecuteReader(CommandBehavior behavior);

	object? ExecuteScalar();
}
