using System.ComponentModel;

namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.All)]
public class MonitoringDescriptionAttribute : DescriptionAttribute
{
	private bool _replaced;

	public override string Description
	{
		get
		{
			if (!_replaced)
			{
				_replaced = true;
				base.DescriptionValue = base.Description;
			}
			return base.Description;
		}
	}

	public MonitoringDescriptionAttribute(string description)
		: base(description)
	{
	}
}
