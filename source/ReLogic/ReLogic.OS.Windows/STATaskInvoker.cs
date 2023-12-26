using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ReLogic.OS.Windows;

internal class STATaskInvoker : IDisposable
{
	private static STATaskInvoker Instance;

	private Thread _staThread;

	private volatile bool _shouldThreadContinue;

	private BlockingCollection<Action> _staTasks = new BlockingCollection<Action>();

	private object _taskInvokeLock = new object();

	private object _taskCompletionLock = new object();

	private bool disposedValue;

	private STATaskInvoker()
	{
		_shouldThreadContinue = true;
		_staThread = new Thread(TaskThreadStart);
		_staThread.Name = "STA Invoker Thread";
		_staThread.SetApartmentState(ApartmentState.STA);
		_staThread.Start();
	}

	public static void Invoke(Action action)
	{
		if (Instance == null)
		{
			Instance = new STATaskInvoker();
		}
		Instance.InvokeAndWait(action);
	}

	public static T Invoke<T>(Func<T> action)
	{
		if (Instance == null)
		{
			Instance = new STATaskInvoker();
		}
		T output = default(T);
		Instance.InvokeAndWait(delegate
		{
			output = action();
		});
		return output;
	}

	private void InvokeAndWait(Action action)
	{
		lock (_taskInvokeLock)
		{
			lock (_taskCompletionLock)
			{
				_staTasks.Add(action);
				Monitor.Wait(_taskCompletionLock);
			}
		}
	}

	private void TaskThreadStart()
	{
		while (_shouldThreadContinue)
		{
			Action action = _staTasks.Take();
			lock (_taskCompletionLock)
			{
				action();
				Monitor.Pulse(_taskCompletionLock);
			}
		}
	}

	private void Shutdown()
	{
		InvokeAndWait(delegate
		{
			_shouldThreadContinue = false;
		});
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposedValue)
		{
			return;
		}
		if (disposing)
		{
			Shutdown();
			if (_staTasks != null)
			{
				_staTasks.Dispose();
				_staTasks = null;
			}
		}
		disposedValue = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
