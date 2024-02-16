using System.Runtime.Versioning;
using System.Threading;

namespace System.Transactions;

[UnsupportedOSPlatform("browser")]
public sealed class CommittableTransaction : Transaction, IAsyncResult
{
	object? IAsyncResult.AsyncState => _internalTransaction._asyncState;

	bool IAsyncResult.CompletedSynchronously => false;

	WaitHandle IAsyncResult.AsyncWaitHandle
	{
		get
		{
			if (_internalTransaction._asyncResultEvent == null)
			{
				lock (_internalTransaction)
				{
					if (_internalTransaction._asyncResultEvent == null)
					{
						ManualResetEvent asyncResultEvent = new ManualResetEvent(_internalTransaction.State.get_Status(_internalTransaction) != TransactionStatus.Active);
						_internalTransaction._asyncResultEvent = asyncResultEvent;
					}
				}
			}
			return _internalTransaction._asyncResultEvent;
		}
	}

	bool IAsyncResult.IsCompleted
	{
		get
		{
			lock (_internalTransaction)
			{
				return _internalTransaction.State.get_Status(_internalTransaction) != TransactionStatus.Active;
			}
		}
	}

	public CommittableTransaction()
		: this(TransactionManager.DefaultIsolationLevel, TransactionManager.DefaultTimeout)
	{
	}

	public CommittableTransaction(TimeSpan timeout)
		: this(TransactionManager.DefaultIsolationLevel, timeout)
	{
	}

	public CommittableTransaction(TransactionOptions options)
		: this(options.IsolationLevel, options.Timeout)
	{
	}

	internal CommittableTransaction(IsolationLevel isoLevel, TimeSpan timeout)
		: base(isoLevel, (InternalTransaction)null)
	{
		_internalTransaction = new InternalTransaction(timeout, this);
		_internalTransaction._cloneCount = 1;
		_cloneId = 1;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionCreated(this, "CommittableTransaction");
		}
	}

	public IAsyncResult BeginCommit(AsyncCallback? asyncCallback, object? asyncState)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "BeginCommit");
			log.TransactionCommit(this, "CommittableTransaction");
		}
		if (base.Disposed)
		{
			throw new ObjectDisposedException("CommittableTransaction");
		}
		lock (_internalTransaction)
		{
			if (_complete)
			{
				throw TransactionException.CreateTransactionCompletedException(base.DistributedTxId);
			}
			_internalTransaction.State.BeginCommit(_internalTransaction, asyncCommit: true, asyncCallback, asyncState);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "BeginCommit");
		}
		return this;
	}

	public void Commit()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Commit");
			log.TransactionCommit(this, "CommittableTransaction");
		}
		if (base.Disposed)
		{
			throw new ObjectDisposedException("CommittableTransaction");
		}
		lock (_internalTransaction)
		{
			if (_complete)
			{
				throw TransactionException.CreateTransactionCompletedException(base.DistributedTxId);
			}
			_internalTransaction.State.BeginCommit(_internalTransaction, asyncCommit: false, null, null);
			while (!_internalTransaction.State.IsCompleted(_internalTransaction) && Monitor.Wait(_internalTransaction))
			{
			}
			_internalTransaction.State.EndCommit(_internalTransaction);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Commit");
		}
	}

	internal override void InternalDispose()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "InternalDispose");
		}
		if (Interlocked.Exchange(ref _disposed, 1) == 1)
		{
			return;
		}
		if (_internalTransaction.State.get_Status(_internalTransaction) == TransactionStatus.Active)
		{
			lock (_internalTransaction)
			{
				_internalTransaction.State.DisposeRoot(_internalTransaction);
			}
		}
		long num = Interlocked.Decrement(ref _internalTransaction._cloneCount);
		if (num == 0L)
		{
			_internalTransaction.Dispose();
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "InternalDispose");
		}
	}

	public void EndCommit(IAsyncResult asyncResult)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EndCommit");
		}
		if (asyncResult != this)
		{
			throw new ArgumentException(System.SR.BadAsyncResult, "asyncResult");
		}
		lock (_internalTransaction)
		{
			while (!_internalTransaction.State.IsCompleted(_internalTransaction) && Monitor.Wait(_internalTransaction))
			{
			}
			_internalTransaction.State.EndCommit(_internalTransaction);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EndCommit");
		}
	}
}
