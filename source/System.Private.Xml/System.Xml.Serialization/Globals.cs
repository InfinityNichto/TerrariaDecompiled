namespace System.Xml.Serialization;

internal static class Globals
{
	internal static Exception NotSupported(string msg)
	{
		return new NotSupportedException(msg);
	}
}
