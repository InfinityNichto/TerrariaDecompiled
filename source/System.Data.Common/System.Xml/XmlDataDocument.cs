using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.XPath;

namespace System.Xml;

[Obsolete("XmlDataDocument has been deprecated and is not supported.")]
public class XmlDataDocument : XmlDocument
{
	private DataSet _dataSet;

	private DataSetMapper _mapper;

	internal Hashtable _pointers;

	private int _countAddPointer;

	private ArrayList _columnChangeList;

	private DataRowState _rollbackState;

	private bool _fBoundToDataSet;

	private bool _fBoundToDocument;

	private bool _fDataRowCreatedSpecial;

	private bool _ignoreXmlEvents;

	private bool _ignoreDataSetEvents;

	private bool _isFoliationEnabled;

	private bool _optimizeStorage;

	private ElementState _autoFoliationState;

	private bool _fAssociateDataRow;

	private object _foliationLock;

	private bool _bForceExpandEntity;

	internal XmlAttribute _attrXml;

	internal bool _bLoadFromDataSet;

	internal bool _bHasXSINIL;

	internal ElementState AutoFoliationState
	{
		get
		{
			return _autoFoliationState;
		}
		set
		{
			_autoFoliationState = value;
		}
	}

	public DataSet DataSet => _dataSet;

	internal bool IgnoreXmlEvents
	{
		get
		{
			return _ignoreXmlEvents;
		}
		set
		{
			_ignoreXmlEvents = value;
		}
	}

	internal bool IgnoreDataSetEvents
	{
		get
		{
			return _ignoreDataSetEvents;
		}
		set
		{
			_ignoreDataSetEvents = value;
		}
	}

	internal bool IsFoliationEnabled
	{
		get
		{
			return _isFoliationEnabled;
		}
		set
		{
			_isFoliationEnabled = value;
		}
	}

	internal DataSetMapper Mapper => _mapper;

	internal void AddPointer(IXmlDataVirtualNode pointer)
	{
		lock (_pointers)
		{
			_countAddPointer++;
			if (_countAddPointer >= 5)
			{
				ArrayList arrayList = new ArrayList();
				foreach (DictionaryEntry pointer2 in _pointers)
				{
					IXmlDataVirtualNode xmlDataVirtualNode = (IXmlDataVirtualNode)pointer2.Value;
					if (!xmlDataVirtualNode.IsInUse())
					{
						arrayList.Add(xmlDataVirtualNode);
					}
				}
				for (int i = 0; i < arrayList.Count; i++)
				{
					_pointers.Remove(arrayList[i]);
				}
				_countAddPointer = 0;
			}
			_pointers[pointer] = pointer;
		}
	}

