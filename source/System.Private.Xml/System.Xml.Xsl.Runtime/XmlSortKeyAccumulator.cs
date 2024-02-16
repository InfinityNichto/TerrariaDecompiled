using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XmlSortKeyAccumulator
{
	private XmlSortKey[] _keys;

	private int _pos;

	public Array Keys => _keys;

	public void Create()
	{
		if (_keys == null)
		{
			_keys = new XmlSortKey[64];
		}
		_pos = 0;
		_keys[0] = null;
	}

	public void AddStringSortKey(XmlCollation collation, string value)
	{
		AppendSortKey(collation.CreateSortKey(value));
	}

	public void AddDecimalSortKey(XmlCollation collation, decimal value)
	{
		AppendSortKey(new XmlDecimalSortKey(value, collation));
	}

	public void AddIntegerSortKey(XmlCollation collation, long value)
	{
		AppendSortKey(new XmlIntegerSortKey(value, collation));
	}

	public void AddIntSortKey(XmlCollation collation, int value)
	{
		AppendSortKey(new XmlIntSortKey(value, collation));
	}

	public void AddDoubleSortKey(XmlCollation collation, double value)
	{
		AppendSortKey(new XmlDoubleSortKey(value, collation));
	}

	public void AddDateTimeSortKey(XmlCollation collation, DateTime value)
	{
		AppendSortKey(new XmlDateTimeSortKey(value, collation));
	}

	public void AddEmptySortKey(XmlCollation collation)
	{
		AppendSortKey(new XmlEmptySortKey(collation));
	}

	public void FinishSortKeys()
	{
		_pos++;
		if (_pos >= _keys.Length)
		{
			XmlSortKey[] array = new XmlSortKey[_pos * 2];
			Array.Copy(_keys, array, _keys.Length);
			_keys = array;
		}
		_keys[_pos] = null;
	}

	private void AppendSortKey(XmlSortKey key)
	{
		key.Priority = _pos;
		if (_keys[_pos] == null)
		{
			_keys[_pos] = key;
		}
		else
		{
			_keys[_pos].AddSortKey(key);
		}
	}
}
