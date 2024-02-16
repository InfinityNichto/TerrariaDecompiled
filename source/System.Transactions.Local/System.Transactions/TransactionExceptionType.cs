namespace System.Transactions;

internal enum TransactionExceptionType
{
	InvalidOperationException,
	TransactionAbortedException,
	TransactionException,
	TransactionInDoubtException,
	TransactionManagerCommunicationException,
	UnrecognizedRecoveryInformation
}
