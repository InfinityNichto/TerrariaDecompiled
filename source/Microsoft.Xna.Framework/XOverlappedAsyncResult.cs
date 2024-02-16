using System;
using System.Threading;

namespace Microsoft.Xna.Framework;

internal class XOverlappedAsyncResult : IAsyncResult
{
	private object asyncState;

	private EventWaitHandle asyncWaitHandle;

	private bool isCompleted;

	private uint kernelHandle;

	private bool endHasBeenCalled;

	private bool isReusable;

	private AsyncOperationCleanup kernelHandleCleanup;

	object IAsyncResult.AsyncState => asyncState;

	WaitHandle IAsyncResult.AsyncWaitHandle => asyncWaitHandle;

	bool IAsyncResult.CompletedSynchronously => false;

	bool IAsyncResult.IsCompleted => isCompleted;

	internal EventWaitHandle AsyncWaitHandle => asyncWaitHandle;

	internal bool IsCompleted
	{
		set
		{
			isCompleted = value;
		}
	}

	internal uint KernelHandle => kernelHandle;

	internal bool IsReusable => isReusable;

	internal XOverlappedAsyncResult(object asyncState, uint kernelHandle, bool isReusable, AsyncOperationCleanup kernelHandleCleanup)
	{
		this.asyncState = asyncState;
		this.kernelHandle = kernelHandle;
		this.isReusable = isReusable;
		this.kernelHandleCleanup = kernelHandleCleanup;
		if (isReusable)
		{
			asyncWaitHandle = new AutoResetEvent(initialState: false);
		}
		else
		{
			asyncWaitHandle = new ManualResetEvent(initialState: false);
		}
	}

	~XOverlappedAsyncResult()
	{
		if (kernelHandleCleanup != null && !UserAsyncDispatcher.OperationStillPending(this))
		{
			kernelHandleCleanup(kernelHandle);
		}
	}

	internal static XOverlappedAsyncResult PrepareForEndFunction(IAsyncResult result)
	{
		if (result == null)
		{
			throw new ArgumentNullException("result");
		}
		if (!(result is XOverlappedAsyncResult xOverlappedAsyncResult))
		{
			throw new ArgumentException(FrameworkResources.IAsyncNotFromBegin);
		}
		if (xOverlappedAsyncResult.endHasBeenCalled)
		{
			throw new InvalidOperationException(FrameworkResources.CannotEndTwice);
		}
		xOverlappedAsyncResult.endHasBeenCalled = true;
		xOverlappedAsyncResult.AsyncWaitHandle.WaitOne();
		GC.SuppressFinalize(xOverlappedAsyncResult);
		return xOverlappedAsyncResult;
	}
}
