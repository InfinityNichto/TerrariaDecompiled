using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.Tracing;

[EventSource(Guid = "E13C0D23-CCBC-4E12-931B-D9CC2EEE27E4", Name = "Microsoft-Windows-DotNETRuntime")]
internal sealed class NativeRuntimeEventSource : EventSource
{
	private static class Messages
	{
		public const string WorkerThread = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}";

		public const string WorkerThreadAdjustmentSample = "Throughput={0};\nClrInstanceID={1}";

		public const string WorkerThreadAdjustmentAdjustment = "AverageThroughput={0};\nNewWorkerThreadCount={1};\nReason={2};\nClrInstanceID={3}";

		public const string WorkerThreadAdjustmentStats = "Duration={0};\nThroughput={1};\nThreadWave={2};\nThroughputWave={3};\nThroughputErrorEstimate={4};\nAverageThroughputErrorEstimate={5};\nThroughputRatio={6};\nConfidence={7};\nNewControlSetting={8};\nNewThreadWaveMagnitude={9};\nClrInstanceID={10}";

		public const string IOEnqueue = "NativeOverlapped={0};\nOverlapped={1};\nMultiDequeues={2};\nClrInstanceID={3}";

		public const string IO = "NativeOverlapped={0};\nOverlapped={1};\nClrInstanceID={2}";

