using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class NameKey
{
	private readonly string _ns;

	private readonly string _name;

	internal NameKey(string name, string ns)
	{
		_name = name;
		_ns = ns;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (!(other is NameKey))
		{
			return false;
		}
		NameKey nameKey = (NameKey)other;
		if (_name == nameKey._name)
		{
			return _ns == nameKey._ns;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((_ns == null) ? "<null>".GetHashCode() : _ns.GetHashCode()) ^ ((_name != null) ? _name.GetHashCode() : 0);
	}
}
