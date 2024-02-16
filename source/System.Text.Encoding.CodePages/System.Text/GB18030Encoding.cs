namespace System.Text;

internal sealed class GB18030Encoding : DBCSCodePageEncoding
{
	internal sealed class GB18030Decoder : System.Text.DecoderNLS
	{
		internal short bLeftOver1 = -1;

		internal short bLeftOver2 = -1;

		internal short bLeftOver3 = -1;

		internal short bLeftOver4 = -1;

		internal override bool HasState => bLeftOver1 >= 0;

		internal GB18030Decoder(EncodingNLS encoding)
			: base(encoding)
		{
		}

		public override void Reset()
		{
			bLeftOver1 = -1;
			bLeftOver2 = -1;
			bLeftOver3 = -1;
			bLeftOver4 = -1;
			if (m_fallbackBuffer != null)
			{
				m_fallbackBuffer.Reset();
			}
		}
	}

	internal unsafe char* map4BytesToUnicode = null;

	internal unsafe byte* mapUnicodeTo4BytesFlags = null;

	private readonly ushort[] _tableUnicodeToGBDiffs = new ushort[439]
	{
		32896, 36, 32769, 2, 32770, 7, 32770, 5, 32769, 31,
		32769, 8, 32770, 6, 32771, 1, 32770, 4, 32770, 3,
		32769, 1, 32770, 1, 32769, 4, 32769, 17, 32769, 7,
		32769, 15, 32769, 24, 32769, 3, 32769, 4, 32769, 29,
		32769, 98, 32769, 1, 32769, 1, 32769, 1, 32769, 1,
		32769, 1, 32769, 1, 32769, 1, 32769, 28, 43199, 87,
		32769, 15, 32769, 101, 32769, 1, 32771, 13, 32769, 183,
		32785, 1, 32775, 7, 32785, 1, 32775, 55, 32769, 14,
		32832, 1, 32769, 7102, 32769, 2, 32772, 1, 32770, 2,
		32770, 7, 32770, 9, 32769, 1, 32770, 1, 32769, 5,
		32769, 112, 41699, 86, 32769, 1, 32769, 3, 32769, 12,
		32769, 10, 32769, 62, 32780, 4, 32778, 22, 32772, 2,
		32772, 110, 32769, 6, 32769, 1, 32769, 3, 32769, 4,
		32769, 2, 32772, 2, 32769, 1, 32769, 1, 32773, 2,
		32769, 5, 32772, 5, 32769, 10, 32769, 3, 32769, 5,
		32769, 13, 32770, 2, 32772, 6, 32770, 37, 32769, 3,
		32769, 11, 32769, 25, 32769, 82, 32769, 333, 32778, 10,
		32808, 100, 32844, 4, 32804, 13, 32783, 3, 32771, 10,
		32770, 16, 32770, 8, 32770, 8, 32770, 3, 32769, 2,
		32770, 18, 32772, 31, 32770, 2, 32769, 54, 32769, 1,
		32769, 2110, 65104, 2, 65108, 3, 65111, 2, 65112, 65117,
		10, 65118, 15, 65131, 2, 65134, 3, 65137, 4, 65139,
		2, 65140, 65141, 3, 65145, 14, 65156, 293, 43402, 43403,
		43404, 43405, 43406, 43407, 43408, 43409, 43410, 43411, 43412, 43413,
		4, 32772, 1, 32787, 5, 32770, 2, 32777, 20, 43401,
		2, 32851, 7, 32772, 2, 32854, 5, 32771, 6, 32805,
		246, 32778, 7, 32769, 113, 32769, 234, 32770, 12, 32771,
		2, 32769, 34, 32769, 9, 32769, 2, 32770, 2, 32769,
		113, 65110, 43, 65109, 298, 65114, 111, 65116, 11, 65115,
		765, 65120, 85, 65119, 96, 65122, 65125, 14, 65123, 147,
		65124, 218, 65128, 287, 65129, 113, 65130, 885, 65135, 264,
		65136, 471, 65138, 116, 65144, 4, 65143, 43, 65146, 248,
		65147, 373, 65149, 20, 65148, 193, 65152, 5, 65153, 82,
		65154, 16, 65155, 441, 65157, 50, 65158, 2, 65159, 4,
		65160, 65161, 1, 65162, 65163, 20, 65165, 3, 65164, 22,
		65167, 65166, 703, 65174, 39, 65171, 65172, 65173, 65175, 65170,
		111, 65176, 65177, 65178, 65179, 65180, 65181, 65182, 148, 65183,
		81, 53670, 14426, 36716, 1, 32859, 1, 32798, 13, 32801,
		1, 32771, 5, 32769, 7, 32769, 4, 32770, 4, 32770,
		8, 32769, 7, 32769, 16, 32770, 14, 32769, 4295, 32769,
		76, 32769, 27, 32769, 81, 32769, 9, 32769, 26, 32772,
		1, 32769, 1, 32770, 3, 32769, 6, 32771, 1, 32770,
		2, 32771, 1030, 32770, 1, 32786, 4, 32778, 1, 32772,
		1, 32782, 1, 32772, 149, 32862, 129, 32774, 26
	};

