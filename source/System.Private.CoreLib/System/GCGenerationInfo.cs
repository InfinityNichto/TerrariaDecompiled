namespace System;

public readonly struct GCGenerationInfo
{
	public long SizeBeforeBytes { get; }

	public long FragmentationBeforeBytes { get; }

	public long SizeAfterBytes { get; }

	public long FragmentationAfterBytes { get; }
}
