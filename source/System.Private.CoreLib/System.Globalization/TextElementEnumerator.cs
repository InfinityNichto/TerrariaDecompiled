using System.Collections;
using System.Text.Unicode;

namespace System.Globalization;

public class TextElementEnumerator : IEnumerator
{
	private readonly string _str;

	private readonly int _strStartIndex;

	private int _currentTextElementOffset;

	private int _currentTextElementLength;

	private string _currentTextElementSubstr;

	public object Current => GetTextElement();

	public int ElementIndex
	{
		get
		{
			if (_currentTextElementOffset >= _str.Length)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
			return _currentTextElementOffset - _strStartIndex;
		}
	}

	internal TextElementEnumerator(string str, int startIndex)
	{
		_str = str;
		_strStartIndex = startIndex;
		Reset();
	}

	public bool MoveNext()
	{
		_currentTextElementSubstr = null;
		int num = (_currentTextElementOffset += _currentTextElementLength);
		_currentTextElementLength = 0;
		if (num >= _str.Length)
		{
			return false;
		}
		_currentTextElementLength = TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(_str.AsSpan(num));
		return true;
	}

	public string GetTextElement()
	{
		string text = _currentTextElementSubstr;
		if (text == null)
		{
			if (_currentTextElementOffset >= _str.Length)
			{
				throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
			}
			text = (_currentTextElementSubstr = _str.Substring(_currentTextElementOffset, _currentTextElementLength));
		}
		return text;
	}

	public void Reset()
	{
		_currentTextElementOffset = _str.Length;
		_currentTextElementLength = _strStartIndex - _str.Length;
		_currentTextElementSubstr = null;
	}
}
