using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class XmlILIndex
{
	private readonly Dictionary<string, XmlQueryNodeSequence> _table;

	internal XmlILIndex()
	{
		_table = new Dictionary<string, XmlQueryNodeSequence>();
	}

	public void Add(string key, XPathNavigator navigator)
	{
		if (!_table.TryGetValue(key, out var value))
		{
			value = new XmlQueryNodeSequence();
			value.AddClone(navigator);
			_table.Add(key, value);
		}
		else if (!navigator.IsSamePosition(value[value.Count - 1]))
		{
			value.AddClone(navigator);
		}
	}

	public XmlQueryNodeSequence Lookup(string key)
	{
		if (!_table.TryGetValue(key, out var value))
		{
			return new XmlQueryNodeSequence();
		}
		return value;
	}
}
