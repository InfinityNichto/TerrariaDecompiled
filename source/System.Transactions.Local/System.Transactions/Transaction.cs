using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading;
using System.Transactions.Distributed;

namespace System.Transactions;

public class Transaction : IDisposable, ISerializable
{
	internal IsolationLevel _isoLevel;

	internal bool _complete;

	internal int _cloneId;

	internal int _disposed;

	internal InternalTransaction _internalTransaction;

	internal TransactionTraceIdentifier _traceIdentifier;

	public static Transaction? Current
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "Transaction.get_Current");
			}
			GetCurrentTransactionAndScope(TxLookup.Default, out var current, out var currentScope, out var _);
			if (currentScope != null && currentScope.ScopeComplete)
			{
				throw new InvalidOperationException(System.SR.TransactionScopeComplete);
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "Transaction.get_Current");
			}
			return current;
		}
		set
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceBase, "Transaction.set_Current");
			}
			if (InteropMode(ContextData.TLSCurrentData.CurrentScope) != 0)
			{
				if (log.IsEnabled())
				{
					log.InvalidOperation("Transaction", "Transaction.set_Current");
				}
				throw new InvalidOperationException(System.SR.CannotSetCurrent);
			}
			ContextData.TLSCurrentData.CurrentTransaction = value;
			CallContextCurrentData.ClearCurrentData(null, removeContextData: false);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceBase, "Transaction.set_Current");
			}
		}
	}

	internal bool Disposed => _disposed == 1;

	internal Guid DistributedTxId
	{
		get
		{
			Guid result = Guid.Empty;
			if (_internalTransaction != null)
			{
				result = _internalTransaction.DistributedTxId;
			}
			return result;
		}
	}

	public TransactionInformation TransactionInformation
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "TransactionInformation");
			}
			if (Disposed)
			{
				throw new ObjectDisposedException("Transaction");
			}
			TransactionInformation transactionInformation = _internalTransaction._transactionInformation;
			if (transactionInformation == null)
			{
				transactionInformation = new TransactionInformation(_internalTransaction);
				_internalTransaction._transactionInformation = transactionInformation;
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "TransactionInformation");
			}
			return transactionInformation;
		}
	}

	public IsolationLevel IsolationLevel
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "IsolationLevel");
			}
			if (Disposed)
			{
				throw new ObjectDisposedException("Transaction");
			}
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "IsolationLevel");
			}
			return _isoLevel;
		}
	}

	public Guid PromoterType
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "PromoterType");
			}
			if (Disposed)
			{
				throw new ObjectDisposedException("Transaction");
			}
			lock (_internalTransaction)
			{
				return _internalTransaction._promoterType;
			}
		}
	}

	internal TransactionTraceIdentifier TransactionTraceId
	{
		get
		{
			if (_traceIdentifier == TransactionTraceIdentifier.Empty)
			{
				lock (_internalTransaction)
				{
					if (_traceIdentifier == TransactionTraceIdentifier.Empty)
					{
						TransactionTraceIdentifier traceIdentifier = new TransactionTraceIdentifier(_internalTransaction.TransactionTraceId.TransactionIdentifier, _cloneId);
						Interlocked.MemoryBarrier();
						_traceIdentifier = traceIdentifier;
					}
				}
			}
			return _traceIdentifier;
		}
	}

	public event TransactionCompletedEventHandler? TransactionCompleted
	{
		add
		{
			if (Disposed)
			{
				throw new ObjectDisposedException("Transaction");
			}
			lock (_internalTransaction)
			{
				_internalTransaction.State.AddOutcomeRegistrant(_internalTransaction, value);
			}
		}
		remove
		{
			lock (_internalTransaction)
			{
				_internalTransaction._transactionCompletedDelegate = (TransactionCompletedEventHandler)Delegate.Remove(_internalTransaction._transactionCompletedDelegate, value);
			}
		}
	}

	internal static EnterpriseServicesInteropOption InteropMode(TransactionScope currentScope)
	{
		return currentScope?.InteropMode ?? EnterpriseServicesInteropOption.None;
	}

	internal static Transaction FastGetTransaction(TransactionScope currentScope, ContextData contextData, out Transaction contextTransaction)
	{
		Transaction transaction = null;
		contextTransaction = null;
		contextTransaction = contextData.CurrentTransaction;
		switch (InteropMode(currentScope))
		{
		case EnterpriseServicesInteropOption.None:
			transaction = contextTransaction;
			if (transaction == null && currentScope == null)
			{
				transaction = ((!TransactionManager.s_currentDelegateSet) ? EnterpriseServices.GetContextTransaction(contextData) : TransactionManager.s_currentDelegate());
			}
			break;
		case EnterpriseServicesInteropOption.Full:
			transaction = EnterpriseServices.GetContextTransaction(contextData);
			break;
		case EnterpriseServicesInteropOption.Automatic:
			if (EnterpriseServices.UseServiceDomainForCurrent())
			{
			}
			transaction = contextData.CurrentTransaction;
			break;
		}
		return transaction;
	}

	internal static void GetCurrentTransactionAndScope(TxLookup defaultLookup, out Transaction current, out TransactionScope currentScope, out Transaction contextTransaction)
	{
		current = null;
		currentScope = null;
		contextTransaction = null;
		ContextData contextData = ContextData.LookupContextData(defaultLookup);
		if (contextData != null)
		{
			currentScope = contextData.CurrentScope;
			current = FastGetTransaction(currentScope, contextData, out contextTransaction);
		}
	}

	internal Transaction(IsolationLevel isoLevel, InternalTransaction internalTransaction)
	{
		TransactionManager.ValidateIsolationLevel(isoLevel);
		_isoLevel = isoLevel;
		if (IsolationLevel.Unspecified == _isoLevel)
		{
			_isoLevel = TransactionManager.DefaultIsolationLevel;
		}
		if (internalTransaction != null)
		{
			_internalTransaction = internalTransaction;
			_cloneId = Interlocked.Increment(ref _internalTransaction._cloneCount);
		}
	}

	internal Transaction(DistributedTransaction distributedTransaction)
	{
		_isoLevel = distributedTransaction.IsolationLevel;
		_internalTransaction = new InternalTransaction(this, distributedTransaction);
		_cloneId = Interlocked.Increment(ref _internalTransaction._cloneCount);
	}

	internal Transaction(IsolationLevel isoLevel, ISimpleTransactionSuperior superior)
	{
		TransactionManager.ValidateIsolationLevel(isoLevel);
		if (superior == null)
		{
			throw new ArgumentNullException("superior");
		}
		_isoLevel = isoLevel;
		if (IsolationLevel.Unspecified == _isoLevel)
		{
			_isoLevel = TransactionManager.DefaultIsolationLevel;
		}
		_internalTransaction = new InternalTransaction(this, superior);
		_internalTransaction.SetPromoterTypeToMSDTC();
		_cloneId = 1;
	}

	public override int GetHashCode()
	{
		return _internalTransaction.TransactionHash;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Transaction transaction)
		{
			return _internalTransaction.TransactionHash == transaction._internalTransaction.TransactionHash;
		}
		return false;
	}

	public static bool operator ==(Transaction? x, Transaction? y)
	{
		return x?.Equals(y) ?? ((object)y == null);
	}

	public static bool operator !=(Transaction? x, Transaction? y)
	{
		if ((object)x != null)
		{
			return !x.Equals(y);
		}
		return (object)y != null;
	}

	public byte[] GetPromotedToken()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "GetPromotedToken");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		byte[] array;
		lock (_internalTransaction)
		{
			array = _internalTransaction.State.PromotedToken(_internalTransaction);
		}
		byte[] array2 = new byte[array.Length];
		Array.Copy(array, array2, array2.Length);
		return array2;
	}

	public Enlistment EnlistDurable(Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EnlistDurable");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		if (enlistmentNotification == null)
		{
			throw new ArgumentNullException("enlistmentNotification");
		}
		if (enlistmentOptions != 0 && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
		{
			throw new ArgumentOutOfRangeException("enlistmentOptions");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			Enlistment result = _internalTransaction.State.EnlistDurable(_internalTransaction, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, this);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EnlistDurable");
			}
			return result;
		}
	}

	public Enlistment EnlistDurable(Guid resourceManagerIdentifier, ISinglePhaseNotification singlePhaseNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EnlistDurable");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		if (singlePhaseNotification == null)
		{
			throw new ArgumentNullException("singlePhaseNotification");
		}
		if (enlistmentOptions != 0 && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
		{
			throw new ArgumentOutOfRangeException("enlistmentOptions");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			Enlistment result = _internalTransaction.State.EnlistDurable(_internalTransaction, resourceManagerIdentifier, singlePhaseNotification, enlistmentOptions, this);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EnlistDurable");
			}
			return result;
		}
	}

	public void Rollback()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Rollback");
			log.TransactionRollback(this, "Transaction");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		lock (_internalTransaction)
		{
			_internalTransaction.State.Rollback(_internalTransaction, null);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Rollback");
		}
	}

	public void Rollback(Exception? e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Rollback");
			log.TransactionRollback(this, "Transaction");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		lock (_internalTransaction)
		{
			_internalTransaction.State.Rollback(_internalTransaction, e);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Rollback");
		}
	}

	public Enlistment EnlistVolatile(IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EnlistVolatile");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (enlistmentNotification == null)
		{
			throw new ArgumentNullException("enlistmentNotification");
		}
		if (enlistmentOptions != 0 && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
		{
			throw new ArgumentOutOfRangeException("enlistmentOptions");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			Enlistment result = _internalTransaction.State.EnlistVolatile(_internalTransaction, enlistmentNotification, enlistmentOptions, this);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EnlistVolatile");
			}
			return result;
		}
	}

	public Enlistment EnlistVolatile(ISinglePhaseNotification singlePhaseNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EnlistVolatile");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (singlePhaseNotification == null)
		{
			throw new ArgumentNullException("singlePhaseNotification");
		}
		if (enlistmentOptions != 0 && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
		{
			throw new ArgumentOutOfRangeException("enlistmentOptions");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			Enlistment result = _internalTransaction.State.EnlistVolatile(_internalTransaction, singlePhaseNotification, enlistmentOptions, this);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EnlistVolatile");
			}
			return result;
		}
	}

	public Transaction Clone()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Clone");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		Transaction result = InternalClone();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Clone");
		}
		return result;
	}

	internal Transaction InternalClone()
	{
		Transaction transaction = new Transaction(_isoLevel, _internalTransaction);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionCloneCreate(transaction, "Transaction");
		}
		return transaction;
	}

	public DependentTransaction DependentClone(DependentCloneOption cloneOption)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "DependentClone");
		}
		if (cloneOption != 0 && cloneOption != DependentCloneOption.RollbackIfNotComplete)
		{
			throw new ArgumentOutOfRangeException("cloneOption");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		DependentTransaction dependentTransaction = new DependentTransaction(_isoLevel, _internalTransaction, cloneOption == DependentCloneOption.BlockCommitUntilComplete);
		if (log.IsEnabled())
		{
			log.TransactionCloneCreate(dependentTransaction, "DependentTransaction");
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "DependentClone");
		}
		return dependentTransaction;
	}

	public void Dispose()
	{
		InternalDispose();
	}

	internal virtual void InternalDispose()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "InternalDispose");
		}
		if (Interlocked.Exchange(ref _disposed, 1) != 1)
		{
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
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public bool EnlistPromotableSinglePhase(IPromotableSinglePhaseNotification promotableSinglePhaseNotification)
	{
		return EnlistPromotableSinglePhase(promotableSinglePhaseNotification, TransactionInterop.PromoterTypeDtc);
	}

	public bool EnlistPromotableSinglePhase(IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Guid promoterType)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "EnlistPromotableSinglePhase");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (promotableSinglePhaseNotification == null)
		{
			throw new ArgumentNullException("promotableSinglePhaseNotification");
		}
		if (promoterType == Guid.Empty)
		{
			throw new ArgumentException(System.SR.PromoterTypeInvalid, "promoterType");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		bool result = false;
		lock (_internalTransaction)
		{
			result = _internalTransaction.State.EnlistPromotableSinglePhase(_internalTransaction, promotableSinglePhaseNotification, this, promoterType);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "EnlistPromotableSinglePhase");
		}
		return result;
	}

	public Enlistment PromoteAndEnlistDurable(Guid resourceManagerIdentifier, IPromotableSinglePhaseNotification promotableNotification, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, this, "PromoteAndEnlistDurable");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (resourceManagerIdentifier == Guid.Empty)
		{
			throw new ArgumentException(System.SR.BadResourceManagerId, "resourceManagerIdentifier");
		}
		if (promotableNotification == null)
		{
			throw new ArgumentNullException("promotableNotification");
		}
		if (enlistmentNotification == null)
		{
			throw new ArgumentNullException("enlistmentNotification");
		}
		if (enlistmentOptions != 0 && enlistmentOptions != EnlistmentOptions.EnlistDuringPrepareRequired)
		{
			throw new ArgumentOutOfRangeException("enlistmentOptions");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			Enlistment result = _internalTransaction.State.PromoteAndEnlistDurable(_internalTransaction, resourceManagerIdentifier, promotableNotification, enlistmentNotification, enlistmentOptions, this);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceDistributed, this, "PromoteAndEnlistDurable");
			}
			return result;
		}
	}

	public void SetDistributedTransactionIdentifier(IPromotableSinglePhaseNotification promotableNotification, Guid distributedTransactionIdentifier)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "SetDistributedTransactionIdentifier");
		}
		if (Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (promotableNotification == null)
		{
			throw new ArgumentNullException("promotableNotification");
		}
		if (distributedTransactionIdentifier == Guid.Empty)
		{
			throw new ArgumentException(null, "distributedTransactionIdentifier");
		}
		if (_complete)
		{
			throw TransactionException.CreateTransactionCompletedException(DistributedTxId);
		}
		lock (_internalTransaction)
		{
			_internalTransaction.State.SetDistributedTransactionId(_internalTransaction, promotableNotification, distributedTransactionIdentifier);
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "SetDistributedTransactionIdentifier");
			}
		}
	}

	internal DistributedTransaction Promote()
	{
		lock (_internalTransaction)
		{
			_internalTransaction.ThrowIfPromoterTypeIsNotMSDTC();
			_internalTransaction.State.Promote(_internalTransaction);
			return _internalTransaction.PromotedTransaction;
		}
	}
}
