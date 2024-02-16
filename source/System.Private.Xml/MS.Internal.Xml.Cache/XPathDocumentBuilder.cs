using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.Cache;

internal sealed class XPathDocumentBuilder : XmlRawWriter
{
	private struct NodePageFactory
	{
		private XPathNode[] _page;

		private XPathNodePageInfo _pageInfo;

		private int _pageSize;

		public XPathNode[] NextNodePage => _page;

		public int NextNodeIndex => _pageInfo.NodeCount;

		public void Init(int initialPageSize)
		{
			_pageSize = initialPageSize;
			_page = new XPathNode[_pageSize];
			_pageInfo = new XPathNodePageInfo(null, 1);
			_page[0].Create(_pageInfo);
		}

		public void AllocateSlot(out XPathNode[] page, out int idx)
		{
			page = _page;
			idx = _pageInfo.NodeCount;
			if (++_pageInfo.NodeCount >= _page.Length)
			{
				if (_pageSize < 65536)
				{
					_pageSize *= 2;
				}
				_page = new XPathNode[_pageSize];
				_pageInfo.NextPage = _page;
				_pageInfo = new XPathNodePageInfo(page, _pageInfo.PageNumber + 1);
				_page[0].Create(_pageInfo);
			}
		}
	}

	private struct TextBlockBuilder
	{
		private IXmlLineInfo _lineInfo;

		private TextBlockType _textType;

		private string _text;

		private int _lineNum;

		private int _linePos;

		public TextBlockType TextType => _textType;

		public bool HasText => _textType != TextBlockType.None;

		public int LineNumber => _lineNum;

		public int LinePosition => _linePos;

		public void Initialize(IXmlLineInfo lineInfo)
		{
			_lineInfo = lineInfo;
			_textType = TextBlockType.None;
		}

		public void WriteTextBlock(string text, TextBlockType textType)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (_textType == TextBlockType.None)
			{
				_text = text;
				_textType = textType;
				if (_lineInfo != null)
				{
					_lineNum = _lineInfo.LineNumber;
					_linePos = _lineInfo.LinePosition;
				}
			}
			else
			{
				_text += text;
				if (textType < _textType)
				{
					_textType = textType;
				}
			}
		}

