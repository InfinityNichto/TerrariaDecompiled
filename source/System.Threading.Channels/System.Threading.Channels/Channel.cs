namespace System.Threading.Channels;

public static class Channel
{
	public static Channel<T> CreateUnbounded<T>()
	{
		return new UnboundedChannel<T>(runContinuationsAsynchronously: true);
	}

	public static Channel<T> CreateUnbounded<T>(UnboundedChannelOptions options)
	{
		if (options != null)
		{
			if (!options.SingleReader)
			{
				return new UnboundedChannel<T>(!options.AllowSynchronousContinuations);
			}
			return new SingleConsumerUnboundedChannel<T>(!options.AllowSynchronousContinuations);
		}
		throw new ArgumentNullException("options");
	}

	public static Channel<T> CreateBounded<T>(int capacity)
	{
		if (capacity < 1)
		{
			throw new ArgumentOutOfRangeException("capacity");
		}
		return new BoundedChannel<T>(capacity, BoundedChannelFullMode.Wait, runContinuationsAsynchronously: true, null);
	}

	public static Channel<T> CreateBounded<T>(BoundedChannelOptions options)
	{
		return CreateBounded<T>(options, null);
	}

	public static Channel<T> CreateBounded<T>(BoundedChannelOptions options, Action<T>? itemDropped)
	{
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		return new BoundedChannel<T>(options.Capacity, options.FullMode, !options.AllowSynchronousContinuations, itemDropped);
	}
}
public abstract class Channel<T> : Channel<T, T>
{
}
public abstract class Channel<TWrite, TRead>
{
	public ChannelReader<TRead> Reader { get; protected set; }

	public ChannelWriter<TWrite> Writer { get; protected set; }

	public static implicit operator ChannelReader<TRead>(Channel<TWrite, TRead> channel)
	{
		return channel.Reader;
	}

	public static implicit operator ChannelWriter<TWrite>(Channel<TWrite, TRead> channel)
	{
		return channel.Writer;
	}
}
