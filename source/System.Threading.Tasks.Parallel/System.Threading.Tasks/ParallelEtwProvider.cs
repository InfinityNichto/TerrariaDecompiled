using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Threading.Tasks;

[EventSource(Name = "System.Threading.Tasks.Parallel.EventSource")]
internal sealed class ParallelEtwProvider : EventSource
{
	public enum ForkJoinOperationType
	{
		ParallelInvoke = 1,
		ParallelFor,
		ParallelForEach
	}

	public static class Tasks
	{
		public const EventTask Loop = (EventTask)1;

		public const EventTask Invoke = (EventTask)2;

		public const EventTask ForkJoin = (EventTask)5;
	}

	public static readonly ParallelEtwProvider Log = new ParallelEtwProvider();

	private ParallelEtwProvider()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(1, Level = EventLevel.Informational, Task = (EventTask)1, Opcode = EventOpcode.Start)]
	public unsafe void ParallelLoopBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, ForkJoinOperationType OperationType, long InclusiveFrom, long ExclusiveTo)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			EventData* ptr = stackalloc EventData[6];
			*ptr = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskSchedulerID)
			};
			ptr[1] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskID)
			};
			ptr[2] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&ForkJoinContextID)
			};
			ptr[3] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OperationType)
			};
			ptr[4] = new EventData
			{
				Size = 8,
				DataPointer = (IntPtr)(&InclusiveFrom)
			};
			ptr[5] = new EventData
			{
				Size = 8,
				DataPointer = (IntPtr)(&ExclusiveTo)
			};
			WriteEventCore(1, 6, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(2, Level = EventLevel.Informational, Task = (EventTask)1, Opcode = EventOpcode.Stop)]
	public unsafe void ParallelLoopEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, long TotalIterations)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			EventData* ptr = stackalloc EventData[4];
			*ptr = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskSchedulerID)
			};
			ptr[1] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskID)
			};
			ptr[2] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&ForkJoinContextID)
			};
			ptr[3] = new EventData
			{
				Size = 8,
				DataPointer = (IntPtr)(&TotalIterations)
			};
			WriteEventCore(2, 4, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(3, Level = EventLevel.Informational, Task = (EventTask)2, Opcode = EventOpcode.Start)]
	public unsafe void ParallelInvokeBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, ForkJoinOperationType OperationType, int ActionCount)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			EventData* ptr = stackalloc EventData[5];
			*ptr = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskSchedulerID)
			};
			ptr[1] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OriginatingTaskID)
			};
			ptr[2] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&ForkJoinContextID)
			};
			ptr[3] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&OperationType)
			};
			ptr[4] = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&ActionCount)
			};
			WriteEventCore(3, 5, ptr);
		}
	}

	[Event(4, Level = EventLevel.Informational, Task = (EventTask)2, Opcode = EventOpcode.Stop)]
	public void ParallelInvokeEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			WriteEvent(4, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
		}
	}

	[Event(5, Level = EventLevel.Verbose, Task = (EventTask)5, Opcode = EventOpcode.Start)]
	public void ParallelFork(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			WriteEvent(5, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
		}
	}

	[Event(6, Level = EventLevel.Verbose, Task = (EventTask)5, Opcode = EventOpcode.Stop)]
	public void ParallelJoin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			WriteEvent(6, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
		}
	}
}
