using System.Threading.Tasks;

namespace System.Xml;

internal static class AsyncHelper
{
	public static readonly Task<bool> DoneTaskTrue = Task.FromResult(result: true);

	public static readonly Task<bool> DoneTaskFalse = Task.FromResult(result: false);

	public static readonly Task<int> DoneTaskZero = Task.FromResult(0);

	public static bool IsSuccess(this Task task)
	{
		return task.IsCompletedSuccessfully;
	}

	public static Task CallVoidFuncWhenFinishAsync<TArg>(this Task task, Action<TArg> func, TArg arg)
	{
		if (task.IsSuccess())
		{
			func(arg);
			return Task.CompletedTask;
		}
		return task.CallVoidFuncWhenFinishCoreAsync(func, arg);
	}

	private static async Task CallVoidFuncWhenFinishCoreAsync<TArg>(this Task task, Action<TArg> func, TArg arg)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		func(arg);
	}

	public static Task<bool> ReturnTrueTaskWhenFinishAsync(this Task task)
	{
		if (!task.IsSuccess())
		{
			return task.ReturnTrueTaskWhenFinishCoreAsync();
		}
		return DoneTaskTrue;
	}

	private static async Task<bool> ReturnTrueTaskWhenFinishCoreAsync(this Task task)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		return true;
	}

	public static Task CallTaskFuncWhenFinishAsync<TArg>(this Task task, Func<TArg, Task> func, TArg arg)
	{
		if (!task.IsSuccess())
		{
			return CallTaskFuncWhenFinishCoreAsync(task, func, arg);
		}
		return func(arg);
	}

	private static async Task CallTaskFuncWhenFinishCoreAsync<TArg>(Task task, Func<TArg, Task> func, TArg arg)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		await func(arg).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static Task<bool> CallBoolTaskFuncWhenFinishAsync<TArg>(this Task task, Func<TArg, Task<bool>> func, TArg arg)
	{
		if (!task.IsSuccess())
		{
			return task.CallBoolTaskFuncWhenFinishCoreAsync(func, arg);
		}
		return func(arg);
	}

	private static async Task<bool> CallBoolTaskFuncWhenFinishCoreAsync<TArg>(this Task task, Func<TArg, Task<bool>> func, TArg arg)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		return await func(arg).ConfigureAwait(continueOnCapturedContext: false);
	}

	public static Task<bool> ContinueBoolTaskFuncWhenFalseAsync<TArg>(this Task<bool> task, Func<TArg, Task<bool>> func, TArg arg)
	{
		if (task.IsSuccess())
		{
			if (!task.Result)
			{
				return func(arg);
			}
			return DoneTaskTrue;
		}
		return ContinueBoolTaskFuncWhenFalseCoreAsync(task, func, arg);
	}

	private static async Task<bool> ContinueBoolTaskFuncWhenFalseCoreAsync<TArg>(Task<bool> task, Func<TArg, Task<bool>> func, TArg arg)
	{
		if (await task.ConfigureAwait(continueOnCapturedContext: false))
		{
			return true;
		}
		return await func(arg).ConfigureAwait(continueOnCapturedContext: false);
	}
}
