using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Xml.Xsl.XsltOld;

internal sealed class ReaderOutput : XmlReader, IRecordOutput
{
	private sealed class XmlEncoder
	{
		private StringBuilder _buffer;

		private XmlTextEncoder _encoder;

		public char QuoteChar => '"';

		[MemberNotNull("_buffer")]
		[MemberNotNull("_encoder")]
		private void Init()
		{
			_buffer = new StringBuilder();
			_encoder = new XmlTextEncoder(new StringWriter(_buffer, CultureInfo.InvariantCulture));
		}

		public string AttributeInnerXml(string value)
		{
			if (_encoder == null)
			{
				Init();
			}
			_buffer.Length = 0;
			_encoder.StartAttribute(cacheAttrValue: false);
			_encoder.Write(value);
			_encoder.EndAttribute();
			return _buffer.ToString();
		}

		public string AttributeOuterXml(string name, string value)
		{
			if (_encoder == null)
			{
				Init();
			}
			_buffer.Length = 0;
			_buffer.Append(name);
			_buffer.Append('=');
			_buffer.Append(QuoteChar);
			_encoder.StartAttribute(cacheAttrValue: false);
			_encoder.Write(value);
			_encoder.EndAttribute();
			_buffer.Append(QuoteChar);
			return _buffer.ToString();
		}
	}

	private Processor _processor;

	private readonly XmlNameTable _nameTable;

	private RecordBuilder _builder;

	private BuilderInfo _mainNode;

	private ArrayList _attributeList;

	private int _attributeCount;

	private BuilderInfo _attributeValue;

	private OutputScopeManager _manager;

	private int _currentIndex;

	private BuilderInfo _currentInfo;

	private ReadState _state;

	private bool _haveRecord;

	private static readonly BuilderInfo s_DefaultInfo = new BuilderInfo();

	private readonly XmlEncoder _encoder = new XmlEncoder();

	public override XmlNodeType NodeType => _currentInfo.NodeType;

	public override string Name
	{
		get
		{
			string prefix = Prefix;
			string localName = LocalName;
			if (prefix != null && prefix.Length > 0)
			{
				if (localName.Length > 0)
				{
					return _nameTable.Add(prefix + ":" + localName);
				}
				return prefix;
			}
			return localName;
		}
	}

	public override string LocalName => _currentInfo.LocalName;

	public override string NamespaceURI => _currentInfo.NamespaceURI;

	public override string Prefix => _currentInfo.Prefix;

	public override bool HasValue => XmlReader.HasValueInternal(NodeType);

	public override string Value => _currentInfo.Value;

	public override int Depth => _currentInfo.Depth;

	public override string BaseURI => string.Empty;

	public override bool IsEmptyElement => _currentInfo.IsEmptyTag;

	public override char QuoteChar => _encoder.QuoteChar;

	public override bool IsDefault => false;

	public override XmlSpace XmlSpace
	{
		get
		{
			if (_manager == null)
			{
				return XmlSpace.None;
			}
			return _manager.XmlSpace;
		}
	}

	public override string XmlLang
	{
		get
		{
			if (_manager == null)
			{
				return string.Empty;
			}
			return _manager.XmlLang;
		}
	}

	public override int AttributeCount => _attributeCount;

	public override string this[int i] => GetAttribute(i);

	public override string this[string name, string namespaceURI] => GetAttribute(name, namespaceURI);

	public override bool EOF => _state == ReadState.EndOfFile;

	public override ReadState ReadState => _state;

	public override XmlNameTable NameTable => _nameTable;

	internal ReaderOutput(Processor processor)
	{
		_processor = processor;
		_nameTable = processor.NameTable;
		Reset();
	}

	public override string GetAttribute(string name)
	{
		if (FindAttribute(name, out var attrIndex))
		{
			return ((BuilderInfo)_attributeList[attrIndex]).Value;
		}
		return null;
	}

