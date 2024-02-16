using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System;

public static class GC
{
	internal enum GC_ALLOC_FLAGS
	{
		GC_ALLOC_NO_FLAGS = 0,
		GC_ALLOC_ZEROING_OPTIONAL = 0x10,
		GC_ALLOC_PINNED_OBJECT_HEAP = 0x40
	}

	private enum StartNoGCRegionStatus
	{
		Succeeded,
		NotEnoughMemory,
		AmountTooLarge,
		AlreadyInProgress
	}

	private enum EndNoGCRegionStatus
	{
		Succeeded,
		NotInProgress,
		GCInduced,
		AllocationExceeded
	}

	public static int MaxGeneration => GetMaxGeneration();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void GetMemoryInfo(GCMemoryInfoData data, int kind);

	public static GCMemoryInfo GetGCMemoryInfo()
	{
		return GetGCMemoryInfo(GCKind.Any);
	}

	public static GCMemoryInfo GetGCMemoryInfo(GCKind kind)
	{
		if (kind < GCKind.Any || kind > GCKind.Background)
		{
			throw new ArgumentOutOfRangeException("kind", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, GCKind.Any, GCKind.Background));
		}
		GCMemoryInfoData data = new GCMemoryInfoData();
		GetMemoryInfo(data, (int)kind);
		return new GCMemoryInfo(data);
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int _StartNoGCRegion(long totalSize, bool lohSizeKnown, long lohSize, bool disallowFullBlockingGC);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	internal static extern int _EndNoGCRegion();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Array AllocateNewArray(IntPtr typeHandle, int length, GC_ALLOC_FLAGS flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetGenerationWR(IntPtr handle);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern long GetTotalMemory();

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _Collect(int generation, int mode);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int GetMaxGeneration();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _CollectionCount(int generation, int getSpecialGCCount);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern ulong GetSegmentSize();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern int GetLastGCPercentTimeInGC();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern ulong GetGenerationSize(int gen);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _AddMemoryPressure(ulong bytesAllocated);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _RemoveMemoryPressure(ulong bytesAllocated);

	public static void AddMemoryPressure(long bytesAllocated)
	{
		if (bytesAllocated <= 0)
		{
			throw new ArgumentOutOfRangeException("bytesAllocated", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (4 == IntPtr.Size)
		{
		}
		_AddMemoryPressure((ulong)bytesAllocated);
	}

	public static void RemoveMemoryPressure(long bytesAllocated)
	{
		if (bytesAllocated <= 0)
		{
			throw new ArgumentOutOfRangeException("bytesAllocated", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (4 == IntPtr.Size)
		{
		}
		_RemoveMemoryPressure((ulong)bytesAllocated);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern int GetGeneration(object obj);

	public static void Collect(int generation)
	{
		Collect(generation, GCCollectionMode.Default);
	}

	public static void Collect()
	{
		_Collect(-1, 2);
	}

	public static void Collect(int generation, GCCollectionMode mode)
	{
		Collect(generation, mode, blocking: true);
	}

	public static void Collect(int generation, GCCollectionMode mode, bool blocking)
	{
		Collect(generation, mode, blocking, compacting: false);
	}

	public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
	{
		if (generation < 0)
		{
			throw new ArgumentOutOfRangeException("generation", SR.ArgumentOutOfRange_GenericPositive);
		}
		if (mode < GCCollectionMode.Default || mode > GCCollectionMode.Optimized)
		{
			throw new ArgumentOutOfRangeException("mode", SR.ArgumentOutOfRange_Enum);
		}
		int num = 0;
		if (mode == GCCollectionMode.Optimized)
		{
			num |= 4;
		}
		if (compacting)
		{
			num |= 8;
		}
		if (blocking)
		{
			num |= 2;
		}
		else if (!compacting)
		{
			num |= 1;
		}
		_Collect(generation, num);
	}

	public static int CollectionCount(int generation)
	{
		if (generation < 0)
		{
			throw new ArgumentOutOfRangeException("generation", SR.ArgumentOutOfRange_GenericPositive);
		}
		return _CollectionCount(generation, 0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Intrinsic]
	public static void KeepAlive(object? obj)
	{
	}

	public static int GetGeneration(WeakReference wo)
	{
		int generationWR = GetGenerationWR(wo.m_handle);
		KeepAlive(wo);
		return generationWR;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _WaitForPendingFinalizers();

	public static void WaitForPendingFinalizers()
	{
		_WaitForPendingFinalizers();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _SuppressFinalize(object o);

	public static void SuppressFinalize(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		_SuppressFinalize(obj);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void _ReRegisterForFinalize(object o);

	public static void ReRegisterForFinalize(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		_ReRegisterForFinalize(obj);
	}

	public static long GetTotalMemory(bool forceFullCollection)
	{
		long totalMemory = GetTotalMemory();
		if (!forceFullCollection)
		{
			return totalMemory;
		}
		int num = 20;
		long num2 = totalMemory;
		float num3;
		do
		{
			WaitForPendingFinalizers();
			Collect();
			totalMemory = num2;
			num2 = GetTotalMemory();
			num3 = (float)(num2 - totalMemory) / (float)totalMemory;
		}
		while (num-- > 0 && (!(-0.05 < (double)num3) || !((double)num3 < 0.05)));
		return num2;
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern IntPtr _RegisterFrozenSegment(IntPtr sectionAddress, nint sectionSize);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _UnregisterFrozenSegment(IntPtr segmentHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetAllocatedBytesForCurrentThread();

	[MethodImpl(MethodImplOptions.InternalCall)]
	public static extern long GetTotalAllocatedBytes(bool precise = false);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern bool _CancelFullGCNotification();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _WaitForFullGCApproach(int millisecondsTimeout);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern int _WaitForFullGCComplete(int millisecondsTimeout);

	public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
	{
		if (maxGenerationThreshold <= 0 || maxGenerationThreshold >= 100)
		{
			throw new ArgumentOutOfRangeException("maxGenerationThreshold", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
		}
		if (largeObjectHeapThreshold <= 0 || largeObjectHeapThreshold >= 100)
		{
			throw new ArgumentOutOfRangeException("largeObjectHeapThreshold", SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper, 1, 99));
		}
		if (!_RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
		}
	}

	public static void CancelFullGCNotification()
	{
		if (!_CancelFullGCNotification())
		{
			throw new InvalidOperationException(SR.InvalidOperation_NotWithConcurrentGC);
		}
	}

	public static GCNotificationStatus WaitForFullGCApproach()
	{
		return (GCNotificationStatus)_WaitForFullGCApproach(-1);
	}

	public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return (GCNotificationStatus)_WaitForFullGCApproach(millisecondsTimeout);
	}

	public static GCNotificationStatus WaitForFullGCComplete()
	{
		return (GCNotificationStatus)_WaitForFullGCComplete(-1);
	}

	public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
	{
		if (millisecondsTimeout < -1)
		{
			throw new ArgumentOutOfRangeException("millisecondsTimeout", SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
		}
		return (GCNotificationStatus)_WaitForFullGCComplete(millisecondsTimeout);
	}

	private static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
	{
		if (totalSize <= 0)
		{
			throw new ArgumentOutOfRangeException("totalSize", "totalSize can't be zero or negative");
		}
		if (hasLohSize)
		{
			if (lohSize <= 0)
			{
				throw new ArgumentOutOfRangeException("lohSize", "lohSize can't be zero or negative");
			}
			if (lohSize > totalSize)
			{
				throw new ArgumentOutOfRangeException("lohSize", "lohSize can't be greater than totalSize");
			}
		}
		return (StartNoGCRegionStatus)_StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC) switch
		{
			StartNoGCRegionStatus.NotEnoughMemory => false, 
			StartNoGCRegionStatus.AlreadyInProgress => throw new InvalidOperationException("The NoGCRegion mode was already in progress"), 
			StartNoGCRegionStatus.AmountTooLarge => throw new ArgumentOutOfRangeException("totalSize", "totalSize is too large. For more information about setting the maximum size, see \"Latency Modes\" in https://go.microsoft.com/fwlink/?LinkId=522706"), 
			_ => true, 
		};
	}

	public static bool TryStartNoGCRegion(long totalSize)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: false, 0L, disallowFullBlockingGC: false);
	}

	public static bool TryStartNoGCRegion(long totalSize, long lohSize)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: true, lohSize, disallowFullBlockingGC: false);
	}

	public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: false, 0L, disallowFullBlockingGC);
	}

	public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
	{
		return StartNoGCRegionWorker(totalSize, hasLohSize: true, lohSize, disallowFullBlockingGC);
	}

	public static void EndNoGCRegion()
	{
		switch ((EndNoGCRegionStatus)_EndNoGCRegion())
		{
		case EndNoGCRegionStatus.NotInProgress:
			throw new InvalidOperationException("NoGCRegion mode must be set");
		case EndNoGCRegionStatus.GCInduced:
			throw new InvalidOperationException("Garbage collection was induced in NoGCRegion mode");
		case EndNoGCRegionStatus.AllocationExceeded:
			throw new InvalidOperationException("Allocated memory exceeds specified memory for NoGCRegion mode");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] AllocateUninitializedArray<T>(int length, bool pinned = false)
	{
		if (!pinned)
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				return new T[length];
			}
			if (length < 2048 / Unsafe.SizeOf<T>())
			{
				return new T[length];
			}
		}
		else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
		}
		return AllocateNewUninitializedArray(length, pinned);
		static T[] AllocateNewUninitializedArray(int length, bool pinned)
		{
			GC_ALLOC_FLAGS gC_ALLOC_FLAGS = GC_ALLOC_FLAGS.GC_ALLOC_ZEROING_OPTIONAL;
			if (pinned)
			{
				gC_ALLOC_FLAGS |= GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
			}
			return Unsafe.As<T[]>(AllocateNewArray(typeof(T[]).TypeHandle.Value, length, gC_ALLOC_FLAGS));
		}
	}

	public static T[] AllocateArray<T>(int length, bool pinned = false)
	{
		GC_ALLOC_FLAGS flags = GC_ALLOC_FLAGS.GC_ALLOC_NO_FLAGS;
		if (pinned)
		{
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
			{
				ThrowHelper.ThrowInvalidTypeWithPointersNotSupported(typeof(T));
			}
			flags = GC_ALLOC_FLAGS.GC_ALLOC_PINNED_OBJECT_HEAP;
		}
		return Unsafe.As<T[]>(AllocateNewArray(typeof(T[]).TypeHandle.Value, length, flags));
	}
}
