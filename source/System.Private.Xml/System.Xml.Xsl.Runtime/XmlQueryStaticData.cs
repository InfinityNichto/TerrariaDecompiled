using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlQueryStaticData
{
	private readonly XmlWriterSettings _defaultWriterSettings;

	private readonly IList<WhitespaceRule> _whitespaceRules;

	private readonly string[] _names;

	private readonly StringPair[][] _prefixMappingsList;

	private readonly Int32Pair[] _filters;

	private readonly XmlQueryType[] _types;

	private readonly XmlCollation[] _collations;

	private readonly string[] _globalNames;

	private readonly EarlyBoundInfo[] _earlyBound;

	public XmlWriterSettings DefaultWriterSettings => _defaultWriterSettings;

	public IList<WhitespaceRule> WhitespaceRules => _whitespaceRules;

	public string[] Names => _names;

	public StringPair[][] PrefixMappingsList => _prefixMappingsList;

	public Int32Pair[] Filters => _filters;

	public XmlQueryType[] Types => _types;

	public XmlCollation[] Collations => _collations;

	public string[] GlobalNames => _globalNames;

	public EarlyBoundInfo[] EarlyBound => _earlyBound;

	[RequiresUnreferencedCode("This method will create a copy that uses earlybound types which cannot be statically analyzed.")]
	public XmlQueryStaticData(XmlWriterSettings defaultWriterSettings, IList<WhitespaceRule> whitespaceRules, StaticDataManager staticData)
	{
		_defaultWriterSettings = defaultWriterSettings;
		_whitespaceRules = whitespaceRules;
		_names = staticData.Names;
		_prefixMappingsList = staticData.PrefixMappingsList;
		_filters = staticData.NameFilters;
		_types = staticData.XmlTypes;
		_collations = staticData.Collations;
		_globalNames = staticData.GlobalNames;
		_earlyBound = staticData.EarlyBound;
	}

	[RequiresUnreferencedCode("This method will create EarlyBoundInfo from passed in ebTypes array which cannot be statically analyzed.")]
	public XmlQueryStaticData(byte[] data, Type[] ebTypes)
	{
		MemoryStream input = new MemoryStream(data, writable: false);
		XmlQueryDataReader xmlQueryDataReader = new XmlQueryDataReader(input);
		int num = xmlQueryDataReader.Read7BitEncodedInt();
		if ((num & -256) > 0)
		{
			throw new NotSupportedException();
		}
		_defaultWriterSettings = new XmlWriterSettings(xmlQueryDataReader);
		int num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_whitespaceRules = new WhitespaceRule[num2];
			for (int i = 0; i < num2; i++)
			{
				_whitespaceRules[i] = new WhitespaceRule(xmlQueryDataReader);
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_names = new string[num2];
			for (int j = 0; j < num2; j++)
			{
				_names[j] = xmlQueryDataReader.ReadString();
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_prefixMappingsList = new StringPair[num2][];
			for (int k = 0; k < num2; k++)
			{
				int num3 = xmlQueryDataReader.ReadInt32();
				_prefixMappingsList[k] = new StringPair[num3];
				for (int l = 0; l < num3; l++)
				{
					_prefixMappingsList[k][l] = new StringPair(xmlQueryDataReader.ReadString(), xmlQueryDataReader.ReadString());
				}
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_filters = new Int32Pair[num2];
			for (int m = 0; m < num2; m++)
			{
				_filters[m] = new Int32Pair(xmlQueryDataReader.Read7BitEncodedInt(), xmlQueryDataReader.Read7BitEncodedInt());
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_types = new XmlQueryType[num2];
			for (int n = 0; n < num2; n++)
			{
				_types[n] = XmlQueryTypeFactory.Deserialize(xmlQueryDataReader);
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_collations = new XmlCollation[num2];
			for (int num4 = 0; num4 < num2; num4++)
			{
				_collations[num4] = new XmlCollation(xmlQueryDataReader);
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_globalNames = new string[num2];
			for (int num5 = 0; num5 < num2; num5++)
			{
				_globalNames[num5] = xmlQueryDataReader.ReadString();
			}
		}
		num2 = xmlQueryDataReader.ReadInt32();
		if (num2 != 0)
		{
			_earlyBound = new EarlyBoundInfo[num2];
			for (int num6 = 0; num6 < num2; num6++)
			{
				_earlyBound[num6] = new EarlyBoundInfo(xmlQueryDataReader.ReadString(), ebTypes[num6]);
			}
		}
		xmlQueryDataReader.Dispose();
	}

	public void GetObjectData(out byte[] data, out Type[] ebTypes)
	{
		MemoryStream memoryStream = new MemoryStream(4096);
		XmlQueryDataWriter xmlQueryDataWriter = new XmlQueryDataWriter(memoryStream);
		xmlQueryDataWriter.Write7BitEncodedInt(0);
		_defaultWriterSettings.GetObjectData(xmlQueryDataWriter);
		if (_whitespaceRules == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_whitespaceRules.Count);
			foreach (WhitespaceRule whitespaceRule in _whitespaceRules)
			{
				whitespaceRule.GetObjectData(xmlQueryDataWriter);
			}
		}
		if (_names == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_names.Length);
			string[] names = _names;
			foreach (string value in names)
			{
				xmlQueryDataWriter.Write(value);
			}
		}
		if (_prefixMappingsList == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_prefixMappingsList.Length);
			StringPair[][] prefixMappingsList = _prefixMappingsList;
			foreach (StringPair[] array in prefixMappingsList)
			{
				xmlQueryDataWriter.Write(array.Length);
				StringPair[] array2 = array;
				for (int k = 0; k < array2.Length; k++)
				{
					StringPair stringPair = array2[k];
					xmlQueryDataWriter.Write(stringPair.Left);
					xmlQueryDataWriter.Write(stringPair.Right);
				}
			}
		}
		if (_filters == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_filters.Length);
			Int32Pair[] filters = _filters;
			for (int l = 0; l < filters.Length; l++)
			{
				Int32Pair int32Pair = filters[l];
				xmlQueryDataWriter.Write7BitEncodedInt(int32Pair.Left);
				xmlQueryDataWriter.Write7BitEncodedInt(int32Pair.Right);
			}
		}
		if (_types == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_types.Length);
			XmlQueryType[] types = _types;
			foreach (XmlQueryType type in types)
			{
				XmlQueryTypeFactory.Serialize(xmlQueryDataWriter, type);
			}
		}
		if (_collations == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_collations.Length);
			XmlCollation[] collations = _collations;
			foreach (XmlCollation xmlCollation in collations)
			{
				xmlCollation.GetObjectData(xmlQueryDataWriter);
			}
		}
		if (_globalNames == null)
		{
			xmlQueryDataWriter.Write(0);
		}
		else
		{
			xmlQueryDataWriter.Write(_globalNames.Length);
			string[] globalNames = _globalNames;
			foreach (string value2 in globalNames)
			{
				xmlQueryDataWriter.Write(value2);
			}
		}
		if (_earlyBound == null)
		{
			xmlQueryDataWriter.Write(0);
			ebTypes = null;
		}
		else
		{
			xmlQueryDataWriter.Write(_earlyBound.Length);
			ebTypes = new Type[_earlyBound.Length];
			int num2 = 0;
			EarlyBoundInfo[] earlyBound = _earlyBound;
			foreach (EarlyBoundInfo earlyBoundInfo in earlyBound)
			{
				xmlQueryDataWriter.Write(earlyBoundInfo.NamespaceUri);
				ebTypes[num2++] = earlyBoundInfo.EarlyBoundType;
			}
		}
		xmlQueryDataWriter.Dispose();
		data = memoryStream.ToArray();
	}
}