		public string ReadText()
		{
			if (_textType == TextBlockType.None)
			{
				return string.Empty;
			}
			_textType = TextBlockType.None;
			return _text;
		}
	}

	private NodePageFactory _nodePageFact;

	private NodePageFactory _nmspPageFact;

	private TextBlockBuilder _textBldr;

	private readonly Stack<XPathNodeRef> _stkNmsp;

	private XPathNodeInfoTable _infoTable;

	private XPathDocument _doc;

	private IXmlLineInfo _lineInfo;

	private XmlNameTable _nameTable;

	private bool _atomizeNames;

	private XPathNode[] _pageNmsp;

	private int _idxNmsp;

	private XPathNode[] _pageParent;

	private int _idxParent;

	private XPathNode[] _pageSibling;

	private int _idxSibling;

	private int _lineNumBase;

	private int _linePosBase;

	private XmlQualifiedName _idAttrName;

	private Hashtable _elemIdMap;

	private XPathNodeRef[] _elemNameIndex;

	public XPathDocumentBuilder(XPathDocument doc, IXmlLineInfo lineInfo, string baseUri, XPathDocument.LoadFlags flags)
	{
		_nodePageFact.Init(256);
		_nmspPageFact.Init(16);
		_stkNmsp = new Stack<XPathNodeRef>();
		Initialize(doc, lineInfo, baseUri, flags);
	}

	[MemberNotNull("_doc")]
	[MemberNotNull("_elemNameIndex")]
	[MemberNotNull("_infoTable")]
	[MemberNotNull("_nameTable")]
	[MemberNotNull("_pageNmsp")]
	public void Initialize(XPathDocument doc, IXmlLineInfo lineInfo, string baseUri, XPathDocument.LoadFlags flags)
	{
		_doc = doc;
		_nameTable = doc.NameTable;
		_atomizeNames = (flags & XPathDocument.LoadFlags.AtomizeNames) != 0;
		_idxParent = (_idxSibling = 0);
		_elemNameIndex = new XPathNodeRef[64];
		_textBldr.Initialize(lineInfo);
		_lineInfo = lineInfo;
		_lineNumBase = 0;
		_linePosBase = 0;
		_infoTable = new XPathNodeInfoTable();
		XPathNode[] page;
		int idxText = NewNode(out page, XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
		_doc.SetCollapsedTextNode(page, idxText);
		_idxNmsp = NewNamespaceNode(out _pageNmsp, _nameTable.Add("xml"), _nameTable.Add("http://www.w3.org/XML/1998/namespace"), null, 0);
		_doc.SetXmlNamespaceNode(_pageNmsp, _idxNmsp);
		if ((flags & XPathDocument.LoadFlags.Fragment) == 0)
		{
			_idxParent = NewNode(out _pageParent, XPathNodeType.Root, string.Empty, string.Empty, string.Empty, baseUri);
			_doc.SetRootNode(_pageParent, _idxParent);
		}
		else
		{
			_doc.SetRootNode(_nodePageFact.NextNodePage, _nodePageFact.NextNodeIndex);
		}
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		WriteStartElement(prefix, localName, ns, string.Empty);
	}

	public void WriteStartElement(string prefix, string localName, string ns, string baseUri)
	{
		if (_atomizeNames)
		{
			prefix = _nameTable.Add(prefix);
			localName = _nameTable.Add(localName);
			ns = _nameTable.Add(ns);
		}
		AddSibling(XPathNodeType.Element, localName, ns, prefix, baseUri);
		_pageParent = _pageSibling;
		_idxParent = _idxSibling;
		_idxSibling = 0;
		int num = _pageParent[_idxParent].LocalNameHashCode & 0x3F;
		_elemNameIndex[num] = LinkSimilarElements(_elemNameIndex[num].Page, _elemNameIndex[num].Index, _pageParent, _idxParent);
		if (_elemIdMap != null)
		{
			_idAttrName = (XmlQualifiedName)_elemIdMap[new XmlQualifiedName(localName, prefix)];
		}
	}

	public override void WriteEndElement()
	{
		WriteEndElement(allowShortcutTag: true);
	}

	public override void WriteFullEndElement()
	{
		WriteEndElement(allowShortcutTag: false);
	}

	internal override void WriteEndElement(string prefix, string localName, string namespaceName)
	{
		WriteEndElement(allowShortcutTag: true);
	}

	internal override void WriteFullEndElement(string prefix, string localName, string namespaceName)
	{
		WriteEndElement(allowShortcutTag: false);
	}

	public void WriteEndElement(bool allowShortcutTag)
	{
		if (!_pageParent[_idxParent].HasContentChild)
		{
			TextBlockType textType = _textBldr.TextType;
			if (textType != TextBlockType.Text)
			{
				if ((uint)(textType - 5) > 1u)
				{
					_pageParent[_idxParent].SetEmptyValue(allowShortcutTag);
					goto IL_012d;
				}
			}
			else
			{
				if (_lineInfo == null)
				{
					goto IL_00aa;
				}
				if (_textBldr.LineNumber == _pageParent[_idxParent].LineNumber)
				{
					int num = _textBldr.LinePosition - _pageParent[_idxParent].LinePosition;
					if (num >= 0 && num <= 255)
					{
						_pageParent[_idxParent].SetCollapsedLineInfoOffset(num);
						goto IL_00aa;
					}
				}
			}
			CachedTextNode();
			_pageParent[_idxParent].SetValue(_pageSibling[_idxSibling].Value);
		}
		else if (_textBldr.HasText)
		{
			CachedTextNode();
		}
		goto IL_012d;
		IL_00aa:
		_pageParent[_idxParent].SetCollapsedValue(_textBldr.ReadText());
		goto IL_012d;
		IL_012d:
		if (_pageParent[_idxParent].HasNamespaceDecls)
		{
			_doc.AddNamespace(_pageParent, _idxParent, _pageNmsp, _idxNmsp);
			XPathNodeRef xPathNodeRef = _stkNmsp.Pop();
			_pageNmsp = xPathNodeRef.Page;
			_idxNmsp = xPathNodeRef.Index;
		}
		_pageSibling = _pageParent;
		_idxSibling = _idxParent;
		_idxParent = _pageParent[_idxParent].GetParent(out _pageParent);
	}

	public override void WriteStartAttribute(string prefix, string localName, string namespaceName)
	{
		if (_atomizeNames)
		{
			prefix = _nameTable.Add(prefix);
			localName = _nameTable.Add(localName);
			namespaceName = _nameTable.Add(namespaceName);
		}
		AddSibling(XPathNodeType.Attribute, localName, namespaceName, prefix, string.Empty);
	}

	public override void WriteEndAttribute()
	{
		_pageSibling[_idxSibling].SetValue(_textBldr.ReadText());
		if (_idAttrName != null && _pageSibling[_idxSibling].LocalName == _idAttrName.Name && _pageSibling[_idxSibling].Prefix == _idAttrName.Namespace)
		{
			string value = _pageSibling[_idxSibling].Value;
			_doc.AddIdElement(value, _pageParent, _idxParent);
		}
	}

	public override void WriteCData(string text)
	{
		WriteString(text, TextBlockType.Text);
	}

	public override void WriteComment(string text)
	{
		AddSibling(XPathNodeType.Comment, string.Empty, string.Empty, string.Empty, string.Empty);
		_pageSibling[_idxSibling].SetValue(text);
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		WriteProcessingInstruction(name, text, string.Empty);
	}

	public void WriteProcessingInstruction(string name, string text, string baseUri)
	{
		if (_atomizeNames)
		{
			name = _nameTable.Add(name);
		}
		AddSibling(XPathNodeType.ProcessingInstruction, name, string.Empty, string.Empty, baseUri);
		_pageSibling[_idxSibling].SetValue(text);
	}

	public override void WriteWhitespace(string ws)
	{
		WriteString(ws, TextBlockType.Whitespace);
	}

	public override void WriteString(string text)
	{
		WriteString(text, TextBlockType.Text);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count), TextBlockType.Text);
	}

	public override void WriteRaw(string data)
	{
		WriteString(data, TextBlockType.Text);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		WriteString(new string(buffer, index, count), TextBlockType.Text);
	}

	public void WriteString(string text, TextBlockType textType)
	{
		_textBldr.WriteTextBlock(text, textType);
	}

	public override void WriteEntityRef(string name)
	{
		throw new NotImplementedException();
	}

	public override void WriteCharEntity(char ch)
	{
		WriteString(char.ToString(ch), TextBlockType.Text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		Span<char> span = stackalloc char[2] { highChar, lowChar };
		ReadOnlySpan<char> value = span;
		WriteString(new string(value), TextBlockType.Text);
	}

	public override void Close()
	{
		if (_textBldr.HasText)
		{
			CachedTextNode();
		}
		XPathNode[] pageRoot;
		int rootNode = _doc.GetRootNode(out pageRoot);
		if (rootNode == _nodePageFact.NextNodeIndex && pageRoot == _nodePageFact.NextNodePage)
		{
			AddSibling(XPathNodeType.Text, string.Empty, string.Empty, string.Empty, string.Empty);
			_pageSibling[_idxSibling].SetValue(string.Empty);
		}
	}

	public override void Flush()
	{
	}

	internal override void WriteXmlDeclaration(XmlStandalone standalone)
	{
	}

	internal override void WriteXmlDeclaration(string xmldecl)
	{
	}

	internal override void StartElementContent()
	{
	}

	internal override void WriteNamespaceDeclaration(string prefix, string namespaceName)
	{
		if (_atomizeNames)
		{
			prefix = _nameTable.Add(prefix);
		}
		namespaceName = _nameTable.Add(namespaceName);
		XPathNode[] pageNode = _pageNmsp;
		int num = _idxNmsp;
		while (num != 0 && (object)pageNode[num].LocalName != prefix)
		{
			num = pageNode[num].GetSibling(out pageNode);
		}
		XPathNode[] page;
		int num2 = NewNamespaceNode(out page, prefix, namespaceName, _pageParent, _idxParent);
		if (num != 0)
		{
			XPathNode[] pageNode2 = _pageNmsp;
			int num3 = _idxNmsp;
			XPathNode[] array = page;
			int num4 = num2;
			while (num3 != num || pageNode2 != pageNode)
			{
				int parent = pageNode2[num3].GetParent(out var pageNode3);
				parent = NewNamespaceNode(out pageNode3, pageNode2[num3].LocalName, pageNode2[num3].Value, pageNode3, parent);
				array[num4].SetSibling(_infoTable, pageNode3, parent);
				array = pageNode3;
				num4 = parent;
				num3 = pageNode2[num3].GetSibling(out pageNode2);
			}
			num = pageNode[num].GetSibling(out pageNode);
			if (num != 0)
			{
				array[num4].SetSibling(_infoTable, pageNode, num);
			}
		}
		else if (_idxParent != 0)
		{
			page[num2].SetSibling(_infoTable, _pageNmsp, _idxNmsp);
		}
		else
		{
			_doc.SetRootNode(page, num2);
		}
		if (_idxParent != 0)
		{
			if (!_pageParent[_idxParent].HasNamespaceDecls)
			{
				_stkNmsp.Push(new XPathNodeRef(_pageNmsp, _idxNmsp));
				_pageParent[_idxParent].HasNamespaceDecls = true;
			}
			_pageNmsp = page;
			_idxNmsp = num2;
		}
	}

	public void CreateIdTables(IDtdInfo dtdInfo)
	{
		foreach (IDtdAttributeListInfo attributeList in dtdInfo.GetAttributeLists())
		{
			IDtdAttributeInfo dtdAttributeInfo = attributeList.LookupIdAttribute();
			if (dtdAttributeInfo != null)
			{
				if (_elemIdMap == null)
				{
					_elemIdMap = new Hashtable();
				}
				_elemIdMap.Add(new XmlQualifiedName(attributeList.LocalName, attributeList.Prefix), new XmlQualifiedName(dtdAttributeInfo.LocalName, dtdAttributeInfo.Prefix));
			}
		}
	}

	private XPathNodeRef LinkSimilarElements(XPathNode[] pagePrev, int idxPrev, XPathNode[] pageNext, int idxNext)
	{
		pagePrev?[idxPrev].SetSimilarElement(_infoTable, pageNext, idxNext);
		return new XPathNodeRef(pageNext, idxNext);
	}

	private int NewNamespaceNode(out XPathNode[] page, string prefix, string namespaceUri, XPathNode[] pageElem, int idxElem)
	{
		_nmspPageFact.AllocateSlot(out var page2, out var idx);
		ComputeLineInfo(isTextNode: false, out var lineNumOffset, out var linePosOffset);
		XPathNodeInfoAtom info = _infoTable.Create(prefix, string.Empty, string.Empty, string.Empty, pageElem, page2, null, _doc, _lineNumBase, _linePosBase);
		page2[idx].Create(info, XPathNodeType.Namespace, idxElem);
		page2[idx].SetValue(namespaceUri);
		page2[idx].SetLineInfoOffsets(lineNumOffset, linePosOffset);
		page = page2;
		return idx;
	}

	private int NewNode(out XPathNode[] page, XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string baseUri)
	{
		_nodePageFact.AllocateSlot(out var page2, out var idx);
		ComputeLineInfo(XPathNavigator.IsText(xptyp), out var lineNumOffset, out var linePosOffset);
		XPathNodeInfoAtom info = _infoTable.Create(localName, namespaceUri, prefix, baseUri, _pageParent, page2, page2, _doc, _lineNumBase, _linePosBase);
		page2[idx].Create(info, xptyp, _idxParent);
		page2[idx].SetLineInfoOffsets(lineNumOffset, linePosOffset);
		page = page2;
		return idx;
	}

	private void ComputeLineInfo(bool isTextNode, out int lineNumOffset, out int linePosOffset)
	{
		if (_lineInfo == null)
		{
			lineNumOffset = 0;
			linePosOffset = 0;
			return;
		}
		int lineNumber;
		int linePosition;
		if (isTextNode)
		{
			lineNumber = _textBldr.LineNumber;
			linePosition = _textBldr.LinePosition;
		}
		else
		{
			lineNumber = _lineInfo.LineNumber;
			linePosition = _lineInfo.LinePosition;
		}
		lineNumOffset = lineNumber - _lineNumBase;
		if (lineNumOffset < 0 || lineNumOffset > 16383)
		{
			_lineNumBase = lineNumber;
			lineNumOffset = 0;
		}
		linePosOffset = linePosition - _linePosBase;
		if (linePosOffset < 0 || linePosOffset > 65535)
		{
			_linePosBase = linePosition;
			linePosOffset = 0;
		}
	}

	[MemberNotNull("_pageSibling")]
	private void AddSibling(XPathNodeType xptyp, string localName, string namespaceUri, string prefix, string baseUri)
	{
		if (_textBldr.HasText)
		{
			CachedTextNode();
		}
		XPathNode[] page;
		int idxSibling = NewNode(out page, xptyp, localName, namespaceUri, prefix, baseUri);
		if (_idxParent != 0)
		{
			_pageParent[_idxParent].SetParentProperties(xptyp);
			if (_idxSibling != 0)
			{
				_pageSibling[_idxSibling].SetSibling(_infoTable, page, idxSibling);
			}
		}
		_pageSibling = page;
		_idxSibling = idxSibling;
	}

	[MemberNotNull("_pageSibling")]
	private void CachedTextNode()
	{
		TextBlockType textType = _textBldr.TextType;
		string value = _textBldr.ReadText();
		AddSibling((XPathNodeType)textType, string.Empty, string.Empty, string.Empty, string.Empty);
		_pageSibling[_idxSibling].SetValue(value);
	}
}
