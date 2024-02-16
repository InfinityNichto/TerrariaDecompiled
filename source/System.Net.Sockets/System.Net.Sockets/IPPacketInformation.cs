using System.Diagnostics.CodeAnalysis;

namespace System.Net.Sockets;

public struct IPPacketInformation
{
	private readonly IPAddress _address;

	private readonly int _networkInterface;

	public IPAddress Address => _address;

	public int Interface => _networkInterface;

	internal IPPacketInformation(IPAddress address, int networkInterface)
	{
		_address = address;
		_networkInterface = networkInterface;
	}

	public static bool operator ==(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
	{
		if (packetInformation1._networkInterface == packetInformation2._networkInterface)
		{
			if (packetInformation1._address != null || packetInformation2._address != null)
			{
				if (packetInformation1._address != null)
				{
					return packetInformation1._address.Equals(packetInformation2._address);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static bool operator !=(IPPacketInformation packetInformation1, IPPacketInformation packetInformation2)
	{
		return !(packetInformation1 == packetInformation2);
	}

	public override bool Equals([NotNullWhen(true)] object? comparand)
	{
		if (comparand is IPPacketInformation iPPacketInformation)
		{
			return this == iPPacketInformation;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _networkInterface.GetHashCode() * -1521134295 + ((_address != null) ? _address.GetHashCode() : 0);
	}
}
