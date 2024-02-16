using System.Threading;

namespace System.Transactions;

internal sealed class CheapUnfairReaderWriterLock
{
	private object _writerFinishedEvent;

	private int _readersIn;

	private int _readersOut;

	private bool _writerPresent;

	private object _syncRoot;

	private object SyncRoot
	{
		get
		{
			if (_syncRoot == null)
			{
				Interlocked.CompareExchange(ref _syncRoot, new object(), null);
			}
			return _syncRoot;
		}
	}

	private bool ReadersPresent => _readersIn != _readersOut;

	private ManualResetEvent WriterFinishedEvent
	{
		get
		{
			if (_writerFinishedEvent == null)
			{
				Interlocked.CompareExchange(ref _writerFinishedEvent, new ManualResetEvent(initialState: true), null);
			}
			return (ManualResetEvent)_writerFinishedEvent;
		}
	}

	public int EnterReadLock()
	{
		int num = 0;
		while (true)
		{
			if (_writerPresent)
			{
				WriterFinishedEvent.WaitOne();
			}
			num = Interlocked.Increment(ref _readersIn);
			if (!_writerPresent)
			{
				break;
			}
			Interlocked.Decrement(ref _readersIn);
		}
		return num;
	}

	public void EnterWriteLock()
	{
		Monitor.Enter(SyncRoot);
		_writerPresent = true;
		WriterFinishedEvent.Reset();
		do
		{
			int num = 0;
			while (ReadersPresent && num < 100)
			{
				Thread.Sleep(0);
				num++;
			}
			if (ReadersPresent)
			{
				Thread.Sleep(500);
			}
		}
		while (ReadersPresent);
	}

	public void ExitReadLock()
	{
		Interlocked.Increment(ref _readersOut);
	}

	public void ExitWriteLock()
	{
		try
		{
			_writerPresent = false;
			WriterFinishedEvent.Set();
		}
		finally
		{
			Monitor.Exit(SyncRoot);
		}
	}
}
