namespace System.Xml;

internal sealed class XmlUnspecifiedAttribute : XmlAttribute
{
	private bool _fSpecified;

	public override bool Specified => _fSpecified;

	public override string InnerText
	{
		set
		{
			base.InnerText = value;
			_fSpecified = true;
		}
	}

	internal XmlUnspecifiedAttribute(string prefix, string localName, string namespaceURI, XmlDocument doc)
		: base(prefix, localName, namespaceURI, doc)
	{
	}

	public override XmlNode CloneNode(bool deep)
	{
		XmlDocument ownerDocument = OwnerDocument;
		XmlUnspecifiedAttribute xmlUnspecifiedAttribute = (XmlUnspecifiedAttribute)ownerDocument.CreateDefaultAttribute(Prefix, LocalName, NamespaceURI);
		xmlUnspecifiedAttribute.CopyChildren(ownerDocument, this, deep: true);
		xmlUnspecifiedAttribute._fSpecified = true;
		return xmlUnspecifiedAttribute;
	}

	public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
	{
		XmlNode result = base.InsertBefore(newChild, refChild);
		_fSpecified = true;
		return result;
	}

	public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
	{
		XmlNode result = base.InsertAfter(newChild, refChild);
		_fSpecified = true;
		return result;
	}

	public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
	{
		XmlNode result = base.ReplaceChild(newChild, oldChild);
		_fSpecified = true;
		return result;
	}

	public override XmlNode RemoveChild(XmlNode oldChild)
	{
		XmlNode result = base.RemoveChild(oldChild);
		_fSpecified = true;
		return result;
	}

	public override XmlNode AppendChild(XmlNode newChild)
	{
		XmlNode result = base.AppendChild(newChild);
		_fSpecified = true;
		return result;
	}

	public override void WriteTo(XmlWriter w)
	{
		if (_fSpecified)
		{
			base.WriteTo(w);
		}
	}

	internal void SetSpecified(bool f)
	{
		_fSpecified = f;
	}
}
