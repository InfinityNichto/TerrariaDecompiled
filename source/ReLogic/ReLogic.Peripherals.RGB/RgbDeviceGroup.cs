using System;
using System.Collections;
using System.Collections.Generic;

namespace ReLogic.Peripherals.RGB;

public abstract class RgbDeviceGroup : IDisposable, IEnumerable<RgbDevice>, IEnumerable
{
	private bool _isDisposed;

	public bool IsEnabled { get; private set; }

	public void Enable()
	{
		IsEnabled = true;
		Initialize();
	}

	public void Disable()
	{
		IsEnabled = false;
		Uninitialize();
	}

	protected abstract void Initialize();

	protected abstract void Uninitialize();

	public virtual void OnceProcessed()
	{
	}

	public virtual void LoadSpecialRules(object specialRulesObject)
	{
	}

	public abstract IEnumerator<RgbDevice> GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			Disable();
			_isDisposed = true;
		}
	}

	~RgbDeviceGroup()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
