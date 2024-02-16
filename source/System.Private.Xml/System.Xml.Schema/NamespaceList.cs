using System.Collections;
using System.Text;

namespace System.Xml.Schema;

internal class NamespaceList
{
	public enum ListType
	{
		Any,
		Other,
		Set
	}

	private ListType _type;

	private Hashtable _set;

	private readonly string _targetNamespace;

	public ListType Type => _type;

	public string Excluded => _targetNamespace;

	public ICollection Enumerate
	{
		get
		{
			ListType type = _type;
			if ((uint)type > 1u && type == ListType.Set)
			{
				return _set.Keys;
			}
			throw new InvalidOperationException();
		}
	}

	public NamespaceList()
	{
	}

	public NamespaceList(string namespaces, string targetNamespace)
	{
		_targetNamespace = targetNamespace;
		namespaces = namespaces.Trim();
		if (namespaces == "##any" || namespaces.Length == 0)
		{
			_type = ListType.Any;
			return;
		}
		if (namespaces == "##other")
		{
			_type = ListType.Other;
			return;
		}
		_type = ListType.Set;
		_set = new Hashtable();
		string[] array = XmlConvert.SplitString(namespaces);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == "##local")
			{
				_set[string.Empty] = string.Empty;
				continue;
			}
			if (array[i] == "##targetNamespace")
			{
				_set[targetNamespace] = targetNamespace;
				continue;
			}
			XmlConvert.ToUri(array[i]);
			_set[array[i]] = array[i];
		}
	}

	public NamespaceList Clone()
	{
		NamespaceList namespaceList = (NamespaceList)MemberwiseClone();
		if (_type == ListType.Set)
		{
			namespaceList._set = (Hashtable)_set.Clone();
		}
		return namespaceList;
	}

	public virtual bool Allows(string ns)
	{
		switch (_type)
		{
		case ListType.Any:
			return true;
		case ListType.Other:
			if (ns != _targetNamespace)
			{
				return ns.Length != 0;
			}
			return false;
		case ListType.Set:
			return _set[ns] != null;
		default:
			return false;
		}
	}

	public bool Allows(XmlQualifiedName qname)
	{
		return Allows(qname.Namespace);
	}

	public override string ToString()
	{
		switch (_type)
		{
		case ListType.Any:
			return "##any";
		case ListType.Other:
			return "##other";
		case ListType.Set:
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = true;
			foreach (string key in _set.Keys)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					stringBuilder.Append(' ');
				}
				if (key == _targetNamespace)
				{
					stringBuilder.Append("##targetNamespace");
				}
				else if (key.Length == 0)
				{
					stringBuilder.Append("##local");
				}
				else
				{
					stringBuilder.Append(key);
				}
			}
			return stringBuilder.ToString();
		}
		default:
			return string.Empty;
		}
	}

	public static bool IsSubset(NamespaceList sub, NamespaceList super)
	{
		if (super._type == ListType.Any)
		{
			return true;
		}
		if (sub._type == ListType.Other && super._type == ListType.Other)
		{
			return super._targetNamespace == sub._targetNamespace;
		}
		if (sub._type == ListType.Set)
		{
			if (super._type == ListType.Other)
			{
				return !sub._set.Contains(super._targetNamespace);
			}
			foreach (string key in sub._set.Keys)
			{
				if (!super._set.Contains(key))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public static NamespaceList Union(NamespaceList o1, NamespaceList o2, bool v1Compat)
	{
		NamespaceList namespaceList = null;
		if (o1._type == ListType.Any)
		{
			namespaceList = new NamespaceList();
		}
		else if (o2._type == ListType.Any)
		{
			namespaceList = new NamespaceList();
		}
		else if (o1._type == ListType.Set && o2._type == ListType.Set)
		{
			namespaceList = o1.Clone();
			foreach (string key in o2._set.Keys)
			{
				namespaceList._set[key] = key;
			}
		}
		else if (o1._type == ListType.Other && o2._type == ListType.Other)
		{
			namespaceList = ((!(o1._targetNamespace == o2._targetNamespace)) ? new NamespaceList("##other", string.Empty) : o1.Clone());
		}
		else if (o1._type == ListType.Set && o2._type == ListType.Other)
		{
			namespaceList = (v1Compat ? ((!o1._set.Contains(o2._targetNamespace)) ? o2.Clone() : new NamespaceList()) : ((o2._targetNamespace != string.Empty) ? o1.CompareSetToOther(o2) : ((!o1._set.Contains(string.Empty)) ? new NamespaceList("##other", string.Empty) : new NamespaceList())));
		}
		else if (o2._type == ListType.Set && o1._type == ListType.Other)
		{
			namespaceList = (v1Compat ? ((!o2._set.Contains(o2._targetNamespace)) ? o1.Clone() : new NamespaceList()) : ((o1._targetNamespace != string.Empty) ? o2.CompareSetToOther(o1) : ((!o2._set.Contains(string.Empty)) ? new NamespaceList("##other", string.Empty) : new NamespaceList())));
		}
		return namespaceList;
	}

	private NamespaceList CompareSetToOther(NamespaceList other)
	{
		NamespaceList namespaceList = null;
		if (_set.Contains(other._targetNamespace))
		{
			if (_set.Contains(string.Empty))
			{
				return new NamespaceList();
			}
			return new NamespaceList("##other", string.Empty);
		}
		if (_set.Contains(string.Empty))
		{
			return null;
		}
		return other.Clone();
	}

	public static NamespaceList Intersection(NamespaceList o1, NamespaceList o2, bool v1Compat)
	{
		NamespaceList namespaceList = null;
		if (o1._type == ListType.Any)
		{
			namespaceList = o2.Clone();
		}
		else if (o2._type == ListType.Any)
		{
			namespaceList = o1.Clone();
		}
		else if (o1._type == ListType.Set && o2._type == ListType.Other)
		{
			namespaceList = o1.Clone();
			namespaceList.RemoveNamespace(o2._targetNamespace);
			if (!v1Compat)
			{
				namespaceList.RemoveNamespace(string.Empty);
			}
		}
		else if (o1._type == ListType.Other && o2._type == ListType.Set)
		{
			namespaceList = o2.Clone();
			namespaceList.RemoveNamespace(o1._targetNamespace);
			if (!v1Compat)
			{
				namespaceList.RemoveNamespace(string.Empty);
			}
		}
		else if (o1._type == ListType.Set && o2._type == ListType.Set)
		{
			namespaceList = o1.Clone();
			namespaceList = new NamespaceList();
			namespaceList._type = ListType.Set;
			namespaceList._set = new Hashtable();
			foreach (string key in o1._set.Keys)
			{
				if (o2._set.Contains(key))
				{
					namespaceList._set.Add(key, key);
				}
			}
		}
		else if (o1._type == ListType.Other && o2._type == ListType.Other)
		{
			if (o1._targetNamespace == o2._targetNamespace)
			{
				return o1.Clone();
			}
			if (!v1Compat)
			{
				if (o1._targetNamespace == string.Empty)
				{
					namespaceList = o2.Clone();
				}
				else if (o2._targetNamespace == string.Empty)
				{
					namespaceList = o1.Clone();
				}
			}
		}
		return namespaceList;
	}

	private void RemoveNamespace(string tns)
	{
		if (_set[tns] != null)
		{
			_set.Remove(tns);
		}
	}
}
