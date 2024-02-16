using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using MS.Internal.Xml.Cache;

namespace System.Xml.XPath;

public class XPathDocument : IXPathNavigable
{
	internal enum LoadFlags
	{
		None,
		AtomizeNames,
		Fragment
	}

	private XPathNode[] _pageText;

	private XPathNode[] _pageRoot;

	private XPathNode[] _pageXmlNmsp;

	private int _idxText;

	private int _idxRoot;

	private int _idxXmlNmsp;

	private XmlNameTable _nameTable;

	private bool _hasLineInfo;

	private Dictionary<XPathNodeRef, XPathNodeRef> _mapNmsp;

	private Dictionary<string, XPathNodeRef> _idValueMap;

	internal XmlNameTable NameTable => _nameTable;

	internal bool HasLineInfo => _hasLineInfo;

	internal XPathDocument()
	{
		_nameTable = new NameTable();
	}

	internal XPathDocument(XmlNameTable nameTable)
	{
		if (nameTable == null)
		{
			throw new ArgumentNullException("nameTable");
		}
		_nameTable = nameTable;
	}

	public XPathDocument(XmlReader reader)
		: this(reader, XmlSpace.Default)
	{
	}

	public XPathDocument(XmlReader reader, XmlSpace space)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		LoadFromReader(reader, space);
	}

	public XPathDocument(TextReader textReader)
	{
		XmlTextReaderImpl xmlTextReaderImpl = SetupReader(new XmlTextReaderImpl(string.Empty, textReader));
		try
		{
			LoadFromReader(xmlTextReaderImpl, XmlSpace.Default);
		}
		finally
		{
			xmlTextReaderImpl.Close();
		}
	}

	public XPathDocument(Stream stream)
	{
		XmlTextReaderImpl xmlTextReaderImpl = SetupReader(new XmlTextReaderImpl(string.Empty, stream));
		try
		{
			LoadFromReader(xmlTextReaderImpl, XmlSpace.Default);
		}
		finally
		{
			xmlTextReaderImpl.Close();
		}
	}

	public XPathDocument(string uri)
		: this(uri, XmlSpace.Default)
	{
	}

	public XPathDocument(string uri, XmlSpace space)
	{
		XmlTextReaderImpl xmlTextReaderImpl = SetupReader(new XmlTextReaderImpl(uri));
		try
		{
			LoadFromReader(xmlTextReaderImpl, space);
		}
		finally
		{
			xmlTextReaderImpl.Close();
		}
	}

	internal XmlRawWriter LoadFromWriter(LoadFlags flags, string baseUri)
	{
		return new XPathDocumentBuilder(this, null, baseUri, flags);
	}

	[MemberNotNull("_nameTable")]
	internal void LoadFromReader(XmlReader reader, XmlSpace space)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		IXmlLineInfo xmlLineInfo = reader as IXmlLineInfo;
		if (xmlLineInfo == null || !xmlLineInfo.HasLineInfo())
		{
			xmlLineInfo = null;
		}
		_hasLineInfo = xmlLineInfo != null;
		_nameTable = reader.NameTable;
		XPathDocumentBuilder xPathDocumentBuilder = new XPathDocumentBuilder(this, xmlLineInfo, reader.BaseURI, LoadFlags.None);
		try
		{
			bool flag = reader.ReadState == ReadState.Initial;
			int depth = reader.Depth;
			string text = _nameTable.Get("http://www.w3.org/2000/xmlns/");
			if (flag && !reader.Read())
			{
				return;
			}
			while (flag || reader.Depth >= depth)
			{
				switch (reader.NodeType)
				{
				case XmlNodeType.Element:
				{
					bool isEmptyElement = reader.IsEmptyElement;
					xPathDocumentBuilder.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.BaseURI);
					while (reader.MoveToNextAttribute())
					{
						string namespaceURI = reader.NamespaceURI;
						if ((object)namespaceURI == text)
						{
							if (reader.Prefix.Length == 0)
							{
								xPathDocumentBuilder.WriteNamespaceDeclaration(string.Empty, reader.Value);
							}
							else
							{
								xPathDocumentBuilder.WriteNamespaceDeclaration(reader.LocalName, reader.Value);
							}
						}
						else
						{
							xPathDocumentBuilder.WriteStartAttribute(reader.Prefix, reader.LocalName, namespaceURI);
							xPathDocumentBuilder.WriteString(reader.Value, TextBlockType.Text);
							xPathDocumentBuilder.WriteEndAttribute();
						}
					}
					if (isEmptyElement)
					{
						xPathDocumentBuilder.WriteEndElement(allowShortcutTag: true);
					}
					break;
				}
				case XmlNodeType.EndElement:
					xPathDocumentBuilder.WriteEndElement(allowShortcutTag: false);
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					xPathDocumentBuilder.WriteString(reader.Value, TextBlockType.Text);
					break;
				case XmlNodeType.SignificantWhitespace:
					if (reader.XmlSpace == XmlSpace.Preserve)
					{
						xPathDocumentBuilder.WriteString(reader.Value, TextBlockType.SignificantWhitespace);
						break;
					}
					goto case XmlNodeType.Whitespace;
				case XmlNodeType.Whitespace:
					if (space == XmlSpace.Preserve && (!flag || reader.Depth != 0))
					{
						xPathDocumentBuilder.WriteString(reader.Value, TextBlockType.Whitespace);
					}
					break;
				case XmlNodeType.Comment:
					xPathDocumentBuilder.WriteComment(reader.Value);
					break;
				case XmlNodeType.ProcessingInstruction:
					xPathDocumentBuilder.WriteProcessingInstruction(reader.LocalName, reader.Value, reader.BaseURI);
					break;
				case XmlNodeType.EntityReference:
					reader.ResolveEntity();
					break;
				case XmlNodeType.DocumentType:
				{
					IDtdInfo dtdInfo = reader.DtdInfo;
					if (dtdInfo != null)
					{
						xPathDocumentBuilder.CreateIdTables(dtdInfo);
					}
					break;
				}
				}
				if (!reader.Read())
				{
					break;
				}
			}
		}
		finally
		{
			xPathDocumentBuilder.Close();
		}
	}

	public XPathNavigator CreateNavigator()
	{
		return new XPathDocumentNavigator(_pageRoot, _idxRoot, null, 0);
	}

	internal int GetCollapsedTextNode(out XPathNode[] pageText)
	{
		pageText = _pageText;
		return _idxText;
	}

	internal void SetCollapsedTextNode(XPathNode[] pageText, int idxText)
	{
		_pageText = pageText;
		_idxText = idxText;
	}

	internal int GetRootNode(out XPathNode[] pageRoot)
	{
		pageRoot = _pageRoot;
		return _idxRoot;
	}

	internal void SetRootNode(XPathNode[] pageRoot, int idxRoot)
	{
		_pageRoot = pageRoot;
		_idxRoot = idxRoot;
	}

	internal int GetXmlNamespaceNode(out XPathNode[] pageXmlNmsp)
	{
		pageXmlNmsp = _pageXmlNmsp;
		return _idxXmlNmsp;
	}

	internal void SetXmlNamespaceNode(XPathNode[] pageXmlNmsp, int idxXmlNmsp)
	{
		_pageXmlNmsp = pageXmlNmsp;
		_idxXmlNmsp = idxXmlNmsp;
	}

	internal void AddNamespace(XPathNode[] pageElem, int idxElem, XPathNode[] pageNmsp, int idxNmsp)
	{
		if (_mapNmsp == null)
		{
			_mapNmsp = new Dictionary<XPathNodeRef, XPathNodeRef>();
		}
		_mapNmsp.Add(new XPathNodeRef(pageElem, idxElem), new XPathNodeRef(pageNmsp, idxNmsp));
	}

	internal int LookupNamespaces(XPathNode[] pageElem, int idxElem, out XPathNode[] pageNmsp)
	{
		XPathNodeRef key = new XPathNodeRef(pageElem, idxElem);
		if (_mapNmsp == null || !_mapNmsp.ContainsKey(key))
		{
			pageNmsp = null;
			return 0;
		}
		key = _mapNmsp[key];
		pageNmsp = key.Page;
		return key.Index;
	}

	internal void AddIdElement(string id, XPathNode[] pageElem, int idxElem)
	{
		if (_idValueMap == null)
		{
			_idValueMap = new Dictionary<string, XPathNodeRef>();
		}
		if (!_idValueMap.ContainsKey(id))
		{
			_idValueMap.Add(id, new XPathNodeRef(pageElem, idxElem));
		}
	}

	internal int LookupIdElement(string id, out XPathNode[] pageElem)
	{
		if (_idValueMap == null || !_idValueMap.ContainsKey(id))
		{
			pageElem = null;
			return 0;
		}
		XPathNodeRef xPathNodeRef = _idValueMap[id];
		pageElem = xPathNodeRef.Page;
		return xPathNodeRef.Index;
	}

	private XmlTextReaderImpl SetupReader(XmlTextReaderImpl reader)
	{
		reader.EntityHandling = EntityHandling.ExpandEntities;
		reader.XmlValidatingReaderCompatibilityMode = true;
		return reader;
	}
}
