using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock;

internal sealed class MockConnection : QuicConnectionProvider
{
	internal sealed class StreamLimit
	{
		public readonly int MaxCount;

		private int _actualCount;

		private TaskCompletionSource _availableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		private readonly object _syncRoot = new object();

		public int AvailableCount => MaxCount - _actualCount;

		public StreamLimit(int maxCount)
		{
			MaxCount = maxCount;
		}

		public void Decrement()
		{
			TaskCompletionSource taskCompletionSource = null;
			lock (_syncRoot)
			{
				_actualCount--;
				if (!_availableTcs.Task.IsCompleted)
				{
					taskCompletionSource = _availableTcs;
					_availableTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				}
			}
			taskCompletionSource?.SetResult();
		}

		public bool TryIncrement()
		{
			lock (_syncRoot)
			{
				if (_actualCount < MaxCount)
				{
					_actualCount++;
					return true;
				}
				return false;
			}
		}

		public ValueTask WaitForAvailableStreams(CancellationToken cancellationToken)
		{
			TaskCompletionSource availableTcs;
			lock (_syncRoot)
			{
				if (_actualCount > 0)
				{
					return default(ValueTask);
				}
				availableTcs = _availableTcs;
			}
			return new ValueTask(availableTcs.Task.WaitAsync(cancellationToken));
		}

		public void CloseWaiters()
		{
			_availableTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
		}
	}

	internal class PeerStreamLimit
	{
		public readonly StreamLimit Unidirectional;

		public readonly StreamLimit Bidirectional;

		public PeerStreamLimit(int maxUnidirectional, int maxBidirectional)
		{
			Unidirectional = new StreamLimit(maxUnidirectional);
			Bidirectional = new StreamLimit(maxBidirectional);
		}
	}

	internal sealed class ConnectionState
	{
		public readonly SslApplicationProtocol _applicationProtocol;

		public readonly Channel<MockStream.StreamState> _clientInitiatedStreamChannel;

		public readonly Channel<MockStream.StreamState> _serverInitiatedStreamChannel;

		public readonly ConcurrentDictionary<long, MockStream.StreamState> _streams;

		public PeerStreamLimit _clientStreamLimit;

		public PeerStreamLimit _serverStreamLimit;

		public long _clientErrorCode;

		public long _serverErrorCode;

		public bool _closed;

		public ConnectionState(SslApplicationProtocol applicationProtocol)
		{
			_applicationProtocol = applicationProtocol;
			_clientInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
			_serverInitiatedStreamChannel = Channel.CreateUnbounded<MockStream.StreamState>();
			_clientErrorCode = (_serverErrorCode = -1L);
			_streams = new ConcurrentDictionary<long, MockStream.StreamState>();
		}
	}

	private readonly bool _isClient;

	private bool _disposed;

	private SslClientAuthenticationOptions _sslClientAuthenticationOptions;

	private IPEndPoint _remoteEndPoint;

	private IPEndPoint _localEndPoint;

	private object _syncObject = new object();

	private long _nextOutboundBidirectionalStream;

	private long _nextOutboundUnidirectionalStream;

	private readonly int _maxUnidirectionalStreams;

	private readonly int _maxBidirectionalStreams;

	private ConnectionState _state;

	internal PeerStreamLimit LocalStreamLimit
	{
		get
		{
			if (!_isClient)
			{
				return _state?._serverStreamLimit;
			}
			return _state?._clientStreamLimit;
		}
	}

	internal PeerStreamLimit RemoteStreamLimit
	{
		get
		{
			if (!_isClient)
			{
				return _state?._clientStreamLimit;
			}
			return _state?._serverStreamLimit;
		}
	}

	internal long? ConnectionError
	{
		get
		{
			long? num = ((!_isClient) ? _state?._clientErrorCode : _state?._serverErrorCode);
			if (num == -1)
			{
				num = null;
			}
			return num;
		}
	}

	internal override X509Certificate RemoteCertificate => null;

	internal override bool Connected
	{
		get
		{
			CheckDisposed();
			return _state != null;
		}
	}

