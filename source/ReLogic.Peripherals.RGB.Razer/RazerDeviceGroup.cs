using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Razer;

public sealed class RazerDeviceGroup : RgbDeviceGroup
{
	private readonly List<RgbDevice> _devices = new List<RgbDevice>();

	private bool _isInitialized;

	private readonly VendorColorProfile _colorProfiles;

	private bool _initializationFailed;

	public RazerDeviceGroup(VendorColorProfile colorProfiles)
	{
		_colorProfiles = colorProfiles;
	}

	protected override void Initialize()
	{
		if (_isInitialized || _initializationFailed)
		{
			return;
		}
		if (!NativeLibrary.TryLoad("RzChromaSDK64.dll", Assembly.GetEntryAssembly(), DllImportSearchPath.SafeDirectories, out var _))
		{
			_initializationFailed = true;
			return;
		}
		try
		{
			RzResult rzResult = NativeMethods.Init();
			if (rzResult != 0)
			{
				int num = (int)rzResult;
				throw new DeviceInitializationException("Unable to initialize Razer Synapse: " + num);
			}
			_isInitialized = true;
			_devices.Add(new RazerMouse(_colorProfiles.Mouse));
			_devices.Add(new RazerKeyboard(_colorProfiles.Keyboard));
			_devices.Add(new RazerMousepad(_colorProfiles.Mousepad));
			_devices.Add(new RazerKeypad(_colorProfiles.Keypad));
			_devices.Add(new RazerHeadset(_colorProfiles.Headset));
			_devices.Add(new RazerLink(_colorProfiles.Generic));
			Console.WriteLine("Razer Chroma initialized.");
		}
		catch (DeviceInitializationException)
		{
			Console.WriteLine("Razer Chroma not supported. (Can be disabled via Config.json)");
			Uninitialize();
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Razer Chroma not supported: " + ex2);
			Uninitialize();
		}
	}

	protected override void Uninitialize()
	{
		if (!_isInitialized)
		{
			return;
		}
		try
		{
			_devices.Clear();
			RzResult rzResult = NativeMethods.UnInit();
			if (rzResult != 0)
			{
				int num = (int)rzResult;
				throw new DeviceInitializationException("Unable to uninitialize Razer Synapse: " + num);
			}
			Console.WriteLine("Razer Chroma unitialized.");
		}
		catch (Exception ex)
		{
			if (!(ex is ObjectDisposedException))
			{
				Console.WriteLine("Razer Chroma failed to uninitialize: " + ex);
			}
		}
		_isInitialized = false;
	}

	public override IEnumerator<RgbDevice> GetEnumerator()
	{
		return _devices.GetEnumerator();
	}
}
