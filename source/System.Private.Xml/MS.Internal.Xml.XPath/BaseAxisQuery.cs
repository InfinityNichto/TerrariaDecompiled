using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal abstract class BaseAxisQuery : Query
{
	internal Query qyInput;

	private readonly bool _nameTest;

	private readonly string _name;

	private readonly string _prefix;

	private string _nsUri;

	private readonly XPathNodeType _typeTest;

	protected XPathNavigator currentNode;

	protected int position;

	protected string Name => _name;

	protected string Namespace => _nsUri;

	protected bool NameTest => _nameTest;

	protected XPathNodeType TypeTest => _typeTest;

	public override int CurrentPosition => position;

	public override XPathNavigator Current => currentNode;

	public override double XsltDefaultPriority
	{
		get
		{
			if (qyInput.GetType() != typeof(ContextQuery))
			{
				return 0.5;
			}
			if (_name.Length != 0)
			{
				return 0.0;
			}
			if (_prefix.Length != 0)
			{
				return -0.25;
			}
			return -0.5;
		}
	}

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	protected BaseAxisQuery(Query qyInput)
	{
		_name = string.Empty;
		_prefix = string.Empty;
		_nsUri = string.Empty;
		this.qyInput = qyInput;
	}

	protected BaseAxisQuery(Query qyInput, string name, string prefix, XPathNodeType typeTest)
	{
		this.qyInput = qyInput;
		_name = name;
		_prefix = prefix;
		_typeTest = typeTest;
		_nameTest = prefix.Length != 0 || name.Length != 0;
		_nsUri = string.Empty;
	}

	protected BaseAxisQuery(BaseAxisQuery other)
		: base(other)
	{
		qyInput = Query.Clone(other.qyInput);
		_name = other._name;
		_prefix = other._prefix;
		_nsUri = other._nsUri;
		_typeTest = other._typeTest;
		_nameTest = other._nameTest;
		position = other.position;
		currentNode = other.currentNode;
	}

	public override void Reset()
	{
		position = 0;
		currentNode = null;
		qyInput.Reset();
	}

	public override void SetXsltContext(XsltContext context)
	{
		_nsUri = context.LookupNamespace(_prefix);
		qyInput.SetXsltContext(context);
	}

	public virtual bool matches(XPathNavigator e)
	{
		if (TypeTest == e.NodeType || TypeTest == XPathNodeType.All || (TypeTest == XPathNodeType.Text && (e.NodeType == XPathNodeType.Whitespace || e.NodeType == XPathNodeType.SignificantWhitespace)))
		{
			if (!NameTest)
			{
				return true;
			}
			if ((_name.Equals(e.LocalName) || _name.Length == 0) && _nsUri.Equals(e.NamespaceURI))
			{
				return true;
			}
		}
		return false;
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		ResetCount();
		Reset();
		qyInput.Evaluate(nodeIterator);
		return this;
	}
}
