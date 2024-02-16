using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal class Axis : AstNode
{
	public enum AxisType
	{
		Ancestor,
		AncestorOrSelf,
		Attribute,
		Child,
		Descendant,
		DescendantOrSelf,
		Following,
		FollowingSibling,
		Namespace,
		Parent,
		Preceding,
		PrecedingSibling,
		Self,
		None
	}

	private readonly AxisType _axisType;

	private AstNode _input;

	private readonly string _prefix;

	private readonly string _name;

	private readonly XPathNodeType _nodeType;

	protected bool abbrAxis;

	private string _urn = string.Empty;

	public override AstType Type => AstType.Axis;

	public override XPathResultType ReturnType => XPathResultType.NodeSet;

	public AstNode Input
	{
		get
		{
			return _input;
		}
		set
		{
			_input = value;
		}
	}

	public string Prefix => _prefix;

	public string Name => _name;

	public XPathNodeType NodeType => _nodeType;

	public AxisType TypeOfAxis => _axisType;

	public bool AbbrAxis => abbrAxis;

	public string Urn
	{
		get
		{
			return _urn;
		}
		set
		{
			_urn = value;
		}
	}

	public Axis(AxisType axisType, AstNode input, string prefix, string name, XPathNodeType nodetype)
	{
		_axisType = axisType;
		_input = input;
		_prefix = prefix;
		_name = name;
		_nodeType = nodetype;
	}

	public Axis(AxisType axisType, AstNode input)
		: this(axisType, input, string.Empty, string.Empty, XPathNodeType.All)
	{
		abbrAxis = true;
	}
}
