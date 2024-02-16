namespace System.Transactions.Distributed;

internal sealed class DistributedTransactionManager
{
	internal object NodeName { get; }

	internal IPromotedEnlistment ReenlistTransaction(Guid resourceManagerIdentifier, byte[] resourceManagerRecoveryInformation, RecoveringInternalEnlistment internalEnlistment)
	{
		throw DistributedTransaction.NotSupported();
	}

	internal DistributedCommittableTransaction CreateTransaction(TransactionOptions options)
	{
		throw DistributedTransaction.NotSupported();
	}

	internal void ResourceManagerRecoveryComplete(Guid resourceManagerIdentifier)
	{
		throw DistributedTransaction.NotSupported();
	}

	internal byte[] GetWhereabouts()
	{
		throw DistributedTransaction.NotSupported();
	}

	internal static Transaction GetTransactionFromDtcTransaction(IDtcTransaction transactionNative)
	{
		throw DistributedTransaction.NotSupported();
	}

	internal static DistributedTransaction GetTransactionFromExportCookie(byte[] cookie, Guid txId)
	{
		throw DistributedTransaction.NotSupported();
	}

	internal static DistributedTransaction GetDistributedTransactionFromTransmitterPropagationToken(byte[] propagationToken)
	{
		throw DistributedTransaction.NotSupported();
	}
}
