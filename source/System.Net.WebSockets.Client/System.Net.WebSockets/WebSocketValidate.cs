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
}
