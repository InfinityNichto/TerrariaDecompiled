using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReLogic.Peripherals.RGB.Corsair;

public class CorsairDeviceGroup : RgbDeviceGroup
{
	private enum State
	{
		Unitialized,
		HandshakeCompleted,
		DeviceControlGranted,
		AnyDevicesAdded
	}

	private readonly List<RgbDevice> _devices = new List<RgbDevice>();

	private State _state;

	private readonly VendorColorProfile _colorProfiles;

	private bool _initializationFailed;

	public CorsairDeviceGroup(VendorColorProfile colorProfiles)
	{
		_colorProfiles = colorProfiles;
	}

	protected override void Initialize()
	{
		if (_state == State.AnyDevicesAdded || _initializationFailed)
		{
			return;
		}
		if (!NativeLibrary.TryLoad("CUESDK.x64_2019.dll", Assembly.GetEntryAssembly(), DllImportSearchPath.SafeDirectories, out var _))
		{
			_initializationFailed = true;
			return;
		}
		try
		{
			if (_state < State.HandshakeCompleted)
			{
				NativeMethods.CorsairPerformProtocolHandshake();
				CorsairError corsairError = NativeMethods.CorsairGetLastError();
				if (corsairError != 0)
				{
					throw new DeviceInitializationException("Corsair initialization failed with: " + corsairError);
				}
				_state = State.HandshakeCompleted;
			}
			if (_state < State.DeviceControlGranted)
			{
				NativeMethods.CorsairRequestControl(CorsairAccessMode.CAM_ExclusiveLightingControl);
				_state = State.DeviceControlGranted;
			}
			int num = NativeMethods.CorsairGetDeviceCount();
			for (int i = 0; i < num; i++)
			{
				CorsairDeviceInfo deviceInfo = (CorsairDeviceInfo)Marshal.PtrToStructure(NativeMethods.CorsairGetDeviceInfo(i), typeof(CorsairDeviceInfo));
				AddDeviceIfSupported(i, deviceInfo);
			}
			Console.WriteLine("Corsair RGB intialized.");
			if (_state != State.AnyDevicesAdded)
			{
				Console.WriteLine("No usable Corsair RGB devices found. Shutting down Corsair SDK.");
				Uninitialize();
			}
		}
		catch (DeviceInitializationException)
		{
			Console.WriteLine("Corsair RGB not supported. (Can be disabled via Config.json)");
			Uninitialize();
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Corsair RGB not supported: " + ex2);
			Uninitialize();
		}
	}

	protected override void Uninitialize()
	{
		if (_state == State.Unitialized)
		{
			return;
		}
		try
		{
			_devices.Clear();
			if (_state >= State.DeviceControlGranted)
			{
				NativeMethods.CorsairReleaseControl(CorsairAccessMode.CAM_ExclusiveLightingControl);
			}
			Console.WriteLine("Corsair RGB unitialized.");
		}
		catch (ObjectDisposedException)
		{
		}
		catch (Exception ex2)
		{
			Console.WriteLine("Corsair RGB failed to uninitialize: " + ex2);
		}
		if (_state >= State.HandshakeCompleted)
		{
			_state = State.HandshakeCompleted;
		}
	}

	private void AddDeviceIfSupported(int deviceIndex, CorsairDeviceInfo deviceInfo)
	{
		if (deviceInfo.CapsMask.HasFlag(CorsairDeviceCaps.CDC_Lighting))
		{
			RgbDevice rgbDevice = null;
			switch (deviceInfo.Type)
			{
			case CorsairDeviceType.CDT_Headset:
				rgbDevice = CorsairHeadset.Create(deviceIndex, _colorProfiles.Headset);
				break;
			case CorsairDeviceType.CDT_Mouse:
				rgbDevice = CorsairMouse.Create(deviceIndex, deviceInfo, _colorProfiles.Mouse);
				break;
			case CorsairDeviceType.CDT_Keyboard:
				rgbDevice = CorsairKeyboard.Create(deviceIndex, _colorProfiles.Keyboard);
				break;
			case CorsairDeviceType.CDT_MouseMat:
				rgbDevice = CorsairMousepad.Create(deviceIndex, _colorProfiles.Mousepad);
				break;
			}
			if (rgbDevice != null && rgbDevice.LedCount > 0)
			{
				_devices.Add(rgbDevice);
				_state = State.AnyDevicesAdded;
			}
		}
	}

	public override IEnumerator<RgbDevice> GetEnumerator()
	{
		return _devices.GetEnumerator();
	}
}
