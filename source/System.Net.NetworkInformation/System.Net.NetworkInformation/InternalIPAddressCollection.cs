using System.Collections.Generic;

namespace System.Net.NetworkInformation;

internal sealed class InternalIPAddressCollection : IPAddressCollection
{
	private readonly List<IPAddress> _addresses;

	public override int Count => _addresses.Count;

	public override bool IsReadOnly => true;

	public override IPAddress this[int index] => _addresses[index];

	internal InternalIPAddressCollection()
	{
		_addresses = new List<IPAddress>();
	}

	public override void CopyTo(IPAddress[] array, int offset)
	{
		_addresses.CopyTo(array, offset);
	}

	public override void Add(IPAddress address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	internal void InternalAdd(IPAddress address)
	{
		_addresses.Add(address);
	}

	public override bool Contains(IPAddress address)
	{
		return _addresses.Contains(address);
	}

	public override IEnumerator<IPAddress> GetEnumerator()
	{
		return _addresses.GetEnumerator();
	}
}
