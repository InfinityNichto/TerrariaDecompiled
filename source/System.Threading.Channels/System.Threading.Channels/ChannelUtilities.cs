using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Channels;

internal static class ChannelUtilities
{
	internal static readonly Exception s_doneWritingSentinel = new Exception("s_doneWritingSentinel");

	internal static readonly Task<bool> s_trueTask = Task.FromResult(result: true);

	internal static readonly Task<bool> s_falseTask = Task.FromResult(result: false);

	internal static readonly Task s_neverCompletingTask = new TaskCompletionSource<bool>().Task;

	internal static void Complete(TaskCompletionSource tcs, Exception error = null)
	{
		if (error is OperationCanceledException ex)
		{
			tcs.TrySetCanceled(ex.CancellationToken);
		}
		else if (error != null && error != s_doneWritingSentinel)
		{
			tcs.TrySetException(error);
		}
		else
		{
			tcs.TrySetResult();
		}
	}

	internal static ValueTask<T> GetInvalidCompletionValueTask<T>(Exception error)
	{
		Task<T> task = ((error == s_doneWritingSentinel) ? Task.FromException<T>(CreateInvalidCompletionException()) : ((error is OperationCanceledException { CancellationToken: var cancellationToken } ex) ? Task.FromCanceled<T>(cancellationToken.IsCancellationRequested ? ex.CancellationToken : new CancellationToken(canceled: true)) : Task.FromException<T>(CreateInvalidCompletionException(error))));
		return new ValueTask<T>(task);
	}

	internal static void QueueWaiter(ref AsyncOperation<bool> tail, AsyncOperation<bool> waiter)
	{
		AsyncOperation<bool> asyncOperation = tail;
		if (asyncOperation == null)
		{
			waiter.Next = waiter;
		}
		else
		{
			waiter.Next = asyncOperation.Next;
			asyncOperation.Next = waiter;
		}
		tail = waiter;
	}

	internal static void WakeUpWaiters(ref AsyncOperation<bool> listTail, bool result, Exception error = null)
	{
		AsyncOperation<bool> asyncOperation = listTail;
		if (asyncOperation != null)
		{
			listTail = null;
			AsyncOperation<bool> next = asyncOperation.Next;
			AsyncOperation<bool> asyncOperation2 = next;
			do
			{
				AsyncOperation<bool> next2 = asyncOperation2.Next;
				asyncOperation2.Next = null;
				bool flag = ((error != null) ? asyncOperation2.TrySetException(error) : asyncOperation2.TrySetResult(result));
				asyncOperation2 = next2;
			}
			while (asyncOperation2 != next);
		}
	}

	internal static void FailOperations<T, TInner>(Deque<T> operations, Exception error) where T : AsyncOperation<TInner>
	{
		while (!operations.IsEmpty)
		{
			operations.DequeueHead().TrySetException(error);
		}
	}

	internal static Exception CreateInvalidCompletionException(Exception inner = null)
	{
		if (!(inner is OperationCanceledException))
		{
			if (inner == null || inner == s_doneWritingSentinel)
			{
				return new ChannelClosedException();
			}
			return new ChannelClosedException(inner);
		}
		return inner;
	}
}