	[MemberNotNull("_dataSet")]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void AttachDataSet(DataSet ds)
	{
		if (ds.FBoundToDocument)
		{
			throw new ArgumentException(System.SR.DataDom_MultipleDataSet);
		}
		ds.FBoundToDocument = true;
		_dataSet = ds;
		BindSpecialListeners();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void SyncRows(DataRow parentRow, XmlNode node, bool fAddRowsToTable)
	{
		if (node is XmlBoundElement { Row: var row } xmlBoundElement)
		{
			if (row != null && xmlBoundElement.ElementState == ElementState.Defoliated)
			{
				return;
			}
			if (row != null)
			{
				SynchronizeRowFromRowElement(xmlBoundElement);
				xmlBoundElement.ElementState = ElementState.WeakFoliation;
				DefoliateRegion(xmlBoundElement);
				if (parentRow != null)
				{
					SetNestedParentRow(row, parentRow);
				}
				if (fAddRowsToTable && row.RowState == DataRowState.Detached)
				{
					row.Table.Rows.Add(row);
				}
				parentRow = row;
			}
		}
		for (XmlNode xmlNode = node.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			SyncRows(parentRow, xmlNode, fAddRowsToTable);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void SyncTree(XmlNode node)
	{
		XmlBoundElement rowElem = null;
		_mapper.GetRegion(node, out rowElem);
		DataRow parentRow = null;
		bool flag = IsConnected(node);
		if (rowElem != null)
		{
			DataRow row = rowElem.Row;
			if (row != null && rowElem.ElementState == ElementState.Defoliated)
			{
				return;
			}
			if (row != null)
			{
				SynchronizeRowFromRowElement(rowElem);
				if (node == rowElem)
				{
					rowElem.ElementState = ElementState.WeakFoliation;
					DefoliateRegion(rowElem);
				}
				if (flag && row.RowState == DataRowState.Detached)
				{
					row.Table.Rows.Add(row);
				}
				parentRow = row;
			}
		}
		for (XmlNode xmlNode = node.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			SyncRows(parentRow, xmlNode, flag);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindForLoad()
	{
		_ignoreDataSetEvents = true;
		_mapper.SetupMapping(this, _dataSet);
		if (_dataSet.Tables.Count > 0)
		{
			LoadDataSetFromTree();
		}
		BindListeners();
		_ignoreDataSetEvents = false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void Bind(bool fLoadFromDataSet)
	{
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		_mapper.SetupMapping(this, _dataSet);
		if (base.DocumentElement != null)
		{
			LoadDataSetFromTree();
			BindListeners();
		}
		else if (fLoadFromDataSet)
		{
			_bLoadFromDataSet = true;
			LoadTreeFromDataSet(DataSet);
			BindListeners();
		}
		_ignoreDataSetEvents = false;
		_ignoreXmlEvents = false;
	}

	internal void Bind(DataRow r, XmlBoundElement e)
	{
		r.Element = e;
		e.Row = r;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindSpecialListeners()
	{
		_dataSet.DataRowCreated += OnDataRowCreatedSpecial;
		_fDataRowCreatedSpecial = true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void UnBindSpecialListeners()
	{
		_dataSet.DataRowCreated -= OnDataRowCreatedSpecial;
		_fDataRowCreatedSpecial = false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindListeners()
	{
		BindToDocument();
		BindToDataSet();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindToDataSet()
	{
		if (_fBoundToDataSet)
		{
			return;
		}
		if (_fDataRowCreatedSpecial)
		{
			UnBindSpecialListeners();
		}
		_dataSet.Tables.CollectionChanging += OnDataSetTablesChanging;
		_dataSet.Relations.CollectionChanging += OnDataSetRelationsChanging;
		_dataSet.DataRowCreated += OnDataRowCreated;
		_dataSet.PropertyChanging += OnDataSetPropertyChanging;
		_dataSet.ClearFunctionCalled += OnClearCalled;
		if (_dataSet.Tables.Count > 0)
		{
			foreach (DataTable table in _dataSet.Tables)
			{
				BindToTable(table);
			}
		}
		foreach (DataRelation relation in _dataSet.Relations)
		{
			relation.PropertyChanging += OnRelationPropertyChanging;
		}
		_fBoundToDataSet = true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindToDocument()
	{
		if (!_fBoundToDocument)
		{
			base.NodeInserting += OnNodeInserting;
			base.NodeInserted += OnNodeInserted;
			base.NodeRemoving += OnNodeRemoving;
			base.NodeRemoved += OnNodeRemoved;
			base.NodeChanging += OnNodeChanging;
			base.NodeChanged += OnNodeChanged;
			_fBoundToDocument = true;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void BindToTable(DataTable t)
	{
		t.ColumnChanged += OnColumnChanged;
		t.RowChanging += OnRowChanging;
		t.RowChanged += OnRowChanged;
		t.RowDeleting += OnRowChanging;
		t.RowDeleted += OnRowChanged;
		t.PropertyChanging += OnTablePropertyChanging;
		t.Columns.CollectionChanging += OnTableColumnsChanging;
		foreach (DataColumn column in t.Columns)
		{
			column.PropertyChanging += OnColumnPropertyChanging;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI)
	{
		if (prefix == null)
		{
			prefix = string.Empty;
		}
		if (namespaceURI == null)
		{
			namespaceURI = string.Empty;
		}
		if (!_fAssociateDataRow)
		{
			return new XmlBoundElement(prefix, localName, namespaceURI, this);
		}
		EnsurePopulatedMode();
		DataTable dataTable = _mapper.SearchMatchingTableSchema(localName, namespaceURI);
		if (dataTable != null)
		{
			DataRow dataRow = dataTable.CreateEmptyRow();
			foreach (DataColumn column in dataTable.Columns)
			{
				if (column.ColumnMapping != MappingType.Hidden)
				{
					SetRowValueToNull(dataRow, column);
				}
			}
			XmlBoundElement element = dataRow.Element;
			element.Prefix = prefix;
			return element;
		}
		return new XmlBoundElement(prefix, localName, namespaceURI, this);
	}

	public override XmlEntityReference CreateEntityReference(string name)
	{
		throw new NotSupportedException(System.SR.DataDom_NotSupport_EntRef);
	}

	private void DefoliateRegion(XmlBoundElement rowElem)
	{
		if (!_optimizeStorage || rowElem.ElementState != ElementState.WeakFoliation || !_mapper.IsRegionRadical(rowElem))
		{
			return;
		}
		bool ignoreXmlEvents = IgnoreXmlEvents;
		IgnoreXmlEvents = true;
		rowElem.ElementState = ElementState.Defoliating;
		try
		{
			rowElem.RemoveAllAttributes();
			XmlNode xmlNode = rowElem.FirstChild;
			while (xmlNode != null)
			{
				XmlNode nextSibling = xmlNode.NextSibling;
				if (xmlNode is XmlBoundElement { Row: not null })
				{
					break;
				}
				rowElem.RemoveChild(xmlNode);
				xmlNode = nextSibling;
			}
			rowElem.ElementState = ElementState.Defoliated;
		}
		finally
		{
			IgnoreXmlEvents = ignoreXmlEvents;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlElement EnsureDocumentElement()
	{
		XmlElement xmlElement = base.DocumentElement;
		if (xmlElement == null)
		{
			string text = XmlConvert.EncodeLocalName(DataSet.DataSetName);
			if (text == null || text.Length == 0)
			{
				text = "Xml";
			}
			string text2 = DataSet.Namespace;
			if (text2 == null)
			{
				text2 = string.Empty;
			}
			xmlElement = new XmlBoundElement(string.Empty, text, text2, this);
			AppendChild(xmlElement);
		}
		return xmlElement;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlElement EnsureNonRowDocumentElement()
	{
		XmlElement documentElement = base.DocumentElement;
		if (documentElement == null)
		{
			return EnsureDocumentElement();
		}
		DataRow rowFromElement = GetRowFromElement(documentElement);
		if (rowFromElement == null)
		{
			return documentElement;
		}
		return DemoteDocumentElement();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlElement DemoteDocumentElement()
	{
		XmlElement documentElement = base.DocumentElement;
		RemoveChild(documentElement);
		XmlElement xmlElement = EnsureDocumentElement();
		xmlElement.AppendChild(documentElement);
		return xmlElement;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void EnsurePopulatedMode()
	{
		if (_fDataRowCreatedSpecial)
		{
			UnBindSpecialListeners();
			_mapper.SetupMapping(this, _dataSet);
			BindListeners();
			_fAssociateDataRow = true;
		}
	}

	private void FixNestedChildren(DataRow row, XmlElement rowElement)
	{
		foreach (DataRelation nestedChildRelation in GetNestedChildRelations(row))
		{
			DataRow[] childRows = row.GetChildRows(nestedChildRelation);
			foreach (DataRow dataRow in childRows)
			{
				XmlElement element = dataRow.Element;
				if (element != null && element.ParentNode != rowElement)
				{
					element.ParentNode.RemoveChild(element);
					rowElement.AppendChild(element);
				}
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void Foliate(XmlBoundElement node, ElementState newState)
	{
		if (IsFoliationEnabled)
		{
			if (node.ElementState == ElementState.Defoliated)
			{
				ForceFoliation(node, newState);
			}
			else if (node.ElementState == ElementState.WeakFoliation && newState == ElementState.StrongFoliation)
			{
				node.ElementState = newState;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void Foliate(XmlElement element)
	{
		if (element is XmlBoundElement)
		{
			((XmlBoundElement)element).Foliate(ElementState.WeakFoliation);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void FoliateIfDataPointers(DataRow row, XmlElement rowElement)
	{
		if (!IsFoliated(rowElement) && HasPointers(rowElement))
		{
			bool isFoliationEnabled = IsFoliationEnabled;
			IsFoliationEnabled = true;
			try
			{
				Foliate(rowElement);
			}
			finally
			{
				IsFoliationEnabled = isFoliationEnabled;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void EnsureFoliation(XmlBoundElement rowElem, ElementState foliation)
	{
		if (!rowElem.IsFoliated)
		{
			ForceFoliation(rowElem, foliation);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void ForceFoliation(XmlBoundElement node, ElementState newState)
	{
		lock (_foliationLock)
		{
			if (node.ElementState != ElementState.Defoliated)
			{
				return;
			}
			node.ElementState = ElementState.Foliating;
			bool ignoreXmlEvents = IgnoreXmlEvents;
			IgnoreXmlEvents = true;
			try
			{
				XmlNode xmlNode = null;
				DataRow row = node.Row;
				DataRowVersion version = ((row.RowState == DataRowState.Detached) ? DataRowVersion.Proposed : DataRowVersion.Current);
				foreach (DataColumn column in row.Table.Columns)
				{
					if (IsNotMapped(column))
					{
						continue;
					}
					object value = row[column, version];
					if (!Convert.IsDBNull(value))
					{
						if (column.ColumnMapping == MappingType.Attribute)
						{
							node.SetAttribute(column.EncodedColumnName, column.Namespace, column.ConvertObjectToXml(value));
							continue;
						}
						XmlNode xmlNode2 = null;
						if (column.ColumnMapping == MappingType.Element)
						{
							xmlNode2 = new XmlBoundElement(string.Empty, column.EncodedColumnName, column.Namespace, this);
							xmlNode2.AppendChild(CreateTextNode(column.ConvertObjectToXml(value)));
							if (xmlNode != null)
							{
								node.InsertAfter(xmlNode2, xmlNode);
							}
							else if (node.FirstChild != null)
							{
								node.InsertBefore(xmlNode2, node.FirstChild);
							}
							else
							{
								node.AppendChild(xmlNode2);
							}
							xmlNode = xmlNode2;
						}
						else
						{
							xmlNode2 = CreateTextNode(column.ConvertObjectToXml(value));
							if (node.FirstChild != null)
							{
								node.InsertBefore(xmlNode2, node.FirstChild);
							}
							else
							{
								node.AppendChild(xmlNode2);
							}
							if (xmlNode == null)
							{
								xmlNode = xmlNode2;
							}
						}
					}
					else if (column.ColumnMapping == MappingType.SimpleContent)
					{
						XmlAttribute xmlAttribute = CreateAttribute("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance");
						xmlAttribute.Value = "true";
						node.SetAttributeNode(xmlAttribute);
						_bHasXSINIL = true;
					}
				}
			}
			finally
			{
				IgnoreXmlEvents = ignoreXmlEvents;
				node.ElementState = newState;
			}
			OnFoliated(node);
		}
	}

	private XmlNode GetColumnInsertAfterLocation(DataRow row, DataColumn col, XmlBoundElement rowElement)
	{
		XmlNode result = null;
		XmlNode xmlNode = null;
		if (IsTextOnly(col))
		{
			return null;
		}
		xmlNode = rowElement.FirstChild;
		while (xmlNode != null && IsTextLikeNode(xmlNode))
		{
			result = xmlNode;
			xmlNode = xmlNode.NextSibling;
		}
		while (xmlNode != null && xmlNode.NodeType == XmlNodeType.Element)
		{
			XmlElement e = xmlNode as XmlElement;
			if (_mapper.GetRowFromElement(e) != null)
			{
				break;
			}
			object columnSchemaForNode = _mapper.GetColumnSchemaForNode(rowElement, xmlNode);
			if (columnSchemaForNode == null || !(columnSchemaForNode is DataColumn) || ((DataColumn)columnSchemaForNode).Ordinal > col.Ordinal)
			{
				break;
			}
			result = xmlNode;
			xmlNode = xmlNode.NextSibling;
		}
		return result;
	}

	private ArrayList GetNestedChildRelations(DataRow row)
	{
		ArrayList arrayList = new ArrayList();
		foreach (DataRelation childRelation in row.Table.ChildRelations)
		{
			if (childRelation.Nested)
			{
				arrayList.Add(childRelation);
			}
		}
		return arrayList;
	}

	private DataRow GetNestedParent(DataRow row)
	{
		DataRelation nestedParentRelation = GetNestedParentRelation(row);
		if (nestedParentRelation != null)
		{
			return row.GetParentRow(nestedParentRelation);
		}
		return null;
	}

	private static DataRelation GetNestedParentRelation(DataRow row)
	{
		DataRelation[] nestedParentRelations = row.Table.NestedParentRelations;
		if (nestedParentRelations.Length == 0)
		{
			return null;
		}
		return nestedParentRelations[0];
	}

	private DataColumn GetTextOnlyColumn(DataRow row)
	{
		return row.Table.XmlText;
	}

	public DataRow? GetRowFromElement(XmlElement? e)
	{
		return _mapper.GetRowFromElement(e);
	}

	private XmlNode GetRowInsertBeforeLocation(DataRow row, XmlElement rowElement, XmlNode parentElement)
	{
		DataRow dataRow = row;
		int num = 0;
		for (num = 0; num < row.Table.Rows.Count && row != row.Table.Rows[num]; num++)
		{
		}
		int num2 = num;
		DataRow nestedParent = GetNestedParent(row);
		for (num = num2 + 1; num < row.Table.Rows.Count; num++)
		{
			dataRow = row.Table.Rows[num];
			if (GetNestedParent(dataRow) == nestedParent && GetElementFromRow(dataRow).ParentNode == parentElement)
			{
				break;
			}
		}
		if (num < row.Table.Rows.Count)
		{
			return GetElementFromRow(dataRow);
		}
		return null;
	}

	public XmlElement GetElementFromRow(DataRow r)
	{
		return r.Element;
	}

	internal bool HasPointers(XmlNode node)
	{
		while (true)
		{
			try
			{
				if (_pointers.Count > 0)
				{
					object obj = null;
					foreach (DictionaryEntry pointer in _pointers)
					{
						obj = pointer.Value;
						if (((IXmlDataVirtualNode)obj).IsOnNode(node))
						{
							return true;
						}
					}
				}
				return false;
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
			}
		}
	}

	private bool IsFoliated(XmlElement element)
	{
		if (element is XmlBoundElement)
		{
			return ((XmlBoundElement)element).IsFoliated;
		}
		return true;
	}

	private bool IsFoliated(XmlBoundElement be)
	{
		return be.IsFoliated;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlNode CloneTree(DataPointer other)
	{
		EnsurePopulatedMode();
		bool ignoreDataSetEvents = _ignoreDataSetEvents;
		bool ignoreXmlEvents = _ignoreXmlEvents;
		bool isFoliationEnabled = IsFoliationEnabled;
		bool fAssociateDataRow = _fAssociateDataRow;
		try
		{
			_ignoreDataSetEvents = true;
			_ignoreXmlEvents = true;
			IsFoliationEnabled = false;
			_fAssociateDataRow = false;
			XmlNode xmlNode = CloneTreeInternal(other);
			LoadRows(null, xmlNode);
			SyncRows(null, xmlNode, fAddRowsToTable: false);
			return xmlNode;
		}
		finally
		{
			_ignoreDataSetEvents = ignoreDataSetEvents;
			_ignoreXmlEvents = ignoreXmlEvents;
			IsFoliationEnabled = isFoliationEnabled;
			_fAssociateDataRow = fAssociateDataRow;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlNode CloneTreeInternal(DataPointer other)
	{
		XmlNode xmlNode = CloneNode(other);
		DataPointer dataPointer = new DataPointer(other);
		try
		{
			dataPointer.AddPointer();
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				int attributeCount = dataPointer.AttributeCount;
				for (int i = 0; i < attributeCount; i++)
				{
					dataPointer.MoveToOwnerElement();
					if (dataPointer.MoveToAttribute(i))
					{
						xmlNode.Attributes.Append((XmlAttribute)CloneTreeInternal(dataPointer));
					}
				}
				dataPointer.MoveTo(other);
			}
			bool flag = dataPointer.MoveToFirstChild();
			while (flag)
			{
				xmlNode.AppendChild(CloneTreeInternal(dataPointer));
				flag = dataPointer.MoveToNextSibling();
			}
			return xmlNode;
		}
		finally
		{
			dataPointer.SetNoLongerUse();
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNode CloneNode(bool deep)
	{
		XmlDataDocument xmlDataDocument = (XmlDataDocument)base.CloneNode(deep: false);
		xmlDataDocument.Init(DataSet.Clone());
		xmlDataDocument._dataSet.EnforceConstraints = _dataSet.EnforceConstraints;
		if (deep)
		{
			DataPointer dataPointer = new DataPointer(this, this);
			try
			{
				dataPointer.AddPointer();
				bool flag = dataPointer.MoveToFirstChild();
				while (flag)
				{
					XmlNode newChild = ((dataPointer.NodeType != XmlNodeType.Element) ? xmlDataDocument.CloneNode(dataPointer) : xmlDataDocument.CloneTree(dataPointer));
					xmlDataDocument.AppendChild(newChild);
					flag = dataPointer.MoveToNextSibling();
				}
			}
			finally
			{
				dataPointer.SetNoLongerUse();
			}
		}
		return xmlDataDocument;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlNode CloneNode(DataPointer dp)
	{
		return dp.NodeType switch
		{
			XmlNodeType.DocumentFragment => CreateDocumentFragment(), 
			XmlNodeType.DocumentType => CreateDocumentType(dp.Name, dp.PublicId, dp.SystemId, dp.InternalSubset), 
			XmlNodeType.XmlDeclaration => CreateXmlDeclaration(dp.Version, dp.Encoding, dp.Standalone), 
			XmlNodeType.Text => CreateTextNode(dp.Value), 
			XmlNodeType.CDATA => CreateCDataSection(dp.Value), 
			XmlNodeType.ProcessingInstruction => CreateProcessingInstruction(dp.Name, dp.Value), 
			XmlNodeType.Comment => CreateComment(dp.Value), 
			XmlNodeType.Whitespace => CreateWhitespace(dp.Value), 
			XmlNodeType.SignificantWhitespace => CreateSignificantWhitespace(dp.Value), 
			XmlNodeType.Element => CreateElement(dp.Prefix, dp.LocalName, dp.NamespaceURI), 
			XmlNodeType.Attribute => CreateAttribute(dp.Prefix, dp.LocalName, dp.NamespaceURI), 
			XmlNodeType.EntityReference => CreateEntityReference(dp.Name), 
			_ => throw new InvalidOperationException(System.SR.Format(System.SR.DataDom_CloneNode, dp.NodeType.ToString())), 
		};
	}

	internal static bool IsTextLikeNode(XmlNode n)
	{
		switch (n.NodeType)
		{
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return true;
		case XmlNodeType.EntityReference:
			return false;
		default:
			return false;
		}
	}

	internal bool IsNotMapped(DataColumn c)
	{
		return DataSetMapper.IsNotMapped(c);
	}

	private bool IsSame(DataColumn c, int recNo1, int recNo2)
	{
		if (c.Compare(recNo1, recNo2) == 0)
		{
			return true;
		}
		return false;
	}

	internal bool IsTextOnly(DataColumn c)
	{
		return c.ColumnMapping == MappingType.SimpleContent;
	}

	public override void Load(string filename)
	{
		_bForceExpandEntity = true;
		base.Load(filename);
		_bForceExpandEntity = false;
	}

	public override void Load(Stream inStream)
	{
		_bForceExpandEntity = true;
		base.Load(inStream);
		_bForceExpandEntity = false;
	}

	public override void Load(TextReader txtReader)
	{
		_bForceExpandEntity = true;
		base.Load(txtReader);
		_bForceExpandEntity = false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override void Load(XmlReader reader)
	{
		if (FirstChild != null)
		{
			throw new InvalidOperationException(System.SR.DataDom_MultipleLoad);
		}
		try
		{
			_ignoreXmlEvents = true;
			if (_fDataRowCreatedSpecial)
			{
				UnBindSpecialListeners();
			}
			_fAssociateDataRow = false;
			_isFoliationEnabled = false;
			if (_bForceExpandEntity)
			{
				((XmlTextReader)reader).EntityHandling = EntityHandling.ExpandEntities;
			}
			base.Load(reader);
			BindForLoad();
		}
		finally
		{
			_ignoreXmlEvents = false;
			_isFoliationEnabled = true;
			_autoFoliationState = ElementState.StrongFoliation;
			_fAssociateDataRow = true;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadDataSetFromTree()
	{
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		bool isFoliationEnabled = IsFoliationEnabled;
		IsFoliationEnabled = false;
		bool enforceConstraints = _dataSet.EnforceConstraints;
		_dataSet.EnforceConstraints = false;
		try
		{
			LoadRows(null, base.DocumentElement);
			SyncRows(null, base.DocumentElement, fAddRowsToTable: true);
			_dataSet.EnforceConstraints = enforceConstraints;
		}
		finally
		{
			_ignoreDataSetEvents = false;
			_ignoreXmlEvents = false;
			IsFoliationEnabled = isFoliationEnabled;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadTreeFromDataSet(DataSet ds)
	{
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		bool isFoliationEnabled = IsFoliationEnabled;
		IsFoliationEnabled = false;
		_fAssociateDataRow = false;
		DataTable[] array = OrderTables(ds);
		try
		{
			foreach (DataTable dataTable in array)
			{
				foreach (DataRow row in dataTable.Rows)
				{
					AttachBoundElementToDataRow(row);
					switch (row.RowState)
					{
					case DataRowState.Unchanged:
					case DataRowState.Added:
					case DataRowState.Modified:
						OnAddRow(row);
						break;
					}
				}
			}
		}
		finally
		{
			_ignoreDataSetEvents = false;
			_ignoreXmlEvents = false;
			IsFoliationEnabled = isFoliationEnabled;
			_fAssociateDataRow = true;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadRows(XmlBoundElement rowElem, XmlNode node)
	{
		if (node is XmlBoundElement xmlBoundElement)
		{
			DataTable dataTable = _mapper.SearchMatchingTableSchema(rowElem, xmlBoundElement);
			if (dataTable != null)
			{
				DataRow rowFromElement = GetRowFromElement(xmlBoundElement);
				if (xmlBoundElement.ElementState == ElementState.None)
				{
					xmlBoundElement.ElementState = ElementState.WeakFoliation;
				}
				rowFromElement = dataTable.CreateEmptyRow();
				Bind(rowFromElement, xmlBoundElement);
				rowElem = xmlBoundElement;
			}
		}
		for (XmlNode xmlNode = node.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			LoadRows(rowElem, xmlNode);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void OnDataRowCreated(object oDataSet, DataRow row)
	{
		OnNewRow(row);
	}

	internal void OnClearCalled(object oDataSet, DataTable table)
	{
		throw new NotSupportedException(System.SR.DataDom_NotSupport_Clear);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void OnDataRowCreatedSpecial(object oDataSet, DataRow row)
	{
		Bind(fLoadFromDataSet: true);
		OnNewRow(row);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void OnNewRow(DataRow row)
	{
		AttachBoundElementToDataRow(row);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private XmlBoundElement AttachBoundElementToDataRow(DataRow row)
	{
		DataTable table = row.Table;
		XmlBoundElement xmlBoundElement = new XmlBoundElement(string.Empty, table.EncodedTableName, table.Namespace, this);
		xmlBoundElement.IsEmpty = false;
		Bind(row, xmlBoundElement);
		xmlBoundElement.ElementState = ElementState.Defoliated;
		return xmlBoundElement;
	}

	private bool NeedXSI_NilAttr(DataRow row)
	{
		DataTable table = row.Table;
		if (table._xmlText == null)
		{
			return false;
		}
		object value = row[table._xmlText];
		return Convert.IsDBNull(value);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnAddRow(DataRow row)
	{
		XmlBoundElement xmlBoundElement = (XmlBoundElement)GetElementFromRow(row);
		if (NeedXSI_NilAttr(row) && !xmlBoundElement.IsFoliated)
		{
			ForceFoliation(xmlBoundElement, AutoFoliationState);
		}
		DataRow rowFromElement = GetRowFromElement(base.DocumentElement);
		if (rowFromElement != null)
		{
			DataRow nestedParent = GetNestedParent(row);
			if (nestedParent == null)
			{
				DemoteDocumentElement();
			}
		}
		EnsureDocumentElement().AppendChild(xmlBoundElement);
		FixNestedChildren(row, xmlBoundElement);
		OnNestedParentChange(row, xmlBoundElement, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnColumnValueChanged(DataRow row, DataColumn col, XmlBoundElement rowElement)
	{
		if (!IsNotMapped(col))
		{
			object value = row[col];
			if (col.ColumnMapping == MappingType.SimpleContent && Convert.IsDBNull(value) && !rowElement.IsFoliated)
			{
				ForceFoliation(rowElement, ElementState.WeakFoliation);
			}
			else if (!IsFoliated(rowElement))
			{
				goto IL_0318;
			}
			if (IsTextOnly(col))
			{
				if (Convert.IsDBNull(value))
				{
					value = string.Empty;
					XmlAttribute attributeNode = rowElement.GetAttributeNode("xsi:nil");
					if (attributeNode == null)
					{
						attributeNode = CreateAttribute("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance");
						attributeNode.Value = "true";
						rowElement.SetAttributeNode(attributeNode);
						_bHasXSINIL = true;
					}
					else
					{
						attributeNode.Value = "true";
					}
				}
				else
				{
					XmlAttribute attributeNode2 = rowElement.GetAttributeNode("xsi:nil");
					if (attributeNode2 != null)
					{
						attributeNode2.Value = "false";
					}
				}
				ReplaceInitialChildText(rowElement, col.ConvertObjectToXml(value));
			}
			else
			{
				bool flag = false;
				if (col.ColumnMapping == MappingType.Attribute)
				{
					foreach (XmlAttribute attribute in rowElement.Attributes)
					{
						if (attribute.LocalName == col.EncodedColumnName && attribute.NamespaceURI == col.Namespace)
						{
							if (Convert.IsDBNull(value))
							{
								attribute.OwnerElement.Attributes.Remove(attribute);
							}
							else
							{
								attribute.Value = col.ConvertObjectToXml(value);
							}
							flag = true;
							break;
						}
					}
					if (!flag && !Convert.IsDBNull(value))
					{
						rowElement.SetAttribute(col.EncodedColumnName, col.Namespace, col.ConvertObjectToXml(value));
					}
				}
				else
				{
					RegionIterator regionIterator = new RegionIterator(rowElement);
					bool flag2 = regionIterator.Next();
					while (true)
					{
						if (flag2)
						{
							if (regionIterator.CurrentNode.NodeType == XmlNodeType.Element)
							{
								XmlElement xmlElement = (XmlElement)regionIterator.CurrentNode;
								if (xmlElement is XmlBoundElement { Row: not null })
								{
									flag2 = regionIterator.NextRight();
									continue;
								}
								if (xmlElement.LocalName == col.EncodedColumnName && xmlElement.NamespaceURI == col.Namespace)
								{
									flag = true;
									if (Convert.IsDBNull(value))
									{
										PromoteNonValueChildren(xmlElement);
										flag2 = regionIterator.NextRight();
										xmlElement.ParentNode.RemoveChild(xmlElement);
										continue;
									}
									ReplaceInitialChildText(xmlElement, col.ConvertObjectToXml(value));
									XmlAttribute attributeNode3 = xmlElement.GetAttributeNode("xsi:nil");
									if (attributeNode3 != null)
									{
										attributeNode3.Value = "false";
									}
									break;
								}
							}
							flag2 = regionIterator.Next();
							continue;
						}
						if (!flag && !Convert.IsDBNull(value))
						{
							XmlElement xmlElement2 = new XmlBoundElement(string.Empty, col.EncodedColumnName, col.Namespace, this);
							xmlElement2.AppendChild(CreateTextNode(col.ConvertObjectToXml(value)));
							XmlNode columnInsertAfterLocation = GetColumnInsertAfterLocation(row, col, rowElement);
							if (columnInsertAfterLocation != null)
							{
								rowElement.InsertAfter(xmlElement2, columnInsertAfterLocation);
							}
							else if (rowElement.FirstChild != null)
							{
								rowElement.InsertBefore(xmlElement2, rowElement.FirstChild);
							}
							else
							{
								rowElement.AppendChild(xmlElement2);
							}
						}
						break;
					}
				}
			}
		}
		goto IL_0318;
		IL_0318:
		DataRelation nestedParentRelation = GetNestedParentRelation(row);
		if (nestedParentRelation != null && nestedParentRelation.ChildKey.ContainsColumn(col))
		{
			OnNestedParentChange(row, rowElement, col);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnColumnChanged(object sender, DataColumnChangeEventArgs args)
	{
		if (_ignoreDataSetEvents)
		{
			return;
		}
		bool ignoreXmlEvents = _ignoreXmlEvents;
		_ignoreXmlEvents = true;
		bool isFoliationEnabled = IsFoliationEnabled;
		IsFoliationEnabled = false;
		try
		{
			DataRow row = args.Row;
			DataColumn column = args.Column;
			if (row.RowState == DataRowState.Detached)
			{
				XmlBoundElement element = row.Element;
				if (element.IsFoliated)
				{
					OnColumnValueChanged(row, column, element);
				}
			}
		}
		finally
		{
			IsFoliationEnabled = isFoliationEnabled;
			_ignoreXmlEvents = ignoreXmlEvents;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnColumnValuesChanged(DataRow row, XmlBoundElement rowElement)
	{
		if (_columnChangeList.Count > 0)
		{
			if (((DataColumn)_columnChangeList[0]).Table == row.Table)
			{
				foreach (DataColumn columnChange in _columnChangeList)
				{
					OnColumnValueChanged(row, columnChange, rowElement);
				}
			}
			else
			{
				foreach (DataColumn column in row.Table.Columns)
				{
					OnColumnValueChanged(row, column, rowElement);
				}
			}
		}
		else
		{
			foreach (DataColumn column2 in row.Table.Columns)
			{
				OnColumnValueChanged(row, column2, rowElement);
			}
		}
		_columnChangeList.Clear();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnDeleteRow(DataRow row, XmlBoundElement rowElement)
	{
		if (rowElement == base.DocumentElement)
		{
			DemoteDocumentElement();
		}
		PromoteInnerRegions(rowElement);
		rowElement.ParentNode.RemoveChild(rowElement);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnDeletingRow(DataRow row, XmlBoundElement rowElement)
	{
		if (IsFoliated(rowElement))
		{
			return;
		}
		bool ignoreXmlEvents = IgnoreXmlEvents;
		IgnoreXmlEvents = true;
		bool isFoliationEnabled = IsFoliationEnabled;
		IsFoliationEnabled = true;
		try
		{
			Foliate(rowElement);
		}
		finally
		{
			IsFoliationEnabled = isFoliationEnabled;
			IgnoreXmlEvents = ignoreXmlEvents;
		}
	}

	private void OnFoliated(XmlNode node)
	{
		while (true)
		{
			try
			{
				if (_pointers.Count <= 0)
				{
					break;
				}
				foreach (DictionaryEntry pointer in _pointers)
				{
					object value = pointer.Value;
					((IXmlDataVirtualNode)value).OnFoliated(node);
				}
				break;
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
			}
		}
	}

	private DataColumn FindAssociatedParentColumn(DataRelation relation, DataColumn childCol)
	{
		DataColumn[] columnsReference = relation.ChildKey.ColumnsReference;
		for (int i = 0; i < columnsReference.Length; i++)
		{
			if (childCol == columnsReference[i])
			{
				return relation.ParentKey.ColumnsReference[i];
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNestedParentChange(DataRow child, XmlBoundElement childElement, DataColumn childCol)
	{
		DataRow dataRow = ((childElement != base.DocumentElement && childElement.ParentNode != null) ? GetRowFromElement((XmlElement)childElement.ParentNode) : null);
		DataRow nestedParent = GetNestedParent(child);
		if (dataRow == nestedParent)
		{
			return;
		}
		if (nestedParent != null)
		{
			XmlElement elementFromRow = GetElementFromRow(nestedParent);
			elementFromRow.AppendChild(childElement);
			return;
		}
		DataRelation nestedParentRelation = GetNestedParentRelation(child);
		if (childCol == null || nestedParentRelation == null || Convert.IsDBNull(child[childCol]))
		{
			EnsureNonRowDocumentElement().AppendChild(childElement);
			return;
		}
		DataColumn dataColumn = FindAssociatedParentColumn(nestedParentRelation, childCol);
		object value = dataColumn.ConvertValue(child[childCol]);
		if (dataRow._tempRecord != -1 && dataColumn.CompareValueTo(dataRow._tempRecord, value) != 0)
		{
			EnsureNonRowDocumentElement().AppendChild(childElement);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeChanged(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents)
		{
			return;
		}
		bool ignoreDataSetEvents = _ignoreDataSetEvents;
		bool ignoreXmlEvents = _ignoreXmlEvents;
		bool isFoliationEnabled = IsFoliationEnabled;
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		IsFoliationEnabled = false;
		bool fEnableCascading = DataSet._fEnableCascading;
		DataSet._fEnableCascading = false;
		try
		{
			XmlBoundElement rowElem = null;
			if (_mapper.GetRegion(args.Node, out rowElem))
			{
				SynchronizeRowFromRowElement(rowElem);
			}
		}
		finally
		{
			_ignoreDataSetEvents = ignoreDataSetEvents;
			_ignoreXmlEvents = ignoreXmlEvents;
			IsFoliationEnabled = isFoliationEnabled;
			DataSet._fEnableCascading = fEnableCascading;
		}
	}

	private void OnNodeChanging(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents || !DataSet.EnforceConstraints)
		{
			return;
		}
		throw new InvalidOperationException(System.SR.DataDom_EnforceConstraintsShouldBeOff);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeInserted(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents)
		{
			return;
		}
		bool ignoreDataSetEvents = _ignoreDataSetEvents;
		bool ignoreXmlEvents = _ignoreXmlEvents;
		bool isFoliationEnabled = IsFoliationEnabled;
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		IsFoliationEnabled = false;
		bool fEnableCascading = DataSet._fEnableCascading;
		DataSet._fEnableCascading = false;
		try
		{
			XmlNode node = args.Node;
			XmlNode oldParent = args.OldParent;
			XmlNode newParent = args.NewParent;
			if (IsConnected(newParent))
			{
				OnNodeInsertedInTree(node);
			}
			else
			{
				OnNodeInsertedInFragment(node);
			}
		}
		finally
		{
			_ignoreDataSetEvents = ignoreDataSetEvents;
			_ignoreXmlEvents = ignoreXmlEvents;
			IsFoliationEnabled = isFoliationEnabled;
			DataSet._fEnableCascading = fEnableCascading;
		}
	}

	private void OnNodeInserting(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents || !DataSet.EnforceConstraints)
		{
			return;
		}
		throw new InvalidOperationException(System.SR.DataDom_EnforceConstraintsShouldBeOff);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeRemoved(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents)
		{
			return;
		}
		bool ignoreDataSetEvents = _ignoreDataSetEvents;
		bool ignoreXmlEvents = _ignoreXmlEvents;
		bool isFoliationEnabled = IsFoliationEnabled;
		_ignoreDataSetEvents = true;
		_ignoreXmlEvents = true;
		IsFoliationEnabled = false;
		bool fEnableCascading = DataSet._fEnableCascading;
		DataSet._fEnableCascading = false;
		try
		{
			XmlNode node = args.Node;
			XmlNode oldParent = args.OldParent;
			if (IsConnected(oldParent))
			{
				OnNodeRemovedFromTree(node, oldParent);
			}
			else
			{
				OnNodeRemovedFromFragment(node, oldParent);
			}
		}
		finally
		{
			_ignoreDataSetEvents = ignoreDataSetEvents;
			_ignoreXmlEvents = ignoreXmlEvents;
			IsFoliationEnabled = isFoliationEnabled;
			DataSet._fEnableCascading = fEnableCascading;
		}
	}

	private void OnNodeRemoving(object sender, XmlNodeChangedEventArgs args)
	{
		if (_ignoreXmlEvents || !DataSet.EnforceConstraints)
		{
			return;
		}
		throw new InvalidOperationException(System.SR.DataDom_EnforceConstraintsShouldBeOff);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeRemovedFromTree(XmlNode node, XmlNode oldParent)
	{
		if (_mapper.GetRegion(oldParent, out var rowElem))
		{
			SynchronizeRowFromRowElement(rowElem);
		}
		if (node is XmlBoundElement { Row: not null } xmlBoundElement)
		{
			EnsureDisconnectedDataRow(xmlBoundElement);
		}
		TreeIterator treeIterator = new TreeIterator(node);
		bool flag = treeIterator.NextRowElement();
		while (flag)
		{
			XmlBoundElement rowElem2 = (XmlBoundElement)treeIterator.CurrentNode;
			EnsureDisconnectedDataRow(rowElem2);
			flag = treeIterator.NextRowElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeRemovedFromFragment(XmlNode node, XmlNode oldParent)
	{
		if (_mapper.GetRegion(oldParent, out var rowElem))
		{
			DataRow row = rowElem.Row;
			if (rowElem.Row.RowState == DataRowState.Detached)
			{
				SynchronizeRowFromRowElement(rowElem);
			}
		}
		if (node is XmlBoundElement { Row: not null } xmlBoundElement)
		{
			SetNestedParentRegion(xmlBoundElement, null);
			return;
		}
		TreeIterator treeIterator = new TreeIterator(node);
		bool flag = treeIterator.NextRowElement();
		while (flag)
		{
			XmlBoundElement childRowElem = (XmlBoundElement)treeIterator.CurrentNode;
			SetNestedParentRegion(childRowElem, null);
			flag = treeIterator.NextRightRowElement();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnRowChanged(object sender, DataRowChangeEventArgs args)
	{
		if (_ignoreDataSetEvents)
		{
			return;
		}
		_ignoreXmlEvents = true;
		bool isFoliationEnabled = IsFoliationEnabled;
		IsFoliationEnabled = false;
		try
		{
			DataRow row = args.Row;
			XmlBoundElement element = row.Element;
			switch (args.Action)
			{
			case DataRowAction.Add:
				OnAddRow(row);
				break;
			case DataRowAction.Delete:
				OnDeleteRow(row, element);
				break;
			case DataRowAction.Rollback:
				switch (_rollbackState)
				{
				case DataRowState.Deleted:
					OnUndeleteRow(row, element);
					UpdateAllColumns(row, element);
					break;
				case DataRowState.Added:
					element.ParentNode.RemoveChild(element);
					break;
				case DataRowState.Modified:
					OnColumnValuesChanged(row, element);
					break;
				}
				break;
			case DataRowAction.Change:
				OnColumnValuesChanged(row, element);
				break;
			case DataRowAction.Commit:
				if (row.RowState == DataRowState.Detached)
				{
					element.RemoveAll();
				}
				break;
			}
		}
		finally
		{
			IsFoliationEnabled = isFoliationEnabled;
			_ignoreXmlEvents = false;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnRowChanging(object sender, DataRowChangeEventArgs args)
	{
		DataRow row = args.Row;
		if (args.Action == DataRowAction.Delete && row.Element != null)
		{
			OnDeletingRow(row, row.Element);
		}
		else
		{
			if (_ignoreDataSetEvents)
			{
				return;
			}
			bool isFoliationEnabled = IsFoliationEnabled;
			IsFoliationEnabled = false;
			try
			{
				_ignoreXmlEvents = true;
				XmlElement elementFromRow = GetElementFromRow(row);
				int num = -1;
				int num2 = -1;
				if (elementFromRow == null)
				{
					return;
				}
				switch (args.Action)
				{
				case DataRowAction.Rollback:
					_rollbackState = row.RowState;
					switch (_rollbackState)
					{
					case DataRowState.Modified:
						_columnChangeList.Clear();
						num = row.GetRecordFromVersion(DataRowVersion.Original);
						num2 = row.GetRecordFromVersion(DataRowVersion.Current);
						{
							foreach (DataColumn column in row.Table.Columns)
							{
								if (!IsSame(column, num, num2))
								{
									_columnChangeList.Add(column);
								}
							}
							break;
						}
					}
					break;
				case DataRowAction.Change:
					_columnChangeList.Clear();
					num = row.GetRecordFromVersion(DataRowVersion.Proposed);
					num2 = row.GetRecordFromVersion(DataRowVersion.Current);
					{
						foreach (DataColumn column2 in row.Table.Columns)
						{
							object value = row[column2, DataRowVersion.Proposed];
							object value2 = row[column2, DataRowVersion.Current];
							if (Convert.IsDBNull(value) && !Convert.IsDBNull(value2) && column2.ColumnMapping != MappingType.Hidden)
							{
								FoliateIfDataPointers(row, elementFromRow);
							}
							if (!IsSame(column2, num, num2))
							{
								_columnChangeList.Add(column2);
							}
						}
						break;
					}
				}
			}
			finally
			{
				_ignoreXmlEvents = false;
				IsFoliationEnabled = isFoliationEnabled;
			}
		}
	}

	private void OnDataSetPropertyChanging(object oDataSet, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == "DataSetName")
		{
			throw new InvalidOperationException(System.SR.DataDom_DataSetNameChange);
		}
	}

	private void OnColumnPropertyChanging(object oColumn, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == "ColumnName")
		{
			throw new InvalidOperationException(System.SR.DataDom_ColumnNameChange);
		}
		if (args.PropertyName == "Namespace")
		{
			throw new InvalidOperationException(System.SR.DataDom_ColumnNamespaceChange);
		}
		if (args.PropertyName == "ColumnMapping")
		{
			throw new InvalidOperationException(System.SR.DataDom_ColumnMappingChange);
		}
	}

	private void OnTablePropertyChanging(object oTable, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == "TableName")
		{
			throw new InvalidOperationException(System.SR.DataDom_TableNameChange);
		}
		if (args.PropertyName == "Namespace")
		{
			throw new InvalidOperationException(System.SR.DataDom_TableNamespaceChange);
		}
	}

	private void OnTableColumnsChanging(object oColumnsCollection, CollectionChangeEventArgs args)
	{
		throw new InvalidOperationException(System.SR.DataDom_TableColumnsChange);
	}

	private void OnDataSetTablesChanging(object oTablesCollection, CollectionChangeEventArgs args)
	{
		throw new InvalidOperationException(System.SR.DataDom_DataSetTablesChange);
	}

	private void OnDataSetRelationsChanging(object oRelationsCollection, CollectionChangeEventArgs args)
	{
		DataRelation dataRelation = (DataRelation)args.Element;
		if (dataRelation != null && dataRelation.Nested)
		{
			throw new InvalidOperationException(System.SR.DataDom_DataSetNestedRelationsChange);
		}
		if (args.Action != CollectionChangeAction.Refresh)
		{
			return;
		}
		foreach (DataRelation item in (DataRelationCollection)oRelationsCollection)
		{
			if (item.Nested)
			{
				throw new InvalidOperationException(System.SR.DataDom_DataSetNestedRelationsChange);
			}
		}
	}

	private void OnRelationPropertyChanging(object oRelationsCollection, PropertyChangedEventArgs args)
	{
		if (args.PropertyName == "Nested")
		{
			throw new InvalidOperationException(System.SR.DataDom_DataSetNestedRelationsChange);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnUndeleteRow(DataRow row, XmlElement rowElement)
	{
		if (rowElement.ParentNode != null)
		{
			rowElement.ParentNode.RemoveChild(rowElement);
		}
		DataRow nestedParent = GetNestedParent(row);
		XmlElement xmlElement = ((nestedParent != null) ? GetElementFromRow(nestedParent) : EnsureNonRowDocumentElement());
		XmlNode rowInsertBeforeLocation;
		if ((rowInsertBeforeLocation = GetRowInsertBeforeLocation(row, rowElement, xmlElement)) != null)
		{
			xmlElement.InsertBefore(rowElement, rowInsertBeforeLocation);
		}
		else
		{
			xmlElement.AppendChild(rowElement);
		}
		FixNestedChildren(row, rowElement);
	}

	private void PromoteChild(XmlNode child, XmlNode prevSibling)
	{
		if (child.ParentNode != null)
		{
			child.ParentNode.RemoveChild(child);
		}
		prevSibling.ParentNode.InsertAfter(child, prevSibling);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void PromoteInnerRegions(XmlNode parent)
	{
		_mapper.GetRegion(parent.ParentNode, out var rowElem);
		TreeIterator treeIterator = new TreeIterator(parent);
		bool flag = treeIterator.NextRowElement();
		while (flag)
		{
			XmlBoundElement xmlBoundElement = (XmlBoundElement)treeIterator.CurrentNode;
			flag = treeIterator.NextRightRowElement();
			PromoteChild(xmlBoundElement, parent);
			SetNestedParentRegion(xmlBoundElement, rowElem);
		}
	}

	private void PromoteNonValueChildren(XmlNode parent)
	{
		XmlNode prevSibling = parent;
		XmlNode xmlNode = parent.FirstChild;
		bool flag = true;
		XmlNode xmlNode2 = null;
		while (xmlNode != null)
		{
			xmlNode2 = xmlNode.NextSibling;
			if (!flag || !IsTextLikeNode(xmlNode))
			{
				flag = false;
				xmlNode2 = xmlNode.NextSibling;
				PromoteChild(xmlNode, prevSibling);
				prevSibling = xmlNode;
			}
			xmlNode = xmlNode2;
		}
	}

	private void RemoveInitialTextNodes(XmlNode node)
	{
		while (node != null && IsTextLikeNode(node))
		{
			XmlNode nextSibling = node.NextSibling;
			node.ParentNode.RemoveChild(node);
			node = nextSibling;
		}
	}

	private void ReplaceInitialChildText(XmlNode parent, string value)
	{
		XmlNode xmlNode = parent.FirstChild;
		while (xmlNode != null && xmlNode.NodeType == XmlNodeType.Whitespace)
		{
			xmlNode = xmlNode.NextSibling;
		}
		if (xmlNode != null)
		{
			if (xmlNode.NodeType == XmlNodeType.Text)
			{
				xmlNode.Value = value;
			}
			else
			{
				xmlNode = parent.InsertBefore(CreateTextNode(value), xmlNode);
			}
			RemoveInitialTextNodes(xmlNode.NextSibling);
		}
		else
		{
			parent.AppendChild(CreateTextNode(value));
		}
	}

	internal XmlNode SafeFirstChild(XmlNode n)
	{
		if (n is XmlBoundElement xmlBoundElement)
		{
			return xmlBoundElement.SafeFirstChild;
		}
		return n.FirstChild;
	}

	internal XmlNode SafeNextSibling(XmlNode n)
	{
		if (n is XmlBoundElement xmlBoundElement)
		{
			return xmlBoundElement.SafeNextSibling;
		}
		return n.NextSibling;
	}

	internal XmlNode SafePreviousSibling(XmlNode n)
	{
		if (n is XmlBoundElement xmlBoundElement)
		{
			return xmlBoundElement.SafePreviousSibling;
		}
		return n.PreviousSibling;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal static void SetRowValueToNull(DataRow row, DataColumn col)
	{
		if (!row.IsNull(col))
		{
			row[col] = DBNull.Value;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal static void SetRowValueFromXmlText(DataRow row, DataColumn col, string xmlText)
	{
		object obj;
		try
		{
			obj = col.ConvertXmlToObject(xmlText);
		}
		catch (Exception e) when (ADP.IsCatchableExceptionType(e))
		{
			SetRowValueToNull(row, col);
			return;
		}
		if (!obj.Equals(row[col]))
		{
			row[col] = obj;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SynchronizeRowFromRowElement(XmlBoundElement rowElement)
	{
		SynchronizeRowFromRowElement(rowElement, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SynchronizeRowFromRowElement(XmlBoundElement rowElement, ArrayList rowElemList)
	{
		DataRow row = rowElement.Row;
		if (row.RowState != DataRowState.Deleted)
		{
			row.BeginEdit();
			SynchronizeRowFromRowElementEx(rowElement, rowElemList);
			row.EndEdit();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SynchronizeRowFromRowElementEx(XmlBoundElement rowElement, ArrayList rowElemList)
	{
		DataRow row = rowElement.Row;
		Hashtable hashtable = new Hashtable();
		string empty = string.Empty;
		RegionIterator regionIterator = new RegionIterator(rowElement);
		DataColumn textOnlyColumn = GetTextOnlyColumn(row);
		bool flag;
		if (textOnlyColumn != null)
		{
			hashtable[textOnlyColumn] = textOnlyColumn;
			flag = regionIterator.NextInitialTextLikeNodes(out var value);
			if (value.Length == 0 && ((empty = rowElement.GetAttribute("xsi:nil")) == "1" || empty == "true"))
			{
				row[textOnlyColumn] = DBNull.Value;
			}
			else
			{
				SetRowValueFromXmlText(row, textOnlyColumn, value);
			}
		}
		else
		{
			flag = regionIterator.Next();
		}
		while (flag)
		{
			if (!(regionIterator.CurrentNode is XmlElement xmlElement))
			{
				flag = regionIterator.Next();
				continue;
			}
			if (xmlElement is XmlBoundElement { Row: not null })
			{
				rowElemList?.Add(xmlElement);
				flag = regionIterator.NextRight();
				continue;
			}
			DataColumn columnSchemaForNode = _mapper.GetColumnSchemaForNode(rowElement, xmlElement);
			if (columnSchemaForNode != null && hashtable[columnSchemaForNode] == null)
			{
				hashtable[columnSchemaForNode] = columnSchemaForNode;
				flag = regionIterator.NextInitialTextLikeNodes(out var value2);
				if (value2.Length == 0 && ((empty = xmlElement.GetAttribute("xsi:nil")) == "1" || empty == "true"))
				{
					row[columnSchemaForNode] = DBNull.Value;
				}
				else
				{
					SetRowValueFromXmlText(row, columnSchemaForNode, value2);
				}
			}
			else
			{
				flag = regionIterator.Next();
			}
		}
		foreach (XmlAttribute attribute in rowElement.Attributes)
		{
			DataColumn columnSchemaForNode2 = _mapper.GetColumnSchemaForNode(rowElement, attribute);
			if (columnSchemaForNode2 != null && hashtable[columnSchemaForNode2] == null)
			{
				hashtable[columnSchemaForNode2] = columnSchemaForNode2;
				SetRowValueFromXmlText(row, columnSchemaForNode2, attribute.Value);
			}
		}
		foreach (DataColumn column in row.Table.Columns)
		{
			if (hashtable[column] == null && !IsNotMapped(column))
			{
				if (!column.AutoIncrement)
				{
					SetRowValueToNull(row, column);
				}
				else
				{
					column.Init(row._tempRecord);
				}
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void UpdateAllColumns(DataRow row, XmlBoundElement rowElement)
	{
		foreach (DataColumn column in row.Table.Columns)
		{
			OnColumnValueChanged(row, column, rowElement);
		}
	}

	[RequiresUnreferencedCode("XmlDataDocument is used for serialization and deserialization. Members from serialized types may be trimmed if not referenced directly.")]
	public XmlDataDocument()
		: base(new XmlDataImplementation())
	{
		Init();
		AttachDataSet(new DataSet());
		_dataSet.EnforceConstraints = false;
	}

	[RequiresUnreferencedCode("XmlDataDocument is used for serialization and deserialization. Members from serialized types may be trimmed if not referenced directly.")]
	public XmlDataDocument(DataSet dataset)
		: base(new XmlDataImplementation())
	{
		Init(dataset);
	}

	[RequiresUnreferencedCode("XmlDataDocument is used for serialization and deserialization. Members from serialized types may be trimmed if not referenced directly.")]
	internal XmlDataDocument(XmlImplementation imp)
		: base(imp)
	{
		_dataSet = null;
		_pointers = null;
		_columnChangeList = null;
		_mapper = null;
		_foliationLock = null;
		_attrXml = null;
	}

	[MemberNotNull("_pointers")]
	[MemberNotNull("_columnChangeList")]
	[MemberNotNull("_mapper")]
	[MemberNotNull("_foliationLock")]
	[MemberNotNull("_attrXml")]
	private void Init()
	{
		_pointers = new Hashtable();
		_countAddPointer = 0;
		_columnChangeList = new ArrayList();
		_ignoreDataSetEvents = false;
		_isFoliationEnabled = true;
		_optimizeStorage = true;
		_fDataRowCreatedSpecial = false;
		_autoFoliationState = ElementState.StrongFoliation;
		_fAssociateDataRow = true;
		_mapper = new DataSetMapper();
		_foliationLock = new object();
		_ignoreXmlEvents = true;
		_attrXml = CreateAttribute("xmlns", "xml", "http://www.w3.org/2000/xmlns/");
		_attrXml.Value = "http://www.w3.org/XML/1998/namespace";
		_ignoreXmlEvents = false;
	}

	[MemberNotNull("_pointers")]
	[MemberNotNull("_columnChangeList")]
	[MemberNotNull("_mapper")]
	[MemberNotNull("_foliationLock")]
	[MemberNotNull("_attrXml")]
	[MemberNotNull("_dataSet")]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void Init(DataSet ds)
	{
		if (ds == null)
		{
			throw new ArgumentException(System.SR.DataDom_DataSetNull);
		}
		Init();
		if (ds.FBoundToDocument)
		{
			throw new ArgumentException(System.SR.DataDom_MultipleDataSet);
		}
		ds.FBoundToDocument = true;
		_dataSet = ds;
		Bind(fLoadFromDataSet: true);
	}

	private bool IsConnected(XmlNode node)
	{
		while (true)
		{
			if (node == null)
			{
				return false;
			}
			if (node == this)
			{
				break;
			}
			node = ((!(node is XmlAttribute xmlAttribute)) ? node.ParentNode : xmlAttribute.OwnerElement);
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void SetNestedParentRow(DataRow childRow, DataRow parentRow)
	{
		DataRelation nestedParentRelation = GetNestedParentRelation(childRow);
		if (nestedParentRelation != null)
		{
			if (parentRow == null || nestedParentRelation.ParentKey.Table != parentRow.Table)
			{
				childRow.SetParentRow(null, nestedParentRelation);
			}
			else
			{
				childRow.SetParentRow(parentRow, nestedParentRelation);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeInsertedInTree(XmlNode node)
	{
		ArrayList arrayList = new ArrayList();
		if (_mapper.GetRegion(node, out var rowElem))
		{
			if (rowElem == node)
			{
				OnRowElementInsertedInTree(rowElem, arrayList);
			}
			else
			{
				OnNonRowElementInsertedInTree(node, rowElem, arrayList);
			}
		}
		else
		{
			TreeIterator treeIterator = new TreeIterator(node);
			bool flag = treeIterator.NextRowElement();
			while (flag)
			{
				arrayList.Add(treeIterator.CurrentNode);
				flag = treeIterator.NextRightRowElement();
			}
		}
		while (arrayList.Count > 0)
		{
			XmlBoundElement rowElem2 = (XmlBoundElement)arrayList[0];
			arrayList.RemoveAt(0);
			OnRowElementInsertedInTree(rowElem2, arrayList);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNodeInsertedInFragment(XmlNode node)
	{
		if (!_mapper.GetRegion(node, out var rowElem))
		{
			return;
		}
		if (rowElem == node)
		{
			SetNestedParentRegion(rowElem);
			return;
		}
		ArrayList arrayList = new ArrayList();
		OnNonRowElementInsertedInFragment(node, rowElem, arrayList);
		while (arrayList.Count > 0)
		{
			XmlBoundElement childRowElem = (XmlBoundElement)arrayList[0];
			arrayList.RemoveAt(0);
			SetNestedParentRegion(childRowElem, rowElem);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnRowElementInsertedInTree(XmlBoundElement rowElem, ArrayList rowElemList)
	{
		DataRow row = rowElem.Row;
		switch (row.RowState)
		{
		case DataRowState.Detached:
			row.Table.Rows.Add(row);
			SetNestedParentRegion(rowElem);
			if (rowElemList != null)
			{
				RegionIterator regionIterator = new RegionIterator(rowElem);
				bool flag = regionIterator.NextRowElement();
				while (flag)
				{
					rowElemList.Add(regionIterator.CurrentNode);
					flag = regionIterator.NextRightRowElement();
				}
			}
			break;
		case DataRowState.Deleted:
			row.RejectChanges();
			SynchronizeRowFromRowElement(rowElem, rowElemList);
			SetNestedParentRegion(rowElem);
			break;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void EnsureDisconnectedDataRow(XmlBoundElement rowElem)
	{
		DataRow row = rowElem.Row;
		switch (row.RowState)
		{
		case DataRowState.Detached:
			SetNestedParentRegion(rowElem);
			break;
		case DataRowState.Unchanged:
		case DataRowState.Modified:
			EnsureFoliation(rowElem, ElementState.WeakFoliation);
			row.Delete();
			break;
		case DataRowState.Added:
			EnsureFoliation(rowElem, ElementState.WeakFoliation);
			row.Delete();
			SetNestedParentRegion(rowElem);
			break;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNonRowElementInsertedInTree(XmlNode node, XmlBoundElement rowElement, ArrayList rowElemList)
	{
		DataRow row = rowElement.Row;
		SynchronizeRowFromRowElement(rowElement);
		if (rowElemList != null)
		{
			TreeIterator treeIterator = new TreeIterator(node);
			bool flag = treeIterator.NextRowElement();
			while (flag)
			{
				rowElemList.Add(treeIterator.CurrentNode);
				flag = treeIterator.NextRightRowElement();
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void OnNonRowElementInsertedInFragment(XmlNode node, XmlBoundElement rowElement, ArrayList rowElemList)
	{
		DataRow row = rowElement.Row;
		if (row.RowState == DataRowState.Detached)
		{
			SynchronizeRowFromRowElementEx(rowElement, rowElemList);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SetNestedParentRegion(XmlBoundElement childRowElem)
	{
		_mapper.GetRegion(childRowElem.ParentNode, out var rowElem);
		SetNestedParentRegion(childRowElem, rowElem);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SetNestedParentRegion(XmlBoundElement childRowElem, XmlBoundElement parentRowElem)
	{
		DataRow row = childRowElem.Row;
		if (parentRowElem == null)
		{
			SetNestedParentRow(row, null);
			return;
		}
		DataRow row2 = parentRowElem.Row;
		DataRelation[] nestedParentRelations = row.Table.NestedParentRelations;
		if (nestedParentRelations.Length != 0 && nestedParentRelations[0].ParentTable == row2.Table)
		{
			SetNestedParentRow(row, row2);
		}
		else
		{
			SetNestedParentRow(row, null);
		}
	}

	internal static bool IsTextNode(XmlNodeType nt)
	{
		if ((uint)(nt - 3) <= 1u || (uint)(nt - 13) <= 1u)
		{
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	protected override XPathNavigator? CreateNavigator(XmlNode node)
	{
		if (XPathNodePointer.s_xmlNodeType_To_XpathNodeType_Map[(int)node.NodeType] == -1)
		{
			return null;
		}
		if (IsTextNode(node.NodeType))
		{
			XmlNode xmlNode = node.ParentNode;
			if (xmlNode != null && xmlNode.NodeType == XmlNodeType.Attribute)
			{
				return null;
			}
			XmlNode xmlNode2 = node.PreviousSibling;
			while (xmlNode2 != null && IsTextNode(xmlNode2.NodeType))
			{
				node = xmlNode2;
				xmlNode2 = SafePreviousSibling(node);
			}
		}
		return new DataDocumentXPathNavigator(this, node);
	}

	public override XmlElement? GetElementById(string elemId)
	{
		throw new NotSupportedException(System.SR.DataDom_NotSupport_GetElementById);
	}

	public override XmlNodeList GetElementsByTagName(string name)
	{
		XmlNodeList elementsByTagName = base.GetElementsByTagName(name);
		_ = elementsByTagName.Count;
		return elementsByTagName;
	}

	private DataTable[] OrderTables(DataSet ds)
	{
		DataTable[] array = null;
		if (ds == null || ds.Tables.Count == 0)
		{
			array = Array.Empty<DataTable>();
		}
		else if (TablesAreOrdered(ds))
		{
			array = new DataTable[ds.Tables.Count];
			ds.Tables.CopyTo(array, 0);
		}
		if (array == null)
		{
			array = new DataTable[ds.Tables.Count];
			List<DataTable> list = new List<DataTable>();
			foreach (DataTable table in ds.Tables)
			{
				if (table.ParentRelations.Count == 0)
				{
					list.Add(table);
				}
			}
			if (list.Count > 0)
			{
				foreach (DataTable table2 in ds.Tables)
				{
					if (IsSelfRelatedDataTable(table2))
					{
						list.Add(table2);
					}
				}
				for (int i = 0; i < list.Count; i++)
				{
					foreach (DataRelation childRelation in list[i].ChildRelations)
					{
						DataTable childTable = childRelation.ChildTable;
						if (!list.Contains(childTable))
						{
							list.Add(childTable);
						}
					}
				}
				list.CopyTo(array);
			}
			else
			{
				ds.Tables.CopyTo(array, 0);
			}
		}
		return array;
	}

	private bool IsSelfRelatedDataTable(DataTable rootTable)
	{
		List<DataTable> list = new List<DataTable>();
		bool flag = false;
		foreach (DataRelation childRelation in rootTable.ChildRelations)
		{
			DataTable childTable = childRelation.ChildTable;
			if (childTable == rootTable)
			{
				flag = true;
				break;
			}
			if (!list.Contains(childTable))
			{
				list.Add(childTable);
			}
		}
		if (!flag)
		{
			for (int i = 0; i < list.Count; i++)
			{
				foreach (DataRelation childRelation2 in list[i].ChildRelations)
				{
					DataTable childTable2 = childRelation2.ChildTable;
					if (childTable2 == rootTable)
					{
						flag = true;
						break;
					}
					if (!list.Contains(childTable2))
					{
						list.Add(childTable2);
					}
				}
				if (flag)
				{
					break;
				}
			}
		}
		return flag;
	}

	private bool TablesAreOrdered(DataSet ds)
	{
		foreach (DataTable table in ds.Tables)
		{
			if (table.Namespace != ds.Namespace)
			{
				return false;
			}
		}
		return true;
	}
}
