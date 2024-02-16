namespace System.Net;

internal struct SecurityPackageInfo
{
	internal int Capabilities;

	internal short Version;

	internal short RPCID;

	internal int MaxToken;

	internal IntPtr Name;

	internal IntPtr Comment;
}