	internal unsafe GB18030Encoding()
		: base(54936, 936, System.Text.EncoderFallback.ReplacementFallback, System.Text.DecoderFallback.ReplacementFallback)
	{
	}

	protected unsafe override void LoadManagedCodePage()
	{
		iExtraBytes = 87032;
		base.LoadManagedCodePage();
		byte* ptr = (byte*)(void*)safeNativeMemoryHandle.DangerousGetHandle();
		mapUnicodeTo4BytesFlags = ptr + 262144;
		map4BytesToUnicode = (char*)(ptr + 262144 + 8192);
		char c = '\0';
		ushort num = 0;
		for (int i = 0; i < _tableUnicodeToGBDiffs.Length; i++)
		{
			ushort num2 = _tableUnicodeToGBDiffs[i];
			if ((num2 & 0x8000u) != 0)
			{
				if (num2 > 36864 && num2 != 53670)
				{
					mapBytesToUnicode[(int)num2] = c;
					mapUnicodeToBytes[(int)c] = num2;
					c = (char)(c + 1);
				}
				else
				{
					c = (char)(c + (ushort)(num2 & 0x7FFF));
				}
				continue;
			}
			while (num2 > 0)
			{
				map4BytesToUnicode[(int)num] = c;
				mapUnicodeToBytes[(int)c] = num;
				byte* num3 = mapUnicodeTo4BytesFlags + c / 8;
				*num3 |= (byte)(1 << c % 8);
				c = (char)(c + 1);
				num++;
				num2--;
			}
		}
	}

	internal unsafe bool Is4Byte(char charTest)
	{
		byte b = mapUnicodeTo4BytesFlags[charTest / 8];
		if (b != 0)
		{
			return (b & (1 << charTest % 8)) != 0;
		}
		return false;
	}

