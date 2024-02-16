using System.Reflection.Emit;

namespace System.Runtime.Serialization;

internal sealed class SwitchState
{
	private readonly Label _defaultLabel;

	private readonly Label _endOfSwitchLabel;

	private bool _defaultDefined;

	internal Label DefaultLabel => _defaultLabel;

	internal Label EndOfSwitchLabel => _endOfSwitchLabel;

	internal bool DefaultDefined
	{
		get
		{
			return _defaultDefined;
		}
		set
		{
			_defaultDefined = value;
		}
	}

	internal SwitchState(Label defaultLabel, Label endOfSwitchLabel)
	{
		_defaultLabel = defaultLabel;
		_endOfSwitchLabel = endOfSwitchLabel;
		_defaultDefined = false;
	}
}
