using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace ReLogic.Threading;

public class AsyncActionDispatcher : IDisposable
{
	private Thread _actionThread;

	private readonly BlockingCollection<Action> _actionQueue = new BlockingCollection<Action>();

	private readonly CancellationTokenSource _threadCancellation = new CancellationTokenSource();

	private volatile bool _isRunning;

	public int ActionsRemaining => _actionQueue.Count;

	public bool IsDisposed { get; private set; }

	public bool IsRunning => _isRunning;

	public void Queue(Action action)
	{
		_actionQueue.Add(action);
	}

	public void Start()
	{
		if (IsRunning)
		{
			throw new InvalidOperationException("AsyncActionDispatcher is already started.");
		}
		_isRunning = true;
		_actionThread = new Thread(LoaderThreadStart)
		{
			IsBackground = true,
			Name = "AsyncActionDispatcher Thread"
		};
		_actionThread.Start();
	}

	public void Stop()
	{
		if (!IsRunning)
		{
			throw new InvalidOperationException("AsyncActionDispatcher is already stopped.");
		}
		_isRunning = false;
		_threadCancellation.Cancel();
		_actionThread.Join();
	}

	[DebuggerNonUserCode]
	private void LoaderThreadStart()
	{
		while (_isRunning)
		{
			try
			{
				_actionQueue.Take(_threadCancellation.Token)();
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}
		if (disposing)
		{
			if (IsRunning)
			{
				Stop();
			}
			_actionQueue.Dispose();
			_threadCancellation.Dispose();
		}
		IsDisposed = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}
}
