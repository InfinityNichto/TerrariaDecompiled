using System.Collections;
using System.Collections.Specialized;

namespace System.Xml.Serialization;

public class ImportContext
{
	private readonly bool _shareTypes;

	private SchemaObjectCache _cache;

	private Hashtable _mappings;

	private Hashtable _elements;

	private CodeIdentifiers _typeIdentifiers;

	internal SchemaObjectCache Cache
	{
		get
		{
			if (_cache == null)
			{
				_cache = new SchemaObjectCache();
			}
			return _cache;
		}
	}

	internal Hashtable Elements
	{
		get
		{
			if (_elements == null)
			{
				_elements = new Hashtable();
			}
			return _elements;
		}
	}

	internal Hashtable Mappings
	{
		get
		{
			if (_mappings == null)
			{
				_mappings = new Hashtable();
			}
			return _mappings;
		}
	}

	public CodeIdentifiers TypeIdentifiers
	{
		get
		{
			if (_typeIdentifiers == null)
			{
				_typeIdentifiers = new CodeIdentifiers();
			}
			return _typeIdentifiers;
		}
	}

	public bool ShareTypes => _shareTypes;

	public StringCollection Warnings => Cache.Warnings;

	public ImportContext(CodeIdentifiers? identifiers, bool shareTypes)
	{
		_typeIdentifiers = identifiers;
		_shareTypes = shareTypes;
	}

	internal ImportContext()
		: this(null, shareTypes: false)
	{
	}
}
