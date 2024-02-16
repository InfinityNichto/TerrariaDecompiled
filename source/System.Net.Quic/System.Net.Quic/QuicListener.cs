using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic;

public sealed class QuicListener : IDisposable
{
	private readonly QuicListenerProvider _provider;

	public IPEndPoint ListenEndPoint => _provider.ListenEndPoint;

	public QuicListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
		: this(QuicImplementationProviders.Default, listenEndPoint, sslServerAuthenticationOptions)
	{
	}

	public QuicListener(QuicListenerOptions options)
		: this(QuicImplementationProviders.Default, options)
	{
	}

	public QuicListener(QuicImplementationProvider implementationProvider, IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
		: this(implementationProvider, new QuicListenerOptions
		{
			ListenEndPoint = listenEndPoint,
			ServerAuthenticationOptions = sslServerAuthenticationOptions
		})
	{
	}

	public QuicListener(QuicImplementationProvider implementationProvider, QuicListenerOptions options)
	{
		_provider = implementationProvider.CreateListener(options);
	}

	public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return new QuicConnection(await _provider.AcceptConnectionAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false));
	}

	public void Dispose()
	{
		_provider.Dispose();
	}
}
