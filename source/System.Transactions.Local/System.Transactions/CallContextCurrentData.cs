using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Transactions;

internal static class CallContextCurrentData
{
	private static readonly AsyncLocal<ContextKey> s_currentTransaction = new AsyncLocal<ContextKey>();

	private static readonly ConditionalWeakTable<ContextKey, ContextData> s_contextDataTable = new ConditionalWeakTable<ContextKey, ContextData>();

	public static ContextData CreateOrGetCurrentData(ContextKey contextKey)
	{
		s_currentTransaction.Value = contextKey;
		return s_contextDataTable.GetValue(contextKey, (ContextKey env) => new ContextData(asyncFlow: true));
	}

	public static void ClearCurrentData(ContextKey contextKey, bool removeContextData)
	{
		ContextKey value = s_currentTransaction.Value;
		if (contextKey != null || value != null)
		{
			if (removeContextData)
			{
				s_contextDataTable.Remove(contextKey ?? value);
			}
			if (value != null)
			{
				s_currentTransaction.Value = null;
			}
		}
	}

	public static bool TryGetCurrentData([NotNullWhen(true)] out ContextData currentData)
	{
		currentData = null;
		ContextKey value = s_currentTransaction.Value;
		if (value == null)
		{
			return false;
		}
		return s_contextDataTable.TryGetValue(value, out currentData);
	}
}
