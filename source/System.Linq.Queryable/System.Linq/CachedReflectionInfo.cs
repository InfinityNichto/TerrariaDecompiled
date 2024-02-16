using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod", Justification = "The methods passed into MakeGenericMethod do not contain trim annotations.")]
internal static class CachedReflectionInfo
{
	private static MethodInfo s_Aggregate_TSource_2;

	private static MethodInfo s_Aggregate_TSource_TAccumulate_3;

	private static MethodInfo s_Aggregate_TSource_TAccumulate_TResult_4;

	private static MethodInfo s_All_TSource_2;

	private static MethodInfo s_Any_TSource_1;

	private static MethodInfo s_Any_TSource_2;

	private static MethodInfo s_Average_Int32_1;

	private static MethodInfo s_Average_NullableInt32_1;

	private static MethodInfo s_Average_Int64_1;

	private static MethodInfo s_Average_NullableInt64_1;

	private static MethodInfo s_Average_Single_1;

	private static MethodInfo s_Average_NullableSingle_1;

	private static MethodInfo s_Average_Double_1;

	private static MethodInfo s_Average_NullableDouble_1;

	private static MethodInfo s_Average_Decimal_1;

	private static MethodInfo s_Average_NullableDecimal_1;

	private static MethodInfo s_Average_Int32_TSource_2;

	private static MethodInfo s_Average_NullableInt32_TSource_2;

	private static MethodInfo s_Average_Single_TSource_2;

	private static MethodInfo s_Average_NullableSingle_TSource_2;

	private static MethodInfo s_Average_Int64_TSource_2;

	private static MethodInfo s_Average_NullableInt64_TSource_2;

	private static MethodInfo s_Average_Double_TSource_2;

	private static MethodInfo s_Average_NullableDouble_TSource_2;

	private static MethodInfo s_Average_Decimal_TSource_2;

	private static MethodInfo s_Average_NullableDecimal_TSource_2;

	private static MethodInfo s_Cast_TResult_1;

	private static MethodInfo s_Chunk_TSource_1;

	private static MethodInfo s_Concat_TSource_2;

	private static MethodInfo s_Contains_TSource_2;

	private static MethodInfo s_Contains_TSource_3;

	private static MethodInfo s_Count_TSource_1;

	private static MethodInfo s_Count_TSource_2;

	private static MethodInfo s_DefaultIfEmpty_TSource_1;

	private static MethodInfo s_DefaultIfEmpty_TSource_2;

	private static MethodInfo s_Distinct_TSource_1;

	private static MethodInfo s_Distinct_TSource_2;

	private static MethodInfo s_DistinctBy_TSource_TKey_2;

	private static MethodInfo s_DistinctBy_TSource_TKey_3;

	private static MethodInfo s_ElementAt_Int32_TSource_2;

	private static MethodInfo s_ElementAt_Index_TSource_2;

	private static MethodInfo s_ElementAtOrDefault_Int32_TSource_2;

	private static MethodInfo s_ElementAtOrDefault_Index_TSource_2;

	private static MethodInfo s_Except_TSource_2;

	private static MethodInfo s_Except_TSource_3;

	private static MethodInfo s_ExceptBy_TSource_TKey_3;

	private static MethodInfo s_ExceptBy_TSource_TKey_4;

	private static MethodInfo s_First_TSource_1;

	private static MethodInfo s_First_TSource_2;

	private static MethodInfo s_FirstOrDefault_TSource_1;

	private static MethodInfo s_FirstOrDefault_TSource_2;

	private static MethodInfo s_FirstOrDefault_TSource_3;

	private static MethodInfo s_FirstOrDefault_TSource_4;

	private static MethodInfo s_GroupBy_TSource_TKey_2;

	private static MethodInfo s_GroupBy_TSource_TKey_3;

	private static MethodInfo s_GroupBy_TSource_TKey_TElement_3;

	private static MethodInfo s_GroupBy_TSource_TKey_TElement_4;

	private static MethodInfo s_GroupBy_TSource_TKey_TResult_3;

	private static MethodInfo s_GroupBy_TSource_TKey_TResult_4;

	private static MethodInfo s_GroupBy_TSource_TKey_TElement_TResult_4;

	private static MethodInfo s_GroupBy_TSource_TKey_TElement_TResult_5;

	private static MethodInfo s_GroupJoin_TOuter_TInner_TKey_TResult_5;

	private static MethodInfo s_GroupJoin_TOuter_TInner_TKey_TResult_6;

	private static MethodInfo s_Intersect_TSource_2;

	private static MethodInfo s_Intersect_TSource_3;

	private static MethodInfo s_IntersectBy_TSource_TKey_3;

	private static MethodInfo s_IntersectBy_TSource_TKey_4;

	private static MethodInfo s_Join_TOuter_TInner_TKey_TResult_5;

	private static MethodInfo s_Join_TOuter_TInner_TKey_TResult_6;

	private static MethodInfo s_Last_TSource_1;

	private static MethodInfo s_Last_TSource_2;

	private static MethodInfo s_LastOrDefault_TSource_1;

