using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Security.Authentication;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace System.Net.Security;

[EventSource(Name = "System.Net.Security")]
internal sealed class NetSecurityTelemetry : EventSource
{
	public static readonly NetSecurityTelemetry Log = new NetSecurityTelemetry();

	private IncrementingPollingCounter _tlsHandshakeRateCounter;

	private PollingCounter _totalTlsHandshakesCounter;

	private PollingCounter _currentTlsHandshakesCounter;

	private PollingCounter _failedTlsHandshakesCounter;

	private PollingCounter _sessionsOpenCounter;

	private PollingCounter _sessionsOpenTls10Counter;

	private PollingCounter _sessionsOpenTls11Counter;

	private PollingCounter _sessionsOpenTls12Counter;

	private PollingCounter _sessionsOpenTls13Counter;

	private EventCounter _handshakeDurationCounter;

	private EventCounter _handshakeDurationTls10Counter;

	private EventCounter _handshakeDurationTls11Counter;

	private EventCounter _handshakeDurationTls12Counter;

	private EventCounter _handshakeDurationTls13Counter;

	private long _finishedTlsHandshakes;

	private long _startedTlsHandshakes;

	private long _failedTlsHandshakes;

	private long _sessionsOpen;

	private long _sessionsOpenTls10;

	private long _sessionsOpenTls11;

	private long _sessionsOpenTls12;

