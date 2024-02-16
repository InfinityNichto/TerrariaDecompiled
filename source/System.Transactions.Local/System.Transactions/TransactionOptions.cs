using System.Diagnostics.CodeAnalysis;

namespace System.Transactions;

public struct TransactionOptions
{
	private TimeSpan _timeout;

	private IsolationLevel _isolationLevel;

	public TimeSpan Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			_timeout = value;
		}
	}

	public IsolationLevel IsolationLevel
	{
		get
		{
			return _isolationLevel;
		}
		set
		{
			_isolationLevel = value;
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is TransactionOptions other)
		{
			return Equals(other);
		}
		return false;
	}

	private bool Equals(TransactionOptions other)
	{
		if (_timeout == other._timeout)
		{
			return _isolationLevel == other._isolationLevel;
		}
		return false;
	}

	public static bool operator ==(TransactionOptions x, TransactionOptions y)
	{
		return x.Equals(y);
	}

	public static bool operator !=(TransactionOptions x, TransactionOptions y)
	{
		return !x.Equals(y);
	}
}
