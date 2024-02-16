namespace System;

internal static class IPv6AddressHelper
{
	internal static string ParseCanonicalName(string str, int start, ref bool isLoopback, ref string scopeId)
	{
		Span<ushort> span = stackalloc ushort[8];
		span.Clear();
		Parse(str, span, start, ref scopeId);
		isLoopback = IsLoopback(span);
		(int longestSequenceStart, int longestSequenceLength) tuple = FindCompressionRange(span);
		int item = tuple.longestSequenceStart;
		int item2 = tuple.longestSequenceLength;
		bool flag = ShouldHaveIpv4Embedded(span);
		Span<char> span2 = stackalloc char[48];
		span2[0] = '[';
		int num = 1;
		for (int i = 0; i < 8; i++)
		{
			int charsWritten;
			if (flag && i == 6)
			{
				span2[num++] = ':';
				bool flag2 = (span[i] >> 8).TryFormat(span2.Slice(num), out charsWritten);
				num += charsWritten;
				span2[num++] = '.';
				flag2 = (span[i] & 0xFF).TryFormat(span2.Slice(num), out charsWritten);
				num += charsWritten;
				span2[num++] = '.';
				flag2 = (span[i + 1] >> 8).TryFormat(span2.Slice(num), out charsWritten);
				num += charsWritten;
				span2[num++] = '.';
				flag2 = (span[i + 1] & 0xFF).TryFormat(span2.Slice(num), out charsWritten);
				num += charsWritten;
				break;
			}
			if (item == i)
			{
				span2[num++] = ':';
			}
			if (item <= i && item2 == 8)
			{
				span2[num++] = ':';
				break;
			}
			if (item > i || i >= item2)
			{
				if (i != 0)
				{
					span2[num++] = ':';
				}
				bool flag2 = span[i].TryFormat(span2.Slice(num), out charsWritten, "x");
				num += charsWritten;
			}
		}
		span2[num++] = ']';
		return new string(span2.Slice(0, num));
	}

