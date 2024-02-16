using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Text;

public abstract class Encoding : ICloneable
{
	internal sealed class DefaultEncoder : Encoder, IObjectReference
	{
		private readonly Encoding _encoding;

		public DefaultEncoder(Encoding encoding)
		{
			_encoding = encoding;
		}

		public object GetRealObject(StreamingContext context)
		{
			throw new PlatformNotSupportedException();
		}

		public override int GetByteCount(char[] chars, int index, int count, bool flush)
		{
			return _encoding.GetByteCount(chars, index, count);
		}

		public unsafe override int GetByteCount(char* chars, int count, bool flush)
		{
			return _encoding.GetByteCount(chars, count);
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
		{
			return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
		}

		public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
		{
			return _encoding.GetBytes(chars, charCount, bytes, byteCount);
		}
	}

	internal sealed class DefaultDecoder : Decoder, IObjectReference
	{
		private readonly Encoding _encoding;

		public DefaultDecoder(Encoding encoding)
		{
			_encoding = encoding;
		}

		public object GetRealObject(StreamingContext context)
		{
			throw new PlatformNotSupportedException();
		}

		public override int GetCharCount(byte[] bytes, int index, int count)
		{
			return GetCharCount(bytes, index, count, flush: false);
		}

		public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
		{
			return _encoding.GetCharCount(bytes, index, count);
		}

		public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
		{
			return _encoding.GetCharCount(bytes, count);
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			return GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush: false);
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
		{
			return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
		}

		public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
		{
			return _encoding.GetChars(bytes, byteCount, chars, charCount);
		}
	}

	internal sealed class EncodingCharBuffer
	{
		private unsafe char* _chars;

		private unsafe readonly char* _charStart;

		private unsafe readonly char* _charEnd;

		private int _charCountResult;

		private readonly Encoding _enc;

		private readonly DecoderNLS _decoder;

		private unsafe readonly byte* _byteStart;

		private unsafe readonly byte* _byteEnd;

		private unsafe byte* _bytes;

		private readonly DecoderFallbackBuffer _fallbackBuffer;

		internal unsafe bool MoreData => _bytes < _byteEnd;

		internal unsafe int BytesUsed => (int)(_bytes - _byteStart);

		internal int Count => _charCountResult;

		internal unsafe EncodingCharBuffer(Encoding enc, DecoderNLS decoder, char* charStart, int charCount, byte* byteStart, int byteCount)
		{
			_enc = enc;
			_decoder = decoder;
			_chars = charStart;
			_charStart = charStart;
			_charEnd = charStart + charCount;
			_byteStart = byteStart;
			_bytes = byteStart;
			_byteEnd = byteStart + byteCount;
			if (_decoder == null)
			{
				_fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
			}
			else
			{
				_fallbackBuffer = _decoder.FallbackBuffer;
			}
			_fallbackBuffer.InternalInitialize(_bytes, _charEnd);
		}

		internal unsafe bool AddChar(char ch, int numBytes)
		{
			if (_chars != null)
			{
				if (_chars >= _charEnd)
				{
					_bytes -= numBytes;
					_enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);
					return false;
				}
				*(_chars++) = ch;
			}
			_charCountResult++;
			return true;
		}

		internal bool AddChar(char ch)
		{
			return AddChar(ch, 1);
		}

		internal unsafe void AdjustBytes(int count)
		{
			_bytes += count;
		}

		internal unsafe byte GetNextByte()
		{
			if (_bytes >= _byteEnd)
			{
				return 0;
			}
			return *(_bytes++);
		}

		internal bool Fallback(byte fallbackByte)
		{
			byte[] byteBuffer = new byte[1] { fallbackByte };
			return Fallback(byteBuffer);
		}

		internal unsafe bool Fallback(byte[] byteBuffer)
		{
			if (_chars != null)
			{
				char* chars = _chars;
				if (!_fallbackBuffer.InternalFallback(byteBuffer, _bytes, ref _chars))
				{
					_bytes -= byteBuffer.Length;
					_fallbackBuffer.InternalReset();
					_enc.ThrowCharsOverflow(_decoder, _chars == _charStart);
					return false;
				}
				_charCountResult += (int)(_chars - chars);
			}
			else
			{
				_charCountResult += _fallbackBuffer.InternalFallback(byteBuffer, _bytes);
			}
			return true;
		}
	}

	internal sealed class EncodingByteBuffer
	{
		private unsafe byte* _bytes;

		private unsafe readonly byte* _byteStart;

		private unsafe readonly byte* _byteEnd;

		private unsafe char* _chars;

		private unsafe readonly char* _charStart;

		private unsafe readonly char* _charEnd;

		private int _byteCountResult;

		private readonly Encoding _enc;

		private readonly EncoderNLS _encoder;

		internal EncoderFallbackBuffer fallbackBuffer;

		internal unsafe bool MoreData
		{
			get
			{
				if (fallbackBuffer.Remaining <= 0)
				{
					return _chars < _charEnd;
				}
				return true;
			}
		}

