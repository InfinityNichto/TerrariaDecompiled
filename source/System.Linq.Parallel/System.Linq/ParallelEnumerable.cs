using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Parallel;
using System.Threading;

namespace System.Linq;

public static class ParallelEnumerable
{
	public static ParallelQuery<TSource> AsParallel<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new ParallelEnumerableWrapper<TSource>(source);
	}

	public static ParallelQuery<TSource> AsParallel<TSource>(this Partitioner<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new PartitionerQueryOperator<TSource>(source);
	}

	public static ParallelQuery<TSource> AsOrdered<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (!(source is ParallelEnumerableWrapper<TSource>) && !(source is IParallelPartitionable<TSource>))
		{
			if (!(source is PartitionerQueryOperator<TSource> partitionerQueryOperator))
			{
				throw new InvalidOperationException(System.SR.ParallelQuery_InvalidAsOrderedCall);
			}
			if (!partitionerQueryOperator.Orderable)
			{
				throw new InvalidOperationException(System.SR.ParallelQuery_PartitionerNotOrderable);
			}
		}
		return new OrderingQueryOperator<TSource>(QueryOperator<TSource>.AsQueryOperator(source), orderOn: true);
	}

	public static ParallelQuery AsOrdered(this ParallelQuery source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (!(source is ParallelEnumerableWrapper source2))
		{
			throw new InvalidOperationException(System.SR.ParallelQuery_InvalidNonGenericAsOrderedCall);
		}
		return new OrderingQueryOperator<object>(QueryOperator<object>.AsQueryOperator(source2), orderOn: true);
	}

	public static ParallelQuery<TSource> AsUnordered<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new OrderingQueryOperator<TSource>(QueryOperator<TSource>.AsQueryOperator(source), orderOn: false);
	}

	public static ParallelQuery AsParallel(this IEnumerable source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new ParallelEnumerableWrapper(source);
	}

	public static IEnumerable<TSource> AsSequential<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source is ParallelEnumerableWrapper<TSource> parallelEnumerableWrapper)
		{
			return parallelEnumerableWrapper.WrappedEnumerable;
		}
		return source;
	}

	public static ParallelQuery<TSource> WithDegreeOfParallelism<TSource>(this ParallelQuery<TSource> source, int degreeOfParallelism)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (degreeOfParallelism < 1 || degreeOfParallelism > 512)
		{
			throw new ArgumentOutOfRangeException("degreeOfParallelism");
		}
		QuerySettings empty = QuerySettings.Empty;
		empty.DegreeOfParallelism = degreeOfParallelism;
		return new QueryExecutionOption<TSource>(QueryOperator<TSource>.AsQueryOperator(source), empty);
	}

	public static ParallelQuery<TSource> WithCancellation<TSource>(this ParallelQuery<TSource> source, CancellationToken cancellationToken)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		QuerySettings empty = QuerySettings.Empty;
		empty.CancellationState = new CancellationState(cancellationToken);
		return new QueryExecutionOption<TSource>(QueryOperator<TSource>.AsQueryOperator(source), empty);
	}

	public static ParallelQuery<TSource> WithExecutionMode<TSource>(this ParallelQuery<TSource> source, ParallelExecutionMode executionMode)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (executionMode != 0 && executionMode != ParallelExecutionMode.ForceParallelism)
		{
			throw new ArgumentException(System.SR.ParallelEnumerable_WithQueryExecutionMode_InvalidMode);
		}
		QuerySettings empty = QuerySettings.Empty;
		empty.ExecutionMode = executionMode;
		return new QueryExecutionOption<TSource>(QueryOperator<TSource>.AsQueryOperator(source), empty);
	}

	public static ParallelQuery<TSource> WithMergeOptions<TSource>(this ParallelQuery<TSource> source, ParallelMergeOptions mergeOptions)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (mergeOptions != 0 && mergeOptions != ParallelMergeOptions.AutoBuffered && mergeOptions != ParallelMergeOptions.NotBuffered && mergeOptions != ParallelMergeOptions.FullyBuffered)
		{
			throw new ArgumentException(System.SR.ParallelEnumerable_WithMergeOptions_InvalidOptions);
		}
		QuerySettings empty = QuerySettings.Empty;
		empty.MergeOptions = mergeOptions;
		return new QueryExecutionOption<TSource>(QueryOperator<TSource>.AsQueryOperator(source), empty);
	}

	public static ParallelQuery<int> Range(int start, int count)
	{
		if (count < 0 || (count > 0 && int.MaxValue - (count - 1) < start))
		{
			throw new ArgumentOutOfRangeException("count");
		}
		return new RangeEnumerable(start, count);
	}

	public static ParallelQuery<TResult> Repeat<TResult>(TResult element, int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		return new RepeatEnumerable<TResult>(element, count);
	}

	public static ParallelQuery<TResult> Empty<TResult>()
	{
		return EmptyEnumerable<TResult>.Instance;
	}

	public static void ForAll<TSource>(this ParallelQuery<TSource> source, Action<TSource> action)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		new ForAllOperator<TSource>(source, action).RunSynchronously();
	}

	public static ParallelQuery<TSource> Where<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new WhereQueryOperator<TSource>(source, predicate);
	}

	public static ParallelQuery<TSource> Where<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new IndexedWhereQueryOperator<TSource>(source, predicate);
	}

	public static ParallelQuery<TResult> Select<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		return new SelectQueryOperator<TSource, TResult>(source, selector);
	}

	public static ParallelQuery<TResult> Select<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, int, TResult> selector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		return new IndexedSelectQueryOperator<TSource, TResult>(source, selector);
	}

	public static ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(this ParallelQuery<TFirst> first, ParallelQuery<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new ZipQueryOperator<TFirst, TSecond, TResult>(first, second, resultSelector);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TResult> Zip<TFirst, TSecond, TResult>(this ParallelQuery<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
	{
		return outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		if (outer == null)
		{
			throw new ArgumentNullException("outer");
		}
		if (inner == null)
		{
			throw new ArgumentNullException("inner");
		}
		if (outerKeySelector == null)
		{
			throw new ArgumentNullException("outerKeySelector");
		}
		if (innerKeySelector == null)
		{
			throw new ArgumentNullException("innerKeySelector");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new JoinQueryOperator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TResult> Join<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
	{
		return outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, ParallelQuery<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		if (outer == null)
		{
			throw new ArgumentNullException("outer");
		}
		if (inner == null)
		{
			throw new ArgumentNullException("inner");
		}
		if (outerKeySelector == null)
		{
			throw new ArgumentNullException("outerKeySelector");
		}
		if (innerKeySelector == null)
		{
			throw new ArgumentNullException("innerKeySelector");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new GroupJoinQueryOperator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this ParallelQuery<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TResult> SelectMany<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		return new SelectManyQueryOperator<TSource, TResult, TResult>(source, selector, null, null);
	}

	public static ParallelQuery<TResult> SelectMany<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (selector == null)
		{
			throw new ArgumentNullException("selector");
		}
		return new SelectManyQueryOperator<TSource, TResult, TResult>(source, null, selector, null);
	}

	public static ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(this ParallelQuery<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (collectionSelector == null)
		{
			throw new ArgumentNullException("collectionSelector");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, collectionSelector, null, resultSelector);
	}

	public static ParallelQuery<TResult> SelectMany<TSource, TCollection, TResult>(this ParallelQuery<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (collectionSelector == null)
		{
			throw new ArgumentNullException("collectionSelector");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new SelectManyQueryOperator<TSource, TCollection, TResult>(source, null, collectionSelector, resultSelector);
	}

	public static OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>(new SortQueryOperator<TSource, TKey>(source, keySelector, null, descending: false));
	}

	public static OrderedParallelQuery<TSource> OrderBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>(new SortQueryOperator<TSource, TKey>(source, keySelector, comparer, descending: false));
	}

	public static OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>(new SortQueryOperator<TSource, TKey>(source, keySelector, null, descending: true));
	}

	public static OrderedParallelQuery<TSource> OrderByDescending<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>(new SortQueryOperator<TSource, TKey>(source, keySelector, comparer, descending: true));
	}

	public static OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>((QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable(keySelector, null, descending: false));
	}

	public static OrderedParallelQuery<TSource> ThenBy<TSource, TKey>(this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>((QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable(keySelector, comparer, descending: false));
	}

	public static OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>((QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable(keySelector, null, descending: true));
	}

	public static OrderedParallelQuery<TSource> ThenByDescending<TSource, TKey>(this OrderedParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new OrderedParallelQuery<TSource>((QueryOperator<TSource>)source.OrderedEnumerable.CreateOrderedEnumerable(keySelector, comparer, descending: true));
	}

	public static ParallelQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.GroupBy(keySelector, null);
	}

	public static ParallelQuery<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		return new GroupByQueryOperator<TSource, TKey, TSource>(source, keySelector, null, comparer);
	}

	public static ParallelQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		return source.GroupBy(keySelector, elementSelector, null);
	}

	public static ParallelQuery<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		if (elementSelector == null)
		{
			throw new ArgumentNullException("elementSelector");
		}
		return new GroupByQueryOperator<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
	}

	public static ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
	{
		Func<TKey, IEnumerable<TSource>, TResult> resultSelector2 = resultSelector;
		if (resultSelector2 == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return from grouping in source.GroupBy(keySelector)
			select resultSelector2(grouping.Key, grouping);
	}

	public static ParallelQuery<TResult> GroupBy<TSource, TKey, TResult>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		Func<TKey, IEnumerable<TSource>, TResult> resultSelector2 = resultSelector;
		if (resultSelector2 == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return from grouping in source.GroupBy(keySelector, comparer)
			select resultSelector2(grouping.Key, grouping);
	}

	public static ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
	{
		Func<TKey, IEnumerable<TElement>, TResult> resultSelector2 = resultSelector;
		if (resultSelector2 == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return from grouping in source.GroupBy(keySelector, elementSelector)
			select resultSelector2(grouping.Key, grouping);
	}

	public static ParallelQuery<TResult> GroupBy<TSource, TKey, TElement, TResult>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		Func<TKey, IEnumerable<TElement>, TResult> resultSelector2 = resultSelector;
		if (resultSelector2 == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return from grouping in source.GroupBy(keySelector, elementSelector, comparer)
			select resultSelector2(grouping.Key, grouping);
	}

	private static T PerformAggregation<T>(this ParallelQuery<T> source, Func<T, T, T> reduce, T seed, bool seedIsSpecified, bool throwIfEmpty, QueryAggregationOptions options)
	{
		AssociativeAggregationOperator<T, T, T> associativeAggregationOperator = new AssociativeAggregationOperator<T, T, T>(source, seed, null, seedIsSpecified, reduce, reduce, (T obj) => obj, throwIfEmpty, options);
		return associativeAggregationOperator.Aggregate();
	}

	private static TAccumulate PerformSequentialAggregation<TSource, TAccumulate>(this ParallelQuery<TSource> source, TAccumulate seed, bool seedIsSpecified, Func<TAccumulate, TSource, TAccumulate> func)
	{
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		TAccumulate val;
		if (seedIsSpecified)
		{
			val = seed;
		}
		else
		{
			if (!enumerator.MoveNext())
			{
				throw new InvalidOperationException(System.SR.NoElements);
			}
			val = (TAccumulate)(object)enumerator.Current;
		}
		while (enumerator.MoveNext())
		{
			TSource current = enumerator.Current;
			try
			{
				val = func(val, current);
			}
			catch (Exception ex)
			{
				throw new AggregateException(ex);
			}
		}
		return val;
	}

	public static TSource Aggregate<TSource>(this ParallelQuery<TSource> source, Func<TSource, TSource, TSource> func)
	{
		return source.Aggregate(func, QueryAggregationOptions.AssociativeCommutative);
	}

	internal static TSource Aggregate<TSource>(this ParallelQuery<TSource> source, Func<TSource, TSource, TSource> func, QueryAggregationOptions options)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (func == null)
		{
			throw new ArgumentNullException("func");
		}
		if ((0xFFFFFFFCu & (uint)options) != 0)
		{
			throw new ArgumentOutOfRangeException("options");
		}
		if ((options & QueryAggregationOptions.Associative) != QueryAggregationOptions.Associative)
		{
			return source.PerformSequentialAggregation(default(TSource), seedIsSpecified: false, func);
		}
		return source.PerformAggregation(func, default(TSource), seedIsSpecified: false, throwIfEmpty: true, options);
	}

	public static TAccumulate Aggregate<TSource, TAccumulate>(this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
	{
		return source.Aggregate(seed, func, QueryAggregationOptions.AssociativeCommutative);
	}

	internal static TAccumulate Aggregate<TSource, TAccumulate>(this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, QueryAggregationOptions options)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (func == null)
		{
			throw new ArgumentNullException("func");
		}
		if ((0xFFFFFFFCu & (uint)options) != 0)
		{
			throw new ArgumentOutOfRangeException("options");
		}
		return source.PerformSequentialAggregation(seed, seedIsSpecified: true, func);
	}

	public static TResult Aggregate<TSource, TAccumulate, TResult>(this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (func == null)
		{
			throw new ArgumentNullException("func");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		TAccumulate arg = source.PerformSequentialAggregation(seed, seedIsSpecified: true, func);
		try
		{
			return resultSelector(arg);
		}
		catch (Exception ex)
		{
			throw new AggregateException(ex);
		}
	}

	public static TResult Aggregate<TSource, TAccumulate, TResult>(this ParallelQuery<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, Func<TAccumulate, TResult> resultSelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (updateAccumulatorFunc == null)
		{
			throw new ArgumentNullException("updateAccumulatorFunc");
		}
		if (combineAccumulatorsFunc == null)
		{
			throw new ArgumentNullException("combineAccumulatorsFunc");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new AssociativeAggregationOperator<TSource, TAccumulate, TResult>(source, seed, null, seedIsSpecified: true, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector, throwIfEmpty: false, QueryAggregationOptions.AssociativeCommutative).Aggregate();
	}

	public static TResult Aggregate<TSource, TAccumulate, TResult>(this ParallelQuery<TSource> source, Func<TAccumulate> seedFactory, Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc, Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc, Func<TAccumulate, TResult> resultSelector)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (seedFactory == null)
		{
			throw new ArgumentNullException("seedFactory");
		}
		if (updateAccumulatorFunc == null)
		{
			throw new ArgumentNullException("updateAccumulatorFunc");
		}
		if (combineAccumulatorsFunc == null)
		{
			throw new ArgumentNullException("combineAccumulatorsFunc");
		}
		if (resultSelector == null)
		{
			throw new ArgumentNullException("resultSelector");
		}
		return new AssociativeAggregationOperator<TSource, TAccumulate, TResult>(source, default(TAccumulate), seedFactory, seedIsSpecified: true, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector, throwIfEmpty: false, QueryAggregationOptions.AssociativeCommutative).Aggregate();
	}

	public static int Count<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source is ParallelEnumerableWrapper<TSource> { WrappedEnumerable: ICollection<TSource> wrappedEnumerable })
		{
			return wrappedEnumerable.Count;
		}
		return new CountAggregationOperator<TSource>(source).Aggregate();
	}

	public static int Count<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new CountAggregationOperator<TSource>(source.Where(predicate)).Aggregate();
	}

	public static long LongCount<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source is ParallelEnumerableWrapper<TSource> { WrappedEnumerable: ICollection<TSource> wrappedEnumerable })
		{
			return wrappedEnumerable.Count;
		}
		return new LongCountAggregationOperator<TSource>(source).Aggregate();
	}

	public static long LongCount<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new LongCountAggregationOperator<TSource>(source.Where(predicate)).Aggregate();
	}

	public static int Sum(this ParallelQuery<int> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new IntSumAggregationOperator(source).Aggregate();
	}

	public static int? Sum(this ParallelQuery<int?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableIntSumAggregationOperator(source).Aggregate();
	}

	public static long Sum(this ParallelQuery<long> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new LongSumAggregationOperator(source).Aggregate();
	}

	public static long? Sum(this ParallelQuery<long?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableLongSumAggregationOperator(source).Aggregate();
	}

	public static float Sum(this ParallelQuery<float> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new FloatSumAggregationOperator(source).Aggregate();
	}

	public static float? Sum(this ParallelQuery<float?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableFloatSumAggregationOperator(source).Aggregate();
	}

	public static double Sum(this ParallelQuery<double> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DoubleSumAggregationOperator(source).Aggregate();
	}

	public static double? Sum(this ParallelQuery<double?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDoubleSumAggregationOperator(source).Aggregate();
	}

	public static decimal Sum(this ParallelQuery<decimal> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DecimalSumAggregationOperator(source).Aggregate();
	}

	public static decimal? Sum(this ParallelQuery<decimal?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDecimalSumAggregationOperator(source).Aggregate();
	}

	public static int Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
	{
		return source.Select(selector).Sum();
	}

	public static int? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
	{
		return source.Select(selector).Sum();
	}

	public static long Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
	{
		return source.Select(selector).Sum();
	}

	public static long? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
	{
		return source.Select(selector).Sum();
	}

	public static float Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
	{
		return source.Select(selector).Sum();
	}

	public static float? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
	{
		return source.Select(selector).Sum();
	}

	public static double Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
	{
		return source.Select(selector).Sum();
	}

	public static double? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
	{
		return source.Select(selector).Sum();
	}

	public static decimal Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
	{
		return source.Select(selector).Sum();
	}

	public static decimal? Sum<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
	{
		return source.Select(selector).Sum();
	}

	public static int Min(this ParallelQuery<int> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new IntMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static int? Min(this ParallelQuery<int?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableIntMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static long Min(this ParallelQuery<long> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new LongMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static long? Min(this ParallelQuery<long?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableLongMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static float Min(this ParallelQuery<float> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new FloatMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static float? Min(this ParallelQuery<float?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableFloatMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static double Min(this ParallelQuery<double> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DoubleMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static double? Min(this ParallelQuery<double?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDoubleMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static decimal Min(this ParallelQuery<decimal> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DecimalMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static decimal? Min(this ParallelQuery<decimal?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDecimalMinMaxAggregationOperator(source, -1).Aggregate();
	}

	public static TSource? Min<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return AggregationMinMaxHelpers<TSource>.ReduceMin(source);
	}

	public static int Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
	{
		return source.Select(selector).Min<int>();
	}

	public static int? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
	{
		return source.Select(selector).Min<int?>();
	}

	public static long Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
	{
		return source.Select(selector).Min<long>();
	}

	public static long? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
	{
		return source.Select(selector).Min<long?>();
	}

	public static float Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
	{
		return source.Select(selector).Min<float>();
	}

	public static float? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
	{
		return source.Select(selector).Min<float?>();
	}

	public static double Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
	{
		return source.Select(selector).Min<double>();
	}

	public static double? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
	{
		return source.Select(selector).Min<double?>();
	}

	public static decimal Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
	{
		return source.Select(selector).Min<decimal>();
	}

	public static decimal? Min<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
	{
		return source.Select(selector).Min<decimal?>();
	}

	public static TResult? Min<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
	{
		return source.Select(selector).Min();
	}

	public static int Max(this ParallelQuery<int> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new IntMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static int? Max(this ParallelQuery<int?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableIntMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static long Max(this ParallelQuery<long> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new LongMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static long? Max(this ParallelQuery<long?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableLongMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static float Max(this ParallelQuery<float> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new FloatMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static float? Max(this ParallelQuery<float?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableFloatMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static double Max(this ParallelQuery<double> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DoubleMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static double? Max(this ParallelQuery<double?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDoubleMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static decimal Max(this ParallelQuery<decimal> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DecimalMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static decimal? Max(this ParallelQuery<decimal?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDecimalMinMaxAggregationOperator(source, 1).Aggregate();
	}

	public static TSource? Max<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return AggregationMinMaxHelpers<TSource>.ReduceMax(source);
	}

	public static int Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
	{
		return source.Select(selector).Max<int>();
	}

	public static int? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
	{
		return source.Select(selector).Max<int?>();
	}

	public static long Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
	{
		return source.Select(selector).Max<long>();
	}

	public static long? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
	{
		return source.Select(selector).Max<long?>();
	}

	public static float Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
	{
		return source.Select(selector).Max<float>();
	}

	public static float? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
	{
		return source.Select(selector).Max<float?>();
	}

	public static double Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
	{
		return source.Select(selector).Max<double>();
	}

	public static double? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
	{
		return source.Select(selector).Max<double?>();
	}

	public static decimal Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
	{
		return source.Select(selector).Max<decimal>();
	}

	public static decimal? Max<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
	{
		return source.Select(selector).Max<decimal?>();
	}

	public static TResult? Max<TSource, TResult>(this ParallelQuery<TSource> source, Func<TSource, TResult> selector)
	{
		return source.Select(selector).Max();
	}

	public static double Average(this ParallelQuery<int> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new IntAverageAggregationOperator(source).Aggregate();
	}

	public static double? Average(this ParallelQuery<int?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableIntAverageAggregationOperator(source).Aggregate();
	}

	public static double Average(this ParallelQuery<long> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new LongAverageAggregationOperator(source).Aggregate();
	}

	public static double? Average(this ParallelQuery<long?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableLongAverageAggregationOperator(source).Aggregate();
	}

	public static float Average(this ParallelQuery<float> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new FloatAverageAggregationOperator(source).Aggregate();
	}

	public static float? Average(this ParallelQuery<float?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableFloatAverageAggregationOperator(source).Aggregate();
	}

	public static double Average(this ParallelQuery<double> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DoubleAverageAggregationOperator(source).Aggregate();
	}

	public static double? Average(this ParallelQuery<double?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDoubleAverageAggregationOperator(source).Aggregate();
	}

	public static decimal Average(this ParallelQuery<decimal> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DecimalAverageAggregationOperator(source).Aggregate();
	}

	public static decimal? Average(this ParallelQuery<decimal?> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new NullableDecimalAverageAggregationOperator(source).Aggregate();
	}

	public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, int> selector)
	{
		return source.Select(selector).Average();
	}

	public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, int?> selector)
	{
		return source.Select(selector).Average();
	}

	public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, long> selector)
	{
		return source.Select(selector).Average();
	}

	public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, long?> selector)
	{
		return source.Select(selector).Average();
	}

	public static float Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, float> selector)
	{
		return source.Select(selector).Average();
	}

	public static float? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, float?> selector)
	{
		return source.Select(selector).Average();
	}

	public static double Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, double> selector)
	{
		return source.Select(selector).Average();
	}

	public static double? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, double?> selector)
	{
		return source.Select(selector).Average();
	}

	public static decimal Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal> selector)
	{
		return source.Select(selector).Average();
	}

	public static decimal? Average<TSource>(this ParallelQuery<TSource> source, Func<TSource, decimal?> selector)
	{
		return source.Select(selector).Average();
	}

	public static bool Any<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new AnyAllSearchOperator<TSource>(source, qualification: true, predicate).Aggregate();
	}

	public static bool Any<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return source.Any((TSource x) => true);
	}

	public static bool All<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new AnyAllSearchOperator<TSource>(source, qualification: false, predicate).Aggregate();
	}

	public static bool Contains<TSource>(this ParallelQuery<TSource> source, TSource value)
	{
		return source.Contains(value, null);
	}

	public static bool Contains<TSource>(this ParallelQuery<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new ContainsSearchOperator<TSource>(source, value, comparer).Aggregate();
	}

	public static ParallelQuery<TSource> Take<TSource>(this ParallelQuery<TSource> source, int count)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (count > 0)
		{
			return new TakeOrSkipQueryOperator<TSource>(source, count, take: true);
		}
		return Empty<TSource>();
	}

	public static ParallelQuery<TSource> TakeWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new TakeOrSkipWhileQueryOperator<TSource>(source, predicate, null, take: true);
	}

	public static ParallelQuery<TSource> TakeWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new TakeOrSkipWhileQueryOperator<TSource>(source, null, predicate, take: true);
	}

	public static ParallelQuery<TSource> Skip<TSource>(this ParallelQuery<TSource> source, int count)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (count <= 0)
		{
			return source;
		}
		return new TakeOrSkipQueryOperator<TSource>(source, count, take: false);
	}

	public static ParallelQuery<TSource> SkipWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new TakeOrSkipWhileQueryOperator<TSource>(source, predicate, null, take: false);
	}

	public static ParallelQuery<TSource> SkipWhile<TSource>(this ParallelQuery<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return new TakeOrSkipWhileQueryOperator<TSource>(source, null, predicate, take: false);
	}

	public static ParallelQuery<TSource> Concat<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		return new ConcatQueryOperator<TSource>(first, second);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Concat<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		return first.SequenceEqual(second, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		comparer = comparer ?? EqualityComparer<TSource>.Default;
		QueryOperator<TSource> queryOperator = QueryOperator<TSource>.AsQueryOperator(first);
		QueryOperator<TSource> queryOperator2 = QueryOperator<TSource>.AsQueryOperator(second);
		QuerySettings querySettings = queryOperator.SpecifiedQuerySettings.Merge(queryOperator2.SpecifiedQuerySettings).WithDefaults().WithPerExecutionSettings(new CancellationTokenSource(), new Shared<bool>(value: false));
		IEnumerator<TSource> enumerator = first.GetEnumerator();
		try
		{
			IEnumerator<TSource> enumerator2 = second.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator2.MoveNext() || !comparer.Equals(enumerator.Current, enumerator2.Current))
					{
						return false;
					}
				}
				if (enumerator2.MoveNext())
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				ExceptionAggregator.ThrowOCEorAggregateException(ex, querySettings.CancellationState);
			}
			finally
			{
				DisposeEnumerator(enumerator2, querySettings.CancellationState);
			}
		}
		finally
		{
			DisposeEnumerator(enumerator, querySettings.CancellationState);
		}
		return true;
	}

	private static void DisposeEnumerator<TSource>(IEnumerator<TSource> e, CancellationState cancelState)
	{
		try
		{
			e.Dispose();
		}
		catch (Exception ex)
		{
			ExceptionAggregator.ThrowOCEorAggregateException(ex, cancelState);
		}
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static bool SequenceEqual<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Distinct<TSource>(this ParallelQuery<TSource> source)
	{
		return source.Distinct(null);
	}

	public static ParallelQuery<TSource> Distinct<TSource>(this ParallelQuery<TSource> source, IEqualityComparer<TSource>? comparer)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DistinctQueryOperator<TSource>(source, comparer);
	}

	public static ParallelQuery<TSource> Union<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
	{
		return first.Union(second, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Union<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Union<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		return new UnionQueryOperator<TSource>(first, second, comparer);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Union<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Intersect<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
	{
		return first.Intersect(second, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Intersect<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Intersect<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		return new IntersectQueryOperator<TSource>(first, second, comparer);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Intersect<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Except<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second)
	{
		return first.Except(second, null);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Except<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static ParallelQuery<TSource> Except<TSource>(this ParallelQuery<TSource> first, ParallelQuery<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			throw new ArgumentNullException("first");
		}
		if (second == null)
		{
			throw new ArgumentNullException("second");
		}
		return new ExceptQueryOperator<TSource>(first, second, comparer);
	}

	[Obsolete("The second data source of a binary operator must be of type System.Linq.ParallelQuery<T> rather than System.Collections.Generic.IEnumerable<T>. To fix this problem, use the AsParallel() extension method to convert the right data source to System.Linq.ParallelQuery<T>.")]
	public static ParallelQuery<TSource> Except<TSource>(this ParallelQuery<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		throw new NotSupportedException(System.SR.ParallelEnumerable_BinaryOpMustUseAsParallel);
	}

	public static IEnumerable<TSource> AsEnumerable<TSource>(this ParallelQuery<TSource> source)
	{
		return source.AsSequential();
	}

	public static TSource[] ToArray<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (source is QueryOperator<TSource> queryOperator)
		{
			return queryOperator.ExecuteAndGetResultsAsArray();
		}
		return Enumerable.ToArray(source.ToList());
	}

	public static List<TSource> ToList<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		List<TSource> list = new List<TSource>();
		IEnumerator<TSource> enumerator;
		if (source is QueryOperator<TSource> queryOperator)
		{
			if (queryOperator.OrdinalIndexState == OrdinalIndexState.Indexable && queryOperator.OutputOrdered)
			{
				return new List<TSource>(source.ToArray());
			}
			enumerator = queryOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered);
		}
		else
		{
			enumerator = source.GetEnumerator();
		}
		using (enumerator)
		{
			while (enumerator.MoveNext())
			{
				list.Add(enumerator.Current);
			}
			return list;
		}
	}

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
	{
		return source.ToDictionary(keySelector, EqualityComparer<TKey>.Default);
	}

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		Dictionary<TKey, TSource> dictionary = new Dictionary<TKey, TSource>(comparer);
		IEnumerator<TSource> enumerator = ((!(source is QueryOperator<TSource> queryOperator)) ? source.GetEnumerator() : queryOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true));
		using (enumerator)
		{
			while (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				try
				{
					TKey key = keySelector(current);
					dictionary.Add(key, current);
				}
				catch (Exception ex)
				{
					throw new AggregateException(ex);
				}
			}
			return dictionary;
		}
	}

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
	{
		return source.ToDictionary(keySelector, elementSelector, EqualityComparer<TKey>.Default);
	}

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		if (elementSelector == null)
		{
			throw new ArgumentNullException("elementSelector");
		}
		Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(comparer);
		IEnumerator<TSource> enumerator = ((!(source is QueryOperator<TSource> queryOperator)) ? source.GetEnumerator() : queryOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered, suppressOrderPreservation: true));
		using (enumerator)
		{
			while (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				try
				{
					dictionary.Add(keySelector(current), elementSelector(current));
				}
				catch (Exception ex)
				{
					throw new AggregateException(ex);
				}
			}
			return dictionary;
		}
	}

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
	{
		return source.ToLookup(keySelector, EqualityComparer<TKey>.Default);
	}

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		comparer = comparer ?? EqualityComparer<TKey>.Default;
		ParallelQuery<IGrouping<TKey, TSource>> parallelQuery = source.GroupBy(keySelector, comparer);
		System.Linq.Parallel.Lookup<TKey, TSource> lookup = new System.Linq.Parallel.Lookup<TKey, TSource>(comparer);
		IEnumerator<IGrouping<TKey, TSource>> enumerator = ((!(parallelQuery is QueryOperator<IGrouping<TKey, TSource>> queryOperator)) ? parallelQuery.GetEnumerator() : queryOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered));
		using (enumerator)
		{
			while (enumerator.MoveNext())
			{
				lookup.Add(enumerator.Current);
			}
			return lookup;
		}
	}

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
	{
		return source.ToLookup(keySelector, elementSelector, EqualityComparer<TKey>.Default);
	}

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this ParallelQuery<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (keySelector == null)
		{
			throw new ArgumentNullException("keySelector");
		}
		if (elementSelector == null)
		{
			throw new ArgumentNullException("elementSelector");
		}
		comparer = comparer ?? EqualityComparer<TKey>.Default;
		ParallelQuery<IGrouping<TKey, TElement>> parallelQuery = source.GroupBy(keySelector, elementSelector, comparer);
		System.Linq.Parallel.Lookup<TKey, TElement> lookup = new System.Linq.Parallel.Lookup<TKey, TElement>(comparer);
		IEnumerator<IGrouping<TKey, TElement>> enumerator = ((!(parallelQuery is QueryOperator<IGrouping<TKey, TElement>> queryOperator)) ? parallelQuery.GetEnumerator() : queryOperator.GetEnumerator(ParallelMergeOptions.FullyBuffered));
		using (enumerator)
		{
			while (enumerator.MoveNext())
			{
				lookup.Add(enumerator.Current);
			}
			return lookup;
		}
	}

	public static ParallelQuery<TSource> Reverse<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new ReverseQueryOperator<TSource>(source);
	}

	public static ParallelQuery<TResult> OfType<TResult>(this ParallelQuery source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return source.OfType<TResult>();
	}

	public static ParallelQuery<TResult> Cast<TResult>(this ParallelQuery source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return source.Cast<TResult>();
	}

	private static TSource GetOneWithPossibleDefault<TSource>(QueryOperator<TSource> queryOp, bool throwIfTwo, bool defaultIfEmpty)
	{
		using (IEnumerator<TSource> enumerator = queryOp.GetEnumerator(ParallelMergeOptions.FullyBuffered))
		{
			if (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				if (throwIfTwo && enumerator.MoveNext())
				{
					throw new InvalidOperationException(System.SR.MoreThanOneMatch);
				}
				return current;
			}
		}
		if (defaultIfEmpty)
		{
			return default(TSource);
		}
		throw new InvalidOperationException(System.SR.NoElements);
	}

	public static TSource First<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		FirstQueryOperator<TSource> firstQueryOperator = new FirstQueryOperator<TSource>(source, null);
		QuerySettings querySettings = firstQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (firstQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = firstQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).First();
		}
		return GetOneWithPossibleDefault(firstQueryOperator, throwIfTwo: false, defaultIfEmpty: false);
	}

	public static TSource First<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		FirstQueryOperator<TSource> firstQueryOperator = new FirstQueryOperator<TSource>(source, predicate);
		QuerySettings querySettings = firstQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (firstQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = firstQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).First(ExceptionAggregator.WrapFunc(predicate, querySettings.CancellationState));
		}
		return GetOneWithPossibleDefault(firstQueryOperator, throwIfTwo: false, defaultIfEmpty: false);
	}

	public static TSource? FirstOrDefault<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		FirstQueryOperator<TSource> firstQueryOperator = new FirstQueryOperator<TSource>(source, null);
		QuerySettings querySettings = firstQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (firstQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = firstQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).FirstOrDefault();
		}
		return GetOneWithPossibleDefault(firstQueryOperator, throwIfTwo: false, defaultIfEmpty: true);
	}

	public static TSource? FirstOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		FirstQueryOperator<TSource> firstQueryOperator = new FirstQueryOperator<TSource>(source, predicate);
		QuerySettings querySettings = firstQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (firstQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = firstQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).FirstOrDefault(ExceptionAggregator.WrapFunc(predicate, querySettings.CancellationState));
		}
		return GetOneWithPossibleDefault(firstQueryOperator, throwIfTwo: false, defaultIfEmpty: true);
	}

	public static TSource Last<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		LastQueryOperator<TSource> lastQueryOperator = new LastQueryOperator<TSource>(source, null);
		QuerySettings querySettings = lastQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (lastQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = lastQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).Last();
		}
		return GetOneWithPossibleDefault(lastQueryOperator, throwIfTwo: false, defaultIfEmpty: false);
	}

	public static TSource Last<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		LastQueryOperator<TSource> lastQueryOperator = new LastQueryOperator<TSource>(source, predicate);
		QuerySettings querySettings = lastQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (lastQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = lastQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).Last(ExceptionAggregator.WrapFunc(predicate, querySettings.CancellationState));
		}
		return GetOneWithPossibleDefault(lastQueryOperator, throwIfTwo: false, defaultIfEmpty: false);
	}

	public static TSource? LastOrDefault<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		LastQueryOperator<TSource> lastQueryOperator = new LastQueryOperator<TSource>(source, null);
		QuerySettings querySettings = lastQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (lastQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = lastQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).LastOrDefault();
		}
		return GetOneWithPossibleDefault(lastQueryOperator, throwIfTwo: false, defaultIfEmpty: true);
	}

	public static TSource? LastOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		LastQueryOperator<TSource> lastQueryOperator = new LastQueryOperator<TSource>(source, predicate);
		QuerySettings querySettings = lastQueryOperator.SpecifiedQuerySettings.WithDefaults();
		if (lastQueryOperator.LimitsParallelism && querySettings.ExecutionMode != ParallelExecutionMode.ForceParallelism)
		{
			IEnumerable<TSource> source2 = lastQueryOperator.Child.AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			IEnumerable<TSource> source3 = CancellableEnumerable.Wrap(source2, querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source3, querySettings.CancellationState).LastOrDefault(ExceptionAggregator.WrapFunc(predicate, querySettings.CancellationState));
		}
		return GetOneWithPossibleDefault(lastQueryOperator, throwIfTwo: false, defaultIfEmpty: true);
	}

	public static TSource Single<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, null), throwIfTwo: true, defaultIfEmpty: false);
	}

	public static TSource Single<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, predicate), throwIfTwo: true, defaultIfEmpty: false);
	}

	public static TSource? SingleOrDefault<TSource>(this ParallelQuery<TSource> source)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, null), throwIfTwo: true, defaultIfEmpty: true);
	}

	public static TSource? SingleOrDefault<TSource>(this ParallelQuery<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (predicate == null)
		{
			throw new ArgumentNullException("predicate");
		}
		return GetOneWithPossibleDefault(new SingleQueryOperator<TSource>(source, predicate), throwIfTwo: true, defaultIfEmpty: true);
	}

	public static ParallelQuery<TSource?> DefaultIfEmpty<TSource>(this ParallelQuery<TSource> source)
	{
		return source.DefaultIfEmpty(default(TSource));
	}

	public static ParallelQuery<TSource> DefaultIfEmpty<TSource>(this ParallelQuery<TSource> source, TSource defaultValue)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		return new DefaultIfEmptyQueryOperator<TSource>(source, defaultValue);
	}

	public static TSource ElementAt<TSource>(this ParallelQuery<TSource> source, int index)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		ElementAtQueryOperator<TSource> elementAtQueryOperator = new ElementAtQueryOperator<TSource>(source, index);
		if (elementAtQueryOperator.Aggregate(out var result, withDefaultValue: false))
		{
			return result;
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public static TSource? ElementAtOrDefault<TSource>(this ParallelQuery<TSource> source, int index)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (index >= 0)
		{
			ElementAtQueryOperator<TSource> elementAtQueryOperator = new ElementAtQueryOperator<TSource>(source, index);
			if (elementAtQueryOperator.Aggregate(out var result, withDefaultValue: true))
			{
				return result;
			}
		}
		return default(TSource);
	}
}
