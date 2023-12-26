using System.Collections.Generic;
using System.Linq;

namespace ReLogic.Peripherals.RGB;

internal class ChromaPipeline
{
	public delegate void PostProcessingEvent(RgbDevice device, Fragment fragment, float time);

	private IEnumerable<RgbKey> _hotkeys;

	public event PostProcessingEvent PostProcessingEvents;

	public void SetHotkeys(IEnumerable<RgbKey> keys)
	{
		_hotkeys = keys;
	}

	public void Process(IEnumerable<RgbDevice> devices, IEnumerable<ShaderOperation> shaders, float time)
	{
		foreach (RgbDevice device in devices)
		{
			ProcessDevice(device, shaders, time);
			device.Present();
		}
	}

	private void ProcessDevice(RgbDevice device, IEnumerable<ShaderOperation> shaders, float time)
	{
		Fragment fragment = device.Rasterize();
		if (shaders != null)
		{
			foreach (ShaderOperation shader in shaders)
			{
				shader.Process(device, fragment, time);
				device.Render(fragment, shader.BlendState);
			}
		}
		if (_hotkeys != null && device is RgbKeyboard rgbKeyboard)
		{
			rgbKeyboard.Render(_hotkeys.Where((RgbKey key) => key.IsVisible));
		}
		if (this.PostProcessingEvents != null)
		{
			fragment.Clear();
			this.PostProcessingEvents(device, fragment, time);
			device.Render(fragment, new ShaderBlendState(BlendMode.PerPixelOpacity));
		}
	}
}