	private static MethodInfo s_LastOrDefault_TSource_2;

	private static MethodInfo s_LastOrDefault_TSource_3;

	private static MethodInfo s_LastOrDefault_TSource_4;

	private static MethodInfo s_LongCount_TSource_1;

	private static MethodInfo s_LongCount_TSource_2;

	private static MethodInfo s_Max_TSource_1;

	private static MethodInfo s_Max_TSource_2;

	private static MethodInfo s_Max_TSource_TResult_2;

	private static MethodInfo s_MaxBy_TSource_TKey_2;

	private static MethodInfo s_MaxBy_TSource_TKey_3;

	private static MethodInfo s_Min_TSource_1;

	private static MethodInfo s_Min_TSource_2;

	private static MethodInfo s_Min_TSource_TResult_2;

	private static MethodInfo s_MinBy_TSource_TKey_2;

	private static MethodInfo s_MinBy_TSource_TKey_3;

	private static MethodInfo s_OfType_TResult_1;

	private static MethodInfo s_OrderBy_TSource_TKey_2;

	private static MethodInfo s_OrderBy_TSource_TKey_3;

	private static MethodInfo s_OrderByDescending_TSource_TKey_2;

	private static MethodInfo s_OrderByDescending_TSource_TKey_3;

	private static MethodInfo s_Reverse_TSource_1;

	private static MethodInfo s_Select_TSource_TResult_2;

	private static MethodInfo s_Select_Index_TSource_TResult_2;

	private static MethodInfo s_SelectMany_TSource_TResult_2;

	private static MethodInfo s_SelectMany_Index_TSource_TResult_2;

	private static MethodInfo s_SelectMany_Index_TSource_TCollection_TResult_3;

	private static MethodInfo s_SelectMany_TSource_TCollection_TResult_3;

	private static MethodInfo s_SequenceEqual_TSource_2;

	private static MethodInfo s_SequenceEqual_TSource_3;

	private static MethodInfo s_Single_TSource_1;

	private static MethodInfo s_Single_TSource_2;

	private static MethodInfo s_SingleOrDefault_TSource_1;

	private static MethodInfo s_SingleOrDefault_TSource_2;

	private static MethodInfo s_SingleOrDefault_TSource_3;

	private static MethodInfo s_SingleOrDefault_TSource_4;

	private static MethodInfo s_Skip_TSource_2;

	private static MethodInfo s_SkipWhile_TSource_2;

	private static MethodInfo s_SkipWhile_Index_TSource_2;

	private static MethodInfo s_Sum_Int32_1;

	private static MethodInfo s_Sum_NullableInt32_1;

	private static MethodInfo s_Sum_Int64_1;

	private static MethodInfo s_Sum_NullableInt64_1;

	private static MethodInfo s_Sum_Single_1;

	private static MethodInfo s_Sum_NullableSingle_1;

	private static MethodInfo s_Sum_Double_1;

	private static MethodInfo s_Sum_NullableDouble_1;

	private static MethodInfo s_Sum_Decimal_1;

	private static MethodInfo s_Sum_NullableDecimal_1;

	private static MethodInfo s_Sum_NullableDecimal_TSource_2;

	private static MethodInfo s_Sum_Int32_TSource_2;

	private static MethodInfo s_Sum_NullableInt32_TSource_2;

	private static MethodInfo s_Sum_Int64_TSource_2;

	private static MethodInfo s_Sum_NullableInt64_TSource_2;

	private static MethodInfo s_Sum_Single_TSource_2;

	private static MethodInfo s_Sum_NullableSingle_TSource_2;

	private static MethodInfo s_Sum_Double_TSource_2;

	private static MethodInfo s_Sum_NullableDouble_TSource_2;

	private static MethodInfo s_Sum_Decimal_TSource_2;

	private static MethodInfo s_Take_Int32_TSource_2;

	private static MethodInfo s_Take_Range_TSource_2;

	private static MethodInfo s_TakeWhile_TSource_2;

	private static MethodInfo s_TakeWhile_Index_TSource_2;

	private static MethodInfo s_ThenBy_TSource_TKey_2;

	private static MethodInfo s_ThenBy_TSource_TKey_3;

	private static MethodInfo s_ThenByDescending_TSource_TKey_2;

	private static MethodInfo s_ThenByDescending_TSource_TKey_3;

	private static MethodInfo s_Union_TSource_2;

	private static MethodInfo s_Union_TSource_3;

	private static MethodInfo s_UnionBy_TSource_TKey_3;

	private static MethodInfo s_UnionBy_TSource_TKey_4;

	private static MethodInfo s_Where_TSource_2;

	private static MethodInfo s_Where_Index_TSource_2;

	private static MethodInfo s_Zip_TFirst_TSecond_2;

	private static MethodInfo s_Zip_TFirst_TSecond_TResult_3;

	private static MethodInfo s_Zip_TFirst_TSecond_TThird_3;

	private static MethodInfo s_SkipLast_TSource_2;

	private static MethodInfo s_TakeLast_TSource_2;

	private static MethodInfo s_Append_TSource_2;

