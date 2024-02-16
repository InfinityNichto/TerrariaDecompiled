using System.Text;

namespace System.Net.WebSockets;

internal static class WebSocketValidate
{
	internal static void ValidateSubprotocol(string subProtocol)
	{
		if (string.IsNullOrWhiteSpace(subProtocol))
		{
			throw new ArgumentException(System.SR.net_WebSockets_InvalidEmptySubProtocol, "subProtocol");
		}
		string text = null;
		for (int i = 0; i < subProtocol.Length; i++)
		{
			char c = subProtocol[i];
			if (c < '!' || c > '~')
			{
				text = $"[{c}]";
				break;
			}
			if (!char.IsLetterOrDigit(c) && "()<>@,;:\\\"/[]?={} ".IndexOf(c) >= 0)
			{
				text = c.ToString();
				break;
			}
		}
		if (text != null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_InvalidCharInProtocolString, subProtocol, text), "subProtocol");
		}
	}

	internal static void ValidateCloseStatus(WebSocketCloseStatus closeStatus, string statusDescription)
	{
		if (closeStatus == WebSocketCloseStatus.Empty && !string.IsNullOrEmpty(statusDescription))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_ReasonNotNull, statusDescription, WebSocketCloseStatus.Empty), "statusDescription");
		}
		if ((closeStatus >= (WebSocketCloseStatus)0 && closeStatus <= (WebSocketCloseStatus)999) || closeStatus == (WebSocketCloseStatus)1006 || closeStatus == (WebSocketCloseStatus)1015)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_InvalidCloseStatusCode, (int)closeStatus), "closeStatus");
		}
		int num = 0;
		if (!string.IsNullOrEmpty(statusDescription))
		{
			num = Encoding.UTF8.GetByteCount(statusDescription);
		}
		if (num > 123)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_InvalidCloseStatusDescription, statusDescription, 123), "statusDescription");
		}
	}

	internal static void ValidateArraySegment(ArraySegment<byte> arraySegment, string parameterName)
	{
		if (arraySegment.Array == null)
		{
			throw new ArgumentNullException(parameterName + ".Array");
		}
		if (arraySegment.Offset < 0 || arraySegment.Offset > arraySegment.Array.Length)
		{
			throw new ArgumentOutOfRangeException(parameterName + ".Offset");
		}
		if (arraySegment.Count < 0 || arraySegment.Count > arraySegment.Array.Length - arraySegment.Offset)
		{
			throw new ArgumentOutOfRangeException(parameterName + ".Count");
		}
	}

	internal static void ValidateBuffer(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0 || offset > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > buffer.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
	}
}
