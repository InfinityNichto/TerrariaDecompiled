using System.Collections.Generic;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class XmlDataNode : DataNode<object>
{
	private IList<XmlAttribute> _xmlAttributes;

	private IList<XmlNode> _xmlChildNodes;

	private XmlDocument _ownerDocument;

	internal IList<XmlAttribute> XmlAttributes
	{
		get
		{
			return _xmlAttributes;
		}
		set
		{
			_xmlAttributes = value;
		}
	}

	internal IList<XmlNode> XmlChildNodes
	{
		get
		{
			return _xmlChildNodes;
		}
		set
		{
			_xmlChildNodes = value;
		}
	}

	internal XmlDocument OwnerDocument
	{
		get
		{
			return _ownerDocument;
		}
		set
		{
			_ownerDocument = value;
		}
	}

	internal XmlDataNode()
	{
		dataType = Globals.TypeOfXmlDataNode;
	}

	public override void Clear()
	{
		base.Clear();
		_xmlAttributes = null;
		_xmlChildNodes = null;
		_ownerDocument = null;
	}
}
