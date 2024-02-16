using System.ComponentModel;

namespace System.Net;

public class UploadValuesCompletedEventArgs : AsyncCompletedEventArgs
{
	private readonly byte[] _result;

	public byte[] Result
	{
		get
		{
			RaiseExceptionIfNecessary();
			return _result;
		}
	}

	internal UploadValuesCompletedEventArgs(byte[] result, Exception exception, bool cancelled, object userToken)
		: base(exception, cancelled, userToken)
	{
		_result = result;
	}
}
