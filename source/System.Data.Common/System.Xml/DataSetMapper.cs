using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal sealed class DataSetMapper
{
	private Hashtable _tableSchemaMap;

	private Hashtable _columnSchemaMap;

	private XmlDataDocument _doc;

	private DataSet _dataSet;

	internal DataSetMapper()
	{
		_tableSchemaMap = new Hashtable();
		_columnSchemaMap = new Hashtable();
	}

	internal void SetupMapping(XmlDataDocument xd, DataSet ds)
	{
		if (IsMapped())
		{
			_tableSchemaMap = new Hashtable();
			_columnSchemaMap = new Hashtable();
		}
		_doc = xd;
		_dataSet = ds;
		foreach (DataTable table in _dataSet.Tables)
		{
			AddTableSchema(table);
			foreach (DataColumn column in table.Columns)
			{
				if (!IsNotMapped(column))
				{
					AddColumnSchema(column);
				}
			}
		}
	}

	internal bool IsMapped()
	{
		return _dataSet != null;
	}

	internal DataTable SearchMatchingTableSchema(string localName, string namespaceURI)
	{
		object identity = GetIdentity(localName, namespaceURI);
		return (DataTable)_tableSchemaMap[identity];
	}

	internal DataTable SearchMatchingTableSchema(XmlBoundElement rowElem, XmlBoundElement elem)
	{
		DataTable dataTable = SearchMatchingTableSchema(elem.LocalName, elem.NamespaceURI);
		if (dataTable == null)
		{
			return null;
		}
		if (rowElem == null)
		{
			return dataTable;
		}
		DataColumn columnSchemaForNode = GetColumnSchemaForNode(rowElem, elem);
		if (columnSchemaForNode == null)
		{
			return dataTable;
		}
		foreach (XmlAttribute attribute in elem.Attributes)
		{
			if ((object)attribute.NamespaceURI != "http://www.w3.org/2000/xmlns/")
			{
				return dataTable;
			}
		}
		for (XmlNode xmlNode = elem.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				return dataTable;
			}
		}
		return null;
	}

	internal DataColumn GetColumnSchemaForNode(XmlBoundElement rowElem, XmlNode node)
	{
		object identity = GetIdentity(rowElem.LocalName, rowElem.NamespaceURI);
		object identity2 = GetIdentity(node.LocalName, node.NamespaceURI);
		Hashtable hashtable = (Hashtable)_columnSchemaMap[identity];
		if (hashtable != null)
		{
			DataColumn dataColumn = (DataColumn)hashtable[identity2];
			if (dataColumn == null)
			{
				return null;
			}
			MappingType columnMapping = dataColumn.ColumnMapping;
			if (node.NodeType == XmlNodeType.Attribute && columnMapping == MappingType.Attribute)
			{
				return dataColumn;
			}
			if (node.NodeType == XmlNodeType.Element && columnMapping == MappingType.Element)
			{
				return dataColumn;
			}
			return null;
		}
		return null;
	}

	internal DataTable GetTableSchemaForElement(XmlBoundElement be)
	{
		return be.Row?.Table;
	}

	internal static bool IsNotMapped(DataColumn c)
	{
		return c.ColumnMapping == MappingType.Hidden;
	}

	internal DataRow GetRowFromElement(XmlElement e)
	{
		return (e as XmlBoundElement)?.Row;
	}

	internal DataRow GetRowFromElement(XmlBoundElement be)
	{
		return be.Row;
	}

	internal bool GetRegion(XmlNode node, [NotNullWhen(true)] out XmlBoundElement rowElem)
	{
		while (node != null)
		{
			if (node is XmlBoundElement xmlBoundElement && GetRowFromElement(xmlBoundElement) != null)
			{
				rowElem = xmlBoundElement;
				return true;
			}
			node = ((node.NodeType != XmlNodeType.Attribute) ? node.ParentNode : ((XmlAttribute)node).OwnerElement);
		}
		rowElem = null;
		return false;
	}

	internal bool IsRegionRadical(XmlBoundElement rowElem)
	{
		if (rowElem.ElementState == ElementState.Defoliated)
		{
			return true;
		}
		DataTable tableSchemaForElement = GetTableSchemaForElement(rowElem);
		DataColumnCollection columns = tableSchemaForElement.Columns;
		int iColumn = 0;
		int count = rowElem.Attributes.Count;
		for (int i = 0; i < count; i++)
		{
			XmlAttribute xmlAttribute = rowElem.Attributes[i];
			if (!xmlAttribute.Specified)
			{
				return false;
			}
			DataColumn columnSchemaForNode = GetColumnSchemaForNode(rowElem, xmlAttribute);
			if (columnSchemaForNode == null)
			{
				return false;
			}
			if (!IsNextColumn(columns, ref iColumn, columnSchemaForNode))
			{
				return false;
			}
			XmlNode firstChild = xmlAttribute.FirstChild;
			if (firstChild == null || firstChild.NodeType != XmlNodeType.Text || firstChild.NextSibling != null)
			{
				return false;
			}
		}
		iColumn = 0;
		XmlNode xmlNode;
		for (xmlNode = rowElem.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode.NodeType != XmlNodeType.Element)
			{
				return false;
			}
			XmlElement xmlElement = xmlNode as XmlElement;
			if (GetRowFromElement(xmlElement) != null)
			{
				break;
			}
			DataColumn columnSchemaForNode2 = GetColumnSchemaForNode(rowElem, xmlElement);
			if (columnSchemaForNode2 == null)
			{
				return false;
			}
			if (!IsNextColumn(columns, ref iColumn, columnSchemaForNode2))
			{
				return false;
			}
			if (xmlElement.HasAttributes)
			{
				return false;
			}
			XmlNode firstChild2 = xmlElement.FirstChild;
			if (firstChild2 == null || firstChild2.NodeType != XmlNodeType.Text || firstChild2.NextSibling != null)
			{
				return false;
			}
		}
		while (xmlNode != null)
		{
			if (xmlNode.NodeType != XmlNodeType.Element)
			{
				return false;
			}
			DataRow rowFromElement = GetRowFromElement((XmlElement)xmlNode);
			if (rowFromElement == null)
			{
				return false;
			}
			xmlNode = xmlNode.NextSibling;
		}
		return true;
	}

	private void AddTableSchema(DataTable table)
	{
		object identity = GetIdentity(table.EncodedTableName, table.Namespace);
		_tableSchemaMap[identity] = table;
	}

	private void AddColumnSchema(DataColumn col)
	{
		DataTable table = col.Table;
		object identity = GetIdentity(table.EncodedTableName, table.Namespace);
		object identity2 = GetIdentity(col.EncodedColumnName, col.Namespace);
		Hashtable hashtable = (Hashtable)_columnSchemaMap[identity];
		if (hashtable == null)
		{
			hashtable = new Hashtable();
			_columnSchemaMap[identity] = hashtable;
		}
		hashtable[identity2] = col;
	}

	private static object GetIdentity(string localName, string namespaceURI)
	{
		return localName + ":" + namespaceURI;
	}

	private bool IsNextColumn(DataColumnCollection columns, ref int iColumn, DataColumn col)
	{
		while (iColumn < columns.Count)
		{
			if (columns[iColumn] == col)
			{
				iColumn++;
				return true;
			}
			iColumn++;
		}
		return false;
	}
}
