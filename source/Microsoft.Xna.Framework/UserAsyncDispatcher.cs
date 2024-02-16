using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Xna.Framework;

internal static class UserAsyncDispatcher
{
	private static SafeWaitHandle parentExitEvent;

	internal static readonly object PendingOperationsLock = new object();

	private static Dictionary<uint, PendingOperation> pendingOperations = new Dictionary<uint, PendingOperation>();

	private static bool initialized = false;

	private unsafe static void PlatformSpecificInitialize()
	{
		UnsafeNativeMethods.SecurityAttributes lpEventAttributes = new UnsafeNativeMethods.SecurityAttributes(inheritHandle: true);
		parentExitEvent = new SafeWaitHandle(UnsafeNativeMethods.CreateEvent(ref lpEventAttributes, bManualReset: true, bInitialState: false, IntPtr.Zero), ownsHandle: true);
		if (parentExitEvent.IsInvalid)
		{
			throw new WaitHandleCannotBeOpenedException();
		}
		AppDomain.CurrentDomain.ProcessExit += OnExit;
		AppDomain.CurrentDomain.DomainUnload += OnExit;
		KernelReturnCode result = (KernelReturnCode)WindowsManagedCallbackHandler.Initialize(parentExitEvent.DangerousGetHandle().ToPointer());
		Helpers.ThrowExceptionFromResult((uint)result);
	}

	private static void OnExit(object sender, EventArgs e)
	{
		if (!parentExitEvent.IsInvalid)
		{
			UnsafeNativeMethods.SetEvent(parentExitEvent.DangerousGetHandle());
		}
	}

	private static KernelReturnCode WaitForAsyncOperationToFinish(out ManagedCallType managedCallType, out uint managedCallArgs)
	{
		return (KernelReturnCode)WindowsManagedCallbackHandler.Instance.WaitForAsyncOperationToFinish(out managedCallType, out managedCallArgs);
	}

	internal static void Initialize()
	{
		if (!initialized)
		{
			PlatformSpecificInitialize();
			Thread thread = new Thread(AsyncDispatcherThreadFunction);
			thread.IsBackground = true;
			thread.Start();
			initialized = true;
		}
	}

	private static void AsyncDispatcherThreadFunction()
	{
		while (true)
		{
			ManagedCallType managedCallType;
			uint managedCallArgs;
			KernelReturnCode kernelReturnCode = WaitForAsyncOperationToFinish(out managedCallType, out managedCallArgs);
			if (kernelReturnCode == KernelReturnCode.AsyncShutdown)
			{
				break;
			}
			Helpers.ThrowExceptionFromResult((uint)kernelReturnCode);
			if (managedCallType == ManagedCallType.AsyncOperationCompleted)
			{
				HandleFinishedOperation(managedCallArgs);
			}
			else
			{
				HandleManagedCallback(managedCallType, managedCallArgs);
			}
		}
	}

	private static void HandleManagedCallback(ManagedCallType managedCallType, uint managedCallArgs)
	{
		if (managedCallType != ManagedCallType.NoManagedCall && CallbackGoesToDispatcher(managedCallType))
		{
			FrameworkDispatcher.AddNewPendingCall(managedCallType, managedCallArgs);
		}
	}

	private static bool CallbackGoesToDispatcher(ManagedCallType managedCallType)
	{
		if (managedCallType != ManagedCallType.Media_ActiveSongChanged && managedCallType != ManagedCallType.Media_PlayStateChanged && managedCallType != ManagedCallType.CaptureBufferReady)
		{
			return managedCallType == ManagedCallType.PlaybackBufferNeeded;
		}
		return true;
	}

	private static void HandleFinishedOperation(uint finishedHandle)
	{
		PendingOperation pendingOp;
		lock (PendingOperationsLock)
		{
			pendingOp = pendingOperations[finishedHandle];
			if (!pendingOp.Async.IsReusable)
			{
				pendingOperations.Remove(finishedHandle);
				pendingOp.Async.IsCompleted = true;
			}
		}
		pendingOp.Async.AsyncWaitHandle.Set();
		if (pendingOp.Callback != null)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				pendingOp.Callback(pendingOp.Async);
			});
		}
	}

	internal static IAsyncResult AddPendingOperation(uint kernelHandle, AsyncCallback callback, object asyncState)
	{
		return AddPendingOperation(kernelHandle, callback, asyncState, isReusable: false, null);
	}

	internal static IAsyncResult AddPendingOperation(uint kernelHandle, AsyncCallback callback, object asyncState, bool isReusable, AsyncOperationCleanup operationCleanup)
	{
		XOverlappedAsyncResult xOverlappedAsyncResult = new XOverlappedAsyncResult(asyncState, kernelHandle, isReusable, operationCleanup);
		pendingOperations.Add(kernelHandle, new PendingOperation(xOverlappedAsyncResult, callback));
		return xOverlappedAsyncResult;
	}

	internal static bool OperationStillPending(XOverlappedAsyncResult result)
	{
		lock (PendingOperationsLock)
		{
			return pendingOperations.ContainsKey(result.KernelHandle);
		}
	}
}
