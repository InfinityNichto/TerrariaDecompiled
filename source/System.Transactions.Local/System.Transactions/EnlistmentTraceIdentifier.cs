using System.Diagnostics.CodeAnalysis;

namespace System.Transactions;

internal readonly struct EnlistmentTraceIdentifier : IEquatable<EnlistmentTraceIdentifier>
{
	private readonly Guid _resourceManagerIdentifier;

	private readonly TransactionTraceIdentifier _transactionTraceIdentifier;

	private readonly int _enlistmentIdentifier;

	public static EnlistmentTraceIdentifier Empty => default(EnlistmentTraceIdentifier);

	public int EnlistmentIdentifier => _enlistmentIdentifier;

	public EnlistmentTraceIdentifier(Guid resourceManagerIdentifier, TransactionTraceIdentifier transactionTraceId, int enlistmentIdentifier)
	{
		_resourceManagerIdentifier = resourceManagerIdentifier;
		_transactionTraceIdentifier = transactionTraceId;
		_enlistmentIdentifier = enlistmentIdentifier;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		if (obj is EnlistmentTraceIdentifier other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(EnlistmentTraceIdentifier other)
	{
		if (_enlistmentIdentifier == other._enlistmentIdentifier && _resourceManagerIdentifier == other._resourceManagerIdentifier)
		{
			return _transactionTraceIdentifier == other._transactionTraceIdentifier;
		}
		return false;
	}

	public static bool operator ==(EnlistmentTraceIdentifier left, EnlistmentTraceIdentifier right)
	{
		return left.Equals(right);
	}
}