	public override string GetAttribute(string localName, string namespaceURI)
	{
		if (FindAttribute(localName, namespaceURI, out var attrIndex))
		{
			return ((BuilderInfo)_attributeList[attrIndex]).Value;
		}
		return null;
	}

	public override string GetAttribute(int i)
	{
		BuilderInfo builderInfo = GetBuilderInfo(i);
		return builderInfo.Value;
	}

	public override bool MoveToAttribute(string name)
	{
		if (FindAttribute(name, out var attrIndex))
		{
			SetAttribute(attrIndex);
			return true;
		}
		return false;
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		if (FindAttribute(localName, namespaceURI, out var attrIndex))
		{
			SetAttribute(attrIndex);
			return true;
		}
		return false;
	}

	public override void MoveToAttribute(int i)
	{
		if (i < 0 || _attributeCount <= i)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		SetAttribute(i);
	}

	public override bool MoveToFirstAttribute()
	{
		if (_attributeCount <= 0)
		{
			return false;
		}
		SetAttribute(0);
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (_currentIndex + 1 < _attributeCount)
		{
			SetAttribute(_currentIndex + 1);
			return true;
		}
		return false;
	}

	public override bool MoveToElement()
	{
		if (NodeType == XmlNodeType.Attribute || _currentInfo == _attributeValue)
		{
			SetMainNode();
			return true;
		}
		return false;
	}

	public override bool Read()
	{
		if (_state != ReadState.Interactive)
		{
			if (_state != 0)
			{
				return false;
			}
			_state = ReadState.Interactive;
		}
		while (true)
		{
			if (_haveRecord)
			{
				_processor.ResetOutput();
				_haveRecord = false;
			}
			_processor.Execute();
			if (_haveRecord)
			{
				switch (NodeType)
				{
				case XmlNodeType.Text:
					if (!XmlCharType.IsOnlyWhitespace(Value))
					{
						break;
					}
					_currentInfo.NodeType = XmlNodeType.Whitespace;
					goto IL_0075;
				case XmlNodeType.Whitespace:
					goto IL_0075;
				}
			}
			else
			{
				_state = ReadState.EndOfFile;
				Reset();
			}
			break;
			IL_0075:
			if (Value.Length != 0)
			{
				if (XmlSpace == XmlSpace.Preserve)
				{
					_currentInfo.NodeType = XmlNodeType.SignificantWhitespace;
				}
				break;
			}
		}
		return _haveRecord;
	}

	public override void Close()
	{
		_processor = null;
		_state = ReadState.Closed;
		Reset();
	}

