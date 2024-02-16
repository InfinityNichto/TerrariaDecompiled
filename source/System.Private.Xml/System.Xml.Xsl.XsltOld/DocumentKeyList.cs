using System.Collections;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal struct DocumentKeyList
{
	private readonly XPathNavigator _rootNav;

	private readonly Hashtable _keyTable;

	public XPathNavigator RootNav => _rootNav;

	public Hashtable KeyTable => _keyTable;

	public DocumentKeyList(XPathNavigator rootNav, Hashtable keyTable)
	{
		_rootNav = rootNav;
		_keyTable = keyTable;
	}
}