	public unsafe override int GetByteCount(char* chars, int count, System.Text.EncoderNLS encoder)
	{
		return GetBytes(chars, count, null, 0, encoder);
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, System.Text.EncoderNLS encoder)
	{
		char c = '\0';
		if (encoder != null)
		{
			c = encoder.charLeftOver;
		}
		EncodingByteBuffer encodingByteBuffer = new EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);
		while (true)
		{
			if (encodingByteBuffer.MoreData)
			{
				char nextChar = encodingByteBuffer.GetNextChar();
				if (c != 0)
				{
					if (!char.IsLowSurrogate(nextChar))
					{
						encodingByteBuffer.MovePrevious(bThrow: false);
						if (encodingByteBuffer.Fallback(c))
						{
							c = '\0';
							continue;
						}
						c = '\0';
					}
					else
					{
						int num = (c - 55296 << 10) + (nextChar - 56320);
						byte b = (byte)(num % 10 + 48);
						num /= 10;
						byte b2 = (byte)(num % 126 + 129);
						num /= 126;
						byte b3 = (byte)(num % 10 + 48);
						num /= 10;
						c = '\0';
						if (encodingByteBuffer.AddByte((byte)(num + 144), b3, b2, b))
						{
							c = '\0';
							continue;
						}
						encodingByteBuffer.MovePrevious(bThrow: false);
					}
				}
				else if (nextChar <= '\u007f')
				{
					if (encodingByteBuffer.AddByte((byte)nextChar))
					{
						continue;
					}
				}
				else
				{
					if (char.IsHighSurrogate(nextChar))
					{
						c = nextChar;
						continue;
					}
					if (char.IsLowSurrogate(nextChar))
					{
						if (encodingByteBuffer.Fallback(nextChar))
						{
							continue;
						}
					}
					else
					{
						ushort num2 = mapUnicodeToBytes[(int)nextChar];
						if (Is4Byte(nextChar))
						{
							byte b4 = (byte)(num2 % 10 + 48);
							num2 /= 10;
							byte b5 = (byte)(num2 % 126 + 129);
							num2 /= 126;
							byte b6 = (byte)(num2 % 10 + 48);
							num2 /= 10;
							if (encodingByteBuffer.AddByte((byte)(num2 + 129), b6, b5, b4))
							{
								continue;
							}
						}
						else if (encodingByteBuffer.AddByte((byte)(num2 >> 8), (byte)(num2 & 0xFFu)))
						{
							continue;
						}
					}
				}
			}
			if ((encoder != null && !encoder.MustFlush) || c <= '\0')
			{
				break;
			}
			encodingByteBuffer.Fallback(c);
			c = '\0';
		}
		if (encoder != null)
		{
			if (bytes != null)
			{
				encoder.charLeftOver = c;
			}
			encoder.m_charsUsed = encodingByteBuffer.CharsUsed;
		}
		return encodingByteBuffer.Count;
	}

	internal bool IsGBLeadByte(short ch)
	{
		if (ch >= 129)
		{
			return ch <= 254;
		}
		return false;
	}

	internal bool IsGBTwoByteTrailing(short ch)
	{
		if (ch < 64 || ch > 126)
		{
			if (ch >= 128)
			{
				return ch <= 254;
			}
			return false;
		}
		return true;
	}

	internal bool IsGBFourByteTrailing(short ch)
	{
		if (ch >= 48)
		{
			return ch <= 57;
		}
		return false;
	}

	internal int GetFourBytesOffset(short offset1, short offset2, short offset3, short offset4)
	{
		return (offset1 - 129) * 10 * 126 * 10 + (offset2 - 48) * 126 * 10 + (offset3 - 129) * 10 + offset4 - 48;
	}

	public unsafe override int GetCharCount(byte* bytes, int count, System.Text.DecoderNLS baseDecoder)
	{
		return GetChars(bytes, count, null, 0, baseDecoder);
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, System.Text.DecoderNLS baseDecoder)
	{
		GB18030Decoder gB18030Decoder = (GB18030Decoder)baseDecoder;
		EncodingCharBuffer encodingCharBuffer = new EncodingCharBuffer(this, gB18030Decoder, chars, charCount, bytes, byteCount);
		short num = -1;
		short num2 = -1;
		short num3 = -1;
		short num4 = -1;
		if (gB18030Decoder != null && gB18030Decoder.bLeftOver1 != -1)
		{
			num = gB18030Decoder.bLeftOver1;
			num2 = gB18030Decoder.bLeftOver2;
			num3 = gB18030Decoder.bLeftOver3;
			num4 = gB18030Decoder.bLeftOver4;
			while (num != -1)
			{
				if (!IsGBLeadByte(num))
				{
					if (num <= 127)
					{
						if (!encodingCharBuffer.AddChar((char)num))
						{
							break;
						}
					}
					else if (!encodingCharBuffer.Fallback((byte)num))
					{
						break;
					}
					num = num2;
					num2 = num3;
					num3 = num4;
					num4 = -1;
					continue;
				}
				while (num2 == -1 || (IsGBFourByteTrailing(num2) && num4 == -1))
				{
					if (!encodingCharBuffer.MoreData)
					{
						if (gB18030Decoder.MustFlush)
						{
							break;
						}
						if (chars != null)
						{
							gB18030Decoder.bLeftOver1 = num;
							gB18030Decoder.bLeftOver2 = num2;
							gB18030Decoder.bLeftOver3 = num3;
							gB18030Decoder.bLeftOver4 = num4;
						}
						gB18030Decoder.m_bytesUsed = encodingCharBuffer.BytesUsed;
						return encodingCharBuffer.Count;
					}
					if (num2 == -1)
					{
						num2 = encodingCharBuffer.GetNextByte();
					}
					else if (num3 == -1)
					{
						num3 = encodingCharBuffer.GetNextByte();
					}
					else
					{
						num4 = encodingCharBuffer.GetNextByte();
					}
				}
				if (IsGBTwoByteTrailing(num2))
				{
					int num5 = num << 8;
					num5 |= (byte)num2;
					if (!encodingCharBuffer.AddChar(mapBytesToUnicode[num5], 2))
					{
						break;
					}
					num = -1;
					num2 = -1;
				}
				else if (IsGBFourByteTrailing(num2) && IsGBLeadByte(num3) && IsGBFourByteTrailing(num4))
				{
					int fourBytesOffset = GetFourBytesOffset(num, num2, num3, num4);
					if (fourBytesOffset <= 39419)
					{
						if (!encodingCharBuffer.AddChar(map4BytesToUnicode[fourBytesOffset], 4))
						{
							break;
						}
					}
					else if (fourBytesOffset >= 189000 && fourBytesOffset <= 1237575)
					{
						fourBytesOffset -= 189000;
						if (!encodingCharBuffer.AddChar((char)(55296 + fourBytesOffset / 1024), (char)(56320 + fourBytesOffset % 1024), 4))
						{
							break;
						}
					}
					else if (!encodingCharBuffer.Fallback((byte)num, (byte)num2, (byte)num3, (byte)num4))
					{
						break;
					}
					num = -1;
					num2 = -1;
					num3 = -1;
					num4 = -1;
				}
				else
				{
					if (!encodingCharBuffer.Fallback((byte)num))
					{
						break;
					}
					num = num2;
					num2 = num3;
					num3 = num4;
					num4 = -1;
				}
			}
		}
		while (encodingCharBuffer.MoreData)
		{
			byte nextByte = encodingCharBuffer.GetNextByte();
			if (nextByte <= 127)
			{
				if (!encodingCharBuffer.AddChar((char)nextByte))
				{
					break;
				}
			}
			else if (IsGBLeadByte(nextByte))
			{
				if (encodingCharBuffer.MoreData)
				{
					byte nextByte2 = encodingCharBuffer.GetNextByte();
					if (IsGBTwoByteTrailing(nextByte2))
					{
						int num6 = nextByte << 8;
						num6 |= nextByte2;
						if (!encodingCharBuffer.AddChar(mapBytesToUnicode[num6], 2))
						{
							break;
						}
					}
					else if (IsGBFourByteTrailing(nextByte2))
					{
						if (encodingCharBuffer.EvenMoreData(2))
						{
							byte nextByte3 = encodingCharBuffer.GetNextByte();
							byte nextByte4 = encodingCharBuffer.GetNextByte();
							if (IsGBLeadByte(nextByte3) && IsGBFourByteTrailing(nextByte4))
							{
								int fourBytesOffset2 = GetFourBytesOffset(nextByte, nextByte2, nextByte3, nextByte4);
								if (fourBytesOffset2 <= 39419)
								{
									if (!encodingCharBuffer.AddChar(map4BytesToUnicode[fourBytesOffset2], 4))
									{
										break;
									}
								}
								else if (fourBytesOffset2 >= 189000 && fourBytesOffset2 <= 1237575)
								{
									fourBytesOffset2 -= 189000;
									if (!encodingCharBuffer.AddChar((char)(55296 + fourBytesOffset2 / 1024), (char)(56320 + fourBytesOffset2 % 1024), 4))
									{
										break;
									}
								}
								else if (!encodingCharBuffer.Fallback(nextByte, nextByte2, nextByte3, nextByte4))
								{
									break;
								}
							}
							else
							{
								encodingCharBuffer.AdjustBytes(-3);
								if (!encodingCharBuffer.Fallback(nextByte))
								{
									break;
								}
							}
							continue;
						}
						if (gB18030Decoder != null && !gB18030Decoder.MustFlush)
						{
							if (chars != null)
							{
								num = nextByte;
								num2 = nextByte2;
								num3 = (short)((!encodingCharBuffer.MoreData) ? (-1) : encodingCharBuffer.GetNextByte());
								num4 = -1;
							}
							break;
						}
						if (!encodingCharBuffer.Fallback(nextByte, nextByte2))
						{
							break;
						}
					}
					else
					{
						encodingCharBuffer.AdjustBytes(-1);
						if (!encodingCharBuffer.Fallback(nextByte))
						{
							break;
						}
					}
					continue;
				}
				if (gB18030Decoder != null && !gB18030Decoder.MustFlush)
				{
					if (chars != null)
					{
						num = nextByte;
						num2 = -1;
						num3 = -1;
						num4 = -1;
					}
					break;
				}
				if (!encodingCharBuffer.Fallback(nextByte))
				{
					break;
				}
			}
			else if (!encodingCharBuffer.Fallback(nextByte))
			{
				break;
			}
		}
		if (gB18030Decoder != null)
		{
			if (chars != null)
			{
				gB18030Decoder.bLeftOver1 = num;
				gB18030Decoder.bLeftOver2 = num2;
				gB18030Decoder.bLeftOver3 = num3;
				gB18030Decoder.bLeftOver4 = num4;
			}
			gB18030Decoder.m_bytesUsed = encodingCharBuffer.BytesUsed;
		}
		return encodingCharBuffer.Count;
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
		num *= 4;
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
		long num = (long)byteCount + 3L;
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

	public override Decoder GetDecoder()
	{
		return new GB18030Decoder(this);
	}
}
