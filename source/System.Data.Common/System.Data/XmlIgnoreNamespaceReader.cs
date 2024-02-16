using System.Collections.Generic;
using System.Xml;

namespace System.Data;

internal sealed class XmlIgnoreNamespaceReader : XmlNodeReader
{
	private readonly List<string> _namespacesToIgnore;

	internal XmlIgnoreNamespaceReader(XmlDocument xdoc, string[] namespacesToIgnore)
		: base(xdoc)
	{
		_namespacesToIgnore = new List<string>(namespacesToIgnore);
	}

	public override bool MoveToFirstAttribute()
	{
		if (base.MoveToFirstAttribute())
		{
			if (_namespacesToIgnore.Contains(NamespaceURI) || (NamespaceURI == "http://www.w3.org/XML/1998/namespace" && LocalName != "lang"))
			{
				return MoveToNextAttribute();
			}
			return true;
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		bool result;
		bool flag;
		do
		{
			result = false;
			flag = false;
			if (base.MoveToNextAttribute())
			{
				result = true;
				if (_namespacesToIgnore.Contains(NamespaceURI) || (NamespaceURI == "http://www.w3.org/XML/1998/namespace" && LocalName != "lang"))
				{
					flag = true;
				}
			}
		}
		while (flag);
		return result;
	}
}
