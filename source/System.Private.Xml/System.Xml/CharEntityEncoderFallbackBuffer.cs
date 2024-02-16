using System.Runtime.CompilerServices;
using System.Text;

namespace System.Xml;

internal sealed class CharEntityEncoderFallbackBuffer : EncoderFallbackBuffer
{
	private readonly CharEntityEncoderFallback _parent;

	private string _charEntity = string.Empty;

	private int _charEntityIndex = -1;

	public override int Remaining
	{
		get
		{
			if (_charEntityIndex == -1)
			{
				return 0;
			}
			return _charEntity.Length - _charEntityIndex;
		}
	}

	internal CharEntityEncoderFallbackBuffer(CharEntityEncoderFallback parent)
	{
		_parent = parent;
	}

	public override bool Fallback(char charUnknown, int index)
	{
		if (_charEntityIndex >= 0)
		{
			new EncoderExceptionFallback().CreateFallbackBuffer().Fallback(charUnknown, index);
		}
		if (_parent.CanReplaceAt(index))
		{
			IFormatProvider formatProvider = null;
			IFormatProvider provider = formatProvider;
			Span<char> initialBuffer = stackalloc char[64];
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 1, formatProvider, initialBuffer);
			handler.AppendLiteral("&#x");
			handler.AppendFormatted((int)charUnknown, "X");
			handler.AppendLiteral(";");
			_charEntity = string.Create(provider, initialBuffer, ref handler);
			_charEntityIndex = 0;
			return true;
		}
		EncoderFallbackBuffer encoderFallbackBuffer = new EncoderExceptionFallback().CreateFallbackBuffer();
		encoderFallbackBuffer.Fallback(charUnknown, index);
		return false;
	}

	public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
	{
		if (!char.IsSurrogatePair(charUnknownHigh, charUnknownLow))
		{
			throw XmlConvert.CreateInvalidSurrogatePairException(charUnknownHigh, charUnknownLow);
		}
		if (_charEntityIndex >= 0)
		{
			new EncoderExceptionFallback().CreateFallbackBuffer().Fallback(charUnknownHigh, charUnknownLow, index);
		}
		if (_parent.CanReplaceAt(index))
		{
			IFormatProvider formatProvider = null;
			IFormatProvider provider = formatProvider;
			Span<char> initialBuffer = stackalloc char[64];
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(4, 1, formatProvider, initialBuffer);
			handler.AppendLiteral("&#x");
			handler.AppendFormatted(SurrogateCharToUtf32(charUnknownHigh, charUnknownLow), "X");
			handler.AppendLiteral(";");
			_charEntity = string.Create(provider, initialBuffer, ref handler);
			_charEntityIndex = 0;
			return true;
		}
		EncoderFallbackBuffer encoderFallbackBuffer = new EncoderExceptionFallback().CreateFallbackBuffer();
		encoderFallbackBuffer.Fallback(charUnknownHigh, charUnknownLow, index);
		return false;
	}

	public override char GetNextChar()
	{
		if (_charEntityIndex == _charEntity.Length)
		{
			_charEntityIndex = -1;
		}
		if (_charEntityIndex == -1)
		{
			return '\0';
		}
		return _charEntity[_charEntityIndex++];
	}

	public override bool MovePrevious()
	{
		if (_charEntityIndex == -1)
		{
			return false;
		}
		if (_charEntityIndex > 0)
		{
			_charEntityIndex--;
			return true;
		}
		return false;
	}

	public override void Reset()
	{
		_charEntityIndex = -1;
	}

	private int SurrogateCharToUtf32(char highSurrogate, char lowSurrogate)
	{
		return XmlCharType.CombineSurrogateChar(lowSurrogate, highSurrogate);
	}
}
