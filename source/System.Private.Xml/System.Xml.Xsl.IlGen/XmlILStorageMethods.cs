using System.Collections.Generic;
using System.Reflection;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILStorageMethods
{
	public readonly MethodInfo AggAvg;

	public readonly MethodInfo AggAvgResult;

	public readonly MethodInfo AggCreate;

	public readonly MethodInfo AggIsEmpty;

	public readonly MethodInfo AggMax;

	public readonly MethodInfo AggMaxResult;

	public readonly MethodInfo AggMin;

	public readonly MethodInfo AggMinResult;

	public readonly MethodInfo AggSum;

	public readonly MethodInfo AggSumResult;

	public readonly Type SeqType;

	public readonly FieldInfo SeqEmpty;

	public readonly MethodInfo SeqReuse;

	public readonly MethodInfo SeqReuseSgl;

	public readonly MethodInfo SeqAdd;

	public readonly MethodInfo SeqSortByKeys;

	public readonly Type IListType;

	public readonly MethodInfo IListCount;

	public readonly MethodInfo IListItem;

	public readonly MethodInfo ValueAs;

	public readonly MethodInfo ToAtomicValue;

	public XmlILStorageMethods(Type storageType)
	{
		Type type = null;
		if (storageType == typeof(int))
		{
			type = typeof(Int32Aggregator);
		}
		else if (storageType == typeof(long))
		{
			type = typeof(Int64Aggregator);
		}
		else if (storageType == typeof(decimal))
		{
			type = typeof(DecimalAggregator);
		}
		else if (storageType == typeof(double))
		{
			type = typeof(DoubleAggregator);
		}
		if (type != null)
		{
			AggAvg = type.GetMethod("Average");
			AggAvgResult = type.GetMethod("get_AverageResult");
			AggCreate = type.GetMethod("Create");
			AggIsEmpty = type.GetMethod("get_IsEmpty");
			AggMax = type.GetMethod("Maximum");
			AggMaxResult = type.GetMethod("get_MaximumResult");
			AggMin = type.GetMethod("Minimum");
			AggMinResult = type.GetMethod("get_MinimumResult");
			AggSum = type.GetMethod("Sum");
			AggSumResult = type.GetMethod("get_SumResult");
		}
		Type type2;
		if (storageType == typeof(XPathNavigator))
		{
			type2 = typeof(XmlQueryNodeSequence);
			SeqAdd = type2.GetMethod("AddClone");
		}
		else if (storageType == typeof(XPathItem))
		{
			type2 = typeof(XmlQueryItemSequence);
			SeqAdd = type2.GetMethod("AddClone");
		}
		else
		{
			type2 = typeof(XmlQuerySequence<>).MakeGenericType(storageType);
			SeqAdd = type2.GetMethod("Add");
		}
		FieldInfo field = type2.GetField("Empty");
		SeqEmpty = field;
		SeqReuse = type2.GetMethod("CreateOrReuse", new Type[1] { type2 });
		SeqReuseSgl = type2.GetMethod("CreateOrReuse", new Type[2] { type2, storageType });
		SeqSortByKeys = type2.GetMethod("SortByKeys");
		SeqType = type2;
		Type type3 = typeof(IList<>).MakeGenericType(storageType);
		IListItem = type3.GetMethod("get_Item");
		IListType = type3;
		IListCount = typeof(ICollection<>).MakeGenericType(storageType).GetMethod("get_Count");
		if (storageType == typeof(string))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_Value");
		}
		else if (storageType == typeof(int))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_ValueAsInt");
		}
		else if (storageType == typeof(long))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_ValueAsLong");
		}
		else if (storageType == typeof(DateTime))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_ValueAsDateTime");
		}
		else if (storageType == typeof(double))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_ValueAsDouble");
		}
		else if (storageType == typeof(bool))
		{
			ValueAs = typeof(XPathItem).GetMethod("get_ValueAsBoolean");
		}
		if (storageType == typeof(byte[]))
		{
			ToAtomicValue = typeof(XmlILStorageConverter).GetMethod("BytesToAtomicValue");
		}
		else if (storageType != typeof(XPathItem) && storageType != typeof(XPathNavigator))
		{
			ToAtomicValue = typeof(XmlILStorageConverter).GetMethod(storageType.Name + "ToAtomicValue");
		}
	}
}
