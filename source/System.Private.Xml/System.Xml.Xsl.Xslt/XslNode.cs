using System.Collections.Generic;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal class XslNode
{
	public readonly XslNodeType NodeType;

	public ISourceLineInfo SourceLine;

	public NsDecl Namespaces;

	public readonly QilName Name;

	public readonly object Arg;

	public readonly XslVersion XslVersion;

	public XslFlags Flags;

	private List<XslNode> _content;

	private static readonly IList<XslNode> s_emptyList = new List<XslNode>().AsReadOnly();

	public string Select => (string)Arg;

	public bool ForwardsCompatible => XslVersion == XslVersion.ForwardsCompatible;

	public IList<XslNode> Content
	{
		get
		{
			IList<XslNode> content = _content;
			return content ?? s_emptyList;
		}
	}

	public XslNode(XslNodeType nodeType, QilName name, object arg, XslVersion xslVer)
	{
		NodeType = nodeType;
		Name = name;
		Arg = arg;
		XslVersion = xslVer;
	}

	public XslNode(XslNodeType nodeType)
	{
		NodeType = nodeType;
		XslVersion = XslVersion.Version10;
	}

	public void SetContent(List<XslNode> content)
	{
		_content = content;
	}

	public void AddContent(XslNode node)
	{
		if (_content == null)
		{
			_content = new List<XslNode>();
		}
		_content.Add(node);
	}

	public void InsertContent(IEnumerable<XslNode> collection)
	{
		if (_content == null)
		{
			_content = new List<XslNode>(collection);
		}
		else
		{
			_content.InsertRange(0, collection);
		}
	}
}
