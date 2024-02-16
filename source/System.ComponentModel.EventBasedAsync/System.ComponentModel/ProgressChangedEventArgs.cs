namespace System.ComponentModel;

public class ProgressChangedEventArgs : EventArgs
{
	private readonly int _progressPercentage;

	private readonly object _userState;

	public int ProgressPercentage => _progressPercentage;

	public object? UserState => _userState;

	public ProgressChangedEventArgs(int progressPercentage, object? userState)
	{
		_progressPercentage = progressPercentage;
		_userState = userState;
	}
}
