using System.Collections;

namespace System.Transactions;

internal sealed class FinalizedObject : IDisposable
{
	private readonly Guid _identifier;

	private readonly InternalTransaction _internalTransaction;

	internal FinalizedObject(InternalTransaction internalTransaction, Guid identifier)
	{
		_internalTransaction = internalTransaction;
		_identifier = identifier;
	}

	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			GC.SuppressFinalize(this);
		}
		Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
		lock (promotedTransactionTable)
		{
			WeakReference weakReference = (WeakReference)promotedTransactionTable[_identifier];
			if (weakReference != null && weakReference.Target != null)
			{
				weakReference.Target = null;
			}
			promotedTransactionTable.Remove(_identifier);
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	~FinalizedObject()
	{
		Dispose(disposing: false);
	}
}
