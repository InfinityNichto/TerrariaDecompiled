using System.Reflection;
using System.Runtime;
using System.Threading;

namespace System.Diagnostics.Tracing;

[EventSource(Guid = "49592C0F-5A05-516D-AA4B-A64E02026C89", Name = "System.Runtime")]
internal sealed class RuntimeEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords AppContext = (EventKeywords)1L;
	}

	private enum EventId
	{
		AppContextSwitch = 1
	}

	internal const string EventSourceName = "System.Runtime";

	private static RuntimeEventSource s_RuntimeEventSource;

	private PollingCounter _gcHeapSizeCounter;

	private IncrementingPollingCounter _gen0GCCounter;

	private IncrementingPollingCounter _gen1GCCounter;

	private IncrementingPollingCounter _gen2GCCounter;

	private PollingCounter _cpuTimeCounter;

	private PollingCounter _workingSetCounter;

	private PollingCounter _threadPoolThreadCounter;

	private IncrementingPollingCounter _monitorContentionCounter;

	private PollingCounter _threadPoolQueueCounter;

	private IncrementingPollingCounter _completedItemsCounter;

	private IncrementingPollingCounter _allocRateCounter;

	private PollingCounter _timerCounter;

	private PollingCounter _fragmentationCounter;

	private PollingCounter _committedCounter;

	private IncrementingPollingCounter _exceptionCounter;

	private PollingCounter _gcTimeCounter;

	private PollingCounter _gen0SizeCounter;

	private PollingCounter _gen1SizeCounter;

	private PollingCounter _gen2SizeCounter;

	private PollingCounter _lohSizeCounter;

	private PollingCounter _pohSizeCounter;

	private PollingCounter _assemblyCounter;

	private PollingCounter _ilBytesJittedCounter;

	private PollingCounter _methodsJittedCounter;

	private IncrementingPollingCounter _jitTimeCounter;

	private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[17]
	{
		17, 0, 83, 121, 115, 116, 101, 109, 46, 82,
		117, 110, 116, 105, 109, 101, 0
	};

	public static void Initialize()
	{
		s_RuntimeEventSource = new RuntimeEventSource();
	}

	private RuntimeEventSource(int _)
	{
	}

	[Event(1, Level = EventLevel.Informational, Keywords = (EventKeywords)1L)]
	internal void LogAppContextSwitch(string switchName, int value)
	{
		WriteEvent(1, switchName, value);
	}

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_cpuTimeCounter == null)
		{
			_cpuTimeCounter = new PollingCounter("cpu-usage", this, () => RuntimeEventSourceHelper.GetCpuUsage())
			{
				DisplayName = "CPU Usage",
				DisplayUnits = "%"
			};
		}
		if (_workingSetCounter == null)
		{
			_workingSetCounter = new PollingCounter("working-set", this, () => Environment.WorkingSet / 1000000)
			{
				DisplayName = "Working Set",
				DisplayUnits = "MB"
			};
		}
		if (_gcHeapSizeCounter == null)
		{
			_gcHeapSizeCounter = new PollingCounter("gc-heap-size", this, () => GC.GetTotalMemory(forceFullCollection: false) / 1000000)
			{
				DisplayName = "GC Heap Size",
				DisplayUnits = "MB"
			};
		}
		if (_gen0GCCounter == null)
		{
			_gen0GCCounter = new IncrementingPollingCounter("gen-0-gc-count", this, () => GC.CollectionCount(0))
			{
				DisplayName = "Gen 0 GC Count",
				DisplayRateTimeScale = new TimeSpan(0, 1, 0)
			};
		}
		if (_gen1GCCounter == null)
		{
			_gen1GCCounter = new IncrementingPollingCounter("gen-1-gc-count", this, () => GC.CollectionCount(1))
			{
				DisplayName = "Gen 1 GC Count",
				DisplayRateTimeScale = new TimeSpan(0, 1, 0)
			};
		}
		if (_gen2GCCounter == null)
		{
			_gen2GCCounter = new IncrementingPollingCounter("gen-2-gc-count", this, () => GC.CollectionCount(2))
			{
				DisplayName = "Gen 2 GC Count",
				DisplayRateTimeScale = new TimeSpan(0, 1, 0)
			};
		}
		if (_threadPoolThreadCounter == null)
		{
			_threadPoolThreadCounter = new PollingCounter("threadpool-thread-count", this, () => ThreadPool.ThreadCount)
			{
				DisplayName = "ThreadPool Thread Count"
			};
		}
		if (_monitorContentionCounter == null)
		{
			_monitorContentionCounter = new IncrementingPollingCounter("monitor-lock-contention-count", this, () => Monitor.LockContentionCount)
			{
				DisplayName = "Monitor Lock Contention Count",
				DisplayRateTimeScale = new TimeSpan(0, 0, 1)
			};
		}
		if (_threadPoolQueueCounter == null)
		{
			_threadPoolQueueCounter = new PollingCounter("threadpool-queue-length", this, () => ThreadPool.PendingWorkItemCount)
			{
				DisplayName = "ThreadPool Queue Length"
			};
		}
		if (_completedItemsCounter == null)
		{
			_completedItemsCounter = new IncrementingPollingCounter("threadpool-completed-items-count", this, () => ThreadPool.CompletedWorkItemCount)
			{
				DisplayName = "ThreadPool Completed Work Item Count",
				DisplayRateTimeScale = new TimeSpan(0, 0, 1)
			};
		}
		if (_allocRateCounter == null)
		{
			_allocRateCounter = new IncrementingPollingCounter("alloc-rate", this, () => GC.GetTotalAllocatedBytes())
			{
				DisplayName = "Allocation Rate",
				DisplayUnits = "B",
				DisplayRateTimeScale = new TimeSpan(0, 0, 1)
			};
		}
		if (_timerCounter == null)
		{
			_timerCounter = new PollingCounter("active-timer-count", this, () => Timer.ActiveCount)
			{
				DisplayName = "Number of Active Timers"
			};
		}
		if (_fragmentationCounter == null)
		{
			_fragmentationCounter = new PollingCounter("gc-fragmentation", this, delegate
			{
				GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
				return (gCMemoryInfo.HeapSizeBytes == 0L) ? 0.0 : ((double)gCMemoryInfo.FragmentedBytes * 100.0 / (double)gCMemoryInfo.HeapSizeBytes);
			})
			{
				DisplayName = "GC Fragmentation",
				DisplayUnits = "%"
			};
		}
		if (_committedCounter == null)
		{
			_committedCounter = new PollingCounter("gc-committed", this, () => GC.GetGCMemoryInfo().TotalCommittedBytes / 1000000)
			{
				DisplayName = "GC Committed Bytes",
				DisplayUnits = "MB"
			};
		}
		if (_exceptionCounter == null)
		{
			_exceptionCounter = new IncrementingPollingCounter("exception-count", this, () => Exception.GetExceptionCount())
			{
				DisplayName = "Exception Count",
				DisplayRateTimeScale = new TimeSpan(0, 0, 1)
			};
		}
		if (_gcTimeCounter == null)
		{
			_gcTimeCounter = new PollingCounter("time-in-gc", this, () => GC.GetLastGCPercentTimeInGC())
			{
				DisplayName = "% Time in GC since last GC",
				DisplayUnits = "%"
			};
		}
		if (_gen0SizeCounter == null)
		{
			_gen0SizeCounter = new PollingCounter("gen-0-size", this, () => GC.GetGenerationSize(0))
			{
				DisplayName = "Gen 0 Size",
				DisplayUnits = "B"
			};
		}
		if (_gen1SizeCounter == null)
		{
			_gen1SizeCounter = new PollingCounter("gen-1-size", this, () => GC.GetGenerationSize(1))
			{
				DisplayName = "Gen 1 Size",
				DisplayUnits = "B"
			};
		}
		if (_gen2SizeCounter == null)
		{
			_gen2SizeCounter = new PollingCounter("gen-2-size", this, () => GC.GetGenerationSize(2))
			{
				DisplayName = "Gen 2 Size",
				DisplayUnits = "B"
			};
		}
		if (_lohSizeCounter == null)
		{
			_lohSizeCounter = new PollingCounter("loh-size", this, () => GC.GetGenerationSize(3))
			{
				DisplayName = "LOH Size",
				DisplayUnits = "B"
			};
		}
		if (_pohSizeCounter == null)
		{
			_pohSizeCounter = new PollingCounter("poh-size", this, () => GC.GetGenerationSize(4))
			{
				DisplayName = "POH (Pinned Object Heap) Size",
				DisplayUnits = "B"
			};
		}
		if (_assemblyCounter == null)
		{
			_assemblyCounter = new PollingCounter("assembly-count", this, () => Assembly.GetAssemblyCount())
			{
				DisplayName = "Number of Assemblies Loaded"
			};
		}
		if (_ilBytesJittedCounter == null)
		{
			_ilBytesJittedCounter = new PollingCounter("il-bytes-jitted", this, () => JitInfo.GetCompiledILBytes())
			{
				DisplayName = "IL Bytes Jitted",
				DisplayUnits = "B"
			};
		}
		if (_methodsJittedCounter == null)
		{
			_methodsJittedCounter = new PollingCounter("methods-jitted-count", this, () => JitInfo.GetCompiledMethodCount())
			{
				DisplayName = "Number of Methods Jitted"
			};
		}
		if (_jitTimeCounter == null)
		{
			_jitTimeCounter = new IncrementingPollingCounter("time-in-jit", this, () => JitInfo.GetCompilationTime().TotalMilliseconds)
			{
				DisplayName = "Time spent in JIT",
				DisplayUnits = "ms",
				DisplayRateTimeScale = new TimeSpan(0, 0, 1)
			};
		}
		AppContext.LogSwitchValues(this);
	}

	private RuntimeEventSource()
		: base(new Guid(1230580751, 23045, 20845, 170, 75, 166, 78, 2, 2, 108, 137), "System.Runtime")
	{
	}
}
