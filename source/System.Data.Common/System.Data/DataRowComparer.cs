using System.Collections.Generic;

namespace System.Data;

public static class DataRowComparer
{
	public static DataRowComparer<DataRow> Default => DataRowComparer<DataRow>.Default;

	internal static bool AreEqual(object a, object b)
	{
		if (a == b)
		{
			return true;
		}
		if (a == null || a == DBNull.Value || b == null || b == DBNull.Value)
		{
			return false;
		}
		if (!a.Equals(b))
		{
			if (a.GetType().IsArray)
			{
				return CompareArray((Array)a, b as Array);
			}
			return false;
		}
		return true;
	}

	private static bool AreElementEqual(object a, object b)
	{
		if (a == b)
		{
			return true;
		}
		if (a == null || a == DBNull.Value || b == null || b == DBNull.Value)
		{
			return false;
		}
		return a.Equals(b);
	}

	private static bool CompareArray(Array a, Array b)
	{
		if (b == null || 1 != a.Rank || 1 != b.Rank || a.Length != b.Length)
		{
			return false;
		}
		int num = a.GetLowerBound(0);
		int num2 = b.GetLowerBound(0);
		if (a.GetType() == b.GetType() && num == 0 && num2 == 0)
		{
			switch (Type.GetTypeCode(a.GetType().GetElementType()))
			{
			case TypeCode.Byte:
				return CompareEquatableArray((byte[])a, (byte[])b);
			case TypeCode.Int16:
				return CompareEquatableArray((short[])a, (short[])b);
			case TypeCode.Int32:
				return CompareEquatableArray((int[])a, (int[])b);
			case TypeCode.Int64:
				return CompareEquatableArray((long[])a, (long[])b);
			case TypeCode.String:
				return CompareEquatableArray((string[])a, (string[])b);
			}
		}
		int num3 = num + a.Length;
		while (num < num3)
		{
			if (!AreElementEqual(a.GetValue(num), b.GetValue(num2)))
			{
				return false;
			}
			num++;
			num2++;
		}
		return true;
	}

	private static bool CompareEquatableArray<TElem>(TElem[] a, TElem[] b) where TElem : IEquatable<TElem>
	{
		for (int num = 0; num < a.Length; num++)
		{
			ref TElem reference = ref a[num];
			TElem val = default(TElem);
			bool num2;
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					num2 = b[num] != null;
					goto IL_0054;
				}
			}
			num2 = !reference.Equals(b[num]);
			goto IL_0054;
			IL_0054:
			if (num2)
			{
				return false;
			}
		}
		return true;
	}
}
public sealed class DataRowComparer<TRow> : IEqualityComparer<TRow> where TRow : DataRow
{
	private static readonly DataRowComparer<TRow> s_instance = new DataRowComparer<TRow>();

	public static DataRowComparer<TRow> Default => s_instance;

	private DataRowComparer()
	{
	}

	public bool Equals(TRow? leftRow, TRow? rightRow)
	{
		if (leftRow == rightRow)
		{
			return true;
		}
		if (leftRow == null || rightRow == null)
		{
			return false;
		}
		if (leftRow.RowState == DataRowState.Deleted || rightRow.RowState == DataRowState.Deleted)
		{
			throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_CannotCompareDeletedRow);
		}
		int count = leftRow.Table.Columns.Count;
		if (count != rightRow.Table.Columns.Count)
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			if (!DataRowComparer.AreEqual(leftRow[i], rightRow[i]))
			{
				return false;
			}
		}
		return true;
	}

	public int GetHashCode(TRow row)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		if (row.RowState == DataRowState.Deleted)
		{
			throw DataSetUtil.InvalidOperation(System.SR.DataSetLinq_CannotCompareDeletedRow);
		}
		int result = 0;
		if (row.Table.Columns.Count > 0)
		{
			object obj = row[0];
			Type type = obj.GetType();
			if (!type.IsArray)
			{
				result = ((!(obj is ValueType valueType)) ? obj.GetHashCode() : valueType.GetHashCode());
			}
			else
			{
				Array array = (Array)obj;
				if (array.Rank > 1)
				{
					result = obj.GetHashCode();
				}
				else if (array.Length > 0)
				{
					result = array.GetValue(array.GetLowerBound(0)).GetHashCode();
				}
			}
		}
		return result;
	}
}
