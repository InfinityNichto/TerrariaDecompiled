namespace System;

public readonly struct GCMemoryInfo
{
	private readonly GCMemoryInfoData _data;

	public long HighMemoryLoadThresholdBytes => _data._highMemoryLoadThresholdBytes;

	public long MemoryLoadBytes => _data._memoryLoadBytes;

	public long TotalAvailableMemoryBytes => _data._totalAvailableMemoryBytes;

	public long HeapSizeBytes => _data._heapSizeBytes;

	public long FragmentedBytes => _data._fragmentedBytes;

	public long Index => _data._index;

	public int Generation => _data._generation;

	public bool Compacted => _data._compacted;

	public bool Concurrent => _data._concurrent;

	public long TotalCommittedBytes => _data._totalCommittedBytes;

	public long PromotedBytes => _data._promotedBytes;

	public long PinnedObjectsCount => _data._pinnedObjectsCount;

	public long FinalizationPendingCount => _data._finalizationPendingCount;

	public ReadOnlySpan<TimeSpan> PauseDurations => _data.PauseDurationsAsSpan;

	public double PauseTimePercentage => (double)_data._pauseTimePercentage / 100.0;

	public ReadOnlySpan<GCGenerationInfo> GenerationInfo => _data.GenerationInfoAsSpan;

	internal GCMemoryInfo(GCMemoryInfoData data)
	{
		_data = data;
	}
}
