using System.ComponentModel;
using System.Reflection;

namespace System.Xml.Serialization;

public class XmlAttributes
{
	private readonly XmlElementAttributes _xmlElements = new XmlElementAttributes();

	private readonly XmlArrayItemAttributes _xmlArrayItems = new XmlArrayItemAttributes();

	private readonly XmlAnyElementAttributes _xmlAnyElements = new XmlAnyElementAttributes();

	private XmlArrayAttribute _xmlArray;

	private XmlAttributeAttribute _xmlAttribute;

	private XmlTextAttribute _xmlText;

	private XmlEnumAttribute _xmlEnum;

	private bool _xmlIgnore;

	private bool _xmlns;

	private object _xmlDefaultValue;

	private XmlRootAttribute _xmlRoot;

	private XmlTypeAttribute _xmlType;

	private XmlAnyAttributeAttribute _xmlAnyAttribute;

	private readonly XmlChoiceIdentifierAttribute _xmlChoiceIdentifier;

	internal XmlAttributeFlags XmlFlags
	{
		get
		{
			XmlAttributeFlags xmlAttributeFlags = (XmlAttributeFlags)0;
			if (_xmlElements.Count > 0)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Elements;
			}
			if (_xmlArrayItems.Count > 0)
			{
				xmlAttributeFlags |= XmlAttributeFlags.ArrayItems;
			}
			if (_xmlAnyElements.Count > 0)
			{
				xmlAttributeFlags |= XmlAttributeFlags.AnyElements;
			}
			if (_xmlArray != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Array;
			}
			if (_xmlAttribute != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Attribute;
			}
			if (_xmlText != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Text;
			}
			if (_xmlEnum != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Enum;
			}
			if (_xmlRoot != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Root;
			}
			if (_xmlType != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.Type;
			}
			if (_xmlAnyAttribute != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.AnyAttribute;
			}
			if (_xmlChoiceIdentifier != null)
			{
				xmlAttributeFlags |= XmlAttributeFlags.ChoiceIdentifier;
			}
			if (_xmlns)
			{
				xmlAttributeFlags |= XmlAttributeFlags.XmlnsDeclarations;
			}
			return xmlAttributeFlags;
		}
	}

	public XmlElementAttributes XmlElements => _xmlElements;

	public XmlAttributeAttribute? XmlAttribute
	{
		get
		{
			return _xmlAttribute;
		}
		set
		{
			_xmlAttribute = value;
		}
	}

	public XmlEnumAttribute? XmlEnum
	{
		get
		{
			return _xmlEnum;
		}
		set
		{
			_xmlEnum = value;
		}
	}

	public XmlTextAttribute? XmlText
	{
		get
		{
			return _xmlText;
		}
		set
		{
			_xmlText = value;
		}
	}

	public XmlArrayAttribute? XmlArray
	{
		get
		{
			return _xmlArray;
		}
		set
		{
			_xmlArray = value;
		}
	}

	public XmlArrayItemAttributes XmlArrayItems => _xmlArrayItems;

	public object? XmlDefaultValue
	{
		get
		{
			return _xmlDefaultValue;
		}
		set
		{
			_xmlDefaultValue = value;
		}
	}

	public bool XmlIgnore
	{
		get
		{
			return _xmlIgnore;
		}
		set
		{
			_xmlIgnore = value;
		}
	}

	public XmlTypeAttribute? XmlType
	{
		get
		{
			return _xmlType;
		}
		set
		{
			_xmlType = value;
		}
	}

	public XmlRootAttribute? XmlRoot
	{
		get
		{
			return _xmlRoot;
		}
		set
		{
			_xmlRoot = value;
		}
	}

	public XmlAnyElementAttributes XmlAnyElements => _xmlAnyElements;

	public XmlAnyAttributeAttribute? XmlAnyAttribute
	{
		get
		{
			return _xmlAnyAttribute;
		}
		set
		{
			_xmlAnyAttribute = value;
		}
	}

	public XmlChoiceIdentifierAttribute? XmlChoiceIdentifier => _xmlChoiceIdentifier;

	public bool Xmlns
	{
		get
		{
			return _xmlns;
		}
		set
		{
			_xmlns = value;
		}
	}

	public XmlAttributes()
	{
	}

	public XmlAttributes(ICustomAttributeProvider provider)
	{
		object[] customAttributes = provider.GetCustomAttributes(inherit: false);
		XmlAnyElementAttribute xmlAnyElementAttribute = null;
		for (int i = 0; i < customAttributes.Length; i++)
		{
			if (customAttributes[i] is XmlIgnoreAttribute || customAttributes[i] is ObsoleteAttribute)
			{
				_xmlIgnore = true;
				break;
			}
			if (customAttributes[i] is XmlElementAttribute)
			{
				_xmlElements.Add((XmlElementAttribute)customAttributes[i]);
			}
			else if (customAttributes[i] is XmlArrayItemAttribute)
			{
				_xmlArrayItems.Add((XmlArrayItemAttribute)customAttributes[i]);
			}
			else if (customAttributes[i] is XmlAnyElementAttribute)
			{
				XmlAnyElementAttribute xmlAnyElementAttribute2 = (XmlAnyElementAttribute)customAttributes[i];
				if ((xmlAnyElementAttribute2.Name == null || xmlAnyElementAttribute2.Name.Length == 0) && xmlAnyElementAttribute2.GetNamespaceSpecified() && xmlAnyElementAttribute2.Namespace == null)
				{
					xmlAnyElementAttribute = xmlAnyElementAttribute2;
				}
				else
				{
					_xmlAnyElements.Add((XmlAnyElementAttribute)customAttributes[i]);
				}
			}
			else if (customAttributes[i] is DefaultValueAttribute)
			{
				_xmlDefaultValue = ((DefaultValueAttribute)customAttributes[i]).Value;
			}
			else if (customAttributes[i] is XmlAttributeAttribute)
			{
				_xmlAttribute = (XmlAttributeAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlArrayAttribute)
			{
				_xmlArray = (XmlArrayAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlTextAttribute)
			{
				_xmlText = (XmlTextAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlEnumAttribute)
			{
				_xmlEnum = (XmlEnumAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlRootAttribute)
			{
				_xmlRoot = (XmlRootAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlTypeAttribute)
			{
				_xmlType = (XmlTypeAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlAnyAttributeAttribute)
			{
				_xmlAnyAttribute = (XmlAnyAttributeAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlChoiceIdentifierAttribute)
			{
				_xmlChoiceIdentifier = (XmlChoiceIdentifierAttribute)customAttributes[i];
			}
			else if (customAttributes[i] is XmlNamespaceDeclarationsAttribute)
			{
				_xmlns = true;
			}
		}
		if (_xmlIgnore)
		{
			_xmlElements.Clear();
			_xmlArrayItems.Clear();
			_xmlAnyElements.Clear();
			_xmlDefaultValue = null;
			_xmlAttribute = null;
			_xmlArray = null;
			_xmlText = null;
			_xmlEnum = null;
			_xmlType = null;
			_xmlAnyAttribute = null;
			_xmlChoiceIdentifier = null;
			_xmlns = false;
		}
		else if (xmlAnyElementAttribute != null)
		{
			_xmlAnyElements.Add(xmlAnyElementAttribute);
		}
	}

	internal static object GetAttr(MemberInfo memberInfo, Type attrType)
	{
		object[] customAttributes = memberInfo.GetCustomAttributes(attrType, inherit: false);
		if (customAttributes.Length == 0)
		{
			return null;
		}
		return customAttributes[0];
	}
}
