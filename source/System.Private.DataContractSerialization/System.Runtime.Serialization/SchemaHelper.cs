using System.Collections;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization;

internal static class SchemaHelper
{
	internal static bool NamespacesEqual(string ns1, string ns2)
	{
		if (ns1 == null || ns1.Length == 0)
		{
			if (ns2 != null)
			{
				return ns2.Length == 0;
			}
			return true;
		}
		return ns1 == ns2;
	}

	internal static XmlSchemaType GetSchemaType(XmlSchemaSet schemas, XmlQualifiedName typeQName, out XmlSchema outSchema)
	{
		outSchema = null;
		ICollection collection = schemas.Schemas();
		string @namespace = typeQName.Namespace;
		foreach (XmlSchema item in collection)
		{
			if (!NamespacesEqual(@namespace, item.TargetNamespace))
			{
				continue;
			}
			outSchema = item;
			foreach (XmlSchemaObject item2 in item.Items)
			{
				if (item2 is XmlSchemaType xmlSchemaType && xmlSchemaType.Name == typeQName.Name)
				{
					return xmlSchemaType;
				}
			}
		}
		return null;
	}

	internal static XmlSchemaElement GetSchemaElement(XmlSchemaSet schemas, XmlQualifiedName elementQName, out XmlSchema outSchema)
	{
		outSchema = null;
		ICollection collection = schemas.Schemas();
		string @namespace = elementQName.Namespace;
		foreach (XmlSchema item in collection)
		{
			if (!NamespacesEqual(@namespace, item.TargetNamespace))
			{
				continue;
			}
			outSchema = item;
			foreach (XmlSchemaObject item2 in item.Items)
			{
				if (item2 is XmlSchemaElement xmlSchemaElement && xmlSchemaElement.Name == elementQName.Name)
				{
					return xmlSchemaElement;
				}
			}
		}
		return null;
	}

	internal static XmlSchema GetSchema(string ns, XmlSchemaSet schemas)
	{
		if (ns == null)
		{
			ns = string.Empty;
		}
		ICollection collection = schemas.Schemas();
		foreach (XmlSchema item in collection)
		{
			if ((item.TargetNamespace == null && ns.Length == 0) || ns.Equals(item.TargetNamespace))
			{
				return item;
			}
		}
		return CreateSchema(ns, schemas);
	}

	private static XmlSchema CreateSchema(string ns, XmlSchemaSet schemas)
	{
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema.ElementFormDefault = XmlSchemaForm.Qualified;
		if (ns.Length > 0)
		{
			xmlSchema.TargetNamespace = ns;
			xmlSchema.Namespaces.Add("tns", ns);
		}
		schemas.Add(xmlSchema);
		return xmlSchema;
	}

	internal static void AddElementForm(XmlSchemaElement element, XmlSchema schema)
	{
		if (schema.ElementFormDefault != XmlSchemaForm.Qualified)
		{
			element.Form = XmlSchemaForm.Qualified;
		}
	}

	internal static void AddSchemaImport(string ns, XmlSchema schema)
	{
		if (NamespacesEqual(ns, schema.TargetNamespace) || NamespacesEqual(ns, "http://www.w3.org/2001/XMLSchema") || NamespacesEqual(ns, "http://www.w3.org/2001/XMLSchema-instance"))
		{
			return;
		}
		foreach (XmlSchemaObject include in schema.Includes)
		{
			if (include is XmlSchemaImport && NamespacesEqual(ns, ((XmlSchemaImport)include).Namespace))
			{
				return;
			}
		}
		XmlSchemaImport xmlSchemaImport = new XmlSchemaImport();
		if (ns != null && ns.Length > 0)
		{
			xmlSchemaImport.Namespace = ns;
		}
		schema.Includes.Add(xmlSchemaImport);
	}
}
