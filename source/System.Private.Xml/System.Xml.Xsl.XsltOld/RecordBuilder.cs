using System.Collections;
using System.Text;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class RecordBuilder
{
	private int _outputState;

	private RecordBuilder _next;

	private readonly IRecordOutput _output;

	private readonly XmlNameTable _nameTable;

	private readonly OutKeywords _atoms;

	private readonly OutputScopeManager _scopeManager;

	private readonly BuilderInfo _mainNode = new BuilderInfo();

	private readonly ArrayList _attributeList = new ArrayList();

	private int _attributeCount;

	private readonly ArrayList _namespaceList = new ArrayList();

	private int _namespaceCount;

	private readonly BuilderInfo _dummy = new BuilderInfo();

	private BuilderInfo _currentInfo;

	private bool _popScope;

	private int _recordState;

	private int _recordDepth;

	internal int OutputState
	{
		get
		{
			return _outputState;
		}
		set
		{
			_outputState = value;
		}
	}

	internal RecordBuilder Next
	{
		get
		{
			return _next;
		}
		set
		{
			_next = value;
		}
	}

	internal IRecordOutput Output => _output;

	internal BuilderInfo MainNode => _mainNode;

	internal ArrayList AttributeList => _attributeList;

	internal int AttributeCount => _attributeCount;

	internal OutputScopeManager Manager => _scopeManager;

	internal RecordBuilder(IRecordOutput output, XmlNameTable nameTable)
	{
		_output = output;
		_nameTable = ((nameTable != null) ? nameTable : new NameTable());
		_atoms = new OutKeywords(_nameTable);
		_scopeManager = new OutputScopeManager(_nameTable, _atoms);
	}

	private void ValueAppend(string s, bool disableOutputEscaping)
	{
		_currentInfo.ValueAppend(s, disableOutputEscaping);
	}

	private bool CanOutput(int state)
	{
		if (_recordState == 0 || (state & 0x2000) == 0)
		{
			return true;
		}
		_recordState = 2;
		FinalizeRecord();
		SetEmptyFlag(state);
		return _output.RecordDone(this) == Processor.OutputResult.Continue;
	}

	internal Processor.OutputResult BeginEvent(int state, XPathNodeType nodeType, string prefix, string name, string nspace, bool empty, object htmlProps, bool search)
	{
		if (!CanOutput(state))
		{
			return Processor.OutputResult.Overflow;
		}
		AdjustDepth(state);
		ResetRecord(state);
		PopElementScope();
		prefix = ((prefix != null) ? _nameTable.Add(prefix) : _atoms.Empty);
		name = ((name != null) ? _nameTable.Add(name) : _atoms.Empty);
		nspace = ((nspace != null) ? _nameTable.Add(nspace) : _atoms.Empty);
		switch (nodeType)
		{
		case XPathNodeType.Element:
			_mainNode.htmlProps = htmlProps as HtmlElementProps;
			_mainNode.search = search;
			BeginElement(prefix, name, nspace, empty);
			break;
		case XPathNodeType.Attribute:
			BeginAttribute(prefix, name, nspace, htmlProps, search);
			break;
		case XPathNodeType.Namespace:
			BeginNamespace(name, nspace);
			break;
		case XPathNodeType.ProcessingInstruction:
			if (!BeginProcessingInstruction(prefix, name, nspace))
			{
				return Processor.OutputResult.Error;
			}
			break;
		case XPathNodeType.Comment:
			BeginComment();
			break;
		}
		return CheckRecordBegin(state);
	}

	internal Processor.OutputResult TextEvent(int state, string text, bool disableOutputEscaping)
	{
		if (!CanOutput(state))
		{
			return Processor.OutputResult.Overflow;
		}
		AdjustDepth(state);
		ResetRecord(state);
		PopElementScope();
		if (((uint)state & 0x2000u) != 0)
		{
			_currentInfo.Depth = _recordDepth;
			_currentInfo.NodeType = XmlNodeType.Text;
		}
		ValueAppend(text, disableOutputEscaping);
		return CheckRecordBegin(state);
	}

	internal Processor.OutputResult EndEvent(int state, XPathNodeType nodeType)
	{
		if (!CanOutput(state))
		{
			return Processor.OutputResult.Overflow;
		}
		AdjustDepth(state);
		PopElementScope();
		_popScope = (state & 0x10000) != 0;
		if (((uint)state & 0x1000u) != 0 && _mainNode.IsEmptyTag)
		{
			return Processor.OutputResult.Continue;
		}
		ResetRecord(state);
		if (((uint)state & 0x2000u) != 0 && nodeType == XPathNodeType.Element)
		{
			EndElement();
		}
		return CheckRecordEnd(state);
	}

	internal void Reset()
	{
		if (_recordState == 2)
		{
			_recordState = 0;
		}
	}

	internal void TheEnd()
	{
		if (_recordState == 1)
		{
			_recordState = 2;
			FinalizeRecord();
			_output.RecordDone(this);
		}
		_output.TheEnd();
	}

	private int FindAttribute(string name, string nspace, ref string prefix)
	{
		for (int i = 0; i < _attributeCount; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[i];
			if (Ref.Equal(builderInfo.LocalName, name))
			{
				if (Ref.Equal(builderInfo.NamespaceURI, nspace))
				{
					return i;
				}
				if (Ref.Equal(builderInfo.Prefix, prefix))
				{
					prefix = string.Empty;
				}
			}
		}
		return -1;
	}

	private void BeginElement(string prefix, string name, string nspace, bool empty)
	{
		_currentInfo.NodeType = XmlNodeType.Element;
		_currentInfo.Prefix = prefix;
		_currentInfo.LocalName = name;
		_currentInfo.NamespaceURI = nspace;
		_currentInfo.Depth = _recordDepth;
		_currentInfo.IsEmptyTag = empty;
		_scopeManager.PushScope(name, nspace, prefix);
	}

	private void EndElement()
	{
		OutputScope currentElementScope = _scopeManager.CurrentElementScope;
		_currentInfo.NodeType = XmlNodeType.EndElement;
		_currentInfo.Prefix = currentElementScope.Prefix;
		_currentInfo.LocalName = currentElementScope.Name;
		_currentInfo.NamespaceURI = currentElementScope.Namespace;
		_currentInfo.Depth = _recordDepth;
	}

	private int NewAttribute()
	{
		if (_attributeCount >= _attributeList.Count)
		{
			_attributeList.Add(new BuilderInfo());
		}
		return _attributeCount++;
	}

	private void BeginAttribute(string prefix, string name, string nspace, object htmlAttrProps, bool search)
	{
		int num = FindAttribute(name, nspace, ref prefix);
		if (num == -1)
		{
			num = NewAttribute();
		}
		BuilderInfo builderInfo = (BuilderInfo)_attributeList[num];
		builderInfo.Initialize(prefix, name, nspace);
		builderInfo.Depth = _recordDepth;
		builderInfo.NodeType = XmlNodeType.Attribute;
		builderInfo.htmlAttrProps = htmlAttrProps as HtmlAttributeProps;
		builderInfo.search = search;
		_currentInfo = builderInfo;
	}

	private void BeginNamespace(string name, string nspace)
	{
		bool thisScope = false;
		if (Ref.Equal(name, _atoms.Empty))
		{
			if (!Ref.Equal(nspace, _scopeManager.DefaultNamespace) && !Ref.Equal(_mainNode.NamespaceURI, _atoms.Empty))
			{
				DeclareNamespace(nspace, name);
			}
		}
		else
		{
			string text = _scopeManager.ResolveNamespace(name, out thisScope);
			if (text != null)
			{
				if (!Ref.Equal(nspace, text) && !thisScope)
				{
					DeclareNamespace(nspace, name);
				}
			}
			else
			{
				DeclareNamespace(nspace, name);
			}
		}
		_currentInfo = _dummy;
		_currentInfo.NodeType = XmlNodeType.Attribute;
	}

	private bool BeginProcessingInstruction(string prefix, string name, string nspace)
	{
		_currentInfo.NodeType = XmlNodeType.ProcessingInstruction;
		_currentInfo.Prefix = prefix;
		_currentInfo.LocalName = name;
		_currentInfo.NamespaceURI = nspace;
		_currentInfo.Depth = _recordDepth;
		return true;
	}

	private void BeginComment()
	{
		_currentInfo.NodeType = XmlNodeType.Comment;
		_currentInfo.Depth = _recordDepth;
	}

	private void AdjustDepth(int state)
	{
		switch (state & 0x300)
		{
		case 256:
			_recordDepth++;
			break;
		case 512:
			_recordDepth--;
			break;
		}
	}

	private void ResetRecord(int state)
	{
		if (((uint)state & 0x2000u) != 0)
		{
			_attributeCount = 0;
			_namespaceCount = 0;
			_currentInfo = _mainNode;
			_currentInfo.Initialize(_atoms.Empty, _atoms.Empty, _atoms.Empty);
			_currentInfo.NodeType = XmlNodeType.None;
			_currentInfo.IsEmptyTag = false;
			_currentInfo.htmlProps = null;
			_currentInfo.htmlAttrProps = null;
		}
	}

	private void PopElementScope()
	{
		if (_popScope)
		{
			_scopeManager.PopScope();
			_popScope = false;
		}
	}

	private Processor.OutputResult CheckRecordBegin(int state)
	{
		if (((uint)state & 0x4000u) != 0)
		{
			_recordState = 2;
			FinalizeRecord();
			SetEmptyFlag(state);
			return _output.RecordDone(this);
		}
		_recordState = 1;
		return Processor.OutputResult.Continue;
	}

	private Processor.OutputResult CheckRecordEnd(int state)
	{
		if (((uint)state & 0x4000u) != 0)
		{
			_recordState = 2;
			FinalizeRecord();
			SetEmptyFlag(state);
			return _output.RecordDone(this);
		}
		return Processor.OutputResult.Continue;
	}

	private void SetEmptyFlag(int state)
	{
		if (((uint)state & 0x400u) != 0)
		{
			_mainNode.IsEmptyTag = false;
		}
	}

	private void AnalyzeSpaceLang()
	{
		for (int i = 0; i < _attributeCount; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[i];
			if (Ref.Equal(builderInfo.Prefix, _atoms.Xml))
			{
				OutputScope currentElementScope = _scopeManager.CurrentElementScope;
				if (Ref.Equal(builderInfo.LocalName, _atoms.Lang))
				{
					currentElementScope.Lang = builderInfo.Value;
				}
				else if (Ref.Equal(builderInfo.LocalName, _atoms.Space))
				{
					currentElementScope.Space = TranslateXmlSpace(builderInfo.Value);
				}
			}
		}
	}

	private void FixupElement()
	{
		if (Ref.Equal(_mainNode.NamespaceURI, _atoms.Empty))
		{
			_mainNode.Prefix = _atoms.Empty;
		}
		if (Ref.Equal(_mainNode.Prefix, _atoms.Empty))
		{
			if (!Ref.Equal(_mainNode.NamespaceURI, _scopeManager.DefaultNamespace))
			{
				DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
			}
		}
		else
		{
			bool thisScope = false;
			string text = _scopeManager.ResolveNamespace(_mainNode.Prefix, out thisScope);
			if (text != null)
			{
				if (!Ref.Equal(_mainNode.NamespaceURI, text))
				{
					if (thisScope)
					{
						_mainNode.Prefix = GetPrefixForNamespace(_mainNode.NamespaceURI);
					}
					else
					{
						DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
					}
				}
			}
			else
			{
				DeclareNamespace(_mainNode.NamespaceURI, _mainNode.Prefix);
			}
		}
		OutputScope currentElementScope = _scopeManager.CurrentElementScope;
		currentElementScope.Prefix = _mainNode.Prefix;
	}

	private void FixupAttributes(int attributeCount)
	{
		for (int i = 0; i < attributeCount; i++)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[i];
			if (Ref.Equal(builderInfo.NamespaceURI, _atoms.Empty))
			{
				builderInfo.Prefix = _atoms.Empty;
				continue;
			}
			if (Ref.Equal(builderInfo.Prefix, _atoms.Empty))
			{
				builderInfo.Prefix = GetPrefixForNamespace(builderInfo.NamespaceURI);
				continue;
			}
			bool thisScope = false;
			string text = _scopeManager.ResolveNamespace(builderInfo.Prefix, out thisScope);
			if (text != null)
			{
				if (!Ref.Equal(builderInfo.NamespaceURI, text))
				{
					if (thisScope)
					{
						builderInfo.Prefix = GetPrefixForNamespace(builderInfo.NamespaceURI);
					}
					else
					{
						DeclareNamespace(builderInfo.NamespaceURI, builderInfo.Prefix);
					}
				}
			}
			else
			{
				DeclareNamespace(builderInfo.NamespaceURI, builderInfo.Prefix);
			}
		}
	}

	private void AppendNamespaces()
	{
		for (int num = _namespaceCount - 1; num >= 0; num--)
		{
			BuilderInfo builderInfo = (BuilderInfo)_attributeList[NewAttribute()];
			builderInfo.Initialize((BuilderInfo)_namespaceList[num]);
		}
	}

	private void AnalyzeComment()
	{
		StringBuilder stringBuilder = null;
		string value = _mainNode.Value;
		bool flag = false;
		int i = 0;
		int num = 0;
		for (; i < value.Length; i++)
		{
			char c = value[i];
			if (c == '-')
			{
				if (flag)
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(value, num, i, 2 * value.Length);
					}
					else
					{
						stringBuilder.Append(value, num, i - num);
					}
					stringBuilder.Append(" -");
					num = i + 1;
				}
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (stringBuilder != null)
		{
			if (num < value.Length)
			{
				stringBuilder.Append(value, num, value.Length - num);
			}
			if (flag)
			{
				stringBuilder.Append(' ');
			}
			_mainNode.Value = stringBuilder.ToString();
		}
		else if (flag)
		{
			_mainNode.ValueAppend(" ", disableEscaping: false);
		}
	}

	private void AnalyzeProcessingInstruction()
	{
		StringBuilder stringBuilder = null;
		string value = _mainNode.Value;
		bool flag = false;
		int i = 0;
		int num = 0;
		for (; i < value.Length; i++)
		{
			switch (value[i])
			{
			case '?':
				flag = true;
				break;
			case '>':
				if (flag)
				{
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder(value, num, i, 2 * value.Length);
					}
					else
					{
						stringBuilder.Append(value, num, i - num);
					}
					stringBuilder.Append(" >");
					num = i + 1;
				}
				flag = false;
				break;
			default:
				flag = false;
				break;
			}
		}
		if (stringBuilder != null)
		{
			if (num < value.Length)
			{
				stringBuilder.Append(value, num, value.Length - num);
			}
			_mainNode.Value = stringBuilder.ToString();
		}
	}

	private void FinalizeRecord()
	{
		switch (_mainNode.NodeType)
		{
		case XmlNodeType.Element:
		{
			int attributeCount = _attributeCount;
			FixupElement();
			FixupAttributes(attributeCount);
			AnalyzeSpaceLang();
			AppendNamespaces();
			break;
		}
		case XmlNodeType.Comment:
			AnalyzeComment();
			break;
		case XmlNodeType.ProcessingInstruction:
			AnalyzeProcessingInstruction();
			break;
		}
	}

	private int NewNamespace()
	{
		if (_namespaceCount >= _namespaceList.Count)
		{
			_namespaceList.Add(new BuilderInfo());
		}
		return _namespaceCount++;
	}

	private void DeclareNamespace(string nspace, string prefix)
	{
		int index = NewNamespace();
		BuilderInfo builderInfo = (BuilderInfo)_namespaceList[index];
		if (prefix == _atoms.Empty)
		{
			builderInfo.Initialize(_atoms.Empty, _atoms.Xmlns, _atoms.XmlnsNamespace);
		}
		else
		{
			builderInfo.Initialize(_atoms.Xmlns, prefix, _atoms.XmlnsNamespace);
		}
		builderInfo.Depth = _recordDepth;
		builderInfo.NodeType = XmlNodeType.Attribute;
		builderInfo.Value = nspace;
		_scopeManager.PushNamespace(prefix, nspace);
	}

	private string DeclareNewNamespace(string nspace)
	{
		string text = _scopeManager.GeneratePrefix("xp_{0}");
		DeclareNamespace(nspace, text);
		return text;
	}

	internal string GetPrefixForNamespace(string nspace)
	{
		string prefix = null;
		if (_scopeManager.FindPrefix(nspace, out prefix))
		{
			return prefix;
		}
		return DeclareNewNamespace(nspace);
	}

	private static XmlSpace TranslateXmlSpace(string space)
	{
		if (space == "default")
		{
			return XmlSpace.Default;
		}
		if (space == "preserve")
		{
			return XmlSpace.Preserve;
		}
		return XmlSpace.None;
	}
}