	private long _sessionsOpenTls13;

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_tlsHandshakeRateCounter == null)
		{
			_tlsHandshakeRateCounter = new IncrementingPollingCounter("tls-handshake-rate", this, () => Interlocked.Read(ref _finishedTlsHandshakes))
			{
				DisplayName = "TLS handshakes completed",
				DisplayRateTimeScale = TimeSpan.FromSeconds(1.0)
			};
		}
		if (_totalTlsHandshakesCounter == null)
		{
			_totalTlsHandshakesCounter = new PollingCounter("total-tls-handshakes", this, () => Interlocked.Read(ref _finishedTlsHandshakes))
			{
				DisplayName = "Total TLS handshakes completed"
			};
		}
		if (_currentTlsHandshakesCounter == null)
		{
			_currentTlsHandshakesCounter = new PollingCounter("current-tls-handshakes", this, () => -Interlocked.Read(ref _finishedTlsHandshakes) + Interlocked.Read(ref _startedTlsHandshakes))
			{
				DisplayName = "Current TLS handshakes"
			};
		}
		if (_failedTlsHandshakesCounter == null)
		{
			_failedTlsHandshakesCounter = new PollingCounter("failed-tls-handshakes", this, () => Interlocked.Read(ref _failedTlsHandshakes))
			{
				DisplayName = "Total TLS handshakes failed"
			};
		}
		if (_sessionsOpenCounter == null)
		{
			_sessionsOpenCounter = new PollingCounter("all-tls-sessions-open", this, () => Interlocked.Read(ref _sessionsOpen))
			{
				DisplayName = "All TLS Sessions Active"
			};
		}
		if (_sessionsOpenTls10Counter == null)
		{
			_sessionsOpenTls10Counter = new PollingCounter("tls10-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls10))
			{
				DisplayName = "TLS 1.0 Sessions Active"
			};
		}
		if (_sessionsOpenTls11Counter == null)
		{
			_sessionsOpenTls11Counter = new PollingCounter("tls11-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls11))
			{
				DisplayName = "TLS 1.1 Sessions Active"
			};
		}
		if (_sessionsOpenTls12Counter == null)
		{
			_sessionsOpenTls12Counter = new PollingCounter("tls12-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls12))
			{
				DisplayName = "TLS 1.2 Sessions Active"
			};
		}
		if (_sessionsOpenTls13Counter == null)
		{
			_sessionsOpenTls13Counter = new PollingCounter("tls13-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls13))
			{
				DisplayName = "TLS 1.3 Sessions Active"
			};
		}
		if (_handshakeDurationCounter == null)
		{
			_handshakeDurationCounter = new EventCounter("all-tls-handshake-duration", this)
			{
				DisplayName = "TLS Handshake Duration",
				DisplayUnits = "ms"
			};
		}
		if (_handshakeDurationTls10Counter == null)
		{
			_handshakeDurationTls10Counter = new EventCounter("tls10-handshake-duration", this)
			{
				DisplayName = "TLS 1.0 Handshake Duration",
				DisplayUnits = "ms"
			};
		}
		if (_handshakeDurationTls11Counter == null)
		{
			_handshakeDurationTls11Counter = new EventCounter("tls11-handshake-duration", this)
			{
				DisplayName = "TLS 1.1 Handshake Duration",
				DisplayUnits = "ms"
			};
		}
		if (_handshakeDurationTls12Counter == null)
		{
			_handshakeDurationTls12Counter = new EventCounter("tls12-handshake-duration", this)
			{
				DisplayName = "TLS 1.2 Handshake Duration",
				DisplayUnits = "ms"
			};
		}
		if (_handshakeDurationTls13Counter == null)
		{
			_handshakeDurationTls13Counter = new EventCounter("tls13-handshake-duration", this)
			{
				DisplayName = "TLS 1.3 Handshake Duration",
				DisplayUnits = "ms"
			};
		}
	}

	[Event(1, Level = EventLevel.Informational)]
	public void HandshakeStart(bool isServer, string targetHost)
	{
		Interlocked.Increment(ref _startedTlsHandshakes);
		if (IsEnabled(EventLevel.Informational, EventKeywords.None))
		{
			WriteEvent(1, isServer, targetHost);
		}
	}

	[Event(2, Level = EventLevel.Informational)]
	private void HandshakeStop(SslProtocols protocol)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.None))
		{
			WriteEvent(2, protocol);
		}
	}

	[Event(3, Level = EventLevel.Error)]
	private void HandshakeFailed(bool isServer, double elapsedMilliseconds, string exceptionMessage)
	{
		WriteEvent(3, isServer, elapsedMilliseconds, exceptionMessage);
	}

	[NonEvent]
	public void HandshakeFailed(bool isServer, ValueStopwatch stopwatch, string exceptionMessage)
	{
		Interlocked.Increment(ref _finishedTlsHandshakes);
		Interlocked.Increment(ref _failedTlsHandshakes);
		if (IsEnabled(EventLevel.Error, EventKeywords.None))
		{
			HandshakeFailed(isServer, stopwatch.GetElapsedTime().TotalMilliseconds, exceptionMessage);
		}
		HandshakeStop(SslProtocols.None);
	}

	[NonEvent]
	public void HandshakeCompleted(SslProtocols protocol, ValueStopwatch stopwatch, bool connectionOpen)
	{
		Interlocked.Increment(ref _finishedTlsHandshakes);
		long num = 0L;
		ref long location = ref num;
		EventCounter eventCounter = null;
		switch (protocol)
		{
		case SslProtocols.Tls:
			location = ref _sessionsOpenTls10;
			eventCounter = _handshakeDurationTls10Counter;
			break;
		case SslProtocols.Tls11:
			location = ref _sessionsOpenTls11;
			eventCounter = _handshakeDurationTls11Counter;
			break;
		case SslProtocols.Tls12:
			location = ref _sessionsOpenTls12;
			eventCounter = _handshakeDurationTls12Counter;
			break;
		case SslProtocols.Tls13:
			location = ref _sessionsOpenTls13;
			eventCounter = _handshakeDurationTls13Counter;
			break;
		}
		if (connectionOpen)
		{
			Interlocked.Increment(ref location);
			Interlocked.Increment(ref _sessionsOpen);
		}
		double totalMilliseconds = stopwatch.GetElapsedTime().TotalMilliseconds;
		eventCounter?.WriteMetric(totalMilliseconds);
		_handshakeDurationCounter.WriteMetric(totalMilliseconds);
		HandshakeStop(protocol);
	}

	[NonEvent]
	public void ConnectionClosed(SslProtocols protocol)
	{
		long num = 0L;
		switch (protocol)
		{
		case SslProtocols.Tls:
			num = Interlocked.Decrement(ref _sessionsOpenTls10);
			break;
		case SslProtocols.Tls11:
			num = Interlocked.Decrement(ref _sessionsOpenTls11);
			break;
		case SslProtocols.Tls12:
			num = Interlocked.Decrement(ref _sessionsOpenTls12);
			break;
		case SslProtocols.Tls13:
			num = Interlocked.Decrement(ref _sessionsOpenTls13);
			break;
		}
		num = Interlocked.Decrement(ref _sessionsOpen);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, bool arg1, string arg2)
	{
		if (IsEnabled())
		{
			if (arg2 == null)
			{
				arg2 = string.Empty;
			}
			fixed (char* ptr2 = arg2)
			{
				EventData* ptr = stackalloc EventData[2];
				*ptr = new EventData
				{
					DataPointer = (IntPtr)(&arg1),
					Size = 4
				};
				ptr[1] = new EventData
				{
					DataPointer = (IntPtr)ptr2,
					Size = (arg2.Length + 1) * 2
				};
				WriteEventCore(eventId, 2, ptr);
			}
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, SslProtocols arg1)
	{
		if (IsEnabled())
		{
			EventData eventData = default(EventData);
			eventData.DataPointer = (IntPtr)(&arg1);
			eventData.Size = 4;
			EventData eventData2 = eventData;
			WriteEventCore(eventId, 1, &eventData2);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
	[NonEvent]
	private unsafe void WriteEvent(int eventId, bool arg1, double arg2, string arg3)
	{
		if (IsEnabled())
		{
			if (arg3 == null)
			{
				arg3 = string.Empty;
			}
			fixed (char* ptr2 = arg3)
			{
				EventData* ptr = stackalloc EventData[3];
				*ptr = new EventData
				{
					DataPointer = (IntPtr)(&arg1),
					Size = 4
				};
				ptr[1] = new EventData
				{
					DataPointer = (IntPtr)(&arg2),
					Size = 8
				};
				ptr[2] = new EventData
				{
					DataPointer = (IntPtr)ptr2,
					Size = (arg3.Length + 1) * 2
				};
				WriteEventCore(eventId, 3, ptr);
			}
		}
	}
}
