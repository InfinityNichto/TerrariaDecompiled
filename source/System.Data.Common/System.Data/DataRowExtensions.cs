using System.Reflection;

namespace System.Data;

public static class DataRowExtensions
{
	private static class UnboxT<T>
	{
		internal static readonly Converter<object, T> s_unbox = Create();

		private static Converter<object, T> Create()
		{
			if (typeof(T).IsValueType)
			{
				if (!typeof(T).IsGenericType || !(typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)))
				{
					return ValueField;
				}
				return (Converter<object, T>)Delegate.CreateDelegate(typeof(Converter<object, T>), typeof(UnboxT<T>).GetMethod("NullableField", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeof(T).GetGenericArguments()[0]));
			}
			return ReferenceField;
		}

		private static T ReferenceField(object value)
		{
			if (value != DBNull.Value)
			{
				return (T)value;
			}
			return default(T);
		}

		private static T ValueField(object value)
		{
			if (value != DBNull.Value)
			{
				return (T)value;
			}
			throw DataSetUtil.InvalidCast(System.SR.Format(System.SR.DataSetLinq_NonNullableCast, typeof(T)));
		}

		private static TElem? NullableField<TElem>(object value) where TElem : struct
		{
			if (value != DBNull.Value)
			{
				return (TElem)value;
			}
			return null;
		}
	}

	public static T? Field<T>(this DataRow row, string columnName)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnName]);
	}

	public static T? Field<T>(this DataRow row, DataColumn column)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[column]);
	}

	public static T? Field<T>(this DataRow row, int columnIndex)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnIndex]);
	}

	public static T? Field<T>(this DataRow row, int columnIndex, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnIndex, version]);
	}

	public static T? Field<T>(this DataRow row, string columnName, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[columnName, version]);
	}

	public static T? Field<T>(this DataRow row, DataColumn column, DataRowVersion version)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		return UnboxT<T>.s_unbox(row[column, version]);
	}

	public static void SetField<T>(this DataRow row, int columnIndex, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[columnIndex] = ((object)value) ?? DBNull.Value;
	}

	public static void SetField<T>(this DataRow row, string columnName, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[columnName] = ((object)value) ?? DBNull.Value;
	}

	public static void SetField<T>(this DataRow row, DataColumn column, T? value)
	{
		DataSetUtil.CheckArgumentNull(row, "row");
		row[column] = ((object)value) ?? DBNull.Value;
	}
}
