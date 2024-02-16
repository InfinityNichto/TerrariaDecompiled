namespace System.Net;

internal sealed class FrameHeader
{
	private int _payloadSize = -1;

	public int MessageId { get; set; } = 22;


	public int MajorV { get; private set; } = 1;


	public int MinorV { get; private set; }

	public int PayloadSize
	{
		get
		{
			return _payloadSize;
		}
		set
		{
			if (value > 65535)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_frame_max_size, 65535, value), "PayloadSize");
			}
			_payloadSize = value;
		}
	}

	public void CopyTo(byte[] dest, int start)
	{
		dest[start++] = (byte)MessageId;
		dest[start++] = (byte)MajorV;
		dest[start++] = (byte)MinorV;
		dest[start++] = (byte)((uint)(_payloadSize >> 8) & 0xFFu);
		dest[start] = (byte)((uint)_payloadSize & 0xFFu);
	}

	public void CopyFrom(byte[] bytes, int start)
	{
		MessageId = bytes[start++];
		MajorV = bytes[start++];
		MinorV = bytes[start++];
		_payloadSize = (bytes[start++] << 8) | bytes[start];
	}
}
