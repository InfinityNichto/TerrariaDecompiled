using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Xml.Linq;

[TypeDescriptionProvider("MS.Internal.Xml.Linq.ComponentModel.XTypeDescriptionProvider`1[[System.Xml.Linq.XAttribute, System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]],System.ComponentModel.TypeConverter")]
public class XAttribute : XObject
{
	internal XAttribute next;

	internal XName name;

	internal string value;

	public static IEnumerable<XAttribute> EmptySequence => Array.Empty<XAttribute>();

	public bool IsNamespaceDeclaration
	{
		get
		{
			string namespaceName = name.NamespaceName;
			if (namespaceName.Length == 0)
			{
				return name.LocalName == "xmlns";
			}
			return (object)namespaceName == "http://www.w3.org/2000/xmlns/";
		}
	}

	public XName Name => name;

	public XAttribute? NextAttribute
	{
		get
		{
			if (parent == null || ((XElement)parent).lastAttr == this)
			{
				return null;
			}
			return next;
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.Attribute;

	public XAttribute? PreviousAttribute
	{
		get
		{
			if (parent == null)
			{
				return null;
			}
			XAttribute lastAttr = ((XElement)parent).lastAttr;
			while (lastAttr.next != this)
			{
				lastAttr = lastAttr.next;
			}
			if (lastAttr == ((XElement)parent).lastAttr)
			{
				return null;
			}
			return lastAttr;
		}
	}

	public string Value
	{
		get
		{
			return value;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			ValidateAttribute(name, value);
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			this.value = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public XAttribute(XName name, object value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		string stringValue = XContainer.GetStringValue(value);
		ValidateAttribute(name, stringValue);
		this.name = name;
		this.value = stringValue;
	}

	public XAttribute(XAttribute other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		name = other.name;
		value = other.value;
	}

	public void Remove()
	{
		if (parent == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MissingParent);
		}
		((XElement)parent).RemoveAttribute(this);
	}

	public void SetValue(object value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Value = XContainer.GetStringValue(value);
	}

	public override string ToString()
	{
		using StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
		using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings))
		{
			xmlWriter.WriteAttributeString(GetPrefixOfNamespace(name.Namespace), name.LocalName, name.NamespaceName, value);
		}
		return stringWriter.ToString().Trim();
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator string?(XAttribute? attribute)
	{
		return attribute?.value;
	}

	[CLSCompliant(false)]
	public static explicit operator bool(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToBoolean(attribute.value.ToLowerInvariant());
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator bool?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToBoolean(attribute.value.ToLowerInvariant());
	}

	[CLSCompliant(false)]
	public static explicit operator int(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToInt32(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator int?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToInt32(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator uint(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToUInt32(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator uint?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToUInt32(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator long(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToInt64(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator long?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToInt64(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator ulong(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToUInt64(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator ulong?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToUInt64(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator float(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToSingle(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator float?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToSingle(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator double(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToDouble(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator double?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToDouble(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator decimal(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToDecimal(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator decimal?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToDecimal(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator DateTime(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return DateTime.Parse(attribute.value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator DateTime?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return DateTime.Parse(attribute.value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}

	[CLSCompliant(false)]
	public static explicit operator DateTimeOffset(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToDateTimeOffset(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator DateTimeOffset?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToDateTimeOffset(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator TimeSpan(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToTimeSpan(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator TimeSpan?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToTimeSpan(attribute.value);
	}

	[CLSCompliant(false)]
	public static explicit operator Guid(XAttribute attribute)
	{
		if (attribute == null)
		{
			throw new ArgumentNullException("attribute");
		}
		return XmlConvert.ToGuid(attribute.value);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("attribute")]
	public static explicit operator Guid?(XAttribute? attribute)
	{
		if (attribute == null)
		{
			return null;
		}
		return XmlConvert.ToGuid(attribute.value);
	}

	internal int GetDeepHashCode()
	{
		return name.GetHashCode() ^ value.GetHashCode();
	}

	internal string GetPrefixOfNamespace(XNamespace ns)
	{
		string namespaceName = ns.NamespaceName;
		if (namespaceName.Length == 0)
		{
			return string.Empty;
		}
		if (parent != null)
		{
			return ((XElement)parent).GetPrefixOfNamespace(ns);
		}
		if ((object)namespaceName == "http://www.w3.org/XML/1998/namespace")
		{
			return "xml";
		}
		if ((object)namespaceName == "http://www.w3.org/2000/xmlns/")
		{
			return "xmlns";
		}
		return null;
	}

	private static void ValidateAttribute(XName name, string value)
	{
		string namespaceName = name.NamespaceName;
		if ((object)namespaceName == "http://www.w3.org/2000/xmlns/")
		{
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_NamespaceDeclarationPrefixed, name.LocalName));
			}
			if (value == "http://www.w3.org/XML/1998/namespace")
			{
				if (name.LocalName != "xml")
				{
					throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXml);
				}
				return;
			}
			if (value == "http://www.w3.org/2000/xmlns/")
			{
				throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXmlns);
			}
			string localName = name.LocalName;
			if (localName == "xml")
			{
				throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXml);
			}
			if (localName == "xmlns")
			{
				throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXmlns);
			}
		}
		else if (namespaceName.Length == 0 && name.LocalName == "xmlns")
		{
			if (value == "http://www.w3.org/XML/1998/namespace")
			{
				throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXml);
			}
			if (value == "http://www.w3.org/2000/xmlns/")
			{
				throw new ArgumentException(System.SR.Argument_NamespaceDeclarationXmlns);
			}
		}
	}
}
