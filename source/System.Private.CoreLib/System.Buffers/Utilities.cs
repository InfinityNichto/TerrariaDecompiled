using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers;

internal static class Utilities
{
	internal enum MemoryPressure
	{
		Low,
		Medium,
		High
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int SelectBucketIndex(int bufferSize)
	{
		return BitOperations.Log2((uint)(bufferSize - 1) | 0xFu) - 3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int GetMaxSizeForBucket(int binIndex)
	{
		return 16 << binIndex;
	}

	internal static MemoryPressure GetMemoryPressure()
	{
		GCMemoryInfo gCMemoryInfo = GC.GetGCMemoryInfo();
		if ((double)gCMemoryInfo.MemoryLoadBytes >= (double)gCMemoryInfo.HighMemoryLoadThresholdBytes * 0.9)
		{
			return MemoryPressure.High;
		}
		if ((double)gCMemoryInfo.MemoryLoadBytes >= (double)gCMemoryInfo.HighMemoryLoadThresholdBytes * 0.7)
		{
			return MemoryPressure.Medium;
		}
		return MemoryPressure.Low;
	}
}
