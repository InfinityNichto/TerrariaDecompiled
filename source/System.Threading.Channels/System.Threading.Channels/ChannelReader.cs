using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Channels;

public abstract class ChannelReader<T>
{
	public virtual Task Completion => ChannelUtilities.s_neverCompletingTask;

	public virtual bool CanCount => false;

	public virtual bool CanPeek => false;

	public virtual int Count
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public abstract bool TryRead([MaybeNullWhen(false)] out T item);

	public virtual bool TryPeek([MaybeNullWhen(false)] out T item)
	{
		item = default(T);
		return false;
	}

	public abstract ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken));

	public virtual ValueTask<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));
		}
		try
		{
			if (TryRead(out var item))
			{
				return new ValueTask<T>(item);
			}
		}
		catch (Exception ex) when (!(ex is ChannelClosedException) && !(ex is OperationCanceledException))
		{
			return new ValueTask<T>(Task.FromException<T>(ex));
		}
		return ReadAsyncCore(cancellationToken);
		async ValueTask<T> ReadAsyncCore(CancellationToken ct)
		{
			T item2;
			do
			{
				if (!(await WaitToReadAsync(ct).ConfigureAwait(continueOnCapturedContext: false)))
				{
					throw new ChannelClosedException();
				}
			}
			while (!TryRead(out item2));
			return item2;
		}
	}

	public virtual async IAsyncEnumerable<T> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken))
	{
		while (await WaitToReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
		{
			T item;
			while (TryRead(out item))
			{
				yield return item;
			}
		}
	}
}
