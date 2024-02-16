using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace System.Data;

internal class XMLSchema
{
	[RequiresUnreferencedCode("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
	internal static TypeConverter GetConverter(Type type)
	{
		return TypeDescriptor.GetConverter(type);
	}

	[RequiresUnreferencedCode("Calls into TypeDescriptor.GetProperties. Type cannot be statically discovered.")]
	internal static void SetProperties(object instance, XmlAttributeCollection attrs)
	{
		for (int i = 0; i < attrs.Count; i++)
		{
			if (!(attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata"))
			{
				continue;
			}
			string localName = attrs[i].LocalName;
			string value = attrs[i].Value;
			switch (localName)
			{
			case "Expression":
				if (instance is DataColumn)
				{
					continue;
				}
				break;
			case "DefaultValue":
			case "RemotingFormat":
				continue;
			}
			PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(instance)[localName];
			if (propertyDescriptor == null)
			{
				continue;
			}
			Type propertyType = propertyDescriptor.PropertyType;
			TypeConverter converter = GetConverter(propertyType);
			object value2;
			if (converter.CanConvertFrom(typeof(string)))
			{
				value2 = converter.ConvertFromInvariantString(value);
			}
			else if (propertyType == typeof(Type))
			{
				value2 = DataStorage.GetType(value);
			}
			else
			{
				if (!(propertyType == typeof(CultureInfo)))
				{
					throw ExceptionBuilder.CannotConvert(value, propertyType.FullName);
				}
				value2 = new CultureInfo(value);
			}
			propertyDescriptor.SetValue(instance, value2);
		}
	}

	internal static bool FEqualIdentity(XmlNode node, string name, string ns)
	{
		if (node != null && node.LocalName == name && node.NamespaceURI == ns)
		{
			return true;
		}
		return false;
	}

	internal static bool GetBooleanAttribute(XmlElement element, string attrName, string attrNS, bool defVal)
	{
		string attribute = element.GetAttribute(attrName, attrNS);
		if (attribute == null || attribute.Length == 0)
		{
			return defVal;
		}
		switch (attribute)
		{
		case "true":
		case "1":
			return true;
		case "false":
		case "0":
			return false;
		default:
			throw ExceptionBuilder.InvalidAttributeValue(attrName, attribute);
		}
	}

	internal static string GenUniqueColumnName(string proposedName, DataTable table)
	{
		if (table.Columns.IndexOf(proposedName) >= 0)
		{
			for (int i = 0; i <= table.Columns.Count; i++)
			{
				string text = proposedName + "_" + i.ToString(CultureInfo.InvariantCulture);
				if (table.Columns.IndexOf(text) < 0)
				{
					return text;
				}
			}
		}
		return proposedName;
	}
}
