using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class XmlILStorageConverter
{
	public static XmlAtomicValue StringToAtomicValue(string value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue DecimalToAtomicValue(decimal value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue Int64ToAtomicValue(long value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue Int32ToAtomicValue(int value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue BooleanToAtomicValue(bool value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue DoubleToAtomicValue(double value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue SingleToAtomicValue(float value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue DateTimeToAtomicValue(DateTime value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue XmlQualifiedNameToAtomicValue(XmlQualifiedName value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue TimeSpanToAtomicValue(TimeSpan value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static XmlAtomicValue BytesToAtomicValue(byte[] value, int index, XmlQueryRuntime runtime)
	{
		return new XmlAtomicValue(runtime.GetXmlType(index).SchemaType, value);
	}

	public static IList<XPathItem> NavigatorsToItems(IList<XPathNavigator> listNavigators)
	{
		if (listNavigators is IList<XPathItem> result)
		{
			return result;
		}
		return new XmlQueryNodeSequence(listNavigators);
	}

	public static IList<XPathNavigator> ItemsToNavigators(IList<XPathItem> listItems)
	{
		if (listItems is IList<XPathNavigator> result)
		{
			return result;
		}
		XmlQueryNodeSequence xmlQueryNodeSequence = new XmlQueryNodeSequence(listItems.Count);
		for (int i = 0; i < listItems.Count; i++)
		{
			xmlQueryNodeSequence.Add((XPathNavigator)listItems[i]);
		}
		return xmlQueryNodeSequence;
	}
}