	public override string ReadString()
	{
		string text = string.Empty;
		if (NodeType == XmlNodeType.Element || NodeType == XmlNodeType.Attribute || _currentInfo == _attributeValue)
		{
			if (_mainNode.IsEmptyTag)
			{
				return text;
			}
			if (!Read())
			{
				throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
			}
		}
		StringBuilder stringBuilder = null;
		bool flag = true;
		while (true)
		{
			XmlNodeType nodeType = NodeType;
			if (nodeType != XmlNodeType.Text && (uint)(nodeType - 13) > 1u)
			{
				break;
			}
			if (flag)
			{
				text = Value;
				flag = false;
			}
			else
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(text);
				}
				stringBuilder.Append(Value);
			}
			if (!Read())
			{
				throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
			}
		}
		if (stringBuilder != null)
		{
			return stringBuilder.ToString();
		}
		return text;
	}

	public override string ReadInnerXml()
	{
		if (ReadState == ReadState.Interactive)
		{
			if (NodeType == XmlNodeType.Element && !IsEmptyElement)
			{
				StringOutput stringOutput = new StringOutput(_processor);
				stringOutput.OmitXmlDecl();
				int depth = Depth;
				Read();
				while (depth < Depth)
				{
					stringOutput.RecordDone(_builder);
					Read();
				}
				Read();
				stringOutput.TheEnd();
				return stringOutput.Result;
			}
			if (NodeType == XmlNodeType.Attribute)
			{
				return _encoder.AttributeInnerXml(Value);
			}
			Read();
		}
		return string.Empty;
	}

	public override string ReadOuterXml()
	{
		if (ReadState == ReadState.Interactive)
		{
			if (NodeType == XmlNodeType.Element)
			{
				StringOutput stringOutput = new StringOutput(_processor);
				stringOutput.OmitXmlDecl();
				bool isEmptyElement = IsEmptyElement;
				int depth = Depth;
				stringOutput.RecordDone(_builder);
				Read();
				while (depth < Depth)
				{
					stringOutput.RecordDone(_builder);
					Read();
				}
				if (!isEmptyElement)
				{
					stringOutput.RecordDone(_builder);
					Read();
				}
				stringOutput.TheEnd();
				return stringOutput.Result;
			}
			if (NodeType == XmlNodeType.Attribute)
			{
				return _encoder.AttributeOuterXml(Name, Value);
			}
			Read();
		}
		return string.Empty;
	}

	public override string LookupNamespace(string prefix)
	{
		string text = _nameTable.Get(prefix);
		if (_manager != null && text != null)
		{
			return _manager.ResolveNamespace(text);
		}
		return null;
	}

	public override void ResolveEntity()
	{
		if (NodeType != XmlNodeType.EntityReference)
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
	}

	public override bool ReadAttributeValue()
	{
		if (ReadState != ReadState.Interactive || NodeType != XmlNodeType.Attribute)
		{
			return false;
		}
		if (_attributeValue == null)
		{
			_attributeValue = new BuilderInfo();
			_attributeValue.NodeType = XmlNodeType.Text;
		}
		if (_currentInfo == _attributeValue)
		{
			return false;
		}
		_attributeValue.Value = _currentInfo.Value;
		_attributeValue.Depth = _currentInfo.Depth + 1;
		_currentInfo = _attributeValue;
		return true;
	}

	[MemberNotNull("_builder")]
	[MemberNotNull("_attributeList")]
	public Processor.OutputResult RecordDone(RecordBuilder record)
	{
		_builder = record;
		_mainNode = record.MainNode;
		_attributeList = record.AttributeList;
		_attributeCount = record.AttributeCount;
		_manager = record.Manager;
		_haveRecord = true;
		SetMainNode();
		return Processor.OutputResult.Interrupt;
	}

	public void TheEnd()
	{
	}

	private void SetMainNode()
	{
		_currentIndex = -1;
		_currentInfo = _mainNode;
	}

	private void SetAttribute(int attrib)
	{
		_currentIndex = attrib;
		_currentInfo = (BuilderInfo)_attributeList[attrib];
	}

	private BuilderInfo GetBuilderInfo(int attrib)
	{
		if (attrib < 0 || _attributeCount <= attrib)
		{
			throw new ArgumentOutOfRangeException("attrib");
		}
		return (BuilderInfo)_attributeList[attrib];
	}

	private bool FindAttribute(string localName, string namespaceURI, out int attrIndex)
	{
		if (namespaceURI == null)
		{
			namespaceURI = string.Empty;
		}
		if (localName == null)
		{
			localName = string.Empty;
		}
		for (int i = 0; i < _attributeCount; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[i];
			if (builderInfo.NamespaceURI == namespaceURI && builderInfo.LocalName == localName)
			{
				attrIndex = i;
				return true;
			}
		}
		attrIndex = -1;
		return false;
	}

	private bool FindAttribute(string name, out int attrIndex)
	{
		if (name == null)
		{
			name = string.Empty;
		}
		for (int i = 0; i < _attributeCount; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[i];
			if (builderInfo.Name == name)
			{
				attrIndex = i;
				return true;
			}
		}
		attrIndex = -1;
		return false;
	}

	[MemberNotNull("_currentInfo")]
	[MemberNotNull("_mainNode")]
	private void Reset()
	{
		_currentIndex = -1;
		_currentInfo = s_DefaultInfo;
		_mainNode = s_DefaultInfo;
		_manager = null;
	}
}
