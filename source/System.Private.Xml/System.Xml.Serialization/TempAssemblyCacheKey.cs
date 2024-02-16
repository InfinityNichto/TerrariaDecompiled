using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Serialization;

internal sealed class TempAssemblyCacheKey
{
	private readonly string _ns;

	private readonly object _type;

	internal TempAssemblyCacheKey(string ns, object type)
	{
		_type = type;
		_ns = ns;
	}

	public override bool Equals([NotNullWhen(true)] object o)
	{
		if (!(o is TempAssemblyCacheKey tempAssemblyCacheKey))
		{
			return false;
		}
		if (tempAssemblyCacheKey._type == _type)
		{
			return tempAssemblyCacheKey._ns == _ns;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((_ns != null) ? _ns.GetHashCode() : 0) ^ ((_type != null) ? _type.GetHashCode() : 0);
	}
}
