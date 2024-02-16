using System.Runtime.Versioning;
using System.Threading;

namespace System.Transactions;

[UnsupportedOSPlatform("browser")]
public sealed class TransactionScope : IDisposable
{
	private bool _complete;

	private Transaction _savedCurrent;

	private Transaction _contextTransaction;

	private TransactionScope _savedCurrentScope;

	private ContextData _threadContextData;

	private ContextData _savedTLSContextData;

	private Transaction _expectedCurrent;

	private CommittableTransaction _committableTransaction;

	private DependentTransaction _dependentTransaction;

	private bool _disposed;

	private Timer _scopeTimer;

	private Thread _scopeThread;

	private bool _interopModeSpecified;

	private EnterpriseServicesInteropOption _interopOption;

	internal bool ScopeComplete => _complete;

	internal EnterpriseServicesInteropOption InteropMode => _interopOption;

	internal ContextKey? ContextKey { get; private set; }

	internal bool AsyncFlowEnabled { get; private set; }

	public TransactionScope()
		: this(TransactionScopeOption.Required)
	{
	}

	public TransactionScope(TransactionScopeOption scopeOption)
		: this(scopeOption, TransactionScopeAsyncFlowOption.Suppress)
	{
	}

	public TransactionScope(TransactionScopeAsyncFlowOption asyncFlowOption)
		: this(TransactionScopeOption.Required, asyncFlowOption)
	{
	}

