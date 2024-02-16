namespace System.Net.Sockets;

public struct SocketInformation
{
	public byte[] ProtocolInformation { get; set; }

	public SocketInformationOptions Options { get; set; }

	internal void SetOption(SocketInformationOptions option, bool value)
	{
		if (value)
		{
			Options |= option;
		}
		else
		{
			Options &= ~option;
		}
	}

	internal bool GetOption(SocketInformationOptions option)
	{
		return (Options & option) == option;
	}
}
