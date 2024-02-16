namespace System.Reflection.Metadata;

internal static class MetadataStreamOptionsExtensions
{
	public static bool IsValid(this MetadataStreamOptions options)
	{
		return (options & ~(MetadataStreamOptions.LeaveOpen | MetadataStreamOptions.PrefetchMetadata)) == 0;
	}
}
