using System.Collections;
using System.Collections.Generic;

namespace System.Net.NetworkInformation;

public class IPAddressCollection : ICollection<IPAddress>, IEnumerable<IPAddress>, IEnumerable
{
	public virtual int Count
	{
		get
		{
			throw System.NotImplemented.ByDesign;
		}
	}

	public virtual bool IsReadOnly => true;

	public virtual IPAddress this[int index]
	{
		get
		{
			throw System.NotImplemented.ByDesign;
		}
	}

	protected internal IPAddressCollection()
	{
	}

	public virtual void CopyTo(IPAddress[] array, int offset)
	{
		throw System.NotImplemented.ByDesign;
	}

	public virtual void Add(IPAddress address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual bool Contains(IPAddress address)
	{
		throw System.NotImplemented.ByDesign;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public virtual IEnumerator<IPAddress> GetEnumerator()
	{
		throw System.NotImplemented.ByDesign;
	}

	public virtual bool Remove(IPAddress address)
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}

	public virtual void Clear()
	{
		throw new NotSupportedException(System.SR.net_collection_readonly);
	}
}
