namespace System.Net.WebSockets;

public sealed class WebSocketDeflateOptions
{
	private int _clientMaxWindowBits = 15;

	private int _serverMaxWindowBits = 15;

	public int ClientMaxWindowBits
	{
		get
		{
			return _clientMaxWindowBits;
		}
		set
		{
			if (value < 9 || value > 15)
			{
				throw new ArgumentOutOfRangeException("ClientMaxWindowBits", value, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange, 9, 15));
			}
			_clientMaxWindowBits = value;
		}
	}

	public bool ClientContextTakeover { get; set; } = true;


	public int ServerMaxWindowBits
	{
		get
		{
			return _serverMaxWindowBits;
		}
		set
		{
			if (value < 9 || value > 15)
			{
				throw new ArgumentOutOfRangeException("ServerMaxWindowBits", value, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange, 9, 15));
			}
			_serverMaxWindowBits = value;
		}
	}

	public bool ServerContextTakeover { get; set; } = true;

}
