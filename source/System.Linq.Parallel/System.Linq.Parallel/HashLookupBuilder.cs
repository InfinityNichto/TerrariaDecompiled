using System.Threading;

namespace System.Linq.Parallel;

internal abstract class HashLookupBuilder<TElement, TOrderKey, THashKey>
{
	protected interface IBaseHashBuilder<TBaseElement, TBaseOrderKey>
	{
		bool Add(THashKey hashKey, TBaseElement element, TBaseOrderKey orderKey);
	}

	public abstract HashJoinHashLookup<THashKey, TElement, TOrderKey> BuildHashLookup(CancellationToken cancellationToken);

	protected void BuildBaseHashLookup<TBaseBuilder, TBaseElement, TBaseOrderKey>(QueryOperatorEnumerator<Pair<TBaseElement, THashKey>, TBaseOrderKey> dataSource, TBaseBuilder baseHashBuilder, CancellationToken cancellationToken) where TBaseBuilder : IBaseHashBuilder<TBaseElement, TBaseOrderKey>
	{
		Pair<TBaseElement, THashKey> currentElement = default(Pair<TBaseElement, THashKey>);
		TBaseOrderKey currentKey = default(TBaseOrderKey);
		int num = 0;
		while (dataSource.MoveNext(ref currentElement, ref currentKey))
		{
			if ((num++ & 0x3F) == 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			TBaseElement first = currentElement.First;
			THashKey second = currentElement.Second;
			if (second != null)
			{
				baseHashBuilder.Add(second, first, currentKey);
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
	}
}
