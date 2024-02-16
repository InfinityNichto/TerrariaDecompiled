using System.Collections;
using System.Collections.Generic;

namespace System.Net.NetworkInformation;

public class UnicastIPAddressInformationCollection : ICollection<UnicastIPAddressInformation>, IEnumerable<UnicastIPAddressInformation>, IEnumerable
{
	private readonly List<UnicastIPAddressInformation> _addresses = new List<UnicastIPAddressInformation>();

	public virtual int Count => _addresses.Count;

	public virtual bool IsReadOnly => true;

	public virtual UnicastIPAddressInformation this[int index] => _addresses[index];

	protected internal UnicastIPAddressInformationCollection()
	{
	}

	public virtual void CopyTo(UnicastIPAddressInformation[] array, int offset)
	{
		_addresses.CopyTo(array, offset);
	}

	public virtual void Add(UnicastIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	internal void InternalAdd(UnicastIPAddressInformation address)
	{
		_addresses.Add(address);
	}

	public virtual bool Contains(UnicastIPAddressInformation address)
	{
		return _addresses.Contains(address);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<UnicastIPAddressInformation> GetEnumerator()
	{
		return _addresses.GetEnumerator();
	}

	public virtual bool Remove(UnicastIPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual void Clear()
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}
}
