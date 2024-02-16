using System.Collections.Generic;

namespace System.Data;

public static class TypedTableBaseExtensions
{
	public static EnumerableRowCollection<TRow> Where<TRow>(this TypedTableBase<TRow> source, Func<TRow, bool> predicate) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.Where(predicate);
	}

	public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.OrderBy(keySelector);
	}

	public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.OrderBy(keySelector, comparer);
	}

	public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.OrderByDescending(keySelector);
	}

	public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.OrderByDescending(keySelector, comparer);
	}

	public static EnumerableRowCollection<S> Select<TRow, S>(this TypedTableBase<TRow> source, Func<TRow, S> selector) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		EnumerableRowCollection<TRow> source2 = new EnumerableRowCollection<TRow>(source);
		return source2.Select(selector);
	}

	public static EnumerableRowCollection<TRow> AsEnumerable<TRow>(this TypedTableBase<TRow> source) where TRow : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		return new EnumerableRowCollection<TRow>(source);
	}

	public static TRow? ElementAtOrDefault<TRow>(this TypedTableBase<TRow> source, int index) where TRow : DataRow
	{
		if (index >= 0 && index < source.Rows.Count)
		{
			return (TRow)source.Rows[index];
		}
		return null;
	}
}