	internal override IPEndPoint LocalEndPoint => _localEndPoint;

	internal override EndPoint RemoteEndPoint => _remoteEndPoint;

	internal override SslApplicationProtocol NegotiatedApplicationProtocol
	{
		get
		{
			if (_state == null)
			{
				throw new InvalidOperationException("not connected");
			}
			return _state._applicationProtocol;
		}
	}

	internal MockConnection(EndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null, int maxUnidirectionalStreams = 100, int maxBidirectionalStreams = 100)
	{
		if (remoteEndPoint == null)
		{
			throw new ArgumentNullException("remoteEndPoint");
		}
		IPEndPoint iPEndPoint = GetIPEndPoint(remoteEndPoint);
		if (iPEndPoint.Address != IPAddress.Loopback)
		{
			throw new ArgumentException("Expected loopback address", "remoteEndPoint");
		}
		_isClient = true;
		_remoteEndPoint = iPEndPoint;
		_localEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
		_sslClientAuthenticationOptions = sslClientAuthenticationOptions;
		_nextOutboundBidirectionalStream = 0L;
		_nextOutboundUnidirectionalStream = 2L;
		_maxUnidirectionalStreams = maxUnidirectionalStreams;
		_maxBidirectionalStreams = maxBidirectionalStreams;
	}

	internal MockConnection(IPEndPoint localEndPoint, ConnectionState state)
	{
		_isClient = false;
		_remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
		_localEndPoint = localEndPoint;
		_nextOutboundBidirectionalStream = 1L;
		_nextOutboundUnidirectionalStream = 3L;
		_state = state;
	}

	private static IPEndPoint GetIPEndPoint(EndPoint endPoint)
	{
		if (endPoint is IPEndPoint result)
		{
			return result;
		}
		if (endPoint is DnsEndPoint dnsEndPoint)
		{
			if (dnsEndPoint.Host == "127.0.0.1")
			{
				return new IPEndPoint(IPAddress.Loopback, dnsEndPoint.Port);
			}
			throw new InvalidOperationException("invalid DNS name " + dnsEndPoint.Host);
		}
		throw new InvalidOperationException("unknown EndPoint type");
	}

