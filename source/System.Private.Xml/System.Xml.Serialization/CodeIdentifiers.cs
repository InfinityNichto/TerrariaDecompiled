using System.Collections;
using System.Globalization;

namespace System.Xml.Serialization;

public class CodeIdentifiers
{
	private Hashtable _identifiers;

	private Hashtable _reservedIdentifiers;

	private ArrayList _list;

	private bool _camelCase;

	public bool UseCamelCasing
	{
		get
		{
			return _camelCase;
		}
		set
		{
			_camelCase = value;
		}
	}

	public CodeIdentifiers()
		: this(caseSensitive: true)
	{
	}

	public CodeIdentifiers(bool caseSensitive)
	{
		if (caseSensitive)
		{
			_identifiers = new Hashtable();
			_reservedIdentifiers = new Hashtable();
		}
		else
		{
			IEqualityComparer equalityComparer = new CaseInsensitiveKeyComparer();
			_identifiers = new Hashtable(equalityComparer);
			_reservedIdentifiers = new Hashtable(equalityComparer);
		}
		_list = new ArrayList();
	}

	public void Clear()
	{
		_identifiers.Clear();
		_list.Clear();
	}

	public string MakeRightCase(string identifier)
	{
		if (_camelCase)
		{
			return CodeIdentifier.MakeCamel(identifier);
		}
		return CodeIdentifier.MakePascal(identifier);
	}

	public string MakeUnique(string identifier)
	{
		if (IsInUse(identifier))
		{
			int num = 1;
			string text;
			while (true)
			{
				text = identifier + num.ToString(CultureInfo.InvariantCulture);
				if (!IsInUse(text))
				{
					break;
				}
				num++;
			}
			identifier = text;
		}
		if (identifier.Length > 511)
		{
			return MakeUnique("Item");
		}
		return identifier;
	}

	public void AddReserved(string identifier)
	{
		_reservedIdentifiers.Add(identifier, identifier);
	}

	public void RemoveReserved(string identifier)
	{
		_reservedIdentifiers.Remove(identifier);
	}

	public string AddUnique(string identifier, object? value)
	{
		identifier = MakeUnique(identifier);
		Add(identifier, value);
		return identifier;
	}

	public bool IsInUse(string identifier)
	{
		if (!_identifiers.Contains(identifier))
		{
			return _reservedIdentifiers.Contains(identifier);
		}
		return true;
	}

	public void Add(string identifier, object? value)
	{
		_identifiers.Add(identifier, value);
		_list.Add(value);
	}

	public void Remove(string identifier)
	{
		_list.Remove(_identifiers[identifier]);
		_identifiers.Remove(identifier);
	}

	public object ToArray(Type type)
	{
		Array array = Array.CreateInstance(type, _list.Count);
		_list.CopyTo(array, 0);
		return array;
	}

	internal CodeIdentifiers Clone()
	{
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		codeIdentifiers._identifiers = (Hashtable)_identifiers.Clone();
		codeIdentifiers._reservedIdentifiers = (Hashtable)_reservedIdentifiers.Clone();
		codeIdentifiers._list = (ArrayList)_list.Clone();
		codeIdentifiers._camelCase = _camelCase;
		return codeIdentifiers;
	}
}
