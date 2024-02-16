namespace System.Net.NetworkInformation;

internal sealed class SystemIPv6InterfaceProperties : IPv6InterfaceProperties
{
	private readonly uint _index;

	private readonly uint _mtu;

	private readonly uint[] _zoneIndices;

	public override int Index => (int)_index;

	public override int Mtu => (int)_mtu;

	internal SystemIPv6InterfaceProperties(uint index, uint mtu, uint[] zoneIndices)
	{
		_index = index;
		_mtu = mtu;
		_zoneIndices = zoneIndices;
	}

	public override long GetScopeId(ScopeLevel scopeLevel)
	{
		if (scopeLevel < ScopeLevel.None || (int)scopeLevel >= _zoneIndices.Length)
		{
			throw new ArgumentOutOfRangeException("scopeLevel");
		}
		return _zoneIndices[(int)scopeLevel];
	}
}
