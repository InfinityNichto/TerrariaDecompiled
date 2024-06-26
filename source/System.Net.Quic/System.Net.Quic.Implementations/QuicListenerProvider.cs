using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations;

internal abstract class QuicListenerProvider : IDisposable
{
	internal abstract IPEndPoint ListenEndPoint { get; }

	internal abstract ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default(CancellationToken));

	public abstract void Dispose();
}
