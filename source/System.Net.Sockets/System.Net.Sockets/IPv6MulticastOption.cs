namespace System.Net.Sockets;

public class IPv6MulticastOption
{
	private IPAddress _group;

	private long _interface;

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

	public long InterfaceIndex
	{
		get
		{
			return _interface;
		}
		set
		{
			if (value < 0 || value > uint.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_interface = value;
		}
	}

	public IPv6MulticastOption(IPAddress group, long ifindex)
	{
		if (group == null)
		{
			throw new ArgumentNullException("group");
		}
		if (ifindex < 0 || ifindex > uint.MaxValue)
		{
			throw new ArgumentOutOfRangeException("ifindex");
		}
		_group = group;
		InterfaceIndex = ifindex;
	}

	public IPv6MulticastOption(IPAddress group)
	{
		if (group == null)
		{
			throw new ArgumentNullException("group");
		}
		_group = group;
		InterfaceIndex = 0L;
	}
}
