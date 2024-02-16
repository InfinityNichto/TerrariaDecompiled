using System.Collections.Generic;
using System.Globalization;

namespace System.Data;

public static class DataTableExtensions
{
	public static EnumerableRowCollection<DataRow> AsEnumerable(this DataTable source)
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		return new EnumerableRowCollection<DataRow>(source);
	}

	public static DataTable CopyToDataTable<T>(this IEnumerable<T> source) where T : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		return LoadTableFromEnumerable(source, null, null, null);
	}

	public static void CopyToDataTable<T>(this IEnumerable<T> source, DataTable table, LoadOption options) where T : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		DataSetUtil.CheckArgumentNull(table, "table");
		LoadTableFromEnumerable(source, table, options, null);
	}

	public static void CopyToDataTable<T>(this IEnumerable<T> source, DataTable table, LoadOption options, FillErrorEventHandler? errorHandler) where T : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		DataSetUtil.CheckArgumentNull(table, "table");
		LoadTableFromEnumerable(source, table, options, errorHandler);
	}

	private static DataTable LoadTableFromEnumerable<T>(IEnumerable<T> source, DataTable table, LoadOption? options, FillErrorEventHandler errorHandler) where T : DataRow
	{
		if (options.HasValue)
		{
			LoadOption value = options.Value;
			if ((uint)(value - 1) > 2u)
			{
				throw DataSetUtil.InvalidLoadOption(options.Value);
			}
		}
		using (IEnumerator<T> enumerator = source.GetEnumerator())
		{
			if (!enumerator.MoveNext())
			{
				return table ?? throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_EmptyDataRowSource);
			}
			if (table == null)
			{
				DataRow current = enumerator.Current;
				if (current == null)
				{
					throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_NullDataRow);
				}
				table = new DataTable
				{
					Locale = CultureInfo.CurrentCulture
				};
				foreach (DataColumn column in current.Table.Columns)
				{
					table.Columns.Add(column.ColumnName, column.DataType);
				}
			}
			table.BeginLoadData();
			try
			{
				do
				{
					DataRow current = enumerator.Current;
					if (current == null)
					{
						continue;
					}
					object[] values = null;
					try
					{
						switch (current.RowState)
						{
						case DataRowState.Detached:
							if (!current.HasVersion(DataRowVersion.Proposed))
							{
								throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_CannotLoadDetachedRow);
							}
							goto case DataRowState.Unchanged;
						case DataRowState.Unchanged:
						case DataRowState.Added:
						case DataRowState.Modified:
							values = current.ItemArray;
							if (options.HasValue)
							{
								table.LoadDataRow(values, options.Value);
							}
							else
							{
								table.LoadDataRow(values, fAcceptChanges: true);
							}
							break;
						case DataRowState.Deleted:
							throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_CannotLoadDeletedRow);
						default:
							throw DataSetUtil.InvalidDataRowState(current.RowState);
						}
					}
					catch (Exception ex)
					{
						if (!DataSetUtil.IsCatchableExceptionType(ex))
						{
							throw;
						}
						FillErrorEventArgs fillErrorEventArgs = null;
						if (errorHandler != null)
						{
							fillErrorEventArgs = new FillErrorEventArgs(table, values)
							{
								Errors = ex
							};
							errorHandler(enumerator, fillErrorEventArgs);
						}
						if (fillErrorEventArgs == null)
						{
							throw;
						}
						if (!fillErrorEventArgs.Continue)
						{
							if ((fillErrorEventArgs.Errors ?? ex) == ex)
							{
								throw;
							}
							throw fillErrorEventArgs.Errors;
						}
					}
				}
				while (enumerator.MoveNext());
			}
			finally
			{
				table.EndLoadData();
			}
		}
		return table;
	}

	public static DataView AsDataView(this DataTable table)
	{
		DataSetUtil.CheckArgumentNull(table, "table");
		return new LinqDataView(table, null);
	}

	public static DataView AsDataView<T>(this EnumerableRowCollection<T> source) where T : DataRow
	{
		DataSetUtil.CheckArgumentNull(source, "source");
		return source.GetLinqDataView();
	}
}
