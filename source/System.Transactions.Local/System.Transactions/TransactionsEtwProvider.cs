using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Transactions;

[EventSource(Name = "System.Transactions.TransactionsEventSource", Guid = "8ac2d80a-1f1a-431b-ace4-bff8824aef0b", LocalizationResources = "FxResources.System.Transactions.Local.SR")]
internal sealed class TransactionsEtwProvider : EventSource
{
	public static class Opcodes
	{
		public const EventOpcode Aborted = (EventOpcode)100;

		public const EventOpcode Activity = (EventOpcode)101;

		public const EventOpcode Adjusted = (EventOpcode)102;

		public const EventOpcode CloneCreate = (EventOpcode)103;

		public const EventOpcode Commit = (EventOpcode)104;

		public const EventOpcode Committed = (EventOpcode)105;

		public const EventOpcode Create = (EventOpcode)106;

		public const EventOpcode Created = (EventOpcode)107;

		public const EventOpcode CurrentChanged = (EventOpcode)108;

		public const EventOpcode DependentCloneComplete = (EventOpcode)109;

		public const EventOpcode Disposed = (EventOpcode)110;

		public const EventOpcode Done = (EventOpcode)111;

		public const EventOpcode Enlist = (EventOpcode)112;

		public const EventOpcode Enter = (EventOpcode)113;

		public const EventOpcode ExceptionConsumed = (EventOpcode)114;

		public const EventOpcode Exit = (EventOpcode)115;

		public const EventOpcode ForceRollback = (EventOpcode)116;

		public const EventOpcode Incomplete = (EventOpcode)117;

		public const EventOpcode InDoubt = (EventOpcode)118;

		public const EventOpcode InternalError = (EventOpcode)119;

		public const EventOpcode InvalidOperation = (EventOpcode)120;

		public const EventOpcode NestedIncorrectly = (EventOpcode)121;

		public const EventOpcode Prepared = (EventOpcode)122;

		public const EventOpcode Promoted = (EventOpcode)123;

		public const EventOpcode RecoveryComplete = (EventOpcode)124;

		public const EventOpcode Reenlist = (EventOpcode)125;

		public const EventOpcode Rollback = (EventOpcode)126;

		public const EventOpcode Serialized = (EventOpcode)127;

		public const EventOpcode Timeout = (EventOpcode)128;
	}

	public static class Tasks
	{
		public const EventTask ConfiguredDefaultTimeout = (EventTask)1;

		public const EventTask Enlistment = (EventTask)2;

		public const EventTask ResourceManager = (EventTask)3;

		public const EventTask Method = (EventTask)4;

		public const EventTask Transaction = (EventTask)5;

		public const EventTask TransactionException = (EventTask)6;

		public const EventTask TransactionManager = (EventTask)7;

		public const EventTask TransactionScope = (EventTask)8;

		public const EventTask TransactionState = (EventTask)9;
	}

	public static class Keywords
	{
		public const EventKeywords TraceBase = (EventKeywords)1L;

		public const EventKeywords TraceLtm = (EventKeywords)2L;

		public const EventKeywords TraceDistributed = (EventKeywords)4L;
	}

	internal static readonly TransactionsEtwProvider Log = new TransactionsEtwProvider();

	private TransactionsEtwProvider()
	{
	}

	[NonEvent]
	public static string IdOf(object value)
	{
		if (value == null)
		{
			return "(null)";
		}
		return value.GetType().Name + "#" + GetHashCode(value);
	}

	[NonEvent]
	public static int GetHashCode(object value)
	{
		return value?.GetHashCode() ?? 0;
	}

