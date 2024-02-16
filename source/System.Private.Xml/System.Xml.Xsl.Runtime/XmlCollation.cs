using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class XmlCollation
{
	private struct Options
	{
		private int _value;

		public bool UpperFirst
		{
			get
			{
				return GetFlag(4096);
			}
			set
			{
				SetFlag(4096, value);
			}
		}

		public bool EmptyGreatest => GetFlag(8192);

		public bool DescendingOrder => GetFlag(16384);

		public bool IgnoreCase => GetFlag(1);

		public bool Ordinal => GetFlag(1073741824);

		public CompareOptions CompareOptions
		{
			get
			{
				return (CompareOptions)(_value & -28673);
			}
			set
			{
				_value = (_value & 0x7000) | (int)value;
			}
		}

		public Options(int value)
		{
			_value = value;
		}

		public bool GetFlag(int flag)
		{
			return (_value & flag) != 0;
		}

		public void SetFlag(int flag, bool value)
		{
			if (value)
			{
				_value |= flag;
			}
			else
			{
				_value &= ~flag;
			}
		}

		public static implicit operator int(Options options)
		{
			return options._value;
		}
	}

	private readonly CultureInfo _cultInfo;

	private Options _options;

	private readonly CompareOptions _compops;

	private static readonly XmlCollation s_cp = new XmlCollation(CultureInfo.InvariantCulture, new Options(1073741824));

	internal static XmlCollation CodePointCollation => s_cp;

	internal bool UpperFirst => _options.UpperFirst;

	internal bool EmptyGreatest => _options.EmptyGreatest;

	internal bool DescendingOrder => _options.DescendingOrder;

	internal CultureInfo Culture
	{
		get
		{
			if (_cultInfo == null)
			{
				return CultureInfo.CurrentCulture;
			}
			return _cultInfo;
		}
	}

	private XmlCollation(CultureInfo cultureInfo, Options options)
	{
		_cultInfo = cultureInfo;
		_options = options;
		_compops = options.CompareOptions;
	}

	internal static XmlCollation Create(string collationLiteral)
	{
		return Create(collationLiteral, throwOnError: true);
	}

	internal static XmlCollation Create(string collationLiteral, bool throwOnError)
	{
		if (collationLiteral == "http://www.w3.org/2004/10/xpath-functions/collation/codepoint")
		{
			return CodePointCollation;
		}
		CultureInfo cultureInfo = null;
		Options options = default(Options);
		Uri result;
		if (throwOnError)
		{
			result = new Uri(collationLiteral);
		}
		else if (!Uri.TryCreate(collationLiteral, UriKind.Absolute, out result))
		{
			return null;
		}
		string components = result.GetComponents(UriComponents.SchemeAndServer | UriComponents.UserInfo, UriFormat.UriEscaped);
		if (components == "http://collations.microsoft.com")
		{
			string text = result.LocalPath.Substring(1);
			if (text.Length != 0)
			{
				try
				{
					cultureInfo = new CultureInfo(text);
				}
				catch (ArgumentException)
				{
					if (!throwOnError)
					{
						return null;
					}
					throw new XslTransformException(System.SR.Coll_UnsupportedLanguage, text);
				}
			}
		}
		else
		{
			if (!result.IsBaseOf(new Uri("http://www.w3.org/2004/10/xpath-functions/collation/codepoint")))
			{
				if (!throwOnError)
				{
					return null;
				}
				throw new XslTransformException(System.SR.Coll_Unsupported, collationLiteral);
			}
			options.CompareOptions = CompareOptions.Ordinal;
		}
		string query = result.Query;
		string text2 = null;
		if (query.Length != 0)
		{
			string[] array = query.Substring(1).Split('&');
			foreach (string text3 in array)
			{
				string[] array2 = text3.Split('=');
				if (array2.Length != 2)
				{
					if (!throwOnError)
					{
						return null;
					}
					throw new XslTransformException(System.SR.Coll_BadOptFormat, text3);
				}
				string text4 = array2[0].ToUpperInvariant();
				string text5 = array2[1].ToUpperInvariant();
				if (text4 == "SORT")
				{
					text2 = text5;
					continue;
				}
				int flag;
				switch (text4)
				{
				case "IGNORECASE":
					flag = 1;
					break;
				case "IGNORENONSPACE":
					flag = 2;
					break;
				case "IGNORESYMBOLS":
					flag = 4;
					break;
				case "IGNOREKANATYPE":
					flag = 8;
					break;
				case "IGNOREWIDTH":
					flag = 16;
					break;
				case "UPPERFIRST":
					flag = 4096;
					break;
				case "EMPTYGREATEST":
					flag = 8192;
					break;
				case "DESCENDINGORDER":
					flag = 16384;
					break;
				default:
					if (!throwOnError)
					{
						return null;
					}
					throw new XslTransformException(System.SR.Coll_UnsupportedOpt, array2[0]);
				}
				switch (text5)
				{
				case "0":
				case "FALSE":
					options.SetFlag(flag, value: false);
					continue;
				case "1":
				case "TRUE":
					options.SetFlag(flag, value: true);
					continue;
				}
				if (!throwOnError)
				{
					return null;
				}
				throw new XslTransformException(System.SR.Coll_UnsupportedOptVal, array2[0], array2[1]);
			}
		}
		if (options.UpperFirst && options.IgnoreCase)
		{
			options.UpperFirst = false;
		}
		if (options.Ordinal)
		{
			options.CompareOptions = CompareOptions.Ordinal;
			options.UpperFirst = false;
		}
		if (text2 != null && cultureInfo != null)
		{
			string name = cultureInfo.Name;
			switch (text2)
			{
			case "bopo":
				if (name == "zh-TW")
				{
					cultureInfo = new CultureInfo("zh-TW_pronun");
				}
				break;
			case "strk":
				switch (name)
				{
				case "zh-CN":
				case "zh-HK":
				case "zh-SG":
				case "zh-MO":
					cultureInfo = new CultureInfo(name);
					break;
				}
				break;
			case "uni":
				if (name == "ja-JP" || name == "ko-KR")
				{
					cultureInfo = new CultureInfo(name);
				}
				break;
			case "phn":
				if (name == "de-DE")
				{
					cultureInfo = new CultureInfo("de-DE_phoneb");
				}
				break;
			case "tech":
				if (name == "hu-HU")
				{
					cultureInfo = new CultureInfo("hu-HU_technl");
				}
				break;
			case "mod":
				if (name == "ka-GE")
				{
					cultureInfo = new CultureInfo("ka-GE_modern");
				}
				break;
			default:
				if (!throwOnError)
				{
					return null;
				}
				throw new XslTransformException(System.SR.Coll_UnsupportedSortOpt, text2);
			case "pron":
			case "dict":
			case "trad":
				break;
			}
		}
		return new XmlCollation(cultureInfo, options);
	}

	public override bool Equals(object obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj is XmlCollation xmlCollation && (int)_options == (int)xmlCollation._options)
		{
			return object.Equals(_cultInfo, xmlCollation._cultInfo);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = _options;
		if (_cultInfo != null)
		{
			num ^= _cultInfo.GetHashCode();
		}
		return num;
	}

	internal void GetObjectData(BinaryWriter writer)
	{
		writer.Write((_cultInfo != null) ? _cultInfo.Name : "<!-- LOCALE CURRENT -->");
		writer.Write(_options);
	}

	internal XmlCollation(BinaryReader reader)
	{
		string text = reader.ReadString();
		_cultInfo = ((text != "<!-- LOCALE CURRENT -->") ? new CultureInfo(text) : null);
		_options = new Options(reader.ReadInt32());
		_compops = _options.CompareOptions;
	}

	internal XmlSortKey CreateSortKey(string s)
	{
		SortKey sortKey = Culture.CompareInfo.GetSortKey(s, _compops);
		if (!UpperFirst)
		{
			return new XmlStringSortKey(sortKey, DescendingOrder);
		}
		byte[] keyData = sortKey.KeyData;
		if (UpperFirst && keyData.Length != 0)
		{
			int i;
			for (i = 0; keyData[i] != 1; i++)
			{
			}
			do
			{
				i++;
			}
			while (keyData[i] != 1);
			do
			{
				i++;
				keyData[i] ^= byte.MaxValue;
			}
			while (keyData[i] != 254);
		}
		return new XmlStringSortKey(keyData, DescendingOrder);
	}
}
