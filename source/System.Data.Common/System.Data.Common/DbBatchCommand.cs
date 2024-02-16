namespace System.Data.Common;

public abstract class DbBatchCommand
{
	public abstract string CommandText { get; set; }

	public abstract CommandType CommandType { get; set; }

	public abstract int RecordsAffected { get; }

	public DbParameterCollection Parameters => DbParameterCollection;

	protected abstract DbParameterCollection DbParameterCollection { get; }
}
