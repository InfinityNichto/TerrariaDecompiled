using System.Xml.XPath;

namespace System.Xml;

public class XmlComment : XmlCharacterData
{
	public override string Name => OwnerDocument.strCommentName;

	public override string LocalName => OwnerDocument.strCommentName;

	public override XmlNodeType NodeType => XmlNodeType.Comment;

	internal override XPathNodeType XPNodeType => XPathNodeType.Comment;

	protected internal XmlComment(string? comment, XmlDocument doc)
		: base(comment, doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateComment(Data);
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteComment(Data);
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}
}
