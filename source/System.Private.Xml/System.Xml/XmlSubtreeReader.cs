using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XmlSubtreeReader : XmlWrappingReader, IXmlLineInfo, IXmlNamespaceResolver
{
	private sealed class NodeData
	{
		internal XmlNodeType type;

		internal string localName;

		internal string prefix;

		internal string name;

		internal string namespaceUri;

		internal string value;

		internal NodeData(XmlNodeType nodeType, string localName, string prefix, string name, string namespaceUri, string value)
		{
			type = nodeType;
			this.localName = localName;
			this.prefix = prefix;
			this.name = name;
			this.namespaceUri = namespaceUri;
			this.value = value;
		}

		internal void Set(XmlNodeType nodeType, string localName, string prefix, string name, string namespaceUri, string value)
		{
			type = nodeType;
			this.localName = localName;
			this.prefix = prefix;
			this.name = name;
			this.namespaceUri = namespaceUri;
			this.value = value;
		}
	}

	private enum State
	{
		Initial,
		Interactive,
		Error,
		EndOfFile,
		Closed,
		PopNamespaceScope,
		ClearNsAttributes,
		ReadElementContentAsBase64,
		ReadElementContentAsBinHex,
		ReadContentAsBase64,
		ReadContentAsBinHex
	}

	private readonly int _initialDepth;

	private State _state;

	private readonly XmlNamespaceManager _nsManager;

	private NodeData[] _nsAttributes;

	private int _nsAttrCount;

	private int _curNsAttr = -1;

	private readonly string _xmlns;

	private readonly string _xmlnsUri;

	private int _nsIncReadOffset;

	private IncrementalReadDecoder _binDecoder;

	private bool _useCurNode;

	private NodeData _curNode;

	private readonly NodeData _tmpNode;

	internal int InitialNamespaceAttributeCount = 4;

	public override XmlNodeType NodeType
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.NodeType;
			}
			return _curNode.type;
		}
	}

	public override string Name
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.Name;
			}
			return _curNode.name;
		}
	}

	public override string LocalName
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.LocalName;
			}
			return _curNode.localName;
		}
	}

	public override string NamespaceURI
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.NamespaceURI;
			}
			return _curNode.namespaceUri;
		}
	}

	public override string Prefix
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.Prefix;
			}
			return _curNode.prefix;
		}
	}

	public override string Value
	{
		get
		{
			if (!_useCurNode)
			{
				return reader.Value;
			}
			return _curNode.value;
		}
	}

	public override int Depth
	{
		get
		{
			int num = reader.Depth - _initialDepth;
			if (_curNsAttr != -1)
			{
				num = ((_curNode.type != XmlNodeType.Text) ? (num + 1) : (num + 2));
			}
			return num;
		}
	}

	public override string BaseURI => reader.BaseURI;

	public override bool IsEmptyElement => reader.IsEmptyElement;

	public override bool EOF
	{
		get
		{
			if (_state != State.EndOfFile)
			{
				return _state == State.Closed;
			}
			return true;
		}
	}

	public override ReadState ReadState
	{
		get
		{
			if (reader.ReadState == ReadState.Error)
			{
				return ReadState.Error;
			}
			if (_state <= State.Closed)
			{
				return (ReadState)_state;
			}
			return ReadState.Interactive;
		}
	}

	public override XmlNameTable NameTable => reader.NameTable;

	public override int AttributeCount
	{
		get
		{
			if (!InAttributeActiveState)
			{
				return 0;
			}
			return reader.AttributeCount + _nsAttrCount;
		}
	}

	public override bool CanReadBinaryContent => reader.CanReadBinaryContent;

	public override bool CanReadValueChunk => reader.CanReadValueChunk;

	int IXmlLineInfo.LineNumber
	{
		get
		{
			if (!_useCurNode && reader is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LineNumber;
			}
			return 0;
		}
	}

	int IXmlLineInfo.LinePosition
	{
		get
		{
			if (!_useCurNode && reader is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LinePosition;
			}
			return 0;
		}
	}

	private bool InAttributeActiveState => (0x62 & (1 << (int)_state)) != 0;

	private bool InNamespaceActiveState => (0x7E2 & (1 << (int)_state)) != 0;

	internal XmlSubtreeReader(XmlReader reader)
		: base(reader)
	{
		_initialDepth = reader.Depth;
		_state = State.Initial;
		_nsManager = new XmlNamespaceManager(reader.NameTable);
		_xmlns = reader.NameTable.Add("xmlns");
		_xmlnsUri = reader.NameTable.Add("http://www.w3.org/2000/xmlns/");
		_tmpNode = new NodeData(XmlNodeType.None, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
		SetCurrentNode(_tmpNode);
	}

	public override string GetAttribute(string name)
	{
		if (!InAttributeActiveState)
		{
			return null;
		}
		string attribute = reader.GetAttribute(name);
		if (attribute != null)
		{
			return attribute;
		}
		for (int i = 0; i < _nsAttrCount; i++)
		{
			if (name == _nsAttributes[i].name)
			{
				return _nsAttributes[i].value;
			}
		}
		return null;
	}

	public override string GetAttribute(string name, string namespaceURI)
	{
		if (!InAttributeActiveState)
		{
			return null;
		}
		string attribute = reader.GetAttribute(name, namespaceURI);
		if (attribute != null)
		{
			return attribute;
		}
		for (int i = 0; i < _nsAttrCount; i++)
		{
			if (name == _nsAttributes[i].localName && namespaceURI == _xmlnsUri)
			{
				return _nsAttributes[i].value;
			}
		}
		return null;
	}

	public override string GetAttribute(int i)
	{
		if (!InAttributeActiveState)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		int attributeCount = reader.AttributeCount;
		if (i < attributeCount)
		{
			return reader.GetAttribute(i);
		}
		if (i - attributeCount < _nsAttrCount)
		{
			return _nsAttributes[i - attributeCount].value;
		}
		throw new ArgumentOutOfRangeException("i");
	}

	public override bool MoveToAttribute(string name)
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		if (reader.MoveToAttribute(name))
		{
			_curNsAttr = -1;
			_useCurNode = false;
			return true;
		}
		for (int i = 0; i < _nsAttrCount; i++)
		{
			if (name == _nsAttributes[i].name)
			{
				MoveToNsAttribute(i);
				return true;
			}
		}
		return false;
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		if (reader.MoveToAttribute(name, ns))
		{
			_curNsAttr = -1;
			_useCurNode = false;
			return true;
		}
		for (int i = 0; i < _nsAttrCount; i++)
		{
			if (name == _nsAttributes[i].localName && ns == _xmlnsUri)
			{
				MoveToNsAttribute(i);
				return true;
			}
		}
		return false;
	}

	public override void MoveToAttribute(int i)
	{
		if (!InAttributeActiveState)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		int attributeCount = reader.AttributeCount;
		if (i < attributeCount)
		{
			reader.MoveToAttribute(i);
			_curNsAttr = -1;
			_useCurNode = false;
			return;
		}
		if (i - attributeCount < _nsAttrCount)
		{
			MoveToNsAttribute(i - attributeCount);
			return;
		}
		throw new ArgumentOutOfRangeException("i");
	}

	public override bool MoveToFirstAttribute()
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		if (reader.MoveToFirstAttribute())
		{
			_useCurNode = false;
			return true;
		}
		if (_nsAttrCount > 0)
		{
			MoveToNsAttribute(0);
			return true;
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		if (_curNsAttr == -1 && reader.MoveToNextAttribute())
		{
			return true;
		}
		if (_curNsAttr + 1 < _nsAttrCount)
		{
			MoveToNsAttribute(_curNsAttr + 1);
			return true;
		}
		return false;
	}

	public override bool MoveToElement()
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		_useCurNode = false;
		if (_curNsAttr >= 0)
		{
			_curNsAttr = -1;
			return true;
		}
		return reader.MoveToElement();
	}

	public override bool ReadAttributeValue()
	{
		if (!InAttributeActiveState)
		{
			return false;
		}
		if (_curNsAttr == -1)
		{
			return reader.ReadAttributeValue();
		}
		if (_curNode.type == XmlNodeType.Text)
		{
			return false;
		}
		_tmpNode.type = XmlNodeType.Text;
		_tmpNode.value = _curNode.value;
		SetCurrentNode(_tmpNode);
		return true;
	}

	public override bool Read()
	{
		switch (_state)
		{
		case State.Initial:
			_useCurNode = false;
			_state = State.Interactive;
			ProcessNamespaces();
			return true;
		case State.Interactive:
			_curNsAttr = -1;
			_useCurNode = false;
			reader.MoveToElement();
			if (reader.Depth == _initialDepth && (reader.NodeType == XmlNodeType.EndElement || (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement)))
			{
				_state = State.EndOfFile;
				SetEmptyNode();
				return false;
			}
			if (reader.Read())
			{
				ProcessNamespaces();
				return true;
			}
			SetEmptyNode();
			return false;
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return false;
		case State.PopNamespaceScope:
			_nsManager.PopScope();
			goto case State.ClearNsAttributes;
		case State.ClearNsAttributes:
			_nsAttrCount = 0;
			_state = State.Interactive;
			goto case State.Interactive;
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
			if (!FinishReadElementContentAsBinary())
			{
				return false;
			}
			return Read();
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			if (!FinishReadContentAsBinary())
			{
				return false;
			}
			return Read();
		default:
			return false;
		}
	}

	public override void Close()
	{
		if (_state == State.Closed)
		{
			return;
		}
		try
		{
			if (_state != State.EndOfFile)
			{
				reader.MoveToElement();
				if (reader.Depth == _initialDepth && reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
				{
					reader.Read();
				}
				while (reader.Depth > _initialDepth && reader.Read())
				{
				}
			}
		}
		catch
		{
		}
		finally
		{
			_curNsAttr = -1;
			_useCurNode = false;
			_state = State.Closed;
			SetEmptyNode();
		}
	}

	public override void Skip()
	{
		switch (_state)
		{
		case State.Initial:
			Read();
			break;
		case State.Interactive:
			_curNsAttr = -1;
			_useCurNode = false;
			reader.MoveToElement();
			if (reader.Depth == _initialDepth)
			{
				if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement && reader.Read())
				{
					while (reader.NodeType != XmlNodeType.EndElement && reader.Depth > _initialDepth)
					{
						reader.Skip();
					}
				}
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			else
			{
				if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
				{
					_nsManager.PopScope();
				}
				reader.Skip();
				ProcessNamespaces();
			}
			break;
		case State.EndOfFile:
		case State.Closed:
			break;
		case State.PopNamespaceScope:
			_nsManager.PopScope();
			goto case State.ClearNsAttributes;
		case State.ClearNsAttributes:
			_nsAttrCount = 0;
			_state = State.Interactive;
			goto case State.Interactive;
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
			if (FinishReadElementContentAsBinary())
			{
				Skip();
			}
			break;
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			if (FinishReadContentAsBinary())
			{
				Skip();
			}
			break;
		case State.Error:
			break;
		}
	}

	public override object ReadContentAsObject()
	{
		try
		{
			InitReadContentAsType("ReadContentAsObject");
			object result = reader.ReadContentAsObject();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override bool ReadContentAsBoolean()
	{
		try
		{
			InitReadContentAsType("ReadContentAsBoolean");
			bool result = reader.ReadContentAsBoolean();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override DateTime ReadContentAsDateTime()
	{
		try
		{
			InitReadContentAsType("ReadContentAsDateTime");
			DateTime result = reader.ReadContentAsDateTime();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override double ReadContentAsDouble()
	{
		try
		{
			InitReadContentAsType("ReadContentAsDouble");
			double result = reader.ReadContentAsDouble();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override float ReadContentAsFloat()
	{
		try
		{
			InitReadContentAsType("ReadContentAsFloat");
			float result = reader.ReadContentAsFloat();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override decimal ReadContentAsDecimal()
	{
		try
		{
			InitReadContentAsType("ReadContentAsDecimal");
			decimal result = reader.ReadContentAsDecimal();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override int ReadContentAsInt()
	{
		try
		{
			InitReadContentAsType("ReadContentAsInt");
			int result = reader.ReadContentAsInt();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override long ReadContentAsLong()
	{
		try
		{
			InitReadContentAsType("ReadContentAsLong");
			long result = reader.ReadContentAsLong();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override string ReadContentAsString()
	{
		try
		{
			InitReadContentAsType("ReadContentAsString");
			string result = reader.ReadContentAsString();
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		try
		{
			InitReadContentAsType("ReadContentAs");
			object result = reader.ReadContentAs(returnType, namespaceResolver);
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			switch (NodeType)
			{
			case XmlNodeType.Element:
				throw CreateReadContentAsException("ReadContentAsBase64");
			case XmlNodeType.EndElement:
				return 0;
			case XmlNodeType.Attribute:
				if (_curNsAttr != -1 && reader.CanReadBinaryContent)
				{
					CheckBuffer(buffer, index, count);
					if (count == 0)
					{
						return 0;
					}
					if (_nsIncReadOffset == 0)
					{
						if (_binDecoder != null && _binDecoder is Base64Decoder)
						{
							_binDecoder.Reset();
						}
						else
						{
							_binDecoder = new Base64Decoder();
						}
					}
					if (_nsIncReadOffset == _curNode.value.Length)
					{
						return 0;
					}
					_binDecoder.SetNextOutputBuffer(buffer, index, count);
					_nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
					return _binDecoder.DecodedCount;
				}
				goto case XmlNodeType.Text;
			case XmlNodeType.Text:
				return reader.ReadContentAsBase64(buffer, index, count);
			default:
				return 0;
			}
		case State.Interactive:
			_state = State.ReadContentAsBase64;
			goto case State.ReadContentAsBase64;
		case State.ReadContentAsBase64:
		{
			int num = reader.ReadContentAsBase64(buffer, index, count);
			if (num == 0)
			{
				_state = State.Interactive;
				ProcessNamespaces();
			}
			return num;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.Interactive:
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (!InitReadElementContentAsBinary(State.ReadElementContentAsBase64))
			{
				return 0;
			}
			goto case State.ReadElementContentAsBase64;
		case State.ReadElementContentAsBase64:
		{
			int num = reader.ReadContentAsBase64(buffer, index, count);
			if (num > 0 || count == 0)
			{
				return num;
			}
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
			}
			_state = State.Interactive;
			ProcessNamespaces();
			if (reader.Depth == _initialDepth)
			{
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			else
			{
				Read();
			}
			return 0;
		}
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			switch (NodeType)
			{
			case XmlNodeType.Element:
				throw CreateReadContentAsException("ReadContentAsBinHex");
			case XmlNodeType.EndElement:
				return 0;
			case XmlNodeType.Attribute:
				if (_curNsAttr != -1 && reader.CanReadBinaryContent)
				{
					CheckBuffer(buffer, index, count);
					if (count == 0)
					{
						return 0;
					}
					if (_nsIncReadOffset == 0)
					{
						if (_binDecoder != null && _binDecoder is BinHexDecoder)
						{
							_binDecoder.Reset();
						}
						else
						{
							_binDecoder = new BinHexDecoder();
						}
					}
					if (_nsIncReadOffset == _curNode.value.Length)
					{
						return 0;
					}
					_binDecoder.SetNextOutputBuffer(buffer, index, count);
					_nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
					return _binDecoder.DecodedCount;
				}
				goto case XmlNodeType.Text;
			case XmlNodeType.Text:
				return reader.ReadContentAsBinHex(buffer, index, count);
			default:
				return 0;
			}
		case State.Interactive:
			_state = State.ReadContentAsBinHex;
			goto case State.ReadContentAsBinHex;
		case State.ReadContentAsBinHex:
		{
			int num = reader.ReadContentAsBinHex(buffer, index, count);
			if (num == 0)
			{
				_state = State.Interactive;
				ProcessNamespaces();
			}
			return num;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.Interactive:
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (!InitReadElementContentAsBinary(State.ReadElementContentAsBinHex))
			{
				return 0;
			}
			goto case State.ReadElementContentAsBinHex;
		case State.ReadElementContentAsBinHex:
		{
			int num = reader.ReadContentAsBinHex(buffer, index, count);
			if (num > 0 || count == 0)
			{
				return num;
			}
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
			}
			_state = State.Interactive;
			ProcessNamespaces();
			if (reader.Depth == _initialDepth)
			{
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			else
			{
				Read();
			}
			return 0;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override int ReadValueChunk(char[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (_curNsAttr != -1 && reader.CanReadValueChunk)
			{
				CheckBuffer(buffer, index, count);
				int num = _curNode.value.Length - _nsIncReadOffset;
				if (num > count)
				{
					num = count;
				}
				if (num > 0)
				{
					_curNode.value.CopyTo(_nsIncReadOffset, buffer, index, num);
				}
				_nsIncReadOffset += num;
				return num;
			}
			goto case State.Interactive;
		case State.Interactive:
			return reader.ReadValueChunk(buffer, index, count);
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingReadValueChunkWithBinary);
		default:
			return 0;
		}
	}

	public override string LookupNamespace(string prefix)
	{
		return ((IXmlNamespaceResolver)this).LookupNamespace(prefix);
	}

	protected override void Dispose(bool disposing)
	{
		Close();
	}

	bool IXmlLineInfo.HasLineInfo()
	{
		return reader is IXmlLineInfo;
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		if (!InNamespaceActiveState)
		{
			return new Dictionary<string, string>();
		}
		return _nsManager.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		if (!InNamespaceActiveState)
		{
			return null;
		}
		return _nsManager.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		if (!InNamespaceActiveState)
		{
			return null;
		}
		return _nsManager.LookupPrefix(namespaceName);
	}

	private void ProcessNamespaces()
	{
		switch (reader.NodeType)
		{
		case XmlNodeType.Element:
		{
			_nsManager.PushScope();
			string prefix = reader.Prefix;
			string namespaceURI = reader.NamespaceURI;
			if (_nsManager.LookupNamespace(prefix) != namespaceURI)
			{
				AddNamespace(prefix, namespaceURI);
			}
			if (reader.MoveToFirstAttribute())
			{
				do
				{
					prefix = reader.Prefix;
					namespaceURI = reader.NamespaceURI;
					if (Ref.Equal(namespaceURI, _xmlnsUri))
					{
						if (prefix.Length == 0)
						{
							_nsManager.AddNamespace(string.Empty, reader.Value);
							RemoveNamespace(string.Empty, _xmlns);
						}
						else
						{
							prefix = reader.LocalName;
							_nsManager.AddNamespace(prefix, reader.Value);
							RemoveNamespace(_xmlns, prefix);
						}
					}
					else if (prefix.Length != 0 && _nsManager.LookupNamespace(prefix) != namespaceURI)
					{
						AddNamespace(prefix, namespaceURI);
					}
				}
				while (reader.MoveToNextAttribute());
				reader.MoveToElement();
			}
			if (reader.IsEmptyElement)
			{
				_state = State.PopNamespaceScope;
			}
			break;
		}
		case XmlNodeType.EndElement:
			_state = State.PopNamespaceScope;
			break;
		}
	}

	private void AddNamespace(string prefix, string ns)
	{
		_nsManager.AddNamespace(prefix, ns);
		int num = _nsAttrCount++;
		if (_nsAttributes == null)
		{
			_nsAttributes = new NodeData[InitialNamespaceAttributeCount];
		}
		if (num == _nsAttributes.Length)
		{
			NodeData[] array = new NodeData[_nsAttributes.Length * 2];
			Array.Copy(_nsAttributes, array, num);
			_nsAttributes = array;
		}
		string localName;
		string prefix2;
		string name;
		if (prefix.Length == 0)
		{
			localName = _xmlns;
			prefix2 = string.Empty;
			name = _xmlns;
		}
		else
		{
			localName = prefix;
			prefix2 = _xmlns;
			name = reader.NameTable.Add(_xmlns + ":" + prefix);
		}
		if (_nsAttributes[num] == null)
		{
			_nsAttributes[num] = new NodeData(XmlNodeType.Attribute, localName, prefix2, name, _xmlnsUri, ns);
		}
		else
		{
			_nsAttributes[num].Set(XmlNodeType.Attribute, localName, prefix2, name, _xmlnsUri, ns);
		}
		_state = State.ClearNsAttributes;
		_curNsAttr = -1;
	}

	private void RemoveNamespace(string prefix, string localName)
	{
		for (int i = 0; i < _nsAttrCount; i++)
		{
			if (Ref.Equal(prefix, _nsAttributes[i].prefix) && Ref.Equal(localName, _nsAttributes[i].localName))
			{
				if (i < _nsAttrCount - 1)
				{
					NodeData nodeData = _nsAttributes[i];
					_nsAttributes[i] = _nsAttributes[_nsAttrCount - 1];
					_nsAttributes[_nsAttrCount - 1] = nodeData;
				}
				_nsAttrCount--;
				break;
			}
		}
	}

	private void MoveToNsAttribute(int index)
	{
		reader.MoveToElement();
		_curNsAttr = index;
		_nsIncReadOffset = 0;
		SetCurrentNode(_nsAttributes[index]);
	}

	private bool InitReadElementContentAsBinary(State binaryState)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw reader.CreateReadElementContentAsException("ReadElementContentAsBase64");
		}
		bool isEmptyElement = IsEmptyElement;
		if (!Read() || isEmptyElement)
		{
			return false;
		}
		switch (NodeType)
		{
		case XmlNodeType.Element:
			throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
		case XmlNodeType.EndElement:
			ProcessNamespaces();
			Read();
			return false;
		default:
			_state = binaryState;
			return true;
		}
	}

	private bool FinishReadElementContentAsBinary()
	{
		byte[] buffer = new byte[256];
		if (_state == State.ReadElementContentAsBase64)
		{
			while (reader.ReadContentAsBase64(buffer, 0, 256) > 0)
			{
			}
		}
		else
		{
			while (reader.ReadContentAsBinHex(buffer, 0, 256) > 0)
			{
			}
		}
		if (NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
		}
		_state = State.Interactive;
		ProcessNamespaces();
		if (reader.Depth == _initialDepth)
		{
			_state = State.EndOfFile;
			SetEmptyNode();
			return false;
		}
		return Read();
	}

	private bool FinishReadContentAsBinary()
	{
		byte[] buffer = new byte[256];
		if (_state == State.ReadContentAsBase64)
		{
			while (reader.ReadContentAsBase64(buffer, 0, 256) > 0)
			{
			}
		}
		else
		{
			while (reader.ReadContentAsBinHex(buffer, 0, 256) > 0)
			{
			}
		}
		_state = State.Interactive;
		ProcessNamespaces();
		if (reader.Depth == _initialDepth)
		{
			_state = State.EndOfFile;
			SetEmptyNode();
			return false;
		}
		return true;
	}

	private void SetEmptyNode()
	{
		_tmpNode.type = XmlNodeType.None;
		_tmpNode.value = string.Empty;
		_curNode = _tmpNode;
		_useCurNode = true;
	}

	private void SetCurrentNode(NodeData node)
	{
		_curNode = node;
		_useCurNode = true;
	}

	private void InitReadContentAsType(string methodName)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			throw new InvalidOperationException(System.SR.Xml_ClosedOrErrorReader);
		case State.Interactive:
			break;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			break;
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingReadValueChunkWithBinary);
		default:
			throw CreateReadContentAsException(methodName);
		}
	}

	private void FinishReadContentAsType()
	{
		switch (NodeType)
		{
		case XmlNodeType.Element:
			ProcessNamespaces();
			break;
		case XmlNodeType.EndElement:
			_state = State.PopNamespaceScope;
			break;
		}
	}

	private void CheckBuffer(Array buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
	}

	public override Task<string> GetValueAsync()
	{
		if (_useCurNode)
		{
			return Task.FromResult(_curNode.value);
		}
		return reader.GetValueAsync();
	}

	public override async Task<bool> ReadAsync()
	{
		switch (_state)
		{
		case State.Initial:
			_useCurNode = false;
			_state = State.Interactive;
			ProcessNamespaces();
			return true;
		case State.Interactive:
			_curNsAttr = -1;
			_useCurNode = false;
			reader.MoveToElement();
			if (reader.Depth == _initialDepth && (reader.NodeType == XmlNodeType.EndElement || (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement)))
			{
				_state = State.EndOfFile;
				SetEmptyNode();
				return false;
			}
			if (await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				ProcessNamespaces();
				return true;
			}
			SetEmptyNode();
			return false;
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return false;
		case State.PopNamespaceScope:
			_nsManager.PopScope();
			goto case State.ClearNsAttributes;
		case State.ClearNsAttributes:
			_nsAttrCount = 0;
			_state = State.Interactive;
			goto case State.Interactive;
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
			if (!(await FinishReadElementContentAsBinaryAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return false;
			}
			return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			if (!(await FinishReadContentAsBinaryAsync().ConfigureAwait(continueOnCapturedContext: false)))
			{
				return false;
			}
			return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		default:
			return false;
		}
	}

	public override async Task SkipAsync()
	{
		switch (_state)
		{
		case State.Initial:
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			break;
		case State.Interactive:
			_curNsAttr = -1;
			_useCurNode = false;
			reader.MoveToElement();
			if (reader.Depth == _initialDepth)
			{
				if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement && await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
				{
					while (reader.NodeType != XmlNodeType.EndElement && reader.Depth > _initialDepth)
					{
						await reader.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
					}
				}
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			else
			{
				if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
				{
					_nsManager.PopScope();
				}
				await reader.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
				ProcessNamespaces();
			}
			break;
		case State.EndOfFile:
		case State.Closed:
			break;
		case State.PopNamespaceScope:
			_nsManager.PopScope();
			goto case State.ClearNsAttributes;
		case State.ClearNsAttributes:
			_nsAttrCount = 0;
			_state = State.Interactive;
			goto case State.Interactive;
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
			if (await FinishReadElementContentAsBinaryAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				await SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			if (await FinishReadContentAsBinaryAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				await SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		case State.Error:
			break;
		}
	}

	public override async Task<object> ReadContentAsObjectAsync()
	{
		try
		{
			InitReadContentAsType("ReadContentAsObject");
			object result = await reader.ReadContentAsObjectAsync().ConfigureAwait(continueOnCapturedContext: false);
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override async Task<string> ReadContentAsStringAsync()
	{
		try
		{
			InitReadContentAsType("ReadContentAsString");
			string result = await reader.ReadContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		try
		{
			InitReadContentAsType("ReadContentAs");
			object result = await reader.ReadContentAsAsync(returnType, namespaceResolver).ConfigureAwait(continueOnCapturedContext: false);
			FinishReadContentAsType();
			return result;
		}
		catch
		{
			_state = State.Error;
			throw;
		}
	}

	public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			switch (NodeType)
			{
			case XmlNodeType.Element:
				throw CreateReadContentAsException("ReadContentAsBase64");
			case XmlNodeType.EndElement:
				return 0;
			case XmlNodeType.Attribute:
				if (_curNsAttr != -1 && reader.CanReadBinaryContent)
				{
					CheckBuffer(buffer, index, count);
					if (count == 0)
					{
						return 0;
					}
					if (_nsIncReadOffset == 0)
					{
						if (_binDecoder != null && _binDecoder is Base64Decoder)
						{
							_binDecoder.Reset();
						}
						else
						{
							_binDecoder = new Base64Decoder();
						}
					}
					if (_nsIncReadOffset == _curNode.value.Length)
					{
						return 0;
					}
					_binDecoder.SetNextOutputBuffer(buffer, index, count);
					_nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
					return _binDecoder.DecodedCount;
				}
				goto case XmlNodeType.Text;
			case XmlNodeType.Text:
				return await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			default:
				return 0;
			}
		case State.Interactive:
			_state = State.ReadContentAsBase64;
			goto case State.ReadContentAsBase64;
		case State.ReadContentAsBase64:
		{
			int num = await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				_state = State.Interactive;
				ProcessNamespaces();
			}
			return num;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.Interactive:
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (!(await InitReadElementContentAsBinaryAsync(State.ReadElementContentAsBase64).ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			goto case State.ReadElementContentAsBase64;
		case State.ReadElementContentAsBase64:
		{
			int num = await reader.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			if (num > 0 || count == 0)
			{
				return num;
			}
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
			}
			_state = State.Interactive;
			ProcessNamespaces();
			if (reader.Depth != _initialDepth)
			{
				await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			return 0;
		}
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			switch (NodeType)
			{
			case XmlNodeType.Element:
				throw CreateReadContentAsException("ReadContentAsBinHex");
			case XmlNodeType.EndElement:
				return 0;
			case XmlNodeType.Attribute:
				if (_curNsAttr != -1 && reader.CanReadBinaryContent)
				{
					CheckBuffer(buffer, index, count);
					if (count == 0)
					{
						return 0;
					}
					if (_nsIncReadOffset == 0)
					{
						if (_binDecoder != null && _binDecoder is BinHexDecoder)
						{
							_binDecoder.Reset();
						}
						else
						{
							_binDecoder = new BinHexDecoder();
						}
					}
					if (_nsIncReadOffset == _curNode.value.Length)
					{
						return 0;
					}
					_binDecoder.SetNextOutputBuffer(buffer, index, count);
					_nsIncReadOffset += _binDecoder.Decode(_curNode.value, _nsIncReadOffset, _curNode.value.Length - _nsIncReadOffset);
					return _binDecoder.DecodedCount;
				}
				goto case XmlNodeType.Text;
			case XmlNodeType.Text:
				return await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			default:
				return 0;
			}
		case State.Interactive:
			_state = State.ReadContentAsBinHex;
			goto case State.ReadContentAsBinHex;
		case State.ReadContentAsBinHex:
		{
			int num = await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			if (num == 0)
			{
				_state = State.Interactive;
				ProcessNamespaces();
			}
			return num;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return 0;
		case State.Interactive:
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (!(await InitReadElementContentAsBinaryAsync(State.ReadElementContentAsBinHex).ConfigureAwait(continueOnCapturedContext: false)))
			{
				return 0;
			}
			goto case State.ReadElementContentAsBinHex;
		case State.ReadElementContentAsBinHex:
		{
			int num = await reader.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
			if (num > 0 || count == 0)
			{
				return num;
			}
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
			}
			_state = State.Interactive;
			ProcessNamespaces();
			if (reader.Depth != _initialDepth)
			{
				await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				_state = State.EndOfFile;
				SetEmptyNode();
			}
			return 0;
		}
		case State.ReadElementContentAsBase64:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingBinaryContentMethods);
		default:
			return 0;
		}
	}

	public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
	{
		switch (_state)
		{
		case State.Initial:
		case State.Error:
		case State.EndOfFile:
		case State.Closed:
			return AsyncHelper.DoneTaskZero;
		case State.PopNamespaceScope:
		case State.ClearNsAttributes:
			if (_curNsAttr != -1 && reader.CanReadValueChunk)
			{
				CheckBuffer(buffer, index, count);
				int num = _curNode.value.Length - _nsIncReadOffset;
				if (num > count)
				{
					num = count;
				}
				if (num > 0)
				{
					_curNode.value.CopyTo(_nsIncReadOffset, buffer, index, num);
				}
				_nsIncReadOffset += num;
				return Task.FromResult(num);
			}
			goto case State.Interactive;
		case State.Interactive:
			return reader.ReadValueChunkAsync(buffer, index, count);
		case State.ReadElementContentAsBase64:
		case State.ReadElementContentAsBinHex:
		case State.ReadContentAsBase64:
		case State.ReadContentAsBinHex:
			throw new InvalidOperationException(System.SR.Xml_MixingReadValueChunkWithBinary);
		default:
			return AsyncHelper.DoneTaskZero;
		}
	}

	private async Task<bool> InitReadElementContentAsBinaryAsync(State binaryState)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw reader.CreateReadElementContentAsException("ReadElementContentAsBase64");
		}
		bool isEmpty = IsEmptyElement;
		if (!(await ReadAsync().ConfigureAwait(continueOnCapturedContext: false)) || isEmpty)
		{
			return false;
		}
		switch (NodeType)
		{
		case XmlNodeType.Element:
			throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
		case XmlNodeType.EndElement:
			ProcessNamespaces();
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return false;
		default:
			_state = binaryState;
			return true;
		}
	}

	private async Task<bool> FinishReadElementContentAsBinaryAsync()
	{
		byte[] bytes = new byte[256];
		if (_state == State.ReadElementContentAsBase64)
		{
			while (await reader.ReadContentAsBase64Async(bytes, 0, 256).ConfigureAwait(continueOnCapturedContext: false) > 0)
			{
			}
		}
		else
		{
			while (await reader.ReadContentAsBinHexAsync(bytes, 0, 256).ConfigureAwait(continueOnCapturedContext: false) > 0)
			{
			}
		}
		if (NodeType != XmlNodeType.EndElement)
		{
			throw new XmlException(System.SR.Xml_InvalidNodeType, reader.NodeType.ToString(), reader as IXmlLineInfo);
		}
		_state = State.Interactive;
		ProcessNamespaces();
		if (reader.Depth == _initialDepth)
		{
			_state = State.EndOfFile;
			SetEmptyNode();
			return false;
		}
		return await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<bool> FinishReadContentAsBinaryAsync()
	{
		byte[] bytes = new byte[256];
		if (_state == State.ReadContentAsBase64)
		{
			while (await reader.ReadContentAsBase64Async(bytes, 0, 256).ConfigureAwait(continueOnCapturedContext: false) > 0)
			{
			}
		}
		else
		{
			while (await reader.ReadContentAsBinHexAsync(bytes, 0, 256).ConfigureAwait(continueOnCapturedContext: false) > 0)
			{
			}
		}
		_state = State.Interactive;
		ProcessNamespaces();
		if (reader.Depth == _initialDepth)
		{
			_state = State.EndOfFile;
			SetEmptyNode();
			return false;
		}
		return true;
	}
}
