using System.ComponentModel;
using System.IO;

namespace System.Net;

public class OpenWriteCompletedEventArgs : AsyncCompletedEventArgs
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

	internal OpenWriteCompletedEventArgs(Stream result, Exception exception, bool cancelled, object userToken)
		: base(exception, cancelled, userToken)
	{
		_result = result;
	}
}
