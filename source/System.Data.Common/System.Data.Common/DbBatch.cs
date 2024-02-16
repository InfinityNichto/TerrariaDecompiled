using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common;

public abstract class DbBatch : IDisposable, IAsyncDisposable
{
	public DbBatchCommandCollection BatchCommands => DbBatchCommands;

	protected abstract DbBatchCommandCollection DbBatchCommands { get; }

	public abstract int Timeout { get; set; }

	public DbConnection? Connection
	{
		get
		{
			return DbConnection;
		}
		set
		{
			DbConnection = value;
		}
	}

	protected abstract DbConnection? DbConnection { get; set; }

	public DbTransaction? Transaction
	{
		get
		{
			return DbTransaction;
		}
		set
		{
			DbTransaction = value;
		}
	}

	protected abstract DbTransaction? DbTransaction { get; set; }

	public DbDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
	{
		return ExecuteDbDataReader(behavior);
	}

	protected abstract DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

	public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken);
	}

	public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default(CancellationToken))
	{
		return ExecuteDbDataReaderAsync(behavior, cancellationToken);
	}

	protected abstract Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken);

	public abstract int ExecuteNonQuery();

	public abstract Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default(CancellationToken));

	public abstract object? ExecuteScalar();

	public abstract Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default(CancellationToken));

	public abstract void Prepare();

	public abstract Task PrepareAsync(CancellationToken cancellationToken = default(CancellationToken));

	public abstract void Cancel();

	public DbBatchCommand CreateBatchCommand()
	{
		return CreateDbBatchCommand();
	}

	protected abstract DbBatchCommand CreateDbBatchCommand();

	public virtual void Dispose()
	{
	}

	public virtual ValueTask DisposeAsync()
	{
		Dispose();
		return default(ValueTask);
	}
}
