using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Http;

[EventSource(Name = "System.Net.Http")]
internal sealed class HttpTelemetry : EventSource
{
	public static readonly HttpTelemetry Log = new HttpTelemetry();

	private long _startedRequests;

	private long _stoppedRequests;

	private long _failedRequests;

	private long _openedHttp11Connections;

	private long _openedHttp20Connections;

	private IncrementingPollingCounter _startedRequestsPerSecondCounter;

	private IncrementingPollingCounter _failedRequestsPerSecondCounter;

	private PollingCounter _startedRequestsCounter;

	private PollingCounter _currentRequestsCounter;

	private PollingCounter _failedRequestsCounter;

	private PollingCounter _totalHttp11ConnectionsCounter;

	private PollingCounter _totalHttp20ConnectionsCounter;

	private EventCounter _http11RequestsQueueDurationCounter;

	private EventCounter _http20RequestsQueueDurationCounter;

	[Event(1, Level = EventLevel.Informational)]
	private void RequestStart(string scheme, string host, int port, string pathAndQuery, byte versionMajor, byte versionMinor, HttpVersionPolicy versionPolicy)
	{
		Interlocked.Increment(ref _startedRequests);
		WriteEvent(1, scheme, host, port, pathAndQuery, versionMajor, versionMinor, versionPolicy);
	}

	[NonEvent]
	public void RequestStart(HttpRequestMessage request)
	{
		RequestStart(request.RequestUri.Scheme, request.RequestUri.IdnHost, request.RequestUri.Port, request.RequestUri.PathAndQuery, (byte)request.Version.Major, (byte)request.Version.Minor, request.VersionPolicy);
	}

	[Event(2, Level = EventLevel.Informational)]
	public void RequestStop()
	{
		Interlocked.Increment(ref _stoppedRequests);
		WriteEvent(2);
	}

	[Event(3, Level = EventLevel.Error)]
	public void RequestFailed()
	{
		Interlocked.Increment(ref _failedRequests);
		WriteEvent(3);
	}

	[Event(4, Level = EventLevel.Informational)]
	private void ConnectionEstablished(byte versionMajor, byte versionMinor)
	{
		WriteEvent(4, versionMajor, versionMinor);
	}

	[Event(5, Level = EventLevel.Informational)]
	private void ConnectionClosed(byte versionMajor, byte versionMinor)
	{
		WriteEvent(5, versionMajor, versionMinor);
	}

	[Event(6, Level = EventLevel.Informational)]
	private void RequestLeftQueue(double timeOnQueueMilliseconds, byte versionMajor, byte versionMinor)
	{
		WriteEvent(6, timeOnQueueMilliseconds, versionMajor, versionMinor);
	}

	[Event(7, Level = EventLevel.Informational)]
	public void RequestHeadersStart()
	{
		WriteEvent(7);
	}

	[Event(8, Level = EventLevel.Informational)]
	public void RequestHeadersStop()
	{
		WriteEvent(8);
	}

	[Event(9, Level = EventLevel.Informational)]
	public void RequestContentStart()
	{
		WriteEvent(9);
	}

	[Event(10, Level = EventLevel.Informational)]
	public void RequestContentStop(long contentLength)
	{
		WriteEvent(10, contentLength);
	}

	[Event(11, Level = EventLevel.Informational)]
	public void ResponseHeadersStart()
	{
		WriteEvent(11);
	}

	[Event(12, Level = EventLevel.Informational)]
	public void ResponseHeadersStop()
	{
		WriteEvent(12);
	}

	[Event(13, Level = EventLevel.Informational)]
	public void ResponseContentStart()
	{
		WriteEvent(13);
	}

	[Event(14, Level = EventLevel.Informational)]
	public void ResponseContentStop()
	{
		WriteEvent(14);
	}

	[NonEvent]
	public void Http11ConnectionEstablished()
	{
		Interlocked.Increment(ref _openedHttp11Connections);
		ConnectionEstablished(1, 1);
	}

	[NonEvent]
	public void Http11ConnectionClosed()
	{
		long num = Interlocked.Decrement(ref _openedHttp11Connections);
		ConnectionClosed(1, 1);
	}

	[NonEvent]
	public void Http20ConnectionEstablished()
	{
		Interlocked.Increment(ref _openedHttp20Connections);
		ConnectionEstablished(2, 0);
	}

