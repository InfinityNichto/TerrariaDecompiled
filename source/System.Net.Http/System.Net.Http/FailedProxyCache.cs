using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Http;

internal sealed class FailedProxyCache
{
	private readonly ConcurrentDictionary<Uri, long> _failedProxies = new ConcurrentDictionary<Uri, long>();

	private long _nextFlushTicks = Environment.TickCount64 + 300000;

	private SpinLock _flushLock = new SpinLock(enableThreadOwnerTracking: false);

	public long GetProxyRenewTicks(Uri uri)
	{
		Cleanup();
		if (!_failedProxies.TryGetValue(uri, out var value))
		{
			return 0L;
		}
		if (Environment.TickCount64 < value)
		{
			return value;
		}
		if (TryRenewProxy(uri, value))
		{
			return 0L;
		}
		if (!_failedProxies.TryGetValue(uri, out value))
		{
			return 0L;
		}
		return value;
	}

	public void SetProxyFailed(Uri uri)
	{
		_failedProxies[uri] = Environment.TickCount64 + 1800000;
		Cleanup();
	}

	public bool TryRenewProxy(Uri uri, long renewTicks)
	{
		return _failedProxies.TryRemove(new KeyValuePair<Uri, long>(uri, renewTicks));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Cleanup()
	{
		if (_failedProxies.Count > 8 && Environment.TickCount64 >= Interlocked.Read(ref _nextFlushTicks))
		{
			CleanupHelper();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void CleanupHelper()
	{
		bool lockTaken = false;
		try
		{
			_flushLock.TryEnter(ref lockTaken);
			if (!lockTaken)
			{
				return;
			}
			long tickCount = Environment.TickCount64;
			foreach (KeyValuePair<Uri, long> failedProxy in _failedProxies)
			{
				if (tickCount >= failedProxy.Value)
				{
					((ICollection<KeyValuePair<Uri, long>>)_failedProxies).Remove(failedProxy);
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Interlocked.Exchange(ref _nextFlushTicks, Environment.TickCount64 + 300000);
				_flushLock.Exit(useMemoryBarrier: false);
			}
		}
	}
}
