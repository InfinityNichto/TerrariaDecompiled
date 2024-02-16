using System.Diagnostics.CodeAnalysis;

namespace System.Data;

public interface IDbConnection : IDisposable
{
	string ConnectionString
	{
		get; [param: AllowNull]
		set;
	}

	int ConnectionTimeout { get; }

	string Database { get; }

	ConnectionState State { get; }

	IDbTransaction BeginTransaction();

	IDbTransaction BeginTransaction(IsolationLevel il);

	void Close();

	void ChangeDatabase(string databaseName);

	IDbCommand CreateCommand();

	void Open();
}
