using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.Mock;

internal sealed class MockListener : QuicListenerProvider
{
	private bool _disposed;

	private readonly QuicListenerOptions _options;

	private readonly IPEndPoint _listenEndPoint;

	private Channel<MockConnection.ConnectionState> _listenQueue;

	private static int s_mockPort;

	private static ConcurrentDictionary<int, MockListener> s_listenerMap = new ConcurrentDictionary<int, MockListener>();

	internal override IPEndPoint ListenEndPoint => _listenEndPoint;

	internal MockListener(QuicListenerOptions options)
	{
		if (options.ListenEndPoint == null || options.ListenEndPoint.Address != IPAddress.Loopback || options.ListenEndPoint.Port != 0)
		{
			throw new ArgumentException("Must pass loopback address and port 0");
		}
		_options = options;
		int num = Interlocked.Increment(ref s_mockPort);
		_listenEndPoint = new IPEndPoint(IPAddress.Loopback, num);
		bool flag = s_listenerMap.TryAdd(num, this);
		_listenQueue = Channel.CreateBounded<MockConnection.ConnectionState>(new BoundedChannelOptions(options.ListenBacklog));
	}

	internal static MockListener TryGetListener(IPEndPoint endpoint)
	{
		if (endpoint.Address != IPAddress.Loopback || endpoint.Port == 0)
		{
			return null;
		}
		if (!s_listenerMap.TryGetValue(endpoint.Port, out var value))
		{
			return null;
		}
		return value;
	}

	internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		CheckDisposed();
		MockConnection.ConnectionState state = await _listenQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return new MockConnection(_listenEndPoint, state);
	}

	internal bool TryConnect(MockConnection.ConnectionState state)
	{
		state._serverStreamLimit = new MockConnection.PeerStreamLimit(_options.MaxUnidirectionalStreams, _options.MaxBidirectionalStreams);
		return _listenQueue.Writer.TryWrite(state);
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("QuicListener");
		}
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing)
			{
				MockListener value;
				bool flag = s_listenerMap.TryRemove(_listenEndPoint.Port, out value);
			}
			_disposed = true;
		}
	}

	~MockListener()
	{
		Dispose(disposing: false);
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
