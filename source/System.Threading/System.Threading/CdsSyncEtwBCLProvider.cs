using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Threading;

[EventSource(Name = "System.Threading.SynchronizationEventSource", Guid = "EC631D38-466B-4290-9306-834971BA0217")]
internal sealed class CdsSyncEtwBCLProvider : EventSource
{
	public static CdsSyncEtwBCLProvider Log = new CdsSyncEtwBCLProvider();

	private CdsSyncEtwBCLProvider()
	{
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[Event(3, Level = EventLevel.Verbose, Version = 1)]
	public unsafe void Barrier_PhaseFinished(bool currentSense, long phaseNum)
	{
		if (IsEnabled(EventLevel.Verbose, EventKeywords.All))
		{
			EventData* ptr = stackalloc EventData[2];
			int num = (currentSense ? 1 : 0);
			*ptr = new EventData
			{
				Size = 4,
				DataPointer = (IntPtr)(&num)
			};
			ptr[1] = new EventData
			{
				Size = 8,
				DataPointer = (IntPtr)(&phaseNum)
			};
			WriteEventCore(3, 2, ptr);
		}
	}
}
