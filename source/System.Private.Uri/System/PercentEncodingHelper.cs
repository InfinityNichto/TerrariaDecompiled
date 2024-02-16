using System.Buffers;
using System.Text;

namespace System;

internal static class PercentEncodingHelper
{
	public unsafe static int UnescapePercentEncodedUTF8Sequence(char* input, int length, ref System.Text.ValueStringBuilder dest, bool isQuery, bool iriParsing)
	{
		uint num = 0u;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int bytesConsumed = 0;
		while (true)
		{
			int num5 = num3 + num2 * 3;
			while (true)
			{
				if ((uint)(length - num5) > 2u && input[num5] == '%')
				{
					uint num6 = input[num5 + 1];
					if ((uint)((num6 - 65) & -33) <= 5)
					{
						num6 = (num6 | 0x20) - 97 + 10;
					}
					else
					{
						if (num6 - 56 > 1)
						{
							goto IL_0135;
						}
						num6 -= 48;
					}
					uint num7 = (uint)(input[num5 + 2] - 48);
					if (num7 > 9)
					{
						if ((uint)((num7 - 17) & -33) > 5)
						{
							goto IL_0135;
						}
						num7 = ((num7 + 48) | 0x20) - 97 + 10;
					}
					num6 = (num6 << 4) | num7;
					num = ((!BitConverter.IsLittleEndian) ? ((num << 8) | num6) : ((num >> 8) | (num6 << 24)));
					if (++num2 == 4)
					{
						break;
					}
					num5 += 3;
					continue;
				}
				goto IL_0135;
				IL_0135:
				if (num2 > 1)
				{
					num = ((bytesConsumed != 1) ? ((!BitConverter.IsLittleEndian) ? (num << 32 - (num2 << 3)) : (num >> 32 - (num2 << 3))) : ((!BitConverter.IsLittleEndian) ? (num << 8) : (num >> 8)));
					break;
				}
				if ((num2 | num4) == 0)
				{
					return num3;
				}
				num2 *= 3;
				dest.Append(input + num3 - num4, num4 + num2);
				return num3 + num2;
			}
			uint num8 = num;
			if (Rune.DecodeFromUtf8(new ReadOnlySpan<byte>(&num8, num2), out var result, out bytesConsumed) == OperationStatus.Done && (!iriParsing || IriHelper.CheckIriUnicodeRange((uint)result.Value, isQuery)))
			{
				if (num4 != 0)
				{
					dest.Append(input + num3 - num4, num4);
					num4 = 0;
				}
				dest.Append(result);
			}
			else
			{
				num4 += bytesConsumed * 3;
			}
			num2 -= bytesConsumed;
			num3 += bytesConsumed * 3;
		}
	}
}
