using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Transactions;

internal class InternalEnlistment : ISinglePhaseNotificationInternal, IEnlistmentNotificationInternal
{
	internal EnlistmentState _twoPhaseState;

	protected IEnlistmentNotification _twoPhaseNotifications;

	protected ISinglePhaseNotification _singlePhaseNotifications;

	protected InternalTransaction _transaction;

	private readonly Transaction _atomicTransaction;

	private EnlistmentTraceIdentifier _traceIdentifier;

	private readonly int _enlistmentId;

	private readonly Enlistment _enlistment;

	private PreparingEnlistment _preparingEnlistment;

	private SinglePhaseEnlistment _singlePhaseEnlistment;

	private IPromotedEnlistment _promotedEnlistment;

	internal Guid DistributedTxId
	{
		get
		{
			Guid result = Guid.Empty;
			if (Transaction != null)
			{
				result = Transaction.DistributedTxId;
			}
			return result;
		}
	}

	internal EnlistmentState State
	{
		get
		{
			return _twoPhaseState;
		}
		set
		{
			_twoPhaseState = value;
		}
	}

	internal Enlistment Enlistment => _enlistment;

	internal PreparingEnlistment PreparingEnlistment
	{
		get
		{
			if (_preparingEnlistment == null)
			{
				_preparingEnlistment = new PreparingEnlistment(this);
			}
			return _preparingEnlistment;
		}
	}

	internal SinglePhaseEnlistment SinglePhaseEnlistment
	{
		get
		{
			if (_singlePhaseEnlistment == null)
			{
				_singlePhaseEnlistment = new SinglePhaseEnlistment(this);
			}
			return _singlePhaseEnlistment;
		}
	}

	internal InternalTransaction Transaction => _transaction;

	internal virtual object SyncRoot => _transaction;

	internal IEnlistmentNotification EnlistmentNotification => _twoPhaseNotifications;

	internal ISinglePhaseNotification SinglePhaseNotification => _singlePhaseNotifications;

	internal virtual IPromotableSinglePhaseNotification PromotableSinglePhaseNotification
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	internal IPromotedEnlistment PromotedEnlistment
	{
		get
		{
			return _promotedEnlistment;
		}
		set
		{
			_promotedEnlistment = value;
		}
	}

	internal EnlistmentTraceIdentifier EnlistmentTraceId
	{
		get
		{
			if (_traceIdentifier == EnlistmentTraceIdentifier.Empty)
			{
				lock (SyncRoot)
				{
					if (_traceIdentifier == EnlistmentTraceIdentifier.Empty)
					{
						EnlistmentTraceIdentifier traceIdentifier;
						if (null != _atomicTransaction)
						{
							traceIdentifier = new EnlistmentTraceIdentifier(Guid.Empty, _atomicTransaction.TransactionTraceId, _enlistmentId);
						}
						else
						{
							Guid empty = Guid.Empty;
							IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
							DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(0, 2, invariantCulture);
							handler.AppendFormatted(InternalTransaction.InstanceIdentifier);
							handler.AppendFormatted(Interlocked.Increment(ref InternalTransaction._nextHash));
							traceIdentifier = new EnlistmentTraceIdentifier(empty, new TransactionTraceIdentifier(string.Create(invariantCulture, ref handler), 0), _enlistmentId);
						}
						Interlocked.MemoryBarrier();
						_traceIdentifier = traceIdentifier;
					}
				}
			}
			return _traceIdentifier;
		}
	}

	internal virtual Guid ResourceManagerIdentifier
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	protected InternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications)
	{
		_enlistment = enlistment;
		_twoPhaseNotifications = twoPhaseNotifications;
		_enlistmentId = 1;
		_traceIdentifier = EnlistmentTraceIdentifier.Empty;
	}

	protected InternalEnlistment(Enlistment enlistment, InternalTransaction transaction, Transaction atomicTransaction)
	{
		_enlistment = enlistment;
		_transaction = transaction;
		_atomicTransaction = atomicTransaction;
		_enlistmentId = transaction._enlistmentCount++;
		_traceIdentifier = EnlistmentTraceIdentifier.Empty;
	}

	internal InternalEnlistment(Enlistment enlistment, InternalTransaction transaction, IEnlistmentNotification twoPhaseNotifications, ISinglePhaseNotification singlePhaseNotifications, Transaction atomicTransaction)
	{
		_enlistment = enlistment;
		_transaction = transaction;
		_twoPhaseNotifications = twoPhaseNotifications;
		_singlePhaseNotifications = singlePhaseNotifications;
		_atomicTransaction = atomicTransaction;
		_enlistmentId = transaction._enlistmentCount++;
		_traceIdentifier = EnlistmentTraceIdentifier.Empty;
	}

	internal InternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications, InternalTransaction transaction, Transaction atomicTransaction)
	{
		_enlistment = enlistment;
		_twoPhaseNotifications = twoPhaseNotifications;
		_transaction = transaction;
		_atomicTransaction = atomicTransaction;
	}

	internal virtual void FinishEnlistment()
	{
		Transaction._phase0Volatiles._preparedVolatileEnlistments++;
		CheckComplete();
	}

	internal virtual void CheckComplete()
	{
		if (Transaction._phase0Volatiles._preparedVolatileEnlistments == Transaction._phase0VolatileWaveCount + Transaction._phase0Volatiles._dependentClones)
		{
			Transaction.State.Phase0VolatilePrepareDone(Transaction);
		}
	}
}
