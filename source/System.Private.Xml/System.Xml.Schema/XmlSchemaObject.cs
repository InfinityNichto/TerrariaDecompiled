using System.Xml.Serialization;

namespace System.Xml.Schema;

public abstract class XmlSchemaObject
{
	private int _lineNum;

	private int _linePos;

	private string _sourceUri;

	private XmlSerializerNamespaces _namespaces;

	private XmlSchemaObject _parent;

	private bool _isProcessing;

	[XmlIgnore]
	public int LineNumber
	{
		get
		{
			return _lineNum;
		}
		set
		{
			_lineNum = value;
		}
	}

	[XmlIgnore]
	public int LinePosition
	{
		get
		{
			return _linePos;
		}
		set
		{
			_linePos = value;
		}
	}

	[XmlIgnore]
	public string? SourceUri
	{
		get
		{
			return _sourceUri;
		}
		set
		{
			_sourceUri = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaObject? Parent
	{
		get
		{
			return _parent;
		}
		set
		{
			_parent = value;
		}
	}

	[XmlNamespaceDeclarations]
	public XmlSerializerNamespaces Namespaces
	{
		get
		{
			if (_namespaces == null)
			{
				_namespaces = new XmlSerializerNamespaces();
			}
			return _namespaces;
		}
		set
		{
			_namespaces = value;
		}
	}

	[XmlIgnore]
	internal virtual string? IdAttribute
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	[XmlIgnore]
	internal virtual string? NameAttribute
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	[XmlIgnore]
	internal bool IsProcessing
	{
		get
		{
			return _isProcessing;
		}
		set
		{
			_isProcessing = value;
		}
	}

	internal virtual void OnAdd(XmlSchemaObjectCollection container, object item)
	{
	}

	internal virtual void OnRemove(XmlSchemaObjectCollection container, object item)
	{
	}

	internal virtual void OnClear(XmlSchemaObjectCollection container)
	{
	}

	internal virtual void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
	{
	}

	internal virtual void AddAnnotation(XmlSchemaAnnotation annotation)
	{
	}

	internal virtual XmlSchemaObject Clone()
	{
		return (XmlSchemaObject)MemberwiseClone();
	}
}
