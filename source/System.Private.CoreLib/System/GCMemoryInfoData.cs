using System.Runtime.InteropServices;

namespace System;

[StructLayout(LayoutKind.Sequential)]
internal sealed class GCMemoryInfoData
{
	internal long _highMemoryLoadThresholdBytes;

	internal long _totalAvailableMemoryBytes;

	internal long _memoryLoadBytes;

	internal long _heapSizeBytes;

	internal long _fragmentedBytes;

	internal long _totalCommittedBytes;

	internal long _promotedBytes;

	internal long _pinnedObjectsCount;

	internal long _finalizationPendingCount;

	internal long _index;

	internal int _generation;

	internal int _pauseTimePercentage;

	internal bool _compacted;

	internal bool _concurrent;

	private GCGenerationInfo _generationInfo0;

	private GCGenerationInfo _generationInfo1;

	private GCGenerationInfo _generationInfo2;

	private GCGenerationInfo _generationInfo3;

	private GCGenerationInfo _generationInfo4;

	private TimeSpan _pauseDuration0;

	private TimeSpan _pauseDuration1;

	internal ReadOnlySpan<GCGenerationInfo> GenerationInfoAsSpan => MemoryMarshal.CreateReadOnlySpan(ref _generationInfo0, 5);

	internal ReadOnlySpan<TimeSpan> PauseDurationsAsSpan => MemoryMarshal.CreateReadOnlySpan(ref _pauseDuration0, 2);
}
