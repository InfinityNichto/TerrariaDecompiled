using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Tracing;

[UnsupportedOSPlatform("browser")]
internal sealed class CounterGroup
{
	private readonly EventSource _eventSource;

	private readonly List<DiagnosticCounter> _counters;

	private static readonly object s_counterGroupLock = new object();

	private static WeakReference<CounterGroup>[] s_counterGroups;

	private DateTime _timeStampSinceCollectionStarted;

	private int _pollingIntervalInMilliseconds;

	private DateTime _nextPollingTimeStamp;

	private static Thread s_pollingThread;

	private static AutoResetEvent s_pollingThreadSleepEvent;

	private static List<CounterGroup> s_counterGroupEnabledList;

	internal CounterGroup(EventSource eventSource)
	{
		_eventSource = eventSource;
		_counters = new List<DiagnosticCounter>();
		RegisterCommandCallback();
	}

	internal void Add(DiagnosticCounter eventCounter)
	{
		lock (s_counterGroupLock)
		{
			_counters.Add(eventCounter);
		}
	}

	internal void Remove(DiagnosticCounter eventCounter)
	{
		lock (s_counterGroupLock)
		{
			_counters.Remove(eventCounter);
		}
	}

	private void RegisterCommandCallback()
	{
		_eventSource.EventCommandExecuted += OnEventSourceCommand;
	}

	private void OnEventSourceCommand(object sender, EventCommandEventArgs e)
	{
		if (e.Command == EventCommand.Enable || e.Command == EventCommand.Update)
		{
			if (e.Arguments.TryGetValue("EventCounterIntervalSec", out string value) && float.TryParse(value, out var result))
			{
				lock (s_counterGroupLock)
				{
					EnableTimer(result);
				}
			}
		}
		else if (e.Command == EventCommand.Disable)
		{
			lock (s_counterGroupLock)
			{
				DisableTimer();
			}
		}
	}

	private static void EnsureEventSourceIndexAvailable(int eventSourceIndex)
	{
		if (s_counterGroups == null)
		{
			s_counterGroups = new WeakReference<CounterGroup>[eventSourceIndex + 1];
		}
		else if (eventSourceIndex >= s_counterGroups.Length)
		{
			WeakReference<CounterGroup>[] destinationArray = new WeakReference<CounterGroup>[eventSourceIndex + 1];
			Array.Copy(s_counterGroups, destinationArray, s_counterGroups.Length);
			s_counterGroups = destinationArray;
		}
	}

	internal static CounterGroup GetCounterGroup(EventSource eventSource)
	{
		lock (s_counterGroupLock)
		{
			int num = EventListener.EventSourceIndex(eventSource);
			EnsureEventSourceIndexAvailable(num);
			WeakReference<CounterGroup> weakReference = s_counterGroups[num];
			if (weakReference == null || !weakReference.TryGetTarget(out var target))
			{
				target = new CounterGroup(eventSource);
				s_counterGroups[num] = new WeakReference<CounterGroup>(target);
			}
			return target;
		}
	}

	private void EnableTimer(float pollingIntervalInSeconds)
	{
		if (pollingIntervalInSeconds <= 0f)
		{
			DisableTimer();
		}
		else if (_pollingIntervalInMilliseconds == 0 || pollingIntervalInSeconds * 1000f < (float)_pollingIntervalInMilliseconds)
		{
			_pollingIntervalInMilliseconds = (int)(pollingIntervalInSeconds * 1000f);
			ResetCounters();
			_timeStampSinceCollectionStarted = DateTime.UtcNow;
			_nextPollingTimeStamp = DateTime.UtcNow + new TimeSpan(0, 0, (int)pollingIntervalInSeconds);
			if (s_pollingThread == null)
			{
				s_pollingThreadSleepEvent = new AutoResetEvent(initialState: false);
				s_counterGroupEnabledList = new List<CounterGroup>();
				s_pollingThread = new Thread(PollForValues)
				{
					IsBackground = true,
					Name = ".NET Counter Poller"
				};
				s_pollingThread.UnsafeStart();
			}
			if (!s_counterGroupEnabledList.Contains(this))
			{
				s_counterGroupEnabledList.Add(this);
			}
			s_pollingThreadSleepEvent.Set();
		}
	}

	private void DisableTimer()
	{
		_pollingIntervalInMilliseconds = 0;
		s_counterGroupEnabledList?.Remove(this);
	}

	private void ResetCounters()
	{
		lock (s_counterGroupLock)
		{
			foreach (DiagnosticCounter counter in _counters)
			{
				if (counter is IncrementingEventCounter incrementingEventCounter)
				{
					incrementingEventCounter.UpdateMetric();
				}
				else if (counter is IncrementingPollingCounter incrementingPollingCounter)
				{
					incrementingPollingCounter.UpdateMetric();
				}
				else if (counter is EventCounter eventCounter)
				{
					eventCounter.ResetStatistics();
				}
			}
		}
	}

	private void OnTimer()
	{
		if (!_eventSource.IsEnabled())
		{
			return;
		}
		DateTime utcNow;
		TimeSpan timeSpan;
		int pollingIntervalInMilliseconds;
		DiagnosticCounter[] array;
		lock (s_counterGroupLock)
		{
			utcNow = DateTime.UtcNow;
			timeSpan = utcNow - _timeStampSinceCollectionStarted;
			pollingIntervalInMilliseconds = _pollingIntervalInMilliseconds;
			array = new DiagnosticCounter[_counters.Count];
			_counters.CopyTo(array);
		}
		DiagnosticCounter[] array2 = array;
		foreach (DiagnosticCounter diagnosticCounter in array2)
		{
			diagnosticCounter.WritePayload((float)timeSpan.TotalSeconds, pollingIntervalInMilliseconds);
		}
		lock (s_counterGroupLock)
		{
			_timeStampSinceCollectionStarted = utcNow;
			TimeSpan timeSpan2 = utcNow - _nextPollingTimeStamp;
			timeSpan2 = (((double)_pollingIntervalInMilliseconds > timeSpan2.TotalMilliseconds) ? TimeSpan.FromMilliseconds(_pollingIntervalInMilliseconds) : timeSpan2);
			if (_pollingIntervalInMilliseconds > 0)
			{
				_nextPollingTimeStamp += TimeSpan.FromMilliseconds((double)_pollingIntervalInMilliseconds * Math.Ceiling(timeSpan2.TotalMilliseconds / (double)_pollingIntervalInMilliseconds));
			}
		}
	}

	private static void PollForValues()
	{
		AutoResetEvent autoResetEvent = null;
		List<CounterGroup> list = new List<CounterGroup>();
		while (true)
		{
			int num = int.MaxValue;
			lock (s_counterGroupLock)
			{
				autoResetEvent = s_pollingThreadSleepEvent;
				foreach (CounterGroup s_counterGroupEnabled in s_counterGroupEnabledList)
				{
					DateTime utcNow = DateTime.UtcNow;
					if (s_counterGroupEnabled._nextPollingTimeStamp < utcNow + new TimeSpan(0, 0, 0, 0, 1))
					{
						list.Add(s_counterGroupEnabled);
					}
					int val = (int)(s_counterGroupEnabled._nextPollingTimeStamp - utcNow).TotalMilliseconds;
					val = Math.Max(1, val);
					num = Math.Min(num, val);
				}
			}
			foreach (CounterGroup item in list)
			{
				item.OnTimer();
			}
			list.Clear();
			if (num == int.MaxValue)
			{
				num = -1;
			}
			autoResetEvent?.WaitOne(num);
		}
	}
}