		public const string WorkingThreadCount = "Count={0};\nClrInstanceID={1}";
	}

	public static class Tasks
	{
		public const EventTask ThreadPoolWorkerThread = (EventTask)16;

		public const EventTask ThreadPoolWorkerThreadAdjustment = (EventTask)18;

		public const EventTask ThreadPool = (EventTask)23;

		public const EventTask ThreadPoolWorkingThreadCount = (EventTask)22;
	}

	public static class Opcodes
	{
		public const EventOpcode IOEnqueue = (EventOpcode)13;

		public const EventOpcode IODequeue = (EventOpcode)14;

		public const EventOpcode Wait = (EventOpcode)90;

		public const EventOpcode Sample = (EventOpcode)100;

		public const EventOpcode Adjustment = (EventOpcode)101;

		public const EventOpcode Stats = (EventOpcode)102;
	}

	public enum ThreadAdjustmentReasonMap : uint
	{
		Warmup,
		Initializing,
		RandomMove,
		ClimbingMove,
		ChangePoint,
		Stabilizing,
		Starvation,
		ThreadTimedOut
	}

	public static class Keywords
	{
		public const EventKeywords GCKeyword = (EventKeywords)1L;

		public const EventKeywords GCHandleKeyword = (EventKeywords)2L;

		public const EventKeywords AssemblyLoaderKeyword = (EventKeywords)4L;

		public const EventKeywords LoaderKeyword = (EventKeywords)8L;

		public const EventKeywords JitKeyword = (EventKeywords)16L;

		public const EventKeywords NGenKeyword = (EventKeywords)32L;

		public const EventKeywords StartEnumerationKeyword = (EventKeywords)64L;

		public const EventKeywords EndEnumerationKeyword = (EventKeywords)128L;

		public const EventKeywords SecurityKeyword = (EventKeywords)1024L;

		public const EventKeywords AppDomainResourceManagementKeyword = (EventKeywords)2048L;

		public const EventKeywords JitTracingKeyword = (EventKeywords)4096L;

		public const EventKeywords InteropKeyword = (EventKeywords)8192L;

		public const EventKeywords ContentionKeyword = (EventKeywords)16384L;

		public const EventKeywords ExceptionKeyword = (EventKeywords)32768L;

		public const EventKeywords ThreadingKeyword = (EventKeywords)65536L;

		public const EventKeywords JittedMethodILToNativeMapKeyword = (EventKeywords)131072L;

		public const EventKeywords OverrideAndSuppressNGenEventsKeyword = (EventKeywords)262144L;

		public const EventKeywords TypeKeyword = (EventKeywords)524288L;

		public const EventKeywords GCHeapDumpKeyword = (EventKeywords)1048576L;

		public const EventKeywords GCSampledObjectAllocationHighKeyword = (EventKeywords)2097152L;

		public const EventKeywords GCHeapSurvivalAndMovementKeyword = (EventKeywords)4194304L;

		public const EventKeywords GCHeapCollectKeyword = (EventKeywords)8388608L;

		public const EventKeywords GCHeapAndTypeNamesKeyword = (EventKeywords)16777216L;

		public const EventKeywords GCSampledObjectAllocationLowKeyword = (EventKeywords)33554432L;

		public const EventKeywords PerfTrackKeyword = (EventKeywords)536870912L;

		public const EventKeywords StackKeyword = (EventKeywords)1073741824L;

		public const EventKeywords ThreadTransferKeyword = (EventKeywords)2147483648L;

		public const EventKeywords DebuggerKeyword = (EventKeywords)4294967296L;

		public const EventKeywords MonitoringKeyword = (EventKeywords)8589934592L;

		public const EventKeywords CodeSymbolsKeyword = (EventKeywords)17179869184L;

		public const EventKeywords EventSourceKeyword = (EventKeywords)34359738368L;

		public const EventKeywords CompilationKeyword = (EventKeywords)68719476736L;

		public const EventKeywords CompilationDiagnosticKeyword = (EventKeywords)137438953472L;

		public const EventKeywords MethodDiagnosticKeyword = (EventKeywords)274877906944L;

		public const EventKeywords TypeDiagnosticKeyword = (EventKeywords)549755813888L;

		public const EventKeywords JitInstrumentationDataKeyword = (EventKeywords)1099511627776L;

		public const EventKeywords ProfilerKeyword = (EventKeywords)2199023255552L;
	}

	internal const string EventSourceName = "Microsoft-Windows-DotNETRuntime";

	public static readonly NativeRuntimeEventSource Log = new NativeRuntimeEventSource();

	private const ushort DefaultClrInstanceId = 0;

	private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[34]
	{
		34, 0, 77, 105, 99, 114, 111, 115, 111, 102,
		116, 45, 87, 105, 110, 100, 111, 119, 115, 45,
		68, 111, 116, 78, 69, 84, 82, 117, 110, 116,
		105, 109, 101, 0
	};

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkerThreadAdjustmentStats(double Duration, double Throughput, double ThreadPoolWorkerThreadWait, double ThroughputWave, double ThroughputErrorEstimate, double AverageThroughputErrorEstimate, double ThroughputRatio, double Confidence, double NewControlSetting, ushort NewThreadWaveMagnitude, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolIOEnqueue(IntPtr NativeOverlapped, IntPtr Overlapped, bool MultiDequeues, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolIODequeue(IntPtr NativeOverlapped, IntPtr Overlapped, ushort ClrInstanceID);

	[DllImport("QCall")]
	[NonEvent]
	internal static extern void LogThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID);

	private NativeRuntimeEventSource(int _)
	{
	}

	[NonEvent]
	internal unsafe void ProcessEvent(uint eventID, uint osThreadID, DateTime timeStamp, Guid activityId, Guid childActivityId, ReadOnlySpan<byte> payload)
	{
		if (System.Diagnostics.Tracing.EventSource.IsSupported && eventID < m_eventData.Length)
		{
			object[] list = EventPipePayloadDecoder.DecodePayload(ref m_eventData[eventID], payload);
			EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this, (int)eventID, &activityId, &childActivityId)
			{
				OSThreadId = (int)osThreadID,
				TimeStamp = timeStamp,
				Payload = new ReadOnlyCollection<object>(list)
			};
			DispatchToAllListeners(eventCallbackArgs);
		}
	}

	[Event(50, Level = EventLevel.Informational, Message = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}", Task = (EventTask)16, Opcode = EventOpcode.Start, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount = 0u, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
		}
	}

	[Event(51, Level = EventLevel.Informational, Message = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}", Task = (EventTask)16, Opcode = EventOpcode.Stop, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount = 0u, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Event(57, Level = EventLevel.Informational, Message = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}", Task = (EventTask)16, Opcode = (EventOpcode)90, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount = 0u, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
		}
	}

	[Event(54, Level = EventLevel.Informational, Message = "Throughput={0};\nClrInstanceID={1}", Task = (EventTask)18, Opcode = (EventOpcode)100, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
		}
	}

	[Event(55, Level = EventLevel.Informational, Message = "AverageThroughput={0};\nNewWorkerThreadCount={1};\nReason={2};\nClrInstanceID={3}", Task = (EventTask)18, Opcode = (EventOpcode)101, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
		}
	}

	[Event(56, Level = EventLevel.Verbose, Message = "Duration={0};\nThroughput={1};\nThreadWave={2};\nThroughputWave={3};\nThroughputErrorEstimate={4};\nAverageThroughputErrorEstimate={5};\nThroughputRatio={6};\nConfidence={7};\nNewControlSetting={8};\nNewThreadWaveMagnitude={9};\nClrInstanceID={10}", Task = (EventTask)18, Opcode = (EventOpcode)102, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkerThreadAdjustmentStats(double Duration, double Throughput, double ThreadWave, double ThroughputWave, double ThroughputErrorEstimate, double AverageThroughputErrorEstimate, double ThroughputRatio, double Confidence, double NewControlSetting, ushort NewThreadWaveMagnitude, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Verbose, (EventKeywords)65536L))
		{
			LogThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadWave, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
		}
	}

	[Event(63, Level = EventLevel.Verbose, Message = "NativeOverlapped={0};\nOverlapped={1};\nMultiDequeues={2};\nClrInstanceID={3}", Task = (EventTask)23, Opcode = (EventOpcode)13, Version = 0, Keywords = (EventKeywords)2147549184L)]
	private void ThreadPoolIOEnqueue(IntPtr NativeOverlapped, IntPtr Overlapped, bool MultiDequeues, ushort ClrInstanceID = 0)
	{
		int num = Convert.ToInt32(MultiDequeues);
		LogThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[NonEvent]
	public void ThreadPoolIOEnqueue(RegisteredWaitHandle registeredWaitHandle)
	{
		if (IsEnabled(EventLevel.Verbose, (EventKeywords)2147549184L))
		{
			ThreadPoolIOEnqueue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero, registeredWaitHandle.Repeating, 0);
		}
	}

	[Event(64, Level = EventLevel.Verbose, Message = "NativeOverlapped={0};\nOverlapped={1};\nClrInstanceID={2}", Task = (EventTask)23, Opcode = (EventOpcode)14, Version = 0, Keywords = (EventKeywords)2147549184L)]
	private void ThreadPoolIODequeue(IntPtr NativeOverlapped, IntPtr Overlapped, ushort ClrInstanceID = 0)
	{
		LogThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[NonEvent]
	public void ThreadPoolIODequeue(RegisteredWaitHandle registeredWaitHandle)
	{
		if (IsEnabled(EventLevel.Verbose, (EventKeywords)2147549184L))
		{
			ThreadPoolIODequeue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero, 0);
		}
	}

	[Event(60, Level = EventLevel.Verbose, Message = "Count={0};\nClrInstanceID={1}", Task = (EventTask)22, Opcode = EventOpcode.Start, Version = 0, Keywords = (EventKeywords)65536L)]
	public void ThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID = 0)
	{
		if (IsEnabled(EventLevel.Verbose, (EventKeywords)65536L))
		{
			LogThreadPoolWorkingThreadCount(Count, ClrInstanceID);
		}
	}

	[Event(1, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCStart_V2(uint Count, uint Depth, uint Reason, uint Type, ushort ClrInstanceID, ulong ClientSequenceNumber)
	{
		throw new NotImplementedException();
	}

	[Event(2, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCEnd_V1(uint Count, uint Depth, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(3, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCRestartEEEnd_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(4, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCHeapStats_V2(ulong GenerationSize0, ulong TotalPromotedSize0, ulong GenerationSize1, ulong TotalPromotedSize1, ulong GenerationSize2, ulong TotalPromotedSize2, ulong GenerationSize3, ulong TotalPromotedSize3, ulong FinalizationPromotedSize, ulong FinalizationPromotedCount, uint PinnedObjectCount, uint SinkBlockCount, uint GCHandleCount, ushort ClrInstanceID, ulong GenerationSize4, ulong TotalPromotedSize4)
	{
		throw new NotImplementedException();
	}

	[Event(5, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCCreateSegment_V1(ulong Address, ulong Size, uint Type, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(6, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCFreeSegment_V1(ulong Address, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(7, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCRestartEEBegin_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(8, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCSuspendEEEnd_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(9, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCSuspendEEBegin_V1(uint Reason, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(10, Version = 4, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void GCAllocationTick_V4(uint AllocationAmount, uint AllocationKind, ushort ClrInstanceID, ulong AllocationAmount64, IntPtr TypeID, string TypeName, uint HeapIndex, IntPtr Address, ulong ObjectSize)
	{
		throw new NotImplementedException();
	}

	[Event(11, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65537L)]
	private void GCCreateConcurrentThread_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(12, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65537L)]
	private void GCTerminateConcurrentThread_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(13, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCFinalizersEnd_V1(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(14, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCFinalizersBegin_V1(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(15, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)524288L)]
	private void BulkType(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(16, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkRootEdge(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(17, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkRootConditionalWeakTableElementEdge(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(18, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkNode(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(19, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkEdge(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(20, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2097152L)]
	private void GCSampledObjectAllocationHigh(IntPtr Address, IntPtr TypeID, uint ObjectCountForTypeSample, ulong TotalSizeForTypeSample, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(21, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4194304L)]
	private void GCBulkSurvivingObjectRanges(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(22, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4194304L)]
	private void GCBulkMovedObjectRanges(uint Index, uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(23, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4194304L)]
	private void GCGenerationRange(byte Generation, IntPtr RangeStart, ulong RangeUsedLength, ulong RangeReservedLength, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(25, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCMarkStackRoots(uint HeapNum, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(26, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCMarkFinalizeQueueRoots(uint HeapNum, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(27, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCMarkHandles(uint HeapNum, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(28, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCMarkOlderGenerationRoots(uint HeapNum, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(29, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void FinalizeObject(IntPtr TypeID, IntPtr ObjectID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(30, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2L)]
	private void SetGCHandle(IntPtr HandleID, IntPtr ObjectID, uint Kind, uint Generation, ulong AppDomainID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(31, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2L)]
	private void DestroyGCHandle(IntPtr HandleID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(32, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)33554432L)]
	private void GCSampledObjectAllocationLow(IntPtr Address, IntPtr TypeID, uint ObjectCountForTypeSample, ulong TotalSizeForTypeSample, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(33, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void PinObjectAtGCTime(IntPtr HandleID, IntPtr ObjectID, ulong ObjectSize, string TypeName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(35, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCTriggered(uint Reason, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(36, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkRootCCW(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(37, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkRCW(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(38, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GCBulkRootStaticVar(uint Count, ulong AppDomainID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(39, Version = 0, Level = EventLevel.LogAlways, Keywords = (EventKeywords)66060291L)]
	private void GCDynamicEvent(string Name, uint DataSize)
	{
		throw new NotImplementedException();
	}

	[Event(40, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void WorkerThreadCreate(uint WorkerThreadCount, uint RetiredWorkerThreads)
	{
		throw new NotImplementedException();
	}

	[Event(41, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void WorkerThreadTerminate(uint WorkerThreadCount, uint RetiredWorkerThreads)
	{
		throw new NotImplementedException();
	}

	[Event(42, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void WorkerThreadRetire(uint WorkerThreadCount, uint RetiredWorkerThreads)
	{
		throw new NotImplementedException();
	}

	[Event(43, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void WorkerThreadUnretire(uint WorkerThreadCount, uint RetiredWorkerThreads)
	{
		throw new NotImplementedException();
	}

	[Event(44, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void IOThreadCreate_V1(uint IOThreadCount, uint RetiredIOThreads, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(45, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void IOThreadTerminate_V1(uint IOThreadCount, uint RetiredIOThreads, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(46, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void IOThreadRetire_V1(uint IOThreadCount, uint RetiredIOThreads, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(47, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void IOThreadUnretire_V1(uint IOThreadCount, uint RetiredIOThreads, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(48, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void ThreadpoolSuspensionSuspendThread(uint ClrThreadID, uint CpuUtilization)
	{
		throw new NotImplementedException();
	}

	[Event(49, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void ThreadpoolSuspensionResumeThread(uint ClrThreadID, uint CpuUtilization)
	{
		throw new NotImplementedException();
	}

	[Event(58, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)65536L)]
	private void YieldProcessorMeasurement(ushort ClrInstanceID, double NsPerYield, double EstablishedNsPerYield)
	{
		throw new NotImplementedException();
	}

	[Event(70, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2147549184L)]
	private void ThreadCreating(IntPtr ID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(71, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2147549184L)]
	private void ThreadRunning(IntPtr ID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(72, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)274877906944L)]
	private void MethodDetails(ulong MethodID, ulong TypeID, uint MethodToken, uint TypeParameterCount, ulong LoaderModuleID)
	{
		throw new NotImplementedException();
	}

	[Event(73, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)549755813888L)]
	private void TypeLoadStart(uint TypeLoadStartID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(74, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)549755813888L)]
	private void TypeLoadStop(uint TypeLoadStartID, ushort ClrInstanceID, ushort LoadLevel, ulong TypeID, string TypeName)
	{
		throw new NotImplementedException();
	}

	[Event(80, Version = 1, Level = EventLevel.Error, Keywords = (EventKeywords)8589967360L)]
	private void ExceptionThrown_V1(string ExceptionType, string ExceptionMessage, IntPtr ExceptionEIP, uint ExceptionHRESULT, ushort ExceptionFlags, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(250, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionCatchStart(ulong EntryEIP, ulong MethodID, string MethodName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(251, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionCatchStop()
	{
		throw new NotImplementedException();
	}

	[Event(252, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionFinallyStart(ulong EntryEIP, ulong MethodID, string MethodName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(253, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionFinallyStop()
	{
		throw new NotImplementedException();
	}

	[Event(254, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionFilterStart(ulong EntryEIP, ulong MethodID, string MethodName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(255, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionFilterStop()
	{
		throw new NotImplementedException();
	}

	[Event(256, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)32768L)]
	private void ExceptionThrownStop()
	{
		throw new NotImplementedException();
	}

	[Event(81, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)16384L)]
	private void ContentionStart_V1(byte ContentionFlags, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(91, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)16384L)]
	private void ContentionStop_V1(byte ContentionFlags, ushort ClrInstanceID, double DurationNs)
	{
		throw new NotImplementedException();
	}

	[Event(82, Version = 0, Level = EventLevel.LogAlways, Keywords = (EventKeywords)1073741824L)]
	private void CLRStackWalk(ushort ClrInstanceID, byte Reserved1, byte Reserved2, uint FrameCount)
	{
		throw new NotImplementedException();
	}

	[Event(83, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2048L)]
	private void AppDomainMemAllocated(ulong AppDomainID, ulong Allocated, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(84, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2048L)]
	private void AppDomainMemSurvived(ulong AppDomainID, ulong Survived, ulong ProcessSurvived, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(85, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)67584L)]
	private void ThreadCreated(ulong ManagedThreadID, ulong AppDomainID, uint Flags, uint ManagedThreadIndex, uint OSThreadID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(86, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)67584L)]
	private void ThreadTerminated(ulong ManagedThreadID, ulong AppDomainID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(87, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)67584L)]
	private void ThreadDomainEnter(ulong ManagedThreadID, ulong AppDomainID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(88, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)8192L)]
	private void ILStubGenerated(ushort ClrInstanceID, ulong ModuleID, ulong StubMethodID, uint StubFlags, uint ManagedInteropMethodToken, string ManagedInteropMethodNamespace, string ManagedInteropMethodName, string ManagedInteropMethodSignature, string NativeMethodSignature, string StubMethodSignature, string StubMethodILCode)
	{
		throw new NotImplementedException();
	}

	[Event(89, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)8192L)]
	private void ILStubCacheHit(ushort ClrInstanceID, ulong ModuleID, ulong StubMethodID, uint ManagedInteropMethodToken, string ManagedInteropMethodNamespace, string ManagedInteropMethodName, string ManagedInteropMethodSignature)
	{
		throw new NotImplementedException();
	}

	[Event(135, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void DCStartCompleteV2()
	{
		throw new NotImplementedException();
	}

	[Event(136, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void DCEndCompleteV2()
	{
		throw new NotImplementedException();
	}

	[Event(137, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodDCStartV2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags)
	{
		throw new NotImplementedException();
	}

	[Event(138, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodDCEndV2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags)
	{
		throw new NotImplementedException();
	}

	[Event(139, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodDCStartVerboseV2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, string MethodNamespace, string MethodName, string MethodSignature)
	{
		throw new NotImplementedException();
	}

	[Event(140, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodDCEndVerboseV2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, string MethodNamespace, string MethodName, string MethodSignature)
	{
		throw new NotImplementedException();
	}

	[Event(141, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodLoad_V2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, ushort ClrInstanceID, ulong ReJITID)
	{
		throw new NotImplementedException();
	}

	[Event(159, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)137438953472L)]
	private void R2RGetEntryPoint(ulong MethodID, string MethodNamespace, string MethodName, string MethodSignature, ulong EntryPoint, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(160, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)137438953472L)]
	private void R2RGetEntryPointStart(ulong MethodID, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(142, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodUnload_V2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, ushort ClrInstanceID, ulong ReJITID)
	{
		throw new NotImplementedException();
	}

	[Event(143, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodLoadVerbose_V2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, string MethodNamespace, string MethodName, string MethodSignature, ushort ClrInstanceID, ulong ReJITID)
	{
		throw new NotImplementedException();
	}

	[Event(144, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)48L)]
	private void MethodUnloadVerbose_V2(ulong MethodID, ulong ModuleID, ulong MethodStartAddress, uint MethodSize, uint MethodToken, uint MethodFlags, string MethodNamespace, string MethodName, string MethodSignature, ushort ClrInstanceID, ulong ReJITID)
	{
		throw new NotImplementedException();
	}

	[Event(145, Version = 1, Level = EventLevel.Verbose, Keywords = (EventKeywords)16L)]
	private void MethodJittingStarted_V1(ulong MethodID, ulong ModuleID, uint MethodToken, uint MethodILSize, string MethodNamespace, string MethodName, string MethodSignature, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(146, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)16L)]
	private void MethodJitMemoryAllocatedForCode(ulong MethodID, ulong ModuleID, ulong JitHotCodeRequestSize, ulong JitRODataRequestSize, ulong AllocatedSizeForJitCode, uint JitAllocFlag, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(185, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitInliningSucceeded(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string InlinerNamespace, string InlinerName, string InlinerNameSignature, string InlineeNamespace, string InlineeName, string InlineeNameSignature, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(186, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitInliningFailedAnsi(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string InlinerNamespace, string InlinerName, string InlinerNameSignature, string InlineeNamespace, string InlineeName, string InlineeNameSignature, bool FailAlways)
	{
		throw new NotImplementedException();
	}

	[Event(188, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitTailCallSucceeded(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string CallerNamespace, string CallerName, string CallerNameSignature, string CalleeNamespace, string CalleeName, string CalleeNameSignature, bool TailPrefix, uint TailCallType, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(189, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitTailCallFailedAnsi(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string CallerNamespace, string CallerName, string CallerNameSignature, string CalleeNamespace, string CalleeName, string CalleeNameSignature, bool TailPrefix)
	{
		throw new NotImplementedException();
	}

	[Event(190, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)131072L)]
	private void MethodILToNativeMap(ulong MethodID, ulong ReJITID, byte MethodExtent, ushort CountOfMapEntries)
	{
		throw new NotImplementedException();
	}

	[Event(191, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitTailCallFailed(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string CallerNamespace, string CallerName, string CallerNameSignature, string CalleeNamespace, string CalleeName, string CalleeNameSignature, bool TailPrefix, string FailReason, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(192, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)4096L)]
	private void MethodJitInliningFailed(string MethodBeingCompiledNamespace, string MethodBeingCompiledName, string MethodBeingCompiledNameSignature, string InlinerNamespace, string InlinerName, string InlinerNameSignature, string InlineeNamespace, string InlineeName, string InlineeNameSignature, bool FailAlways, string FailReason, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(149, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void ModuleDCStartV2(ulong ModuleID, ulong AssemblyID, uint ModuleFlags, uint Reserved1, string ModuleILPath, string ModuleNativePath)
	{
		throw new NotImplementedException();
	}

	[Event(150, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void ModuleDCEndV2(ulong ModuleID, ulong AssemblyID, uint ModuleFlags, uint Reserved1, string ModuleILPath, string ModuleNativePath)
	{
		throw new NotImplementedException();
	}

	[Event(151, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void DomainModuleLoad_V1(ulong ModuleID, ulong AssemblyID, ulong AppDomainID, uint ModuleFlags, uint Reserved1, string ModuleILPath, string ModuleNativePath, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(152, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)536870920L)]
	private void ModuleLoad_V2(ulong ModuleID, ulong AssemblyID, uint ModuleFlags, uint Reserved1, string ModuleILPath, string ModuleNativePath, ushort ClrInstanceID, Guid ManagedPdbSignature, uint ManagedPdbAge, string ManagedPdbBuildPath, Guid NativePdbSignature, uint NativePdbAge, string NativePdbBuildPath)
	{
		throw new NotImplementedException();
	}

	[Event(153, Version = 2, Level = EventLevel.Informational, Keywords = (EventKeywords)536870920L)]
	private void ModuleUnload_V2(ulong ModuleID, ulong AssemblyID, uint ModuleFlags, uint Reserved1, string ModuleILPath, string ModuleNativePath, ushort ClrInstanceID, Guid ManagedPdbSignature, uint ManagedPdbAge, string ManagedPdbBuildPath, Guid NativePdbSignature, uint NativePdbAge, string NativePdbBuildPath)
	{
		throw new NotImplementedException();
	}

	[Event(154, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void AssemblyLoad_V1(ulong AssemblyID, ulong AppDomainID, ulong BindingID, uint AssemblyFlags, string FullyQualifiedAssemblyName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(155, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void AssemblyUnload_V1(ulong AssemblyID, ulong AppDomainID, ulong BindingID, uint AssemblyFlags, string FullyQualifiedAssemblyName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(156, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void AppDomainLoad_V1(ulong AppDomainID, uint AppDomainFlags, string AppDomainName, uint AppDomainIndex, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(157, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)8L)]
	private void AppDomainUnload_V1(ulong AppDomainID, uint AppDomainFlags, string AppDomainName, uint AppDomainIndex, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(158, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)536870912L)]
	private void ModuleRangeLoad(ushort ClrInstanceID, ulong ModuleID, uint RangeBegin, uint RangeSize, byte RangeType)
	{
		throw new NotImplementedException();
	}

	[Event(181, Version = 1, Level = EventLevel.Verbose, Keywords = (EventKeywords)1024L)]
	private void StrongNameVerificationStart_V1(uint VerificationFlags, uint ErrorCode, string FullyQualifiedAssemblyName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(182, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1024L)]
	private void StrongNameVerificationStop_V1(uint VerificationFlags, uint ErrorCode, string FullyQualifiedAssemblyName, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(183, Version = 1, Level = EventLevel.Verbose, Keywords = (EventKeywords)1024L)]
	private void AuthenticodeVerificationStart_V1(uint VerificationFlags, uint ErrorCode, string ModulePath, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(184, Version = 1, Level = EventLevel.Informational, Keywords = (EventKeywords)1024L)]
	private void AuthenticodeVerificationStop_V1(uint VerificationFlags, uint ErrorCode, string ModulePath, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(187, Version = 0, Level = EventLevel.Informational)]
	private void RuntimeInformationStart(ushort ClrInstanceID, ushort Sku, ushort BclMajorVersion, ushort BclMinorVersion, ushort BclBuildNumber, ushort BclQfeNumber, ushort VMMajorVersion, ushort VMMinorVersion, ushort VMBuildNumber, ushort VMQfeNumber, uint StartupFlags, byte StartupMode, string CommandLine, Guid ComObjectGuid, string RuntimeDllPath)
	{
		throw new NotImplementedException();
	}

	[Event(200, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void IncreaseMemoryPressure(ulong BytesAllocated, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(201, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void DecreaseMemoryPressure(ulong BytesFreed, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(202, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCMarkWithType(uint HeapNum, ushort ClrInstanceID, uint Type, ulong Bytes)
	{
		throw new NotImplementedException();
	}

	[Event(203, Version = 2, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void GCJoin_V2(uint Heap, uint JoinTime, uint JoinType, ushort ClrInstanceID, uint JoinID)
	{
		throw new NotImplementedException();
	}

	[Event(204, Version = 3, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCPerHeapHistory_V3(ushort ClrInstanceID, IntPtr FreeListAllocated, IntPtr FreeListRejected, IntPtr EndOfSegAllocated, IntPtr CondemnedAllocated, IntPtr PinnedAllocated, IntPtr PinnedAllocatedAdvance, uint RunningFreeListEfficiency, uint CondemnReasons0, uint CondemnReasons1, uint CompactMechanisms, uint ExpandMechanisms, uint HeapIndex, IntPtr ExtraGen0Commit, uint Count)
	{
		throw new NotImplementedException();
	}

	[Event(205, Version = 4, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCGlobalHeapHistory_V4(ulong FinalYoungestDesired, int NumHeaps, uint CondemnedGeneration, uint Gen0ReductionCount, uint Reason, uint GlobalMechanisms, ushort ClrInstanceID, uint PauseMode, uint MemoryPressure, uint CondemnReasons0, uint CondemnReasons1, uint Count)
	{
		throw new NotImplementedException();
	}

	[Event(206, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GenAwareBegin(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(207, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1048576L)]
	private void GenAwareEnd(uint Count, ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(208, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	private void GCLOHCompact(ushort ClrInstanceID, ushort Count)
	{
		throw new NotImplementedException();
	}

	[Event(209, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)1L)]
	private void GCFitBucketInfo(ushort ClrInstanceID, ushort BucketKind, ulong TotalSize, ushort Count)
	{
		throw new NotImplementedException();
	}

	[Event(240, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4294967296L)]
	private void DebugIPCEventStart()
	{
		throw new NotImplementedException();
	}

	[Event(241, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4294967296L)]
	private void DebugIPCEventEnd()
	{
		throw new NotImplementedException();
	}

	[Event(242, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4294967296L)]
	private void DebugExceptionProcessingStart()
	{
		throw new NotImplementedException();
	}

	[Event(243, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4294967296L)]
	private void DebugExceptionProcessingEnd()
	{
		throw new NotImplementedException();
	}

	[Event(260, Version = 0, Level = EventLevel.Verbose, Keywords = (EventKeywords)17179869184L)]
	private void CodeSymbols(ulong ModuleId, ushort TotalChunks, ushort ChunkNumber, uint ChunkLength)
	{
		throw new NotImplementedException();
	}

	[Event(270, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)34359738368L)]
	private void EventSource(int EventID, string EventName, string EventSourceName, string Payload)
	{
		throw new NotImplementedException();
	}

	[Event(280, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)68719476736L)]
	private void TieredCompilationSettings(ushort ClrInstanceID, uint Flags)
	{
		throw new NotImplementedException();
	}

	[Event(281, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)68719476736L)]
	private void TieredCompilationPause(ushort ClrInstanceID)
	{
		throw new NotImplementedException();
	}

	[Event(282, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)68719476736L)]
	private void TieredCompilationResume(ushort ClrInstanceID, uint NewMethodCount)
	{
		throw new NotImplementedException();
	}

	[Event(283, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)68719476736L)]
	private void TieredCompilationBackgroundJitStart(ushort ClrInstanceID, uint PendingMethodCount)
	{
		throw new NotImplementedException();
	}

	[Event(284, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)68719476736L)]
	private void TieredCompilationBackgroundJitStop(ushort ClrInstanceID, uint PendingMethodCount, uint JittedMethodCount)
	{
		throw new NotImplementedException();
	}

	[Event(290, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void AssemblyLoadStart(ushort ClrInstanceID, string AssemblyName, string AssemblyPath, string RequestingAssembly, string AssemblyLoadContext, string RequestingAssemblyLoadContext)
	{
		throw new NotImplementedException();
	}

	[Event(291, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void AssemblyLoadStop(ushort ClrInstanceID, string AssemblyName, string AssemblyPath, string RequestingAssembly, string AssemblyLoadContext, string RequestingAssemblyLoadContext, bool Success, string ResultAssemblyName, string ResultAssemblyPath, bool Cached)
	{
		throw new NotImplementedException();
	}

	[Event(292, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void ResolutionAttempted(ushort ClrInstanceID, string AssemblyName, ushort Stage, string AssemblyLoadContext, ushort Result, string ResultAssemblyName, string ResultAssemblyPath, string ErrorMessage)
	{
		throw new NotImplementedException();
	}

	[Event(293, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void AssemblyLoadContextResolvingHandlerInvoked(ushort ClrInstanceID, string AssemblyName, string HandlerName, string AssemblyLoadContext, string ResultAssemblyName, string ResultAssemblyPath)
	{
		throw new NotImplementedException();
	}

	[Event(294, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void AppDomainAssemblyResolveHandlerInvoked(ushort ClrInstanceID, string AssemblyName, string HandlerName, string ResultAssemblyName, string ResultAssemblyPath)
	{
		throw new NotImplementedException();
	}

	[Event(295, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void AssemblyLoadFromResolveHandlerInvoked(ushort ClrInstanceID, string AssemblyName, bool IsTrackedLoad, string RequestingAssemblyPath, string ComputedRequestedAssemblyPath)
	{
		throw new NotImplementedException();
	}

	[Event(296, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)4L)]
	private void KnownPathProbed(ushort ClrInstanceID, string FilePath, ushort Source, int Result)
	{
		throw new NotImplementedException();
	}

	[Event(297, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1099511627776L)]
	private void JitInstrumentationData(ushort ClrInstanceID, uint MethodFlags, uint DataSize, ulong MethodID)
	{
		throw new NotImplementedException();
	}

	[Event(298, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)1099511627776L)]
	private void JitInstrumentationDataVerbose(ushort ClrInstanceID, uint MethodFlags, uint DataSize, ulong MethodID, ulong ModuleID, uint MethodToken, string MethodNamespace, string MethodName, string MethodSignature)
	{
		throw new NotImplementedException();
	}

	[Event(299, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)2199023255552L)]
	private void ProfilerMessage(ushort ClrInstanceID, string Message)
	{
		throw new NotImplementedException();
	}

	[Event(300, Version = 0, Level = EventLevel.Informational, Keywords = (EventKeywords)536870912L)]
	private void ExecutionCheckpoint(ushort ClrInstanceID, string Name, long Timestamp)
	{
		throw new NotImplementedException();
	}

	private NativeRuntimeEventSource()
		: base(new Guid(3778809123u, 52412, 19986, 147, 27, 217, 204, 46, 238, 39, 228), "Microsoft-Windows-DotNETRuntime")
	{
	}
}
