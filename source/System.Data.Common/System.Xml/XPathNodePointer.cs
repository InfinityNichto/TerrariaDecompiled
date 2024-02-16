using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class XPathNodePointer : IXmlDataVirtualNode
{
	private readonly WeakReference _owner;

	private readonly XmlDataDocument _doc;

	private XmlNode _node;

	private DataColumn _column;

	private bool _fOnValue;

	internal XmlBoundElement _parentOfNS;

	internal static readonly int[] s_xmlNodeType_To_XpathNodeType_Map = CreateXmlNodeTypeToXpathNodeTypeMap();

	private bool _bNeedFoliate;

	internal bool IsEmptyElement
	{
		get
		{
			if (_node != null && _column == null && _node.NodeType == XmlNodeType.Element)
			{
				return ((XmlElement)_node).IsEmpty;
			}
			return false;
		}
	}

	internal XPathNodeType NodeType
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return XPathNodeType.All;
			}
			if (_column == null)
			{
				return ConvertNodeType(_node);
			}
			if (_fOnValue)
			{
				return XPathNodeType.Text;
			}
			if (_column.ColumnMapping == MappingType.Attribute)
			{
				if (_column.Namespace == "http://www.w3.org/2000/xmlns/")
				{
					return XPathNodeType.Namespace;
				}
				return XPathNodeType.Attribute;
			}
			return XPathNodeType.Element;
		}
	}

	internal string LocalName
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return string.Empty;
			}
			if (_column == null)
			{
				XmlNodeType nodeType = _node.NodeType;
				if (IsNamespaceNode(nodeType, _node.NamespaceURI) && _node.LocalName == "xmlns")
				{
					return string.Empty;
				}
				if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.Attribute || nodeType == XmlNodeType.ProcessingInstruction)
				{
					return _node.LocalName;
				}
				return string.Empty;
			}
			if (_fOnValue)
			{
				return string.Empty;
			}
			return _doc.NameTable.Add(_column.EncodedColumnName);
		}
	}

	internal string Name
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return string.Empty;
			}
			if (_column == null)
			{
				XmlNodeType nodeType = _node.NodeType;
				if (IsNamespaceNode(nodeType, _node.NamespaceURI))
				{
					if (_node.LocalName == "xmlns")
					{
						return string.Empty;
					}
					return _node.LocalName;
				}
				if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.Attribute || nodeType == XmlNodeType.ProcessingInstruction)
				{
					return _node.Name;
				}
				return string.Empty;
			}
			if (_fOnValue)
			{
				return string.Empty;
			}
			return _doc.NameTable.Add(_column.EncodedColumnName);
		}
	}

	internal string NamespaceURI
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return string.Empty;
			}
			if (_column == null)
			{
				XPathNodeType xPathNodeType = ConvertNodeType(_node);
				if (xPathNodeType == XPathNodeType.Element || xPathNodeType == XPathNodeType.Root || xPathNodeType == XPathNodeType.Attribute)
				{
					return _node.NamespaceURI;
				}
				return string.Empty;
			}
			if (_fOnValue)
			{
				return string.Empty;
			}
			if (_column.Namespace == "http://www.w3.org/2000/xmlns/")
			{
				return string.Empty;
			}
			return _doc.NameTable.Add(_column.Namespace);
		}
	}

	internal string Prefix
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return string.Empty;
			}
			if (_column == null)
			{
				if (IsNamespaceNode(_node.NodeType, _node.NamespaceURI))
				{
					return string.Empty;
				}
				return _node.Prefix;
			}
			return string.Empty;
		}
	}

	internal string Value
	{
		[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return null;
			}
			if (_column == null)
			{
				string text = _node.Value;
				if (XmlDataDocument.IsTextNode(_node.NodeType))
				{
					XmlNode parentNode = _node.ParentNode;
					if (parentNode == null)
					{
						return text;
					}
					XmlNode xmlNode = _doc.SafeNextSibling(_node);
					while (xmlNode != null && XmlDataDocument.IsTextNode(xmlNode.NodeType))
					{
						text += xmlNode.Value;
						xmlNode = _doc.SafeNextSibling(xmlNode);
					}
				}
				return text;
			}
			if (_column.ColumnMapping == MappingType.Attribute || _fOnValue)
			{
				DataRow row = Row;
				DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
				object value = row[_column, version];
				if (!Convert.IsDBNull(value))
				{
					return _column.ConvertObjectToXml(value);
				}
				return null;
			}
			return null;
		}
	}

	internal string InnerText
	{
		[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return string.Empty;
			}
			if (_column == null)
			{
				if (_node.NodeType == XmlNodeType.Document)
				{
					XmlElement documentElement = ((XmlDocument)_node).DocumentElement;
					if (documentElement != null)
					{
						return documentElement.InnerText;
					}
					return string.Empty;
				}
				return _node.InnerText;
			}
			DataRow row = Row;
			DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
			object value = row[_column, version];
			if (!Convert.IsDBNull(value))
			{
				return _column.ConvertObjectToXml(value);
			}
			return string.Empty;
		}
	}

	internal string BaseURI
	{
		get
		{
			RealFoliate();
			if (_node != null)
			{
				return _node.BaseURI;
			}
			return string.Empty;
		}
	}

	internal string XmlLang
	{
		get
		{
			RealFoliate();
			XmlNode xmlNode = _node;
			XmlBoundElement xmlBoundElement = null;
			object obj = null;
			while (xmlNode != null)
			{
				if (xmlNode is XmlBoundElement xmlBoundElement2)
				{
					if (xmlBoundElement2.ElementState == ElementState.Defoliated)
					{
						DataRow row = xmlBoundElement2.Row;
						foreach (DataColumn column in row.Table.Columns)
						{
							if (column.Prefix == "xml" && column.EncodedColumnName == "lang")
							{
								obj = row[column];
								if (obj == DBNull.Value)
								{
									break;
								}
								return (string)obj;
							}
						}
					}
					else if (xmlBoundElement2.HasAttribute("xml:lang"))
					{
						return xmlBoundElement2.GetAttribute("xml:lang");
					}
				}
				xmlNode = ((xmlNode.NodeType != XmlNodeType.Attribute) ? xmlNode.ParentNode : ((XmlAttribute)xmlNode).OwnerElement);
			}
			return string.Empty;
		}
	}

	private DataRow Row => GetRowElement()?.Row;

	internal int AttributeCount
	{
		get
		{
			RealFoliate();
			if (_node != null && _column == null && _node.NodeType == XmlNodeType.Element)
			{
				if (!IsFoliated(_node))
				{
					return ColumnCount(Row, fAttribute: true);
				}
				int num = 0;
				{
					foreach (XmlAttribute attribute in _node.Attributes)
					{
						if (attribute.NamespaceURI != "http://www.w3.org/2000/xmlns/")
						{
							num++;
						}
					}
					return num;
				}
			}
			return 0;
		}
	}

	internal bool HasChildren
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return false;
			}
			if (_column != null)
			{
				if (_column.ColumnMapping == MappingType.Attribute || _column.ColumnMapping == MappingType.Hidden)
				{
					return false;
				}
				return !_fOnValue;
			}
			if (!IsFoliated(_node))
			{
				DataRow row = Row;
				for (DataColumn dataColumn = NextColumn(row, null, fAttribute: false); dataColumn != null; dataColumn = NextColumn(row, dataColumn, fAttribute: false))
				{
					if (IsValidChild(_node, dataColumn))
					{
						return true;
					}
				}
			}
			for (XmlNode xmlNode = _doc.SafeFirstChild(_node); xmlNode != null; xmlNode = _doc.SafeNextSibling(xmlNode))
			{
				if (IsValidChild(_node, xmlNode))
				{
					return true;
				}
			}
			return false;
		}
	}

	internal XmlNode Node
	{
		[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return null;
			}
			XmlBoundElement rowElement = GetRowElement();
			if (rowElement != null)
			{
				bool isFoliationEnabled = _doc.IsFoliationEnabled;
				_doc.IsFoliationEnabled = true;
				_doc.Foliate(rowElement, ElementState.StrongFoliation);
				_doc.IsFoliationEnabled = isFoliationEnabled;
			}
			RealFoliate();
			return _node;
		}
	}

	internal XmlDataDocument Document => _doc;

	private static int[] CreateXmlNodeTypeToXpathNodeTypeMap()
	{
		return new int[20]
		{
			-1, 1, 2, 4, 4, -1, -1, 7, 8, 0,
			-1, 0, -1, 6, 5, -1, -1, -1, 0, 0
		};
	}

	private XPathNodeType DecideXPNodeTypeForTextNodes(XmlNode node)
	{
		XPathNodeType result = XPathNodeType.Whitespace;
		for (XmlNode xmlNode = node; xmlNode != null; xmlNode = _doc.SafeNextSibling(xmlNode))
		{
			switch (xmlNode.NodeType)
			{
			case XmlNodeType.SignificantWhitespace:
				result = XPathNodeType.SignificantWhitespace;
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				return XPathNodeType.Text;
			default:
				return result;
			case XmlNodeType.Whitespace:
				break;
			}
		}
		return result;
	}

	private XPathNodeType ConvertNodeType(XmlNode node)
	{
		int num = -1;
		if (XmlDataDocument.IsTextNode(node.NodeType))
		{
			return DecideXPNodeTypeForTextNodes(node);
		}
		num = s_xmlNodeType_To_XpathNodeType_Map[(int)node.NodeType];
		if (num == 2)
		{
			if (node.NamespaceURI == "http://www.w3.org/2000/xmlns/")
			{
				return XPathNodeType.Namespace;
			}
			return XPathNodeType.Attribute;
		}
		return (XPathNodeType)num;
	}

	private bool IsNamespaceNode(XmlNodeType nt, string ns)
	{
		if (nt == XmlNodeType.Attribute)
		{
			return ns == "http://www.w3.org/2000/xmlns/";
		}
		return false;
	}

	internal XPathNodePointer(DataDocumentXPathNavigator owner, XmlDataDocument doc, XmlNode node)
		: this(owner, doc, node, null, bOnValue: false, null)
	{
	}

	internal XPathNodePointer(DataDocumentXPathNavigator owner, XPathNodePointer pointer)
		: this(owner, pointer._doc, pointer._node, pointer._column, pointer._fOnValue, pointer._parentOfNS)
	{
	}

	private XPathNodePointer(DataDocumentXPathNavigator owner, XmlDataDocument doc, XmlNode node, DataColumn c, bool bOnValue, XmlBoundElement parentOfNS)
	{
		_owner = new WeakReference(owner);
		_doc = doc;
		_node = node;
		_column = c;
		_fOnValue = bOnValue;
		_parentOfNS = parentOfNS;
		_doc.AddPointer(this);
		_bNeedFoliate = false;
	}

	internal XPathNodePointer Clone(DataDocumentXPathNavigator owner)
	{
		RealFoliate();
		return new XPathNodePointer(owner, this);
	}

	private XmlBoundElement GetRowElement()
	{
		if (_column != null)
		{
			return (XmlBoundElement)_node;
		}
		_doc.Mapper.GetRegion(_node, out var rowElem);
		return rowElem;
	}

	internal bool MoveTo(XPathNodePointer pointer)
	{
		if (_doc != pointer._doc)
		{
			return false;
		}
		_node = pointer._node;
		_column = pointer._column;
		_fOnValue = pointer._fOnValue;
		_bNeedFoliate = pointer._bNeedFoliate;
		return true;
	}

	private void MoveTo(XmlNode node)
	{
		_node = node;
		_column = null;
		_fOnValue = false;
	}

	private void MoveTo(XmlNode node, DataColumn column, bool fOnValue)
	{
		_node = node;
		_column = column;
		_fOnValue = fOnValue;
	}

	private bool IsFoliated(XmlNode node)
	{
		if (node != null && node is XmlBoundElement)
		{
			return ((XmlBoundElement)node).IsFoliated;
		}
		return true;
	}

	private int ColumnCount(DataRow row, bool fAttribute)
	{
		DataColumn dataColumn = null;
		int num = 0;
		while ((dataColumn = NextColumn(row, dataColumn, fAttribute)) != null)
		{
			if (dataColumn.Namespace != "http://www.w3.org/2000/xmlns/")
			{
				num++;
			}
		}
		return num;
	}

	internal DataColumn NextColumn(DataRow row, DataColumn col, bool fAttribute)
	{
		if (row.RowState == DataRowState.Deleted)
		{
			return null;
		}
		DataTable table = row.Table;
		DataColumnCollection columns = table.Columns;
		int i = ((col != null) ? (col.Ordinal + 1) : 0);
		int count = columns.Count;
		DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
		for (; i < count; i++)
		{
			DataColumn dataColumn = columns[i];
			if (!_doc.IsNotMapped(dataColumn) && dataColumn.ColumnMapping == MappingType.Attribute == fAttribute && !Convert.IsDBNull(row[dataColumn, version]))
			{
				return dataColumn;
			}
		}
		return null;
	}

	internal DataColumn PreviousColumn(DataRow row, DataColumn col, bool fAttribute)
	{
		if (row.RowState == DataRowState.Deleted)
		{
			return null;
		}
		DataTable table = row.Table;
		DataColumnCollection columns = table.Columns;
		int num = ((col != null) ? (col.Ordinal - 1) : (columns.Count - 1));
		DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
		while (num >= 0)
		{
			DataColumn dataColumn = columns[num];
			if (!_doc.IsNotMapped(dataColumn) && dataColumn.ColumnMapping == MappingType.Attribute == fAttribute && !Convert.IsDBNull(row[dataColumn, version]))
			{
				return dataColumn;
			}
			num--;
		}
		return null;
	}

	internal bool MoveToAttribute(string localName, string namespaceURI)
	{
		RealFoliate();
		if (namespaceURI == "http://www.w3.org/2000/xmlns/")
		{
			return false;
		}
		if (_node != null && (_column == null || _column.ColumnMapping == MappingType.Attribute) && _node.NodeType == XmlNodeType.Element)
		{
			if (!IsFoliated(_node))
			{
				DataColumn dataColumn = null;
				while ((dataColumn = NextColumn(Row, dataColumn, fAttribute: true)) != null)
				{
					if (dataColumn.EncodedColumnName == localName && dataColumn.Namespace == namespaceURI)
					{
						MoveTo(_node, dataColumn, fOnValue: false);
						return true;
					}
				}
			}
			else
			{
				XmlNode namedItem = _node.Attributes.GetNamedItem(localName, namespaceURI);
				if (namedItem != null)
				{
					MoveTo(namedItem, null, fOnValue: false);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToNextAttribute(bool bFirst)
	{
		RealFoliate();
		if (_node != null)
		{
			if (bFirst && (_column != null || _node.NodeType != XmlNodeType.Element))
			{
				return false;
			}
			if (!bFirst)
			{
				if (_column != null && _column.ColumnMapping != MappingType.Attribute)
				{
					return false;
				}
				if (_column == null && _node.NodeType != XmlNodeType.Attribute)
				{
					return false;
				}
			}
			if (!IsFoliated(_node))
			{
				DataColumn dataColumn = _column;
				while ((dataColumn = NextColumn(Row, dataColumn, fAttribute: true)) != null)
				{
					if (dataColumn.Namespace != "http://www.w3.org/2000/xmlns/")
					{
						MoveTo(_node, dataColumn, fOnValue: false);
						return true;
					}
				}
				return false;
			}
			if (bFirst)
			{
				XmlAttributeCollection attributes = _node.Attributes;
				foreach (XmlAttribute item in attributes)
				{
					if (item.NamespaceURI != "http://www.w3.org/2000/xmlns/")
					{
						MoveTo(item, null, fOnValue: false);
						return true;
					}
				}
			}
			else
			{
				XmlAttributeCollection attributes2 = ((XmlAttribute)_node).OwnerElement.Attributes;
				bool flag = false;
				foreach (XmlAttribute item2 in attributes2)
				{
					if (flag && item2.NamespaceURI != "http://www.w3.org/2000/xmlns/")
					{
						MoveTo(item2, null, fOnValue: false);
						return true;
					}
					if (item2 == _node)
					{
						flag = true;
					}
				}
			}
		}
		return false;
	}

	private bool IsValidChild(XmlNode parent, XmlNode child)
	{
		int num = s_xmlNodeType_To_XpathNodeType_Map[(int)child.NodeType];
		if (num == -1)
		{
			return false;
		}
		return s_xmlNodeType_To_XpathNodeType_Map[(int)parent.NodeType] switch
		{
			0 => num == 1 || num == 8 || num == 7, 
			1 => num == 1 || num == 4 || num == 8 || num == 6 || num == 5 || num == 7, 
			_ => false, 
		};
	}

	private bool IsValidChild(XmlNode parent, DataColumn c)
	{
		return s_xmlNodeType_To_XpathNodeType_Map[(int)parent.NodeType] switch
		{
			0 => c.ColumnMapping == MappingType.Element, 
			1 => c.ColumnMapping == MappingType.Element || c.ColumnMapping == MappingType.SimpleContent, 
			_ => false, 
		};
	}

	internal bool MoveToNextSibling()
	{
		RealFoliate();
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue)
				{
					return false;
				}
				DataRow row = Row;
				for (DataColumn dataColumn = NextColumn(row, _column, fAttribute: false); dataColumn != null; dataColumn = NextColumn(row, dataColumn, fAttribute: false))
				{
					if (IsValidChild(_node, dataColumn))
					{
						MoveTo(_node, dataColumn, _doc.IsTextOnly(dataColumn));
						return true;
					}
				}
				XmlNode xmlNode = _doc.SafeFirstChild(_node);
				if (xmlNode != null)
				{
					MoveTo(xmlNode);
					return true;
				}
			}
			else
			{
				XmlNode xmlNode2 = _node;
				XmlNode parentNode = _node.ParentNode;
				if (parentNode == null)
				{
					return false;
				}
				bool flag = XmlDataDocument.IsTextNode(_node.NodeType);
				do
				{
					xmlNode2 = _doc.SafeNextSibling(xmlNode2);
				}
				while ((xmlNode2 != null && flag && XmlDataDocument.IsTextNode(xmlNode2.NodeType)) || (xmlNode2 != null && !IsValidChild(parentNode, xmlNode2)));
				if (xmlNode2 != null)
				{
					MoveTo(xmlNode2);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToPreviousSibling()
	{
		RealFoliate();
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue)
				{
					return false;
				}
				DataRow row = Row;
				for (DataColumn dataColumn = PreviousColumn(row, _column, fAttribute: false); dataColumn != null; dataColumn = PreviousColumn(row, dataColumn, fAttribute: false))
				{
					if (IsValidChild(_node, dataColumn))
					{
						MoveTo(_node, dataColumn, _doc.IsTextOnly(dataColumn));
						return true;
					}
				}
			}
			else
			{
				XmlNode xmlNode = _node;
				XmlNode parentNode = _node.ParentNode;
				if (parentNode == null)
				{
					return false;
				}
				bool flag = XmlDataDocument.IsTextNode(_node.NodeType);
				do
				{
					xmlNode = _doc.SafePreviousSibling(xmlNode);
				}
				while ((xmlNode != null && flag && XmlDataDocument.IsTextNode(xmlNode.NodeType)) || (xmlNode != null && !IsValidChild(parentNode, xmlNode)));
				if (xmlNode != null)
				{
					MoveTo(xmlNode);
					return true;
				}
				if (!IsFoliated(parentNode) && parentNode is XmlBoundElement)
				{
					DataRow row2 = ((XmlBoundElement)parentNode).Row;
					if (row2 != null)
					{
						DataColumn dataColumn2 = PreviousColumn(row2, null, fAttribute: false);
						if (dataColumn2 != null)
						{
							MoveTo(parentNode, dataColumn2, _doc.IsTextOnly(dataColumn2));
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	internal bool MoveToFirst()
	{
		RealFoliate();
		if (_node != null)
		{
			DataRow dataRow = null;
			XmlNode xmlNode = null;
			if (_column != null)
			{
				dataRow = Row;
				xmlNode = _node;
			}
			else
			{
				xmlNode = _node.ParentNode;
				if (xmlNode == null)
				{
					return false;
				}
				if (!IsFoliated(xmlNode) && xmlNode is XmlBoundElement)
				{
					dataRow = ((XmlBoundElement)xmlNode).Row;
				}
			}
			if (dataRow != null)
			{
				for (DataColumn dataColumn = NextColumn(dataRow, null, fAttribute: false); dataColumn != null; dataColumn = NextColumn(dataRow, dataColumn, fAttribute: false))
				{
					if (IsValidChild(_node, dataColumn))
					{
						MoveTo(_node, dataColumn, _doc.IsTextOnly(dataColumn));
						return true;
					}
				}
			}
			for (XmlNode xmlNode2 = _doc.SafeFirstChild(xmlNode); xmlNode2 != null; xmlNode2 = _doc.SafeNextSibling(xmlNode2))
			{
				if (IsValidChild(xmlNode, xmlNode2))
				{
					MoveTo(xmlNode2);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToFirstChild()
	{
		RealFoliate();
		if (_node == null)
		{
			return false;
		}
		if (_column != null)
		{
			if (_column.ColumnMapping == MappingType.Attribute || _column.ColumnMapping == MappingType.Hidden)
			{
				return false;
			}
			if (_fOnValue)
			{
				return false;
			}
			_fOnValue = true;
			return true;
		}
		if (!IsFoliated(_node))
		{
			DataRow row = Row;
			for (DataColumn dataColumn = NextColumn(row, null, fAttribute: false); dataColumn != null; dataColumn = NextColumn(row, dataColumn, fAttribute: false))
			{
				if (IsValidChild(_node, dataColumn))
				{
					MoveTo(_node, dataColumn, _doc.IsTextOnly(dataColumn));
					return true;
				}
			}
		}
		for (XmlNode xmlNode = _doc.SafeFirstChild(_node); xmlNode != null; xmlNode = _doc.SafeNextSibling(xmlNode))
		{
			if (IsValidChild(_node, xmlNode))
			{
				MoveTo(xmlNode);
				return true;
			}
		}
		return false;
	}

	internal bool MoveToParent()
	{
		RealFoliate();
		if (NodeType == XPathNodeType.Namespace)
		{
			MoveTo(_parentOfNS);
			return true;
		}
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue && !_doc.IsTextOnly(_column))
				{
					MoveTo(_node, _column, fOnValue: false);
					return true;
				}
				MoveTo(_node, null, fOnValue: false);
				return true;
			}
			XmlNode xmlNode = null;
			xmlNode = ((_node.NodeType != XmlNodeType.Attribute) ? _node.ParentNode : ((XmlAttribute)_node).OwnerElement);
			if (xmlNode != null)
			{
				MoveTo(xmlNode);
				return true;
			}
		}
		return false;
	}

	private XmlNode GetParent(XmlNode node)
	{
		return ConvertNodeType(node) switch
		{
			XPathNodeType.Namespace => _parentOfNS, 
			XPathNodeType.Attribute => ((XmlAttribute)node).OwnerElement, 
			_ => node.ParentNode, 
		};
	}

	internal void MoveToRoot()
	{
		XmlNode node = _node;
		for (XmlNode xmlNode = _node; xmlNode != null; xmlNode = GetParent(xmlNode))
		{
			node = xmlNode;
		}
		_node = node;
		_column = null;
		_fOnValue = false;
	}

	internal bool IsSamePosition(XPathNodePointer pointer)
	{
		RealFoliate();
		pointer.RealFoliate();
		if (_column == null && pointer._column == null)
		{
			if (pointer._node == _node)
			{
				return pointer._parentOfNS == _parentOfNS;
			}
			return false;
		}
		if (pointer._doc == _doc && pointer._node == _node && pointer._column == _column && pointer._fOnValue == _fOnValue)
		{
			return pointer._parentOfNS == _parentOfNS;
		}
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlNodeOrder CompareNamespacePosition(XPathNodePointer other)
	{
		XPathNodePointer xPathNodePointer = Clone((DataDocumentXPathNavigator)_owner.Target);
		XPathNodePointer pointer = other.Clone((DataDocumentXPathNavigator)other._owner.Target);
		while (xPathNodePointer.MoveToNextNamespace(XPathNamespaceScope.All))
		{
			if (xPathNodePointer.IsSamePosition(pointer))
			{
				return XmlNodeOrder.Before;
			}
		}
		return XmlNodeOrder.After;
	}

	private static XmlNode GetRoot(XmlNode node, ref int depth)
	{
		depth = 0;
		XmlNode xmlNode = node;
		XmlNode xmlNode2 = ((xmlNode.NodeType == XmlNodeType.Attribute) ? ((XmlAttribute)xmlNode).OwnerElement : xmlNode.ParentNode);
		while (xmlNode2 != null)
		{
			xmlNode = xmlNode2;
			xmlNode2 = xmlNode.ParentNode;
			depth++;
		}
		return xmlNode;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlNodeOrder ComparePosition(XPathNodePointer other)
	{
		RealFoliate();
		other.RealFoliate();
		if (IsSamePosition(other))
		{
			return XmlNodeOrder.Same;
		}
		XmlNode xmlNode = null;
		XmlNode xmlNode2 = null;
		if (NodeType == XPathNodeType.Namespace && other.NodeType == XPathNodeType.Namespace)
		{
			if (_parentOfNS == other._parentOfNS)
			{
				return CompareNamespacePosition(other);
			}
			xmlNode = _parentOfNS;
			xmlNode2 = other._parentOfNS;
		}
		else if (NodeType == XPathNodeType.Namespace)
		{
			if (_parentOfNS == other._node)
			{
				if (other._column == null)
				{
					return XmlNodeOrder.After;
				}
				return XmlNodeOrder.Before;
			}
			xmlNode = _parentOfNS;
			xmlNode2 = other._node;
		}
		else if (other.NodeType == XPathNodeType.Namespace)
		{
			if (_node == other._parentOfNS)
			{
				if (_column == null)
				{
					return XmlNodeOrder.Before;
				}
				return XmlNodeOrder.After;
			}
			xmlNode = _node;
			xmlNode2 = other._parentOfNS;
		}
		else
		{
			if (_node == other._node)
			{
				if (_column == other._column)
				{
					if (_fOnValue)
					{
						return XmlNodeOrder.After;
					}
					return XmlNodeOrder.Before;
				}
				if (_column == null)
				{
					return XmlNodeOrder.Before;
				}
				if (other._column == null)
				{
					return XmlNodeOrder.After;
				}
				if (_column.Ordinal < other._column.Ordinal)
				{
					return XmlNodeOrder.Before;
				}
				return XmlNodeOrder.After;
			}
			xmlNode = _node;
			xmlNode2 = other._node;
		}
		if (xmlNode == null || xmlNode2 == null)
		{
			return XmlNodeOrder.Unknown;
		}
		int depth = -1;
		int depth2 = -1;
		XmlNode root = GetRoot(xmlNode, ref depth);
		XmlNode root2 = GetRoot(xmlNode2, ref depth2);
		if (root != root2)
		{
			return XmlNodeOrder.Unknown;
		}
		if (depth > depth2)
		{
			while (xmlNode != null && depth > depth2)
			{
				xmlNode = ((xmlNode.NodeType == XmlNodeType.Attribute) ? ((XmlAttribute)xmlNode).OwnerElement : xmlNode.ParentNode);
				depth--;
			}
			if (xmlNode == xmlNode2)
			{
				return XmlNodeOrder.After;
			}
		}
		else if (depth2 > depth)
		{
			while (xmlNode2 != null && depth2 > depth)
			{
				xmlNode2 = ((xmlNode2.NodeType == XmlNodeType.Attribute) ? ((XmlAttribute)xmlNode2).OwnerElement : xmlNode2.ParentNode);
				depth2--;
			}
			if (xmlNode == xmlNode2)
			{
				return XmlNodeOrder.Before;
			}
		}
		XmlNode xmlNode3 = GetParent(xmlNode);
		XmlNode xmlNode4 = GetParent(xmlNode2);
		XmlNode xmlNode5 = null;
		while (xmlNode3 != null && xmlNode4 != null)
		{
			if (xmlNode3 == xmlNode4)
			{
				while (xmlNode != null)
				{
					xmlNode5 = xmlNode.NextSibling;
					if (xmlNode5 == xmlNode2)
					{
						return XmlNodeOrder.Before;
					}
					xmlNode = xmlNode5;
				}
				return XmlNodeOrder.After;
			}
			xmlNode = xmlNode3;
			xmlNode2 = xmlNode4;
			xmlNode3 = xmlNode.ParentNode;
			xmlNode4 = xmlNode2.ParentNode;
		}
		return XmlNodeOrder.Unknown;
	}

	bool IXmlDataVirtualNode.IsOnNode(XmlNode nodeToCheck)
	{
		RealFoliate();
		return nodeToCheck == _node;
	}

	void IXmlDataVirtualNode.OnFoliated(XmlNode foliatedNode)
	{
		if (_node == foliatedNode && _column != null)
		{
			_bNeedFoliate = true;
		}
	}

	private void RealFoliate()
	{
		if (!_bNeedFoliate)
		{
			return;
		}
		_bNeedFoliate = false;
		XmlNode xmlNode = null;
		if (_doc.IsTextOnly(_column))
		{
			xmlNode = _node.FirstChild;
		}
		else
		{
			if (_column.ColumnMapping == MappingType.Attribute)
			{
				xmlNode = _node.Attributes.GetNamedItem(_column.EncodedColumnName, _column.Namespace);
			}
			else
			{
				xmlNode = _node.FirstChild;
				while (xmlNode != null && (!(xmlNode.LocalName == _column.EncodedColumnName) || !(xmlNode.NamespaceURI == _column.Namespace)))
				{
					xmlNode = xmlNode.NextSibling;
				}
			}
			if (xmlNode != null && _fOnValue)
			{
				xmlNode = xmlNode.FirstChild;
			}
		}
		if (xmlNode == null)
		{
			throw new InvalidOperationException(System.SR.DataDom_Foliation);
		}
		_node = xmlNode;
		_column = null;
		_fOnValue = false;
		_bNeedFoliate = false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private string GetNamespace(XmlBoundElement be, string name)
	{
		if (be == null)
		{
			return null;
		}
		XmlAttribute xmlAttribute = null;
		if (be.IsFoliated)
		{
			return be.GetAttributeNode(name, "http://www.w3.org/2000/xmlns/")?.Value;
		}
		DataRow row = be.Row;
		if (row == null)
		{
			return null;
		}
		for (DataColumn dataColumn = PreviousColumn(row, null, fAttribute: true); dataColumn != null; dataColumn = PreviousColumn(row, dataColumn, fAttribute: true))
		{
			if (dataColumn.Namespace == "http://www.w3.org/2000/xmlns/")
			{
				DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
				return dataColumn.ConvertObjectToXml(row[dataColumn, version]);
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal string GetNamespace(string name)
	{
		switch (name)
		{
		case "xml":
			return "http://www.w3.org/XML/1998/namespace";
		case "xmlns":
			return "http://www.w3.org/2000/xmlns/";
		default:
			if (name.Length == 0)
			{
				name = "xmlns";
			}
			break;
		case null:
			break;
		}
		RealFoliate();
		XmlNode xmlNode = _node;
		XmlNodeType nodeType = xmlNode.NodeType;
		string text = null;
		while (xmlNode != null)
		{
			while (xmlNode != null && (nodeType = xmlNode.NodeType) != XmlNodeType.Element)
			{
				xmlNode = ((nodeType != XmlNodeType.Attribute) ? xmlNode.ParentNode : ((XmlAttribute)xmlNode).OwnerElement);
			}
			if (xmlNode != null)
			{
				text = GetNamespace((XmlBoundElement)xmlNode, name);
				if (text != null)
				{
					return text;
				}
				xmlNode = xmlNode.ParentNode;
			}
		}
		return string.Empty;
	}

	internal bool MoveToNamespace(string name)
	{
		_parentOfNS = _node as XmlBoundElement;
		if (_parentOfNS == null)
		{
			return false;
		}
		string text = name;
		if (text == "xmlns")
		{
			text = "xmlns:xmlns";
		}
		if (text != null && text.Length == 0)
		{
			text = "xmlns";
		}
		RealFoliate();
		XmlNode xmlNode = _node;
		XmlAttribute xmlAttribute = null;
		XmlBoundElement xmlBoundElement = null;
		while (xmlNode != null)
		{
			if (xmlNode is XmlBoundElement xmlBoundElement2)
			{
				if (xmlBoundElement2.IsFoliated)
				{
					xmlAttribute = xmlBoundElement2.GetAttributeNode(name, "http://www.w3.org/2000/xmlns/");
					if (xmlAttribute != null)
					{
						MoveTo(xmlAttribute);
						return true;
					}
				}
				else
				{
					DataRow row = xmlBoundElement2.Row;
					if (row == null)
					{
						return false;
					}
					for (DataColumn dataColumn = PreviousColumn(row, null, fAttribute: true); dataColumn != null; dataColumn = PreviousColumn(row, dataColumn, fAttribute: true))
					{
						if (dataColumn.Namespace == "http://www.w3.org/2000/xmlns/" && dataColumn.ColumnName == name)
						{
							MoveTo(xmlBoundElement2, dataColumn, fOnValue: false);
							return true;
						}
					}
				}
			}
			do
			{
				xmlNode = xmlNode.ParentNode;
			}
			while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element);
		}
		_parentOfNS = null;
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private bool MoveToNextNamespace(XmlBoundElement be, DataColumn col, XmlAttribute curAttr)
	{
		if (be != null)
		{
			if (be.IsFoliated)
			{
				XmlAttributeCollection attributes = be.Attributes;
				XmlAttribute xmlAttribute = null;
				bool flag = false;
				if (curAttr == null)
				{
					flag = true;
				}
				int num = attributes.Count;
				while (num > 0)
				{
					num--;
					xmlAttribute = attributes[num];
					if (flag && xmlAttribute.NamespaceURI == "http://www.w3.org/2000/xmlns/" && !DuplicateNS(be, xmlAttribute.LocalName))
					{
						MoveTo(xmlAttribute);
						return true;
					}
					if (xmlAttribute == curAttr)
					{
						flag = true;
					}
				}
			}
			else
			{
				DataRow row = be.Row;
				if (row == null)
				{
					return false;
				}
				for (DataColumn dataColumn = PreviousColumn(row, col, fAttribute: true); dataColumn != null; dataColumn = PreviousColumn(row, dataColumn, fAttribute: true))
				{
					if (dataColumn.Namespace == "http://www.w3.org/2000/xmlns/" && !DuplicateNS(be, dataColumn.ColumnName))
					{
						MoveTo(be, dataColumn, fOnValue: false);
						return true;
					}
				}
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
	{
		RealFoliate();
		_parentOfNS = _node as XmlBoundElement;
		if (_parentOfNS == null)
		{
			return false;
		}
		XmlNode xmlNode = _node;
		XmlBoundElement xmlBoundElement = null;
		while (true)
		{
			if (xmlNode != null)
			{
				xmlBoundElement = xmlNode as XmlBoundElement;
				if (MoveToNextNamespace(xmlBoundElement, null, null))
				{
					return true;
				}
				if (namespaceScope == XPathNamespaceScope.Local)
				{
					break;
				}
				do
				{
					xmlNode = xmlNode.ParentNode;
				}
				while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element);
				continue;
			}
			if (namespaceScope != 0)
			{
				break;
			}
			MoveTo(_doc._attrXml, null, fOnValue: false);
			return true;
		}
		_parentOfNS = null;
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private bool DuplicateNS(XmlBoundElement endElem, string lname)
	{
		if (_parentOfNS == null || endElem == null)
		{
			return false;
		}
		XmlBoundElement xmlBoundElement = _parentOfNS;
		XmlNode xmlNode = null;
		while (xmlBoundElement != null && xmlBoundElement != endElem)
		{
			if (GetNamespace(xmlBoundElement, lname) != null)
			{
				return true;
			}
			xmlNode = xmlBoundElement;
			do
			{
				xmlNode = xmlNode.ParentNode;
			}
			while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element);
			xmlBoundElement = xmlNode as XmlBoundElement;
		}
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
	{
		RealFoliate();
		XmlNode xmlNode = _node;
		if (_column != null)
		{
			if (namespaceScope == XPathNamespaceScope.Local && _parentOfNS != _node)
			{
				return false;
			}
			XmlBoundElement xmlBoundElement = _node as XmlBoundElement;
			DataRow row = xmlBoundElement.Row;
			for (DataColumn dataColumn = PreviousColumn(row, _column, fAttribute: true); dataColumn != null; dataColumn = PreviousColumn(row, dataColumn, fAttribute: true))
			{
				if (dataColumn.Namespace == "http://www.w3.org/2000/xmlns/")
				{
					MoveTo(xmlBoundElement, dataColumn, fOnValue: false);
					return true;
				}
			}
			if (namespaceScope == XPathNamespaceScope.Local)
			{
				return false;
			}
			do
			{
				xmlNode = xmlNode.ParentNode;
			}
			while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element);
		}
		else if (_node.NodeType == XmlNodeType.Attribute)
		{
			XmlAttribute xmlAttribute = (XmlAttribute)_node;
			xmlNode = xmlAttribute.OwnerElement;
			if (xmlNode == null)
			{
				return false;
			}
			if (namespaceScope == XPathNamespaceScope.Local && _parentOfNS != xmlNode)
			{
				return false;
			}
			if (MoveToNextNamespace((XmlBoundElement)xmlNode, null, xmlAttribute))
			{
				return true;
			}
			if (namespaceScope == XPathNamespaceScope.Local)
			{
				return false;
			}
			do
			{
				xmlNode = xmlNode.ParentNode;
			}
			while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element);
		}
		while (xmlNode != null)
		{
			XmlBoundElement be = xmlNode as XmlBoundElement;
			if (MoveToNextNamespace(be, null, null))
			{
				return true;
			}
			do
			{
				xmlNode = xmlNode.ParentNode;
			}
			while (xmlNode != null && xmlNode.NodeType == XmlNodeType.Element);
		}
		if (namespaceScope == XPathNamespaceScope.All)
		{
			MoveTo(_doc._attrXml, null, fOnValue: false);
			return true;
		}
		return false;
	}

	bool IXmlDataVirtualNode.IsInUse()
	{
		return _owner.IsAlive;
	}
}
