using System.Threading;

namespace System.Transactions;

internal abstract class DurableEnlistmentState : EnlistmentState
{
	private static DurableEnlistmentActive s_durableEnlistmentActive;

	private static DurableEnlistmentAborting s_durableEnlistmentAborting;

	private static DurableEnlistmentCommitting s_durableEnlistmentCommitting;

	private static DurableEnlistmentDelegated s_durableEnlistmentDelegated;

	private static DurableEnlistmentEnded s_durableEnlistmentEnded;

	private static object s_classSyncObject;

	internal static DurableEnlistmentActive DurableEnlistmentActive => LazyInitializer.EnsureInitialized(ref s_durableEnlistmentActive, ref s_classSyncObject, () => new DurableEnlistmentActive());

	protected static DurableEnlistmentAborting DurableEnlistmentAborting => LazyInitializer.EnsureInitialized(ref s_durableEnlistmentAborting, ref s_classSyncObject, () => new DurableEnlistmentAborting());

	protected static DurableEnlistmentCommitting DurableEnlistmentCommitting => LazyInitializer.EnsureInitialized(ref s_durableEnlistmentCommitting, ref s_classSyncObject, () => new DurableEnlistmentCommitting());

	protected static DurableEnlistmentDelegated DurableEnlistmentDelegated => LazyInitializer.EnsureInitialized(ref s_durableEnlistmentDelegated, ref s_classSyncObject, () => new DurableEnlistmentDelegated());

	protected static DurableEnlistmentEnded DurableEnlistmentEnded => LazyInitializer.EnsureInitialized(ref s_durableEnlistmentEnded, ref s_classSyncObject, () => new DurableEnlistmentEnded());
}
