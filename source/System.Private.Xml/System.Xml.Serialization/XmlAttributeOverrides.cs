using System.Collections.Generic;

namespace System.Xml.Serialization;

public class XmlAttributeOverrides
{
	private readonly Dictionary<Type, Dictionary<string, XmlAttributes>> _types = new Dictionary<Type, Dictionary<string, XmlAttributes>>();

	public XmlAttributes? this[Type type] => this[type, string.Empty];

	public XmlAttributes? this[Type type, string member]
	{
		get
		{
			if (!_types.TryGetValue(type, out var value) || !value.TryGetValue(member, out var value2))
			{
				return null;
			}
			return value2;
		}
	}

	public void Add(Type type, XmlAttributes attributes)
	{
		Add(type, string.Empty, attributes);
	}

	public void Add(Type type, string member, XmlAttributes? attributes)
	{
		if (!_types.TryGetValue(type, out var value))
		{
			value = new Dictionary<string, XmlAttributes>();
			_types.Add(type, value);
		}
		else if (value.ContainsKey(member))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlAttributeSetAgain, type.FullName, member));
		}
		value.Add(member, attributes);
	}
}
