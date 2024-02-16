using System.Collections;

namespace System.Xml.Schema;

internal sealed class SymbolsDictionary
{
	private int _last;

	private readonly Hashtable _names;

	private Hashtable _wildcards;

	private readonly ArrayList _particles;

	private object _particleLast;

	private bool _isUpaEnforced = true;

	public int Count => _last + 1;

	public bool IsUpaEnforced
	{
		get
		{
			return _isUpaEnforced;
		}
		set
		{
			_isUpaEnforced = value;
		}
	}

	public int this[XmlQualifiedName name]
	{
		get
		{
			object obj = _names[name];
			if (obj != null)
			{
				return (int)obj;
			}
			if (_wildcards != null)
			{
				obj = _wildcards[name.Namespace];
				if (obj != null)
				{
					return (int)obj;
				}
			}
			return _last;
		}
	}

	public SymbolsDictionary()
	{
		_names = new Hashtable();
		_particles = new ArrayList();
	}

	public int AddName(XmlQualifiedName name, object particle)
	{
		object obj = _names[name];
		if (obj != null)
		{
			int num = (int)obj;
			if (_particles[num] != particle)
			{
				_isUpaEnforced = false;
			}
			return num;
		}
		_names.Add(name, _last);
		_particles.Add(particle);
		return _last++;
	}

	public void AddNamespaceList(NamespaceList list, object particle, bool allowLocal)
	{
		switch (list.Type)
		{
		case NamespaceList.ListType.Any:
			_particleLast = particle;
			break;
		case NamespaceList.ListType.Other:
			AddWildcard(list.Excluded, null);
			if (!allowLocal)
			{
				AddWildcard(string.Empty, null);
			}
			break;
		case NamespaceList.ListType.Set:
		{
			foreach (string item in list.Enumerate)
			{
				AddWildcard(item, particle);
			}
			break;
		}
		}
	}

	private void AddWildcard(string wildcard, object particle)
	{
		if (_wildcards == null)
		{
			_wildcards = new Hashtable();
		}
		object obj = _wildcards[wildcard];
		if (obj == null)
		{
			_wildcards.Add(wildcard, _last);
			_particles.Add(particle);
			_last++;
		}
		else if (particle != null)
		{
			_particles[(int)obj] = particle;
		}
	}

	public ICollection GetNamespaceListSymbols(NamespaceList list)
	{
		ArrayList arrayList = new ArrayList();
		foreach (XmlQualifiedName key in _names.Keys)
		{
			if (key != XmlQualifiedName.Empty && list.Allows(key))
			{
				arrayList.Add(_names[key]);
			}
		}
		if (_wildcards != null)
		{
			foreach (string key2 in _wildcards.Keys)
			{
				if (list.Allows(key2))
				{
					arrayList.Add(_wildcards[key2]);
				}
			}
		}
		if (list.Type == NamespaceList.ListType.Any || list.Type == NamespaceList.ListType.Other)
		{
			arrayList.Add(_last);
		}
		return arrayList;
	}

	public bool Exists(XmlQualifiedName name)
	{
		object obj = _names[name];
		if (obj != null)
		{
			return true;
		}
		return false;
	}

	public object GetParticle(int symbol)
	{
		if (symbol != _last)
		{
			return _particles[symbol];
		}
		return _particleLast;
	}

	public string NameOf(int symbol)
	{
		foreach (DictionaryEntry name in _names)
		{
			if ((int)name.Value == symbol)
			{
				return ((XmlQualifiedName)name.Key).ToString();
			}
		}
		if (_wildcards != null)
		{
			foreach (DictionaryEntry wildcard in _wildcards)
			{
				if ((int)wildcard.Value == symbol)
				{
					return (string)wildcard.Key + ":*";
				}
			}
		}
		return "##other:*";
	}
}
