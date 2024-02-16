using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml;

internal sealed class RegionIterator : BaseRegionIterator
{
	private readonly XmlBoundElement _rowElement;

	private XmlNode _currentNode;

	internal override XmlNode CurrentNode => _currentNode;

	internal RegionIterator(XmlBoundElement rowElement)
		: base(((XmlDataDocument)rowElement.OwnerDocument).Mapper)
	{
		_rowElement = rowElement;
		_currentNode = rowElement;
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal override bool Next()
	{
		ElementState elementState = _rowElement.ElementState;
		XmlNode firstChild = _currentNode.FirstChild;
		if (firstChild != null)
		{
			_currentNode = firstChild;
			_rowElement.ElementState = elementState;
			return true;
		}
		return NextRight();
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal override bool NextRight()
	{
		if (_currentNode == _rowElement)
		{
			_currentNode = null;
			return false;
		}
		ElementState elementState = _rowElement.ElementState;
		XmlNode nextSibling = _currentNode.NextSibling;
		if (nextSibling != null)
		{
			_currentNode = nextSibling;
			_rowElement.ElementState = elementState;
			return true;
		}
		nextSibling = _currentNode;
		while (nextSibling != _rowElement && nextSibling.NextSibling == null)
		{
			nextSibling = nextSibling.ParentNode;
		}
		if (nextSibling == _rowElement)
		{
			_currentNode = null;
			_rowElement.ElementState = elementState;
			return false;
		}
		_currentNode = nextSibling.NextSibling;
		_rowElement.ElementState = elementState;
		return true;
	}

	[MemberNotNullWhen(true, "CurrentNode")]
	internal bool NextInitialTextLikeNodes(out string value)
	{
		ElementState elementState = _rowElement.ElementState;
		XmlNode n = CurrentNode.FirstChild;
		value = GetInitialTextFromNodes(ref n);
		if (n == null)
		{
			_rowElement.ElementState = elementState;
			return NextRight();
		}
		_currentNode = n;
		_rowElement.ElementState = elementState;
		return true;
	}

	private static string GetInitialTextFromNodes(ref XmlNode n)
	{
		string text = null;
		if (n != null)
		{
			while (n.NodeType == XmlNodeType.Whitespace)
			{
				n = n.NextSibling;
				if (n == null)
				{
					return string.Empty;
				}
			}
			if (XmlDataDocument.IsTextLikeNode(n) && (n.NextSibling == null || !XmlDataDocument.IsTextLikeNode(n.NextSibling)))
			{
				text = n.Value;
				n = n.NextSibling;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				while (n != null && XmlDataDocument.IsTextLikeNode(n))
				{
					if (n.NodeType != XmlNodeType.Whitespace)
					{
						stringBuilder.Append(n.Value);
					}
					n = n.NextSibling;
				}
				text = stringBuilder.ToString();
			}
		}
		return text ?? string.Empty;
	}
}
