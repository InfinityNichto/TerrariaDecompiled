using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks;

internal static class TaskToApm
{
	internal sealed class TaskAsyncResult : IAsyncResult
	{
		internal readonly Task _task;

		private readonly AsyncCallback _callback;

		public object AsyncState { get; }

		public bool CompletedSynchronously { get; }

		public bool IsCompleted => _task.IsCompleted;

		public WaitHandle AsyncWaitHandle => ((IAsyncResult)_task).AsyncWaitHandle;

		internal TaskAsyncResult(Task task, object state, AsyncCallback callback)
		{
			_task = task;
			AsyncState = state;
			if (task.IsCompleted)
			{
				CompletedSynchronously = true;
				callback?.Invoke(this);
			}
			else if (callback != null)
			{
				_callback = callback;
				_task.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().OnCompleted(InvokeCallback);
			}
		}

		private void InvokeCallback()
		{
			_callback(this);
		}
	}

	public static IAsyncResult Begin(Task task, AsyncCallback callback, object state)
	{
		return new TaskAsyncResult(task, state, callback);
	}

	public static TResult End<TResult>(IAsyncResult asyncResult)
	{
		if (GetTask(asyncResult) is Task<TResult> task)
		{
			return task.GetAwaiter().GetResult();
		}
		ThrowArgumentException(asyncResult);
		return default(TResult);
	}

	public static Task GetTask(IAsyncResult asyncResult)
	{
		return (asyncResult as TaskAsyncResult)?._task;
	}

	[DoesNotReturn]
	private static void ThrowArgumentException(IAsyncResult asyncResult)
	{
		throw (asyncResult == null) ? new ArgumentNullException("asyncResult") : new ArgumentException(null, "asyncResult");
	}
}
