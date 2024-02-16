using System.Diagnostics.CodeAnalysis;

namespace System.Text;

internal abstract class EncodingNLS : Encoding
{
	private string _encodingName;

	private string _webName;

	public override string EncodingName
	{
		get
		{
			if (_encodingName == null)
			{
				_encodingName = GetLocalizedEncodingNameResource(CodePage);
				if (_encodingName == null)
				{
					throw new NotSupportedException(System.SR.Format(System.SR.MissingEncodingNameResource, WebName, CodePage));
				}
				if (_encodingName.StartsWith("Globalization_cp_", StringComparison.OrdinalIgnoreCase))
				{
					_encodingName = System.Text.EncodingTable.GetEnglishNameFromCodePage(CodePage);
					if (_encodingName == null)
					{
						throw new NotSupportedException(System.SR.Format(System.SR.MissingEncodingNameResource, WebName, CodePage));
					}
				}
			}
			return _encodingName;
		}
	}

	public override string WebName
	{
		get
		{
			if (_webName == null)
			{
				_webName = System.Text.EncodingTable.GetWebNameFromCodePage(CodePage);
				if (_webName == null)
				{
					throw new NotSupportedException(System.SR.Format(System.SR.NotSupported_NoCodepageData, CodePage));
				}
			}
			return _webName;
		}
	}

	public override string HeaderName => CodePage switch
	{
		932 => "iso-2022-jp", 
		50221 => "iso-2022-jp", 
		50225 => "euc-kr", 
		_ => WebName, 
	};

	public override string BodyName => CodePage switch
	{
		932 => "iso-2022-jp", 
		1250 => "iso-8859-2", 
		1251 => "koi8-r", 
		1252 => "iso-8859-1", 
		1253 => "iso-8859-7", 
		1254 => "iso-8859-9", 
		50221 => "iso-2022-jp", 
		50225 => "iso-2022-kr", 
		_ => WebName, 
	};

	protected EncodingNLS(int codePage)
		: base(codePage)
	{
	}

	protected EncodingNLS(int codePage, EncoderFallback enc, DecoderFallback dec)
		: base(codePage, enc, dec)
	{
	}

	public unsafe abstract int GetByteCount(char* chars, int count, System.Text.EncoderNLS encoder);