	private static MethodInfo s_Prepend_TSource_2;

	public static MethodInfo Average_Int32_1 => s_Average_Int32_1 ?? (s_Average_Int32_1 = new Func<IQueryable<int>, double>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_NullableInt32_1 => s_Average_NullableInt32_1 ?? (s_Average_NullableInt32_1 = new Func<IQueryable<int?>, double?>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_Int64_1 => s_Average_Int64_1 ?? (s_Average_Int64_1 = new Func<IQueryable<long>, double>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_NullableInt64_1 => s_Average_NullableInt64_1 ?? (s_Average_NullableInt64_1 = new Func<IQueryable<long?>, double?>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_Single_1 => s_Average_Single_1 ?? (s_Average_Single_1 = new Func<IQueryable<float>, float>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_NullableSingle_1 => s_Average_NullableSingle_1 ?? (s_Average_NullableSingle_1 = new Func<IQueryable<float?>, float?>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_Double_1 => s_Average_Double_1 ?? (s_Average_Double_1 = new Func<IQueryable<double>, double>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_NullableDouble_1 => s_Average_NullableDouble_1 ?? (s_Average_NullableDouble_1 = new Func<IQueryable<double?>, double?>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_Decimal_1 => s_Average_Decimal_1 ?? (s_Average_Decimal_1 = new Func<IQueryable<decimal>, decimal>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Average_NullableDecimal_1 => s_Average_NullableDecimal_1 ?? (s_Average_NullableDecimal_1 = new Func<IQueryable<decimal?>, decimal?>(Queryable.Average).GetMethodInfo());

	public static MethodInfo Sum_Int32_1 => s_Sum_Int32_1 ?? (s_Sum_Int32_1 = new Func<IQueryable<int>, int>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_NullableInt32_1 => s_Sum_NullableInt32_1 ?? (s_Sum_NullableInt32_1 = new Func<IQueryable<int?>, int?>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_Int64_1 => s_Sum_Int64_1 ?? (s_Sum_Int64_1 = new Func<IQueryable<long>, long>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_NullableInt64_1 => s_Sum_NullableInt64_1 ?? (s_Sum_NullableInt64_1 = new Func<IQueryable<long?>, long?>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_Single_1 => s_Sum_Single_1 ?? (s_Sum_Single_1 = new Func<IQueryable<float>, float>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_NullableSingle_1 => s_Sum_NullableSingle_1 ?? (s_Sum_NullableSingle_1 = new Func<IQueryable<float?>, float?>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_Double_1 => s_Sum_Double_1 ?? (s_Sum_Double_1 = new Func<IQueryable<double>, double>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_NullableDouble_1 => s_Sum_NullableDouble_1 ?? (s_Sum_NullableDouble_1 = new Func<IQueryable<double?>, double?>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_Decimal_1 => s_Sum_Decimal_1 ?? (s_Sum_Decimal_1 = new Func<IQueryable<decimal>, decimal>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Sum_NullableDecimal_1 => s_Sum_NullableDecimal_1 ?? (s_Sum_NullableDecimal_1 = new Func<IQueryable<decimal?>, decimal?>(Queryable.Sum).GetMethodInfo());

	public static MethodInfo Aggregate_TSource_2(Type TSource)
	{
		return (s_Aggregate_TSource_2 ?? (s_Aggregate_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, object, object>>, object>(Queryable.Aggregate).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Aggregate_TSource_TAccumulate_3(Type TSource, Type TAccumulate)
	{
		return (s_Aggregate_TSource_TAccumulate_3 ?? (s_Aggregate_TSource_TAccumulate_3 = new Func<IQueryable<object>, object, Expression<Func<object, object, object>>, object>(Queryable.Aggregate).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TAccumulate);
	}

	public static MethodInfo Aggregate_TSource_TAccumulate_TResult_4(Type TSource, Type TAccumulate, Type TResult)
	{
		return (s_Aggregate_TSource_TAccumulate_TResult_4 ?? (s_Aggregate_TSource_TAccumulate_TResult_4 = new Func<IQueryable<object>, object, Expression<Func<object, object, object>>, Expression<Func<object, object>>, object>(Queryable.Aggregate).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TAccumulate, TResult);
	}

	public static MethodInfo All_TSource_2(Type TSource)
	{
		return (s_All_TSource_2 ?? (s_All_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.All).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Any_TSource_1(Type TSource)
	{
		return (s_Any_TSource_1 ?? (s_Any_TSource_1 = new Func<IQueryable<object>, bool>(Queryable.Any).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Any_TSource_2(Type TSource)
	{
		return (s_Any_TSource_2 ?? (s_Any_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool>(Queryable.Any).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_Int32_TSource_2(Type TSource)
	{
		return (s_Average_Int32_TSource_2 ?? (s_Average_Int32_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int>>, double>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_NullableInt32_TSource_2(Type TSource)
	{
		return (s_Average_NullableInt32_TSource_2 ?? (s_Average_NullableInt32_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int?>>, double?>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_Single_TSource_2(Type TSource)
	{
		return (s_Average_Single_TSource_2 ?? (s_Average_Single_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_NullableSingle_TSource_2(Type TSource)
	{
		return (s_Average_NullableSingle_TSource_2 ?? (s_Average_NullableSingle_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_Int64_TSource_2(Type TSource)
	{
		return (s_Average_Int64_TSource_2 ?? (s_Average_Int64_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, long>>, double>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_NullableInt64_TSource_2(Type TSource)
	{
		return (s_Average_NullableInt64_TSource_2 ?? (s_Average_NullableInt64_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, long?>>, double?>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_Double_TSource_2(Type TSource)
	{
		return (s_Average_Double_TSource_2 ?? (s_Average_Double_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_NullableDouble_TSource_2(Type TSource)
	{
		return (s_Average_NullableDouble_TSource_2 ?? (s_Average_NullableDouble_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_Decimal_TSource_2(Type TSource)
	{
		return (s_Average_Decimal_TSource_2 ?? (s_Average_Decimal_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Average_NullableDecimal_TSource_2(Type TSource)
	{
		return (s_Average_NullableDecimal_TSource_2 ?? (s_Average_NullableDecimal_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Average).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Cast_TResult_1(Type TResult)
	{
		return (s_Cast_TResult_1 ?? (s_Cast_TResult_1 = new Func<IQueryable, IQueryable<object>>(Queryable.Cast<object>).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TResult);
	}

	public static MethodInfo Chunk_TSource_1(Type TSource)
	{
		return (s_Chunk_TSource_1 ?? (s_Chunk_TSource_1 = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Chunk).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Concat_TSource_2(Type TSource)
	{
		return (s_Concat_TSource_2 ?? (s_Concat_TSource_2 = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Concat).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Contains_TSource_2(Type TSource)
	{
		return (s_Contains_TSource_2 ?? (s_Contains_TSource_2 = new Func<IQueryable<object>, object, bool>(Queryable.Contains).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Contains_TSource_3(Type TSource)
	{
		return (s_Contains_TSource_3 ?? (s_Contains_TSource_3 = new Func<IQueryable<object>, object, IEqualityComparer<object>, bool>(Queryable.Contains).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Count_TSource_1(Type TSource)
	{
		return (s_Count_TSource_1 ?? (s_Count_TSource_1 = new Func<IQueryable<object>, int>(Queryable.Count).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Count_TSource_2(Type TSource)
	{
		return (s_Count_TSource_2 ?? (s_Count_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, int>(Queryable.Count).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo DefaultIfEmpty_TSource_1(Type TSource)
	{
		return (s_DefaultIfEmpty_TSource_1 ?? (s_DefaultIfEmpty_TSource_1 = new Func<IQueryable<object>, IQueryable<object>>(Queryable.DefaultIfEmpty).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo DefaultIfEmpty_TSource_2(Type TSource)
	{
		return (s_DefaultIfEmpty_TSource_2 ?? (s_DefaultIfEmpty_TSource_2 = new Func<IQueryable<object>, object, IQueryable<object>>(Queryable.DefaultIfEmpty).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Distinct_TSource_1(Type TSource)
	{
		return (s_Distinct_TSource_1 ?? (s_Distinct_TSource_1 = new Func<IQueryable<object>, IQueryable<object>>(Queryable.Distinct).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Distinct_TSource_2(Type TSource)
	{
		return (s_Distinct_TSource_2 ?? (s_Distinct_TSource_2 = new Func<IQueryable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Distinct).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo DistinctBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_DistinctBy_TSource_TKey_2 ?? (s_DistinctBy_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.DistinctBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo DistinctBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_DistinctBy_TSource_TKey_3 ?? (s_DistinctBy_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.DistinctBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo ElementAt_Int32_TSource_2(Type TSource)
	{
		return (s_ElementAt_Int32_TSource_2 ?? (s_ElementAt_Int32_TSource_2 = new Func<IQueryable<object>, int, object>(Queryable.ElementAt).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo ElementAt_Index_TSource_2(Type TSource)
	{
		return (s_ElementAt_Index_TSource_2 ?? (s_ElementAt_Index_TSource_2 = new Func<IQueryable<object>, Index, object>(Queryable.ElementAt).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo ElementAtOrDefault_Int32_TSource_2(Type TSource)
	{
		return (s_ElementAtOrDefault_Int32_TSource_2 ?? (s_ElementAtOrDefault_Int32_TSource_2 = new Func<IQueryable<object>, int, object>(Queryable.ElementAtOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo ElementAtOrDefault_Index_TSource_2(Type TSource)
	{
		return (s_ElementAtOrDefault_Index_TSource_2 ?? (s_ElementAtOrDefault_Index_TSource_2 = new Func<IQueryable<object>, Index, object>(Queryable.ElementAtOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Except_TSource_2(Type TSource)
	{
		return (s_Except_TSource_2 ?? (s_Except_TSource_2 = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Except).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Except_TSource_3(Type TSource)
	{
		return (s_Except_TSource_3 ?? (s_Except_TSource_3 = new Func<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Except).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo ExceptBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_ExceptBy_TSource_TKey_3 ?? (s_ExceptBy_TSource_TKey_3 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.ExceptBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo ExceptBy_TSource_TKey_4(Type TSource, Type TKey)
	{
		return (s_ExceptBy_TSource_TKey_4 ?? (s_ExceptBy_TSource_TKey_4 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.ExceptBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo First_TSource_1(Type TSource)
	{
		return (s_First_TSource_1 ?? (s_First_TSource_1 = new Func<IQueryable<object>, object>(Queryable.First).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo First_TSource_2(Type TSource)
	{
		return (s_First_TSource_2 ?? (s_First_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.First).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo FirstOrDefault_TSource_1(Type TSource)
	{
		return (s_FirstOrDefault_TSource_1 ?? (s_FirstOrDefault_TSource_1 = new Func<IQueryable<object>, object>(Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo FirstOrDefault_TSource_2(Type TSource)
	{
		return (s_FirstOrDefault_TSource_2 ?? (s_FirstOrDefault_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo FirstOrDefault_TSource_3(Type TSource)
	{
		return (s_FirstOrDefault_TSource_3 ?? (s_FirstOrDefault_TSource_3 = new Func<IQueryable<object>, object, object>(Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo FirstOrDefault_TSource_4(Type TSource)
	{
		return (s_FirstOrDefault_TSource_4 ?? (s_FirstOrDefault_TSource_4 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object, object>(Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo GroupBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_GroupBy_TSource_TKey_2 ?? (s_GroupBy_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo GroupBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_GroupBy_TSource_TKey_3 ?? (s_GroupBy_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo GroupBy_TSource_TKey_TElement_3(Type TSource, Type TKey, Type TElement)
	{
		return (s_GroupBy_TSource_TKey_TElement_3 ?? (s_GroupBy_TSource_TKey_TElement_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TElement);
	}

	public static MethodInfo GroupBy_TSource_TKey_TElement_4(Type TSource, Type TKey, Type TElement)
	{
		return (s_GroupBy_TSource_TKey_TElement_4 ?? (s_GroupBy_TSource_TKey_TElement_4 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<IGrouping<object, object>>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TElement);
	}

	public static MethodInfo GroupBy_TSource_TKey_TResult_3(Type TSource, Type TKey, Type TResult)
	{
		return (s_GroupBy_TSource_TKey_TResult_3 ?? (s_GroupBy_TSource_TKey_TResult_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TResult);
	}

	public static MethodInfo GroupBy_TSource_TKey_TResult_4(Type TSource, Type TKey, Type TResult)
	{
		return (s_GroupBy_TSource_TKey_TResult_4 ?? (s_GroupBy_TSource_TKey_TResult_4 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TResult);
	}

	public static MethodInfo GroupBy_TSource_TKey_TElement_TResult_4(Type TSource, Type TKey, Type TElement, Type TResult)
	{
		return (s_GroupBy_TSource_TKey_TElement_TResult_4 ?? (s_GroupBy_TSource_TKey_TElement_TResult_4 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TElement, TResult);
	}

	public static MethodInfo GroupBy_TSource_TKey_TElement_TResult_5(Type TSource, Type TKey, Type TElement, Type TResult)
	{
		return (s_GroupBy_TSource_TKey_TElement_TResult_5 ?? (s_GroupBy_TSource_TKey_TElement_TResult_5 = new Func<IQueryable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey, TElement, TResult);
	}

	public static MethodInfo GroupJoin_TOuter_TInner_TKey_TResult_5(Type TOuter, Type TInner, Type TKey, Type TResult)
	{
		return (s_GroupJoin_TOuter_TInner_TKey_TResult_5 ?? (s_GroupJoin_TOuter_TInner_TKey_TResult_5 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IQueryable<object>>(Queryable.GroupJoin).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TOuter, TInner, TKey, TResult);
	}

	public static MethodInfo GroupJoin_TOuter_TInner_TKey_TResult_6(Type TOuter, Type TInner, Type TKey, Type TResult)
	{
		return (s_GroupJoin_TOuter_TInner_TKey_TResult_6 ?? (s_GroupJoin_TOuter_TInner_TKey_TResult_6 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, IEnumerable<object>, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.GroupJoin).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TOuter, TInner, TKey, TResult);
	}

	public static MethodInfo Intersect_TSource_2(Type TSource)
	{
		return (s_Intersect_TSource_2 ?? (s_Intersect_TSource_2 = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Intersect).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Intersect_TSource_3(Type TSource)
	{
		return (s_Intersect_TSource_3 ?? (s_Intersect_TSource_3 = new Func<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Intersect).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo IntersectBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_IntersectBy_TSource_TKey_3 ?? (s_IntersectBy_TSource_TKey_3 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.IntersectBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo IntersectBy_TSource_TKey_4(Type TSource, Type TKey)
	{
		return (s_IntersectBy_TSource_TKey_4 ?? (s_IntersectBy_TSource_TKey_4 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.IntersectBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo Join_TOuter_TInner_TKey_TResult_5(Type TOuter, Type TInner, Type TKey, Type TResult)
	{
		return (s_Join_TOuter_TInner_TKey_TResult_5 ?? (s_Join_TOuter_TInner_TKey_TResult_5 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Join).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TOuter, TInner, TKey, TResult);
	}

	public static MethodInfo Join_TOuter_TInner_TKey_TResult_6(Type TOuter, Type TInner, Type TKey, Type TResult)
	{
		return (s_Join_TOuter_TInner_TKey_TResult_6 ?? (s_Join_TOuter_TInner_TKey_TResult_6 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, Expression<Func<object, object>>, Expression<Func<object, object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Join).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TOuter, TInner, TKey, TResult);
	}

	public static MethodInfo Last_TSource_1(Type TSource)
	{
		return (s_Last_TSource_1 ?? (s_Last_TSource_1 = new Func<IQueryable<object>, object>(Queryable.Last).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Last_TSource_2(Type TSource)
	{
		return (s_Last_TSource_2 ?? (s_Last_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Last).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LastOrDefault_TSource_1(Type TSource)
	{
		return (s_LastOrDefault_TSource_1 ?? (s_LastOrDefault_TSource_1 = new Func<IQueryable<object>, object>(Queryable.LastOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LastOrDefault_TSource_2(Type TSource)
	{
		return (s_LastOrDefault_TSource_2 ?? (s_LastOrDefault_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.LastOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LastOrDefault_TSource_3(Type TSource)
	{
		return (s_LastOrDefault_TSource_3 ?? (s_LastOrDefault_TSource_3 = new Func<IQueryable<object>, object, object>(Queryable.LastOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LastOrDefault_TSource_4(Type TSource)
	{
		return (s_LastOrDefault_TSource_4 ?? (s_LastOrDefault_TSource_4 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object, object>(Queryable.LastOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LongCount_TSource_1(Type TSource)
	{
		return (s_LongCount_TSource_1 ?? (s_LongCount_TSource_1 = new Func<IQueryable<object>, long>(Queryable.LongCount).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo LongCount_TSource_2(Type TSource)
	{
		return (s_LongCount_TSource_2 ?? (s_LongCount_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, long>(Queryable.LongCount).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Max_TSource_1(Type TSource)
	{
		return (s_Max_TSource_1 ?? (s_Max_TSource_1 = new Func<IQueryable<object>, object>(Queryable.Max).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Max_TSource_2(Type TSource)
	{
		return (s_Max_TSource_2 ?? (s_Max_TSource_2 = new Func<IQueryable<object>, IComparer<object>, object>(Queryable.Max).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Max_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_Max_TSource_TResult_2 ?? (s_Max_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Max).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo MaxBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_MaxBy_TSource_TKey_2 ?? (s_MaxBy_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.MaxBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo MaxBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_MaxBy_TSource_TKey_3 ?? (s_MaxBy_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, object>(Queryable.MaxBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo Min_TSource_1(Type TSource)
	{
		return (s_Min_TSource_1 ?? (s_Min_TSource_1 = new Func<IQueryable<object>, object>(Queryable.Min).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Min_TSource_2(Type TSource)
	{
		return (s_Min_TSource_2 ?? (s_Min_TSource_2 = new Func<IQueryable<object>, IComparer<object>, object>(Queryable.Min).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Min_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_Min_TSource_TResult_2 ?? (s_Min_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.Min).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo MinBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_MinBy_TSource_TKey_2 ?? (s_MinBy_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, object>(Queryable.MinBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo MinBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_MinBy_TSource_TKey_3 ?? (s_MinBy_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, object>(Queryable.MinBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo OfType_TResult_1(Type TResult)
	{
		return (s_OfType_TResult_1 ?? (s_OfType_TResult_1 = new Func<IQueryable, IQueryable<object>>(Queryable.OfType<object>).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TResult);
	}

	public static MethodInfo OrderBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_OrderBy_TSource_TKey_2 ?? (s_OrderBy_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo OrderBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_OrderBy_TSource_TKey_3 ?? (s_OrderBy_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.OrderBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo OrderByDescending_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_OrderByDescending_TSource_TKey_2 ?? (s_OrderByDescending_TSource_TKey_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.OrderByDescending).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo OrderByDescending_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_OrderByDescending_TSource_TKey_3 ?? (s_OrderByDescending_TSource_TKey_3 = new Func<IQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.OrderByDescending).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo Reverse_TSource_1(Type TSource)
	{
		return (s_Reverse_TSource_1 ?? (s_Reverse_TSource_1 = new Func<IQueryable<object>, IQueryable<object>>(Queryable.Reverse).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Select_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_Select_TSource_TResult_2 ?? (s_Select_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.Select).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo Select_Index_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_Select_Index_TSource_TResult_2 ?? (s_Select_Index_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, int, object>>, IQueryable<object>>(Queryable.Select).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo SelectMany_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_SelectMany_TSource_TResult_2 ?? (s_SelectMany_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo SelectMany_Index_TSource_TResult_2(Type TSource, Type TResult)
	{
		return (s_SelectMany_Index_TSource_TResult_2 ?? (s_SelectMany_Index_TSource_TResult_2 = new Func<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, IQueryable<object>>(Queryable.SelectMany).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TResult);
	}

	public static MethodInfo SelectMany_Index_TSource_TCollection_TResult_3(Type TSource, Type TCollection, Type TResult)
	{
		return (s_SelectMany_Index_TSource_TCollection_TResult_3 ?? (s_SelectMany_Index_TSource_TCollection_TResult_3 = new Func<IQueryable<object>, Expression<Func<object, int, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TCollection, TResult);
	}

	public static MethodInfo SelectMany_TSource_TCollection_TResult_3(Type TSource, Type TCollection, Type TResult)
	{
		return (s_SelectMany_TSource_TCollection_TResult_3 ?? (s_SelectMany_TSource_TCollection_TResult_3 = new Func<IQueryable<object>, Expression<Func<object, IEnumerable<object>>>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.SelectMany).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TCollection, TResult);
	}

	public static MethodInfo SequenceEqual_TSource_2(Type TSource)
	{
		return (s_SequenceEqual_TSource_2 ?? (s_SequenceEqual_TSource_2 = new Func<IQueryable<object>, IEnumerable<object>, bool>(Queryable.SequenceEqual).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SequenceEqual_TSource_3(Type TSource)
	{
		return (s_SequenceEqual_TSource_3 ?? (s_SequenceEqual_TSource_3 = new Func<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, bool>(Queryable.SequenceEqual).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Single_TSource_1(Type TSource)
	{
		return (s_Single_TSource_1 ?? (s_Single_TSource_1 = new Func<IQueryable<object>, object>(Queryable.Single).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Single_TSource_2(Type TSource)
	{
		return (s_Single_TSource_2 ?? (s_Single_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.Single).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SingleOrDefault_TSource_1(Type TSource)
	{
		return (s_SingleOrDefault_TSource_1 ?? (s_SingleOrDefault_TSource_1 = new Func<IQueryable<object>, object>(Queryable.SingleOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SingleOrDefault_TSource_2(Type TSource)
	{
		return (s_SingleOrDefault_TSource_2 ?? (s_SingleOrDefault_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(Queryable.SingleOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SingleOrDefault_TSource_3(Type TSource)
	{
		return (s_SingleOrDefault_TSource_3 ?? (s_SingleOrDefault_TSource_3 = new Func<IQueryable<object>, object, object>(Queryable.SingleOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SingleOrDefault_TSource_4(Type TSource)
	{
		return (s_SingleOrDefault_TSource_4 ?? (s_SingleOrDefault_TSource_4 = new Func<IQueryable<object>, Expression<Func<object, bool>>, object, object>(Queryable.SingleOrDefault).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Skip_TSource_2(Type TSource)
	{
		return (s_Skip_TSource_2 ?? (s_Skip_TSource_2 = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Skip).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SkipWhile_TSource_2(Type TSource)
	{
		return (s_SkipWhile_TSource_2 ?? (s_SkipWhile_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.SkipWhile).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo SkipWhile_Index_TSource_2(Type TSource)
	{
		return (s_SkipWhile_Index_TSource_2 ?? (s_SkipWhile_Index_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.SkipWhile).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_NullableDecimal_TSource_2(Type TSource)
	{
		return (s_Sum_NullableDecimal_TSource_2 ?? (s_Sum_NullableDecimal_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, decimal?>>, decimal?>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_Int32_TSource_2(Type TSource)
	{
		return (s_Sum_Int32_TSource_2 ?? (s_Sum_Int32_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int>>, int>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_NullableInt32_TSource_2(Type TSource)
	{
		return (s_Sum_NullableInt32_TSource_2 ?? (s_Sum_NullableInt32_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int?>>, int?>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_Int64_TSource_2(Type TSource)
	{
		return (s_Sum_Int64_TSource_2 ?? (s_Sum_Int64_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, long>>, long>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_NullableInt64_TSource_2(Type TSource)
	{
		return (s_Sum_NullableInt64_TSource_2 ?? (s_Sum_NullableInt64_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, long?>>, long?>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_Single_TSource_2(Type TSource)
	{
		return (s_Sum_Single_TSource_2 ?? (s_Sum_Single_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, float>>, float>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_NullableSingle_TSource_2(Type TSource)
	{
		return (s_Sum_NullableSingle_TSource_2 ?? (s_Sum_NullableSingle_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, float?>>, float?>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_Double_TSource_2(Type TSource)
	{
		return (s_Sum_Double_TSource_2 ?? (s_Sum_Double_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, double>>, double>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_NullableDouble_TSource_2(Type TSource)
	{
		return (s_Sum_NullableDouble_TSource_2 ?? (s_Sum_NullableDouble_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, double?>>, double?>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Sum_Decimal_TSource_2(Type TSource)
	{
		return (s_Sum_Decimal_TSource_2 ?? (s_Sum_Decimal_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, decimal>>, decimal>(Queryable.Sum).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Take_Int32_TSource_2(Type TSource)
	{
		return (s_Take_Int32_TSource_2 ?? (s_Take_Int32_TSource_2 = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.Take).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Take_Range_TSource_2(Type TSource)
	{
		return (s_Take_Range_TSource_2 ?? (s_Take_Range_TSource_2 = new Func<IQueryable<object>, Range, IQueryable<object>>(Queryable.Take).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo TakeWhile_TSource_2(Type TSource)
	{
		return (s_TakeWhile_TSource_2 ?? (s_TakeWhile_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.TakeWhile).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo TakeWhile_Index_TSource_2(Type TSource)
	{
		return (s_TakeWhile_Index_TSource_2 ?? (s_TakeWhile_Index_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.TakeWhile).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo ThenBy_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_ThenBy_TSource_TKey_2 ?? (s_ThenBy_TSource_TKey_2 = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo ThenBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_ThenBy_TSource_TKey_3 ?? (s_ThenBy_TSource_TKey_3 = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.ThenBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo ThenByDescending_TSource_TKey_2(Type TSource, Type TKey)
	{
		return (s_ThenByDescending_TSource_TKey_2 ?? (s_ThenByDescending_TSource_TKey_2 = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IOrderedQueryable<object>>(Queryable.ThenByDescending).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo ThenByDescending_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_ThenByDescending_TSource_TKey_3 ?? (s_ThenByDescending_TSource_TKey_3 = new Func<IOrderedQueryable<object>, Expression<Func<object, object>>, IComparer<object>, IOrderedQueryable<object>>(Queryable.ThenByDescending).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo Union_TSource_2(Type TSource)
	{
		return (s_Union_TSource_2 ?? (s_Union_TSource_2 = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<object>>(Queryable.Union).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Union_TSource_3(Type TSource)
	{
		return (s_Union_TSource_3 ?? (s_Union_TSource_3 = new Func<IQueryable<object>, IEnumerable<object>, IEqualityComparer<object>, IQueryable<object>>(Queryable.Union).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo UnionBy_TSource_TKey_3(Type TSource, Type TKey)
	{
		return (s_UnionBy_TSource_TKey_3 ?? (s_UnionBy_TSource_TKey_3 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IQueryable<object>>(Queryable.UnionBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo UnionBy_TSource_TKey_4(Type TSource, Type TKey)
	{
		return (s_UnionBy_TSource_TKey_4 ?? (s_UnionBy_TSource_TKey_4 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object>>, IEqualityComparer<object>, IQueryable<object>>(Queryable.UnionBy).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource, TKey);
	}

	public static MethodInfo Where_TSource_2(Type TSource)
	{
		return (s_Where_TSource_2 ?? (s_Where_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>(Queryable.Where).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Where_Index_TSource_2(Type TSource)
	{
		return (s_Where_Index_TSource_2 ?? (s_Where_Index_TSource_2 = new Func<IQueryable<object>, Expression<Func<object, int, bool>>, IQueryable<object>>(Queryable.Where).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Zip_TFirst_TSecond_2(Type TFirst, Type TSecond)
	{
		return (s_Zip_TFirst_TSecond_2 ?? (s_Zip_TFirst_TSecond_2 = new Func<IQueryable<object>, IEnumerable<object>, IQueryable<(object, object)>>(Queryable.Zip).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TFirst, TSecond);
	}

	public static MethodInfo Zip_TFirst_TSecond_TResult_3(Type TFirst, Type TSecond, Type TResult)
	{
		return (s_Zip_TFirst_TSecond_TResult_3 ?? (s_Zip_TFirst_TSecond_TResult_3 = new Func<IQueryable<object>, IEnumerable<object>, Expression<Func<object, object, object>>, IQueryable<object>>(Queryable.Zip).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TFirst, TSecond, TResult);
	}

	public static MethodInfo Zip_TFirst_TSecond_TThird_3(Type TFirst, Type TSecond, Type TThird)
	{
		return (s_Zip_TFirst_TSecond_TThird_3 ?? (s_Zip_TFirst_TSecond_TThird_3 = new Func<IQueryable<object>, IEnumerable<object>, IEnumerable<object>, IQueryable<(object, object, object)>>(Queryable.Zip).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TFirst, TSecond, TThird);
	}

	public static MethodInfo SkipLast_TSource_2(Type TSource)
	{
		return (s_SkipLast_TSource_2 ?? (s_SkipLast_TSource_2 = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.SkipLast).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo TakeLast_TSource_2(Type TSource)
	{
		return (s_TakeLast_TSource_2 ?? (s_TakeLast_TSource_2 = new Func<IQueryable<object>, int, IQueryable<object>>(Queryable.TakeLast).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Append_TSource_2(Type TSource)
	{
		return (s_Append_TSource_2 ?? (s_Append_TSource_2 = new Func<IQueryable<object>, object, IQueryable<object>>(Queryable.Append).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}

	public static MethodInfo Prepend_TSource_2(Type TSource)
	{
		return (s_Prepend_TSource_2 ?? (s_Prepend_TSource_2 = new Func<IQueryable<object>, object, IQueryable<object>>(Queryable.Prepend).GetMethodInfo().GetGenericMethodDefinition())).MakeGenericMethod(TSource);
	}
}
