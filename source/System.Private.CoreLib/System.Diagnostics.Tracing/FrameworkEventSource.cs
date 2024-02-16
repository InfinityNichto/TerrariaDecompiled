using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing;

[EventSource(Guid = "8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1", Name = "System.Diagnostics.Eventing.FrameworkEventSource")]
internal sealed class FrameworkEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords ThreadPool = (EventKeywords)2L;

		public const EventKeywords ThreadTransfer = (EventKeywords)16L;
	}

	public static class Tasks
	{
		public const EventTask ThreadTransfer = (EventTask)3;
	}

	private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";

	public static readonly FrameworkEventSource Log = new FrameworkEventSource();

	private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[51]
	{
		51, 0, 83, 121, 115, 116, 101, 109, 46, 68,
		105, 97, 103, 110, 111, 115, 116, 105, 99, 115,
		46, 69, 118, 101, 110, 116, 105, 110, 103, 46,
		70, 114, 97, 109, 101, 119, 111, 114, 107, 69,
		118, 101, 110, 116, 83, 111, 117, 114, 99, 101,
		0
	};

	private FrameworkEventSource(int _)
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3, bool arg4, int arg5, int arg6)
	{
		if (IsEnabled())
		{
			if (arg3 == null)
			{
				arg3 = "";
			}
			fixed (char* ptr2 = arg3)
			{
				EventData* ptr = stackalloc EventData[6];
				ptr->DataPointer = (IntPtr)(&arg1);
				ptr->Size = 8;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)(&arg2);
				ptr[1].Size = 4;
				ptr[1].Reserved = 0;
				ptr[2].DataPointer = (IntPtr)ptr2;
				ptr[2].Size = (arg3.Length + 1) * 2;
				ptr[2].Reserved = 0;
				ptr[3].DataPointer = (IntPtr)(&arg4);
				ptr[3].Size = 4;
				ptr[3].Reserved = 0;
				ptr[4].DataPointer = (IntPtr)(&arg5);
				ptr[4].Size = 4;
				ptr[4].Reserved = 0;
				ptr[5].DataPointer = (IntPtr)(&arg6);
				ptr[5].Size = 4;
				ptr[5].Reserved = 0;
				WriteEventCore(eventId, 6, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3)
	{
		if (IsEnabled())
		{
			if (arg3 == null)
			{
				arg3 = "";
			}
			fixed (char* ptr2 = arg3)
			{
				EventData* ptr = stackalloc EventData[3];
				ptr->DataPointer = (IntPtr)(&arg1);
				ptr->Size = 8;
				ptr->Reserved = 0;
				ptr[1].DataPointer = (IntPtr)(&arg2);
				ptr[1].Size = 4;
				ptr[1].Reserved = 0;
				ptr[2].DataPointer = (IntPtr)ptr2;
				ptr[2].Size = (arg3.Length + 1) * 2;
				ptr[2].Reserved = 0;
				WriteEventCore(eventId, 3, ptr);
			}
		}
	}

	[Event(30, Level = EventLevel.Verbose, Keywords = (EventKeywords)18L)]
	public void ThreadPoolEnqueueWork(long workID)
	{
		WriteEvent(30, workID);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[NonEvent]
	public void ThreadPoolEnqueueWorkObject(object workID)
	{
		ThreadPoolEnqueueWork(workID.GetHashCode());
	}

	[Event(31, Level = EventLevel.Verbose, Keywords = (EventKeywords)18L)]
	public void ThreadPoolDequeueWork(long workID)
	{
		WriteEvent(31, workID);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[NonEvent]
	public void ThreadPoolDequeueWorkObject(object workID)
	{
		ThreadPoolDequeueWork(workID.GetHashCode());
	}

	[Event(150, Level = EventLevel.Informational, Keywords = (EventKeywords)16L, Task = (EventTask)3, Opcode = EventOpcode.Send)]
	public void ThreadTransferSend(long id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2)
	{
		WriteEvent(150, id, kind, info, multiDequeues, intInfo1, intInfo2);
	}

	[NonEvent]
	public void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2)
	{
		ThreadTransferSend(id.GetHashCode(), kind, info, multiDequeues, intInfo1, intInfo2);
	}

	[Event(151, Level = EventLevel.Informational, Keywords = (EventKeywords)16L, Task = (EventTask)3, Opcode = EventOpcode.Receive)]
	public void ThreadTransferReceive(long id, int kind, string info)
	{
		WriteEvent(151, id, kind, info);
	}

	[NonEvent]
	public void ThreadTransferReceiveObj(object id, int kind, string info)
	{
		ThreadTransferReceive(id.GetHashCode(), kind, info);
	}

	private FrameworkEventSource()
		: base(new Guid(2392805520u, 11637, 19715, 138, 129, 229, 175, 191, 133, 218, 241), "System.Diagnostics.Eventing.FrameworkEventSource")
	{
	}
}