	public unsafe abstract int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, System.Text.EncoderNLS encoder);

	public unsafe abstract int GetCharCount(byte* bytes, int count, System.Text.DecoderNLS decoder);

	public unsafe abstract int GetChars(byte* bytes, int byteCount, char* chars, int charCount, System.Text.DecoderNLS decoder);

	public unsafe override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length == 0)
		{
			return 0;
		}
		fixed (char* ptr = &chars[0])
		{
			return GetByteCount(ptr + index, count, null);
		}
	}

	public unsafe override int GetByteCount(string s)
	{
		if (s == null)
		{
			throw new ArgumentNullException("s");
		}
		fixed (char* chars = s)
		{
			return GetByteCount(chars, s.Length, null);
		}
	}

	public unsafe override int GetByteCount(char* chars, int count)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetByteCount(chars, count, null);
	}

	public unsafe override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (s == null || bytes == null)
		{
			throw new ArgumentNullException((s == null) ? "s" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (s.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("s", System.SR.ArgumentOutOfRange_IndexCount);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.ArgumentOutOfRange_Index);
		}
		int byteCount = bytes.Length - byteIndex;
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		fixed (char* ptr = s)
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				return GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, null);
			}
		}
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (chars.Length == 0)
		{
			return 0;
		}
		int byteCount = bytes.Length - byteIndex;
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		fixed (char* ptr = &chars[0])
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				return GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, null);
			}
		}
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetBytes(chars, charCount, bytes, byteCount, null);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length == 0)
		{
			return 0;
		}
		fixed (byte* ptr = &bytes[0])
		{
			return GetCharCount(ptr + index, count, null);
		}
	}

	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetCharCount(bytes, count, null);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charIndex < 0 || charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (bytes.Length == 0)
		{
			return 0;
		}
		int charCount = chars.Length - charIndex;
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		fixed (byte* ptr = &bytes[0])
		{
			fixed (char* ptr2 = &chars[0])
			{
				return GetChars(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, null);
			}
		}
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetChars(bytes, byteCount, chars, charCount, null);
	}

	public unsafe override string GetString(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = &bytes[0])
		{
			return GetString(ptr + index, count);
		}
	}

	public override Decoder GetDecoder()
	{
		return new System.Text.DecoderNLS(this);
	}

	public override Encoder GetEncoder()
	{
		return new System.Text.EncoderNLS(this);
	}

	internal void ThrowBytesOverflow(System.Text.EncoderNLS encoder, bool nothingEncoded)
	{
		if ((encoder?.m_throwOnOverflow ?? true) || nothingEncoded)
		{
			if (encoder != null && encoder.InternalHasFallbackBuffer)
			{
				encoder.FallbackBuffer.Reset();
			}
			ThrowBytesOverflow();
		}
		encoder.ClearMustFlush();
	}

	internal void ThrowCharsOverflow(System.Text.DecoderNLS decoder, bool nothingDecoded)
	{
		if ((decoder?.m_throwOnOverflow ?? true) || nothingDecoded)
		{
			if (decoder != null && decoder.InternalHasFallbackBuffer)
			{
				decoder.FallbackBuffer.Reset();
			}
			ThrowCharsOverflow();
		}
		decoder.ClearMustFlush();
	}

	[DoesNotReturn]
	internal void ThrowBytesOverflow()
	{
		throw new ArgumentException(System.SR.Format(System.SR.Argument_EncodingConversionOverflowBytes, EncodingName, base.EncoderFallback.GetType()), "bytes");
	}

	[DoesNotReturn]
	internal void ThrowCharsOverflow()
	{
		throw new ArgumentException(System.SR.Format(System.SR.Argument_EncodingConversionOverflowChars, EncodingName, base.DecoderFallback.GetType()), "chars");
	}

	internal static string GetLocalizedEncodingNameResource(int codePage)
	{
		return codePage switch
		{
			37 => System.SR.Globalization_cp_37, 
			437 => System.SR.Globalization_cp_437, 
			500 => System.SR.Globalization_cp_500, 
			708 => System.SR.Globalization_cp_708, 
			720 => System.SR.Globalization_cp_720, 
			737 => System.SR.Globalization_cp_737, 
			775 => System.SR.Globalization_cp_775, 
			850 => System.SR.Globalization_cp_850, 
			852 => System.SR.Globalization_cp_852, 
			855 => System.SR.Globalization_cp_855, 
			857 => System.SR.Globalization_cp_857, 
			858 => System.SR.Globalization_cp_858, 
			860 => System.SR.Globalization_cp_860, 
			861 => System.SR.Globalization_cp_861, 
			862 => System.SR.Globalization_cp_862, 
			863 => System.SR.Globalization_cp_863, 
			864 => System.SR.Globalization_cp_864, 
			865 => System.SR.Globalization_cp_865, 
			866 => System.SR.Globalization_cp_866, 
			869 => System.SR.Globalization_cp_869, 
			870 => System.SR.Globalization_cp_870, 
			874 => System.SR.Globalization_cp_874, 
			875 => System.SR.Globalization_cp_875, 
			932 => System.SR.Globalization_cp_932, 
			936 => System.SR.Globalization_cp_936, 
			949 => System.SR.Globalization_cp_949, 
			950 => System.SR.Globalization_cp_950, 
			1026 => System.SR.Globalization_cp_1026, 
			1047 => System.SR.Globalization_cp_1047, 
			1140 => System.SR.Globalization_cp_1140, 
			1141 => System.SR.Globalization_cp_1141, 
			1142 => System.SR.Globalization_cp_1142, 
			1143 => System.SR.Globalization_cp_1143, 
			1144 => System.SR.Globalization_cp_1144, 
			1145 => System.SR.Globalization_cp_1145, 
			1146 => System.SR.Globalization_cp_1146, 
			1147 => System.SR.Globalization_cp_1147, 
			1148 => System.SR.Globalization_cp_1148, 
			1149 => System.SR.Globalization_cp_1149, 
			1250 => System.SR.Globalization_cp_1250, 
			1251 => System.SR.Globalization_cp_1251, 
			1252 => System.SR.Globalization_cp_1252, 
			1253 => System.SR.Globalization_cp_1253, 
			1254 => System.SR.Globalization_cp_1254, 
			1255 => System.SR.Globalization_cp_1255, 
			1256 => System.SR.Globalization_cp_1256, 
			1257 => System.SR.Globalization_cp_1257, 
			1258 => System.SR.Globalization_cp_1258, 
			1361 => System.SR.Globalization_cp_1361, 
			10000 => System.SR.Globalization_cp_10000, 
			10001 => System.SR.Globalization_cp_10001, 
			10002 => System.SR.Globalization_cp_10002, 
			10003 => System.SR.Globalization_cp_10003, 
			10004 => System.SR.Globalization_cp_10004, 
			10005 => System.SR.Globalization_cp_10005, 
			10006 => System.SR.Globalization_cp_10006, 
			10007 => System.SR.Globalization_cp_10007, 
			10008 => System.SR.Globalization_cp_10008, 
			10010 => System.SR.Globalization_cp_10010, 
			10017 => System.SR.Globalization_cp_10017, 
			10021 => System.SR.Globalization_cp_10021, 
			10029 => System.SR.Globalization_cp_10029, 
			10079 => System.SR.Globalization_cp_10079, 
			10081 => System.SR.Globalization_cp_10081, 
			10082 => System.SR.Globalization_cp_10082, 
			20000 => System.SR.Globalization_cp_20000, 
			20001 => System.SR.Globalization_cp_20001, 
			20002 => System.SR.Globalization_cp_20002, 
			20003 => System.SR.Globalization_cp_20003, 
			20004 => System.SR.Globalization_cp_20004, 
			20005 => System.SR.Globalization_cp_20005, 
			20105 => System.SR.Globalization_cp_20105, 
			20106 => System.SR.Globalization_cp_20106, 
			20107 => System.SR.Globalization_cp_20107, 
			20108 => System.SR.Globalization_cp_20108, 
			20261 => System.SR.Globalization_cp_20261, 
			20269 => System.SR.Globalization_cp_20269, 
			20273 => System.SR.Globalization_cp_20273, 
			20277 => System.SR.Globalization_cp_20277, 
			20278 => System.SR.Globalization_cp_20278, 
			20280 => System.SR.Globalization_cp_20280, 
			20284 => System.SR.Globalization_cp_20284, 
			20285 => System.SR.Globalization_cp_20285, 
			20290 => System.SR.Globalization_cp_20290, 
			20297 => System.SR.Globalization_cp_20297, 
			20420 => System.SR.Globalization_cp_20420, 
			20423 => System.SR.Globalization_cp_20423, 
			20424 => System.SR.Globalization_cp_20424, 
			20833 => System.SR.Globalization_cp_20833, 
			20838 => System.SR.Globalization_cp_20838, 
			20866 => System.SR.Globalization_cp_20866, 
			20871 => System.SR.Globalization_cp_20871, 
			20880 => System.SR.Globalization_cp_20880, 
			20905 => System.SR.Globalization_cp_20905, 
			20924 => System.SR.Globalization_cp_20924, 
			20932 => System.SR.Globalization_cp_20932, 
			20936 => System.SR.Globalization_cp_20936, 
			20949 => System.SR.Globalization_cp_20949, 
			21025 => System.SR.Globalization_cp_21025, 
			21027 => System.SR.Globalization_cp_21027, 
			21866 => System.SR.Globalization_cp_21866, 
			28592 => System.SR.Globalization_cp_28592, 
			28593 => System.SR.Globalization_cp_28593, 
			28594 => System.SR.Globalization_cp_28594, 
			28595 => System.SR.Globalization_cp_28595, 
			28596 => System.SR.Globalization_cp_28596, 
			28597 => System.SR.Globalization_cp_28597, 
			28598 => System.SR.Globalization_cp_28598, 
			28599 => System.SR.Globalization_cp_28599, 
			28603 => System.SR.Globalization_cp_28603, 
			28605 => System.SR.Globalization_cp_28605, 
			29001 => System.SR.Globalization_cp_29001, 
			38598 => System.SR.Globalization_cp_38598, 
			50000 => System.SR.Globalization_cp_50000, 
			50220 => System.SR.Globalization_cp_50220, 
			50221 => System.SR.Globalization_cp_50221, 
			50222 => System.SR.Globalization_cp_50222, 
			50225 => System.SR.Globalization_cp_50225, 
			50227 => System.SR.Globalization_cp_50227, 
			50229 => System.SR.Globalization_cp_50229, 
			50930 => System.SR.Globalization_cp_50930, 
			50931 => System.SR.Globalization_cp_50931, 
			50933 => System.SR.Globalization_cp_50933, 
			50935 => System.SR.Globalization_cp_50935, 
			50937 => System.SR.Globalization_cp_50937, 
			50939 => System.SR.Globalization_cp_50939, 
			51932 => System.SR.Globalization_cp_51932, 
			51936 => System.SR.Globalization_cp_51936, 
			51949 => System.SR.Globalization_cp_51949, 
			52936 => System.SR.Globalization_cp_52936, 
			54936 => System.SR.Globalization_cp_54936, 
			57002 => System.SR.Globalization_cp_57002, 
			57003 => System.SR.Globalization_cp_57003, 
			57004 => System.SR.Globalization_cp_57004, 
			57005 => System.SR.Globalization_cp_57005, 
			57006 => System.SR.Globalization_cp_57006, 
			57007 => System.SR.Globalization_cp_57007, 
			57008 => System.SR.Globalization_cp_57008, 
			57009 => System.SR.Globalization_cp_57009, 
			57010 => System.SR.Globalization_cp_57010, 
			57011 => System.SR.Globalization_cp_57011, 
			_ => null, 
		};
	}
}
