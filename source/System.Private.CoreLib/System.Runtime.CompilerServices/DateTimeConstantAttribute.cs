namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
public sealed class DateTimeConstantAttribute : CustomConstantAttribute
{
	private readonly DateTime _date;

	public override object Value => _date;

	public DateTimeConstantAttribute(long ticks)
	{
		_date = new DateTime(ticks);
	}
}
