using System.Collections.Generic;

namespace System.Data;

public sealed class OrderedEnumerableRowCollection<TRow> : EnumerableRowCollection<TRow>
{
	internal OrderedEnumerableRowCollection(EnumerableRowCollection<TRow> enumerableTable, IEnumerable<TRow> enumerableRows)
		: base(enumerableTable, enumerableRows, (Func<TRow, TRow>)null)
	{
	}
}
