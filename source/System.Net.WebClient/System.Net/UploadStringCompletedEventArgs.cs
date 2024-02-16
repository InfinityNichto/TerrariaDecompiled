using System.ComponentModel;

namespace System.Net;

public class UploadStringCompletedEventArgs : AsyncCompletedEventArgs
{
	private readonly string _result;

	public string Result
	{
		get
		{
			RaiseExceptionIfNecessary();
			return _result;
		}
	}

	internal UploadStringCompletedEventArgs(string result, Exception exception, bool cancelled, object userToken)
		: base(exception, cancelled, userToken)
	{
		_result = result;
	}
}
