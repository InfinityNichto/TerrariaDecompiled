using System.Collections;
using System.Xml.XPath;

namespace System.Xml;

public class XmlDocumentFragment : XmlNode
{
	private XmlLinkedNode _lastChild;

	public override string Name => OwnerDocument.strDocumentFragmentName;

	public override string LocalName => OwnerDocument.strDocumentFragmentName;

	public override XmlNodeType NodeType => XmlNodeType.DocumentFragment;

	public override XmlNode? ParentNode => null;

	public override XmlDocument OwnerDocument => (XmlDocument)parentNode;

	public override string InnerXml
	{
		get
		{
			return base.InnerXml;
		}
		set
		{
			RemoveAll();
			XmlLoader xmlLoader = new XmlLoader();
			xmlLoader.ParsePartialContent(this, value, XmlNodeType.Element);
		}
	}

	internal override bool IsContainer => true;

	internal override XmlLinkedNode? LastNode
	{
		get
		{
			return _lastChild;
		}
		set
		{
			_lastChild = value;
		}
	}

	internal override XPathNodeType XPNodeType => XPathNodeType.Root;

	protected internal XmlDocumentFragment(XmlDocument ownerDocument)
	{
		if (ownerDocument == null)
		{
			throw new ArgumentException(System.SR.Xdom_Node_Null_Doc);
		}
		parentNode = ownerDocument;
	}

	public override XmlNode CloneNode(bool deep)
	{
		XmlDocument ownerDocument = OwnerDocument;
		XmlDocumentFragment xmlDocumentFragment = ownerDocument.CreateDocumentFragment();
		if (deep)
		{
			xmlDocumentFragment.CopyChildren(ownerDocument, this, deep);
		}
		return xmlDocumentFragment;
	}

	internal override bool IsValidChildType(XmlNodeType type)
	{
		switch (type)
		{
		case XmlNodeType.Element:
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.EntityReference:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return true;
		case XmlNodeType.XmlDeclaration:
		{
			XmlNode firstChild = FirstChild;
			if (firstChild == null || firstChild.NodeType != XmlNodeType.XmlDeclaration)
			{
				return true;
			}
			return false;
		}
		default:
			return false;
		}
	}

	internal override bool CanInsertAfter(XmlNode newChild, XmlNode refChild)
	{
		if (newChild.NodeType == XmlNodeType.XmlDeclaration)
		{
			if (refChild == null)
			{
				return LastNode == null;
			}
			return false;
		}
		return true;
	}

	internal override bool CanInsertBefore(XmlNode newChild, XmlNode refChild)
	{
		if (newChild.NodeType == XmlNodeType.XmlDeclaration)
		{
			if (refChild != null)
			{
				return refChild == FirstChild;
			}
			return true;
		}
		return true;
	}

	public override void WriteTo(XmlWriter w)
	{
		WriteContentTo(w);
	}

	public override void WriteContentTo(XmlWriter w)
	{
		IEnumerator enumerator = GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				XmlNode xmlNode = (XmlNode)enumerator.Current;
				xmlNode.WriteTo(w);
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}
}
