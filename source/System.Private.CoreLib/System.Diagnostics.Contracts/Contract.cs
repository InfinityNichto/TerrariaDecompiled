using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Contracts;

public static class Contract
{
	public static event EventHandler<ContractFailedEventArgs>? ContractFailed
	{
		add
		{
			ContractHelper.InternalContractFailed += value;
		}
		remove
		{
			ContractHelper.InternalContractFailed -= value;
		}
	}

	[Conditional("DEBUG")]
	[Conditional("CONTRACTS_FULL")]
	public static void Assume([DoesNotReturnIf(false)] bool condition)
	{
		if (!condition)
		{
			ReportFailure(ContractFailureKind.Assume, null, null, null);
		}
	}

	[Conditional("DEBUG")]
	[Conditional("CONTRACTS_FULL")]
	public static void Assume([DoesNotReturnIf(false)] bool condition, string? userMessage)
	{
		if (!condition)
		{
			ReportFailure(ContractFailureKind.Assume, userMessage, null, null);
		}
	}

	[Conditional("DEBUG")]
	[Conditional("CONTRACTS_FULL")]
	public static void Assert([DoesNotReturnIf(false)] bool condition)
	{
		if (!condition)
		{
			ReportFailure(ContractFailureKind.Assert, null, null, null);
		}
	}

	[Conditional("DEBUG")]
	[Conditional("CONTRACTS_FULL")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string? userMessage)
	{
		if (!condition)
		{
			ReportFailure(ContractFailureKind.Assert, userMessage, null, null);
		}
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Requires(bool condition)
	{
		AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Requires(bool condition, string? userMessage)
	{
		AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires");
	}

	public static void Requires<TException>(bool condition) where TException : Exception
	{
		AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
	}

	public static void Requires<TException>(bool condition, string? userMessage) where TException : Exception
	{
		AssertMustUseRewriter(ContractFailureKind.Precondition, "Requires<TException>");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Ensures(bool condition)
	{
		AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Ensures(bool condition, string? userMessage)
	{
		AssertMustUseRewriter(ContractFailureKind.Postcondition, "Ensures");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void EnsuresOnThrow<TException>(bool condition) where TException : Exception
	{
		AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void EnsuresOnThrow<TException>(bool condition, string? userMessage) where TException : Exception
	{
		AssertMustUseRewriter(ContractFailureKind.PostconditionOnException, "EnsuresOnThrow");
	}

	public static T Result<T>()
	{
		return default(T);
	}

	public static T ValueAtReturn<T>(out T value)
	{
		value = default(T);
		return value;
	}

	public static T OldValue<T>(T value)
	{
		return default(T);
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Invariant(bool condition)
	{
		AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
	}

	[Conditional("CONTRACTS_FULL")]
	public static void Invariant(bool condition, string? userMessage)
	{
		AssertMustUseRewriter(ContractFailureKind.Invariant, "Invariant");
	}

	public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
	{
		if (fromInclusive > toExclusive)
		{
			throw new ArgumentException(SR.Argument_ToExclusiveLessThanFromExclusive);
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			if (!predicate(i))
			{
				return false;
			}
		}
		return true;
	}

	public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		foreach (T item in collection)
		{
			if (!predicate(item))
			{
				return false;
			}
		}
		return true;
	}

	public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
	{
		if (fromInclusive > toExclusive)
		{
			throw new ArgumentException(SR.Argument_ToExclusiveLessThanFromExclusive);
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		for (int i = fromInclusive; i < toExclusive; i++)
		{
			if (predicate(i))
			{
				return true;
			}
		}
		return false;
	}

	public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		foreach (T item in collection)
		{
			if (predicate(item))
			{
				return true;
			}
		}
		return false;
	}

	[Conditional("CONTRACTS_FULL")]
	public static void EndContractBlock()
	{
	}

	private static void AssertMustUseRewriter(ContractFailureKind kind, string contractKind)
	{
		Assembly assembly = typeof(Contract).Assembly;
		StackTrace stackTrace = new StackTrace();
		Assembly assembly2 = null;
		for (int i = 0; i < stackTrace.FrameCount; i++)
		{
			Assembly assembly3 = stackTrace.GetFrame(i).GetMethod()?.DeclaringType?.Assembly;
			if (assembly3 != null && assembly3 != assembly)
			{
				assembly2 = assembly3;
				break;
			}
		}
		if ((object)assembly2 == null)
		{
			assembly2 = assembly;
		}
		string name = assembly2.GetName().Name;
		ContractHelper.TriggerFailure(kind, SR.Format(SR.MustUseCCRewrite, contractKind, name), null, null, null);
	}

	[DebuggerNonUserCode]
	private static void ReportFailure(ContractFailureKind failureKind, string userMessage, string conditionText, Exception innerException)
	{
		if (failureKind < ContractFailureKind.Precondition || failureKind > ContractFailureKind.Assume)
		{
			throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, failureKind), "failureKind");
		}
		string text = ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
		if (text != null)
		{
			ContractHelper.TriggerFailure(failureKind, text, userMessage, conditionText, innerException);
		}
	}
}
