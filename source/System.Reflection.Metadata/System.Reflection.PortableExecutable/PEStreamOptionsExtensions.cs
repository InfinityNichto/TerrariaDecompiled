namespace System.Reflection.PortableExecutable;

internal static class PEStreamOptionsExtensions
{
	public static bool IsValid(this PEStreamOptions options)
	{
		return (options & ~(PEStreamOptions.LeaveOpen | PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage | PEStreamOptions.IsLoadedImage)) == 0;
	}
}
