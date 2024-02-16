using System.Collections;

namespace System.Xml.Schema;

[Obsolete("XmlSchemaCollection has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation instead.")]
public sealed class XmlSchemaCollection : ICollection, IEnumerable
{
	private readonly Hashtable _collection;

	private readonly XmlNameTable _nameTable;

	private SchemaNames _schemaNames;

	private readonly object _wLock;

	private readonly bool _isThreadSafe = true;

	private ValidationEventHandler _validationEventHandler;

	private XmlResolver _xmlResolver;

	public int Count => _collection.Count;

	public XmlNameTable NameTable => _nameTable;

	internal XmlResolver? XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	public XmlSchema? this[string? ns] => ((XmlSchemaCollectionNode)_collection[(ns != null) ? ns : string.Empty])?.Schema;

	bool ICollection.IsSynchronized => true;

	object ICollection.SyncRoot => this;

	int ICollection.Count => _collection.Count;

	internal ValidationEventHandler? EventHandler
	{
		get
		{
			return _validationEventHandler;
		}
		set
		{
			_validationEventHandler = value;
		}
	}

	public event ValidationEventHandler ValidationEventHandler
	{
		add
		{
			_validationEventHandler = (ValidationEventHandler)Delegate.Combine(_validationEventHandler, value);
		}
		remove
		{
			_validationEventHandler = (ValidationEventHandler)Delegate.Remove(_validationEventHandler, value);
		}
	}

	public XmlSchemaCollection()
		: this(new NameTable())
	{
	}

	public XmlSchemaCollection(XmlNameTable nametable)
	{
		if (nametable == null)
		{
			throw new ArgumentNullException("nametable");
		}
		_nameTable = nametable;
		_collection = Hashtable.Synchronized(new Hashtable());
		_xmlResolver = null;
		_isThreadSafe = true;
		if (_isThreadSafe)
		{
			_wLock = new object();
		}
	}

	public XmlSchema? Add(string? ns, string uri)
	{
		if (uri == null || uri.Length == 0)
		{
			throw new ArgumentNullException("uri");
		}
		XmlTextReader xmlTextReader = new XmlTextReader(uri, _nameTable);
		xmlTextReader.XmlResolver = _xmlResolver;
		XmlSchema xmlSchema = null;
		try
		{
			xmlSchema = Add(ns, xmlTextReader, _xmlResolver);
			while (xmlTextReader.Read())
			{
			}
			return xmlSchema;
		}
		finally
		{
			xmlTextReader.Close();
		}
	}

	public XmlSchema? Add(string? ns, XmlReader reader)
	{
		return Add(ns, reader, _xmlResolver);
	}

	public XmlSchema? Add(string? ns, XmlReader reader, XmlResolver? resolver)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		XmlNameTable nameTable = reader.NameTable;
		SchemaInfo schemaInfo = new SchemaInfo();
		Parser parser = new Parser(SchemaType.None, nameTable, GetSchemaNames(nameTable), _validationEventHandler);
		parser.XmlResolver = resolver;
		SchemaType schemaType;
		try
		{
			schemaType = parser.Parse(reader, ns);
		}
		catch (XmlSchemaException e)
		{
			SendValidationEvent(e);
			return null;
		}
		if (schemaType == SchemaType.XSD)
		{
			schemaInfo.SchemaType = SchemaType.XSD;
			return Add(ns, schemaInfo, parser.XmlSchema, compile: true, resolver);
		}
		return Add(ns, parser.XdrSchema, null, compile: true, resolver);
	}

	public XmlSchema? Add(XmlSchema schema)
	{
		return Add(schema, _xmlResolver);
	}

	public XmlSchema? Add(XmlSchema schema, XmlResolver? resolver)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		SchemaInfo schemaInfo = new SchemaInfo();
		schemaInfo.SchemaType = SchemaType.XSD;
		return Add(schema.TargetNamespace, schemaInfo, schema, compile: true, resolver);
	}

	public void Add(XmlSchemaCollection schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		if (this != schema)
		{
			IDictionaryEnumerator enumerator = schema._collection.GetEnumerator();
			while (enumerator.MoveNext())
			{
				XmlSchemaCollectionNode xmlSchemaCollectionNode = (XmlSchemaCollectionNode)enumerator.Value;
				Add(xmlSchemaCollectionNode.NamespaceURI, xmlSchemaCollectionNode);
			}
		}
	}

	public bool Contains(XmlSchema schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		return this[schema.TargetNamespace] != null;
	}

	public bool Contains(string? ns)
	{
		return _collection[(ns != null) ? ns : string.Empty] != null;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new XmlSchemaCollectionEnumerator(_collection);
	}

	public XmlSchemaCollectionEnumerator GetEnumerator()
	{
		return new XmlSchemaCollectionEnumerator(_collection);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		XmlSchemaCollectionEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			if (index == array.Length)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			array.SetValue(enumerator.Current, index++);
		}
	}

	public void CopyTo(XmlSchema[] array, int index)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		XmlSchemaCollectionEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			XmlSchema current = enumerator.Current;
			if (current != null)
			{
				if (index == array.Length)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				array[index++] = enumerator.Current;
			}
		}
	}

	internal SchemaInfo GetSchemaInfo(string ns)
	{
		return ((XmlSchemaCollectionNode)_collection[(ns != null) ? ns : string.Empty])?.SchemaInfo;
	}

	internal SchemaNames GetSchemaNames(XmlNameTable nt)
	{
		if (_nameTable != nt)
		{
			return new SchemaNames(nt);
		}
		if (_schemaNames == null)
		{
			_schemaNames = new SchemaNames(_nameTable);
		}
		return _schemaNames;
	}

	internal XmlSchema Add(string ns, SchemaInfo schemaInfo, XmlSchema schema, bool compile)
	{
		return Add(ns, schemaInfo, schema, compile, _xmlResolver);
	}

	private XmlSchema Add(string ns, SchemaInfo schemaInfo, XmlSchema schema, bool compile, XmlResolver resolver)
	{
		int num = 0;
		if (schema != null)
		{
			if (schema.ErrorCount == 0 && compile)
			{
				if (!schema.CompileSchema(this, resolver, schemaInfo, ns, _validationEventHandler, _nameTable, CompileContentModel: true))
				{
					num = 1;
				}
				ns = ((schema.TargetNamespace == null) ? string.Empty : schema.TargetNamespace);
			}
			num += schema.ErrorCount;
		}
		else
		{
			num += schemaInfo.ErrorCount;
			ns = NameTable.Add(ns);
		}
		if (num == 0)
		{
			XmlSchemaCollectionNode xmlSchemaCollectionNode = new XmlSchemaCollectionNode();
			xmlSchemaCollectionNode.NamespaceURI = ns;
			xmlSchemaCollectionNode.SchemaInfo = schemaInfo;
			xmlSchemaCollectionNode.Schema = schema;
			Add(ns, xmlSchemaCollectionNode);
			return schema;
		}
		return null;
	}

	private void AddNonThreadSafe(string ns, XmlSchemaCollectionNode node)
	{
		if (_collection[ns] != null)
		{
			_collection.Remove(ns);
		}
		_collection.Add(ns, node);
	}

	private void Add(string ns, XmlSchemaCollectionNode node)
	{
		if (_isThreadSafe)
		{
			lock (_wLock)
			{
				AddNonThreadSafe(ns, node);
				return;
			}
		}
		AddNonThreadSafe(ns, node);
	}

	private void SendValidationEvent(XmlSchemaException e)
	{
		if (_validationEventHandler != null)
		{
			_validationEventHandler(this, new ValidationEventArgs(e));
			return;
		}
		throw e;
	}
}
