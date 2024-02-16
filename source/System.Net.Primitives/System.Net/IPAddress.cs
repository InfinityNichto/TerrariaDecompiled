using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net;

public class IPAddress
{
	private sealed class ReadOnlyIPAddress : IPAddress
	{
		public ReadOnlyIPAddress(ReadOnlySpan<byte> newAddress)
			: base(newAddress)
		{
		}
	}

	public static readonly IPAddress Any = new ReadOnlyIPAddress(new byte[4] { 0, 0, 0, 0 });

	public static readonly IPAddress Loopback = new ReadOnlyIPAddress(new byte[4] { 127, 0, 0, 1 });

	public static readonly IPAddress Broadcast = new ReadOnlyIPAddress(new byte[4] { 255, 255, 255, 255 });

	public static readonly IPAddress None = Broadcast;

	public static readonly IPAddress IPv6Any = new IPAddress((ReadOnlySpan<byte>)new byte[16]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0
	}, 0L);

	public static readonly IPAddress IPv6Loopback = new IPAddress((ReadOnlySpan<byte>)new byte[16]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 1
	}, 0L);

	public static readonly IPAddress IPv6None = IPv6Any;

	private static readonly IPAddress s_loopbackMappedToIPv6 = new IPAddress((ReadOnlySpan<byte>)new byte[16]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		255, 255, 127, 0, 0, 1
	}, 0L);

	private uint _addressOrScopeId;

	private readonly ushort[] _numbers;

	private string _toString;

	private int _hashCode;

	private bool IsIPv4 => _numbers == null;

	private bool IsIPv6 => _numbers != null;

	private uint PrivateAddress
	{
		get
		{
			return _addressOrScopeId;
		}
		set
		{
			_toString = null;
			_hashCode = 0;
			_addressOrScopeId = value;
		}
	}

	private uint PrivateScopeId
	{
		get
		{
			return _addressOrScopeId;
		}
		set
		{
			_toString = null;
			_hashCode = 0;
			_addressOrScopeId = value;
		}
	}

	public AddressFamily AddressFamily
	{
		get
		{
			if (!IsIPv4)
			{
				return AddressFamily.InterNetworkV6;
			}
			return AddressFamily.InterNetwork;
		}
	}

	public long ScopeId
	{
		get
		{
			if (IsIPv4)
			{
				throw new SocketException(SocketError.OperationNotSupported);
			}
			return PrivateScopeId;
		}
		set
		{
			if (IsIPv4)
			{
				throw new SocketException(SocketError.OperationNotSupported);
			}
			if (value < 0 || value > uint.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			PrivateScopeId = (uint)value;
		}
	}

	public bool IsIPv6Multicast
	{
		get
		{
			if (IsIPv6)
			{
				return (_numbers[0] & 0xFF00) == 65280;
			}
			return false;
		}
	}

	public bool IsIPv6LinkLocal
	{
		get
		{
			if (IsIPv6)
			{
				return (_numbers[0] & 0xFFC0) == 65152;
			}
			return false;
		}
	}

	public bool IsIPv6SiteLocal
	{
		get
		{
			if (IsIPv6)
			{
				return (_numbers[0] & 0xFFC0) == 65216;
			}
			return false;
		}
	}

	public bool IsIPv6Teredo
	{
		get
		{
			if (IsIPv6 && _numbers[0] == 8193)
			{
				return _numbers[1] == 0;
			}
			return false;
		}
	}

	public bool IsIPv6UniqueLocal
	{
		get
		{
			if (IsIPv6)
			{
				return (_numbers[0] & 0xFE00) == 64512;
			}
			return false;
		}
	}

	public bool IsIPv4MappedToIPv6
	{
		get
		{
			if (IsIPv4)
			{
				return false;
			}
			for (int i = 0; i < 5; i++)
			{
				if (_numbers[i] != 0)
				{
					return false;
				}
			}
			return _numbers[5] == ushort.MaxValue;
		}
	}

	[Obsolete("IPAddress.Address is address family dependent and has been deprecated. Use IPAddress.Equals to perform comparisons instead.")]
	public long Address
	{
		get
		{
			if (AddressFamily == AddressFamily.InterNetworkV6)
			{
				throw new SocketException(SocketError.OperationNotSupported);
			}
			return PrivateAddress;
		}
		set
		{
			if (AddressFamily == AddressFamily.InterNetworkV6)
			{
				throw new SocketException(SocketError.OperationNotSupported);
			}
			if (PrivateAddress != value)
			{
				if (this is ReadOnlyIPAddress)
				{
					throw new SocketException(SocketError.OperationNotSupported);
				}
				PrivateAddress = (uint)value;
			}
		}
	}

	public IPAddress(long newAddress)
	{
		if (newAddress < 0 || newAddress > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("newAddress");
		}
		PrivateAddress = (uint)newAddress;
	}

	public IPAddress(byte[] address, long scopeid)
		: this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()), scopeid)
	{
	}

	public IPAddress(ReadOnlySpan<byte> address, long scopeid)
	{
		if (address.Length != 16)
		{
			throw new ArgumentException(System.SR.dns_bad_ip_address, "address");
		}
		if (scopeid < 0 || scopeid > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("scopeid");
		}
		_numbers = new ushort[8];
		for (int i = 0; i < 8; i++)
		{
			_numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
		}
		PrivateScopeId = (uint)scopeid;
	}

	internal IPAddress(ReadOnlySpan<ushort> numbers, uint scopeid)
	{
		ushort[] array = new ushort[8];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = numbers[i];
		}
		_numbers = array;
		PrivateScopeId = scopeid;
	}

	private IPAddress(ushort[] numbers, uint scopeid)
	{
		_numbers = numbers;
		PrivateScopeId = scopeid;
	}

	public IPAddress(byte[] address)
		: this(new ReadOnlySpan<byte>(address ?? ThrowAddressNullException()))
	{
	}

	public IPAddress(ReadOnlySpan<byte> address)
	{
		if (address.Length == 4)
		{
			PrivateAddress = MemoryMarshal.Read<uint>(address);
			return;
		}
		if (address.Length == 16)
		{
			_numbers = new ushort[8];
			for (int i = 0; i < 8; i++)
			{
				_numbers[i] = (ushort)(address[i * 2] * 256 + address[i * 2 + 1]);
			}
			return;
		}
		throw new ArgumentException(System.SR.dns_bad_ip_address, "address");
	}

	public static bool TryParse([NotNullWhen(true)] string? ipString, [NotNullWhen(true)] out IPAddress? address)
	{
		if (ipString == null)
		{
			address = null;
			return false;
		}
		address = IPAddressParser.Parse(ipString.AsSpan(), tryParse: true);
		return address != null;
	}

	public static bool TryParse(ReadOnlySpan<char> ipSpan, [NotNullWhen(true)] out IPAddress? address)
	{
		address = IPAddressParser.Parse(ipSpan, tryParse: true);
		return address != null;
	}

	public static IPAddress Parse(string ipString)
	{
		if (ipString == null)
		{
			throw new ArgumentNullException("ipString");
		}
		return IPAddressParser.Parse(ipString.AsSpan(), tryParse: false);
	}

	public static IPAddress Parse(ReadOnlySpan<char> ipSpan)
	{
		return IPAddressParser.Parse(ipSpan, tryParse: false);
	}

	public bool TryWriteBytes(Span<byte> destination, out int bytesWritten)
	{
		if (IsIPv6)
		{
			if (destination.Length < 16)
			{
				bytesWritten = 0;
				return false;
			}
			WriteIPv6Bytes(destination);
			bytesWritten = 16;
		}
		else
		{
			if (destination.Length < 4)
			{
				bytesWritten = 0;
				return false;
			}
			WriteIPv4Bytes(destination);
			bytesWritten = 4;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteIPv6Bytes(Span<byte> destination)
	{
		int num = 0;
		for (int i = 0; i < 8; i++)
		{
			destination[num++] = (byte)((uint)(_numbers[i] >> 8) & 0xFFu);
			destination[num++] = (byte)(_numbers[i] & 0xFFu);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteIPv4Bytes(Span<byte> destination)
	{
		uint value = PrivateAddress;
		MemoryMarshal.Write(destination, ref value);
	}

	public byte[] GetAddressBytes()
	{
		if (IsIPv6)
		{
			byte[] array = new byte[16];
			WriteIPv6Bytes(array);
			return array;
		}
		byte[] array2 = new byte[4];
		WriteIPv4Bytes(array2);
		return array2;
	}

	public override string ToString()
	{
		if (_toString == null)
		{
			_toString = (IsIPv4 ? IPAddressParser.IPv4AddressToString(PrivateAddress) : IPAddressParser.IPv6AddressToString(_numbers, PrivateScopeId));
		}
		return _toString;
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		if (!IsIPv4)
		{
			return IPAddressParser.IPv6AddressToString(_numbers, PrivateScopeId, destination, out charsWritten);
		}
		return IPAddressParser.IPv4AddressToString(PrivateAddress, destination, out charsWritten);
	}

	public static long HostToNetworkOrder(long host)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return host;
		}
		return BinaryPrimitives.ReverseEndianness(host);
	}

	public static int HostToNetworkOrder(int host)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return host;
		}
		return BinaryPrimitives.ReverseEndianness(host);
	}

	public static short HostToNetworkOrder(short host)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return host;
		}
		return BinaryPrimitives.ReverseEndianness(host);
	}

	public static long NetworkToHostOrder(long network)
	{
		return HostToNetworkOrder(network);
	}

	public static int NetworkToHostOrder(int network)
	{
		return HostToNetworkOrder(network);
	}

	public static short NetworkToHostOrder(short network)
	{
		return HostToNetworkOrder(network);
	}

	public static bool IsLoopback(IPAddress address)
	{
		if (address == null)
		{
			ThrowAddressNullException();
		}
		if (address.IsIPv6)
		{
			if (!address.Equals(IPv6Loopback))
			{
				return address.Equals(s_loopbackMappedToIPv6);
			}
			return true;
		}
		long num = (uint)HostToNetworkOrder(-16777216);
		return (address.PrivateAddress & num) == (Loopback.PrivateAddress & num);
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is IPAddress comparand2)
		{
			return Equals(comparand2);
		}
		return false;
	}

	internal bool Equals(IPAddress comparand)
	{
		if (AddressFamily != comparand.AddressFamily)
		{
			return false;
		}
		if (IsIPv6)
		{
			ReadOnlySpan<byte> source = MemoryMarshal.AsBytes<ushort>(_numbers);
			ReadOnlySpan<byte> source2 = MemoryMarshal.AsBytes<ushort>(comparand._numbers);
			if (MemoryMarshal.Read<ulong>(source) == MemoryMarshal.Read<ulong>(source2) && MemoryMarshal.Read<ulong>(source.Slice(8)) == MemoryMarshal.Read<ulong>(source2.Slice(8)))
			{
				return PrivateScopeId == comparand.PrivateScopeId;
			}
			return false;
		}
		return comparand.PrivateAddress == PrivateAddress;
	}

	public override int GetHashCode()
	{
		if (_hashCode == 0)
		{
			if (IsIPv6)
			{
				ReadOnlySpan<byte> source = MemoryMarshal.AsBytes<ushort>(_numbers);
				_hashCode = HashCode.Combine(MemoryMarshal.Read<uint>(source), MemoryMarshal.Read<uint>(source.Slice(4)), MemoryMarshal.Read<uint>(source.Slice(8)), MemoryMarshal.Read<uint>(source.Slice(12)), _addressOrScopeId);
			}
			else
			{
				_hashCode = HashCode.Combine(_addressOrScopeId);
			}
		}
		return _hashCode;
	}

	public IPAddress MapToIPv6()
	{
		if (IsIPv6)
		{
			return this;
		}
		uint num = (uint)NetworkToHostOrder((int)PrivateAddress);
		return new IPAddress(new ushort[8]
		{
			0,
			0,
			0,
			0,
			0,
			65535,
			(ushort)(num >> 16),
			(ushort)num
		}, 0u);
	}

	public IPAddress MapToIPv4()
	{
		if (IsIPv4)
		{
			return this;
		}
		uint host = (uint)((_numbers[6] << 16) | _numbers[7]);
		return new IPAddress((uint)HostToNetworkOrder((int)host));
	}

	[DoesNotReturn]
	private static byte[] ThrowAddressNullException()
	{
		throw new ArgumentNullException("address");
	}
}
