using System.ComponentModel;

namespace System.Timers;

[AttributeUsage(AttributeTargets.All)]
public class TimersDescriptionAttribute : DescriptionAttribute
{
	private bool _replaced;

	public override string Description
	{
		get
		{
			if (!_replaced)
			{
				_replaced = true;
				base.DescriptionValue = System.SR.Format(base.Description);
			}
			return base.Description;
		}
	}

	public TimersDescriptionAttribute(string description)
		: base(description)
	{
	}

	internal TimersDescriptionAttribute(string description, string unused)
		: base(System.SR.GetResourceString(description))
	{
	}
}