		internal unsafe int CharsUsed => (int)(_chars - _charStart);

		internal int Count => _byteCountResult;

		internal unsafe EncodingByteBuffer(Encoding inEncoding, EncoderNLS inEncoder, byte* inByteStart, int inByteCount, char* inCharStart, int inCharCount)
		{
			_enc = inEncoding;
			_encoder = inEncoder;
			_charStart = inCharStart;
			_chars = inCharStart;
			_charEnd = inCharStart + inCharCount;
			_bytes = inByteStart;
			_byteStart = inByteStart;
			_byteEnd = inByteStart + inByteCount;
			if (_encoder == null)
			{
				fallbackBuffer = _enc.EncoderFallback.CreateFallbackBuffer();
			}
			else
			{
				fallbackBuffer = _encoder.FallbackBuffer;
				if (_encoder._throwOnOverflow && _encoder.InternalHasFallbackBuffer && fallbackBuffer.Remaining > 0)
				{
					throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, _encoder.Encoding.EncodingName, _encoder.Fallback.GetType()));
				}
			}
			fallbackBuffer.InternalInitialize(_chars, _charEnd, _encoder, _bytes != null);
		}

		internal unsafe bool AddByte(byte b, int moreBytesExpected)
		{
			if (_bytes != null)
			{
				if (_bytes >= _byteEnd - moreBytesExpected)
				{
					MovePrevious(bThrow: true);
					return false;
				}
				*(_bytes++) = b;
			}
			_byteCountResult++;
			return true;
		}

		internal bool AddByte(byte b1)
		{
			return AddByte(b1, 0);
		}

		internal bool AddByte(byte b1, byte b2)
		{
			return AddByte(b1, b2, 0);
		}

		internal bool AddByte(byte b1, byte b2, int moreBytesExpected)
		{
			if (AddByte(b1, 1 + moreBytesExpected))
			{
				return AddByte(b2, moreBytesExpected);
			}
			return false;
		}

		internal unsafe void MovePrevious(bool bThrow)
		{
			if (fallbackBuffer.bFallingBack)
			{
				fallbackBuffer.MovePrevious();
			}
			else if (_chars > _charStart)
			{
				_chars--;
			}
			if (bThrow)
			{
				_enc.ThrowBytesOverflow(_encoder, _bytes == _byteStart);
			}
		}

		internal unsafe char GetNextChar()
		{
			char c = fallbackBuffer.InternalGetNextChar();
			if (c == '\0' && _chars < _charEnd)
			{
				c = *(_chars++);
			}
			return c;
		}
	}

	private static readonly UTF8Encoding.UTF8EncodingSealed s_defaultEncoding = new UTF8Encoding.UTF8EncodingSealed(encoderShouldEmitUTF8Identifier: false);

	internal int _codePage;

	internal CodePageDataItem _dataItem;

	[OptionalField(VersionAdded = 2)]
	private bool _isReadOnly = true;

	internal EncoderFallback encoderFallback;

	internal DecoderFallback decoderFallback;

	public static Encoding Default => s_defaultEncoding;

	public virtual ReadOnlySpan<byte> Preamble => GetPreamble();

	public virtual string BodyName
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return _dataItem.BodyName;
		}
	}

	public virtual string EncodingName
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return _dataItem.DisplayName;
		}
	}

	public virtual string HeaderName
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return _dataItem.HeaderName;
		}
	}

	public virtual string WebName
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return _dataItem.WebName;
		}
	}

	public virtual int WindowsCodePage
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return _dataItem.UIFamilyCodePage;
		}
	}

	public virtual bool IsBrowserDisplay
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return (_dataItem.Flags & 2) != 0;
		}
	}

	public virtual bool IsBrowserSave
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return (_dataItem.Flags & 0x200) != 0;
		}
	}

	public virtual bool IsMailNewsDisplay
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return (_dataItem.Flags & 1) != 0;
		}
	}

	public virtual bool IsMailNewsSave
	{
		get
		{
			if (_dataItem == null)
			{
				GetDataItem();
			}
			return (_dataItem.Flags & 0x100) != 0;
		}
	}

	public virtual bool IsSingleByte => false;

	public EncoderFallback EncoderFallback
	{
		get
		{
			return encoderFallback;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			encoderFallback = value;
		}
	}

	public DecoderFallback DecoderFallback
	{
		get
		{
			return decoderFallback;
		}
		set
		{
			if (IsReadOnly)
			{
				throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			decoderFallback = value;
		}
	}

	public bool IsReadOnly
	{
		get
		{
			return _isReadOnly;
		}
		private protected set
		{
			_isReadOnly = value;
		}
	}

	public static Encoding ASCII => ASCIIEncoding.s_default;

	public static Encoding Latin1 => Latin1Encoding.s_default;

	public virtual int CodePage => _codePage;

	internal bool IsUTF8CodePage => CodePage == 65001;

	public static Encoding Unicode => UnicodeEncoding.s_littleEndianDefault;

	public static Encoding BigEndianUnicode => UnicodeEncoding.s_bigEndianDefault;

	[Obsolete("The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.", DiagnosticId = "SYSLIB0001", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static Encoding UTF7 => UTF7Encoding.s_default;

	public static Encoding UTF8 => UTF8Encoding.s_default;

	public static Encoding UTF32 => UTF32Encoding.s_default;

	private static Encoding BigEndianUTF32 => UTF32Encoding.s_bigEndianDefault;

	protected Encoding()
		: this(0)
	{
	}

	protected Encoding(int codePage)
	{
		if (codePage < 0)
		{
			throw new ArgumentOutOfRangeException("codePage");
		}
		_codePage = codePage;
		SetDefaultFallbacks();
	}

	protected Encoding(int codePage, EncoderFallback? encoderFallback, DecoderFallback? decoderFallback)
	{
		if (codePage < 0)
		{
			throw new ArgumentOutOfRangeException("codePage");
		}
		_codePage = codePage;
		this.encoderFallback = encoderFallback ?? System.Text.EncoderFallback.ReplacementFallback;
		this.decoderFallback = decoderFallback ?? System.Text.DecoderFallback.ReplacementFallback;
	}

	[MemberNotNull("encoderFallback")]
	[MemberNotNull("decoderFallback")]
	internal virtual void SetDefaultFallbacks()
	{
		encoderFallback = System.Text.EncoderFallback.ReplacementFallback;
		decoderFallback = System.Text.DecoderFallback.ReplacementFallback;
	}

	public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		return Convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length);
	}

	public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes, int index, int count)
	{
		if (srcEncoding == null || dstEncoding == null)
		{
			throw new ArgumentNullException((srcEncoding == null) ? "srcEncoding" : "dstEncoding", SR.ArgumentNull_Array);
		}
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		return dstEncoding.GetBytes(srcEncoding.GetChars(bytes, index, count));
	}

	public static void RegisterProvider(EncodingProvider provider)
	{
		EncodingProvider.AddProvider(provider);
	}

	public static Encoding GetEncoding(int codepage)
	{
		Encoding encoding = FilterDisallowedEncodings(EncodingProvider.GetEncodingFromProvider(codepage));
		if (encoding != null)
		{
			return encoding;
		}
		switch (codepage)
		{
		case 0:
			return Default;
		case 1200:
			return Unicode;
		case 1201:
			return BigEndianUnicode;
		case 12000:
			return UTF32;
		case 12001:
			return BigEndianUTF32;
		case 65001:
			return UTF8;
		case 20127:
			return ASCII;
		case 28591:
			return Latin1;
		case 1:
		case 2:
		case 3:
		case 42:
			throw new ArgumentException(SR.Format(SR.Argument_CodepageNotSupported, codepage), "codepage");
		case 65000:
		{
			if (LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
			{
				return UTF7;
			}
			string p = string.Format(CultureInfo.InvariantCulture, "https://aka.ms/dotnet-warnings/{0}", "SYSLIB0001");
			string message = SR.Format(SR.Encoding_UTF7_Disabled, p);
			throw new NotSupportedException(message);
		}
		default:
			if (codepage < 0 || codepage > 65535)
			{
				throw new ArgumentOutOfRangeException("codepage", SR.Format(SR.ArgumentOutOfRange_Range, 0, 65535));
			}
			throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, codepage));
		}
	}

	public static Encoding GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
	{
		Encoding encoding = FilterDisallowedEncodings(EncodingProvider.GetEncodingFromProvider(codepage, encoderFallback, decoderFallback));
		if (encoding != null)
		{
			return encoding;
		}
		encoding = GetEncoding(codepage);
		Encoding encoding2 = (Encoding)encoding.Clone();
		encoding2.EncoderFallback = encoderFallback;
		encoding2.DecoderFallback = decoderFallback;
		return encoding2;
	}

	public static Encoding GetEncoding(string name)
	{
		return FilterDisallowedEncodings(EncodingProvider.GetEncodingFromProvider(name)) ?? GetEncoding(EncodingTable.GetCodePageFromName(name));
	}

	public static Encoding GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
	{
		return FilterDisallowedEncodings(EncodingProvider.GetEncodingFromProvider(name, encoderFallback, decoderFallback)) ?? GetEncoding(EncodingTable.GetCodePageFromName(name), encoderFallback, decoderFallback);
	}

	private static Encoding FilterDisallowedEncodings(Encoding encoding)
	{
		if (LocalAppContextSwitches.EnableUnsafeUTF7Encoding)
		{
			return encoding;
		}
		if (encoding == null || encoding.CodePage != 65000)
		{
			return encoding;
		}
		return null;
	}

	public static EncodingInfo[] GetEncodings()
	{
		Dictionary<int, EncodingInfo> encodingListFromProviders = EncodingProvider.GetEncodingListFromProviders();
		if (encodingListFromProviders != null)
		{
			return EncodingTable.GetEncodings(encodingListFromProviders);
		}
		return EncodingTable.GetEncodings();
	}

	public virtual byte[] GetPreamble()
	{
		return Array.Empty<byte>();
	}

	private void GetDataItem()
	{
		if (_dataItem == null)
		{
			_dataItem = EncodingTable.GetCodePageDataItem(_codePage);
			if (_dataItem == null)
			{
				throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, _codePage));
			}
		}
	}

	public virtual object Clone()
	{
		Encoding encoding = (Encoding)MemberwiseClone();
		encoding._isReadOnly = false;
		return encoding;
	}

	public virtual int GetByteCount(char[] chars)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", SR.ArgumentNull_Array);
		}
		return GetByteCount(chars, 0, chars.Length);
	}

	public virtual int GetByteCount(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		char[] array = s.ToCharArray();
		return GetByteCount(array, 0, array.Length);
	}

	public abstract int GetByteCount(char[] chars, int index, int count);

	public unsafe int GetByteCount(string s, int index, int count)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s", SR.ArgumentNull_String);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (index > s.Length - count)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexCount);
		}
		fixed (char* ptr = s)
		{
			return GetByteCount(ptr + index, count);
		}
	}

	[CLSCompliant(false)]
	public unsafe virtual int GetByteCount(char* chars, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		char[] chars2 = new ReadOnlySpan<char>(chars, count).ToArray();
		return GetByteCount(chars2, 0, count);
	}

	public unsafe virtual int GetByteCount(ReadOnlySpan<char> chars)
	{
		fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
		{
			return GetByteCount(chars2, chars.Length);
		}
	}

	public virtual byte[] GetBytes(char[] chars)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", SR.ArgumentNull_Array);
		}
		return GetBytes(chars, 0, chars.Length);
	}

	public virtual byte[] GetBytes(char[] chars, int index, int count)
	{
		byte[] array = new byte[GetByteCount(chars, index, count)];
		GetBytes(chars, index, count, array, 0);
		return array;
	}

	public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);

	public virtual byte[] GetBytes(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s", SR.ArgumentNull_String);
		}
		int byteCount = GetByteCount(s);
		byte[] array = new byte[byteCount];
		int bytes = GetBytes(s, 0, s.Length, array, 0);
		return array;
	}

	public unsafe byte[] GetBytes(string s, int index, int count)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s", SR.ArgumentNull_String);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (index > s.Length - count)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_IndexCount);
		}
		fixed (char* ptr = s)
		{
			int byteCount = GetByteCount(ptr + index, count);
			if (byteCount == 0)
			{
				return Array.Empty<byte>();
			}
			byte[] array = new byte[byteCount];
			fixed (byte* bytes = &array[0])
			{
				int bytes2 = GetBytes(ptr + index, count, bytes, byteCount);
			}
			return array;
		}
	}

	public virtual int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
	}

	[CLSCompliant(false)]
	public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		char[] chars2 = new ReadOnlySpan<char>(chars, charCount).ToArray();
		byte[] array = new byte[byteCount];
		int bytes2 = GetBytes(chars2, 0, charCount, array, 0);
		if (bytes2 < byteCount)
		{
			byteCount = bytes2;
		}
		new ReadOnlySpan<byte>(array, 0, byteCount).CopyTo(new Span<byte>(bytes, byteCount));
		return byteCount;
	}

	public unsafe virtual int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
		{
			fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
			{
				return GetBytes(chars2, chars.Length, bytes2, bytes.Length);
			}
		}
	}

	public virtual int GetCharCount(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		return GetCharCount(bytes, 0, bytes.Length);
	}

	public abstract int GetCharCount(byte[] bytes, int index, int count);

	[CLSCompliant(false)]
	public unsafe virtual int GetCharCount(byte* bytes, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		byte[] bytes2 = new ReadOnlySpan<byte>(bytes, count).ToArray();
		return GetCharCount(bytes2, 0, count);
	}

	public unsafe virtual int GetCharCount(ReadOnlySpan<byte> bytes)
	{
		fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
		{
			return GetCharCount(bytes2, bytes.Length);
		}
	}

	public virtual char[] GetChars(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		return GetChars(bytes, 0, bytes.Length);
	}

	public virtual char[] GetChars(byte[] bytes, int index, int count)
	{
		char[] array = new char[GetCharCount(bytes, index, count)];
		GetChars(bytes, index, count, array, 0);
		return array;
	}

	public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);

	[CLSCompliant(false)]
	public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		byte[] bytes2 = new ReadOnlySpan<byte>(bytes, byteCount).ToArray();
		char[] array = new char[charCount];
		int chars2 = GetChars(bytes2, 0, byteCount, array, 0);
		if (chars2 < charCount)
		{
			charCount = chars2;
		}
		new ReadOnlySpan<char>(array, 0, charCount).CopyTo(new Span<char>(chars, charCount));
		return charCount;
	}

	public unsafe virtual int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
	{
		fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
		{
			fixed (char* chars2 = &MemoryMarshal.GetNonNullPinnableReference(chars))
			{
				return GetChars(bytes2, bytes.Length, chars2, chars.Length);
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe string GetString(byte* bytes, int byteCount)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return string.CreateStringFromEncoding(bytes, byteCount, this);
	}

	public unsafe string GetString(ReadOnlySpan<byte> bytes)
	{
		fixed (byte* bytes2 = &MemoryMarshal.GetNonNullPinnableReference(bytes))
		{
			return string.CreateStringFromEncoding(bytes2, bytes.Length, this);
		}
	}

	public bool IsAlwaysNormalized()
	{
		return IsAlwaysNormalized(NormalizationForm.FormC);
	}

	public virtual bool IsAlwaysNormalized(NormalizationForm form)
	{
		return false;
	}

	public virtual Decoder GetDecoder()
	{
		return new DefaultDecoder(this);
	}

	public virtual Encoder GetEncoder()
	{
		return new DefaultEncoder(this);
	}

	public abstract int GetMaxByteCount(int charCount);

	public abstract int GetMaxCharCount(int byteCount);

	public virtual string GetString(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		return GetString(bytes, 0, bytes.Length);
	}

	public virtual string GetString(byte[] bytes, int index, int count)
	{
		return new string(GetChars(bytes, index, count));
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is Encoding encoding && _codePage == encoding._codePage && EncoderFallback.Equals(encoding.EncoderFallback))
		{
			return DecoderFallback.Equals(encoding.DecoderFallback);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _codePage + EncoderFallback.GetHashCode() + DecoderFallback.GetHashCode();
	}

	public static Stream CreateTranscodingStream(Stream innerStream, Encoding innerStreamEncoding, Encoding outerStreamEncoding, bool leaveOpen = false)
	{
		if (innerStream == null)
		{
			throw new ArgumentNullException("innerStream");
		}
		if (innerStreamEncoding == null)
		{
			throw new ArgumentNullException("innerStreamEncoding");
		}
		if (outerStreamEncoding == null)
		{
			throw new ArgumentNullException("outerStreamEncoding");
		}
		return new TranscodingStream(innerStream, innerStreamEncoding, outerStreamEncoding, leaveOpen);
	}

	[DoesNotReturn]
	internal void ThrowBytesOverflow()
	{
		throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowBytes, EncodingName, EncoderFallback.GetType()), "bytes");
	}

	internal void ThrowBytesOverflow(EncoderNLS encoder, bool nothingEncoded)
	{
		if ((encoder?._throwOnOverflow ?? true) || nothingEncoded)
		{
			if (encoder != null && encoder.InternalHasFallbackBuffer)
			{
				encoder.FallbackBuffer.InternalReset();
			}
			ThrowBytesOverflow();
		}
		encoder.ClearMustFlush();
	}

	[DoesNotReturn]
	[StackTraceHidden]
	internal static void ThrowConversionOverflow()
	{
		throw new ArgumentException(SR.Argument_ConversionOverflow);
	}

	[DoesNotReturn]
	[StackTraceHidden]
	internal void ThrowCharsOverflow()
	{
		throw new ArgumentException(SR.Format(SR.Argument_EncodingConversionOverflowChars, EncodingName, DecoderFallback.GetType()), "chars");
	}

	internal void ThrowCharsOverflow(DecoderNLS decoder, bool nothingDecoded)
	{
		if ((decoder?._throwOnOverflow ?? true) || nothingDecoded)
		{
			if (decoder != null && decoder.InternalHasFallbackBuffer)
			{
				decoder.FallbackBuffer.InternalReset();
			}
			ThrowCharsOverflow();
		}
		decoder.ClearMustFlush();
	}

	internal virtual OperationStatus DecodeFirstRune(ReadOnlySpan<byte> bytes, out Rune value, out int bytesConsumed)
	{
		throw NotImplemented.ByDesign;
	}

	internal virtual OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
	{
		throw NotImplemented.ByDesign;
	}

	internal virtual bool TryGetByteCount(Rune value, out int byteCount)
	{
		throw NotImplemented.ByDesign;
	}

	internal unsafe virtual int GetByteCount(char* pChars, int charCount, EncoderNLS encoder)
	{
		int num = 0;
		int charsConsumed = 0;
		if (!encoder.HasLeftoverData)
		{
			num = GetByteCountFast(pChars, charCount, encoder.Fallback, out charsConsumed);
			if (charsConsumed == charCount)
			{
				return num;
			}
		}
		num += GetByteCountWithFallback(pChars, charCount, charsConsumed, encoder);
		if (num < 0)
		{
			ThrowConversionOverflow();
		}
		return num;
	}

	private protected unsafe virtual int GetByteCountFast(char* pChars, int charsLength, EncoderFallback fallback, out int charsConsumed)
	{
		throw NotImplemented.ByDesign;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private protected unsafe int GetByteCountWithFallback(char* pCharsOriginal, int originalCharCount, int charsConsumedSoFar)
	{
		return GetByteCountWithFallback(new ReadOnlySpan<char>(pCharsOriginal, originalCharCount).Slice(charsConsumedSoFar), originalCharCount, null);
	}

	private unsafe int GetByteCountWithFallback(char* pOriginalChars, int originalCharCount, int charsConsumedSoFar, EncoderNLS encoder)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(pOriginalChars, originalCharCount).Slice(charsConsumedSoFar);
		int num = encoder.DrainLeftoverDataForGetByteCount(readOnlySpan, out var charsConsumed);
		readOnlySpan = readOnlySpan.Slice(charsConsumed);
		num += GetByteCountFast((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(readOnlySpan)), readOnlySpan.Length, encoder.Fallback, out charsConsumed);
		if (num < 0)
		{
			ThrowConversionOverflow();
		}
		readOnlySpan = readOnlySpan.Slice(charsConsumed);
		if (!readOnlySpan.IsEmpty)
		{
			num += GetByteCountWithFallback(readOnlySpan, originalCharCount, encoder);
			if (num < 0)
			{
				ThrowConversionOverflow();
			}
		}
		return num;
	}

	private protected unsafe virtual int GetByteCountWithFallback(ReadOnlySpan<char> chars, int originalCharsLength, EncoderNLS encoder)
	{
		fixed (char* ptr = &MemoryMarshal.GetReference(chars))
		{
			EncoderFallbackBuffer encoderFallbackBuffer = EncoderFallbackBuffer.CreateAndInitialize(this, encoder, originalCharsLength);
			int num = 0;
			Rune result;
			int charsConsumed;
			while (Rune.DecodeFromUtf16(chars, out result, out charsConsumed) != OperationStatus.NeedMoreData || encoder == null || encoder.MustFlush)
			{
				int num2 = encoderFallbackBuffer.InternalFallbackGetByteCount(chars, out charsConsumed);
				num += num2;
				if (num < 0)
				{
					ThrowConversionOverflow();
				}
				chars = chars.Slice(charsConsumed);
				if (!chars.IsEmpty)
				{
					num2 = GetByteCountFast((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(chars)), chars.Length, null, out charsConsumed);
					num += num2;
					if (num < 0)
					{
						ThrowConversionOverflow();
					}
					chars = chars.Slice(charsConsumed);
				}
				if (chars.IsEmpty)
				{
					break;
				}
			}
			return num;
		}
	}

	internal unsafe virtual int GetBytes(char* pChars, int charCount, byte* pBytes, int byteCount, EncoderNLS encoder)
	{
		int num = 0;
		int charsConsumed = 0;
		if (!encoder.HasLeftoverData)
		{
			num = GetBytesFast(pChars, charCount, pBytes, byteCount, out charsConsumed);
			if (charsConsumed == charCount)
			{
				encoder._charsUsed = charCount;
				return num;
			}
		}
		return GetBytesWithFallback(pChars, charCount, pBytes, byteCount, charsConsumed, num, encoder);
	}

	private protected unsafe virtual int GetBytesFast(char* pChars, int charsLength, byte* pBytes, int bytesLength, out int charsConsumed)
	{
		throw NotImplemented.ByDesign;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private protected unsafe int GetBytesWithFallback(char* pOriginalChars, int originalCharCount, byte* pOriginalBytes, int originalByteCount, int charsConsumedSoFar, int bytesWrittenSoFar)
	{
		return GetBytesWithFallback(new ReadOnlySpan<char>(pOriginalChars, originalCharCount).Slice(charsConsumedSoFar), originalCharCount, new Span<byte>(pOriginalBytes, originalByteCount).Slice(bytesWrittenSoFar), originalByteCount, null);
	}

	private unsafe int GetBytesWithFallback(char* pOriginalChars, int originalCharCount, byte* pOriginalBytes, int originalByteCount, int charsConsumedSoFar, int bytesWrittenSoFar, EncoderNLS encoder)
	{
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(pOriginalChars, originalCharCount).Slice(charsConsumedSoFar);
		Span<byte> span = new Span<byte>(pOriginalBytes, originalByteCount).Slice(bytesWrittenSoFar);
		int charsConsumed;
		int bytesWritten;
		bool flag = encoder.TryDrainLeftoverDataForGetBytes(readOnlySpan, span, out charsConsumed, out bytesWritten);
		readOnlySpan = readOnlySpan.Slice(charsConsumed);
		span = span.Slice(bytesWritten);
		if (!flag)
		{
			ThrowBytesOverflow(encoder, span.Length == originalByteCount);
		}
		else
		{
			bytesWritten = GetBytesFast((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(readOnlySpan)), readOnlySpan.Length, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length, out charsConsumed);
			readOnlySpan = readOnlySpan.Slice(charsConsumed);
			span = span.Slice(bytesWritten);
			if (!readOnlySpan.IsEmpty)
			{
				encoder._charsUsed = originalCharCount;
				return GetBytesWithFallback(readOnlySpan, originalCharCount, span, originalByteCount, encoder);
			}
		}
		encoder._charsUsed = originalCharCount - readOnlySpan.Length;
		return originalByteCount - span.Length;
	}

	private protected unsafe virtual int GetBytesWithFallback(ReadOnlySpan<char> chars, int originalCharsLength, Span<byte> bytes, int originalBytesLength, EncoderNLS encoder)
	{
		fixed (char* ptr2 = &MemoryMarshal.GetReference(chars))
		{
			fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
			{
				EncoderFallbackBuffer encoderFallbackBuffer = EncoderFallbackBuffer.CreateAndInitialize(this, encoder, originalCharsLength);
				do
				{
					Rune result;
					int charsConsumed;
					OperationStatus operationStatus = Rune.DecodeFromUtf16(chars, out result, out charsConsumed);
					if (operationStatus != OperationStatus.NeedMoreData)
					{
						if (operationStatus != OperationStatus.InvalidData && EncodeRune(result, bytes, out var _) == OperationStatus.DestinationTooSmall)
						{
							break;
						}
					}
					else if (encoder != null && !encoder.MustFlush)
					{
						encoder._charLeftOver = chars[0];
						chars = ReadOnlySpan<char>.Empty;
						break;
					}
					int bytesWritten2;
					bool flag = encoderFallbackBuffer.TryInternalFallbackGetBytes(chars, bytes, out charsConsumed, out bytesWritten2);
					chars = chars.Slice(charsConsumed);
					bytes = bytes.Slice(bytesWritten2);
					if (!flag)
					{
						break;
					}
					if (!chars.IsEmpty)
					{
						bytesWritten2 = GetBytesFast((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(chars)), chars.Length, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)), bytes.Length, out charsConsumed);
						chars = chars.Slice(charsConsumed);
						bytes = bytes.Slice(bytesWritten2);
					}
				}
				while (!chars.IsEmpty);
				if (!chars.IsEmpty || encoderFallbackBuffer.Remaining > 0)
				{
					ThrowBytesOverflow(encoder, bytes.Length == originalBytesLength);
				}
				if (encoder != null)
				{
					encoder._charsUsed = originalCharsLength - chars.Length;
				}
				return originalBytesLength - bytes.Length;
			}
		}
	}

	internal unsafe virtual int GetCharCount(byte* pBytes, int byteCount, DecoderNLS decoder)
	{
		int num = 0;
		int bytesConsumed = 0;
		if (!decoder.HasLeftoverData)
		{
			num = GetCharCountFast(pBytes, byteCount, decoder.Fallback, out bytesConsumed);
			if (bytesConsumed == byteCount)
			{
				return num;
			}
		}
		num += GetCharCountWithFallback(pBytes, byteCount, bytesConsumed, decoder);
		if (num < 0)
		{
			ThrowConversionOverflow();
		}
		return num;
	}

	private protected unsafe virtual int GetCharCountFast(byte* pBytes, int bytesLength, DecoderFallback fallback, out int bytesConsumed)
	{
		throw NotImplemented.ByDesign;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private protected unsafe int GetCharCountWithFallback(byte* pBytesOriginal, int originalByteCount, int bytesConsumedSoFar)
	{
		return GetCharCountWithFallback(new ReadOnlySpan<byte>(pBytesOriginal, originalByteCount).Slice(bytesConsumedSoFar), originalByteCount, null);
	}

	private unsafe int GetCharCountWithFallback(byte* pOriginalBytes, int originalByteCount, int bytesConsumedSoFar, DecoderNLS decoder)
	{
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(pOriginalBytes, originalByteCount).Slice(bytesConsumedSoFar);
		int num = 0;
		int bytesConsumed;
		if (decoder.HasLeftoverData)
		{
			num = decoder.DrainLeftoverDataForGetCharCount(readOnlySpan, out bytesConsumed);
			readOnlySpan = readOnlySpan.Slice(bytesConsumed);
		}
		num += GetCharCountFast((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(readOnlySpan)), readOnlySpan.Length, decoder.Fallback, out bytesConsumed);
		if (num < 0)
		{
			ThrowConversionOverflow();
		}
		readOnlySpan = readOnlySpan.Slice(bytesConsumed);
		if (!readOnlySpan.IsEmpty)
		{
			num += GetCharCountWithFallback(readOnlySpan, originalByteCount, decoder);
			if (num < 0)
			{
				ThrowConversionOverflow();
			}
		}
		return num;
	}

	private unsafe int GetCharCountWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, DecoderNLS decoder)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
		{
			DecoderFallbackBuffer decoderFallbackBuffer = DecoderFallbackBuffer.CreateAndInitialize(this, decoder, originalBytesLength);
			int num = 0;
			Rune value;
			int bytesConsumed;
			while (DecodeFirstRune(bytes, out value, out bytesConsumed) != OperationStatus.NeedMoreData || decoder == null || decoder.MustFlush)
			{
				int num2 = decoderFallbackBuffer.InternalFallbackGetCharCount(bytes, bytesConsumed);
				num += num2;
				if (num < 0)
				{
					ThrowConversionOverflow();
				}
				bytes = bytes.Slice(bytesConsumed);
				if (!bytes.IsEmpty)
				{
					num2 = GetCharCountFast((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)), bytes.Length, null, out bytesConsumed);
					num += num2;
					if (num < 0)
					{
						ThrowConversionOverflow();
					}
					bytes = bytes.Slice(bytesConsumed);
				}
				if (bytes.IsEmpty)
				{
					break;
				}
			}
			return num;
		}
	}

	internal unsafe virtual int GetChars(byte* pBytes, int byteCount, char* pChars, int charCount, DecoderNLS decoder)
	{
		int num = 0;
		int bytesConsumed = 0;
		if (!decoder.HasLeftoverData)
		{
			num = GetCharsFast(pBytes, byteCount, pChars, charCount, out bytesConsumed);
			if (bytesConsumed == byteCount)
			{
				decoder._bytesUsed = byteCount;
				return num;
			}
		}
		return GetCharsWithFallback(pBytes, byteCount, pChars, charCount, bytesConsumed, num, decoder);
	}

	private protected unsafe virtual int GetCharsFast(byte* pBytes, int bytesLength, char* pChars, int charsLength, out int bytesConsumed)
	{
		throw NotImplemented.ByDesign;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private protected unsafe int GetCharsWithFallback(byte* pOriginalBytes, int originalByteCount, char* pOriginalChars, int originalCharCount, int bytesConsumedSoFar, int charsWrittenSoFar)
	{
		return GetCharsWithFallback(new ReadOnlySpan<byte>(pOriginalBytes, originalByteCount).Slice(bytesConsumedSoFar), originalByteCount, new Span<char>(pOriginalChars, originalCharCount).Slice(charsWrittenSoFar), originalCharCount, null);
	}

	private protected unsafe int GetCharsWithFallback(byte* pOriginalBytes, int originalByteCount, char* pOriginalChars, int originalCharCount, int bytesConsumedSoFar, int charsWrittenSoFar, DecoderNLS decoder)
	{
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(pOriginalBytes, originalByteCount).Slice(bytesConsumedSoFar);
		Span<char> span = new Span<char>(pOriginalChars, originalCharCount).Slice(charsWrittenSoFar);
		int bytesConsumed;
		int start;
		if (decoder.HasLeftoverData)
		{
			start = decoder.DrainLeftoverDataForGetChars(readOnlySpan, span, out bytesConsumed);
			readOnlySpan = readOnlySpan.Slice(bytesConsumed);
			span = span.Slice(start);
		}
		start = GetCharsFast((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(readOnlySpan)), readOnlySpan.Length, (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), span.Length, out bytesConsumed);
		readOnlySpan = readOnlySpan.Slice(bytesConsumed);
		span = span.Slice(start);
		decoder._bytesUsed = originalByteCount;
		if (readOnlySpan.IsEmpty)
		{
			return originalCharCount - span.Length;
		}
		return GetCharsWithFallback(readOnlySpan, originalByteCount, span, originalCharCount, decoder);
	}

	private protected unsafe virtual int GetCharsWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, Span<char> chars, int originalCharsLength, DecoderNLS decoder)
	{
		fixed (byte* ptr2 = &MemoryMarshal.GetReference(bytes))
		{
			fixed (char* ptr = &MemoryMarshal.GetReference(chars))
			{
				DecoderFallbackBuffer decoderFallbackBuffer = DecoderFallbackBuffer.CreateAndInitialize(this, decoder, originalBytesLength);
				do
				{
					Rune value;
					int bytesConsumed;
					OperationStatus operationStatus = DecodeFirstRune(bytes, out value, out bytesConsumed);
					if (operationStatus != OperationStatus.NeedMoreData)
					{
						if (operationStatus != OperationStatus.InvalidData)
						{
							break;
						}
					}
					else if (decoder != null && !decoder.MustFlush)
					{
						decoder.SetLeftoverData(bytes);
						bytes = ReadOnlySpan<byte>.Empty;
						break;
					}
					if (!decoderFallbackBuffer.TryInternalFallbackGetChars(bytes, bytesConsumed, chars, out var charsWritten))
					{
						break;
					}
					bytes = bytes.Slice(bytesConsumed);
					chars = chars.Slice(charsWritten);
					if (!bytes.IsEmpty)
					{
						charsWritten = GetCharsFast((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)), bytes.Length, (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(chars)), chars.Length, out bytesConsumed);
						bytes = bytes.Slice(bytesConsumed);
						chars = chars.Slice(charsWritten);
					}
				}
				while (!bytes.IsEmpty);
				if (!bytes.IsEmpty)
				{
					ThrowCharsOverflow(decoder, chars.Length == originalCharsLength);
				}
				if (decoder != null)
				{
					decoder._bytesUsed = originalBytesLength - bytes.Length;
				}
				return originalCharsLength - chars.Length;
			}
		}
	}
}
