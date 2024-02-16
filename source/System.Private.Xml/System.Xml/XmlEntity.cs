namespace System.Xml;

public class XmlEntity : XmlNode
{
	private readonly string _publicId;

	private readonly string _systemId;

	private readonly string _notationName;

	private readonly string _name;

	private string _baseURI = string.Empty;

	private XmlLinkedNode _lastChild;

	private bool _childrenFoliating;

	public override bool IsReadOnly => true;

	public override string Name => _name;

	public override string LocalName => _name;

	public override string InnerText
	{
		get
		{
			return base.InnerText;
		}
		set
		{
			throw new InvalidOperationException(System.SR.Xdom_Ent_Innertext);
		}
	}

	internal override bool IsContainer => true;

	internal override XmlLinkedNode? LastNode
	{
		get
		{
			if (_lastChild == null && !_childrenFoliating)
			{
				_childrenFoliating = true;
				XmlLoader xmlLoader = new XmlLoader();
				xmlLoader.ExpandEntity(this);
			}
			return _lastChild;
		}
		set
		{
			_lastChild = value;
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.Entity;

	public string? PublicId => _publicId;

	public string? SystemId => _systemId;

	public string? NotationName => _notationName;

	public override string OuterXml => string.Empty;

	public override string InnerXml
	{
		get
		{
			return string.Empty;
		}
		set
		{
			throw new InvalidOperationException(System.SR.Xdom_Set_InnerXml);
		}
	}

	public override string BaseURI => _baseURI;

	internal XmlEntity(string name, string strdata, string publicId, string systemId, string notationName, XmlDocument doc)
		: base(doc)
	{
		_name = doc.NameTable.Add(name);
		_publicId = publicId;
		_systemId = systemId;
		_notationName = notationName;
		_childrenFoliating = false;
	}

	public override XmlNode CloneNode(bool deep)
	{
		throw new InvalidOperationException(System.SR.Xdom_Node_Cloning);
	}

	internal override bool IsValidChildType(XmlNodeType type)
	{
		if (type != XmlNodeType.Text && type != XmlNodeType.Element && type != XmlNodeType.ProcessingInstruction && type != XmlNodeType.Comment && type != XmlNodeType.CDATA && type != XmlNodeType.Whitespace && type != XmlNodeType.SignificantWhitespace)
		{
			return type == XmlNodeType.EntityReference;
		}
		return true;
	}

	public override void WriteTo(XmlWriter w)
	{
	}

	public override void WriteContentTo(XmlWriter w)
	{
	}

	internal void SetBaseURI(string inBaseURI)
	{
		_baseURI = inBaseURI;
	}
}
