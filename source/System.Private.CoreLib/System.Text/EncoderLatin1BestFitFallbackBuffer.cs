namespace System.Text;

internal sealed class EncoderLatin1BestFitFallbackBuffer : EncoderFallbackBuffer
{
	private char _cBestFit;

	private int _iCount = -1;

	private int _iSize;

	private static readonly char[] s_arrayCharBestFit = new char[604]
	{
		'Ā', 'A', 'ā', 'a', 'Ă', 'A', 'ă', 'a', 'Ą', 'A',
		'ą', 'a', 'Ć', 'C', 'ć', 'c', 'Ĉ', 'C', 'ĉ', 'c',
		'Ċ', 'C', 'ċ', 'c', 'Č', 'C', 'č', 'c', 'Ď', 'D',
		'ď', 'd', 'Đ', 'D', 'đ', 'd', 'Ē', 'E', 'ē', 'e',
		'Ĕ', 'E', 'ĕ', 'e', 'Ė', 'E', 'ė', 'e', 'Ę', 'E',
		'ę', 'e', 'Ě', 'E', 'ě', 'e', 'Ĝ', 'G', 'ĝ', 'g',
		'Ğ', 'G', 'ğ', 'g', 'Ġ', 'G', 'ġ', 'g', 'Ģ', 'G',
		'ģ', 'g', 'Ĥ', 'H', 'ĥ', 'h', 'Ħ', 'H', 'ħ', 'h',
		'Ĩ', 'I', 'ĩ', 'i', 'Ī', 'I', 'ī', 'i', 'Ĭ', 'I',
		'ĭ', 'i', 'Į', 'I', 'į', 'i', 'İ', 'I', 'ı', 'i',
		'Ĵ', 'J', 'ĵ', 'j', 'Ķ', 'K', 'ķ', 'k', 'Ĺ', 'L',
		'ĺ', 'l', 'Ļ', 'L', 'ļ', 'l', 'Ľ', 'L', 'ľ', 'l',
		'Ł', 'L', 'ł', 'l', 'Ń', 'N', 'ń', 'n', 'Ņ', 'N',
		'ņ', 'n', 'Ň', 'N', 'ň', 'n', 'Ō', 'O', 'ō', 'o',
		'Ŏ', 'O', 'ŏ', 'o', 'Ő', 'O', 'ő', 'o', 'Œ', 'O',
		'œ', 'o', 'Ŕ', 'R', 'ŕ', 'r', 'Ŗ', 'R', 'ŗ', 'r',
		'Ř', 'R', 'ř', 'r', 'Ś', 'S', 'ś', 's', 'Ŝ', 'S',
		'ŝ', 's', 'Ş', 'S', 'ş', 's', 'Š', 'S', 'š', 's',
		'Ţ', 'T', 'ţ', 't', 'Ť', 'T', 'ť', 't', 'Ŧ', 'T',
		'ŧ', 't', 'Ũ', 'U', 'ũ', 'u', 'Ū', 'U', 'ū', 'u',
		'Ŭ', 'U', 'ŭ', 'u', 'Ů', 'U', 'ů', 'u', 'Ű', 'U',
		'ű', 'u', 'Ų', 'U', 'ų', 'u', 'Ŵ', 'W', 'ŵ', 'w',
		'Ŷ', 'Y', 'ŷ', 'y', 'Ÿ', 'Y', 'Ź', 'Z', 'ź', 'z',
		'Ż', 'Z', 'ż', 'z', 'Ž', 'Z', 'ž', 'z', 'ƀ', 'b',
		'Ɖ', 'D', 'Ƒ', 'F', 'ƒ', 'f', 'Ɨ', 'I', 'ƚ', 'l',
		'Ɵ', 'O', 'Ơ', 'O', 'ơ', 'o', 'ƫ', 't', 'Ʈ', 'T',
		'Ư', 'U', 'ư', 'u', 'ƶ', 'z', 'Ǎ', 'A', 'ǎ', 'a',
		'Ǐ', 'I', 'ǐ', 'i', 'Ǒ', 'O', 'ǒ', 'o', 'Ǔ', 'U',
		'ǔ', 'u', 'Ǖ', 'U', 'ǖ', 'u', 'Ǘ', 'U', 'ǘ', 'u',
		'Ǚ', 'U', 'ǚ', 'u', 'Ǜ', 'U', 'ǜ', 'u', 'Ǟ', 'A',
		'ǟ', 'a', 'Ǥ', 'G', 'ǥ', 'g', 'Ǧ', 'G', 'ǧ', 'g',
		'Ǩ', 'K', 'ǩ', 'k', 'Ǫ', 'O', 'ǫ', 'o', 'Ǭ', 'O',
		'ǭ', 'o', 'ǰ', 'j', 'ɡ', 'g', 'ʹ', '\'', 'ʺ', '"',
		'ʼ', '\'', '\u02c4', '^', 'ˆ', '^', 'ˈ', '\'', 'ˉ', '?',
		'ˊ', '?', 'ˋ', '`', 'ˍ', '_', '\u02da', '?', '\u02dc', '~',
		'\u0300', '`', '\u0302', '^', '\u0303', '~', '\u030e', '"', '\u0331', '_',
		'\u0332', '_', '\u2000', ' ', '\u2001', ' ', '\u2002', ' ', '\u2003', ' ',
		'\u2004', ' ', '\u2005', ' ', '\u2006', ' ', '‐', '-', '‑', '-',
		'–', '-', '—', '-', '‘', '\'', '’', '\'', '‚', ',',
		'“', '"', '”', '"', '„', '"', '†', '?', '‡', '?',
		'•', '.', '…', '.', '‰', '?', '′', '\'', '‵', '`',
		'‹', '<', '›', '>', '™', 'T', '！', '!', '＂', '"',
		'＃', '#', '＄', '$', '％', '%', '＆', '&', '＇', '\'',
		'（', '(', '）', ')', '＊', '*', '＋', '+', '，', ',',
		'－', '-', '．', '.', '／', '/', '０', '0', '１', '1',
		'２', '2', '３', '3', '４', '4', '５', '5', '６', '6',
		'７', '7', '８', '8', '９', '9', '：', ':', '；', ';',
		'＜', '<', '＝', '=', '＞', '>', '？', '?', '＠', '@',
		'Ａ', 'A', 'Ｂ', 'B', 'Ｃ', 'C', 'Ｄ', 'D', 'Ｅ', 'E',
		'Ｆ', 'F', 'Ｇ', 'G', 'Ｈ', 'H', 'Ｉ', 'I', 'Ｊ', 'J',
		'Ｋ', 'K', 'Ｌ', 'L', 'Ｍ', 'M', 'Ｎ', 'N', 'Ｏ', 'O',
		'Ｐ', 'P', 'Ｑ', 'Q', 'Ｒ', 'R', 'Ｓ', 'S', 'Ｔ', 'T',
		'Ｕ', 'U', 'Ｖ', 'V', 'Ｗ', 'W', 'Ｘ', 'X', 'Ｙ', 'Y',
		'Ｚ', 'Z', '［', '[', '＼', '\\', '］', ']', '\uff3e', '^',
		'\uff3f', '_', '\uff40', '`', 'ａ', 'a', 'ｂ', 'b', 'ｃ', 'c',
		'ｄ', 'd', 'ｅ', 'e', 'ｆ', 'f', 'ｇ', 'g', 'ｈ', 'h',
		'ｉ', 'i', 'ｊ', 'j', 'ｋ', 'k', 'ｌ', 'l', 'ｍ', 'm',
		'ｎ', 'n', 'ｏ', 'o', 'ｐ', 'p', 'ｑ', 'q', 'ｒ', 'r',
		'ｓ', 's', 'ｔ', 't', 'ｕ', 'u', 'ｖ', 'v', 'ｗ', 'w',
		'ｘ', 'x', 'ｙ', 'y', 'ｚ', 'z', '｛', '{', '｜', '|',
		'｝', '}', '～', '~'
	};

