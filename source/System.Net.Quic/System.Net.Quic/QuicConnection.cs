using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

public sealed class QuicConnection : IDisposable
{
	private readonly QuicConnectionProvider _provider;

	public bool Connected => _provider.Connected;

	public IPEndPoint? LocalEndPoint => _provider.LocalEndPoint;

	public EndPoint RemoteEndPoint => _provider.RemoteEndPoint;

	public X509Certificate? RemoteCertificate => _provider.RemoteCertificate;

	public SslApplicationProtocol NegotiatedApplicationProtocol => _provider.NegotiatedApplicationProtocol;

	public QuicConnection(EndPoint remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null)
		: this(QuicImplementationProviders.Default, remoteEndPoint, sslClientAuthenticationOptions, localEndPoint)
	{
	}

	public QuicConnection(QuicClientConnectionOptions options)
		: this(QuicImplementationProviders.Default, options)
	{
	}

	public QuicConnection(QuicImplementationProvider implementationProvider, EndPoint remoteEndPoint, SslClientAuthenticationOptions? sslClientAuthenticationOptions, IPEndPoint? localEndPoint = null)
		: this(implementationProvider, new QuicClientConnectionOptions
		{
			RemoteEndPoint = remoteEndPoint,
			ClientAuthenticationOptions = sslClientAuthenticationOptions,
			LocalEndPoint = localEndPoint
		})
	{
	}

	public QuicConnection(QuicImplementationProvider implementationProvider, QuicClientConnectionOptions options)
	{
		_provider = implementationProvider.CreateConnection(options);
	}

	internal QuicConnection(QuicConnectionProvider provider)
	{
		_provider = provider;
	}

	public ValueTask ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _provider.ConnectAsync(cancellationToken);
	}

	public ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _provider.WaitForAvailableUnidirectionalStreamsAsync(cancellationToken);
	}

	public ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _provider.WaitForAvailableBidirectionalStreamsAsync(cancellationToken);
	}

	public QuicStream OpenUnidirectionalStream()
	{
		return new QuicStream(_provider.OpenUnidirectionalStream());
	}

	public QuicStream OpenBidirectionalStream()
	{
		return new QuicStream(_provider.OpenBidirectionalStream());
	}

	public async ValueTask<QuicStream> AcceptStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return new QuicStream(await _provider.AcceptStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
	}

	public ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _provider.CloseAsync(errorCode, cancellationToken);
	}

	public void Dispose()
	{
		_provider.Dispose();
	}

	public int GetRemoteAvailableUnidirectionalStreamCount()
	{
		return _provider.GetRemoteAvailableUnidirectionalStreamCount();
	}

	public int GetRemoteAvailableBidirectionalStreamCount()
	{
		return _provider.GetRemoteAvailableBidirectionalStreamCount();
	}
}
