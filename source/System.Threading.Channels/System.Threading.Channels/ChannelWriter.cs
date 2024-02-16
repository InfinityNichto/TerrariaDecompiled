using System.Threading.Tasks;

namespace System.Threading.Channels;

public abstract class ChannelWriter<T>
{
	public virtual bool TryComplete(Exception? error = null)
	{
		return false;
	}

	public abstract bool TryWrite(T item);

	public abstract ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default(CancellationToken));

	public virtual ValueTask WriteAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
	{
		try
		{
			return cancellationToken.IsCancellationRequested ? new ValueTask(Task.FromCanceled<T>(cancellationToken)) : (TryWrite(item) ? default(ValueTask) : WriteAsyncCore(item, cancellationToken));
		}
		catch (Exception exception)
		{
			return new ValueTask(Task.FromException(exception));
		}
	}

	private async ValueTask WriteAsyncCore(T innerItem, CancellationToken ct)
	{
		while (await WaitToWriteAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
		{
			if (TryWrite(innerItem))
			{
				return;
			}
		}
		throw ChannelUtilities.CreateInvalidCompletionException();
	}

	public void Complete(Exception? error = null)
	{
		if (!TryComplete(error))
		{
			throw ChannelUtilities.CreateInvalidCompletionException();
		}
	}
}
