using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net;

internal static class IPAddressParser
{
	internal static IPAddress Parse(ReadOnlySpan<char> ipSpan, bool tryParse)
	{
		long address;
		if (ipSpan.Contains(':'))
		{
			Span<ushort> span = stackalloc ushort[8];
			span.Clear();
			if (Ipv6StringToAddress(ipSpan, span, 8, out var scope))
			{
				return new IPAddress(span, scope);
			}
		}
		else if (Ipv4StringToAddress(ipSpan, out address))
		{
			return new IPAddress(address);
		}
		if (tryParse)
		{
			return null;
		}
		throw new FormatException(System.SR.dns_bad_ip_address, new SocketException(SocketError.InvalidArgument));
	}

	internal unsafe static string IPv4AddressToString(uint address)
	{
		char* ptr = stackalloc char[15];
		int length = IPv4AddressToStringHelper(address, ptr);
		return new string(ptr, 0, length);
	}

	internal unsafe static void IPv4AddressToString(uint address, StringBuilder destination)
	{
		char* ptr = stackalloc char[15];
		int valueCount = IPv4AddressToStringHelper(address, ptr);
		destination.Append(ptr, valueCount);
	}

	internal unsafe static bool IPv4AddressToString(uint address, Span<char> formatted, out int charsWritten)
	{
		if (formatted.Length < 15)
		{
			charsWritten = 0;
			return false;
		}
		fixed (char* addressString = &MemoryMarshal.GetReference(formatted))
		{
			charsWritten = IPv4AddressToStringHelper(address, addressString);
		}
		return true;
	}

	private unsafe static int IPv4AddressToStringHelper(uint address, char* addressString)
	{
		int offset = 0;
		address = (uint)IPAddress.NetworkToHostOrder((int)address);
		FormatIPv4AddressNumber((int)((address >> 24) & 0xFF), addressString, ref offset);
		addressString[offset++] = '.';
		FormatIPv4AddressNumber((int)((address >> 16) & 0xFF), addressString, ref offset);
		addressString[offset++] = '.';
		FormatIPv4AddressNumber((int)((address >> 8) & 0xFF), addressString, ref offset);
		addressString[offset++] = '.';
		FormatIPv4AddressNumber((int)(address & 0xFF), addressString, ref offset);
		return offset;
	}

	internal static string IPv6AddressToString(ushort[] address, uint scopeId)
	{
		StringBuilder sb = IPv6AddressToStringHelper(address, scopeId);
		return System.Text.StringBuilderCache.GetStringAndRelease(sb);
	}

	internal static bool IPv6AddressToString(ushort[] address, uint scopeId, Span<char> destination, out int charsWritten)
	{
		StringBuilder stringBuilder = IPv6AddressToStringHelper(address, scopeId);
		if (destination.Length < stringBuilder.Length)
		{
			System.Text.StringBuilderCache.Release(stringBuilder);
			charsWritten = 0;
			return false;
		}
		stringBuilder.CopyTo(0, destination, stringBuilder.Length);
		charsWritten = stringBuilder.Length;
		System.Text.StringBuilderCache.Release(stringBuilder);
		return true;
	}

	internal static StringBuilder IPv6AddressToStringHelper(ushort[] address, uint scopeId)
	{
		StringBuilder stringBuilder = System.Text.StringBuilderCache.Acquire(65);
		if (System.IPv6AddressHelper.ShouldHaveIpv4Embedded(address))
		{
			AppendSections(address, 0, 6, stringBuilder);
			if (stringBuilder[stringBuilder.Length - 1] != ':')
			{
				stringBuilder.Append(':');
			}
			IPv4AddressToString(ExtractIPv4Address(address), stringBuilder);
		}
		else
		{
			AppendSections(address, 0, 8, stringBuilder);
		}
		if (scopeId != 0)
		{
			stringBuilder.Append('%').Append(scopeId);
		}
		return stringBuilder;
	}

	private unsafe static void FormatIPv4AddressNumber(int number, char* addressString, ref int offset)
	{
		offset += ((number > 99) ? 3 : ((number <= 9) ? 1 : 2));
		int num = offset;
		do
		{
			number = Math.DivRem(number, 10, out var result);
			addressString[--num] = (char)(48 + result);
		}
		while (number != 0);
	}

	public unsafe static bool Ipv4StringToAddress(ReadOnlySpan<char> ipSpan, out long address)
	{
		int end = ipSpan.Length;
		long num;
		fixed (char* name = &MemoryMarshal.GetReference(ipSpan))
		{
			num = System.IPv4AddressHelper.ParseNonCanonical(name, 0, ref end, notImplicitFile: true);
		}
		if (num != -1 && end == ipSpan.Length)
		{
			address = (uint)IPAddress.HostToNetworkOrder((int)num);
			return true;
		}
		address = 0L;
		return false;
	}

	public unsafe static bool Ipv6StringToAddress(ReadOnlySpan<char> ipSpan, Span<ushort> numbers, int numbersLength, out uint scope)
	{
		int end = ipSpan.Length;
		bool flag = false;
		fixed (char* name = &MemoryMarshal.GetReference(ipSpan))
		{
			flag = System.IPv6AddressHelper.IsValidStrict(name, 0, ref end);
		}
		if (flag || end != ipSpan.Length)
		{
			string scopeId = null;
			System.IPv6AddressHelper.Parse(ipSpan, numbers, 0, ref scopeId);
			if (scopeId != null && scopeId.Length > 1)
			{
				if (uint.TryParse(scopeId.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out scope))
				{
					return true;
				}
				uint num = InterfaceInfoPal.InterfaceNameToIndex(scopeId);
				if (num != 0)
				{
					scope = num;
					return true;
				}
			}
			scope = 0u;
			return true;
		}
		scope = 0u;
		return false;
	}

	private static void AppendSections(ushort[] address, int fromInclusive, int toExclusive, StringBuilder buffer)
	{
		ReadOnlySpan<ushort> numbers = new ReadOnlySpan<ushort>(address, fromInclusive, toExclusive - fromInclusive);
		(int longestSequenceStart, int longestSequenceLength) tuple = System.IPv6AddressHelper.FindCompressionRange(numbers);
		int item = tuple.longestSequenceStart;
		int item2 = tuple.longestSequenceLength;
		bool flag = false;
		for (int i = fromInclusive; i < item; i++)
		{
			if (flag)
			{
				buffer.Append(':');
			}
			flag = true;
			AppendHex(address[i], buffer);
		}
		if (item >= 0)
		{
			buffer.Append("::");
			flag = false;
			fromInclusive = item2;
		}
		for (int j = fromInclusive; j < toExclusive; j++)
		{
			if (flag)
			{
				buffer.Append(':');
			}
			flag = true;
			AppendHex(address[j], buffer);
		}
	}

	private static void AppendHex(ushort value, StringBuilder buffer)
	{
		if ((value & 0xF000u) != 0)
		{
			buffer.Append(System.HexConverter.ToCharLower(value >> 12));
		}
		if ((value & 0xFF00u) != 0)
		{
			buffer.Append(System.HexConverter.ToCharLower(value >> 8));
		}
		if ((value & 0xFFF0u) != 0)
		{
			buffer.Append(System.HexConverter.ToCharLower(value >> 4));
		}
		buffer.Append(System.HexConverter.ToCharLower(value));
	}

	private static uint ExtractIPv4Address(ushort[] address)
	{
		uint host = (uint)((address[6] << 16) | address[7]);
		return (uint)IPAddress.HostToNetworkOrder((int)host);
	}
}
