using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace ReLogic.Peripherals.RGB;

internal class HotkeyCollection : IEnumerable<RgbKey>, IEnumerable
{
	private Dictionary<Keys, RgbKey> _keys = new Dictionary<Keys, RgbKey>();

	public RgbKey BindKey(Keys key, string keyTriggerName)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!_keys.ContainsKey(key))
		{
			_keys.Add(key, new RgbKey(key, keyTriggerName));
		}
		return _keys[key];
	}

	public void UnbindKey(Keys key)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_keys.Remove(key);
	}

	public IEnumerator<RgbKey> GetEnumerator()
	{
		return _keys.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _keys.Values.GetEnumerator();
	}

	public void UpdateAll(float timeElapsed)
	{
		foreach (RgbKey value in _keys.Values)
		{
			value.Update(timeElapsed);
		}
	}
}
