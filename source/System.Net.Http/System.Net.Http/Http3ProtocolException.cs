using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Net.Http;

[Serializable]
[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal class Http3ProtocolException : Exception
{
	public Http3ErrorCode ErrorCode { get; }

	protected Http3ProtocolException(string message, Http3ErrorCode errorCode)
		: base(message)
	{
		ErrorCode = errorCode;
	}

	protected Http3ProtocolException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		ErrorCode = (Http3ErrorCode)info.GetUInt32("ErrorCode");
	}

	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.AddValue("ErrorCode", (uint)ErrorCode);
		base.GetObjectData(info, context);
	}

	protected static string GetName(Http3ErrorCode errorCode)
	{
		Http3ErrorCode num = errorCode - 256;
		if ((ulong)num <= 16uL)
		{
			switch (num)
			{
			case (Http3ErrorCode)0L:
				return "H3_NO_ERROR (0x100)";
			case (Http3ErrorCode)1L:
				return "H3_GENERAL_PROTOCOL_ERROR (0x101)";
			case (Http3ErrorCode)2L:
				return "H3_INTERNAL_ERROR (0x102)";
			case (Http3ErrorCode)3L:
				return "H3_STREAM_CREATION_ERROR (0x103)";
			case (Http3ErrorCode)4L:
				return "H3_CLOSED_CRITICAL_STREAM (0x104)";
			case (Http3ErrorCode)5L:
				return "H3_FRAME_UNEXPECTED (0x105)";
			case (Http3ErrorCode)6L:
				return "H3_FRAME_ERROR (0x106)";
			case (Http3ErrorCode)7L:
				return "H3_EXCESSIVE_LOAD (0x107)";
			case (Http3ErrorCode)8L:
				return "H3_ID_ERROR (0x108)";
			case (Http3ErrorCode)9L:
				return "H3_SETTINGS_ERROR (0x109)";
			case (Http3ErrorCode)10L:
				return "H3_MISSING_SETTINGS (0x10A)";
			case (Http3ErrorCode)11L:
				return "H3_REQUEST_REJECTED (0x10B)";
			case (Http3ErrorCode)12L:
				return "H3_REQUEST_CANCELLED (0x10C)";
			case (Http3ErrorCode)13L:
				return "H3_REQUEST_INCOMPLETE (0x10D)";
			case (Http3ErrorCode)15L:
				return "H3_CONNECT_ERROR (0x10F)";
			case (Http3ErrorCode)16L:
				return "H3_VERSION_FALLBACK (0x110)";
			}
		}
		return "(unknown error)";
	}
}
