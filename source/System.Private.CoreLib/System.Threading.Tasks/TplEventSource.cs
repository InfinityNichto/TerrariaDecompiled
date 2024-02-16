using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Threading.Tasks;

[EventSource(Name = "System.Threading.Tasks.TplEventSource", Guid = "2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5", LocalizationResources = "System.Private.CoreLib.Strings")]
internal sealed class TplEventSource : EventSource
{
	public enum TaskWaitBehavior
	{
		Synchronous = 1,
		Asynchronous
	}

	public static class Tasks
	{
		public const EventTask Loop = (EventTask)1;

		public const EventTask Invoke = (EventTask)2;

		public const EventTask TaskExecute = (EventTask)3;

		public const EventTask TaskWait = (EventTask)4;

		public const EventTask ForkJoin = (EventTask)5;

		public const EventTask TaskScheduled = (EventTask)6;

		public const EventTask AwaitTaskContinuationScheduled = (EventTask)7;

		public const EventTask TraceOperation = (EventTask)8;

		public const EventTask TraceSynchronousWork = (EventTask)9;
	}

	public static class Keywords
	{
		public const EventKeywords TaskTransfer = (EventKeywords)1L;

		public const EventKeywords Tasks = (EventKeywords)2L;

		public const EventKeywords Parallel = (EventKeywords)4L;

		public const EventKeywords AsyncCausalityOperation = (EventKeywords)8L;

		public const EventKeywords AsyncCausalityRelation = (EventKeywords)16L;

		public const EventKeywords AsyncCausalitySynchronousWork = (EventKeywords)32L;

		public const EventKeywords TaskStops = (EventKeywords)64L;

		public const EventKeywords TasksFlowActivityIds = (EventKeywords)128L;

		public const EventKeywords AsyncMethod = (EventKeywords)256L;

		public const EventKeywords TasksSetActivityIds = (EventKeywords)65536L;

		public const EventKeywords Debug = (EventKeywords)131072L;

		public const EventKeywords DebugActivityId = (EventKeywords)262144L;
	}

	private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";

	internal bool TasksSetActivityIds;

	internal bool Debug;

	private bool DebugActivityId;

	private const int DefaultAppDomainID = 1;

	public static readonly TplEventSource Log = new TplEventSource();

	private const int TASKSCHEDULED_ID = 7;

	private const int TASKSTARTED_ID = 8;

	private const int TASKCOMPLETED_ID = 9;

	private const int TASKWAITBEGIN_ID = 10;

	private const int TASKWAITEND_ID = 11;

	private const int AWAITTASKCONTINUATIONSCHEDULED_ID = 12;

	private const int TASKWAITCONTINUATIONCOMPLETE_ID = 13;

	private const int TASKWAITCONTINUATIONSTARTED_ID = 19;

	private const int TRACEOPERATIONSTART_ID = 14;

	private const int TRACEOPERATIONSTOP_ID = 15;

	private const int TRACEOPERATIONRELATION_ID = 16;

	private const int TRACESYNCHRONOUSWORKSTART_ID = 17;

	private const int TRACESYNCHRONOUSWORKSTOP_ID = 18;

