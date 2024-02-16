namespace System.Net.NetworkInformation;

public class NetworkAvailabilityEventArgs : EventArgs
{
	private readonly bool _isAvailable;

	public bool IsAvailable => _isAvailable;

	internal NetworkAvailabilityEventArgs(bool isAvailable)
	{
		_isAvailable = isAvailable;
	}
}
