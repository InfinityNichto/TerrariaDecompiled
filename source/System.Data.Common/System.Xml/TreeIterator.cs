namespace System.Xml;

internal sealed class TreeIterator : BaseTreeIterator
{
	private readonly XmlNode _nodeTop;

	private XmlNode _currentNode;

	internal override XmlNode CurrentNode => _currentNode;

	internal TreeIterator(XmlNode nodeTop)
		: base(((XmlDataDocument)nodeTop.OwnerDocument).Mapper)
	{
		_nodeTop = nodeTop;
		_currentNode = nodeTop;
	}

	internal override bool Next()
	{
		XmlNode firstChild = _currentNode.FirstChild;
		if (firstChild != null)
		{
			_currentNode = firstChild;
			return true;
		}
		return NextRight();
	}

	internal override bool NextRight()
	{
		if (_currentNode == _nodeTop)
		{
			_currentNode = null;
			return false;
		}
		XmlNode nextSibling = _currentNode.NextSibling;
		if (nextSibling != null)
		{
			_currentNode = nextSibling;
			return true;
		}
		nextSibling = _currentNode;
		while (nextSibling != _nodeTop && nextSibling.NextSibling == null)
		{
			nextSibling = nextSibling.ParentNode;
		}
		if (nextSibling == _nodeTop)
		{
			_currentNode = null;
			return false;
		}
		_currentNode = nextSibling.NextSibling;
		return true;
	}
}
