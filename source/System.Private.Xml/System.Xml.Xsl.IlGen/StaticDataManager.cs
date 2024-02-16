using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal sealed class StaticDataManager
{
	private UniqueList<string> _uniqueNames;

	private UniqueList<Int32Pair> _uniqueFilters;

	private List<StringPair[]> _prefixMappingsList;

	private List<string> _globalNames;

	private UniqueList<EarlyBoundInfo> _earlyInfo;

	private UniqueList<XmlQueryType> _uniqueXmlTypes;

	private UniqueList<XmlCollation> _uniqueCollations;

	public string[] Names
	{
		get
		{
			if (_uniqueNames == null)
			{
				return null;
			}
			return _uniqueNames.ToArray();
		}
	}

	public Int32Pair[] NameFilters
	{
		get
		{
			if (_uniqueFilters == null)
			{
				return null;
			}
			return _uniqueFilters.ToArray();
		}
	}

	public StringPair[][] PrefixMappingsList
	{
		get
		{
			if (_prefixMappingsList == null)
			{
				return null;
			}
			return _prefixMappingsList.ToArray();
		}
	}

	public string[] GlobalNames
	{
		get
		{
			if (_globalNames == null)
			{
				return null;
			}
			return _globalNames.ToArray();
		}
	}

	public EarlyBoundInfo[] EarlyBound
	{
		get
		{
			if (_earlyInfo != null)
			{
				return _earlyInfo.ToArray();
			}
			return null;
		}
	}

	public XmlQueryType[] XmlTypes
	{
		get
		{
			if (_uniqueXmlTypes == null)
			{
				return null;
			}
			return _uniqueXmlTypes.ToArray();
		}
	}

	public XmlCollation[] Collations
	{
		get
		{
			if (_uniqueCollations == null)
			{
				return null;
			}
			return _uniqueCollations.ToArray();
		}
	}

	public int DeclareName(string name)
	{
		if (_uniqueNames == null)
		{
			_uniqueNames = new UniqueList<string>();
		}
		return _uniqueNames.Add(name);
	}

	public int DeclareNameFilter(string locName, string nsUri)
	{
		if (_uniqueFilters == null)
		{
			_uniqueFilters = new UniqueList<Int32Pair>();
		}
		return _uniqueFilters.Add(new Int32Pair(DeclareName(locName), DeclareName(nsUri)));
	}

	public int DeclarePrefixMappings(IList<QilNode> list)
	{
		StringPair[] array = new StringPair[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			QilBinary qilBinary = (QilBinary)list[i];
			array[i] = new StringPair((QilLiteral)qilBinary.Left, (QilLiteral)qilBinary.Right);
		}
		if (_prefixMappingsList == null)
		{
			_prefixMappingsList = new List<StringPair[]>();
		}
		_prefixMappingsList.Add(array);
		return _prefixMappingsList.Count - 1;
	}

	public int DeclareGlobalValue(string name)
	{
		if (_globalNames == null)
		{
			_globalNames = new List<string>();
		}
		int count = _globalNames.Count;
		_globalNames.Add(name);
		return count;
	}

	public int DeclareEarlyBound(string namespaceUri, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type ebType)
	{
		if (_earlyInfo == null)
		{
			_earlyInfo = new UniqueList<EarlyBoundInfo>();
		}
		return _earlyInfo.Add(new EarlyBoundInfo(namespaceUri, ebType));
	}

	public int DeclareXmlType(XmlQueryType type)
	{
		if (_uniqueXmlTypes == null)
		{
			_uniqueXmlTypes = new UniqueList<XmlQueryType>();
		}
		return _uniqueXmlTypes.Add(type);
	}

	public int DeclareCollation(string collation)
	{
		if (_uniqueCollations == null)
		{
			_uniqueCollations = new UniqueList<XmlCollation>();
		}
		return _uniqueCollations.Add(XmlCollation.Create(collation));
	}
}
