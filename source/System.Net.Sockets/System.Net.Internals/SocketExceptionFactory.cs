using System.Net.Sockets;

namespace System.Net.Internals;

internal static class SocketExceptionFactory
{
	private sealed class ExtendedSocketException : SocketException
	{
		private readonly EndPoint _endPoint;

		public override string Message
		{
			get
			{
				if (_endPoint != null)
				{
					return base.Message + " " + _endPoint.ToString();
				}
				return base.Message;
			}
		}

		public ExtendedSocketException(int errorCode, EndPoint endPoint)
			: base(errorCode)
		{
			_endPoint = endPoint;
		}
	}

	public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
	{
		return new ExtendedSocketException(socketError, endPoint);
	}
}
