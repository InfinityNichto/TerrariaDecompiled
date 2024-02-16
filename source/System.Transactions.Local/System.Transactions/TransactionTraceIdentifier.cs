using System.Diagnostics.CodeAnalysis;

namespace System.Transactions;

internal readonly struct TransactionTraceIdentifier : IEquatable<TransactionTraceIdentifier>
{
	private readonly string _transactionIdentifier;

	private readonly int _cloneIdentifier;

	public static TransactionTraceIdentifier Empty => default(TransactionTraceIdentifier);

	public string TransactionIdentifier => _transactionIdentifier;

	public TransactionTraceIdentifier(string transactionIdentifier, int cloneIdentifier)
	{
		_transactionIdentifier = transactionIdentifier;
		_cloneIdentifier = cloneIdentifier;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is TransactionTraceIdentifier other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(TransactionTraceIdentifier other)
	{
		if (_cloneIdentifier == other._cloneIdentifier)
		{
			return _transactionIdentifier == other._transactionIdentifier;
		}
		return false;
	}

	public static bool operator ==(TransactionTraceIdentifier left, TransactionTraceIdentifier right)
	{
		return left.Equals(right);
	}
}
