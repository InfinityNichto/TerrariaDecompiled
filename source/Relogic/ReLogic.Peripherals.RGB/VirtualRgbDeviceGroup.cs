using System.Collections.Generic;

namespace ReLogic.Peripherals.RGB;

public class VirtualRgbDeviceGroup : RgbDeviceGroup
{
	private static readonly List<RgbDevice> EmptyList = new List<RgbDevice>();

	private readonly List<RgbDevice> _devices;

	private bool _isInitialized;

	public VirtualRgbDeviceGroup(params RgbDevice[] devices)
	{
		_devices = new List<RgbDevice>(devices);
	}

	protected override void Initialize()
	{
		_isInitialized = true;
	}

	protected override void Uninitialize()
	{
		_isInitialized = false;
	}

	public override IEnumerator<RgbDevice> GetEnumerator()
	{
		return _isInitialized ? _devices.GetEnumerator() : EmptyList.GetEnumerator();
	}
}
