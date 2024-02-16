namespace System.Reflection;

public static class EventInfoExtensions
{
	public static MethodInfo? GetAddMethod(this EventInfo eventInfo)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetAddMethod();
	}

	public static MethodInfo? GetAddMethod(this EventInfo eventInfo, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetAddMethod(nonPublic);
	}

	public static MethodInfo? GetRaiseMethod(this EventInfo eventInfo)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetRaiseMethod();
	}

	public static MethodInfo? GetRaiseMethod(this EventInfo eventInfo, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetRaiseMethod(nonPublic);
	}

	public static MethodInfo? GetRemoveMethod(this EventInfo eventInfo)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetRemoveMethod();
	}

	public static MethodInfo? GetRemoveMethod(this EventInfo eventInfo, bool nonPublic)
	{
		ArgumentNullException.ThrowIfNull(eventInfo, "eventInfo");
		return eventInfo.GetRemoveMethod(nonPublic);
	}
}
