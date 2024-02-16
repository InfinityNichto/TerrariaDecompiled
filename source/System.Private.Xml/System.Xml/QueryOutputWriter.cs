using System.Collections.Generic;

namespace System.Xml;

internal sealed class QueryOutputWriter : XmlRawWriter
{
	private readonly XmlRawWriter _wrapped;

	private bool _inCDataSection;

	private readonly Dictionary<XmlQualifiedName, int> _lookupCDataElems;

	private readonly BitStack _bitsCData;

	private readonly XmlQualifiedName _qnameCData;

	private bool _outputDocType;

	private readonly bool _checkWellFormedDoc;

	private bool _hasDocElem;

	private bool _inAttr;

	private readonly string _systemId;

	private readonly string _publicId;

	private int _depth;

	internal override IXmlNamespaceResolver NamespaceResolver
	{
		set
		{
			_resolver = value;
			_wrapped.NamespaceResolver = value;
		}
	}

	public override XmlWriterSettings Settings
	{
		get
		{
			XmlWriterSettings settings = _wrapped.Settings;
			settings.ReadOnly = false;
			settings.DocTypeSystem = _systemId;
			settings.DocTypePublic = _publicId;
			settings.ReadOnly = true;
			return settings;
		}
	}

	internal override bool SupportsNamespaceDeclarationInChunks => _wrapped.SupportsNamespaceDeclarationInChunks;

	public QueryOutputWriter(XmlRawWriter writer, XmlWriterSettings settings)
	{
		_wrapped = writer;
		_systemId = settings.DocTypeSystem;
		_publicId = settings.DocTypePublic;
		if (settings.OutputMethod == XmlOutputMethod.Xml)
		{
			if (_systemId != null)
			{
				_outputDocType = true;
				_checkWellFormedDoc = true;
			}
			if (settings.AutoXmlDeclaration && settings.Standalone == XmlStandalone.Yes)
			{
				_checkWellFormedDoc = true;
			}
			if (settings.CDataSectionElements.Count <= 0)
			{
				return;
			}
			_bitsCData = new BitStack();
			_lookupCDataElems = new Dictionary<XmlQualifiedName, int>();
			_qnameCData = new XmlQualifiedName();
			foreach (XmlQualifiedName cDataSectionElement in settings.CDataSectionElements)
			{
				_lookupCDataElems[cDataSectionElement] = 0;
			}
			_bitsCData.PushBit(bit: false);
		}
		else if (settings.OutputMethod == XmlOutputMethod.Html && (_systemId != null || _publicId != null))
		{
			_outputDocType = true;
		}
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
		_wrapped.WriteXmlDeclaration(standalone);
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
		_wrapped.WriteXmlDeclaration(xmldecl);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		if (_publicId == null && _systemId == null)
		{
			_wrapped.WriteDocType(name, pubid, sysid, subset);
		}
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		EndCDataSection();
		if (_checkWellFormedDoc)
		{
			if (_depth == 0 && _hasDocElem)
			{
				throw new XmlException(System.SR.Xml_NoMultipleRoots, string.Empty);
			}
			_depth++;
			_hasDocElem = true;
		}
		if (_outputDocType)
		{
			_wrapped.WriteDocType(string.IsNullOrEmpty(prefix) ? localName : (prefix + ":" + localName), _publicId, _systemId, null);
			_outputDocType = false;
		}
		_wrapped.WriteStartElement(prefix, localName, ns);
		if (_lookupCDataElems != null)
		{
			_qnameCData.Init(localName, ns);
			_bitsCData.PushBit(_lookupCDataElems.ContainsKey(_qnameCData));
		}
	}

	internal override void WriteEndElement(string prefix, string localName, string ns)
	{
		EndCDataSection();
		_wrapped.WriteEndElement(prefix, localName, ns);
		if (_checkWellFormedDoc)
		{
			_depth--;
		}
		if (_lookupCDataElems != null)
		{
			_bitsCData.PopBit();
		}
	}

	internal override void WriteFullEndElement(string prefix, string localName, string ns)
	{
		EndCDataSection();
		_wrapped.WriteFullEndElement(prefix, localName, ns);
		if (_checkWellFormedDoc)
		{
			_depth--;
		}
		if (_lookupCDataElems != null)
		{
			_bitsCData.PopBit();
		}
	}

	internal override void StartElementContent()
	{
		_wrapped.StartElementContent();
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		_inAttr = true;
		_wrapped.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteEndAttribute()
	{
		_inAttr = false;
		_wrapped.WriteEndAttribute();
	}

	internal override void WriteNamespaceDeclaration(string prefix, string ns)
	{
		_wrapped.WriteNamespaceDeclaration(prefix, ns);
	}

	internal override void WriteStartNamespaceDeclaration(string prefix)
	{
		_wrapped.WriteStartNamespaceDeclaration(prefix);
	}

	internal override void WriteEndNamespaceDeclaration()
	{
		_wrapped.WriteEndNamespaceDeclaration();
	}

	public override void WriteCData(string text)
	{
		_wrapped.WriteCData(text);
	}

	public override void WriteComment(string text)
	{
		EndCDataSection();
		_wrapped.WriteComment(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		EndCDataSection();
		_wrapped.WriteProcessingInstruction(name, text);
	}

	public override void WriteWhitespace(string ws)
	{
		if (!_inAttr && (_inCDataSection || StartCDataSection()))
		{
			_wrapped.WriteCData(ws);
		}
		else
		{
			_wrapped.WriteWhitespace(ws);
		}
	}

	public override void WriteString(string text)
	{
		if (!_inAttr && (_inCDataSection || StartCDataSection()))
		{
			_wrapped.WriteCData(text);
		}
		else
		{
			_wrapped.WriteString(text);
		}
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		if (!_inAttr && (_inCDataSection || StartCDataSection()))
		{
			_wrapped.WriteCData(new string(buffer, index, count));
		}
		else
		{
			_wrapped.WriteChars(buffer, index, count);
		}
	}

	public override void WriteEntityRef(string name)
	{
		EndCDataSection();
		_wrapped.WriteEntityRef(name);
	}

	public override void WriteCharEntity(char ch)
	{
		EndCDataSection();
		_wrapped.WriteCharEntity(ch);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		EndCDataSection();
		_wrapped.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		if (!_inAttr && (_inCDataSection || StartCDataSection()))
		{
			_wrapped.WriteCData(new string(buffer, index, count));
		}
		else
		{
			_wrapped.WriteRaw(buffer, index, count);
		}
	}

	public override void WriteRaw(string data)
	{
		if (!_inAttr && (_inCDataSection || StartCDataSection()))
		{
			_wrapped.WriteCData(data);
		}
		else
		{
			_wrapped.WriteRaw(data);
		}
	}

	public override void Close()
	{
		_wrapped.Close();
		if (_checkWellFormedDoc && !_hasDocElem)
		{
			throw new XmlException(System.SR.Xml_NoRoot, string.Empty);
		}
	}

	public override void Flush()
	{
		_wrapped.Flush();
	}

	private bool StartCDataSection()
	{
		if (_lookupCDataElems != null && _bitsCData.PeekBit())
		{
			_inCDataSection = true;
			return true;
		}
		return false;
	}

	private void EndCDataSection()
	{
		_inCDataSection = false;
	}
}
