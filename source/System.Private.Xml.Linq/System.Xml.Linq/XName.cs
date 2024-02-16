using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Xml.Linq;

public sealed class XName : IEquatable<XName>, ISerializable
{
	private readonly XNamespace _ns;

	private readonly string _localName;

	private readonly int _hashCode;

	public string LocalName => _localName;

	public XNamespace Namespace => _ns;

	public string NamespaceName => _ns.NamespaceName;

	internal XName(XNamespace ns, string localName)
	{
		_ns = ns;
		_localName = XmlConvert.VerifyNCName(localName);
		_hashCode = ns.GetHashCode() ^ localName.GetHashCode();
	}

	public override string ToString()
	{
		if (_ns.NamespaceName.Length == 0)
		{
			return _localName;
		}
		return "{" + _ns.NamespaceName + "}" + _localName;
	}

	public static XName Get(string expandedName)
	{
		if (expandedName == null)
		{
			throw new ArgumentNullException("expandedName");
		}
		if (expandedName.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidExpandedName, expandedName));
		}
		if (expandedName[0] == '{')
		{
			int num = expandedName.LastIndexOf('}');
			if (num <= 1 || num == expandedName.Length - 1)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidExpandedName, expandedName));
			}
			return XNamespace.Get(expandedName, 1, num - 1).GetName(expandedName, num + 1, expandedName.Length - num - 1);
		}
		return XNamespace.None.GetName(expandedName);
	}

	public static XName Get(string localName, string namespaceName)
	{
		return XNamespace.Get(namespaceName).GetName(localName);
	}

	[CLSCompliant(false)]
	[return: NotNullIfNotNull("expandedName")]
	public static implicit operator XName?(string? expandedName)
	{
		if (expandedName == null)
		{
			return null;
		}
		return Get(expandedName);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	public static bool operator ==(XName? left, XName? right)
	{
		return (object)left == right;
	}

	public static bool operator !=(XName? left, XName? right)
	{
		return (object)left != right;
	}

	bool IEquatable<XName>.Equals(XName other)
	{
		return (object)this == other;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}
}
