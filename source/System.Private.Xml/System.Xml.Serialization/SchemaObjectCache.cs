using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class SchemaObjectCache
{
	private Hashtable _graph;

	private Hashtable _hash;

	private Hashtable _objectCache;

	private StringCollection _warnings;

	internal Hashtable looks = new Hashtable();

	private Hashtable Graph
	{
		get
		{
			if (_graph == null)
			{
				_graph = new Hashtable();
			}
			return _graph;
		}
	}

	private Hashtable Hash
	{
		get
		{
			if (_hash == null)
			{
				_hash = new Hashtable();
			}
			return _hash;
		}
	}

	private Hashtable ObjectCache
	{
		get
		{
			if (_objectCache == null)
			{
				_objectCache = new Hashtable();
			}
			return _objectCache;
		}
	}

	internal StringCollection Warnings
	{
		get
		{
			if (_warnings == null)
			{
				_warnings = new StringCollection();
			}
			return _warnings;
		}
	}

	internal XmlSchemaObject AddItem(XmlSchemaObject item, XmlQualifiedName qname, XmlSchemas schemas)
	{
		if (item == null)
		{
			return null;
		}
		if (qname == null || qname.IsEmpty)
		{
			return null;
		}
		string key = item.GetType().Name + ":" + qname.ToString();
		ArrayList arrayList = (ArrayList)ObjectCache[key];
		if (arrayList == null)
		{
			arrayList = new ArrayList();
			ObjectCache[key] = arrayList;
		}
		for (int i = 0; i < arrayList.Count; i++)
		{
			XmlSchemaObject xmlSchemaObject = (XmlSchemaObject)arrayList[i];
			if (xmlSchemaObject == item)
			{
				return xmlSchemaObject;
			}
			if (Match(xmlSchemaObject, item, shareTypes: true))
			{
				return xmlSchemaObject;
			}
			Warnings.Add(System.SR.Format(System.SR.XmlMismatchSchemaObjects, item.GetType().Name, qname.Name, qname.Namespace));
			Warnings.Add("DEBUG:Cached item key:\r\n" + (string)looks[xmlSchemaObject] + "\r\nnew item key:\r\n" + (string)looks[item]);
		}
		arrayList.Add(item);
		return item;
	}

	internal bool Match(XmlSchemaObject o1, XmlSchemaObject o2, bool shareTypes)
	{
		if (o1 == o2)
		{
			return true;
		}
		if (o1.GetType() != o2.GetType())
		{
			return false;
		}
		if (Hash[o1] == null)
		{
			Hash[o1] = GetHash(o1);
		}
		int num = (int)Hash[o1];
		int hash = GetHash(o2);
		if (num != hash)
		{
			return false;
		}
		if (shareTypes)
		{
			return CompositeHash(o1, num) == CompositeHash(o2, hash);
		}
		return true;
	}

	private ArrayList GetDependencies(XmlSchemaObject o, ArrayList deps, Hashtable refs)
	{
		if (refs[o] == null)
		{
			refs[o] = o;
			deps.Add(o);
			if (Graph[o] is ArrayList arrayList)
			{
				for (int i = 0; i < arrayList.Count; i++)
				{
					GetDependencies((XmlSchemaObject)arrayList[i], deps, refs);
				}
			}
		}
		return deps;
	}

	private int CompositeHash(XmlSchemaObject o, int hash)
	{
		ArrayList dependencies = GetDependencies(o, new ArrayList(), new Hashtable());
		double num = 0.0;
		for (int i = 0; i < dependencies.Count; i++)
		{
			object obj = Hash[dependencies[i]];
			if (obj is int)
			{
				num += (double)((int)obj / dependencies.Count);
			}
		}
		return (int)num;
	}

	[RequiresUnreferencedCode("creates SchemaGraph")]
	internal void GenerateSchemaGraph(XmlSchemas schemas)
	{
		SchemaGraph schemaGraph = new SchemaGraph(Graph, schemas);
		ArrayList items = schemaGraph.GetItems();
		for (int i = 0; i < items.Count; i++)
		{
			GetHash((XmlSchemaObject)items[i]);
		}
	}

	private int GetHash(XmlSchemaObject o)
	{
		object obj = Hash[o];
		if (obj != null && !(obj is XmlSchemaObject))
		{
			return (int)obj;
		}
		string text = ToString(o, new SchemaObjectWriter());
		looks[o] = text;
		int hashCode = text.GetHashCode();
		Hash[o] = hashCode;
		return hashCode;
	}

	private string ToString(XmlSchemaObject o, SchemaObjectWriter writer)
	{
		return writer.WriteXmlSchemaObject(o);
	}
}
