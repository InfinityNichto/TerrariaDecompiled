using System.Collections;
using System.Collections.Generic;

namespace System.Net.NetworkInformation;

public class MulticastIPAddressInformationCollection : ICollection<MulticastIPAddressInformation>, IEnumerable<MulticastIPAddressInformation>, IEnumerable
{
	private readonly List<MulticastIPAddressInformation> _addresses = new List<MulticastIPAddressInformation>();

	public virtual int Count => _addresses.Count;

	public virtual bool IsReadOnly => true;

	public virtual MulticastIPAddressInformation this[int index] => _addresses[index];

	protected internal MulticastIPAddressInformationCollection()
	{
	}

	public virtual void CopyTo(MulticastIPAddressInformation[] array, int offset)
	{
		_addresses.CopyTo(array, offset);
	}

	public virtual void Add(MulticastIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	internal void InternalAdd(MulticastIPAddressInformation address)
	{
		_addresses.Add(address);
	}

	public virtual bool Contains(MulticastIPAddressInformation address)
	{
		return _addresses.Contains(address);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<MulticastIPAddressInformation> GetEnumerator()
	{
		return _addresses.GetEnumerator();
	}

	public virtual bool Remove(MulticastIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual void Clear()
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}
}
