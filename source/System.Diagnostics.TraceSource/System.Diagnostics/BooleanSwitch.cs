namespace System.Diagnostics;

[SwitchLevel(typeof(bool))]
public class BooleanSwitch : Switch
{
	public bool Enabled
	{
		get
		{
			if (base.SwitchSetting != 0)
			{
				return true;
			}
			return false;
		}
		set
		{
			base.SwitchSetting = (value ? 1 : 0);
		}
	}

	public BooleanSwitch(string displayName, string? description)
		: base(displayName, description)
	{
	}

	public BooleanSwitch(string displayName, string? description, string defaultSwitchValue)
		: base(displayName, description, defaultSwitchValue)
	{
	}

	protected override void OnValueChanged()
	{
		if (bool.TryParse(base.Value, out var result))
		{
			base.SwitchSetting = (result ? 1 : 0);
		}
		else
		{
			base.OnValueChanged();
		}
	}
}
