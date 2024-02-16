using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text;

internal sealed class SBCSCodePageEncoding : BaseCodePageEncoding
{
	private unsafe char* _mapBytesToUnicode = null;

	private unsafe byte* _mapUnicodeToBytes = null;

	private byte _byteUnknown;

	private char _charUnknown;

	private static object s_InternalSyncObject;

	private static object InternalSyncObject
	{
		get
		{
			if (s_InternalSyncObject == null)
			{
				object value = new object();
				Interlocked.CompareExchange<object>(ref s_InternalSyncObject, value, (object)null);
			}
			return s_InternalSyncObject;
		}
	}

	public override bool IsSingleByte => true;

	public SBCSCodePageEncoding(int codePage)
		: this(codePage, codePage)
	{
	}

	public unsafe SBCSCodePageEncoding(int codePage, int dataCodePage)
		: base(codePage, dataCodePage)
	{
	}

	internal unsafe static ushort ReadUInt16(byte* pByte)
	{
		if (BitConverter.IsLittleEndian)
		{
			return *(ushort*)pByte;
		}
		return BinaryPrimitives.ReverseEndianness(*(ushort*)pByte);
	}

	protected unsafe override void LoadManagedCodePage()
	{
		fixed (byte* ptr = &m_codePageHeader[0])
		{
			CodePageHeader* ptr2 = (CodePageHeader*)ptr;
			if (ptr2->ByteCount != 1)
			{
				throw new NotSupportedException(System.SR.Format(System.SR.NotSupported_NoCodepageData, CodePage));
			}
			_byteUnknown = (byte)ptr2->ByteReplace;
			_charUnknown = ptr2->UnicodeReplace;
			int num = 66052 + iExtraBytes;
			byte* nativeMemory = GetNativeMemory(num);
			Unsafe.InitBlockUnaligned(nativeMemory, 0, (uint)num);
			char* ptr3 = (char*)nativeMemory;
			byte* ptr4 = nativeMemory + 512;
			byte[] array = new byte[512];
			lock (BaseCodePageEncoding.s_streamLock)
			{
				BaseCodePageEncoding.s_codePagesEncodingDataStream.Seek(m_firstDataWordOffset, SeekOrigin.Begin);
				BaseCodePageEncoding.s_codePagesEncodingDataStream.Read(array, 0, array.Length);
			}
			fixed (byte* ptr5 = &array[0])
			{
				for (int i = 0; i < 256; i++)
				{
					char c = (char)ReadUInt16(ptr5 + 2 * i);
					if (c != 0 || i == 0)
					{
						ptr3[i] = c;
						if (c != '\ufffd')
						{
							ptr4[(int)c] = (byte)i;
						}
					}
					else
					{
						ptr3[i] = '\ufffd';
					}
				}
			}
			_mapBytesToUnicode = ptr3;
			_mapUnicodeToBytes = ptr4;
		}
	}

