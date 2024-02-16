using System.Diagnostics.CodeAnalysis;

namespace System.Data.Common;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public abstract class DbProviderFactory
{
	private bool? _canCreateDataAdapter;

	private bool? _canCreateCommandBuilder;

	public virtual bool CanCreateBatch => false;

	public virtual bool CanCreateDataSourceEnumerator => false;

	public virtual bool CanCreateDataAdapter
	{
		get
		{
			if (!_canCreateDataAdapter.HasValue)
			{
				using DbDataAdapter dbDataAdapter = CreateDataAdapter();
				_canCreateDataAdapter = dbDataAdapter != null;
			}
			return _canCreateDataAdapter.Value;
		}
	}

	public virtual bool CanCreateCommandBuilder
	{
		get
		{
			if (!_canCreateCommandBuilder.HasValue)
			{
				using DbCommandBuilder dbCommandBuilder = CreateCommandBuilder();
				_canCreateCommandBuilder = dbCommandBuilder != null;
			}
			return _canCreateCommandBuilder.Value;
		}
	}

	public virtual DbBatch CreateBatch()
	{
		throw new NotSupportedException();
	}

	public virtual DbBatchCommand CreateBatchCommand()
	{
		throw new NotSupportedException();
	}

	public virtual DbCommand? CreateCommand()
	{
		return null;
	}

	public virtual DbCommandBuilder? CreateCommandBuilder()
	{
		return null;
	}

	public virtual DbConnection? CreateConnection()
	{
		return null;
	}

	public virtual DbConnectionStringBuilder? CreateConnectionStringBuilder()
	{
		return null;
	}

	public virtual DbDataAdapter? CreateDataAdapter()
	{
		return null;
	}

	public virtual DbParameter? CreateParameter()
	{
		return null;
	}

	public virtual DbDataSourceEnumerator? CreateDataSourceEnumerator()
	{
		return null;
	}
}