	private static bool IsLoopback(ReadOnlySpan<ushort> numbers)
	{
		if (numbers[0] == 0 && numbers[1] == 0 && numbers[2] == 0 && numbers[3] == 0 && numbers[4] == 0)
		{
			if (numbers[5] != 0 || numbers[6] != 0 || numbers[7] != 1)
			{
				if (numbers[6] == 32512 && numbers[7] == 1)
				{
					if (numbers[5] != 0)
					{
						return numbers[5] == ushort.MaxValue;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private unsafe static bool InternalIsValid(char* name, int start, ref int end, bool validateStrictAddress)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = true;
		int start2 = 1;
		if (name[start] == ':' && (start + 1 >= end || name[start + 1] != ':'))
		{
			return false;
		}
		int i;
		for (i = start; i < end; i++)
		{
			bool num3;
			if (!flag3)
			{
				num3 = Uri.IsHexDigit(name[i]);
			}
			else
			{
				if (name[i] < '0')
				{
					goto IL_0079;
				}
				num3 = name[i] <= '9';
			}
			if (num3)
			{
				num2++;
				flag4 = false;
				continue;
			}
			goto IL_0079;
			IL_0079:
			if (num2 > 4)
			{
				return false;
			}
			if (num2 != 0)
			{
				num++;
				start2 = i - num2;
			}
			char c = name[i];
			if ((uint)c <= 46u)
			{
				if (c == '%')
				{
					while (true)
					{
						if (++i == end)
						{
							return false;
						}
						if (name[i] == ']')
						{
							break;
						}
						if (name[i] != '/')
						{
							continue;
						}
						goto IL_011c;
					}
					goto IL_00ee;
				}
				if (c != '.')
				{
					goto IL_015c;
				}
				if (flag2)
				{
					return false;
				}
				i = end;
				if (!IPv4AddressHelper.IsValid(name, start2, ref i, allowIPv6: true, notImplicitFile: false, unknownScheme: false))
				{
					return false;
				}
				num++;
				flag2 = true;
				i--;
			}
			else
			{
				if (c == '/')
				{
					goto IL_011c;
				}
				if (c != ':')
				{
					if (c == ']')
					{
						goto IL_00ee;
					}
					goto IL_015c;
				}
				if (i > 0 && name[i - 1] == ':')
				{
					if (flag)
					{
						return false;
					}
					flag = true;
					flag4 = false;
				}
				else
				{
					flag4 = true;
				}
			}
			goto IL_015e;
			IL_015c:
			return false;
			IL_011c:
			if (validateStrictAddress)
			{
				return false;
			}
			if (num == 0 || flag3)
			{
				return false;
			}
			flag3 = true;
			flag4 = true;
			goto IL_015e;
			IL_015e:
			num2 = 0;
			continue;
			IL_00ee:
			start = i;
			i = end;
		}
		if (flag3 && (num2 < 1 || num2 > 2))
		{
			return false;
		}
		int num4 = 8 + (flag3 ? 1 : 0);
		if (!flag4 && num2 <= 4 && (flag ? (num < num4) : (num == num4)))
		{
			if (i == end + 1)
			{
				end = start + 1;
				return true;
			}
			return false;
		}
		return false;
	}

	internal unsafe static bool IsValid(char* name, int start, ref int end)
	{
		return InternalIsValid(name, start, ref end, validateStrictAddress: false);
	}

	internal static (int longestSequenceStart, int longestSequenceLength) FindCompressionRange(ReadOnlySpan<ushort> numbers)
	{
		int num = 0;
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < numbers.Length; i++)
		{
			if (numbers[i] == 0)
			{
				num3++;
				if (num3 > num)
				{
					num = num3;
					num2 = i - num3 + 1;
				}
			}
			else
			{
				num3 = 0;
			}
		}
		if (num <= 1)
		{
			return (longestSequenceStart: -1, longestSequenceLength: -1);
		}
		return (longestSequenceStart: num2, longestSequenceLength: num2 + num);
	}

	internal static bool ShouldHaveIpv4Embedded(ReadOnlySpan<ushort> numbers)
	{
		if (numbers[0] == 0 && numbers[1] == 0 && numbers[2] == 0 && numbers[3] == 0 && numbers[6] != 0)
		{
			if (numbers[4] == 0 && (numbers[5] == 0 || numbers[5] == ushort.MaxValue))
			{
				return true;
			}
			if (numbers[4] == ushort.MaxValue && numbers[5] == 0)
			{
				return true;
			}
		}
		if (numbers[4] == 0)
		{
			return numbers[5] == 24318;
		}
		return false;
	}

	internal static void Parse(ReadOnlySpan<char> address, Span<ushort> numbers, int start, ref string scopeId)
	{
		int num = 0;
		int num2 = 0;
		int num3 = -1;
		bool flag = true;
		int num4 = 0;
		if (address[start] == '[')
		{
			start++;
		}
		int i = start;
		while (i < address.Length && address[i] != ']')
		{
			switch (address[i])
			{
			case '%':
				if (flag)
				{
					numbers[num2++] = (ushort)num;
					flag = false;
				}
				start = i;
				for (i++; i < address.Length && address[i] != ']' && address[i] != '/'; i++)
				{
				}
				scopeId = new string(address.Slice(start, i - start));
				for (; i < address.Length && address[i] != ']'; i++)
				{
				}
				break;
			case ':':
			{
				numbers[num2++] = (ushort)num;
				num = 0;
				i++;
				if (address[i] == ':')
				{
					num3 = num2;
					i++;
				}
				else if (num3 < 0 && num2 < 6)
				{
					break;
				}
				for (int j = i; j < address.Length && address[j] != ']' && address[j] != ':' && address[j] != '%' && address[j] != '/' && j < i + 4; j++)
				{
					if (address[j] == '.')
					{
						for (; j < address.Length && address[j] != ']' && address[j] != '/' && address[j] != '%'; j++)
						{
						}
						num = IPv4AddressHelper.ParseHostNumber(address, i, j);
						numbers[num2++] = (ushort)(num >> 16);
						numbers[num2++] = (ushort)num;
						i = j;
						num = 0;
						flag = false;
						break;
					}
				}
				break;
			}
			case '/':
				if (flag)
				{
					numbers[num2++] = (ushort)num;
					flag = false;
				}
				for (i++; address[i] != ']'; i++)
				{
					num4 = num4 * 10 + (address[i] - 48);
				}
				break;
			default:
				num = num * 16 + Uri.FromHex(address[i++]);
				break;
			}
		}
		if (flag)
		{
			numbers[num2++] = (ushort)num;
		}
		if (num3 <= 0)
		{
			return;
		}
		int num5 = 7;
		int num6 = num2 - 1;
		if (num6 != num5)
		{
			for (int num7 = num2 - num3; num7 > 0; num7--)
			{
				numbers[num5--] = numbers[num6];
				numbers[num6--] = 0;
			}
		}
	}
}
