using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace ReLogic.Peripherals.RGB;

public class VendorColorProfile
{
	private readonly DeviceColorProfile[] _profiles = new DeviceColorProfile[Enum.GetValues(typeof(RgbDeviceType)).Cast<int>().Max() + 1];

	[JsonProperty]
	public DeviceColorProfile Keyboard
	{
		get
		{
			return _profiles[0];
		}
		private set
		{
			_profiles[0] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Mouse
	{
		get
		{
			return _profiles[1];
		}
		private set
		{
			_profiles[1] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Keypad
	{
		get
		{
			return _profiles[2];
		}
		private set
		{
			_profiles[2] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Mousepad
	{
		get
		{
			return _profiles[3];
		}
		private set
		{
			_profiles[3] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Headset
	{
		get
		{
			return _profiles[4];
		}
		private set
		{
			_profiles[4] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Generic
	{
		get
		{
			return _profiles[5];
		}
		private set
		{
			_profiles[5] = value;
		}
	}

	[JsonProperty]
	public DeviceColorProfile Virtual
	{
		get
		{
			return _profiles[6];
		}
		private set
		{
			_profiles[6] = value;
		}
	}

	public DeviceColorProfile this[RgbDeviceType type] => _profiles[(int)type];

	public VendorColorProfile()
	{
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i] = new DeviceColorProfile();
		}
	}

	public VendorColorProfile(Vector3 multiplier)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < _profiles.Length; i++)
		{
			_profiles[i] = new DeviceColorProfile(multiplier);
		}
	}

	public void SetColorMultiplier(RgbDeviceType type, DeviceColorProfile profile)
	{
		_profiles[(int)type] = profile;
	}
}
