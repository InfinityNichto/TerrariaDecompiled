using System.Collections.Generic;

namespace System.Xml;

internal sealed class QueryOutputWriterV1 : XmlWriter
{
	private readonly XmlWriter _wrapped;

	private bool _inCDataSection;

	private readonly Dictionary<XmlQualifiedName, XmlQualifiedName> _lookupCDataElems;

	private readonly BitStack _bitsCData;

	private readonly XmlQualifiedName _qnameCData;

	private bool _outputDocType;

	private bool _inAttr;

	private readonly string _systemId;

	private readonly string _publicId;

	public override WriteState WriteState => _wrapped.WriteState;

	public QueryOutputWriterV1(XmlWriter writer, XmlWriterSettings settings)
	{
		_wrapped = writer;
		_systemId = settings.DocTypeSystem;
		_publicId = settings.DocTypePublic;
		if (settings.OutputMethod == XmlOutputMethod.Xml)
		{
			bool flag = false;
			if (_systemId != null)
			{
				flag = true;
				_outputDocType = true;
			}
			if (settings.Standalone == XmlStandalone.Yes)
			{
				flag = true;
			}
			if (flag)
			{
				if (settings.Standalone == XmlStandalone.Yes)
				{
					_wrapped.WriteStartDocument(standalone: true);
				}
				else
				{
					_wrapped.WriteStartDocument();
				}
			}
			if (settings.CDataSectionElements == null || settings.CDataSectionElements.Count <= 0)
			{
				return;
			}
			_bitsCData = new BitStack();
			_lookupCDataElems = new Dictionary<XmlQualifiedName, XmlQualifiedName>();
			_qnameCData = new XmlQualifiedName();
			foreach (XmlQualifiedName cDataSectionElement in settings.CDataSectionElements)
			{
				_lookupCDataElems[cDataSectionElement] = null;
			}
			_bitsCData.PushBit(bit: false);
		}
		else if (settings.OutputMethod == XmlOutputMethod.Html && (_systemId != null || _publicId != null))
		{
			_outputDocType = true;
		}
	}

	public override void WriteStartDocument()
	{
		_wrapped.WriteStartDocument();
	}

	public override void WriteStartDocument(bool standalone)
	{
		_wrapped.WriteStartDocument(standalone);
	}

	public override void WriteEndDocument()
	{
		_wrapped.WriteEndDocument();
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
		if (_outputDocType)
		{
			WriteState writeState = _wrapped.WriteState;
			if (writeState == WriteState.Start || writeState == WriteState.Prolog)
			{
				_wrapped.WriteDocType(string.IsNullOrEmpty(prefix) ? localName : (prefix + ":" + localName), _publicId, _systemId, null);
			}
			_outputDocType = false;
		}
		_wrapped.WriteStartElement(prefix, localName, ns);
		if (_lookupCDataElems != null)
		{
			_qnameCData.Init(localName, ns);
			_bitsCData.PushBit(_lookupCDataElems.ContainsKey(_qnameCData));
		}
	}

	public override void WriteEndElement()
	{
		EndCDataSection();
		_wrapped.WriteEndElement();
		if (_lookupCDataElems != null)
		{
			_bitsCData.PopBit();
		}
	}

	public override void WriteFullEndElement()
	{
		EndCDataSection();
		_wrapped.WriteFullEndElement();
		if (_lookupCDataElems != null)
		{
			_bitsCData.PopBit();
		}
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

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		if (!_inAttr && !_inCDataSection)
		{
			StartCDataSection();
		}
		_wrapped.WriteBase64(buffer, index, count);
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
	}

	public override void Flush()
	{
		_wrapped.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		return _wrapped.LookupPrefix(ns);
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
