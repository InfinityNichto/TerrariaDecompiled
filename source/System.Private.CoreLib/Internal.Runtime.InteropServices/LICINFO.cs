using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices;

internal struct LICINFO
{
	public int cbLicInfo;

	[MarshalAs(UnmanagedType.Bool)]
	public bool fRuntimeKeyAvail;

	[MarshalAs(UnmanagedType.Bool)]
	public bool fLicVerified;
}
