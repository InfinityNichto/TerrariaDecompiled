using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal sealed class DataPointer : IXmlDataVirtualNode
{
	private XmlDataDocument _doc;

	private XmlNode _node;

	private DataColumn _column;

	private bool _fOnValue;

	private bool _bNeedFoliate;

	private bool _isInUse;

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
					return ColumnCount(Row, fAttribute: true, fNulls: false);
				}
				return _node.Attributes.Count;
			}
			return 0;
		}
	}

	internal XmlNodeType NodeType
	{
		get
		{
			RealFoliate();
			if (_node == null)
			{
				return XmlNodeType.None;
			}
			if (_column == null)
			{
				return _node.NodeType;
			}
			if (_fOnValue)
			{
				return XmlNodeType.Text;
			}
			if (_column.ColumnMapping == MappingType.Attribute)
			{
				return XmlNodeType.Attribute;
			}
			return XmlNodeType.Element;
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
				string localName = _node.LocalName;
				if (IsLocalNameEmpty(_node.NodeType))
				{
					return string.Empty;
				}
				return localName;
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
				return _node.NamespaceURI;
			}
			if (_fOnValue)
			{
				return string.Empty;
			}
			return _doc.NameTable.Add(_column.Namespace);
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
				string name = _node.Name;
				if (IsLocalNameEmpty(_node.NodeType))
				{
					return string.Empty;
				}
				return name;
			}
			string prefix = Prefix;
			string localName = LocalName;
			if (prefix != null && prefix.Length > 0)
			{
				if (localName != null && localName.Length > 0)
				{
					return _doc.NameTable.Add(prefix + ":" + localName);
				}
				return prefix;
			}
			return localName;
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
				return _node.Value;
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

	internal bool IsEmptyElement
	{
		get
		{
			RealFoliate();
			if (_node != null && _column == null && _node.NodeType == XmlNodeType.Element)
			{
				return ((XmlElement)_node).IsEmpty;
			}
			return false;
		}
	}

	internal bool IsDefault
	{
		get
		{
			RealFoliate();
			if (_node != null && _column == null && _node.NodeType == XmlNodeType.Attribute)
			{
				return !((XmlAttribute)_node).Specified;
			}
			return false;
		}
	}

	internal string PublicId => NodeType switch
	{
		XmlNodeType.DocumentType => ((XmlDocumentType)_node).PublicId, 
		XmlNodeType.Entity => ((XmlEntity)_node).PublicId, 
		XmlNodeType.Notation => ((XmlNotation)_node).PublicId, 
		_ => null, 
	};

	internal string SystemId => NodeType switch
	{
		XmlNodeType.DocumentType => ((XmlDocumentType)_node).SystemId, 
		XmlNodeType.Entity => ((XmlEntity)_node).SystemId, 
		XmlNodeType.Notation => ((XmlNotation)_node).SystemId, 
		_ => null, 
	};

	internal string InternalSubset
	{
		get
		{
			if (NodeType == XmlNodeType.DocumentType)
			{
				return ((XmlDocumentType)_node).InternalSubset;
			}
			return null;
		}
	}

	internal XmlDeclaration Declaration
	{
		get
		{
			XmlNode xmlNode = _doc.SafeFirstChild(_doc);
			if (xmlNode != null && xmlNode.NodeType == XmlNodeType.XmlDeclaration)
			{
				return (XmlDeclaration)xmlNode;
			}
			return null;
		}
	}

	internal string Encoding
	{
		get
		{
			if (NodeType == XmlNodeType.XmlDeclaration)
			{
				return ((XmlDeclaration)_node).Encoding;
			}
			if (NodeType == XmlNodeType.Document)
			{
				XmlDeclaration declaration = Declaration;
				if (declaration != null)
				{
					return declaration.Encoding;
				}
			}
			return null;
		}
	}

	internal string Standalone
	{
		get
		{
			if (NodeType == XmlNodeType.XmlDeclaration)
			{
				return ((XmlDeclaration)_node).Standalone;
			}
			if (NodeType == XmlNodeType.Document)
			{
				XmlDeclaration declaration = Declaration;
				if (declaration != null)
				{
					return declaration.Standalone;
				}
			}
			return null;
		}
	}

	internal string Version
	{
		get
		{
			if (NodeType == XmlNodeType.XmlDeclaration)
			{
				return ((XmlDeclaration)_node).Version;
			}
			if (NodeType == XmlNodeType.Document)
			{
				XmlDeclaration declaration = Declaration;
				if (declaration != null)
				{
					return declaration.Version;
				}
			}
			return null;
		}
	}

	internal DataPointer(XmlDataDocument doc, XmlNode node)
	{
		_doc = doc;
		_node = node;
		_column = null;
		_fOnValue = false;
		_bNeedFoliate = false;
		_isInUse = true;
	}

	internal DataPointer(DataPointer pointer)
	{
		_doc = pointer._doc;
		_node = pointer._node;
		_column = pointer._column;
		_fOnValue = pointer._fOnValue;
		_bNeedFoliate = false;
		_isInUse = true;
	}

	internal void AddPointer()
	{
		_doc.AddPointer(this);
	}

	private XmlBoundElement GetRowElement()
	{
		if (_column != null)
		{
			return _node as XmlBoundElement;
		}
		_doc.Mapper.GetRegion(_node, out var rowElem);
		return rowElem;
	}

	private static bool IsFoliated(XmlNode node)
	{
		if (node == null || !(node is XmlBoundElement))
		{
			return true;
		}
		return ((XmlBoundElement)node).IsFoliated;
	}

	internal void MoveTo(DataPointer pointer)
	{
		_doc = pointer._doc;
		_node = pointer._node;
		_column = pointer._column;
		_fOnValue = pointer._fOnValue;
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

	private DataColumn NextColumn(DataRow row, DataColumn col, bool fAttribute, bool fNulls)
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
			if (!_doc.IsNotMapped(dataColumn) && dataColumn.ColumnMapping == MappingType.Attribute == fAttribute && (fNulls || !Convert.IsDBNull(row[dataColumn, version])))
			{
				return dataColumn;
			}
		}
		return null;
	}

	private DataColumn NthColumn(DataRow row, bool fAttribute, int iColumn, bool fNulls)
	{
		DataColumn dataColumn = null;
		while ((dataColumn = NextColumn(row, dataColumn, fAttribute, fNulls)) != null)
		{
			if (iColumn == 0)
			{
				return dataColumn;
			}
			iColumn = checked(iColumn - 1);
		}
		return null;
	}

	private int ColumnCount(DataRow row, bool fAttribute, bool fNulls)
	{
		DataColumn col = null;
		int num = 0;
		while ((col = NextColumn(row, col, fAttribute, fNulls)) != null)
		{
			num++;
		}
		return num;
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
			if (_fOnValue)
			{
				return false;
			}
			_fOnValue = true;
			return true;
		}
		if (!IsFoliated(_node))
		{
			DataColumn dataColumn = NextColumn(Row, null, fAttribute: false, fNulls: false);
			if (dataColumn != null)
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
		return false;
	}

	internal bool MoveToNextSibling()
	{
		RealFoliate();
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue && !_doc.IsTextOnly(_column))
				{
					return false;
				}
				DataColumn dataColumn = NextColumn(Row, _column, fAttribute: false, fNulls: false);
				if (dataColumn != null)
				{
					MoveTo(_node, dataColumn, fOnValue: false);
					return true;
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
				XmlNode xmlNode2 = _doc.SafeNextSibling(_node);
				if (xmlNode2 != null)
				{
					MoveTo(xmlNode2);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToParent()
	{
		RealFoliate();
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue && !_doc.IsTextOnly(_column))
				{
					MoveTo(_node, _column, fOnValue: false);
					return true;
				}
				if (_column.ColumnMapping != MappingType.Attribute)
				{
					MoveTo(_node, null, fOnValue: false);
					return true;
				}
			}
			else
			{
				XmlNode parentNode = _node.ParentNode;
				if (parentNode != null)
				{
					MoveTo(parentNode);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToOwnerElement()
	{
		RealFoliate();
		if (_node != null)
		{
			if (_column != null)
			{
				if (_fOnValue || _doc.IsTextOnly(_column) || _column.ColumnMapping != MappingType.Attribute)
				{
					return false;
				}
				MoveTo(_node, null, fOnValue: false);
				return true;
			}
			if (_node.NodeType == XmlNodeType.Attribute)
			{
				XmlNode ownerElement = ((XmlAttribute)_node).OwnerElement;
				if (ownerElement != null)
				{
					MoveTo(ownerElement, null, fOnValue: false);
					return true;
				}
			}
		}
		return false;
	}

	internal bool MoveToAttribute(int i)
	{
		RealFoliate();
		if (i < 0)
		{
			return false;
		}
		if (_node != null && (_column == null || _column.ColumnMapping == MappingType.Attribute) && _node.NodeType == XmlNodeType.Element)
		{
			if (!IsFoliated(_node))
			{
				DataColumn dataColumn = NthColumn(Row, fAttribute: true, i, fNulls: false);
				if (dataColumn != null)
				{
					MoveTo(_node, dataColumn, fOnValue: false);
					return true;
				}
			}
			else
			{
				XmlNode xmlNode = _node.Attributes.Item(i);
				if (xmlNode != null)
				{
					MoveTo(xmlNode, null, fOnValue: false);
					return true;
				}
			}
		}
		return false;
	}

	private bool IsLocalNameEmpty(XmlNodeType nt)
	{
		switch (nt)
		{
		case XmlNodeType.None:
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Comment:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
		case XmlNodeType.EndElement:
		case XmlNodeType.EndEntity:
			return true;
		case XmlNodeType.Element:
		case XmlNodeType.Attribute:
		case XmlNodeType.EntityReference:
		case XmlNodeType.Entity:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.DocumentType:
		case XmlNodeType.Notation:
		case XmlNodeType.XmlDeclaration:
			return false;
		default:
			return true;
		}
	}

	bool IXmlDataVirtualNode.IsOnNode(XmlNode nodeToCheck)
	{
		RealFoliate();
		return nodeToCheck == _node;
	}

	internal XmlNode GetNode()
	{
		return _node;
	}

	void IXmlDataVirtualNode.OnFoliated(XmlNode foliatedNode)
	{
		if (_node == foliatedNode && _column != null)
		{
			_bNeedFoliate = true;
		}
	}

	internal void RealFoliate()
	{
		if (!_bNeedFoliate)
		{
			return;
		}
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

	bool IXmlDataVirtualNode.IsInUse()
	{
		return _isInUse;
	}

	internal void SetNoLongerUse()
	{
		_node = null;
		_column = null;
		_fOnValue = false;
		_bNeedFoliate = false;
		_isInUse = false;
	}
}
