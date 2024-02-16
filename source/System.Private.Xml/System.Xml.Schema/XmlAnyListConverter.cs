using System.Collections;

namespace System.Xml.Schema;

internal sealed class XmlAnyListConverter : XmlListConverter
{
	public static readonly XmlValueConverter ItemList = new XmlAnyListConverter((XmlBaseConverter)XmlAnyConverter.Item);

	public static readonly XmlValueConverter AnyAtomicList = new XmlAnyListConverter((XmlBaseConverter)XmlAnyConverter.AnyAtomic);

	private XmlAnyListConverter(XmlBaseConverter atomicConverter)
		: base(atomicConverter)
	{
	}

	public override object ChangeType(object value, Type destinationType, IXmlNamespaceResolver nsResolver)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (destinationType == null)
		{
			throw new ArgumentNullException("destinationType");
		}
		if (!(value is IEnumerable) || value.GetType() == XmlBaseConverter.StringType || value.GetType() == XmlBaseConverter.ByteArrayType)
		{
			value = new object[1] { value };
		}
		return ChangeListType(value, destinationType, nsResolver);
	}
}
