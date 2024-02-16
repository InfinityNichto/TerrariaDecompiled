using System.Collections.Generic;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct StringConcat
{
	private string _s1;

	private string _s2;

	private string _s3;

	private string _s4;

	private string _delimiter;

	private List<string> _strList;

	private int _idxStr;

	public string? Delimiter
	{
		get
		{
			return _delimiter;
		}
		set
		{
			_delimiter = value;
		}
	}

	internal int Count => _idxStr;

	public void Clear()
	{
		_idxStr = 0;
		_delimiter = null;
	}

	public void Concat(string value)
	{
		if (_delimiter != null && _idxStr != 0)
		{
			ConcatNoDelimiter(_delimiter);
		}
		ConcatNoDelimiter(value);
	}

	public string GetResult()
	{
		return _idxStr switch
		{
			0 => string.Empty, 
			1 => _s1 ?? string.Empty, 
			2 => _s1 + _s2, 
			3 => _s1 + _s2 + _s3, 
			4 => _s1 + _s2 + _s3 + _s4, 
			_ => string.Concat(_strList.ToArray()), 
		};
	}

	internal void ConcatNoDelimiter(string s)
	{
		switch (_idxStr)
		{
		case 0:
			_s1 = s;
			break;
		case 1:
			_s2 = s;
			break;
		case 2:
			_s3 = s;
			break;
		case 3:
			_s4 = s;
			break;
		case 4:
		{
			int capacity = ((_strList == null) ? 8 : _strList.Count);
			List<string> list = (_strList = new List<string>(capacity));
			list.Add(_s1);
			list.Add(_s2);
			list.Add(_s3);
			list.Add(_s4);
			goto default;
		}
		default:
			_strList.Add(s);
			break;
		}
		_idxStr++;
	}
}
