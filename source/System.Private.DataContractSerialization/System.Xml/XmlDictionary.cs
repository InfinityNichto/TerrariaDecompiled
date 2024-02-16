using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Xml;

public class XmlDictionary : IXmlDictionary
{
	private sealed class EmptyDictionary : IXmlDictionary
	{
		public bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString result)
		{
			result = null;
			return false;
		}

		public bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString result)
		{
			result = null;
			return false;
		}

		public bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString result)
		{
			result = null;
			return false;
		}
	}

	private static IXmlDictionary s_empty;

	private readonly Dictionary<string, XmlDictionaryString> _lookup;

	private XmlDictionaryString[] _strings;

	private int _nextId;

	public static IXmlDictionary Empty
	{
		get
		{
			if (s_empty == null)
			{
				s_empty = new EmptyDictionary();
			}
			return s_empty;
		}
	}

	public XmlDictionary()
	{
		_lookup = new Dictionary<string, XmlDictionaryString>();
		_strings = null;
		_nextId = 0;
	}

	public XmlDictionary(int capacity)
	{
		_lookup = new Dictionary<string, XmlDictionaryString>(capacity);
		_strings = new XmlDictionaryString[capacity];
		_nextId = 0;
	}

	public virtual XmlDictionaryString Add(string value)
	{
		if (!_lookup.TryGetValue(value, out var value2))
		{
			if (_strings == null)
			{
				_strings = new XmlDictionaryString[4];
			}
			else if (_nextId == _strings.Length)
			{
				int num = _nextId * 2;
				if (num == 0)
				{
					num = 4;
				}
				Array.Resize(ref _strings, num);
			}
			value2 = new XmlDictionaryString(this, value, _nextId);
			_strings[_nextId] = value2;
			_lookup.Add(value, value2);
			_nextId++;
		}
		return value2;
	}

	public virtual bool TryLookup(string value, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		return _lookup.TryGetValue(value, out result);
	}

	public virtual bool TryLookup(int key, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		if (key < 0 || key >= _nextId)
		{
			result = null;
			return false;
		}
		result = _strings[key];
		return true;
	}

	public virtual bool TryLookup(XmlDictionaryString value, [NotNullWhen(true)] out XmlDictionaryString? result)
	{
		if (value == null)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
		}
		if (value.Dictionary != this)
		{
			result = null;
			return false;
		}
		result = value;
		return true;
	}
}
