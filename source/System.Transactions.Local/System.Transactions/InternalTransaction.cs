using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Transactions.Distributed;

namespace System.Transactions;

internal sealed class InternalTransaction : IDisposable
{
	private TransactionState _transactionState;

	internal TransactionState _promoteState;

	internal Guid _promoterType = Guid.Empty;

	internal byte[] promotedToken;

	internal Guid _distributedTransactionIdentifierNonMSDTC = Guid.Empty;

	internal FinalizedObject _finalizedObject;

	internal readonly int _transactionHash;

	internal static int _nextHash;

	private readonly long _absoluteTimeout;

	private long _creationTime;

	internal InternalEnlistment _durableEnlistment;

	internal VolatileEnlistmentSet _phase0Volatiles;

	internal VolatileEnlistmentSet _phase1Volatiles;

	internal int _phase0VolatileWaveCount;

	internal DistributedDependentTransaction _phase0WaveDependentClone;

	internal int _phase0WaveDependentCloneCount;

	internal DistributedDependentTransaction _abortingDependentClone;

	internal int _abortingDependentCloneCount;

	internal Bucket _tableBucket;

	internal int _bucketIndex;

	internal TransactionCompletedEventHandler _transactionCompletedDelegate;

	private DistributedTransaction _promotedTransaction;

	internal Exception _innerException;

	internal int _cloneCount;

	internal int _enlistmentCount;

	internal volatile ManualResetEvent _asyncResultEvent;

	internal bool _asyncCommit;

	internal AsyncCallback _asyncCallback;

	internal object _asyncState;

	internal bool _needPulse;

	internal TransactionInformation _transactionInformation;

	internal readonly CommittableTransaction _committableTransaction;

	internal readonly Transaction _outcomeSource;

	private static object s_classSyncObject;

	private static string s_instanceIdentifier;

	private volatile bool _traceIdentifierInited;

	private TransactionTraceIdentifier _traceIdentifier;

	internal ITransactionPromoter _promoter;

	internal bool _attemptingPSPEPromote;

	internal TransactionState State
	{
		get
		{
			return _transactionState;
		}
		set
		{
			_transactionState = value;
		}
	}

	internal int TransactionHash => _transactionHash;

	internal long AbsoluteTimeout => _absoluteTimeout;

	internal long CreationTime
	{
		get
		{
			return _creationTime;
		}
		set
		{
			_creationTime = value;
		}
	}

	internal DistributedTransaction PromotedTransaction
	{
		get
		{
			return _promotedTransaction;
		}
		set
		{
			_promotedTransaction = value;
		}
	}

	internal Guid DistributedTxId => State.get_Identifier(this);

	internal static string InstanceIdentifier => LazyInitializer.EnsureInitialized(ref s_instanceIdentifier, ref s_classSyncObject, () => $"{Guid.NewGuid()}:");

	internal TransactionTraceIdentifier TransactionTraceId
	{
		get
		{
			if (!_traceIdentifierInited)
			{
				lock (this)
				{
					if (!_traceIdentifierInited)
					{
						IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
						DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(0, 2, invariantCulture);
						handler.AppendFormatted(InstanceIdentifier);
						handler.AppendFormatted(_transactionHash);
						TransactionTraceIdentifier traceIdentifier = new TransactionTraceIdentifier(string.Create(invariantCulture, ref handler), 0);
						_traceIdentifier = traceIdentifier;
						_traceIdentifierInited = true;
					}
				}
			}
			return _traceIdentifier;
		}
	}

	internal void SetPromoterTypeToMSDTC()
	{
		if (_promoterType != Guid.Empty && _promoterType != TransactionInterop.PromoterTypeDtc)
		{
			throw new InvalidOperationException(System.SR.PromoterTypeInvalid);
		}
		_promoterType = TransactionInterop.PromoterTypeDtc;
	}

	internal void ThrowIfPromoterTypeIsNotMSDTC()
	{
		if (_promoterType != Guid.Empty && _promoterType != TransactionInterop.PromoterTypeDtc)
		{
			throw new TransactionPromotionException(System.SR.Format(System.SR.PromoterTypeUnrecognized, _promoterType.ToString()), _innerException);
		}
	}

	internal InternalTransaction(TimeSpan timeout, CommittableTransaction committableTransaction)
	{
		_absoluteTimeout = TransactionManager.TransactionTable.TimeoutTicks(timeout);
		TransactionState.TransactionStateActive.EnterState(this);
		_promoteState = TransactionState.TransactionStatePromoted;
		_committableTransaction = committableTransaction;
		_outcomeSource = committableTransaction;
		_transactionHash = TransactionManager.TransactionTable.Add(this);
	}

	internal InternalTransaction(Transaction outcomeSource, DistributedTransaction distributedTx)
	{
		_promotedTransaction = distributedTx;
		_absoluteTimeout = long.MaxValue;
		_outcomeSource = outcomeSource;
		_transactionHash = TransactionManager.TransactionTable.Add(this);
		TransactionState.TransactionStateNonCommittablePromoted.EnterState(this);
		_promoteState = TransactionState.TransactionStateNonCommittablePromoted;
	}

	internal InternalTransaction(Transaction outcomeSource, ITransactionPromoter promoter)
	{
		_absoluteTimeout = long.MaxValue;
		_outcomeSource = outcomeSource;
		_transactionHash = TransactionManager.TransactionTable.Add(this);
		_promoter = promoter;
		TransactionState.TransactionStateSubordinateActive.EnterState(this);
		_promoteState = TransactionState.TransactionStateDelegatedSubordinate;
	}

	internal void SignalAsyncCompletion()
	{
		if (_asyncResultEvent != null)
		{
			_asyncResultEvent.Set();
		}
		if (_asyncCallback != null)
		{
			Monitor.Exit(this);
			try
			{
				_asyncCallback(_committableTransaction);
			}
			finally
			{
				Monitor.Enter(this);
			}
		}
	}

	internal void FireCompletion()
	{
		TransactionCompletedEventHandler transactionCompletedDelegate = _transactionCompletedDelegate;
		if (transactionCompletedDelegate != null)
		{
			TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
			transactionEventArgs._transaction = _outcomeSource.InternalClone();
			transactionCompletedDelegate(transactionEventArgs._transaction, transactionEventArgs);
		}
	}

	public void Dispose()
	{
		if (_promotedTransaction != null)
		{
			_promotedTransaction.Dispose();
		}
	}
}
