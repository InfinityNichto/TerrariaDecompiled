using System.Collections;

namespace System.Xml;

public class XmlEntityReference : XmlLinkedNode
{
	private readonly string _name;

	private XmlLinkedNode _lastChild;

	public override string Name => _name;

	public override string LocalName => _name;

	public override string? Value
	{
		get
		{
			return null;
		}
		set
		{
			throw new InvalidOperationException(System.SR.Xdom_EntRef_SetVal);
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.EntityReference;

	public override bool IsReadOnly => true;

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

	public override string BaseURI => OwnerDocument.BaseURI;

	internal string ChildBaseURI
	{
		get
		{
			XmlEntity entityNode = OwnerDocument.GetEntityNode(_name);
			if (entityNode != null)
			{
				if (!string.IsNullOrEmpty(entityNode.SystemId))
				{
					return ConstructBaseURI(entityNode.BaseURI, entityNode.SystemId);
				}
				return entityNode.BaseURI;
			}
			return string.Empty;
		}
	}

	protected internal XmlEntityReference(string name, XmlDocument doc)
		: base(doc)
	{
		if (!doc.IsLoading && name.Length > 0 && name[0] == '#')
		{
			throw new ArgumentException(System.SR.Xdom_InvalidCharacter_EntityReference);
		}
		_name = doc.NameTable.Add(name);
		doc.fEntRefNodesPresent = true;
	}

	public override XmlNode CloneNode(bool deep)
	{
		return OwnerDocument.CreateEntityReference(_name);
	}

	internal override void SetParent(XmlNode node)
	{
		base.SetParent(node);
		if (LastNode == null && node != null && node != OwnerDocument)
		{
			XmlLoader xmlLoader = new XmlLoader();
			xmlLoader.ExpandEntityReference(this);
		}
	}

	internal override void SetParentForLoad(XmlNode node)
	{
		SetParent(node);
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
		default:
			return false;
		}
	}

	public override void WriteTo(XmlWriter w)
	{
		w.WriteEntityRef(_name);
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

	private string ConstructBaseURI(string baseURI, string systemId)
	{
		if (baseURI == null)
		{
			return systemId;
		}
		int num = baseURI.LastIndexOf('/') + 1;
		string text = baseURI;
		if (num > 0 && num < baseURI.Length)
		{
			text = baseURI.Substring(0, num);
		}
		else if (num == 0)
		{
			text += "\\";
		}
		return text + systemId.Replace('\\', '/');
	}
}
