namespace System.Transactions;

internal enum TransactionScopeResult
{
	CreatedTransaction,
	UsingExistingCurrent,
	TransactionPassed,
	DependentTransactionPassed,
	NoTransaction
}