	[NonEvent]
	internal void TransactionCreated(Transaction transaction, string type)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			if (transaction != null && transaction.TransactionTraceId.TransactionIdentifier != null)
			{
				TransactionCreated(transaction.TransactionTraceId.TransactionIdentifier, type);
			}
			else
			{
				TransactionCreated(string.Empty, type);
			}
		}
	}

	[Event(21, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, Task = (EventTask)5, Opcode = (EventOpcode)106, Message = "Transaction Created. ID is {0}, type is {1}")]
	private void TransactionCreated(string transactionIdentifier, string type)
	{
		SetActivityId(transactionIdentifier);
		WriteEvent(21, transactionIdentifier, type);
	}

	[NonEvent]
	internal void TransactionCloneCreate(Transaction transaction, string type)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			if (transaction != null && transaction.TransactionTraceId.TransactionIdentifier != null)
			{
				TransactionCloneCreate(transaction.TransactionTraceId.TransactionIdentifier, type);
			}
			else
			{
				TransactionCloneCreate(string.Empty, type);
			}
		}
	}

	[Event(18, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, Task = (EventTask)5, Opcode = (EventOpcode)103, Message = "Transaction Clone Created. ID is {0}, type is {1}")]
	private void TransactionCloneCreate(string transactionIdentifier, string type)
	{
		SetActivityId(transactionIdentifier);
		WriteEvent(18, transactionIdentifier, type);
	}

	[NonEvent]
	internal void TransactionExceptionTrace(TraceSourceType traceSource, TransactionExceptionType type, string message, string innerExceptionStr)
	{
		if (IsEnabled(EventLevel.Error, EventKeywords.All))
		{
			if (traceSource == TraceSourceType.TraceSourceBase)
			{
				TransactionExceptionBase(type.ToString(), message, innerExceptionStr);
			}
			else
			{
				TransactionExceptionLtm(type.ToString(), message, innerExceptionStr);
			}
		}
	}

	[NonEvent]
	internal void TransactionExceptionTrace(TransactionExceptionType type, string message, string innerExceptionStr)
	{
		if (IsEnabled(EventLevel.Error, EventKeywords.All))
		{
			TransactionExceptionLtm(type.ToString(), message, innerExceptionStr);
		}
	}

	[Event(24, Keywords = (EventKeywords)1L, Level = EventLevel.Error, Task = (EventTask)6, Message = "Transaction Exception. Type is {0}, message is {1}, InnerException is {2}")]
	private void TransactionExceptionBase(string type, string message, string innerExceptionStr)
	{
		SetActivityId(string.Empty);
		WriteEvent(24, type, message, innerExceptionStr);
	}

	[Event(23, Keywords = (EventKeywords)2L, Level = EventLevel.Error, Task = (EventTask)6, Message = "Transaction Exception. Type is {0}, message is {1}, InnerException is {2}")]
	private void TransactionExceptionLtm(string type, string message, string innerExceptionStr)
	{
		SetActivityId(string.Empty);
		WriteEvent(23, type, message, innerExceptionStr);
	}

	[NonEvent]
	internal void InvalidOperation(string type, string operation)
	{
		if (IsEnabled(EventLevel.Error, EventKeywords.All))
		{
			TransactionInvalidOperation(string.Empty, type, operation);
		}
	}

	[Event(26, Keywords = (EventKeywords)1L, Level = EventLevel.Error, Task = (EventTask)5, Opcode = (EventOpcode)120, Message = "Transaction Invalid Operation. ID is {0}, type is {1} and operation is {2}")]
	private void TransactionInvalidOperation(string transactionIdentifier, string type, string operation)
	{
		SetActivityId(string.Empty);
		WriteEvent(26, transactionIdentifier, type, operation);
	}

	[NonEvent]
	internal void TransactionRollback(Transaction transaction, string type)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			if (transaction != null && transaction.TransactionTraceId.TransactionIdentifier != null)
			{
				TransactionRollback(transaction.TransactionTraceId.TransactionIdentifier, type);
			}
			else
			{
				TransactionRollback(string.Empty, type);
			}
		}
	}

	[Event(28, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)5, Opcode = (EventOpcode)126, Message = "Transaction Rollback. ID is {0}, type is {1}")]
	private void TransactionRollback(string transactionIdentifier, string type)
	{
		SetActivityId(transactionIdentifier);
		WriteEvent(28, transactionIdentifier, type);
	}

	[NonEvent]
	internal void TransactionDependentCloneComplete(Transaction transaction, string type)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			if (transaction != null && transaction.TransactionTraceId.TransactionIdentifier != null)
			{
				TransactionDependentCloneComplete(transaction.TransactionTraceId.TransactionIdentifier, type);
			}
			else
			{
				TransactionDependentCloneComplete(string.Empty, type);
			}
		}
	}

	[Event(22, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, Task = (EventTask)5, Opcode = (EventOpcode)109, Message = "Transaction Dependent Clone Completed. ID is {0}, type is {1}")]
	private void TransactionDependentCloneComplete(string transactionIdentifier, string type)
	{
		SetActivityId(transactionIdentifier);
		WriteEvent(22, transactionIdentifier, type);
	}

	[NonEvent]
	internal void TransactionCommit(Transaction transaction, string type)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (transaction != null && transaction.TransactionTraceId.TransactionIdentifier != null)
			{
				TransactionCommit(transaction.TransactionTraceId.TransactionIdentifier, type);
			}
			else
			{
				TransactionCommit(string.Empty, type);
			}
		}
	}

	[Event(19, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)5, Opcode = (EventOpcode)104, Message = "Transaction Commit: ID is {0}, type is {1}")]
	private void TransactionCommit(string transactionIdentifier, string type)
	{
		SetActivityId(transactionIdentifier);
		WriteEvent(19, transactionIdentifier, type);
	}

	[NonEvent]
	internal void EnlistmentStatus(InternalEnlistment enlistment, NotificationCall notificationCall)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentStatus(enlistment.EnlistmentTraceId.EnlistmentIdentifier, notificationCall.ToString());
			}
			else
			{
				EnlistmentStatus(0, notificationCall.ToString());
			}
		}
	}

	[Event(5, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)2, Message = "Enlistment status: ID is {0}, notificationcall is {1}")]
	private void EnlistmentStatus(int enlistmentIdentifier, string notificationCall)
	{
		SetActivityId(string.Empty);
		WriteEvent(5, enlistmentIdentifier, notificationCall);
	}

	[NonEvent]
	internal void EnlistmentDone(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentDone(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentDone(0);
			}
		}
	}

	[Event(4, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)2, Opcode = (EventOpcode)111, Message = "Enlistment.Done: ID is {0}")]
	private void EnlistmentDone(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(4, enlistmentIdentifier);
	}

	[NonEvent]
	internal void EnlistmentPrepared(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentPrepared(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentPrepared(0);
			}
		}
	}

	[Event(8, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)2, Opcode = (EventOpcode)122, Message = "PreparingEnlistment.Prepared: ID is {0}")]
	private void EnlistmentPrepared(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(8, enlistmentIdentifier);
	}

	[NonEvent]
	internal void EnlistmentForceRollback(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentForceRollback(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentForceRollback(0);
			}
		}
	}

	[Event(6, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)2, Opcode = (EventOpcode)116, Message = "Enlistment forceRollback: ID is {0}")]
	private void EnlistmentForceRollback(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(6, enlistmentIdentifier);
	}

	[NonEvent]
	internal void EnlistmentAborted(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentAborted(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentAborted(0);
			}
		}
	}

	[Event(2, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)2, Opcode = (EventOpcode)100, Message = "Enlistment SinglePhase Aborted: ID is {0}")]
	private void EnlistmentAborted(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(2, enlistmentIdentifier);
	}

	[NonEvent]
	internal void EnlistmentCommitted(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentCommitted(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentCommitted(0);
			}
		}
	}

	[Event(3, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)2, Opcode = (EventOpcode)105, Message = "Enlistment Committed: ID is {0}")]
	private void EnlistmentCommitted(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(3, enlistmentIdentifier);
	}

	[NonEvent]
	internal void EnlistmentInDoubt(InternalEnlistment enlistment)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			if (enlistment != null && enlistment.EnlistmentTraceId.EnlistmentIdentifier != 0)
			{
				EnlistmentInDoubt(enlistment.EnlistmentTraceId.EnlistmentIdentifier);
			}
			else
			{
				EnlistmentInDoubt(0);
			}
		}
	}

	[Event(7, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)2, Opcode = (EventOpcode)118, Message = "Enlistment SinglePhase InDoubt: ID is {0}")]
	private void EnlistmentInDoubt(int enlistmentIdentifier)
	{
		SetActivityId(string.Empty);
		WriteEvent(7, enlistmentIdentifier);
	}

	[NonEvent]
	internal void MethodEnter(TraceSourceType traceSource, object thisOrContextObject, [CallerMemberName] string methodname = null)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			switch (traceSource)
			{
			case TraceSourceType.TraceSourceLtm:
				MethodEnterTraceLtm(IdOf(thisOrContextObject), methodname);
				break;
			case TraceSourceType.TraceSourceBase:
				MethodEnterTraceBase(IdOf(thisOrContextObject), methodname);
				break;
			case TraceSourceType.TraceSourceDistributed:
				MethodEnterTraceDistributed(IdOf(thisOrContextObject), methodname);
				break;
			}
		}
	}

	[NonEvent]
	internal void MethodEnter(TraceSourceType traceSource, [CallerMemberName] string methodname = null)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			switch (traceSource)
			{
			case TraceSourceType.TraceSourceLtm:
				MethodEnterTraceLtm(string.Empty, methodname);
				break;
			case TraceSourceType.TraceSourceBase:
				MethodEnterTraceBase(string.Empty, methodname);
				break;
			case TraceSourceType.TraceSourceDistributed:
				MethodEnterTraceDistributed(string.Empty, methodname);
				break;
			}
		}
	}

	[Event(11, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)113, Message = "Enter method : {0}.{1}")]
	private void MethodEnterTraceLtm(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(11, thisOrContextObject, methodname);
	}

	[Event(13, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)113, Message = "Enter method : {0}.{1}")]
	private void MethodEnterTraceBase(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(13, thisOrContextObject, methodname);
	}

	[Event(15, Keywords = (EventKeywords)4L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)113, Message = "Enter method : {0}.{1}")]
	private void MethodEnterTraceDistributed(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(15, thisOrContextObject, methodname);
	}

	[NonEvent]
	internal void MethodExit(TraceSourceType traceSource, object thisOrContextObject, [CallerMemberName] string methodname = null)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			switch (traceSource)
			{
			case TraceSourceType.TraceSourceLtm:
				MethodExitTraceLtm(IdOf(thisOrContextObject), methodname);
				break;
			case TraceSourceType.TraceSourceBase:
				MethodExitTraceBase(IdOf(thisOrContextObject), methodname);
				break;
			case TraceSourceType.TraceSourceDistributed:
				MethodExitTraceDistributed(IdOf(thisOrContextObject), methodname);
				break;
			}
		}
	}

	[NonEvent]
	internal void MethodExit(TraceSourceType traceSource, [CallerMemberName] string methodname = null)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			switch (traceSource)
			{
			case TraceSourceType.TraceSourceLtm:
				MethodExitTraceLtm(string.Empty, methodname);
				break;
			case TraceSourceType.TraceSourceBase:
				MethodExitTraceBase(string.Empty, methodname);
				break;
			case TraceSourceType.TraceSourceDistributed:
				MethodExitTraceDistributed(string.Empty, methodname);
				break;
			}
		}
	}

	[Event(12, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)115, Message = "Exit method: {0}.{1}")]
	private void MethodExitTraceLtm(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(12, thisOrContextObject, methodname);
	}

	[Event(14, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)115, Message = "Exit method: {0}.{1}")]
	private void MethodExitTraceBase(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(14, thisOrContextObject, methodname);
	}

	[Event(16, Keywords = (EventKeywords)4L, Level = EventLevel.Verbose, Task = (EventTask)4, Opcode = (EventOpcode)115, Message = "Exit method: {0}.{1}")]
	private void MethodExitTraceDistributed(string thisOrContextObject, string methodname)
	{
		SetActivityId(string.Empty);
		WriteEvent(16, thisOrContextObject, methodname);
	}

	[NonEvent]
	internal void ExceptionConsumed(TraceSourceType traceSource, Exception exception)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			if (traceSource == TraceSourceType.TraceSourceBase)
			{
				ExceptionConsumedBase(exception.ToString());
			}
			else
			{
				ExceptionConsumedLtm(exception.ToString());
			}
		}
	}

	[NonEvent]
	internal void ExceptionConsumed(Exception exception)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			ExceptionConsumedLtm(exception.ToString());
		}
	}

	[Event(9, Keywords = (EventKeywords)1L, Level = EventLevel.Verbose, Opcode = (EventOpcode)114, Message = "Exception consumed: {0}")]
	private void ExceptionConsumedBase(string exceptionStr)
	{
		SetActivityId(string.Empty);
		WriteEvent(9, exceptionStr);
	}

	[Event(10, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Opcode = (EventOpcode)114, Message = "Exception consumed: {0}")]
	private void ExceptionConsumedLtm(string exceptionStr)
	{
		SetActivityId(string.Empty);
		WriteEvent(10, exceptionStr);
	}

	[NonEvent]
	internal void TransactionManagerReenlist(Guid resourceManagerID)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			TransactionManagerReenlistTrace(resourceManagerID.ToString());
		}
	}

	[Event(32, Keywords = (EventKeywords)1L, Level = EventLevel.Informational, Task = (EventTask)7, Opcode = (EventOpcode)125, Message = "Reenlist in: {0}")]
	private void TransactionManagerReenlistTrace(string rmID)
	{
		SetActivityId(string.Empty);
		WriteEvent(32, rmID);
	}

	[NonEvent]
	internal void TransactionManagerRecoveryComplete(Guid resourceManagerID)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			TransactionManagerRecoveryComplete(resourceManagerID.ToString());
		}
	}

	[Event(31, Keywords = (EventKeywords)1L, Level = EventLevel.Informational, Task = (EventTask)7, Opcode = (EventOpcode)124, Message = "Recovery complete: {0}")]
	private void TransactionManagerRecoveryComplete(string rmID)
	{
		SetActivityId(string.Empty);
		WriteEvent(31, rmID);
	}

	[NonEvent]
	internal void ConfiguredDefaultTimeoutAdjusted()
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			ConfiguredDefaultTimeoutAdjustedTrace();
		}
	}

	[Event(1, Keywords = (EventKeywords)1L, Level = EventLevel.Warning, Task = (EventTask)1, Opcode = (EventOpcode)102, Message = "Configured Default Timeout Adjusted")]
	private void ConfiguredDefaultTimeoutAdjustedTrace()
	{
		SetActivityId(string.Empty);
		WriteEvent(1);
	}

	[NonEvent]
	internal void TransactionScopeCreated(TransactionTraceIdentifier transactionID, TransactionScopeResult transactionScopeResult)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			TransactionScopeCreated(transactionID.TransactionIdentifier ?? string.Empty, transactionScopeResult);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "TransactionScopeResult parameter is an enum and is trimmer safe")]
	[Event(33, Keywords = (EventKeywords)1L, Level = EventLevel.Informational, Task = (EventTask)8, Opcode = (EventOpcode)107, Message = "Transactionscope was created: Transaction ID is {0}, TransactionScope Result is {1}")]
	private void TransactionScopeCreated(string transactionID, TransactionScopeResult transactionScopeResult)
	{
		SetActivityId(transactionID);
		WriteEvent(33, transactionID, transactionScopeResult);
	}

	[NonEvent]
	internal void TransactionScopeCurrentChanged(TransactionTraceIdentifier currenttransactionID, TransactionTraceIdentifier newtransactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			string currenttransactionID2 = string.Empty;
			string newtransactionID2 = string.Empty;
			if (currenttransactionID.TransactionIdentifier != null)
			{
				currenttransactionID2 = currenttransactionID.TransactionIdentifier.ToString();
			}
			if (newtransactionID.TransactionIdentifier != null)
			{
				newtransactionID2 = newtransactionID.TransactionIdentifier.ToString();
			}
			TransactionScopeCurrentChanged(currenttransactionID2, newtransactionID2);
		}
	}

	[Event(34, Keywords = (EventKeywords)1L, Level = EventLevel.Warning, Task = (EventTask)8, Opcode = (EventOpcode)108, Message = "Transactionscope current transaction ID changed from {0} to {1}")]
	private void TransactionScopeCurrentChanged(string currenttransactionID, string newtransactionID)
	{
		SetActivityId(newtransactionID);
		WriteEvent(34, currenttransactionID, newtransactionID);
	}

	[NonEvent]
	internal void TransactionScopeNestedIncorrectly(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionScopeNestedIncorrectly(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(38, Keywords = (EventKeywords)1L, Level = EventLevel.Warning, Task = (EventTask)8, Opcode = (EventOpcode)121, Message = "Transactionscope nested incorrectly: transaction ID is {0}")]
	private void TransactionScopeNestedIncorrectly(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(38, transactionID);
	}

	[NonEvent]
	internal void TransactionScopeDisposed(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			TransactionScopeDisposed(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(35, Keywords = (EventKeywords)1L, Level = EventLevel.Informational, Task = (EventTask)8, Opcode = (EventOpcode)110, Message = "Transactionscope disposed: transaction ID is {0}")]
	private void TransactionScopeDisposed(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(35, transactionID);
	}

	[NonEvent]
	internal void TransactionScopeIncomplete(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionScopeIncomplete(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(36, Keywords = (EventKeywords)1L, Level = EventLevel.Warning, Task = (EventTask)8, Opcode = (EventOpcode)117, Message = "Transactionscope incomplete: transaction ID is {0}")]
	private void TransactionScopeIncomplete(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(36, transactionID);
	}

	[NonEvent]
	internal void TransactionScopeInternalError(string error)
	{
		if (IsEnabled(EventLevel.Critical, EventKeywords.All))
		{
			TransactionScopeInternalErrorTrace(error);
		}
	}

	[Event(37, Keywords = (EventKeywords)1L, Level = EventLevel.Critical, Task = (EventTask)8, Opcode = (EventOpcode)119, Message = "Transactionscope internal error: {0}")]
	private void TransactionScopeInternalErrorTrace(string error)
	{
		SetActivityId(string.Empty);
		WriteEvent(37, error);
	}

	[NonEvent]
	internal void TransactionScopeTimeout(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionScopeTimeout(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(39, Keywords = (EventKeywords)1L, Level = EventLevel.Warning, Task = (EventTask)8, Opcode = (EventOpcode)128, Message = "Transactionscope timeout: transaction ID is {0}")]
	private void TransactionScopeTimeout(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(39, transactionID);
	}

	[NonEvent]
	internal void TransactionTimeout(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionTimeout(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(30, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)5, Opcode = (EventOpcode)128, Message = "Transaction timeout: transaction ID is {0}")]
	private void TransactionTimeout(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(30, transactionID);
	}

	[NonEvent]
	internal void TransactionstateEnlist(EnlistmentTraceIdentifier enlistmentID, EnlistmentType enlistmentType, EnlistmentOptions enlistmentOption)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			if (enlistmentID.EnlistmentIdentifier != 0)
			{
				TransactionstateEnlist(enlistmentID.EnlistmentIdentifier.ToString(), enlistmentType.ToString(), enlistmentOption.ToString());
			}
			else
			{
				TransactionstateEnlist(string.Empty, enlistmentType.ToString(), enlistmentOption.ToString());
			}
		}
	}

	[Event(40, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, Task = (EventTask)9, Opcode = (EventOpcode)112, Message = "Transactionstate enlist: Enlistment ID is {0}, type is {1} and options is {2}")]
	private void TransactionstateEnlist(string enlistmentID, string type, string option)
	{
		SetActivityId(string.Empty);
		WriteEvent(40, enlistmentID, type, option);
	}

	[NonEvent]
	internal void TransactionCommitted(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			TransactionCommitted(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(20, Keywords = (EventKeywords)2L, Level = EventLevel.Verbose, Task = (EventTask)5, Opcode = (EventOpcode)105, Message = "Transaction committed: transaction ID is {0}")]
	private void TransactionCommitted(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(20, transactionID);
	}

	[NonEvent]
	internal void TransactionInDoubt(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionInDoubt(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(25, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)5, Opcode = (EventOpcode)118, Message = "Transaction indoubt: transaction ID is {0}")]
	private void TransactionInDoubt(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(25, transactionID);
	}

	[NonEvent]
	internal void TransactionPromoted(TransactionTraceIdentifier transactionID, TransactionTraceIdentifier distributedTxID)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			TransactionPromoted(transactionID.TransactionIdentifier ?? string.Empty, distributedTxID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(27, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, Task = (EventTask)5, Opcode = (EventOpcode)123, Message = "Transaction promoted: transaction ID is {0} and distributed transaction ID is {1}")]
	private void TransactionPromoted(string transactionID, string distributedTxID)
	{
		SetActivityId(transactionID);
		WriteEvent(27, transactionID, distributedTxID);
	}

	[NonEvent]
	internal void TransactionAborted(TransactionTraceIdentifier transactionID)
	{
		if (IsEnabled(EventLevel.Warning, EventKeywords.All))
		{
			TransactionAborted(transactionID.TransactionIdentifier ?? string.Empty);
		}
	}

	[Event(17, Keywords = (EventKeywords)2L, Level = EventLevel.Warning, Task = (EventTask)5, Opcode = (EventOpcode)100, Message = "Transaction aborted: transaction ID is {0}")]
	private void TransactionAborted(string transactionID)
	{
		SetActivityId(transactionID);
		WriteEvent(17, transactionID);
	}

	private void SetActivityId(string str)
	{
		Guid result = Guid.Empty;
		if (str.Contains('-'))
		{
			if (str.Length >= 36)
			{
				Guid.TryParse(str.AsSpan(0, 36), out result);
			}
		}
		else if (str.Length >= 32)
		{
			Guid.TryParse(str.AsSpan(0, 32), out result);
		}
		EventSource.SetCurrentThreadActivityId(result);
	}
}
