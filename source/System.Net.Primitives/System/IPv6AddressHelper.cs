namespace System;

internal static class IPv6AddressHelper
{
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

	internal unsafe static bool IsValidStrict(char* name, int start, ref int end)
	{
		int num = 0;
		int num2 = 0;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = true;
		int start2 = 1;
		bool flag4 = false;
		if (start < end && name[start] == '[')
		{
			start++;
			flag4 = true;
		}
		if (name[start] == ':' && (start + 1 >= end || name[start + 1] != ':'))
		{
			return false;
		}
		for (int i = start; i < end; i++)
		{
			if (Uri.IsHexDigit(name[i]))
			{
				num2++;
				flag3 = false;
				continue;
			}
			if (num2 > 4)
			{
				return false;
			}
			if (num2 != 0)
			{
				num++;
				start2 = i - num2;
				num2 = 0;
			}
			char c = name[i];
			if ((uint)c <= 46u)
			{
				if (c != '%')
				{
					if (c != '.')
					{
						goto IL_01e6;
					}
					if (flag2)
					{
						return false;
					}
					i = end;
					if (!System.IPv4AddressHelper.IsValid(name, start2, ref i, allowIPv6: true, notImplicitFile: false, unknownScheme: false))
					{
						return false;
					}
					num++;
					start2 = i - num2;
					num2 = 0;
					flag2 = true;
					i--;
				}
				else
				{
					while (i + 1 < end)
					{
						i++;
						if (name[i] == ']')
						{
							goto IL_00f1;
						}
						if (name[i] != '/')
						{
							continue;
						}
						goto IL_01b4;
					}
				}
			}
			else
			{
				if (c == '/')
				{
					goto IL_01b4;
				}
				if (c != ':')
				{
					if (c == ']')
					{
						goto IL_00f1;
					}
					goto IL_01e6;
				}
				if (i > 0 && name[i - 1] == ':')
				{
					if (flag)
					{
						return false;
					}
					flag = true;
					flag3 = false;
				}
				else
				{
					flag3 = true;
				}
			}
			num2 = 0;
			continue;
			IL_01e6:
			return false;
			IL_01b4:
			return false;
			IL_00f1:
			if (!flag4)
			{
				return false;
			}
			flag4 = false;
			if (i + 1 < end && name[i + 1] != ':')
			{
				return false;
			}
			if (i + 3 < end && name[i + 2] == '0' && name[i + 3] == 'x')
			{
				for (i += 4; i < end; i++)
				{
					if (!Uri.IsHexDigit(name[i]))
					{
						return false;
					}
				}
				continue;
			}
			for (i += 2; i < end; i++)
			{
				if (name[i] < '0' || name[i] > '9')
				{
					return false;
				}
			}
		}
		if (num2 != 0)
		{
			if (num2 > 4)
			{
				return false;
			}
			num++;
		}
		if (!flag3 && (flag ? (num < 8) : (num == 8)))
		{
			return !flag4;
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
						num = System.IPv4AddressHelper.ParseHostNumber(address, i, j);
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
