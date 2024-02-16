using System.Collections;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class Key
{
	private readonly XmlQualifiedName _name;

	private readonly int _matchKey;

	private readonly int _useKey;

	private ArrayList _keyNodes;

	public XmlQualifiedName Name => _name;

	public int MatchKey => _matchKey;

	public int UseKey => _useKey;

	public Key(XmlQualifiedName name, int matchkey, int usekey)
	{
		_name = name;
		_matchKey = matchkey;
		_useKey = usekey;
		_keyNodes = null;
	}

	public void AddKey(XPathNavigator root, Hashtable table)
	{
		if (_keyNodes == null)
		{
			_keyNodes = new ArrayList();
		}
		_keyNodes.Add(new DocumentKeyList(root, table));
	}

	public Hashtable GetKeys(XPathNavigator root)
	{
		if (_keyNodes != null)
		{
			for (int i = 0; i < _keyNodes.Count; i++)
			{
				if (((DocumentKeyList)_keyNodes[i]).RootNav.IsSamePosition(root))
				{
					return ((DocumentKeyList)_keyNodes[i]).KeyTable;
				}
			}
		}
		return null;
	}

	public Key Clone()
	{
		return new Key(_name, _matchKey, _useKey);
	}
}
