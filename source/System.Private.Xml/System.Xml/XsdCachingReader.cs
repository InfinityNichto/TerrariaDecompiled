using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XsdCachingReader : XmlReader, IXmlLineInfo
{
	private enum CachingReaderState
	{
		None,
		Init,
		Record,
		Replay,
		ReaderClosed,
		Error
	}

	private XmlReader _coreReader;

	private XmlNameTable _coreReaderNameTable;

	private ValidatingReaderNodeData[] _contentEvents;

	private ValidatingReaderNodeData[] _attributeEvents;

	private ValidatingReaderNodeData _cachedNode;

	private CachingReaderState _cacheState;

	private int _contentIndex;

	private int _attributeCount;

	private bool _returnOriginalStringValues;

	private readonly CachingEventHandler _cacheHandler;

	private int _currentAttrIndex;

	private int _currentContentIndex;

	private bool _readAhead;

	private readonly IXmlLineInfo _lineInfo;

	private ValidatingReaderNodeData _textNode;

	public override XmlReaderSettings Settings => _coreReader.Settings;

	public override XmlNodeType NodeType => _cachedNode.NodeType;

	public override string Name => _cachedNode.GetAtomizedNameWPrefix(_coreReaderNameTable);

	public override string LocalName => _cachedNode.LocalName;

	public override string NamespaceURI => _cachedNode.Namespace;

	public override string Prefix => _cachedNode.Prefix;

	public override bool HasValue => XmlReader.HasValueInternal(_cachedNode.NodeType);

	public override string Value
	{
		get
		{
			if (!_returnOriginalStringValues)
			{
				return _cachedNode.RawValue;
			}
			return _cachedNode.OriginalStringValue;
		}
	}

	public override int Depth => _cachedNode.Depth;

	public override string BaseURI => _coreReader.BaseURI;

	public override bool IsEmptyElement => false;

	public override bool IsDefault => false;

	public override char QuoteChar => _coreReader.QuoteChar;

	public override XmlSpace XmlSpace => _coreReader.XmlSpace;

	public override string XmlLang => _coreReader.XmlLang;

	public override int AttributeCount => _attributeCount;

	public override string this[int i] => GetAttribute(i);

	public override string this[string name, string namespaceURI] => GetAttribute(name, namespaceURI);

	public override bool EOF
	{
		get
		{
			if (_cacheState == CachingReaderState.ReaderClosed)
			{
				return _coreReader.EOF;
			}
			return false;
		}
	}

	public override ReadState ReadState => _coreReader.ReadState;

	public override XmlNameTable NameTable => _coreReaderNameTable;

	int IXmlLineInfo.LineNumber => _cachedNode.LineNumber;

	int IXmlLineInfo.LinePosition => _cachedNode.LinePosition;

	internal XsdCachingReader(XmlReader reader, IXmlLineInfo lineInfo, CachingEventHandler handlerMethod)
	{
		_coreReader = reader;
		_lineInfo = lineInfo;
		_cacheHandler = handlerMethod;
		_attributeEvents = new ValidatingReaderNodeData[8];
		_contentEvents = new ValidatingReaderNodeData[4];
		Init();
	}

	[MemberNotNull("_coreReaderNameTable")]
	private void Init()
	{
		_coreReaderNameTable = _coreReader.NameTable;
		_cacheState = CachingReaderState.Init;
		_contentIndex = 0;
		_currentAttrIndex = -1;
		_currentContentIndex = -1;
		_attributeCount = 0;
		_cachedNode = null;
		_readAhead = false;
		if (_coreReader.NodeType == XmlNodeType.Element)
		{
			ValidatingReaderNodeData validatingReaderNodeData = AddContent(_coreReader.NodeType);
			validatingReaderNodeData.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
			validatingReaderNodeData.SetLineInfo(_lineInfo);
			RecordAttributes();
		}
	}

	internal void Reset(XmlReader reader)
	{
		_coreReader = reader;
		Init();
	}

	public override string GetAttribute(string name)
	{
		int num = (name.Contains(':') ? GetAttributeIndexWithPrefix(name) : GetAttributeIndexWithoutPrefix(name));
		if (num < 0)
		{
			return null;
		}
		return _attributeEvents[num].RawValue;
	}

	public override string GetAttribute(string name, string namespaceURI)
	{
		namespaceURI = ((namespaceURI == null) ? string.Empty : _coreReaderNameTable.Get(namespaceURI));
		string strB = _coreReaderNameTable.Get(name);
		for (int i = 0; i < _attributeCount; i++)
		{
			ValidatingReaderNodeData validatingReaderNodeData = _attributeEvents[i];
			if (Ref.Equal(validatingReaderNodeData.LocalName, strB) && Ref.Equal(validatingReaderNodeData.Namespace, namespaceURI))
			{
				return validatingReaderNodeData.RawValue;
			}
		}
		return null;
	}

	public override string GetAttribute(int i)
	{
		if (i < 0 || i >= _attributeCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		return _attributeEvents[i].RawValue;
	}

	public override bool MoveToAttribute(string name)
	{
		int num = (name.Contains(':') ? GetAttributeIndexWithPrefix(name) : GetAttributeIndexWithoutPrefix(name));
		if (num >= 0)
		{
			_currentAttrIndex = num;
			_cachedNode = _attributeEvents[num];
			return true;
		}
		return false;
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		ns = ((ns == null) ? string.Empty : _coreReaderNameTable.Get(ns));
		string strB = _coreReaderNameTable.Get(name);
		for (int i = 0; i < _attributeCount; i++)
		{
			ValidatingReaderNodeData validatingReaderNodeData = _attributeEvents[i];
			if (Ref.Equal(validatingReaderNodeData.LocalName, strB) && Ref.Equal(validatingReaderNodeData.Namespace, ns))
			{
				_currentAttrIndex = i;
				_cachedNode = _attributeEvents[i];
				return true;
			}
		}
		return false;
	}

	public override void MoveToAttribute(int i)
	{
		if (i < 0 || i >= _attributeCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		_currentAttrIndex = i;
		_cachedNode = _attributeEvents[i];
	}

	public override bool MoveToFirstAttribute()
	{
		if (_attributeCount == 0)
		{
			return false;
		}
		_currentAttrIndex = 0;
		_cachedNode = _attributeEvents[0];
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (_currentAttrIndex + 1 < _attributeCount)
		{
			_cachedNode = _attributeEvents[++_currentAttrIndex];
			return true;
		}
		return false;
	}

	public override bool MoveToElement()
	{
		if (_cacheState != CachingReaderState.Replay || _cachedNode.NodeType != XmlNodeType.Attribute)
		{
			return false;
		}
		_currentContentIndex = 0;
		_currentAttrIndex = -1;
		Read();
		return true;
	}

	public override bool Read()
	{
		switch (_cacheState)
		{
		case CachingReaderState.Init:
			_cacheState = CachingReaderState.Record;
			goto case CachingReaderState.Record;
		case CachingReaderState.Record:
		{
			ValidatingReaderNodeData validatingReaderNodeData = null;
			if (_coreReader.Read())
			{
				switch (_coreReader.NodeType)
				{
				case XmlNodeType.Element:
					_cacheState = CachingReaderState.ReaderClosed;
					return false;
				case XmlNodeType.EndElement:
					validatingReaderNodeData = AddContent(_coreReader.NodeType);
					validatingReaderNodeData.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
					validatingReaderNodeData.SetLineInfo(_lineInfo);
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Comment:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					validatingReaderNodeData = AddContent(_coreReader.NodeType);
					validatingReaderNodeData.SetItemData(_coreReader.Value);
					validatingReaderNodeData.SetLineInfo(_lineInfo);
					validatingReaderNodeData.Depth = _coreReader.Depth;
					break;
				}
				_cachedNode = validatingReaderNodeData;
				return true;
			}
			_cacheState = CachingReaderState.ReaderClosed;
			return false;
		}
		case CachingReaderState.Replay:
			if (_currentContentIndex >= _contentIndex)
			{
				_cacheState = CachingReaderState.ReaderClosed;
				_cacheHandler(this);
				if (_coreReader.NodeType != XmlNodeType.Element || _readAhead)
				{
					return _coreReader.Read();
				}
				return true;
			}
			_cachedNode = _contentEvents[_currentContentIndex];
			if (_currentContentIndex > 0)
			{
				ClearAttributesInfo();
			}
			_currentContentIndex++;
			return true;
		default:
			return false;
		}
	}

	internal ValidatingReaderNodeData RecordTextNode(string textValue, string originalStringValue, int depth, int lineNo, int linePos)
	{
		ValidatingReaderNodeData validatingReaderNodeData = AddContent(XmlNodeType.Text);
		validatingReaderNodeData.SetItemData(textValue, originalStringValue);
		validatingReaderNodeData.SetLineInfo(lineNo, linePos);
		validatingReaderNodeData.Depth = depth;
		return validatingReaderNodeData;
	}

	internal void SwitchTextNodeAndEndElement(string textValue, string originalStringValue)
	{
		ValidatingReaderNodeData validatingReaderNodeData = RecordTextNode(textValue, originalStringValue, _coreReader.Depth + 1, 0, 0);
		int num = _contentIndex - 2;
		ValidatingReaderNodeData validatingReaderNodeData2 = _contentEvents[num];
		_contentEvents[num] = validatingReaderNodeData;
		_contentEvents[_contentIndex - 1] = validatingReaderNodeData2;
	}

	internal void RecordEndElementNode()
	{
		ValidatingReaderNodeData validatingReaderNodeData = AddContent(XmlNodeType.EndElement);
		validatingReaderNodeData.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
		validatingReaderNodeData.SetLineInfo(_coreReader as IXmlLineInfo);
		if (_coreReader.IsEmptyElement)
		{
			_readAhead = true;
		}
	}

	internal string ReadOriginalContentAsString()
	{
		_returnOriginalStringValues = true;
		string result = InternalReadContentAsString();
		_returnOriginalStringValues = false;
		return result;
	}

	public override void Close()
	{
		_coreReader.Close();
		_cacheState = CachingReaderState.ReaderClosed;
	}

	public override void Skip()
	{
		XmlNodeType nodeType = _cachedNode.NodeType;
		if (nodeType != XmlNodeType.Element)
		{
			if (nodeType != XmlNodeType.Attribute)
			{
				Read();
				return;
			}
			MoveToElement();
		}
		if (_coreReader.NodeType != XmlNodeType.EndElement && !_readAhead)
		{
			int num = _coreReader.Depth - 1;
			while (_coreReader.Read() && _coreReader.Depth > num)
			{
			}
		}
		_coreReader.Read();
		_cacheState = CachingReaderState.ReaderClosed;
		_cacheHandler(this);
	}

	public override string LookupNamespace(string prefix)
	{
		return _coreReader.LookupNamespace(prefix);
	}

	public override void ResolveEntity()
	{
		throw new InvalidOperationException();
	}

	public override bool ReadAttributeValue()
	{
		if (_cachedNode.NodeType != XmlNodeType.Attribute)
		{
			return false;
		}
		_cachedNode = CreateDummyTextNode(_cachedNode.RawValue, _cachedNode.Depth + 1);
		return true;
	}

	bool IXmlLineInfo.HasLineInfo()
	{
		return true;
	}

	internal void SetToReplayMode()
	{
		_cacheState = CachingReaderState.Replay;
		_currentContentIndex = 0;
		_currentAttrIndex = -1;
		Read();
	}

	internal XmlReader GetCoreReader()
	{
		return _coreReader;
	}

	internal IXmlLineInfo GetLineInfo()
	{
		return _lineInfo;
	}

	private void ClearAttributesInfo()
	{
		_attributeCount = 0;
		_currentAttrIndex = -1;
	}

	private ValidatingReaderNodeData AddAttribute(int attIndex)
	{
		ValidatingReaderNodeData validatingReaderNodeData = _attributeEvents[attIndex];
		if (validatingReaderNodeData != null)
		{
			validatingReaderNodeData.Clear(XmlNodeType.Attribute);
			return validatingReaderNodeData;
		}
		if (attIndex >= _attributeEvents.Length - 1)
		{
			ValidatingReaderNodeData[] array = new ValidatingReaderNodeData[_attributeEvents.Length * 2];
			Array.Copy(_attributeEvents, array, _attributeEvents.Length);
			_attributeEvents = array;
		}
		validatingReaderNodeData = _attributeEvents[attIndex];
		if (validatingReaderNodeData == null)
		{
			validatingReaderNodeData = new ValidatingReaderNodeData(XmlNodeType.Attribute);
			_attributeEvents[attIndex] = validatingReaderNodeData;
		}
		return validatingReaderNodeData;
	}

	private ValidatingReaderNodeData AddContent(XmlNodeType nodeType)
	{
		ValidatingReaderNodeData validatingReaderNodeData = _contentEvents[_contentIndex];
		if (validatingReaderNodeData != null)
		{
			validatingReaderNodeData.Clear(nodeType);
			_contentIndex++;
			return validatingReaderNodeData;
		}
		if (_contentIndex >= _contentEvents.Length - 1)
		{
			ValidatingReaderNodeData[] array = new ValidatingReaderNodeData[_contentEvents.Length * 2];
			Array.Copy(_contentEvents, array, _contentEvents.Length);
			_contentEvents = array;
		}
		validatingReaderNodeData = _contentEvents[_contentIndex];
		if (validatingReaderNodeData == null)
		{
			validatingReaderNodeData = new ValidatingReaderNodeData(nodeType);
			_contentEvents[_contentIndex] = validatingReaderNodeData;
		}
		_contentIndex++;
		return validatingReaderNodeData;
	}

	private void RecordAttributes()
	{
		_attributeCount = _coreReader.AttributeCount;
		if (_coreReader.MoveToFirstAttribute())
		{
			int num = 0;
			do
			{
				ValidatingReaderNodeData validatingReaderNodeData = AddAttribute(num);
				validatingReaderNodeData.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
				validatingReaderNodeData.SetLineInfo(_lineInfo);
				validatingReaderNodeData.RawValue = _coreReader.Value;
				num++;
			}
			while (_coreReader.MoveToNextAttribute());
			_coreReader.MoveToElement();
		}
	}

	private int GetAttributeIndexWithoutPrefix(string name)
	{
		string text = _coreReaderNameTable.Get(name);
		if (text == null)
		{
			return -1;
		}
		for (int i = 0; i < _attributeCount; i++)
		{
			ValidatingReaderNodeData validatingReaderNodeData = _attributeEvents[i];
			if (Ref.Equal(validatingReaderNodeData.LocalName, text) && validatingReaderNodeData.Prefix.Length == 0)
			{
				return i;
			}
		}
		return -1;
	}

	private int GetAttributeIndexWithPrefix(string name)
	{
		string text = _coreReaderNameTable.Get(name);
		if (text == null)
		{
			return -1;
		}
		for (int i = 0; i < _attributeCount; i++)
		{
			ValidatingReaderNodeData validatingReaderNodeData = _attributeEvents[i];
			if (Ref.Equal(validatingReaderNodeData.GetAtomizedNameWPrefix(_coreReaderNameTable), text))
			{
				return i;
			}
		}
		return -1;
	}

	private ValidatingReaderNodeData CreateDummyTextNode(string attributeValue, int depth)
	{
		if (_textNode == null)
		{
			_textNode = new ValidatingReaderNodeData(XmlNodeType.Text);
		}
		_textNode.Depth = depth;
		_textNode.RawValue = attributeValue;
		return _textNode;
	}

	public override Task<string> GetValueAsync()
	{
		if (_returnOriginalStringValues)
		{
			return Task.FromResult(_cachedNode.OriginalStringValue);
		}
		return Task.FromResult(_cachedNode.RawValue);
	}

	public override async Task<bool> ReadAsync()
	{
		switch (_cacheState)
		{
		case CachingReaderState.Init:
			_cacheState = CachingReaderState.Record;
			goto case CachingReaderState.Record;
		case CachingReaderState.Record:
		{
			ValidatingReaderNodeData recordedNode = null;
			if (await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				switch (_coreReader.NodeType)
				{
				case XmlNodeType.Element:
					_cacheState = CachingReaderState.ReaderClosed;
					return false;
				case XmlNodeType.EndElement:
					recordedNode = AddContent(_coreReader.NodeType);
					recordedNode.SetItemData(_coreReader.LocalName, _coreReader.Prefix, _coreReader.NamespaceURI, _coreReader.Depth);
					recordedNode.SetLineInfo(_lineInfo);
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Comment:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
				{
					recordedNode = AddContent(_coreReader.NodeType);
					ValidatingReaderNodeData validatingReaderNodeData = recordedNode;
					validatingReaderNodeData.SetItemData(await _coreReader.GetValueAsync().ConfigureAwait(continueOnCapturedContext: false));
					recordedNode.SetLineInfo(_lineInfo);
					recordedNode.Depth = _coreReader.Depth;
					break;
				}
				}
				_cachedNode = recordedNode;
				return true;
			}
			_cacheState = CachingReaderState.ReaderClosed;
			return false;
		}
		case CachingReaderState.Replay:
			if (_currentContentIndex >= _contentIndex)
			{
				_cacheState = CachingReaderState.ReaderClosed;
				_cacheHandler(this);
				if (_coreReader.NodeType != XmlNodeType.Element || _readAhead)
				{
					return await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				return true;
			}
			_cachedNode = _contentEvents[_currentContentIndex];
			if (_currentContentIndex > 0)
			{
				ClearAttributesInfo();
			}
			_currentContentIndex++;
			return true;
		default:
			return false;
		}
	}

	public override async Task SkipAsync()
	{
		XmlNodeType nodeType = _cachedNode.NodeType;
		if (nodeType != XmlNodeType.Element)
		{
			if (nodeType != XmlNodeType.Attribute)
			{
				await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
				return;
			}
			MoveToElement();
		}
		if (_coreReader.NodeType != XmlNodeType.EndElement && !_readAhead)
		{
			int startDepth = _coreReader.Depth - 1;
			while (await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false) && _coreReader.Depth > startDepth)
			{
			}
		}
		await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		_cacheState = CachingReaderState.ReaderClosed;
		_cacheHandler(this);
	}

	internal Task SetToReplayModeAsync()
	{
		_cacheState = CachingReaderState.Replay;
		_currentContentIndex = 0;
		_currentAttrIndex = -1;
		return ReadAsync();
	}
}
