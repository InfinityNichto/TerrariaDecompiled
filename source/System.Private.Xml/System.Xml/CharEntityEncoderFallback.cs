using System.Text;

namespace System.Xml;

internal sealed class CharEntityEncoderFallback : EncoderFallback
{
	private CharEntityEncoderFallbackBuffer _fallbackBuffer;

	private int[] _textContentMarks;

	private int _endMarkPos;

	private int _curMarkPos;

	private int _startOffset;

	public override int MaxCharCount => 12;

	internal int StartOffset
	{
		set
		{
			_startOffset = value;
		}
	}

	internal CharEntityEncoderFallback()
	{
	}

	public override EncoderFallbackBuffer CreateFallbackBuffer()
	{
		if (_fallbackBuffer == null)
		{
			_fallbackBuffer = new CharEntityEncoderFallbackBuffer(this);
		}
		return _fallbackBuffer;
	}

	internal void Reset(int[] textContentMarks, int endMarkPos)
	{
		_textContentMarks = textContentMarks;
		_endMarkPos = endMarkPos;
		_curMarkPos = 0;
	}

	internal bool CanReplaceAt(int index)
	{
		int i = _curMarkPos;
		for (int num = _startOffset + index; i < _endMarkPos && num >= _textContentMarks[i + 1]; i++)
		{
		}
		_curMarkPos = i;
		return (i & 1) != 0;
	}
}
