using System.Threading;

namespace System.Transactions;

internal abstract class VolatileEnlistmentState : EnlistmentState
{
	private static VolatileEnlistmentActive s_volatileEnlistmentActive;

	private static VolatileEnlistmentPreparing s_volatileEnlistmentPreparing;

	private static VolatileEnlistmentPrepared s_volatileEnlistmentPrepared;

	private static VolatileEnlistmentSPC s_volatileEnlistmentSPC;

	private static VolatileEnlistmentPreparingAborting s_volatileEnlistmentPreparingAborting;

	private static VolatileEnlistmentAborting s_volatileEnlistmentAborting;

	private static VolatileEnlistmentCommitting s_volatileEnlistmentCommitting;

	private static VolatileEnlistmentInDoubt s_volatileEnlistmentInDoubt;

	private static VolatileEnlistmentEnded s_volatileEnlistmentEnded;

	private static VolatileEnlistmentDone s_volatileEnlistmentDone;

	private static object s_classSyncObject;

	internal static VolatileEnlistmentActive VolatileEnlistmentActive => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentActive, ref s_classSyncObject, () => new VolatileEnlistmentActive());

	protected static VolatileEnlistmentPreparing VolatileEnlistmentPreparing => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPreparing, ref s_classSyncObject, () => new VolatileEnlistmentPreparing());

	protected static VolatileEnlistmentPrepared VolatileEnlistmentPrepared => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPrepared, ref s_classSyncObject, () => new VolatileEnlistmentPrepared());

	protected static VolatileEnlistmentSPC VolatileEnlistmentSPC => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentSPC, ref s_classSyncObject, () => new VolatileEnlistmentSPC());

	protected static VolatileEnlistmentPreparingAborting VolatileEnlistmentPreparingAborting => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentPreparingAborting, ref s_classSyncObject, () => new VolatileEnlistmentPreparingAborting());

	protected static VolatileEnlistmentAborting VolatileEnlistmentAborting => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentAborting, ref s_classSyncObject, () => new VolatileEnlistmentAborting());

	protected static VolatileEnlistmentCommitting VolatileEnlistmentCommitting => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentCommitting, ref s_classSyncObject, () => new VolatileEnlistmentCommitting());

	protected static VolatileEnlistmentInDoubt VolatileEnlistmentInDoubt => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentInDoubt, ref s_classSyncObject, () => new VolatileEnlistmentInDoubt());

	protected static VolatileEnlistmentEnded VolatileEnlistmentEnded => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentEnded, ref s_classSyncObject, () => new VolatileEnlistmentEnded());

	protected static VolatileEnlistmentDone VolatileEnlistmentDone => LazyInitializer.EnsureInitialized(ref s_volatileEnlistmentDone, ref s_classSyncObject, () => new VolatileEnlistmentDone());

	internal override byte[] RecoveryInformation(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.VolEnlistNoRecoveryInfo, null, enlistment?.DistributedTxId ?? Guid.Empty);
	}
}
