using System.Collections;
using System.Collections.Generic;

namespace System.Net.NetworkInformation;

public class IPAddressInformationCollection : ICollection<IPAddressInformation>, IEnumerable<IPAddressInformation>, IEnumerable
{
	private readonly List<IPAddressInformation> _addresses = new List<IPAddressInformation>();

	public virtual int Count => _addresses.Count;

	public virtual bool IsReadOnly => true;

	public virtual IPAddressInformation this[int index] => _addresses[index];

	internal IPAddressInformationCollection()
	{
	}

	public virtual void CopyTo(IPAddressInformation[] array, int offset)
	{
		_addresses.CopyTo(array, offset);
	}

	public virtual void Add(IPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	internal void InternalAdd(IPAddressInformation address)
	{
		_addresses.Add(address);
	}

	public virtual bool Contains(IPAddressInformation address)
	{
		return _addresses.Contains(address);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<IPAddressInformation> GetEnumerator()
	{
		return _addresses.GetEnumerator();
	}

	public virtual bool Remove(IPAddressInformation address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual void Clear()
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}
}
