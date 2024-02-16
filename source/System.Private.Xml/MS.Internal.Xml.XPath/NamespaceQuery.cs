using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class NamespaceQuery : BaseAxisQuery
{
	private bool _onNamespace;

	public NamespaceQuery(Query qyParent, string Name, string Prefix, XPathNodeType Type)
		: base(qyParent, Name, Prefix, Type)
	{
	}

	private NamespaceQuery(NamespaceQuery other)
		: base(other)
	{
		_onNamespace = other._onNamespace;
	}

	public override void Reset()
	{
		_onNamespace = false;
		base.Reset();
	}

	public override XPathNavigator Advance()
	{
		do
		{
			if (!_onNamespace)
			{
				currentNode = qyInput.Advance();
				if (currentNode == null)
				{
					return null;
				}
				position = 0;
				currentNode = currentNode.Clone();
				_onNamespace = currentNode.MoveToFirstNamespace();
			}
			else
			{
				_onNamespace = currentNode.MoveToNextNamespace();
			}
		}
		while (!_onNamespace || !matches(currentNode));
		position++;
		return currentNode;
	}

	public override bool matches(XPathNavigator e)
	{
		if (e.Value.Length == 0)
		{
			return false;
		}
		if (base.NameTest)
		{
			return base.Name.Equals(e.LocalName);
		}
		return true;
	}

	public override XPathNodeIterator Clone()
	{
		return new NamespaceQuery(this);
	}
}
