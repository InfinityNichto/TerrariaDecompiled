using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Transactions.Configuration;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionException : SystemException
{
	internal static bool IncludeDistributedTxId(Guid distributedTxId)
	{
		if (distributedTxId != Guid.Empty)
		{
			return AppSettings.IncludeDistributedTxIdInExceptionMessage;
		}
		return false;
	}

	internal static TransactionException Create(string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.TransactionException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new TransactionException(message, innerException);
	}

	internal static TransactionException Create(TraceSourceType traceSource, string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.TransactionException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new TransactionException(message, innerException);
	}

	internal static Exception CreateEnlistmentStateException(Exception innerException, Guid distributedTxId)
	{
		string text = System.SR.EnlistmentStateException;
		if (IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, text, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new InvalidOperationException(text, innerException);
	}

	internal static Exception CreateInvalidOperationException(TraceSourceType traceSource, string message, Exception innerException)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(traceSource, TransactionExceptionType.InvalidOperationException, message, (innerException == null) ? string.Empty : innerException.ToString());
		}
		return new InvalidOperationException(message, innerException);
	}

	public TransactionException()
	{
	}

	public TransactionException(string? message)
		: base(message)
	{
	}

	public TransactionException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected TransactionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	internal static TransactionException Create(string message, Guid distributedTxId)
	{
		if (IncludeDistributedTxId(distributedTxId))
		{
			return new TransactionException(System.SR.Format(System.SR.DistributedTxIDInTransactionException, message, distributedTxId));
		}
		return new TransactionException(message);
	}

	internal static TransactionException Create(string message, Exception innerException, Guid distributedTxId)
	{
		string text = message;
		if (IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		return Create(text, innerException);
	}

	internal static TransactionException CreateTransactionStateException(Exception innerException, Guid distributedTxId)
	{
		return Create(System.SR.TransactionStateException, innerException, distributedTxId);
	}

	internal static Exception CreateTransactionCompletedException(Guid distributedTxId)
	{
		string text = System.SR.TransactionAlreadyCompleted;
		if (IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, text, string.Empty);
		}
		return new InvalidOperationException(text);
	}

	internal static Exception CreateInvalidOperationException(TraceSourceType traceSource, string message, Exception innerException, Guid distributedTxId)
	{
		string text = message;
		if (IncludeDistributedTxId(distributedTxId))
		{
			text = System.SR.Format(System.SR.DistributedTxIDInTransactionException, text, distributedTxId);
		}
		return CreateInvalidOperationException(traceSource, text, innerException);
	}
}
