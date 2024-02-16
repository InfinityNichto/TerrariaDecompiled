namespace System.Transactions;

public class TransactionInformation
{
	private readonly InternalTransaction _internalTransaction;

	public string LocalIdentifier
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "LocalIdentifier");
			}
			try
			{
				return _internalTransaction.TransactionTraceId.TransactionIdentifier;
			}
			finally
			{
				if (log.IsEnabled())
				{
					log.MethodExit(TraceSourceType.TraceSourceLtm, this, "LocalIdentifier");
				}
			}
		}
	}

	public Guid DistributedIdentifier
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "DistributedIdentifier");
			}
			try
			{
				lock (_internalTransaction)
				{
					return _internalTransaction.State.get_Identifier(_internalTransaction);
				}
			}
			finally
			{
				if (log.IsEnabled())
				{
					log.MethodExit(TraceSourceType.TraceSourceLtm, this, "DistributedIdentifier");
				}
			}
		}
	}

	public DateTime CreationTime => new DateTime(_internalTransaction.CreationTime);

	public TransactionStatus Status
	{
		get
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Status");
			}
			try
			{
				return _internalTransaction.State.get_Status(_internalTransaction);
			}
			finally
			{
				if (log.IsEnabled())
				{
					log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Status");
				}
			}
		}
	}

	internal TransactionInformation(InternalTransaction internalTransaction)
	{
		_internalTransaction = internalTransaction;
	}
}
