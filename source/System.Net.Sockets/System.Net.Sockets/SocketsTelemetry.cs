using System.Diagnostics.Tracing;
using System.Net.Internals;
using System.Threading;

namespace System.Net.Sockets;

[EventSource(Name = "System.Net.Sockets")]
internal sealed class SocketsTelemetry : EventSource
{
	public static readonly SocketsTelemetry Log = new SocketsTelemetry();

	private PollingCounter _outgoingConnectionsEstablishedCounter;

	private PollingCounter _incomingConnectionsEstablishedCounter;

	private PollingCounter _bytesReceivedCounter;

	private PollingCounter _bytesSentCounter;

	private PollingCounter _datagramsReceivedCounter;

	private PollingCounter _datagramsSentCounter;

	private long _outgoingConnectionsEstablished;

	private long _incomingConnectionsEstablished;

	private long _bytesReceived;

	private long _bytesSent;

	private long _datagramsReceived;

	private long _datagramsSent;

	[Event(1, Level = EventLevel.Informational)]
	private void ConnectStart(string address)
	{
		WriteEvent(1, address);
	}

	[Event(2, Level = EventLevel.Informational)]
	private void ConnectStop()
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			WriteEvent(2);
		}
	}

	[Event(3, Level = EventLevel.Error)]
	private void ConnectFailed(SocketError error, string exceptionMessage)
	{
		if (IsEnabled(EventLevel.Error, EventKeywords.All))
		{
			WriteEvent(3, (int)error, exceptionMessage);
		}
	}

	[Event(4, Level = EventLevel.Informational)]
	private void AcceptStart(string address)
	{
		WriteEvent(4, address);
	}

	[Event(5, Level = EventLevel.Informational)]
	private void AcceptStop()
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			WriteEvent(5);
		}
	}

	[Event(6, Level = EventLevel.Error)]
	private void AcceptFailed(SocketError error, string exceptionMessage)
	{
		if (IsEnabled(EventLevel.Error, EventKeywords.All))
		{
			WriteEvent(6, (int)error, exceptionMessage);
		}
	}

	[NonEvent]
	public void ConnectStart(System.Net.Internals.SocketAddress address)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			ConnectStart(address.ToString());
		}
	}

	[NonEvent]
	public void AfterConnect(SocketError error, string exceptionMessage = null)
	{
		if (error == SocketError.Success)
		{
			Interlocked.Increment(ref _outgoingConnectionsEstablished);
		}
		else
		{
			ConnectFailed(error, exceptionMessage);
		}
		ConnectStop();
	}

	[NonEvent]
	public void AcceptStart(System.Net.Internals.SocketAddress address)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			AcceptStart(address.ToString());
		}
	}

	[NonEvent]
	public void AcceptStart(EndPoint address)
	{
		if (IsEnabled(EventLevel.Informational, EventKeywords.All))
		{
			AcceptStart(address.Serialize().ToString());
		}
	}

	[NonEvent]
	public void AfterAccept(SocketError error, string exceptionMessage = null)
	{
		if (error == SocketError.Success)
		{
			Interlocked.Increment(ref _incomingConnectionsEstablished);
		}
		else
		{
			AcceptFailed(error, exceptionMessage);
		}
		AcceptStop();
	}

	[NonEvent]
	public void BytesReceived(int count)
	{
		Interlocked.Add(ref _bytesReceived, count);
	}

	[NonEvent]
	public void BytesSent(int count)
	{
		Interlocked.Add(ref _bytesSent, count);
	}

	[NonEvent]
	public void DatagramReceived()
	{
		Interlocked.Increment(ref _datagramsReceived);
	}

	[NonEvent]
	public void DatagramSent()
	{
		Interlocked.Increment(ref _datagramsSent);
	}

	protected override void OnEventCommand(EventCommandEventArgs command)
	{
		if (command.Command != EventCommand.Enable)
		{
			return;
		}
		if (_outgoingConnectionsEstablishedCounter == null)
		{
			_outgoingConnectionsEstablishedCounter = new PollingCounter("outgoing-connections-established", this, () => Interlocked.Read(ref _outgoingConnectionsEstablished))
			{
				DisplayName = "Outgoing Connections Established"
			};
		}
		if (_incomingConnectionsEstablishedCounter == null)
		{
			_incomingConnectionsEstablishedCounter = new PollingCounter("incoming-connections-established", this, () => Interlocked.Read(ref _incomingConnectionsEstablished))
			{
				DisplayName = "Incoming Connections Established"
			};
		}
		if (_bytesReceivedCounter == null)
		{
			_bytesReceivedCounter = new PollingCounter("bytes-received", this, () => Interlocked.Read(ref _bytesReceived))
			{
				DisplayName = "Bytes Received"
			};
		}
		if (_bytesSentCounter == null)
		{
			_bytesSentCounter = new PollingCounter("bytes-sent", this, () => Interlocked.Read(ref _bytesSent))
			{
				DisplayName = "Bytes Sent"
			};
		}
		if (_datagramsReceivedCounter == null)
		{
			_datagramsReceivedCounter = new PollingCounter("datagrams-received", this, () => Interlocked.Read(ref _datagramsReceived))
			{
				DisplayName = "Datagrams Received"
			};
		}
		if (_datagramsSentCounter == null)
		{
			_datagramsSentCounter = new PollingCounter("datagrams-sent", this, () => Interlocked.Read(ref _datagramsSent))
			{
				DisplayName = "Datagrams Sent"
			};
		}
	}
}
