using System.Buffers;
using System.Text.Unicode;

namespace System.Text.Encodings.Web;

internal sealed class DefaultUrlEncoder : UrlEncoder
{
	private sealed class EscaperImplementation : ScalarEscaperBase
	{
		internal static readonly EscaperImplementation Singleton = new EscaperImplementation();

		private EscaperImplementation()
		{
		}

		internal override int EncodeUtf8(Rune value, Span<byte> destination)
		{
			uint utf8RepresentationForScalarValue = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)value.Value);
			if (SpanUtility.IsValidIndex(destination, 2))
			{
				destination[0] = 37;
				System.HexConverter.ToBytesBuffer((byte)utf8RepresentationForScalarValue, destination, 1);
				if ((utf8RepresentationForScalarValue >>= 8) == 0)
				{
					return 3;
				}
				if (SpanUtility.IsValidIndex(destination, 5))
				{
					destination[3] = 37;
					System.HexConverter.ToBytesBuffer((byte)utf8RepresentationForScalarValue, destination, 4);
					if ((utf8RepresentationForScalarValue >>= 8) == 0)
					{
						return 6;
					}
					if (SpanUtility.IsValidIndex(destination, 8))
					{
						destination[6] = 37;
						System.HexConverter.ToBytesBuffer((byte)utf8RepresentationForScalarValue, destination, 7);
						if ((utf8RepresentationForScalarValue >>= 8) == 0)
						{
							return 9;
						}
						if (SpanUtility.IsValidIndex(destination, 11))
						{
							destination[9] = 37;
							System.HexConverter.ToBytesBuffer((byte)utf8RepresentationForScalarValue, destination, 10);
							return 12;
						}
					}
				}
			}
			return -1;
		}

		internal override int EncodeUtf16(Rune value, Span<char> destination)
		{
			uint utf8RepresentationForScalarValue = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)value.Value);
			if (SpanUtility.IsValidIndex(destination, 2))
			{
				destination[0] = '%';
				System.HexConverter.ToCharsBuffer((byte)utf8RepresentationForScalarValue, destination, 1);
				if ((utf8RepresentationForScalarValue >>= 8) == 0)
				{
					return 3;
				}
				if (SpanUtility.IsValidIndex(destination, 5))
				{
					destination[3] = '%';
					System.HexConverter.ToCharsBuffer((byte)utf8RepresentationForScalarValue, destination, 4);
					if ((utf8RepresentationForScalarValue >>= 8) == 0)
					{
						return 6;
					}
					if (SpanUtility.IsValidIndex(destination, 8))
					{
						destination[6] = '%';
						System.HexConverter.ToCharsBuffer((byte)utf8RepresentationForScalarValue, destination, 7);
						if ((utf8RepresentationForScalarValue >>= 8) == 0)
						{
							return 9;
						}
						if (SpanUtility.IsValidIndex(destination, 11))
						{
							destination[9] = '%';
							System.HexConverter.ToCharsBuffer((byte)utf8RepresentationForScalarValue, destination, 10);
							return 12;
						}
					}
				}
			}
			return -1;
		}
	}

	internal static readonly DefaultUrlEncoder BasicLatinSingleton = new DefaultUrlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

	private readonly OptimizedInboxTextEncoder _innerEncoder;

	public override int MaxOutputCharactersPerInputCharacter => 9;

	internal DefaultUrlEncoder(TextEncoderSettings settings)
	{
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		ScalarEscaperBase singleton = EscaperImplementation.Singleton;
		ref readonly AllowedBmpCodePointsBitmap allowedCodePointsBitmap = ref settings.GetAllowedCodePointsBitmap();
		Span<char> span = stackalloc char[31]
		{
			' ', '#', '%', '/', ':', '=', '?', '[', '\\', ']',
			'^', '`', '{', '|', '}', '\ufff0', '\ufff1', '\ufff2', '\ufff3', '\ufff4',
			'\ufff5', '\ufff6', '\ufff7', '\ufff8', '\ufff9', '\ufffa', '\ufffb', '￼', '\ufffd', '\ufffe',
			'\uffff'
		};
		_innerEncoder = new OptimizedInboxTextEncoder(singleton, in allowedCodePointsBitmap, forbidHtmlSensitiveCharacters: true, span);
	}

	private protected override OperationStatus EncodeCore(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock)
	{
		return _innerEncoder.Encode(source, destination, out charsConsumed, out charsWritten, isFinalBlock);
	}

	private protected override OperationStatus EncodeUtf8Core(ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
	{
		return _innerEncoder.EncodeUtf8(utf8Source, utf8Destination, out bytesConsumed, out bytesWritten, isFinalBlock);
	}

	private protected override int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
	{
		return _innerEncoder.GetIndexOfFirstCharToEncode(text);
	}

	public unsafe override int FindFirstCharacterToEncode(char* text, int textLength)
	{
		return _innerEncoder.FindFirstCharacterToEncode(text, textLength);
	}

	public override int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
	{
		return _innerEncoder.GetIndexOfFirstByteToEncode(utf8Text);
	}

	public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
	{
		return _innerEncoder.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
	}

	public override bool WillEncode(int unicodeScalar)
	{
		return !_innerEncoder.IsScalarValueAllowed(new Rune(unicodeScalar));
	}
}
