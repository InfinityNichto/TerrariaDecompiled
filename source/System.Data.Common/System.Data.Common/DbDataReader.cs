using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common;

public abstract class DbDataReader : MarshalByRefObject, IDataReader, IDisposable, IDataRecord, IEnumerable, IAsyncDisposable
{
	public abstract int Depth { get; }

	public abstract int FieldCount { get; }

	public abstract bool HasRows { get; }

	public abstract bool IsClosed { get; }

	public abstract int RecordsAffected { get; }

	public virtual int VisibleFieldCount => FieldCount;

	public abstract object this[int ordinal] { get; }

	public abstract object this[string name] { get; }

	public virtual void Close()
	{
	}

	public virtual Task CloseAsync()
	{
		try
		{
			Close();
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			Close();
		}
	}

	public virtual ValueTask DisposeAsync()
	{
		Dispose();
		return default(ValueTask);
	}

	public abstract string GetDataTypeName(int ordinal);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract IEnumerator GetEnumerator();

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public abstract Type GetFieldType(int ordinal);

	public abstract string GetName(int ordinal);

	public abstract int GetOrdinal(string name);

	public virtual DataTable? GetSchemaTable()
	{
		throw new NotSupportedException();
	}

	public virtual Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<DataTable>(cancellationToken);
		}
		try
		{
			return Task.FromResult(GetSchemaTable());
		}
		catch (Exception exception)
		{
			return Task.FromException<DataTable>(exception);
		}
	}

	public virtual Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<ReadOnlyCollection<DbColumn>>(cancellationToken);
		}
		try
		{
			return Task.FromResult(this.GetColumnSchema());
		}
		catch (Exception exception)
		{
			return Task.FromException<ReadOnlyCollection<DbColumn>>(exception);
		}
	}

	public abstract bool GetBoolean(int ordinal);

	public abstract byte GetByte(int ordinal);

	public abstract long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length);

	public abstract char GetChar(int ordinal);

	public abstract long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public DbDataReader GetData(int ordinal)
	{
		return GetDbDataReader(ordinal);
	}

	IDataReader IDataRecord.GetData(int ordinal)
	{
		return GetDbDataReader(ordinal);
	}

	protected virtual DbDataReader GetDbDataReader(int ordinal)
	{
		throw ADP.NotSupported();
	}

	public abstract DateTime GetDateTime(int ordinal);

	public abstract decimal GetDecimal(int ordinal);

	public abstract double GetDouble(int ordinal);

	public abstract float GetFloat(int ordinal);

	public abstract Guid GetGuid(int ordinal);

	public abstract short GetInt16(int ordinal);

	public abstract int GetInt32(int ordinal);

	public abstract long GetInt64(int ordinal);

	[EditorBrowsable(EditorBrowsableState.Never)]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public virtual Type GetProviderSpecificFieldType(int ordinal)
	{
		return GetFieldType(ordinal);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual object GetProviderSpecificValue(int ordinal)
	{
		return GetValue(ordinal);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual int GetProviderSpecificValues(object[] values)
	{
		return GetValues(values);
	}

	public abstract string GetString(int ordinal);

	public virtual Stream GetStream(int ordinal)
	{
		using MemoryStream memoryStream = new MemoryStream();
		long num = 0L;
		long num2 = 0L;
		byte[] array = new byte[4096];
		do
		{
			num = GetBytes(ordinal, num2, array, 0, array.Length);
			memoryStream.Write(array, 0, (int)num);
			num2 += num;
		}
		while (num > 0);
		return new MemoryStream(memoryStream.ToArray(), writable: false);
	}

	public virtual TextReader GetTextReader(int ordinal)
	{
		if (IsDBNull(ordinal))
		{
			return new StringReader(string.Empty);
		}
		return new StringReader(GetString(ordinal));
	}

	public abstract object GetValue(int ordinal);

	public virtual T GetFieldValue<T>(int ordinal)
	{
		return (T)GetValue(ordinal);
	}

	public Task<T> GetFieldValueAsync<T>(int ordinal)
	{
		return GetFieldValueAsync<T>(ordinal, CancellationToken.None);
	}

	public virtual Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<T>();
		}
		try
		{
			return Task.FromResult(GetFieldValue<T>(ordinal));
		}
		catch (Exception exception)
		{
			return Task.FromException<T>(exception);
		}
	}

	public abstract int GetValues(object[] values);

	public abstract bool IsDBNull(int ordinal);

	public Task<bool> IsDBNullAsync(int ordinal)
	{
		return IsDBNullAsync(ordinal, CancellationToken.None);
	}

	public virtual Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<bool>();
		}
		try
		{
			return IsDBNull(ordinal) ? ADP.TrueTask : ADP.FalseTask;
		}
		catch (Exception exception)
		{
			return Task.FromException<bool>(exception);
		}
	}

	public abstract bool NextResult();

	public abstract bool Read();

	public Task<bool> ReadAsync()
	{
		return ReadAsync(CancellationToken.None);
	}

	public virtual Task<bool> ReadAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<bool>();
		}
		try
		{
			return Read() ? ADP.TrueTask : ADP.FalseTask;
		}
		catch (Exception exception)
		{
			return Task.FromException<bool>(exception);
		}
	}

	public Task<bool> NextResultAsync()
	{
		return NextResultAsync(CancellationToken.None);
	}

	public virtual Task<bool> NextResultAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ADP.CreatedTaskWithCancellation<bool>();
		}
		try
		{
			return NextResult() ? ADP.TrueTask : ADP.FalseTask;
		}
		catch (Exception exception)
		{
			return Task.FromException<bool>(exception);
		}
	}
}
