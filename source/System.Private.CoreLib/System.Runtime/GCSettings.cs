using System.Runtime.CompilerServices;

namespace System.Runtime;

public static class GCSettings
{
	private enum SetLatencyModeStatus
	{
		Succeeded,
		NoGCInProgress
	}

	public static extern bool IsServerGC
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		get;
	}

	public static GCLatencyMode LatencyMode
	{
		get
		{
			return GetGCLatencyMode();
		}
		set
		{
			if (value < GCLatencyMode.Batch || value > GCLatencyMode.SustainedLowLatency)
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			SetLatencyModeStatus setLatencyModeStatus = SetGCLatencyMode(value);
			if (setLatencyModeStatus == SetLatencyModeStatus.NoGCInProgress)
			{
				throw new InvalidOperationException(SR.InvalidOperation_SetLatencyModeNoGC);
			}
		}
	}

	public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
	{
		get
		{
			return GetLOHCompactionMode();
		}
		set
		{
			if (value < GCLargeObjectHeapCompactionMode.Default || value > GCLargeObjectHeapCompactionMode.CompactOnce)
			{
				ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
			}
			SetLOHCompactionMode(value);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern GCLatencyMode GetGCLatencyMode();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern SetLatencyModeStatus SetGCLatencyMode(GCLatencyMode newLatencyMode);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern GCLargeObjectHeapCompactionMode GetLOHCompactionMode();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void SetLOHCompactionMode(GCLargeObjectHeapCompactionMode newLOHCompactionMode);
}