	public override int Remaining
	{
		get
		{
			if (_iCount <= 0)
			{
				return 0;
			}
			return _iCount;
		}
	}

	public override bool Fallback(char charUnknown, int index)
	{
		_iCount = (_iSize = 1);
		_cBestFit = TryBestFit(charUnknown);
		if (_cBestFit == '\0')
		{
			_cBestFit = '?';
		}
		return true;
	}

	public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
	{
		if (!char.IsHighSurrogate(charUnknownHigh))
		{
			throw new ArgumentOutOfRangeException("charUnknownHigh", SR.Format(SR.ArgumentOutOfRange_Range, 55296, 56319));
		}
		if (!char.IsLowSurrogate(charUnknownLow))
		{
			throw new ArgumentOutOfRangeException("charUnknownLow", SR.Format(SR.ArgumentOutOfRange_Range, 56320, 57343));
		}
		_cBestFit = '?';
		_iCount = (_iSize = 2);
		return true;
	}

	public override char GetNextChar()
	{
		_iCount--;
		if (_iCount < 0)
		{
			return '\0';
		}
		if (_iCount == int.MaxValue)
		{
			_iCount = -1;
			return '\0';
		}
		return _cBestFit;
	}

	public override bool MovePrevious()
	{
		if (_iCount >= 0)
		{
			_iCount++;
		}
		if (_iCount >= 0)
		{
			return _iCount <= _iSize;
		}
		return false;
	}

	public unsafe override void Reset()
	{
		_iCount = -1;
		charStart = null;
		bFallingBack = false;
	}

	private static char TryBestFit(char cUnknown)
	{
		int num = 0;
		int num2 = s_arrayCharBestFit.Length;
		int num3;
		while ((num3 = num2 - num) > 6)
		{
			int num4 = (num3 / 2 + num) & 0xFFFE;
			char c = s_arrayCharBestFit[num4];
			if (c == cUnknown)
			{
				return s_arrayCharBestFit[num4 + 1];
			}
			if (c < cUnknown)
			{
				num = num4;
			}
			else
			{
				num2 = num4;
			}
		}
		for (int num4 = num; num4 < num2; num4 += 2)
		{
			if (s_arrayCharBestFit[num4] == cUnknown)
			{
				return s_arrayCharBestFit[num4 + 1];
			}
		}
		return '\0';
	}
}