	private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[40]
	{
		40, 0, 83, 121, 115, 116, 101, 109, 46, 84,
		104, 114, 101, 97, 100, 105, 110, 103, 46, 84,
		97, 115, 107, 115, 46, 84, 112, 108, 69, 118,
		101, 110, 116, 83, 111, 117, 114, 99, 101, 0
	};

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)128L))
		{
			ActivityTracker.Instance.Enable();
		}
		else
		{
			TasksSetActivityIds = IsEnabled(EventLevel.Informational, (EventKeywords)65536L);
		}
		Debug = IsEnabled(EventLevel.Informational, (EventKeywords)131072L);
		DebugActivityId = IsEnabled(EventLevel.Informational, (EventKeywords)262144L);
	}

	private TplEventSource(int _)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(7, Task = (EventTask)6, Version = 1, Opcode = EventOpcode.Send, Level = EventLevel.Informational, Keywords = (EventKeywords)3L)]
	public unsafe void TaskScheduled(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, int CreatingTaskID, int TaskCreationOptions, int appDomain = 1)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)3L))
		{
			EventData* ptr = stackalloc EventData[6];
			ptr->Size = 4;
			ptr->DataPointer = (IntPtr)(&OriginatingTaskSchedulerID);
			ptr->Reserved = 0;
			ptr[1].Size = 4;
			ptr[1].DataPointer = (IntPtr)(&OriginatingTaskID);
			ptr[1].Reserved = 0;
			ptr[2].Size = 4;
			ptr[2].DataPointer = (IntPtr)(&TaskID);
			ptr[2].Reserved = 0;
			ptr[3].Size = 4;
			ptr[3].DataPointer = (IntPtr)(&CreatingTaskID);
			ptr[3].Reserved = 0;
			ptr[4].Size = 4;
			ptr[4].DataPointer = (IntPtr)(&TaskCreationOptions);
			ptr[4].Reserved = 0;
			ptr[5].Size = 4;
			ptr[5].DataPointer = (IntPtr)(&appDomain);
			ptr[5].Reserved = 0;
			if (TasksSetActivityIds)
			{
				Guid guid = CreateGuidForTaskID(TaskID);
				WriteEventWithRelatedActivityIdCore(7, &guid, 6, ptr);
			}
			else
			{
				WriteEventCore(7, 6, ptr);
			}
		}
	}

	[Event(8, Level = EventLevel.Informational, Keywords = (EventKeywords)2L)]
	public void TaskStarted(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)2L))
		{
			WriteEvent(8, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(9, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)64L)]
	public unsafe void TaskCompleted(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, bool IsExceptional)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)2L))
		{
			EventData* ptr = stackalloc EventData[4];
			int num = (IsExceptional ? 1 : 0);
			ptr->Size = 4;
			ptr->DataPointer = (IntPtr)(&OriginatingTaskSchedulerID);
			ptr->Reserved = 0;
			ptr[1].Size = 4;
			ptr[1].DataPointer = (IntPtr)(&OriginatingTaskID);
			ptr[1].Reserved = 0;
			ptr[2].Size = 4;
			ptr[2].DataPointer = (IntPtr)(&TaskID);
			ptr[2].Reserved = 0;
			ptr[3].Size = 4;
			ptr[3].DataPointer = (IntPtr)(&num);
			ptr[3].Reserved = 0;
			WriteEventCore(9, 4, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(10, Version = 3, Task = (EventTask)4, Opcode = EventOpcode.Send, Level = EventLevel.Informational, Keywords = (EventKeywords)3L)]
	public unsafe void TaskWaitBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, TaskWaitBehavior Behavior, int ContinueWithTaskID)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)3L))
		{
			EventData* ptr = stackalloc EventData[5];
			ptr->Size = 4;
			ptr->DataPointer = (IntPtr)(&OriginatingTaskSchedulerID);
			ptr->Reserved = 0;
			ptr[1].Size = 4;
			ptr[1].DataPointer = (IntPtr)(&OriginatingTaskID);
			ptr[1].Reserved = 0;
			ptr[2].Size = 4;
			ptr[2].DataPointer = (IntPtr)(&TaskID);
			ptr[2].Reserved = 0;
			ptr[3].Size = 4;
			ptr[3].DataPointer = (IntPtr)(&Behavior);
			ptr[3].Reserved = 0;
			ptr[4].Size = 4;
			ptr[4].DataPointer = (IntPtr)(&ContinueWithTaskID);
			ptr[4].Reserved = 0;
			if (TasksSetActivityIds)
			{
				Guid guid = CreateGuidForTaskID(TaskID);
				WriteEventWithRelatedActivityIdCore(10, &guid, 5, ptr);
			}
			else
			{
				WriteEventCore(10, 5, ptr);
			}
		}
	}

	[Event(11, Level = EventLevel.Verbose, Keywords = (EventKeywords)2L)]
	public void TaskWaitEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Verbose, (EventKeywords)2L))
		{
			WriteEvent(11, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
		}
	}

	[Event(13, Level = EventLevel.Verbose, Keywords = (EventKeywords)64L)]
	public void TaskWaitContinuationComplete(int TaskID)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Verbose, (EventKeywords)64L))
		{
			WriteEvent(13, TaskID);
		}
	}

	[Event(19, Level = EventLevel.Verbose, Keywords = (EventKeywords)2L)]
	public void TaskWaitContinuationStarted(int TaskID)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Verbose, (EventKeywords)2L))
		{
			WriteEvent(19, TaskID);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(12, Task = (EventTask)7, Opcode = EventOpcode.Send, Level = EventLevel.Informational, Keywords = (EventKeywords)3L)]
	public unsafe void AwaitTaskContinuationScheduled(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ContinueWithTaskId)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)3L))
		{
			EventData* ptr = stackalloc EventData[3];
			ptr->Size = 4;
			ptr->DataPointer = (IntPtr)(&OriginatingTaskSchedulerID);
			ptr->Reserved = 0;
			ptr[1].Size = 4;
			ptr[1].DataPointer = (IntPtr)(&OriginatingTaskID);
			ptr[1].Reserved = 0;
			ptr[2].Size = 4;
			ptr[2].DataPointer = (IntPtr)(&ContinueWithTaskId);
			ptr[2].Reserved = 0;
			if (TasksSetActivityIds)
			{
				Guid guid = CreateGuidForTaskID(ContinueWithTaskId);
				WriteEventWithRelatedActivityIdCore(12, &guid, 3, ptr);
			}
			else
			{
				WriteEventCore(12, 3, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(14, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	public unsafe void TraceOperationBegin(int TaskID, string OperationName, long RelatedContext)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)8L))
		{
			fixed (char* ptr2 = OperationName)
			{
				EventData* ptr = stackalloc EventData[3];
				ptr->Size = 4;
				ptr->DataPointer = (IntPtr)(&TaskID);
				ptr->Reserved = 0;
				ptr[1].Size = (OperationName.Length + 1) * 2;
				ptr[1].DataPointer = (IntPtr)ptr2;
				ptr[1].Reserved = 0;
				ptr[2].Size = 8;
				ptr[2].DataPointer = (IntPtr)(&RelatedContext);
				ptr[2].Reserved = 0;
				WriteEventCore(14, 3, ptr);
			}
		}
	}

	[Event(16, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)16L)]
	public void TraceOperationRelation(int TaskID, CausalityRelation Relation)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)16L))
		{
			WriteEvent(16, TaskID, (int)Relation);
		}
	}

	[Event(15, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	public void TraceOperationEnd(int TaskID, AsyncCausalityStatus Status)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)8L))
		{
			WriteEvent(15, TaskID, (int)Status);
		}
	}

	[Event(17, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)32L)]
	public void TraceSynchronousWorkBegin(int TaskID, CausalitySynchronousWork Work)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)32L))
		{
			WriteEvent(17, TaskID, (int)Work);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(18, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)32L)]
	public unsafe void TraceSynchronousWorkEnd(CausalitySynchronousWork Work)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Informational, (EventKeywords)32L))
		{
			EventData* ptr = stackalloc EventData[1];
			ptr->Size = 4;
			ptr->DataPointer = (IntPtr)(&Work);
			ptr->Reserved = 0;
			WriteEventCore(18, 1, ptr);
		}
	}

	[NonEvent]
	public unsafe void RunningContinuation(int TaskID, object Object)
	{
		RunningContinuation(TaskID, (long)(nuint)(*(nint*)Unsafe.AsPointer(ref Object)));
	}

	[Event(20, Keywords = (EventKeywords)131072L)]
	private void RunningContinuation(int TaskID, long Object)
	{
		if (Debug)
		{
			WriteEvent(20, TaskID, Object);
		}
	}

	[NonEvent]
	public unsafe void RunningContinuationList(int TaskID, int Index, object Object)
	{
		RunningContinuationList(TaskID, Index, (long)(nuint)(*(nint*)Unsafe.AsPointer(ref Object)));
	}

	[Event(21, Keywords = (EventKeywords)131072L)]
	public void RunningContinuationList(int TaskID, int Index, long Object)
	{
		if (Debug)
		{
			WriteEvent(21, TaskID, Index, Object);
		}
	}

	[Event(23, Keywords = (EventKeywords)131072L)]
	public void DebugFacilityMessage(string Facility, string Message)
	{
		WriteEvent(23, Facility, Message);
	}

	[Event(24, Keywords = (EventKeywords)131072L)]
	public void DebugFacilityMessage1(string Facility, string Message, string Value1)
	{
		WriteEvent(24, Facility, Message, Value1);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Guid parameter is safe with WriteEvent")]
	[Event(25, Keywords = (EventKeywords)262144L)]
	public void SetActivityId(Guid NewId)
	{
		if (DebugActivityId)
		{
			WriteEvent(25, NewId);
		}
	}

	[Event(26, Keywords = (EventKeywords)131072L)]
	public void NewID(int TaskID)
	{
		if (Debug)
		{
			WriteEvent(26, TaskID);
		}
	}

	[NonEvent]
	public void IncompleteAsyncMethod(IAsyncStateMachineBox stateMachineBox)
	{
		if (IsEnabled() && IsEnabled(EventLevel.Warning, (EventKeywords)256L))
		{
			IAsyncStateMachine stateMachineObject = stateMachineBox.GetStateMachineObject();
			if (stateMachineObject != null)
			{
				string asyncStateMachineDescription = AsyncMethodBuilderCore.GetAsyncStateMachineDescription(stateMachineObject);
				IncompleteAsyncMethod(asyncStateMachineDescription);
			}
		}
	}

	[Event(27, Level = EventLevel.Warning, Keywords = (EventKeywords)256L)]
	private void IncompleteAsyncMethod(string stateMachineDescription)
	{
		WriteEvent(27, stateMachineDescription);
	}

	internal static Guid CreateGuidForTaskID(int taskID)
	{
		int processId = Environment.ProcessId;
		return new Guid(taskID, 1, 0, (byte)processId, (byte)(processId >> 8), (byte)(processId >> 16), (byte)(processId >> 24), byte.MaxValue, 220, 215, 181);
	}

	private TplEventSource()
		: base(new Guid(777894471u, 41938, 19734, 142, 224, 102, 113, byte.MaxValue, 220, 215, 181), "System.Threading.Tasks.TplEventSource")
	{
	}
}
