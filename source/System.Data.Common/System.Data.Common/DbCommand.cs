using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common;

public abstract class DbCommand : Component, IDbCommand, IDisposable, IAsyncDisposable
{
	[DefaultValue("")]
	[RefreshProperties(RefreshProperties.All)]
	public abstract string CommandText
	{
		get; [param: AllowNull]
		set;
	}

	public abstract int CommandTimeout { get; set; }

	[DefaultValue(CommandType.Text)]
	[RefreshProperties(RefreshProperties.All)]
	public abstract CommandType CommandType { get; set; }

	[Browsable(false)]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

	IDbConnection? IDbCommand.Connection
	{
		get
		{
			return DbConnection;
		}
		set
		{
			DbConnection = (DbConnection)value;
		}
	}

	protected abstract DbConnection? DbConnection { get; set; }

	protected abstract DbParameterCollection DbParameterCollection { get; }

	protected abstract DbTransaction? DbTransaction { get; set; }

	[DefaultValue(true)]
	[DesignOnly(true)]
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract bool DesignTimeVisible { get; set; }

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public DbParameterCollection Parameters => DbParameterCollection;

	IDataParameterCollection IDbCommand.Parameters => DbParameterCollection;

	[Browsable(false)]
	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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

	IDbTransaction? IDbCommand.Transaction
	{
		get
		{
			return DbTransaction;
		}
		set
		{
			DbTransaction = (DbTransaction)value;
		}
	}

	[DefaultValue(UpdateRowSource.Both)]
	public abstract UpdateRowSource UpdatedRowSource { get; set; }

	internal void CancelIgnoreFailure()
	{
		try
		{
			Cancel();
		}
		catch (Exception)
		{
		}
	}

	public abstract void Cancel();

	public DbParameter CreateParameter()
	{
		return CreateDbParameter();
	}

	IDbDataParameter IDbCommand.CreateParameter()
	{
		return CreateDbParameter();
	}

	protected abstract DbParameter CreateDbParameter();

	protected abstract DbDataReader ExecuteDbDataReader(CommandBehavior behavior);

	public abstract int ExecuteNonQuery();

	public DbDataReader ExecuteReader()
	{
		return ExecuteDbDataReader(CommandBehavior.Default);
	}

	IDataReader IDbCommand.ExecuteReader()
	{
		return ExecuteDbDataReader(CommandBehavior.Default);
	}

	public DbDataReader ExecuteReader(CommandBehavior behavior)
	{
		return ExecuteDbDataReader(behavior);
	}

	IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior)
	{
		return ExecuteDbDataReader(behavior);
	}

	public Task<int> ExecuteNonQueryAsync()
	{
		return ExecuteNonQueryAsync(CancellationToken.None);
	}

	public virtual Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<int>();
		}
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		if (cancellationToken.CanBeCanceled)
		{
			cancellationTokenRegistration = cancellationToken.Register(delegate(object s)
			{
				((DbCommand)s).CancelIgnoreFailure();
			}, this);
		}
		try
		{
			return Task.FromResult(ExecuteNonQuery());
		}
		catch (Exception exception)
		{
			return Task.FromException<int>(exception);
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public Task<DbDataReader> ExecuteReaderAsync()
	{
		return ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);
	}

	public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
	{
		return ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);
	}

	public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior)
	{
		return ExecuteReaderAsync(behavior, CancellationToken.None);
	}

	public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
	{
		return ExecuteDbDataReaderAsync(behavior, cancellationToken);
	}

	protected virtual Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<DbDataReader>();
		}
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		if (cancellationToken.CanBeCanceled)
		{
			cancellationTokenRegistration = cancellationToken.Register(delegate(object s)
			{
				((DbCommand)s).CancelIgnoreFailure();
			}, this);
		}
		try
		{
			return Task.FromResult(ExecuteReader(behavior));
		}
		catch (Exception exception)
		{
			return Task.FromException<DbDataReader>(exception);
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public Task<object?> ExecuteScalarAsync()
	{
		return ExecuteScalarAsync(CancellationToken.None);
	}

	public virtual Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<object>();
		}
		CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);
		if (cancellationToken.CanBeCanceled)
		{
			cancellationTokenRegistration = cancellationToken.Register(delegate(object s)
			{
				((DbCommand)s).CancelIgnoreFailure();
			}, this);
		}
		try
		{
			return Task.FromResult(ExecuteScalar());
		}
		catch (Exception exception)
		{
			return Task.FromException<object>(exception);
		}
		finally
		{
			cancellationTokenRegistration.Dispose();
		}
	}

	public abstract object? ExecuteScalar();

	public abstract void Prepare();

	public virtual Task PrepareAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		try
		{
			Prepare();
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	public virtual ValueTask DisposeAsync()
	{
		Dispose();
		return default(ValueTask);
	}
}