	public TransactionScope(TransactionScopeOption scopeOption, TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateAndSetAsyncFlowOption(asyncFlowOption);
		if (NeedToCreateTransaction(scopeOption))
		{
			_committableTransaction = new CommittableTransaction();
			_expectedCurrent = _committableTransaction.Clone();
		}
		if (null == _expectedCurrent)
		{
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
			}
		}
		else
		{
			TransactionScopeResult transactionScopeResult = ((null == _committableTransaction) ? TransactionScopeResult.UsingExistingCurrent : TransactionScopeResult.CreatedTransaction);
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, transactionScopeResult);
			}
		}
		PushScope();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(TransactionScopeOption scopeOption, TimeSpan scopeTimeout)
		: this(scopeOption, scopeTimeout, TransactionScopeAsyncFlowOption.Suppress)
	{
	}

	public TransactionScope(TransactionScopeOption scopeOption, TimeSpan scopeTimeout, TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateScopeTimeout("scopeTimeout", scopeTimeout);
		TimeSpan timeout = TransactionManager.ValidateTimeout(scopeTimeout);
		ValidateAndSetAsyncFlowOption(asyncFlowOption);
		if (NeedToCreateTransaction(scopeOption))
		{
			_committableTransaction = new CommittableTransaction(timeout);
			_expectedCurrent = _committableTransaction.Clone();
		}
		if (null != _expectedCurrent && null == _committableTransaction && TimeSpan.Zero != scopeTimeout)
		{
			_scopeTimer = new Timer(TimerCallback, this, scopeTimeout, TimeSpan.Zero);
		}
		if (null == _expectedCurrent)
		{
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
			}
		}
		else
		{
			TransactionScopeResult transactionScopeResult = ((null == _committableTransaction) ? TransactionScopeResult.UsingExistingCurrent : TransactionScopeResult.CreatedTransaction);
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, transactionScopeResult);
			}
		}
		PushScope();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(TransactionScopeOption scopeOption, TransactionOptions transactionOptions)
		: this(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Suppress)
	{
	}

	public TransactionScope(TransactionScopeOption scopeOption, TransactionOptions transactionOptions, TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateScopeTimeout("transactionOptions.Timeout", transactionOptions.Timeout);
		TimeSpan timeout = transactionOptions.Timeout;
		transactionOptions.Timeout = TransactionManager.ValidateTimeout(transactionOptions.Timeout);
		TransactionManager.ValidateIsolationLevel(transactionOptions.IsolationLevel);
		ValidateAndSetAsyncFlowOption(asyncFlowOption);
		if (NeedToCreateTransaction(scopeOption))
		{
			_committableTransaction = new CommittableTransaction(transactionOptions);
			_expectedCurrent = _committableTransaction.Clone();
		}
		else if (null != _expectedCurrent && IsolationLevel.Unspecified != transactionOptions.IsolationLevel && _expectedCurrent.IsolationLevel != transactionOptions.IsolationLevel)
		{
			throw new ArgumentException(System.SR.TransactionScopeIsolationLevelDifferentFromTransaction, "transactionOptions");
		}
		if (null != _expectedCurrent && null == _committableTransaction && TimeSpan.Zero != timeout)
		{
			_scopeTimer = new Timer(TimerCallback, this, timeout, TimeSpan.Zero);
		}
		if (null == _expectedCurrent)
		{
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
			}
		}
		else
		{
			TransactionScopeResult transactionScopeResult = ((null == _committableTransaction) ? TransactionScopeResult.UsingExistingCurrent : TransactionScopeResult.CreatedTransaction);
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, transactionScopeResult);
			}
		}
		PushScope();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(TransactionScopeOption scopeOption, TransactionOptions transactionOptions, EnterpriseServicesInteropOption interopOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateScopeTimeout("transactionOptions.Timeout", transactionOptions.Timeout);
		TimeSpan timeout = transactionOptions.Timeout;
		transactionOptions.Timeout = TransactionManager.ValidateTimeout(transactionOptions.Timeout);
		TransactionManager.ValidateIsolationLevel(transactionOptions.IsolationLevel);
		ValidateInteropOption(interopOption);
		_interopModeSpecified = true;
		_interopOption = interopOption;
		if (NeedToCreateTransaction(scopeOption))
		{
			_committableTransaction = new CommittableTransaction(transactionOptions);
			_expectedCurrent = _committableTransaction.Clone();
		}
		else if (null != _expectedCurrent && IsolationLevel.Unspecified != transactionOptions.IsolationLevel && _expectedCurrent.IsolationLevel != transactionOptions.IsolationLevel)
		{
			throw new ArgumentException(System.SR.TransactionScopeIsolationLevelDifferentFromTransaction, "transactionOptions");
		}
		if (null != _expectedCurrent && null == _committableTransaction && TimeSpan.Zero != timeout)
		{
			_scopeTimer = new Timer(TimerCallback, this, timeout, TimeSpan.Zero);
		}
		if (null == _expectedCurrent)
		{
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(TransactionTraceIdentifier.Empty, TransactionScopeResult.NoTransaction);
			}
		}
		else
		{
			TransactionScopeResult transactionScopeResult = ((null == _committableTransaction) ? TransactionScopeResult.UsingExistingCurrent : TransactionScopeResult.CreatedTransaction);
			if (log.IsEnabled())
			{
				log.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, transactionScopeResult);
			}
		}
		PushScope();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(Transaction transactionToUse)
		: this(transactionToUse, TransactionScopeAsyncFlowOption.Suppress)
	{
	}

	public TransactionScope(Transaction transactionToUse, TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateAndSetAsyncFlowOption(asyncFlowOption);
		Initialize(transactionToUse, TimeSpan.Zero, interopModeSpecified: false);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(Transaction transactionToUse, TimeSpan scopeTimeout)
		: this(transactionToUse, scopeTimeout, TransactionScopeAsyncFlowOption.Suppress)
	{
	}

	public TransactionScope(Transaction transactionToUse, TimeSpan scopeTimeout, TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateAndSetAsyncFlowOption(asyncFlowOption);
		Initialize(transactionToUse, scopeTimeout, interopModeSpecified: false);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	public TransactionScope(Transaction transactionToUse, TimeSpan scopeTimeout, EnterpriseServicesInteropOption interopOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
		ValidateInteropOption(interopOption);
		_interopOption = interopOption;
		Initialize(transactionToUse, scopeTimeout, interopModeSpecified: true);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, ".ctor");
		}
	}

	private bool NeedToCreateTransaction(TransactionScopeOption scopeOption)
	{
		bool result = false;
		CommonInitialize();
		switch (scopeOption)
		{
		case TransactionScopeOption.Suppress:
			_expectedCurrent = null;
			result = false;
			break;
		case TransactionScopeOption.Required:
			_expectedCurrent = _savedCurrent;
			if (null == _expectedCurrent)
			{
				result = true;
			}
			break;
		case TransactionScopeOption.RequiresNew:
			result = true;
			break;
		default:
			throw new ArgumentOutOfRangeException("scopeOption");
		}
		return result;
	}

	private void Initialize(Transaction transactionToUse, TimeSpan scopeTimeout, bool interopModeSpecified)
	{
		if (null == transactionToUse)
		{
			throw new ArgumentNullException("transactionToUse");
		}
		ValidateScopeTimeout("scopeTimeout", scopeTimeout);
		CommonInitialize();
		if (TimeSpan.Zero != scopeTimeout)
		{
			_scopeTimer = new Timer(TimerCallback, this, scopeTimeout, TimeSpan.Zero);
		}
		_expectedCurrent = transactionToUse;
		_interopModeSpecified = interopModeSpecified;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionScopeCreated(_expectedCurrent.TransactionTraceId, TransactionScopeResult.TransactionPassed);
		}
		PushScope();
	}

	public void Dispose()
	{
		bool flag = false;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, "Dispose");
		}
		if (_disposed)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, this, "Dispose");
			}
			return;
		}
		if (_scopeThread != Thread.CurrentThread && !AsyncFlowEnabled)
		{
			if (log.IsEnabled())
			{
				log.InvalidOperation("TransactionScope", "InvalidScopeThread");
			}
			throw new InvalidOperationException(System.SR.InvalidScopeThread);
		}
		Exception ex = null;
		try
		{
			_disposed = true;
			TransactionScope currentScope = _threadContextData.CurrentScope;
			Transaction contextTransaction = null;
			Transaction transaction = Transaction.FastGetTransaction(currentScope, _threadContextData, out contextTransaction);
			if (!Equals(currentScope))
			{
				if (currentScope == null)
				{
					Transaction transaction2 = _committableTransaction;
					if (transaction2 == null)
					{
						transaction2 = _dependentTransaction;
					}
					transaction2.Rollback();
					flag = true;
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, System.SR.TransactionScopeInvalidNesting, null, transaction2.DistributedTxId);
				}
				if (currentScope._interopOption == EnterpriseServicesInteropOption.None && ((null != currentScope._expectedCurrent && !currentScope._expectedCurrent.Equals(transaction)) || (null != transaction && null == currentScope._expectedCurrent)))
				{
					TransactionTraceIdentifier currenttransactionID = ((!(null == transaction)) ? transaction.TransactionTraceId : TransactionTraceIdentifier.Empty);
					TransactionTraceIdentifier newtransactionID = ((!(null == _expectedCurrent)) ? _expectedCurrent.TransactionTraceId : TransactionTraceIdentifier.Empty);
					if (log.IsEnabled())
					{
						log.TransactionScopeCurrentChanged(currenttransactionID, newtransactionID);
					}
					ex = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, System.SR.TransactionScopeIncorrectCurrent, null, (transaction == null) ? Guid.Empty : transaction.DistributedTxId);
					if (null != transaction)
					{
						try
						{
							transaction.Rollback();
						}
						catch (TransactionException)
						{
						}
						catch (ObjectDisposedException)
						{
						}
					}
				}
				while (!Equals(currentScope))
				{
					if (ex == null)
					{
						ex = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, System.SR.TransactionScopeInvalidNesting, null, (transaction == null) ? Guid.Empty : transaction.DistributedTxId);
					}
					if (null == currentScope._expectedCurrent)
					{
						if (log.IsEnabled())
						{
							log.TransactionScopeNestedIncorrectly(TransactionTraceIdentifier.Empty);
						}
					}
					else if (log.IsEnabled())
					{
						log.TransactionScopeNestedIncorrectly(currentScope._expectedCurrent.TransactionTraceId);
					}
					currentScope._complete = false;
					try
					{
						currentScope.InternalDispose();
					}
					catch (TransactionException)
					{
					}
					currentScope = _threadContextData.CurrentScope;
					_complete = false;
				}
			}
			else if (_interopOption == EnterpriseServicesInteropOption.None && ((null != _expectedCurrent && !_expectedCurrent.Equals(transaction)) || (null != transaction && null == _expectedCurrent)))
			{
				TransactionTraceIdentifier currenttransactionID2 = ((!(null == transaction)) ? transaction.TransactionTraceId : TransactionTraceIdentifier.Empty);
				TransactionTraceIdentifier newtransactionID2 = ((!(null == _expectedCurrent)) ? _expectedCurrent.TransactionTraceId : TransactionTraceIdentifier.Empty);
				if (log.IsEnabled())
				{
					log.TransactionScopeCurrentChanged(currenttransactionID2, newtransactionID2);
				}
				if (ex == null)
				{
					ex = TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, System.SR.TransactionScopeIncorrectCurrent, null, (transaction == null) ? Guid.Empty : transaction.DistributedTxId);
				}
				if (null != transaction)
				{
					try
					{
						transaction.Rollback();
					}
					catch (TransactionException)
					{
					}
					catch (ObjectDisposedException)
					{
					}
				}
				_complete = false;
			}
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				PopScope();
			}
		}
		InternalDispose();
		if (ex != null)
		{
			throw ex;
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, "Dispose");
		}
	}

	private void InternalDispose()
	{
		_disposed = true;
		try
		{
			PopScope();
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (null == _expectedCurrent)
			{
				if (log.IsEnabled())
				{
					log.TransactionScopeDisposed(TransactionTraceIdentifier.Empty);
				}
			}
			else if (log.IsEnabled())
			{
				log.TransactionScopeDisposed(_expectedCurrent.TransactionTraceId);
			}
			if (!(null != _expectedCurrent))
			{
				return;
			}
			if (!_complete)
			{
				if (log.IsEnabled())
				{
					log.TransactionScopeIncomplete(_expectedCurrent.TransactionTraceId);
				}
				Transaction transaction = _committableTransaction;
				if (transaction == null)
				{
					transaction = _dependentTransaction;
				}
				transaction.Rollback();
			}
			else if (null != _committableTransaction)
			{
				_committableTransaction.Commit();
			}
			else
			{
				_dependentTransaction.Complete();
			}
		}
		finally
		{
			if (_scopeTimer != null)
			{
				_scopeTimer.Dispose();
			}
			if (null != _committableTransaction)
			{
				_committableTransaction.Dispose();
				_expectedCurrent.Dispose();
			}
			if (null != _dependentTransaction)
			{
				_dependentTransaction.Dispose();
			}
		}
	}

	public void Complete()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceBase, this, "Complete");
		}
		if (_disposed)
		{
			throw new ObjectDisposedException("TransactionScope");
		}
		if (_complete)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceBase, System.SR.DisposeScope, null);
		}
		_complete = true;
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceBase, this, "Complete");
		}
	}

	private static void TimerCallback(object state)
	{
		if (!(state is TransactionScope transactionScope))
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.TransactionScopeInternalError("TransactionScopeTimerObjectInvalid");
			}
			throw TransactionException.Create(TraceSourceType.TraceSourceBase, System.SR.InternalError + System.SR.TransactionScopeTimerObjectInvalid, null);
		}
		transactionScope.Timeout();
	}

	private void Timeout()
	{
		if (_complete || !(null != _expectedCurrent))
		{
			return;
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionScopeTimeout(_expectedCurrent.TransactionTraceId);
		}
		try
		{
			_expectedCurrent.Rollback();
		}
		catch (ObjectDisposedException exception)
		{
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceBase, exception);
			}
		}
		catch (TransactionException exception2)
		{
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(TraceSourceType.TraceSourceBase, exception2);
			}
		}
	}

	private void CommonInitialize()
	{
		ContextKey = new ContextKey();
		_complete = false;
		_dependentTransaction = null;
		_disposed = false;
		_committableTransaction = null;
		_expectedCurrent = null;
		_scopeTimer = null;
		_scopeThread = Thread.CurrentThread;
		Transaction.GetCurrentTransactionAndScope(AsyncFlowEnabled ? TxLookup.DefaultCallContext : TxLookup.DefaultTLS, out _savedCurrent, out _savedCurrentScope, out _contextTransaction);
		ValidateAsyncFlowOptionAndESInteropOption();
	}

	private void PushScope()
	{
		if (!_interopModeSpecified)
		{
			_interopOption = Transaction.InteropMode(_savedCurrentScope);
		}
		SaveTLSContextData();
		if (AsyncFlowEnabled)
		{
			_threadContextData = CallContextCurrentData.CreateOrGetCurrentData(ContextKey);
			if (_savedCurrentScope == null && _savedCurrent == null)
			{
				ContextData.TLSCurrentData = null;
			}
		}
		else
		{
			_threadContextData = ContextData.TLSCurrentData;
			CallContextCurrentData.ClearCurrentData(ContextKey, removeContextData: false);
		}
		SetCurrent(_expectedCurrent);
		_threadContextData.CurrentScope = this;
	}

	private void PopScope()
	{
		bool flag = true;
		if (AsyncFlowEnabled)
		{
			CallContextCurrentData.ClearCurrentData(ContextKey, removeContextData: true);
		}
		if (_scopeThread == Thread.CurrentThread)
		{
			RestoreSavedTLSContextData();
		}
		if (_savedCurrentScope != null)
		{
			if (_savedCurrentScope.AsyncFlowEnabled)
			{
				_threadContextData = CallContextCurrentData.CreateOrGetCurrentData(_savedCurrentScope.ContextKey);
			}
			else
			{
				if (_savedCurrentScope._scopeThread != Thread.CurrentThread)
				{
					flag = false;
					ContextData.TLSCurrentData = null;
				}
				else
				{
					_threadContextData = ContextData.TLSCurrentData;
				}
				CallContextCurrentData.ClearCurrentData(_savedCurrentScope.ContextKey, removeContextData: false);
			}
		}
		else
		{
			CallContextCurrentData.ClearCurrentData(null, removeContextData: false);
			if (_scopeThread != Thread.CurrentThread)
			{
				flag = false;
				ContextData.TLSCurrentData = null;
			}
			else
			{
				ContextData.TLSCurrentData = _threadContextData;
			}
		}
		if (flag)
		{
			_threadContextData.CurrentScope = _savedCurrentScope;
			RestoreCurrent();
		}
	}

	private void SetCurrent(Transaction newCurrent)
	{
		if (_dependentTransaction == null && _committableTransaction == null && newCurrent != null)
		{
			_dependentTransaction = newCurrent.DependentClone(DependentCloneOption.RollbackIfNotComplete);
		}
		switch (_interopOption)
		{
		case EnterpriseServicesInteropOption.None:
			_threadContextData.CurrentTransaction = newCurrent;
			break;
		case EnterpriseServicesInteropOption.Automatic:
			EnterpriseServices.VerifyEnterpriseServicesOk();
			if (EnterpriseServices.UseServiceDomainForCurrent())
			{
			}
			_threadContextData.CurrentTransaction = newCurrent;
			break;
		case EnterpriseServicesInteropOption.Full:
			EnterpriseServices.VerifyEnterpriseServicesOk();
			EnterpriseServices.PushServiceDomain(newCurrent);
			break;
		}
	}

	private void SaveTLSContextData()
	{
		if (_savedTLSContextData == null)
		{
			_savedTLSContextData = new ContextData(asyncFlow: false);
		}
		_savedTLSContextData.CurrentScope = ContextData.TLSCurrentData.CurrentScope;
		_savedTLSContextData.CurrentTransaction = ContextData.TLSCurrentData.CurrentTransaction;
		_savedTLSContextData.DefaultComContextState = ContextData.TLSCurrentData.DefaultComContextState;
		_savedTLSContextData.WeakDefaultComContext = ContextData.TLSCurrentData.WeakDefaultComContext;
	}

	private void RestoreSavedTLSContextData()
	{
		if (_savedTLSContextData != null)
		{
			ContextData.TLSCurrentData.CurrentScope = _savedTLSContextData.CurrentScope;
			ContextData.TLSCurrentData.CurrentTransaction = _savedTLSContextData.CurrentTransaction;
			ContextData.TLSCurrentData.DefaultComContextState = _savedTLSContextData.DefaultComContextState;
			ContextData.TLSCurrentData.WeakDefaultComContext = _savedTLSContextData.WeakDefaultComContext;
		}
	}

	private void RestoreCurrent()
	{
		if (EnterpriseServices.CreatedServiceDomain)
		{
			EnterpriseServices.LeaveServiceDomain();
		}
		_threadContextData.CurrentTransaction = _contextTransaction;
	}

	private void ValidateInteropOption(EnterpriseServicesInteropOption interopOption)
	{
		if (interopOption < EnterpriseServicesInteropOption.None || interopOption > EnterpriseServicesInteropOption.Full)
		{
			throw new ArgumentOutOfRangeException("interopOption");
		}
	}

	private void ValidateScopeTimeout(string paramName, TimeSpan scopeTimeout)
	{
		if (scopeTimeout < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(paramName);
		}
	}

	private void ValidateAndSetAsyncFlowOption(TransactionScopeAsyncFlowOption asyncFlowOption)
	{
		switch (asyncFlowOption)
		{
		default:
			throw new ArgumentOutOfRangeException("asyncFlowOption");
		case TransactionScopeAsyncFlowOption.Enabled:
			AsyncFlowEnabled = true;
			break;
		case TransactionScopeAsyncFlowOption.Suppress:
			break;
		}
	}

	private void ValidateAsyncFlowOptionAndESInteropOption()
	{
		if (AsyncFlowEnabled)
		{
			EnterpriseServicesInteropOption enterpriseServicesInteropOption = _interopOption;
			if (!_interopModeSpecified)
			{
				enterpriseServicesInteropOption = Transaction.InteropMode(_savedCurrentScope);
			}
			if (enterpriseServicesInteropOption != 0)
			{
				throw new NotSupportedException(System.SR.AsyncFlowAndESInteropNotSupported);
			}
		}
	}
}
