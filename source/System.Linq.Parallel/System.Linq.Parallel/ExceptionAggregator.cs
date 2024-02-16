using System.Collections.Generic;

namespace System.Linq.Parallel;

internal static class ExceptionAggregator
{
	internal static IEnumerable<TElement> WrapEnumerable<TElement>(IEnumerable<TElement> source, CancellationState cancellationState)
	{
		using IEnumerator<TElement> enumerator = source.GetEnumerator();
		while (true)
		{
			TElement val = default(TElement);
			try
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				val = enumerator.Current;
			}
			catch (Exception ex)
			{
				ThrowOCEorAggregateException(ex, cancellationState);
			}
			yield return val;
		}
	}

	internal static IEnumerable<TElement> WrapQueryEnumerator<TElement, TIgnoreKey>(QueryOperatorEnumerator<TElement, TIgnoreKey> source, CancellationState cancellationState)
	{
		TElement elem = default(TElement);
		TIgnoreKey ignoreKey = default(TIgnoreKey);
		try
		{
			while (true)
			{
				try
				{
					if (!source.MoveNext(ref elem, ref ignoreKey))
					{
						break;
					}
				}
				catch (Exception ex)
				{
					ThrowOCEorAggregateException(ex, cancellationState);
				}
				yield return elem;
			}
		}
		finally
		{
			source.Dispose();
		}
	}

	internal static void ThrowOCEorAggregateException(Exception ex, CancellationState cancellationState)
	{
		if (ThrowAnOCE(ex, cancellationState))
		{
			CancellationState.ThrowWithStandardMessageIfCanceled(cancellationState.ExternalCancellationToken);
			return;
		}
		throw new AggregateException(ex);
	}

	internal static Func<T, U> WrapFunc<T, U>(Func<T, U> f, CancellationState cancellationState)
	{
		return delegate(T t)
		{
			U result = default(U);
			try
			{
				result = f(t);
				return result;
			}
			catch (Exception ex)
			{
				ThrowOCEorAggregateException(ex, cancellationState);
			}
			return result;
		};
	}

	private static bool ThrowAnOCE(Exception ex, CancellationState cancellationState)
	{
		if (ex is OperationCanceledException ex2)
		{
			if (ex2.CancellationToken == cancellationState.ExternalCancellationToken && cancellationState.ExternalCancellationToken.IsCancellationRequested)
			{
				return true;
			}
			if (ex2.CancellationToken == cancellationState.MergedCancellationToken && cancellationState.MergedCancellationToken.IsCancellationRequested && cancellationState.ExternalCancellationToken.IsCancellationRequested)
			{
				return true;
			}
		}
		return false;
	}
}
