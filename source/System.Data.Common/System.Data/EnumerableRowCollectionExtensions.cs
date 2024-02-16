using System.Collections.Generic;
using System.Linq;

namespace System.Data;

public static class EnumerableRowCollectionExtensions
{
	public static EnumerableRowCollection<TRow> Where<TRow>(this EnumerableRowCollection<TRow> source, Func<TRow, bool> predicate)
	{
		EnumerableRowCollection<TRow> enumerableRowCollection = new EnumerableRowCollection<TRow>(source, Enumerable.Where(source, predicate), null);
		enumerableRowCollection.AddPredicate(predicate);
		return enumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
	{
		IEnumerable<TRow> enumerableRows = Enumerable.OrderBy(source, keySelector);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, isDescending: false, isOrderBy: true);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
	{
		IEnumerable<TRow> enumerableRows = Enumerable.OrderBy(source, keySelector, comparer);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, comparer, isDescending: false, isOrderBy: true);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
	{
		IEnumerable<TRow> enumerableRows = Enumerable.OrderByDescending(source, keySelector);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, isDescending: true, isOrderBy: true);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
	{
		IEnumerable<TRow> enumerableRows = Enumerable.OrderByDescending(source, keySelector, comparer);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, comparer, isDescending: true, isOrderBy: true);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
	{
		IEnumerable<TRow> enumerableRows = ((IOrderedEnumerable<TRow>)source.EnumerableRows).ThenBy(keySelector);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, isDescending: false, isOrderBy: false);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
	{
		IEnumerable<TRow> enumerableRows = ((IOrderedEnumerable<TRow>)source.EnumerableRows).ThenBy(keySelector, comparer);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, comparer, isDescending: false, isOrderBy: false);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
	{
		IEnumerable<TRow> enumerableRows = ((IOrderedEnumerable<TRow>)source.EnumerableRows).ThenByDescending(keySelector);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, isDescending: true, isOrderBy: false);
		return orderedEnumerableRowCollection;
	}

	public static OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
	{
		IEnumerable<TRow> enumerableRows = ((IOrderedEnumerable<TRow>)source.EnumerableRows).ThenByDescending(keySelector, comparer);
		OrderedEnumerableRowCollection<TRow> orderedEnumerableRowCollection = new OrderedEnumerableRowCollection<TRow>(source, enumerableRows);
		orderedEnumerableRowCollection.AddSortExpression(keySelector, comparer, isDescending: true, isOrderBy: false);
		return orderedEnumerableRowCollection;
	}

	public static EnumerableRowCollection<S> Select<TRow, S>(this EnumerableRowCollection<TRow> source, Func<TRow, S> selector)
	{
		IEnumerable<S> enumerableRows = Enumerable.Select(source, selector);
		return new EnumerableRowCollection<S>(source as EnumerableRowCollection<S>, enumerableRows, selector as Func<S, S>);
	}

	public static EnumerableRowCollection<TResult> Cast<TResult>(this EnumerableRowCollection source)
	{
		if (source != null && source.ElementType.Equals(typeof(TResult)))
		{
			return (EnumerableRowCollection<TResult>)source;
		}
		IEnumerable<TResult> enumerableRows = Enumerable.Cast<TResult>(source);
		return new EnumerableRowCollection<TResult>(enumerableRows, typeof(TResult).IsAssignableFrom(source.ElementType) && typeof(DataRow).IsAssignableFrom(typeof(TResult)), source.Table);
	}
}