	protected unsafe override void ReadBestFitTable()
	{
		lock (InternalSyncObject)
		{
			if (arrayUnicodeBestFit != null)
			{
				return;
			}
			byte[] array = new byte[m_dataSize - 512];
			lock (BaseCodePageEncoding.s_streamLock)
			{
				BaseCodePageEncoding.s_codePagesEncodingDataStream.Seek(m_firstDataWordOffset + 512, SeekOrigin.Begin);
				BaseCodePageEncoding.s_codePagesEncodingDataStream.Read(array, 0, array.Length);
			}
			fixed (byte* ptr = array)
			{
				byte* ptr2 = ptr;
				char[] array2 = new char[256];
				for (int i = 0; i < 256; i++)
				{
					array2[i] = _mapBytesToUnicode[i];
				}
				ushort num;
				while ((num = ReadUInt16(ptr2)) != 0)
				{
					ptr2 += 2;
					array2[num] = (char)ReadUInt16(ptr2);
					ptr2 += 2;
				}
				arrayBytesBestFit = array2;
				ptr2 += 2;
				byte* ptr3 = ptr2;
				int num2 = 0;
				int num3 = ReadUInt16(ptr2);
				ptr2 += 2;
				while (num3 < 65536)
				{
					byte b = *ptr2;
					ptr2++;
					switch (b)
					{
					case 1:
						num3 = ReadUInt16(ptr2);
						ptr2 += 2;
						continue;
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
					case 9:
					case 10:
					case 11:
					case 12:
					case 13:
					case 14:
					case 15:
					case 16:
					case 17:
					case 18:
					case 19:
					case 20:
					case 21:
					case 22:
					case 23:
					case 24:
					case 25:
					case 26:
					case 27:
					case 28:
					case 29:
					case 31:
						num3 += b;
						continue;
					}
					if (b > 0)
					{
						num2++;
					}
					num3++;
				}
				array2 = new char[num2 * 2];
				ptr2 = ptr3;
				num3 = ReadUInt16(ptr2);
				ptr2 += 2;
				num2 = 0;
				while (num3 < 65536)
				{
					byte b2 = *ptr2;
					ptr2++;
					switch (b2)
					{
					case 1:
						num3 = ReadUInt16(ptr2);
						ptr2 += 2;
						continue;
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
					case 7:
					case 8:
					case 9:
					case 10:
					case 11:
					case 12:
					case 13:
					case 14:
					case 15:
					case 16:
					case 17:
					case 18:
					case 19:
					case 20:
					case 21:
					case 22:
					case 23:
					case 24:
					case 25:
					case 26:
					case 27:
					case 28:
					case 29:
					case 31:
						num3 += b2;
						continue;
					}
					if (b2 == 30)
					{
						b2 = *ptr2;
						ptr2++;
					}
					if (b2 > 0)
					{
						array2[num2++] = (char)num3;
						array2[num2++] = _mapBytesToUnicode[(int)b2];
					}
					num3++;
				}
				arrayUnicodeBestFit = array2;
			}
		}
	}

