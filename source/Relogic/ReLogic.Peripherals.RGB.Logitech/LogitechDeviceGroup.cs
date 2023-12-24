using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Logitech;

public class LogitechDeviceGroup : RgbDeviceGroup
{
	private readonly List<RgbDevice> _devices = new List<RgbDevice>();

	private bool _isInitialized;

	private readonly VendorColorProfile _colorProfiles;

	private bool _initializationFailed;

	public LogitechDeviceGroup(VendorColorProfile colorProfiles)
	{
		_colorProfiles = colorProfiles;
	}

	protected override void Initialize()
	{
		if (_isInitialized || _initializationFailed)
		{
			return;
		}
		if (!NativeLibrary.TryLoad("LogitechLedEnginesWrapper ", out var _))
		{
			_initializationFailed = true;
			return;
		}
		try
		{
			if (!NativeMethods.LogiLedInit())
			{
				throw new DeviceInitializationException("LogitechGSDK failed to initialize.");
			}
			_isInitialized = true;
			if (!NativeMethods.LogiLedSetTargetDevice(6))
			{
				throw new DeviceInitializationException("LogitechGSDK failed to target RGB devices.");
			}
			_devices.Add(new LogitechKeyboard(_colorProfiles.Keyboard));
			_devices.Add(new LogitechSingleLightDevice(_colorProfiles.Generic));
			Console.WriteLine("Logitech RGB initialized.");
		}
		catch (DeviceInitializationException)
		{
			Console.WriteLine("Logitech RGB not supported. (Can be disabled via Config.json)");
			Uninitialize();
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Logitech RGB not supported: " + ex2);
		}
	}

	protected override void Uninitialize()
	{
		if (_isInitialized)
		{
			try
			{
				NativeMethods.LogiLedShutdown();
				Console.WriteLine("Logitech RGB uninitialized.");
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex2)
			{
				Console.WriteLine("Logitech RGB failed to uninitialize: " + ex2);
			}
			_isInitialized = false;
		}
	}

	public override IEnumerator<RgbDevice> GetEnumerator()
	{
		return _devices.GetEnumerator();
	}
}
