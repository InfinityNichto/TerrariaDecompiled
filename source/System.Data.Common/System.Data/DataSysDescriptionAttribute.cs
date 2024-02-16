using System.ComponentModel;

namespace System.Data;

[AttributeUsage(AttributeTargets.All)]
[Obsolete("DataSysDescriptionAttribute has been deprecated and is not supported.")]
public class DataSysDescriptionAttribute : DescriptionAttribute
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

	[Obsolete("DataSysDescriptionAttribute has been deprecated and is not supported.")]
	public DataSysDescriptionAttribute(string description)
		: base(description)
	{
	}
}
