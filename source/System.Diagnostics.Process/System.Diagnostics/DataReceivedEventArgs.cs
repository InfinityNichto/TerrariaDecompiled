namespace System.Diagnostics;

public class DataReceivedEventArgs : EventArgs
{
	private readonly string _data;

	public string? Data => _data;

	internal DataReceivedEventArgs(string data)
	{
		_data = data;
	}
}
