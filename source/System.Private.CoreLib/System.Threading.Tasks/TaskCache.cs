namespace System.Threading.Tasks;

internal static class TaskCache
{
	internal static readonly Task<bool> s_trueTask = CreateCacheableTask(result: true);

	internal static readonly Task<bool> s_falseTask = CreateCacheableTask(result: false);

	internal static readonly Task<int>[] s_int32Tasks = CreateInt32Tasks();

	internal static Task<TResult> CreateCacheableTask<TResult>(TResult result)
	{
		return new Task<TResult>(canceled: false, result, (TaskCreationOptions)16384, default(CancellationToken));
	}

	private static Task<int>[] CreateInt32Tasks()
	{
		Task<int>[] array = new Task<int>[10];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = CreateCacheableTask(i + -1);
		}
		return array;
	}
}
