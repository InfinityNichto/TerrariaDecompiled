using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Xml.Xsl.IlGen;

internal static class XmlILTypeHelper
{
	private static readonly Type[] s_typeCodeToStorage = new Type[55]
	{
		typeof(XPathItem),
		typeof(XPathItem),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathNavigator),
		typeof(XPathItem),
		typeof(string),
		typeof(string),
		typeof(bool),
		typeof(decimal),
		typeof(float),
		typeof(double),
		typeof(string),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(DateTime),
		typeof(byte[]),
		typeof(byte[]),
		typeof(string),
		typeof(XmlQualifiedName),
		typeof(XmlQualifiedName),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(string),
		typeof(long),
		typeof(decimal),
		typeof(decimal),
		typeof(long),
		typeof(int),
		typeof(int),
		typeof(int),
		typeof(decimal),
		typeof(decimal),
		typeof(long),
		typeof(int),
		typeof(int),
		typeof(decimal),
		typeof(TimeSpan),
		typeof(TimeSpan)
	};

	private static readonly Type[] s_typeCodeToCachedStorage = new Type[55]
	{
		typeof(IList<XPathItem>),
		typeof(IList<XPathItem>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathNavigator>),
		typeof(IList<XPathItem>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<bool>),
		typeof(IList<decimal>),
		typeof(IList<float>),
		typeof(IList<double>),
		typeof(IList<string>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<DateTime>),
		typeof(IList<byte[]>),
		typeof(IList<byte[]>),
		typeof(IList<string>),
		typeof(IList<XmlQualifiedName>),
		typeof(IList<XmlQualifiedName>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<string>),
		typeof(IList<long>),
		typeof(IList<decimal>),
		typeof(IList<decimal>),
		typeof(IList<long>),
		typeof(IList<int>),
		typeof(IList<int>),
		typeof(IList<int>),
		typeof(IList<decimal>),
		typeof(IList<decimal>),
		typeof(IList<long>),
		typeof(IList<int>),
		typeof(IList<int>),
		typeof(IList<decimal>),
		typeof(IList<TimeSpan>),
		typeof(IList<TimeSpan>)
	};

	public static Type GetStorageType(XmlQueryType qyTyp)
	{
		Type type;
		if (qyTyp.IsSingleton)
		{
			type = s_typeCodeToStorage[(int)qyTyp.TypeCode];
			if (!qyTyp.IsStrict && type != typeof(XPathNavigator))
			{
				return typeof(XPathItem);
			}
		}
		else
		{
			type = s_typeCodeToCachedStorage[(int)qyTyp.TypeCode];
			if (!qyTyp.IsStrict && type != typeof(IList<XPathNavigator>))
			{
				return typeof(IList<XPathItem>);
			}
		}
		return type;
	}
}