	public unsafe override int GetByteCount(char* chars, int count, System.Text.EncoderNLS encoder)
	{
		CheckMemorySection();
		EncoderReplacementFallback encoderReplacementFallback = null;
		char c = '\0';
		if (encoder != null)
		{
			c = encoder.charLeftOver;
			encoderReplacementFallback = encoder.Fallback as EncoderReplacementFallback;
		}
		else
		{
			encoderReplacementFallback = base.EncoderFallback as EncoderReplacementFallback;
		}
		if (encoderReplacementFallback != null && encoderReplacementFallback.MaxCharCount == 1)
		{
			if (c > '\0')
			{
				count++;
			}
			return count;
		}
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		int num = 0;
		char* ptr = chars + count;
		EncoderFallbackBufferHelper encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
		if (c > '\0')
		{
			encoderFallbackBuffer = encoder.FallbackBuffer;
			encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
			encoderFallbackBufferHelper.InternalInitialize(chars, ptr, encoder, _setEncoder: false);
			encoderFallbackBufferHelper.InternalFallback(c, ref chars);
		}
		char c2;
		while ((c2 = ((encoderFallbackBuffer != null) ? encoderFallbackBufferHelper.InternalGetNextChar() : '\0')) != 0 || chars < ptr)
		{
			if (c2 == '\0')
			{
				c2 = *chars;
				chars++;
			}
			if (_mapUnicodeToBytes[(int)c2] == 0 && c2 != 0)
			{
				if (encoderFallbackBuffer == null)
				{
					encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : base.EncoderFallback.CreateFallbackBuffer());
					encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
					encoderFallbackBufferHelper.InternalInitialize(ptr - count, ptr, encoder, _setEncoder: false);
				}
				encoderFallbackBufferHelper.InternalFallback(c2, ref chars);
			}
			else
			{
				num++;
			}
		}
		return num;
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, System.Text.EncoderNLS encoder)
	{
		CheckMemorySection();
		EncoderReplacementFallback encoderReplacementFallback = null;
		char c = '\0';
		if (encoder != null)
		{
			c = encoder.charLeftOver;
			encoderReplacementFallback = encoder.Fallback as EncoderReplacementFallback;
		}
		else
		{
			encoderReplacementFallback = base.EncoderFallback as EncoderReplacementFallback;
		}
		char* ptr = chars + charCount;
		byte* ptr2 = bytes;
		char* ptr3 = chars;
		if (encoderReplacementFallback != null && encoderReplacementFallback.MaxCharCount == 1)
		{
			byte b = _mapUnicodeToBytes[(int)encoderReplacementFallback.DefaultString[0]];
			if (b != 0)
			{
				if (c > '\0')
				{
					if (byteCount == 0)
					{
						ThrowBytesOverflow(encoder, nothingEncoded: true);
					}
					*(bytes++) = b;
					byteCount--;
				}
				if (byteCount < charCount)
				{
					ThrowBytesOverflow(encoder, byteCount < 1);
					ptr = chars + byteCount;
				}
				while (chars < ptr)
				{
					char c2 = *chars;
					chars++;
					byte b2 = _mapUnicodeToBytes[(int)c2];
					if (b2 == 0 && c2 != 0)
					{
						*bytes = b;
					}
					else
					{
						*bytes = b2;
					}
					bytes++;
				}
				if (encoder != null)
				{
					encoder.charLeftOver = '\0';
					encoder.m_charsUsed = (int)(chars - ptr3);
				}
				return (int)(bytes - ptr2);
			}
		}
		EncoderFallbackBuffer encoderFallbackBuffer = null;
		byte* ptr4 = bytes + byteCount;
		EncoderFallbackBufferHelper encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
		if (c > '\0')
		{
			encoderFallbackBuffer = encoder.FallbackBuffer;
			encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
			encoderFallbackBufferHelper.InternalInitialize(chars, ptr, encoder, _setEncoder: true);
			encoderFallbackBufferHelper.InternalFallback(c, ref chars);
			if (encoderFallbackBuffer.Remaining > ptr4 - bytes)
			{
				ThrowBytesOverflow(encoder, nothingEncoded: true);
			}
		}
		char c3;
		while ((c3 = ((encoderFallbackBuffer != null) ? encoderFallbackBufferHelper.InternalGetNextChar() : '\0')) != 0 || chars < ptr)
		{
			if (c3 == '\0')
			{
				c3 = *chars;
				chars++;
			}
			byte b3 = _mapUnicodeToBytes[(int)c3];
			if (b3 == 0 && c3 != 0)
			{
				if (encoderFallbackBuffer == null)
				{
					encoderFallbackBuffer = ((encoder != null) ? encoder.FallbackBuffer : base.EncoderFallback.CreateFallbackBuffer());
					encoderFallbackBufferHelper = new EncoderFallbackBufferHelper(encoderFallbackBuffer);
					encoderFallbackBufferHelper.InternalInitialize(ptr - charCount, ptr, encoder, _setEncoder: true);
				}
				encoderFallbackBufferHelper.InternalFallback(c3, ref chars);
				if (encoderFallbackBuffer.Remaining > ptr4 - bytes)
				{
					chars--;
					encoderFallbackBufferHelper.InternalReset();
					ThrowBytesOverflow(encoder, chars == ptr3);
					break;
				}
				continue;
			}
			if (bytes >= ptr4)
			{
				if (encoderFallbackBuffer == null || !encoderFallbackBufferHelper.bFallingBack)
				{
					chars--;
				}
				ThrowBytesOverflow(encoder, chars == ptr3);
				break;
			}
			*bytes = b3;
			bytes++;
		}
		if (encoder != null)
		{
			if (encoderFallbackBuffer != null && !encoderFallbackBufferHelper.bUsedEncoder)
			{
				encoder.charLeftOver = '\0';
			}
			encoder.m_charsUsed = (int)(chars - ptr3);
		}
		return (int)(bytes - ptr2);
	}

	public unsafe override int GetCharCount(byte* bytes, int count, System.Text.DecoderNLS decoder)
	{
		CheckMemorySection();
		bool flag = false;
		DecoderReplacementFallback decoderReplacementFallback = null;
		if (decoder == null)
		{
			decoderReplacementFallback = base.DecoderFallback as DecoderReplacementFallback;
			flag = base.DecoderFallback is InternalDecoderBestFitFallback;
		}
		else
		{
			decoderReplacementFallback = decoder.Fallback as DecoderReplacementFallback;
			flag = decoder.Fallback is InternalDecoderBestFitFallback;
		}
		if (flag || (decoderReplacementFallback != null && decoderReplacementFallback.MaxCharCount == 1))
		{
			return count;
		}
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		DecoderFallbackBufferHelper decoderFallbackBufferHelper = new DecoderFallbackBufferHelper(decoderFallbackBuffer);
		int num = count;
		byte[] array = new byte[1];
		byte* ptr = bytes + count;
		while (bytes < ptr)
		{
			char c = _mapBytesToUnicode[(int)(*bytes)];
			bytes++;
			if (c == '\ufffd')
			{
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : base.DecoderFallback.CreateFallbackBuffer());
					decoderFallbackBufferHelper = new DecoderFallbackBufferHelper(decoderFallbackBuffer);
					decoderFallbackBufferHelper.InternalInitialize(ptr - count, null);
				}
				array[0] = *(bytes - 1);
				num--;
				num += decoderFallbackBufferHelper.InternalFallback(array, bytes);
			}
		}
		return num;
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, System.Text.DecoderNLS decoder)
	{
		CheckMemorySection();
		bool flag = false;
		byte* ptr = bytes + byteCount;
		byte* ptr2 = bytes;
		char* ptr3 = chars;
		DecoderReplacementFallback decoderReplacementFallback = null;
		if (decoder == null)
		{
			decoderReplacementFallback = base.DecoderFallback as DecoderReplacementFallback;
			flag = base.DecoderFallback is InternalDecoderBestFitFallback;
		}
		else
		{
			decoderReplacementFallback = decoder.Fallback as DecoderReplacementFallback;
			flag = decoder.Fallback is InternalDecoderBestFitFallback;
		}
		if (flag || (decoderReplacementFallback != null && decoderReplacementFallback.MaxCharCount == 1))
		{
			char c = decoderReplacementFallback?.DefaultString[0] ?? '?';
			if (charCount < byteCount)
			{
				ThrowCharsOverflow(decoder, charCount < 1);
				ptr = bytes + charCount;
			}
			while (bytes < ptr)
			{
				char c2;
				if (flag)
				{
					if (arrayBytesBestFit == null)
					{
						ReadBestFitTable();
					}
					c2 = arrayBytesBestFit[*bytes];
				}
				else
				{
					c2 = _mapBytesToUnicode[(int)(*bytes)];
				}
				bytes++;
				if (c2 == '\ufffd')
				{
					*chars = c;
				}
				else
				{
					*chars = c2;
				}
				chars++;
			}
			if (decoder != null)
			{
				decoder.m_bytesUsed = (int)(bytes - ptr2);
			}
			return (int)(chars - ptr3);
		}
		DecoderFallbackBuffer decoderFallbackBuffer = null;
		byte[] array = new byte[1];
		char* ptr4 = chars + charCount;
		DecoderFallbackBufferHelper decoderFallbackBufferHelper = new DecoderFallbackBufferHelper(null);
		while (bytes < ptr)
		{
			char c3 = _mapBytesToUnicode[(int)(*bytes)];
			bytes++;
			if (c3 == '\ufffd')
			{
				if (decoderFallbackBuffer == null)
				{
					decoderFallbackBuffer = ((decoder != null) ? decoder.FallbackBuffer : base.DecoderFallback.CreateFallbackBuffer());
					decoderFallbackBufferHelper = new DecoderFallbackBufferHelper(decoderFallbackBuffer);
					decoderFallbackBufferHelper.InternalInitialize(ptr - byteCount, ptr4);
				}
				array[0] = *(bytes - 1);
				if (!decoderFallbackBufferHelper.InternalFallback(array, bytes, ref chars))
				{
					bytes--;
					decoderFallbackBufferHelper.InternalReset();
					ThrowCharsOverflow(decoder, bytes == ptr2);
					break;
				}
			}
			else
			{
				if (chars >= ptr4)
				{
					bytes--;
					ThrowCharsOverflow(decoder, bytes == ptr2);
					break;
				}
				*chars = c3;
				chars++;
			}
		}
		if (decoder != null)
		{
			decoder.m_bytesUsed = (int)(bytes - ptr2);
		}
		return (int)(chars - ptr3);
	}

	public override int GetMaxByteCount(int charCount)
	{
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = (long)charCount + 1L;
		if (base.EncoderFallback.MaxCharCount > 1)
		{
			num *= base.EncoderFallback.MaxCharCount;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("charCount", System.SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return (int)num;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = byteCount;
		if (base.DecoderFallback.MaxCharCount > 1)
		{
			num *= base.DecoderFallback.MaxCharCount;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("byteCount", System.SR.ArgumentOutOfRange_GetCharCountOverflow);
		}
		return (int)num;
	}
}
