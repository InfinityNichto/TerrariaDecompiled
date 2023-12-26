using System;

namespace ReLogic.Peripherals.RGB.Corsair;

internal struct CorsairProtocolDetails
{
	public IntPtr SdkVersion;

	public IntPtr ServerVersion;

	public int SdkProtocolVersion;

	public int ServerProtocolVersion;

	public byte BreakingChanges;
}
