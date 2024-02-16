using System.Collections.Generic;
using System.Xml.Schema;

namespace System.Xml;

public class XmlNodeReader : XmlReader, IXmlNamespaceResolver
{
	private readonly XmlNodeReaderNavigator _readerNav;

	private XmlNodeType _nodeType;

	private int _curDepth;

	private ReadState _readState;

	private bool _fEOF;

	private bool _bResolveEntity;

	private bool _bStartFromDocument;

	private bool _bInReadBinary;

	private ReadContentAsBinaryHelper _readBinaryHelper;

	public override XmlNodeType NodeType
	{
		get
		{
			if (!IsInReadingStates())
			{
				return XmlNodeType.None;
			}
			return _nodeType;
		}
	}

	public override string Name
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.Name;
		}
	}

	public override string LocalName
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.LocalName;
		}
	}

	public override string NamespaceURI
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.NamespaceURI;
		}
	}

	public override string Prefix
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.Prefix;
		}
	}

	public override bool HasValue
	{
		get
		{
			if (!IsInReadingStates())
			{
				return false;
			}
			return _readerNav.HasValue;
		}
	}

	public override string Value
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.Value;
		}
	}

	public override int Depth => _curDepth;

	public override string BaseURI => _readerNav.BaseURI;

	public override bool CanResolveEntity => true;

	public override bool IsEmptyElement
	{
		get
		{
			if (!IsInReadingStates())
			{
				return false;
			}
			return _readerNav.IsEmptyElement;
		}
	}

	public override bool IsDefault
	{
		get
		{
			if (!IsInReadingStates())
			{
				return false;
			}
			return _readerNav.IsDefault;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			if (!IsInReadingStates())
			{
				return XmlSpace.None;
			}
			return _readerNav.XmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			if (!IsInReadingStates())
			{
				return string.Empty;
			}
			return _readerNav.XmlLang;
		}
	}

	public override IXmlSchemaInfo? SchemaInfo
	{
		get
		{
			if (!IsInReadingStates())
			{
				return null;
			}
			return _readerNav.SchemaInfo;
		}
	}

	public override int AttributeCount
	{
		get
		{
			if (!IsInReadingStates() || _nodeType == XmlNodeType.EndElement)
			{
				return 0;
			}
			return _readerNav.AttributeCount;
		}
	}

	public override bool EOF
	{
		get
		{
			if (_readState != ReadState.Closed)
			{
				return _fEOF;
			}
			return false;
		}
	}

	public override ReadState ReadState => _readState;

	public override bool HasAttributes => AttributeCount > 0;

	public override XmlNameTable NameTable => _readerNav.NameTable;

	public override bool CanReadBinaryContent => true;

	internal override IDtdInfo? DtdInfo => _readerNav.Document.DtdSchemaInfo;

	public XmlNodeReader(XmlNode node)
	{
		if (node == null)
		{
			throw new ArgumentNullException("node");
		}
		_readerNav = new XmlNodeReaderNavigator(node);
		_curDepth = 0;
		_readState = ReadState.Initial;
		_fEOF = false;
		_nodeType = XmlNodeType.None;
		_bResolveEntity = false;
		_bStartFromDocument = false;
	}

	internal bool IsInReadingStates()
	{
		return _readState == ReadState.Interactive;
	}

	public override string? GetAttribute(string name)
	{
		if (!IsInReadingStates())
		{
			return null;
		}
		return _readerNav.GetAttribute(name);
	}

	public override string? GetAttribute(string name, string? namespaceURI)
	{
		if (!IsInReadingStates())
		{
			return null;
		}
		string ns = ((namespaceURI == null) ? string.Empty : namespaceURI);
		return _readerNav.GetAttribute(name, ns);
	}

	public override string GetAttribute(int attributeIndex)
	{
		if (!IsInReadingStates())
		{
			throw new ArgumentOutOfRangeException("attributeIndex");
		}
		return _readerNav.GetAttribute(attributeIndex);
	}

	public override bool MoveToAttribute(string name)
	{
		if (!IsInReadingStates())
		{
			return false;
		}
		_readerNav.ResetMove(ref _curDepth, ref _nodeType);
		if (_readerNav.MoveToAttribute(name))
		{
			_curDepth++;
			_nodeType = _readerNav.NodeType;
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
			return true;
		}
		_readerNav.RollBackMove(ref _curDepth);
		return false;
	}

	public override bool MoveToAttribute(string name, string? namespaceURI)
	{
		if (!IsInReadingStates())
		{
			return false;
		}
		_readerNav.ResetMove(ref _curDepth, ref _nodeType);
		string namespaceURI2 = ((namespaceURI == null) ? string.Empty : namespaceURI);
		if (_readerNav.MoveToAttribute(name, namespaceURI2))
		{
			_curDepth++;
			_nodeType = _readerNav.NodeType;
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
			return true;
		}
		_readerNav.RollBackMove(ref _curDepth);
		return false;
	}

	public override void MoveToAttribute(int attributeIndex)
	{
		if (!IsInReadingStates())
		{
			throw new ArgumentOutOfRangeException("attributeIndex");
		}
		_readerNav.ResetMove(ref _curDepth, ref _nodeType);
		try
		{
			if (AttributeCount <= 0)
			{
				throw new ArgumentOutOfRangeException("attributeIndex");
			}
			_readerNav.MoveToAttribute(attributeIndex);
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
		}
		catch
		{
			_readerNav.RollBackMove(ref _curDepth);
			throw;
		}
		_curDepth++;
		_nodeType = _readerNav.NodeType;
	}

	public override bool MoveToFirstAttribute()
	{
		if (!IsInReadingStates())
		{
			return false;
		}
		_readerNav.ResetMove(ref _curDepth, ref _nodeType);
		if (AttributeCount > 0)
		{
			_readerNav.MoveToAttribute(0);
			_curDepth++;
			_nodeType = _readerNav.NodeType;
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
			return true;
		}
		_readerNav.RollBackMove(ref _curDepth);
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (!IsInReadingStates() || _nodeType == XmlNodeType.EndElement)
		{
			return false;
		}
		_readerNav.LogMove(_curDepth);
		_readerNav.ResetToAttribute(ref _curDepth);
		if (_readerNav.MoveToNextAttribute(ref _curDepth))
		{
			_nodeType = _readerNav.NodeType;
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
			return true;
		}
		_readerNav.RollBackMove(ref _curDepth);
		return false;
	}

	public override bool MoveToElement()
	{
		if (!IsInReadingStates())
		{
			return false;
		}
		_readerNav.LogMove(_curDepth);
		_readerNav.ResetToAttribute(ref _curDepth);
		if (_readerNav.MoveToElement())
		{
			_curDepth--;
			_nodeType = _readerNav.NodeType;
			if (_bInReadBinary)
			{
				FinishReadBinary();
			}
			return true;
		}
		_readerNav.RollBackMove(ref _curDepth);
		return false;
	}

	public override bool Read()
	{
		return Read(fSkipChildren: false);
	}

	private bool Read(bool fSkipChildren)
	{
		if (_fEOF)
		{
			return false;
		}
		if (_readState == ReadState.Initial)
		{
			if (_readerNav.NodeType == XmlNodeType.Document || _readerNav.NodeType == XmlNodeType.DocumentFragment)
			{
				_bStartFromDocument = true;
				if (!ReadNextNode(fSkipChildren))
				{
					_readState = ReadState.Error;
					return false;
				}
			}
			ReSetReadingMarks();
			_readState = ReadState.Interactive;
			_nodeType = _readerNav.NodeType;
			_curDepth = 0;
			return true;
		}
		if (_bInReadBinary)
		{
			FinishReadBinary();
		}
		bool flag = false;
		if (_readerNav.CreatedOnAttribute)
		{
			return false;
		}
		ReSetReadingMarks();
		if (ReadNextNode(fSkipChildren))
		{
			return true;
		}
		if (_readState == ReadState.Initial || _readState == ReadState.Interactive)
		{
			_readState = ReadState.Error;
		}
		if (_readState == ReadState.EndOfFile)
		{
			_nodeType = XmlNodeType.None;
		}
		return false;
	}

	private bool ReadNextNode(bool fSkipChildren)
	{
		if (_readState != ReadState.Interactive && _readState != 0)
		{
			_nodeType = XmlNodeType.None;
			return false;
		}
		bool flag = !fSkipChildren;
		XmlNodeType nodeType = _readerNav.NodeType;
		if (flag && _nodeType != XmlNodeType.EndElement && _nodeType != XmlNodeType.EndEntity && (nodeType == XmlNodeType.Element || (nodeType == XmlNodeType.EntityReference && _bResolveEntity) || ((_readerNav.NodeType == XmlNodeType.Document || _readerNav.NodeType == XmlNodeType.DocumentFragment) && _readState == ReadState.Initial)))
		{
			if (_readerNav.MoveToFirstChild())
			{
				_nodeType = _readerNav.NodeType;
				_curDepth++;
				if (_bResolveEntity)
				{
					_bResolveEntity = false;
				}
				return true;
			}
			if (_readerNav.NodeType == XmlNodeType.Element && !_readerNav.IsEmptyElement)
			{
				_nodeType = XmlNodeType.EndElement;
				return true;
			}
			if (_readerNav.NodeType == XmlNodeType.EntityReference && _bResolveEntity)
			{
				_bResolveEntity = false;
				_nodeType = XmlNodeType.EndEntity;
				return true;
			}
			return ReadForward(fSkipChildren);
		}
		if (_readerNav.NodeType == XmlNodeType.EntityReference && _bResolveEntity)
		{
			if (_readerNav.MoveToFirstChild())
			{
				_nodeType = _readerNav.NodeType;
				_curDepth++;
			}
			else
			{
				_nodeType = XmlNodeType.EndEntity;
			}
			_bResolveEntity = false;
			return true;
		}
		return ReadForward(fSkipChildren);
	}

	private void SetEndOfFile()
	{
		_fEOF = true;
		_readState = ReadState.EndOfFile;
		_nodeType = XmlNodeType.None;
	}

	private bool ReadAtZeroLevel(bool fSkipChildren)
	{
		if (!fSkipChildren && _nodeType != XmlNodeType.EndElement && _readerNav.NodeType == XmlNodeType.Element && !_readerNav.IsEmptyElement)
		{
			_nodeType = XmlNodeType.EndElement;
			return true;
		}
		SetEndOfFile();
		return false;
	}

	private bool ReadForward(bool fSkipChildren)
	{
		if (_readState == ReadState.Error)
		{
			return false;
		}
		if (!_bStartFromDocument && _curDepth == 0)
		{
			return ReadAtZeroLevel(fSkipChildren);
		}
		if (_readerNav.MoveToNext())
		{
			_nodeType = _readerNav.NodeType;
			return true;
		}
		if (_curDepth == 0)
		{
			return ReadAtZeroLevel(fSkipChildren);
		}
		if (_readerNav.MoveToParent())
		{
			if (_readerNav.NodeType == XmlNodeType.Element)
			{
				_curDepth--;
				_nodeType = XmlNodeType.EndElement;
				return true;
			}
			if (_readerNav.NodeType == XmlNodeType.EntityReference)
			{
				_curDepth--;
				_nodeType = XmlNodeType.EndEntity;
				return true;
			}
			return true;
		}
		return false;
	}

	private void ReSetReadingMarks()
	{
		_readerNav.ResetMove(ref _curDepth, ref _nodeType);
	}

	public override void Close()
	{
		_readState = ReadState.Closed;
	}

	public override void Skip()
	{
		Read(fSkipChildren: true);
	}

	public override string ReadString()
	{
		if (NodeType == XmlNodeType.EntityReference && _bResolveEntity && !Read())
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
		return base.ReadString();
	}

	public override string? LookupNamespace(string prefix)
	{
		if (!IsInReadingStates())
		{
			return null;
		}
		string text = _readerNav.LookupNamespace(prefix);
		if (text != null && text.Length == 0)
		{
			return null;
		}
		return text;
	}

	public override void ResolveEntity()
	{
		if (!IsInReadingStates() || _nodeType != XmlNodeType.EntityReference)
		{
			throw new InvalidOperationException(System.SR.Xnr_ResolveEntity);
		}
		_bResolveEntity = true;
	}

	public override bool ReadAttributeValue()
	{
		if (!IsInReadingStates())
		{
			return false;
		}
		if (_readerNav.ReadAttributeValue(ref _curDepth, ref _bResolveEntity, ref _nodeType))
		{
			_bInReadBinary = false;
			return true;
		}
		return false;
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		if (_readState != ReadState.Interactive)
		{
			return 0;
		}
		if (!_bInReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		_bInReadBinary = false;
		int result = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);
		_bInReadBinary = true;
		return result;
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (_readState != ReadState.Interactive)
		{
			return 0;
		}
		if (!_bInReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		_bInReadBinary = false;
		int result = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);
		_bInReadBinary = true;
		return result;
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		if (_readState != ReadState.Interactive)
		{
			return 0;
		}
		if (!_bInReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		_bInReadBinary = false;
		int result = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);
		_bInReadBinary = true;
		return result;
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (_readState != ReadState.Interactive)
		{
			return 0;
		}
		if (!_bInReadBinary)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
		}
		_bInReadBinary = false;
		int result = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);
		_bInReadBinary = true;
		return result;
	}

	private void FinishReadBinary()
	{
		_bInReadBinary = false;
		_readBinaryHelper.Finish();
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _readerNav.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return _readerNav.LookupPrefix(namespaceName);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		if (!IsInReadingStates())
		{
			return _readerNav.DefaultLookupNamespace(prefix);
		}
		string text = _readerNav.LookupNamespace(prefix);
		if (text != null)
		{
			text = _readerNav.NameTable.Add(text);
		}
		return text;
	}
}
