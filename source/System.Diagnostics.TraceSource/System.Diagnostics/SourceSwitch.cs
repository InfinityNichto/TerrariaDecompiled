namespace System.Diagnostics;

public class SourceSwitch : Switch
{
	public SourceLevels Level
	{
		get
		{
			return (SourceLevels)base.SwitchSetting;
		}
		set
		{
			base.SwitchSetting = (int)value;
		}
	}

	public SourceSwitch(string name)
		: base(name, string.Empty)
	{
	}

	public SourceSwitch(string displayName, string defaultSwitchValue)
		: base(displayName, string.Empty, defaultSwitchValue)
	{
	}

	public bool ShouldTrace(TraceEventType eventType)
	{
		return ((uint)base.SwitchSetting & (uint)eventType) != 0;
	}

	protected override void OnValueChanged()
	{
		base.SwitchSetting = (int)Enum.Parse(typeof(SourceLevels), base.Value, ignoreCase: true);
	}
}
