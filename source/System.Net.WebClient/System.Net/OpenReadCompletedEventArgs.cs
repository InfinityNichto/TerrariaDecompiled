using System.ComponentModel;
using System.IO;

namespace System.Net;

public class OpenReadCompletedEventArgs : AsyncCompletedEventArgs
{
	private readonly Stream _result;

	public Stream Result
	{
		get
		{
			RaiseExceptionIfNecessary();
			return _result;
		}
	}

	internal OpenReadCompletedEventArgs(Stream result, Exception exception, bool cancelled, object userToken)
		: base(exception, cancelled, userToken)
	{
		_result = result;
	}
}
