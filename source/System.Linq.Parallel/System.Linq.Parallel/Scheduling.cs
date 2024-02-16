namespace System.Linq.Parallel;

internal static class Scheduling
{
	internal static int DefaultDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 512);

	internal static int GetDefaultDegreeOfParallelism()
	{
		return DefaultDegreeOfParallelism;
	}

	internal static int GetDefaultChunkSize<T>()
	{
		if (typeof(T).IsValueType)
		{
			return 128;
		}
		return 512 / IntPtr.Size;
	}
}