	[NonEvent]
	public void Http20ConnectionClosed()
	{
		long num = Interlocked.Decrement(ref _openedHttp20Connections);
		ConnectionClosed(2, 0);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, string arg1, string arg2, int arg3, string arg4, byte arg5, byte arg6, HttpVersionPolicy arg7)
	{
		//The blocks IL_004b, IL_004e, IL_0060, IL_01e3 are reachable both inside and outside the pinned region starting at IL_0048. ILSpy has duplicated these blocks in order to place them both within and outside the `fixed` statement.
		if (!IsEnabled())
		{
			return;
		}
		if (arg1 == null)
		{
			arg1 = "";
		}
		if (arg2 == null)
		{
			arg2 = "";
		}
		if (arg4 == null)
		{
			arg4 = "";
		}
		fixed (char* ptr5 = arg1)
		{
			char* intPtr;
			EventData* intPtr2;
			nint num;
			nint num2;
			nint num3;
			nint num4;
			nint num5;
			nint num6;
			if (arg2 == null)
			{
				char* ptr;
				intPtr = (ptr = null);
				fixed (char* ptr2 = arg4)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[7];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&arg3),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr4 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (IntPtr)(&arg5),
						Size = 1
					};
					num5 = (nint)(ptr4 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (IntPtr)(&arg6),
						Size = 1
					};
					num6 = (nint)(ptr4 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (IntPtr)(&arg7),
						Size = 4
					};
					WriteEventCore(eventId, 7, ptr4);
				}
				return;
			}
			fixed (char* ptr6 = &arg2.GetPinnableReference())
			{
				char* ptr;
				intPtr = (ptr = ptr6);
				fixed (char* ptr2 = arg4)
				{
					char* ptr3 = ptr2;
					EventData* ptr4 = stackalloc EventData[7];
					intPtr2 = ptr4;
					*intPtr2 = new EventData
					{
						DataPointer = (IntPtr)ptr5,
						Size = (arg1.Length + 1) * 2
					};
					num = (nint)(ptr4 + 1);
					*(EventData*)num = new EventData
					{
						DataPointer = (IntPtr)ptr,
						Size = (arg2.Length + 1) * 2
					};
					num2 = (nint)(ptr4 + 2);
					*(EventData*)num2 = new EventData
					{
						DataPointer = (IntPtr)(&arg3),
						Size = 4
					};
					num3 = (nint)(ptr4 + 3);
					*(EventData*)num3 = new EventData
					{
						DataPointer = (IntPtr)ptr3,
						Size = (arg4.Length + 1) * 2
					};
					num4 = (nint)(ptr4 + 4);
					*(EventData*)num4 = new EventData
					{
						DataPointer = (IntPtr)(&arg5),
						Size = 1
					};
					num5 = (nint)(ptr4 + 5);
					*(EventData*)num5 = new EventData
					{
						DataPointer = (IntPtr)(&arg6),
						Size = 1
					};
					num6 = (nint)(ptr4 + 6);
					*(EventData*)num6 = new EventData
					{
						DataPointer = (IntPtr)(&arg7),
						Size = 4
					};
					WriteEventCore(eventId, 7, ptr4);
				}
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, double arg1, byte arg2, byte arg3)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[3];
			*ptr = new EventData
			{
				DataPointer = (IntPtr)(&arg1),
				Size = 8
			};
			ptr[1] = new EventData
			{
				DataPointer = (IntPtr)(&arg2),
				Size = 1
			};
			ptr[2] = new EventData
			{
				DataPointer = (IntPtr)(&arg3),
				Size = 1
			};
			WriteEventCore(eventId, 3, ptr);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, byte arg1, byte arg2)
	{
		if (IsEnabled())
		{
			EventData* ptr = stackalloc EventData[2];
			*ptr = new EventData
			{
				DataPointer = (IntPtr)(&arg1),
				Size = 1
			};
			ptr[1] = new EventData
			{
				DataPointer = (IntPtr)(&arg2),
				Size = 1
			};
			WriteEventCore(eventId, 2, ptr);
		}
	}

	[NonEvent]
	public void Http11RequestLeftQueue(double timeOnQueueMilliseconds)
	{
		_http11RequestsQueueDurationCounter.WriteMetric(timeOnQueueMilliseconds);
		RequestLeftQueue(timeOnQueueMilliseconds, 1, 1);
	}

	[NonEvent]
	public void Http20RequestLeftQueue(double timeOnQueueMilliseconds)
	{
		_http20RequestsQueueDurationCounter.WriteMetric(timeOnQueueMilliseconds);
		RequestLeftQueue(timeOnQueueMilliseconds, 2, 0);
	}

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_startedRequestsCounter == null)
		{
			_startedRequestsCounter = new PollingCounter("requests-started", this, () => Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Requests Started"
			};
		}
		if (_startedRequestsPerSecondCounter == null)
		{
			_startedRequestsPerSecondCounter = new IncrementingPollingCounter("requests-started-rate", this, () => Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Requests Started Rate",
				DisplayRateTimeScale = TimeSpan.FromSeconds(1.0)
			};
		}
		if (_failedRequestsCounter == null)
		{
			_failedRequestsCounter = new PollingCounter("requests-failed", this, () => Interlocked.Read(ref _failedRequests))
			{
				DisplayName = "Requests Failed"
			};
		}
		if (_failedRequestsPerSecondCounter == null)
		{
			_failedRequestsPerSecondCounter = new IncrementingPollingCounter("requests-failed-rate", this, () => Interlocked.Read(ref _failedRequests))
			{
				DisplayName = "Requests Failed Rate",
				DisplayRateTimeScale = TimeSpan.FromSeconds(1.0)
			};
		}
		if (_currentRequestsCounter == null)
		{
			_currentRequestsCounter = new PollingCounter("current-requests", this, () => -Interlocked.Read(ref _stoppedRequests) + Interlocked.Read(ref _startedRequests))
			{
				DisplayName = "Current Requests"
			};
		}
		if (_totalHttp11ConnectionsCounter == null)
		{
			_totalHttp11ConnectionsCounter = new PollingCounter("http11-connections-current-total", this, () => Interlocked.Read(ref _openedHttp11Connections))
			{
				DisplayName = "Current Http 1.1 Connections"
			};
		}
		if (_totalHttp20ConnectionsCounter == null)
		{
			_totalHttp20ConnectionsCounter = new PollingCounter("http20-connections-current-total", this, () => Interlocked.Read(ref _openedHttp20Connections))
			{
				DisplayName = "Current Http 2.0 Connections"
			};
		}
		if (_http11RequestsQueueDurationCounter == null)
		{
			_http11RequestsQueueDurationCounter = new EventCounter("http11-requests-queue-duration", this)
			{
				DisplayName = "HTTP 1.1 Requests Queue Duration",
				DisplayUnits = "ms"
			};
		}
		if (_http20RequestsQueueDurationCounter == null)
		{
			_http20RequestsQueueDurationCounter = new EventCounter("http20-requests-queue-duration", this)
			{
				DisplayName = "HTTP 2.0 Requests Queue Duration",
				DisplayUnits = "ms"
			};
		}
	}
}
