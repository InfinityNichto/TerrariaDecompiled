using System.Threading;
using System.Threading.Tasks;

namespace System.Xml.Linq;

public class XDocumentType : XNode
{
	private string _name;

	private string _publicId;

	private string _systemId;

	private string _internalSubset;

	public string? InternalSubset
	{
		get
		{
			return _internalSubset;
		}
		set
		{
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			_internalSubset = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			value = XmlConvert.VerifyName(value);
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Name);
			_name = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Name);
			}
		}
	}

	public override XmlNodeType NodeType => XmlNodeType.DocumentType;

	public string? PublicId
	{
		get
		{
			return _publicId;
		}
		set
		{
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			_publicId = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public string? SystemId
	{
		get
		{
			return _systemId;
		}
		set
		{
			bool flag = NotifyChanging(this, XObjectChangeEventArgs.Value);
			_systemId = value;
			if (flag)
			{
				NotifyChanged(this, XObjectChangeEventArgs.Value);
			}
		}
	}

	public XDocumentType(string name, string? publicId, string? systemId, string? internalSubset)
	{
		_name = XmlConvert.VerifyName(name);
		_publicId = publicId;
		_systemId = systemId;
		_internalSubset = internalSubset;
	}

	public XDocumentType(XDocumentType other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		_name = other._name;
		_publicId = other._publicId;
		_systemId = other._systemId;
		_internalSubset = other._internalSubset;
	}

	internal XDocumentType(XmlReader r)
	{
		_name = r.Name;
		_publicId = r.GetAttribute("PUBLIC");
		_systemId = r.GetAttribute("SYSTEM");
		_internalSubset = r.Value;
		r.Read();
	}

	public override void WriteTo(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WriteDocType(_name, _publicId, _systemId, _internalSubset);
	}

	public override Task WriteToAsync(XmlWriter writer, CancellationToken cancellationToken)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		return writer.WriteDocTypeAsync(_name, _publicId, _systemId, _internalSubset);
	}

	internal override XNode CloneNode()
	{
		return new XDocumentType(this);
	}

	internal override bool DeepEquals(XNode node)
	{
		if (node is XDocumentType xDocumentType && _name == xDocumentType._name && _publicId == xDocumentType._publicId && _systemId == xDocumentType.SystemId)
		{
			return _internalSubset == xDocumentType._internalSubset;
		}
		return false;
	}

	internal override int GetDeepHashCode()
	{
		return _name.GetHashCode() ^ ((_publicId != null) ? _publicId.GetHashCode() : 0) ^ ((_systemId != null) ? _systemId.GetHashCode() : 0) ^ ((_internalSubset != null) ? _internalSubset.GetHashCode() : 0);
	}
}