	internal override ValueTask ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		if (Connected)
		{
			throw new InvalidOperationException("Already connected");
		}
		MockListener mockListener = MockListener.TryGetListener(_remoteEndPoint);
		if (mockListener == null)
		{
			throw new InvalidOperationException("Could not find listener");
		}
		_state = new ConnectionState(_sslClientAuthenticationOptions.ApplicationProtocols[0])
		{
			_clientStreamLimit = new PeerStreamLimit(_maxUnidirectionalStreams, _maxBidirectionalStreams)
		};
		if (!mockListener.TryConnect(_state))
		{
			throw new QuicException("Connection refused");
		}
		return ValueTask.CompletedTask;
	}

	internal override ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		return remoteStreamLimit.Unidirectional.WaitForAvailableStreams(cancellationToken);
	}

	internal override ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		return remoteStreamLimit.Bidirectional.WaitForAvailableStreams(cancellationToken);
	}

	internal override QuicStreamProvider OpenUnidirectionalStream()
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		if (!remoteStreamLimit.Unidirectional.TryIncrement())
		{
			throw new QuicException("No available unidirectional stream");
		}
		long nextOutboundUnidirectionalStream;
		lock (_syncObject)
		{
			nextOutboundUnidirectionalStream = _nextOutboundUnidirectionalStream;
			_nextOutboundUnidirectionalStream += 4L;
		}
		return OpenStream(nextOutboundUnidirectionalStream, bidirectional: false);
	}

	internal override QuicStreamProvider OpenBidirectionalStream()
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		if (!remoteStreamLimit.Bidirectional.TryIncrement())
		{
			throw new QuicException("No available bidirectional stream");
		}
		long nextOutboundBidirectionalStream;
		lock (_syncObject)
		{
			nextOutboundBidirectionalStream = _nextOutboundBidirectionalStream;
			_nextOutboundBidirectionalStream += 4L;
		}
		return OpenStream(nextOutboundBidirectionalStream, bidirectional: true);
	}

	internal MockStream OpenStream(long streamId, bool bidirectional)
	{
		CheckDisposed();
		ConnectionState state = _state;
		if (state == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		MockStream.StreamState streamState = new MockStream.StreamState(streamId, bidirectional);
		state._streams[streamState._streamId] = streamState;
		Channel<MockStream.StreamState> channel = (_isClient ? state._clientInitiatedStreamChannel : state._serverInitiatedStreamChannel);
		channel.Writer.TryWrite(streamState);
		return new MockStream(this, streamState, isInitiator: true);
	}

	internal override int GetRemoteAvailableUnidirectionalStreamCount()
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		return remoteStreamLimit.Unidirectional.AvailableCount;
	}

	internal override int GetRemoteAvailableBidirectionalStreamCount()
	{
		PeerStreamLimit remoteStreamLimit = RemoteStreamLimit;
		if (remoteStreamLimit == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		return remoteStreamLimit.Bidirectional.AvailableCount;
	}

	internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		ConnectionState state = _state;
		if (state == null)
		{
			throw new InvalidOperationException("Not connected");
		}
		Channel<MockStream.StreamState> channel = (_isClient ? state._serverInitiatedStreamChannel : state._clientInitiatedStreamChannel);
		try
		{
			return new MockStream(this, await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), isInitiator: false);
		}
		catch (ChannelClosedException)
		{
			long num = (_isClient ? state._serverErrorCode : state._clientErrorCode);
			throw (num == -1) ? ((QuicException)new QuicOperationAbortedException()) : ((QuicException)new QuicConnectionAbortedException(num));
		}
	}

	internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default(CancellationToken))
	{
		ConnectionState state = _state;
		if (state != null)
		{
			if (state._closed)
			{
				return default(ValueTask);
			}
			state._closed = true;
			if (_isClient)
			{
				state._clientErrorCode = errorCode;
				DrainAcceptQueue(-1L, errorCode);
			}
			else
			{
				state._serverErrorCode = errorCode;
				DrainAcceptQueue(errorCode, -1L);
			}
			foreach (KeyValuePair<long, MockStream.StreamState> stream in state._streams)
			{
				stream.Value._outboundWritesCompletedTcs.TrySetException(new QuicConnectionAbortedException(errorCode));
				stream.Value._inboundWritesCompletedTcs.TrySetException(new QuicConnectionAbortedException(errorCode));
			}
		}
		Dispose();
		return default(ValueTask);
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("QuicConnection");
		}
	}

	private void DrainAcceptQueue(long outboundErrorCode, long inboundErrorCode)
	{
		ConnectionState state = _state;
		if (state != null)
		{
			state._clientInitiatedStreamChannel.Writer.TryComplete();
			MockStream.StreamState item;
			while (state._clientInitiatedStreamChannel.Reader.TryRead(out item))
			{
				item._outboundReadErrorCode = (item._outboundWriteErrorCode = outboundErrorCode);
				item._inboundStreamBuffer?.AbortRead();
				item._outboundStreamBuffer?.EndWrite();
			}
			state._serverInitiatedStreamChannel.Writer.TryComplete();
			MockStream.StreamState item2;
			while (state._serverInitiatedStreamChannel.Reader.TryRead(out item2))
			{
				item2._inboundReadErrorCode = (item2._inboundWriteErrorCode = inboundErrorCode);
				item2._outboundStreamBuffer?.AbortRead();
				item2._inboundStreamBuffer?.EndWrite();
			}
		}
	}

	private void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}
		if (disposing)
		{
			DrainAcceptQueue(-1L, -1L);
			PeerStreamLimit localStreamLimit = LocalStreamLimit;
			if (localStreamLimit != null)
			{
				localStreamLimit.Unidirectional.CloseWaiters();
				localStreamLimit.Bidirectional.CloseWaiters();
			}
		}
		_disposed = true;
	}

	~MockConnection()
	{
		Dispose(disposing: false);
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
