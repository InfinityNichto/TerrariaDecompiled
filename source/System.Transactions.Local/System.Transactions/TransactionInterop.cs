using System.Transactions.Distributed;

namespace System.Transactions;

public static class TransactionInterop
{
	public static readonly Guid PromoterTypeDtc = new Guid("14229753-FFE1-428D-82B7-DF73045CB8DA");

	internal static DistributedTransaction ConvertToDistributedTransaction(Transaction transaction)
	{
		if (null == transaction)
		{
			throw new ArgumentNullException("transaction");
		}
		if (transaction.Disposed)
		{
			throw new ObjectDisposedException("Transaction");
		}
		if (transaction._complete)
		{
			throw TransactionException.CreateTransactionCompletedException(transaction.DistributedTxId);
		}
		DistributedTransaction distributedTransaction = transaction.Promote();
		if (distributedTransaction == null)
		{
			throw DistributedTransaction.NotSupported();
		}
		return distributedTransaction;
	}

	public static byte[] GetExportCookie(Transaction transaction, byte[] whereabouts)
	{
		if (null == transaction)
		{
			throw new ArgumentNullException("transaction");
		}
		if (whereabouts == null)
		{
			throw new ArgumentNullException("whereabouts");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetExportCookie");
		}
		byte[] array = new byte[whereabouts.Length];
		Buffer.BlockCopy(whereabouts, 0, array, 0, whereabouts.Length);
		DistributedTransaction distributedTransaction = ConvertToDistributedTransaction(transaction);
		byte[] exportCookie = distributedTransaction.GetExportCookie(array);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetExportCookie");
		}
		return exportCookie;
	}

	public static Transaction GetTransactionFromExportCookie(byte[] cookie)
	{
		if (cookie == null)
		{
			throw new ArgumentNullException("cookie");
		}
		if (cookie.Length < 32)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "cookie");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromExportCookie");
		}
		byte[] array = new byte[cookie.Length];
		Buffer.BlockCopy(cookie, 0, array, 0, cookie.Length);
		cookie = array;
		Guid guid = new Guid(cookie.AsSpan(16, 16));
		Transaction transaction = TransactionManager.FindPromotedTransaction(guid);
		if (transaction != null)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromExportCookie");
			}
			return transaction;
		}
		DistributedTransaction transactionFromExportCookie = DistributedTransactionManager.GetTransactionFromExportCookie(array, guid);
		transaction = TransactionManager.FindOrCreatePromotedTransaction(guid, transactionFromExportCookie);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromExportCookie");
		}
		return transaction;
	}

	public static byte[] GetTransmitterPropagationToken(Transaction transaction)
	{
		if (null == transaction)
		{
			throw new ArgumentNullException("transaction");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransmitterPropagationToken");
		}
		DistributedTransaction distributedTransaction = ConvertToDistributedTransaction(transaction);
		byte[] transmitterPropagationToken = distributedTransaction.GetTransmitterPropagationToken();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransmitterPropagationToken");
		}
		return transmitterPropagationToken;
	}

	public static Transaction GetTransactionFromTransmitterPropagationToken(byte[] propagationToken)
	{
		if (propagationToken == null)
		{
			throw new ArgumentNullException("propagationToken");
		}
		if (propagationToken.Length < 24)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "propagationToken");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
		}
		Guid transactionIdentifier = new Guid(propagationToken.AsSpan(8, 16));
		Transaction transaction = TransactionManager.FindPromotedTransaction(transactionIdentifier);
		if (null != transaction)
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
			}
			return transaction;
		}
		DistributedTransaction distributedTransactionFromTransmitterPropagationToken = GetDistributedTransactionFromTransmitterPropagationToken(propagationToken);
		Transaction result = TransactionManager.FindOrCreatePromotedTransaction(transactionIdentifier, distributedTransactionFromTransmitterPropagationToken);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromTransmitterPropagationToken");
		}
		return result;
	}

	public static IDtcTransaction GetDtcTransaction(Transaction transaction)
	{
		if (null == transaction)
		{
			throw new ArgumentNullException("transaction");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetDtcTransaction");
		}
		DistributedTransaction distributedTransaction = ConvertToDistributedTransaction(transaction);
		IDtcTransaction dtcTransaction = distributedTransaction.GetDtcTransaction();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetDtcTransaction");
		}
		return dtcTransaction;
	}

	public static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
	{
		if (transactionNative == null)
		{
			throw new ArgumentNullException("transactionNative");
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromDtcTransaction");
		}
		Transaction transactionFromDtcTransaction = DistributedTransactionManager.GetTransactionFromDtcTransaction(transactionNative);
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetTransactionFromDtcTransaction");
		}
		return transactionFromDtcTransaction;
	}

	public static byte[] GetWhereabouts()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetWhereabouts");
		}
		DistributedTransactionManager distributedTransactionManager = TransactionManager.DistributedTransactionManager;
		byte[] whereabouts = distributedTransactionManager.GetWhereabouts();
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceDistributed, "TransactionInterop.GetWhereabouts");
		}
		return whereabouts;
	}

	internal static DistributedTransaction GetDistributedTransactionFromTransmitterPropagationToken(byte[] propagationToken)
	{
		if (propagationToken == null)
		{
			throw new ArgumentNullException("propagationToken");
		}
		if (propagationToken.Length < 24)
		{
			throw new ArgumentException(System.SR.InvalidArgument, "propagationToken");
		}
		byte[] array = new byte[propagationToken.Length];
		Array.Copy(propagationToken, array, propagationToken.Length);
		return DistributedTransactionManager.GetDistributedTransactionFromTransmitterPropagationToken(array);
	}
}
