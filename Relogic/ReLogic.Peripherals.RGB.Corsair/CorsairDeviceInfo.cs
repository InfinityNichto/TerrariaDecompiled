namespace ReLogic.Peripherals.RGB.Corsair;

internal struct CorsairDeviceInfo
{
	public CorsairDeviceType Type;

	public string Model;

	public CorsairPhysicalLayout PhysicalLayout;

	public CorsairLogicalLayout LogicalLayout;

	public CorsairDeviceCaps CapsMask;

	public int LedsCount;
}
