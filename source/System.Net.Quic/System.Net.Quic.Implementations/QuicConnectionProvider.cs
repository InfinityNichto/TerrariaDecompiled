using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations;

internal abstract class QuicConnectionProvider : IDisposable
{
	internal abstract bool Connected { get; }

	internal abstract IPEndPoint LocalEndPoint { get; }

	internal abstract EndPoint RemoteEndPoint { get; }

	internal abstract SslApplicationProtocol NegotiatedApplicationProtocol { get; }

	internal abstract X509Certificate RemoteCertificate { get; }

	internal abstract ValueTask ConnectAsync(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract QuicStreamProvider OpenUnidirectionalStream();

	internal abstract QuicStreamProvider OpenBidirectionalStream();

	internal abstract int GetRemoteAvailableUnidirectionalStreamCount();

	internal abstract int GetRemoteAvailableBidirectionalStreamCount();

	internal abstract ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default(CancellationToken));

	internal abstract ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default(CancellationToken));

	public abstract void Dispose();
}
