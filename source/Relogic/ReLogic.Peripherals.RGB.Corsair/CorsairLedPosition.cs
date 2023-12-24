using System.Diagnostics;

namespace ReLogic.Peripherals.RGB.Corsair;

[DebuggerDisplay("LedID = {LedId}")]
internal struct CorsairLedPosition
{
	public CorsairLedId LedId;

	public double Top;

	public double Left;

	public double Height;

	public double Width;
}
