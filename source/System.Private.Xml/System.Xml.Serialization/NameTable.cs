using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Serialization;

internal sealed class NameTable : INameScope
{
	private readonly Dictionary<NameKey, object> _table = new Dictionary<NameKey, object>();

	internal object this[XmlQualifiedName qname]
	{
		get
		{
			if (!_table.TryGetValue(new NameKey(qname.Name, qname.Namespace), out var value))
			{
				return null;
			}
			return value;
		}
		set
		{
			_table[new NameKey(qname.Name, qname.Namespace)] = value;
		}
	}

	internal object this[string name, string ns]
	{
		get
		{
			if (!_table.TryGetValue(new NameKey(name, ns), out var value))
			{
				return null;
			}
			return value;
		}
		set
		{
			_table[new NameKey(name, ns)] = value;
		}
	}

	object INameScope.this[string name, string ns]
	{
		get
		{
			_table.TryGetValue(new NameKey(name, ns), out var value);
			return value;
		}
		set
		{
			_table[new NameKey(name, ns)] = value;
		}
	}

	internal ICollection Values => _table.Values;

	internal void Add(XmlQualifiedName qname, object value)
	{
		Add(qname.Name, qname.Namespace, value);
	}

	internal void Add(string name, string ns, object value)
	{
		NameKey key = new NameKey(name, ns);
		_table.Add(key, value);
	}

	internal Array ToArray(Type type)
	{
		Array array = Array.CreateInstance(type, _table.Count);
		((ICollection)_table.Values).CopyTo(array, 0);
		return array;
	}
}
