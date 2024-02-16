using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Xml;

internal sealed class XmlBoundElement : XmlElement
{
	private DataRow _row;

	private ElementState _state;

	public override XmlAttributeCollection Attributes
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			AutoFoliate();
			return base.Attributes;
		}
	}

	public override bool HasAttributes => Attributes.Count > 0;

	public override XmlNode FirstChild
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			AutoFoliate();
			return base.FirstChild;
		}
	}

	internal XmlNode SafeFirstChild => base.FirstChild;

	public override XmlNode LastChild
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			AutoFoliate();
			return base.LastChild;
		}
	}

	public override XmlNode PreviousSibling
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			XmlNode previousSibling = base.PreviousSibling;
			if (previousSibling == null && ParentNode is XmlBoundElement xmlBoundElement)
			{
				xmlBoundElement.AutoFoliate();
				return base.PreviousSibling;
			}
			return previousSibling;
		}
	}

	internal XmlNode SafePreviousSibling => base.PreviousSibling;

	public override XmlNode NextSibling
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			XmlNode nextSibling = base.NextSibling;
			if (nextSibling == null && ParentNode is XmlBoundElement xmlBoundElement)
			{
				xmlBoundElement.AutoFoliate();
				return base.NextSibling;
			}
			return nextSibling;
		}
	}

	internal XmlNode SafeNextSibling => base.NextSibling;

	public override bool HasChildNodes
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			AutoFoliate();
			return base.HasChildNodes;
		}
	}

	public override string InnerXml
	{
		get
		{
			return base.InnerXml;
		}
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		set
		{
			RemoveAllChildren();
			XmlDataDocument xmlDataDocument = (XmlDataDocument)OwnerDocument;
			bool ignoreXmlEvents = xmlDataDocument.IgnoreXmlEvents;
			bool ignoreDataSetEvents = xmlDataDocument.IgnoreDataSetEvents;
			xmlDataDocument.IgnoreXmlEvents = true;
			xmlDataDocument.IgnoreDataSetEvents = true;
			base.InnerXml = value;
			xmlDataDocument.SyncTree(this);
			xmlDataDocument.IgnoreDataSetEvents = ignoreDataSetEvents;
			xmlDataDocument.IgnoreXmlEvents = ignoreXmlEvents;
		}
	}

	internal DataRow Row
	{
		get
		{
			return _row;
		}
		set
		{
			_row = value;
		}
	}

	internal bool IsFoliated
	{
		get
		{
			while (_state == ElementState.Foliating || _state == ElementState.Defoliating)
			{
				Thread.Sleep(0);
			}
			return _state != ElementState.Defoliated;
		}
	}

	internal ElementState ElementState
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlBoundElement(string prefix, string localName, string namespaceURI, XmlDocument doc)
		: base(prefix, localName, namespaceURI, doc)
	{
		_state = ElementState.None;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNode InsertBefore(XmlNode newChild, XmlNode refChild)
	{
		AutoFoliate();
		return base.InsertBefore(newChild, refChild);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNode InsertAfter(XmlNode newChild, XmlNode refChild)
	{
		AutoFoliate();
		return base.InsertAfter(newChild, refChild);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild)
	{
		AutoFoliate();
		return base.ReplaceChild(newChild, oldChild);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNode AppendChild(XmlNode newChild)
	{
		AutoFoliate();
		return base.AppendChild(newChild);
	}

	internal void RemoveAllChildren()
	{
		XmlNode xmlNode = FirstChild;
		XmlNode xmlNode2 = null;
		while (xmlNode != null)
		{
			xmlNode2 = xmlNode.NextSibling;
			RemoveChild(xmlNode);
			xmlNode = xmlNode2;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	internal void Foliate(ElementState newState)
	{
		((XmlDataDocument)OwnerDocument)?.Foliate(this, newState);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void AutoFoliate()
	{
		XmlDataDocument xmlDataDocument = (XmlDataDocument)OwnerDocument;
		xmlDataDocument?.Foliate(this, xmlDataDocument.AutoFoliationState);
	}

	public override XmlNode CloneNode(bool deep)
	{
		XmlDataDocument xmlDataDocument = (XmlDataDocument)OwnerDocument;
		ElementState autoFoliationState = xmlDataDocument.AutoFoliationState;
		xmlDataDocument.AutoFoliationState = ElementState.WeakFoliation;
		try
		{
			Foliate(ElementState.WeakFoliation);
			return (XmlElement)base.CloneNode(deep);
		}
		finally
		{
			xmlDataDocument.AutoFoliationState = autoFoliationState;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override void WriteContentTo(XmlWriter w)
	{
		DataPointer dataPointer = new DataPointer((XmlDataDocument)OwnerDocument, this);
		try
		{
			dataPointer.AddPointer();
			WriteBoundElementContentTo(dataPointer, w);
		}
		finally
		{
			dataPointer.SetNoLongerUse();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override void WriteTo(XmlWriter w)
	{
		DataPointer dataPointer = new DataPointer((XmlDataDocument)OwnerDocument, this);
		try
		{
			dataPointer.AddPointer();
			WriteRootBoundElementTo(dataPointer, w);
		}
		finally
		{
			dataPointer.SetNoLongerUse();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void WriteRootBoundElementTo(DataPointer dp, XmlWriter w)
	{
		XmlDataDocument xmlDataDocument = (XmlDataDocument)OwnerDocument;
		w.WriteStartElement(dp.Prefix, dp.LocalName, dp.NamespaceURI);
		int attributeCount = dp.AttributeCount;
		bool flag = false;
		if (attributeCount > 0)
		{
			for (int i = 0; i < attributeCount; i++)
			{
				dp.MoveToAttribute(i);
				if (dp.Prefix == "xmlns" && dp.LocalName == "xsi")
				{
					flag = true;
				}
				WriteTo(dp, w);
				dp.MoveToOwnerElement();
			}
		}
		if (!flag && xmlDataDocument._bLoadFromDataSet && xmlDataDocument._bHasXSINIL)
		{
			w.WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2000/xmlns/", "http://www.w3.org/2001/XMLSchema-instance");
		}
		WriteBoundElementContentTo(dp, w);
		if (dp.IsEmptyElement)
		{
			w.WriteEndElement();
		}
		else
		{
			w.WriteFullEndElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void WriteBoundElementTo(DataPointer dp, XmlWriter w)
	{
		w.WriteStartElement(dp.Prefix, dp.LocalName, dp.NamespaceURI);
		int attributeCount = dp.AttributeCount;
		if (attributeCount > 0)
		{
			for (int i = 0; i < attributeCount; i++)
			{
				dp.MoveToAttribute(i);
				WriteTo(dp, w);
				dp.MoveToOwnerElement();
			}
		}
		WriteBoundElementContentTo(dp, w);
		if (dp.IsEmptyElement)
		{
			w.WriteEndElement();
		}
		else
		{
			w.WriteFullEndElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void WriteBoundElementContentTo(DataPointer dp, XmlWriter w)
	{
		if (!dp.IsEmptyElement && dp.MoveToFirstChild())
		{
			do
			{
				WriteTo(dp, w);
			}
			while (dp.MoveToNextSibling());
			dp.MoveToParent();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void WriteTo(DataPointer dp, XmlWriter w)
	{
		switch (dp.NodeType)
		{
		case XmlNodeType.Attribute:
			if (dp.IsDefault)
			{
				break;
			}
			w.WriteStartAttribute(dp.Prefix, dp.LocalName, dp.NamespaceURI);
			if (dp.MoveToFirstChild())
			{
				do
				{
					WriteTo(dp, w);
				}
				while (dp.MoveToNextSibling());
				dp.MoveToParent();
			}
			w.WriteEndAttribute();
			break;
		case XmlNodeType.Element:
			WriteBoundElementTo(dp, w);
			break;
		case XmlNodeType.Text:
			w.WriteString(dp.Value);
			break;
		default:
			if (dp.GetNode() != null)
			{
				dp.GetNode().WriteTo(w);
			}
			break;
		}
	}

	public override XmlNodeList GetElementsByTagName(string name)
	{
		XmlNodeList elementsByTagName = base.GetElementsByTagName(name);
		_ = elementsByTagName.Count;
		return elementsByTagName;
	}
}
