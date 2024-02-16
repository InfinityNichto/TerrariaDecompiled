namespace System.Net.Sockets;

public class MulticastOption
{
	private IPAddress _group;

	private IPAddress _localAddress;

	private int _ifIndex;

	public IPAddress Group
	{
		get
		{
			return _group;
		}
		set
		{
			_group = value ?? throw new ArgumentNullException("value");
		}
	}

	public IPAddress? LocalAddress
	{
		get
		{
			return _localAddress;
		}
		set
		{
			_ifIndex = 0;
			_localAddress = value;
		}
	}

	public int InterfaceIndex
	{
		get
		{
			return _ifIndex;
		}
		set
		{
			if (value < 0 || value > 16777215)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_localAddress = null;
			_ifIndex = value;
		}
	}

	public MulticastOption(IPAddress group, IPAddress mcint)
	{
		if (group == null)
		{
			throw new ArgumentNullException("group");
		}
		if (mcint == null)
		{
			throw new ArgumentNullException("mcint");
		}
		_group = group;
		LocalAddress = mcint;
	}

	public MulticastOption(IPAddress group, int interfaceIndex)
	{
		if (group == null)
		{
			throw new ArgumentNullException("group");
		}
		if (interfaceIndex < 0 || interfaceIndex > 16777215)
		{
			throw new ArgumentOutOfRangeException("interfaceIndex");
		}
		_group = group;
		_ifIndex = interfaceIndex;
	}

	public MulticastOption(IPAddress group)
	{
		if (group == null)
		{
			throw new ArgumentNullException("group");
		}
		_group = group;
		LocalAddress = IPAddress.Any;
	}
}
