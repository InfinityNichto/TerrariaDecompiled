namespace System.Net;

public class IPHostEntry
{
	public string HostName { get; set; }

	public string[] Aliases { get; set; }

	public IPAddress[] AddressList { get; set; }
}
