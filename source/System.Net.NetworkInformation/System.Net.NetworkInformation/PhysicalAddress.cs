using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.NetworkInformation;

public class PhysicalAddress
{
	private readonly byte[] _address;

	private int _hash;

	public static readonly PhysicalAddress None = new PhysicalAddress(Array.Empty<byte>());

	public PhysicalAddress(byte[] address)
	{
		_address = address;
	}

	public override int GetHashCode()
	{
		if (_hash == 0)
		{
			int num = 0;
			int num2 = _address.Length & -4;
			int i;
			for (i = 0; i < num2; i += 4)
			{
				num ^= BinaryPrimitives.ReadInt32LittleEndian(_address.AsSpan(i));
			}
			if (((uint)_address.Length & 3u) != 0)
			{
				int num3 = 0;
				int num4 = 0;
				for (; i < _address.Length; i++)
				{
					num3 |= _address[i] << num4;
					num4 += 8;
				}
				num ^= num3;
			}
			if (num == 0)
			{
				num = 1;
			}
			_hash = num;
		}
		return _hash;
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (!(comparand is PhysicalAddress physicalAddress))
		{
			return false;
		}
		if (_address.Length != physicalAddress._address.Length)
		{
			return false;
		}
		if (GetHashCode() != physicalAddress.GetHashCode())
		{
			return false;
		}
		for (int i = 0; i < physicalAddress._address.Length; i++)
		{
			if (_address[i] != physicalAddress._address[i])
			{
				return false;
			}
		}
		return true;
	}

	public override string ToString()
	{
		return Convert.ToHexString(_address.AsSpan());
	}

	public byte[] GetAddressBytes()
	{
		return (byte[])_address.Clone();
	}

	public static PhysicalAddress Parse(string? address)
	{
		if (address == null)
		{
			return None;
		}
		return Parse(address.AsSpan());
	}

	public static PhysicalAddress Parse(ReadOnlySpan<char> address)
	{
		if (!TryParse(address, out PhysicalAddress value))
		{
			throw new FormatException(System.SR.Format(System.SR.net_bad_mac_address, new string(address)));
		}
		return value;
	}

	public static bool TryParse(string? address, [NotNullWhen(true)] out PhysicalAddress? value)
	{
		if (address == null)
		{
			value = None;
			return true;
		}
		return TryParse(address.AsSpan(), out value);
	}

	public static bool TryParse(ReadOnlySpan<char> address, [NotNullWhen(true)] out PhysicalAddress? value)
	{
		char? c = null;
		value = null;
		byte[] array;
		int value2;
		if (address.Contains('-'))
		{
			if ((address.Length + 1) % 3 != 0)
			{
				return false;
			}
			c = '-';
			array = new byte[(address.Length + 1) / 3];
			value2 = 2;
		}
		else if (address.Contains(':'))
		{
			c = ':';
			if (!TryGetValidSegmentLength(address, ':', out value2))
			{
				return false;
			}
			if (value2 != 2 && value2 != 4)
			{
				return false;
			}
			array = new byte[6];
		}
		else if (address.Contains('.'))
		{
			c = '.';
			if (!TryGetValidSegmentLength(address, '.', out value2))
			{
				return false;
			}
			if (value2 != 4)
			{
				return false;
			}
			array = new byte[6];
		}
		else
		{
			if (address.Length % 2 > 0)
			{
				return false;
			}
			value2 = address.Length;
			array = new byte[address.Length / 2];
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < address.Length; i++)
		{
			int num3 = address[i];
			int num4;
			if ((num4 = System.HexConverter.FromChar(num3)) == 255)
			{
				if (c == num3 && num == value2)
				{
					num = 0;
					continue;
				}
				return false;
			}
			num3 = num4;
			if (num >= value2)
			{
				return false;
			}
			if (num % 2 == 0)
			{
				array[num2] = (byte)(num3 << 4);
			}
			else
			{
				array[num2++] |= (byte)num3;
			}
			num++;
		}
		if (num < value2)
		{
			return false;
		}
		value = new PhysicalAddress(array);
		return true;
	}

	private static bool TryGetValidSegmentLength(ReadOnlySpan<char> address, char delimiter, out int value)
	{
		value = -1;
		int num = 1;
		int num2 = 0;
		for (int i = 0; i < address.Length; i++)
		{
			if (address[i] == delimiter)
			{
				if (num2 == 0)
				{
					num2 = i;
				}
				else if ((i - (num - 1)) % num2 != 0)
				{
					return false;
				}
				num++;
			}
		}
		if (num * num2 != 12)
		{
			return false;
		}
		value = num2;
		return true;
	}
}
