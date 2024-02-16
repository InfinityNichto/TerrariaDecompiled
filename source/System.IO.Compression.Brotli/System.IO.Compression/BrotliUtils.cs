namespace System.IO.Compression;

internal static class BrotliUtils
{
	internal static int GetQualityFromCompressionLevel(CompressionLevel level)
	{
		return level switch
		{
			CompressionLevel.Optimal => 11, 
			CompressionLevel.NoCompression => 0, 
			CompressionLevel.Fastest => 1, 
			CompressionLevel.SmallestSize => 11, 
			_ => (int)level, 
		};
	}
}
