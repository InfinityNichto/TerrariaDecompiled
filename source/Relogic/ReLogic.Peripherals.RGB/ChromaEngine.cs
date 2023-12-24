using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;

namespace ReLogic.Peripherals.RGB;

public class ChromaEngine
{
	private readonly ChromaPipeline _pipeline = new ChromaPipeline();

	private readonly ShaderSelector _shaderSelector = new ShaderSelector();

	private readonly HotkeyCollection _hotkeys = new HotkeyCollection();

	private readonly Dictionary<string, RgbDeviceGroup> _deviceGroups = new Dictionary<string, RgbDeviceGroup>();

	private readonly object _updateLock = new object();

	private float _lastTime;

	public float FrameTimeInSeconds { get; set; }

	public event EventHandler<ChromaEngineUpdateEventArgs> OnUpdate;

	public ChromaEngine()
	{
		_pipeline.SetHotkeys(_hotkeys);
		FrameTimeInSeconds = 1f / 45f;
	}

	public void AddDeviceGroup(string name, RgbDeviceGroup deviceGroup)
	{
		lock (_updateLock)
		{
			_deviceGroups[name] = deviceGroup;
		}
	}

	public bool HasDeviceGroup(string name)
	{
		return _deviceGroups.ContainsKey(name);
	}

	public void RemoveDeviceGroup(string name)
	{
		RgbDeviceGroup rgbDeviceGroup = _deviceGroups[name];
		lock (_updateLock)
		{
			if (rgbDeviceGroup.IsEnabled)
			{
				rgbDeviceGroup.Disable();
			}
			_deviceGroups.Remove(name);
		}
	}

	public void EnableDeviceGroup(string name)
	{
		RgbDeviceGroup rgbDeviceGroup = _deviceGroups[name];
		lock (_updateLock)
		{
			if (!rgbDeviceGroup.IsEnabled)
			{
				rgbDeviceGroup.Enable();
			}
		}
	}

	public void DisableDeviceGroup(string name)
	{
		RgbDeviceGroup rgbDeviceGroup = _deviceGroups[name];
		lock (_updateLock)
		{
			if (rgbDeviceGroup.IsEnabled)
			{
				rgbDeviceGroup.Disable();
			}
		}
	}

	public void EnableAllDeviceGroups()
	{
		lock (_updateLock)
		{
			foreach (RgbDeviceGroup item in _deviceGroups.Values.Where((RgbDeviceGroup deviceGroup) => !deviceGroup.IsEnabled))
			{
				item.Enable();
			}
		}
	}

	public void DisableAllDeviceGroups()
	{
		lock (_updateLock)
		{
			foreach (RgbDeviceGroup item in _deviceGroups.Values.Where((RgbDeviceGroup deviceGroup) => deviceGroup.IsEnabled))
			{
				item.Disable();
			}
		}
	}

	public void LoadSpecialRules(object specialRulesObject)
	{
		lock (_updateLock)
		{
			foreach (RgbDeviceGroup item in _deviceGroups.Values.Where((RgbDeviceGroup deviceGroup) => deviceGroup.IsEnabled))
			{
				item.LoadSpecialRules(specialRulesObject);
			}
		}
	}

	public bool IsDeviceGroupEnabled(string name)
	{
		return _deviceGroups[name].IsEnabled;
	}

	public void RegisterShader(ChromaShader shader, ChromaCondition condition, ShaderLayer layer)
	{
		lock (_updateLock)
		{
			_shaderSelector.Register(shader, condition, layer);
		}
	}

	public void UnregisterShader(ChromaShader shader)
	{
		lock (_updateLock)
		{
			_shaderSelector.Unregister(shader);
		}
	}

	public RgbKey BindKey(Keys key, string keyTriggerName)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		lock (_updateLock)
		{
			return _hotkeys.BindKey(key, keyTriggerName);
		}
	}

	public void UnbindKey(Keys key)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		lock (_updateLock)
		{
			_hotkeys.UnbindKey(key);
		}
	}

	public void DebugDraw(IDebugDrawer drawer, Vector2 position, float scale)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		foreach (RgbDeviceGroup value in _deviceGroups.Values)
		{
			bool flag = false;
			foreach (RgbDevice item in value)
			{
				item.DebugDraw(drawer, position, scale);
				flag = true;
			}
			if (flag)
			{
				position.Y += 1.2f * scale;
			}
		}
	}

	public void Update(float totalTime)
	{
		if (_deviceGroups.Count == 0)
		{
			return;
		}
		if (totalTime < _lastTime)
		{
			_lastTime = totalTime;
		}
		float num = totalTime - _lastTime;
		if (!(num >= FrameTimeInSeconds) || !Monitor.TryEnter(_updateLock))
		{
			return;
		}
		try
		{
			if (this.OnUpdate != null)
			{
				this.OnUpdate(this, new ChromaEngineUpdateEventArgs(num));
			}
			_hotkeys.UpdateAll(num);
			_shaderSelector.Update(num);
			_lastTime = totalTime;
			ThreadPool.QueueUserWorkItem(delegate(object context)
			{
				((ChromaEngine)context).Draw();
			}, this);
		}
		catch
		{
			DisableAllDeviceGroups();
		}
		finally
		{
			Monitor.Exit(_updateLock);
		}
	}

	private void Draw()
	{
		lock (_updateLock)
		{
			try
			{
				for (int i = 0; i <= 1; i++)
				{
					EffectDetailLevel detail = (EffectDetailLevel)i;
					ICollection<ShaderOperation> shaders = _shaderSelector.AtDetailLevel(detail);
					foreach (RgbDeviceGroup value in _deviceGroups.Values)
					{
						_pipeline.Process(value.Where((RgbDevice device) => device.PreferredLevelOfDetail == detail), shaders, _lastTime);
					}
				}
			}
			catch
			{
				DisableAllDeviceGroups();
			}
			foreach (RgbDeviceGroup value2 in _deviceGroups.Values)
			{
				value2.OnceProcessed();
			}
		}
	}
}
