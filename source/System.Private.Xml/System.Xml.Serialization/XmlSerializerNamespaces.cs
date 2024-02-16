using System.Collections;
using System.Collections.Generic;
using System.Xml.Extensions;

namespace System.Xml.Serialization;

public class XmlSerializerNamespaces
{
	private Dictionary<string, XmlQualifiedName> _namespaces;

	public int Count
	{
		get
		{
			if (_namespaces != null)
			{
				return _namespaces.Count;
			}
			return 0;
		}
	}

	internal Dictionary<string, XmlQualifiedName>.ValueCollection Namespaces => NamespacesInternal.Values;

	private Dictionary<string, XmlQualifiedName> NamespacesInternal
	{
		get
		{
			if (_namespaces == null)
			{
				_namespaces = new Dictionary<string, XmlQualifiedName>();
			}
			return _namespaces;
		}
	}

	internal ArrayList? NamespaceList
	{
		get
		{
			if (_namespaces == null || _namespaces.Count == 0)
			{
				return null;
			}
			return new ArrayList(_namespaces.Values);
		}
	}

	public XmlSerializerNamespaces()
	{
	}

	public XmlSerializerNamespaces(XmlSerializerNamespaces namespaces)
	{
		_namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.NamespacesInternal);
	}

	public XmlSerializerNamespaces(XmlQualifiedName[] namespaces)
	{
		_namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.Length);
		foreach (XmlQualifiedName xmlQualifiedName in namespaces)
		{
			_namespaces.Add(xmlQualifiedName.Name, xmlQualifiedName);
		}
	}

	internal XmlSerializerNamespaces(IList<XmlQualifiedName> namespaces)
	{
		_namespaces = new Dictionary<string, XmlQualifiedName>(namespaces.Count);
		foreach (XmlQualifiedName @namespace in namespaces)
		{
			_namespaces.Add(@namespace.Name, @namespace);
		}
	}

	public void Add(string prefix, string? ns)
	{
		if (prefix != null && prefix.Length > 0)
		{
			XmlConvert.VerifyNCName(prefix);
		}
		if (ns != null && ns.Length > 0)
		{
			ExtensionMethods.ToUri(ns);
		}
		AddInternal(prefix, ns);
	}

	internal void AddInternal(string prefix, string ns)
	{
		NamespacesInternal[prefix] = new XmlQualifiedName(prefix, ns);
	}

	public XmlQualifiedName[] ToArray()
	{
		if (_namespaces == null || _namespaces.Count == 0)
		{
			return Array.Empty<XmlQualifiedName>();
		}
		XmlQualifiedName[] array = new XmlQualifiedName[_namespaces.Count];
		_namespaces.Values.CopyTo(array, 0);
		return array;
	}

	internal bool TryLookupPrefix(string ns, out string prefix)
	{
		prefix = null;
		if (_namespaces == null || _namespaces.Count == 0 || string.IsNullOrEmpty(ns))
		{
			return false;
		}
		foreach (KeyValuePair<string, XmlQualifiedName> @namespace in _namespaces)
		{
			if (!string.IsNullOrEmpty(@namespace.Key) && @namespace.Value.Namespace == ns)
			{
				prefix = @namespace.Key;
				return true;
			}
		}
		return false;
	}

	internal bool TryLookupNamespace(string prefix, out string ns)
	{
		ns = null;
		if (_namespaces == null || _namespaces.Count == 0 || string.IsNullOrEmpty(prefix))
		{
			return false;
		}
		if (_namespaces.TryGetValue(prefix, out var value))
		{
			ns = value.Namespace;
			return true;
		}
		return false;
	}
}
