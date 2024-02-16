using System.Collections;
using System.Collections.Generic;

namespace System.Net.NetworkInformation;

public class GatewayIPAddressInformationCollection : ICollection<GatewayIPAddressInformation>, IEnumerable<GatewayIPAddressInformation>, IEnumerable
{
	private readonly List<GatewayIPAddressInformation> _addresses;

	public virtual int Count => _addresses.Count;

	public virtual bool IsReadOnly => true;

	public virtual GatewayIPAddressInformation this[int index] => _addresses[index];

	protected internal GatewayIPAddressInformationCollection()
	{
		_addresses = new List<GatewayIPAddressInformation>();
	}

	public virtual void CopyTo(GatewayIPAddressInformation[] array, int offset)
	{
		_addresses.CopyTo(array, offset);
	}

	public virtual void Add(GatewayIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	internal void InternalAdd(GatewayIPAddressInformation address)
	{
		_addresses.Add(address);
	}

	public virtual bool Contains(GatewayIPAddressInformation address)
	{
		return _addresses.Contains(address);
	}

	public virtual IEnumerator<GatewayIPAddressInformation> GetEnumerator()
	{
		return _addresses.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual bool Remove(GatewayIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual void Clear()
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}
}
