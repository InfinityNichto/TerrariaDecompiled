using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Xml.Linq;

public sealed class XNamespace
{
	private static XHashtable<WeakReference<XNamespace>> s_namespaces;

	private static WeakReference<XNamespace> s_refNone;

	private static WeakReference<XNamespace> s_refXml;

	private static WeakReference<XNamespace> s_refXmlns;

	private readonly string _namespaceName;

	private readonly int _hashCode;

	private readonly XHashtable<XName> _names;

	public string NamespaceName => _namespaceName;

	public static XNamespace None => EnsureNamespace(ref s_refNone, string.Empty);

	public static XNamespace Xml => EnsureNamespace(ref s_refXml, "http://www.w3.org/XML/1998/namespace");

	public static XNamespace Xmlns => EnsureNamespace(ref s_refXmlns, "http://www.w3.org/2000/xmlns/");

	internal XNamespace(string namespaceName)
	{
		_namespaceName = namespaceName;
		_hashCode = namespaceName.GetHashCode();
		_names = new XHashtable<XName>(ExtractLocalName, 8);
	}

	public XName GetName(string localName)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		return GetName(localName, 0, localName.Length);
	}

	public override string ToString()
	{
		return _namespaceName;
	}

	public static XNamespace Get(string namespaceName)
	{
		if (namespaceName == null)
		{
			throw new ArgumentNullException("namespaceName");
		}
		return Get(namespaceName, 0, namespaceName.Length);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("namespaceName")]
	public static implicit operator XNamespace?(string? namespaceName)
	{
		if (namespaceName == null)
		{
			return null;
		}
		return Get(namespaceName);
	}

	public static XName operator +(XNamespace ns, string localName)
	{
		if (ns == null)
		{
			throw new ArgumentNullException("ns");
		}
		return ns.GetName(localName);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	public static bool operator ==(XNamespace? left, XNamespace? right)
	{
		return (object)left == right;
	}

	public static bool operator !=(XNamespace? left, XNamespace? right)
	{
		return (object)left != right;
	}

	internal XName GetName(string localName, int index, int count)
	{
		if (_names.TryGetValue(localName, index, count, out var value))
		{
			return value;
		}
		return _names.Add(new XName(this, localName.Substring(index, count)));
	}

	internal static XNamespace Get(string namespaceName, int index, int count)
	{
		if (count == 0)
		{
			return None;
		}
		if (s_namespaces == null)
		{
			Interlocked.CompareExchange(ref s_namespaces, new XHashtable<WeakReference<XNamespace>>(ExtractNamespace, 32), null);
		}
		XNamespace xNamespace;
		do
		{
			if (!s_namespaces.TryGetValue(namespaceName, index, count, out var value))
			{
				if (count == "http://www.w3.org/XML/1998/namespace".Length && string.CompareOrdinal(namespaceName, index, "http://www.w3.org/XML/1998/namespace", 0, count) == 0)
				{
					return Xml;
				}
				if (count == "http://www.w3.org/2000/xmlns/".Length && string.CompareOrdinal(namespaceName, index, "http://www.w3.org/2000/xmlns/", 0, count) == 0)
				{
					return Xmlns;
				}
				value = s_namespaces.Add(new WeakReference<XNamespace>(new XNamespace(namespaceName.Substring(index, count))));
			}
			xNamespace = ((value != null && value.TryGetTarget(out var target)) ? target : null);
		}
		while (xNamespace == null);
		return xNamespace;
	}

	private static string ExtractLocalName(XName n)
	{
		return n.LocalName;
	}

	private static string ExtractNamespace(WeakReference<XNamespace> r)
	{
		if (r == null || !r.TryGetTarget(out var target))
		{
			return null;
		}
		return target.NamespaceName;
	}

	private static XNamespace EnsureNamespace(ref WeakReference<XNamespace> refNmsp, string namespaceName)
	{
		XNamespace target;
		while (true)
		{
			WeakReference<XNamespace> weakReference = refNmsp;
			if (weakReference != null && weakReference.TryGetTarget(out target))
			{
				break;
			}
			Interlocked.CompareExchange(ref refNmsp, new WeakReference<XNamespace>(new XNamespace(namespaceName)), weakReference);
		}
		return target;
	}
}
