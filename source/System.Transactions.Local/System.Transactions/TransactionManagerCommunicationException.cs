using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Transactions;

[Serializable]
[TypeForwardedFrom("System.Transactions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class TransactionManagerCommunicationException : TransactionException
{
	public TransactionManagerCommunicationException()
		: base(System.SR.TransactionManagerCommunicationException)
	{
	}

	public TransactionManagerCommunicationException(string? message)
		: base(message)
	{
	}

	public TransactionManagerCommunicationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	protected TransactionManagerCommunicationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
