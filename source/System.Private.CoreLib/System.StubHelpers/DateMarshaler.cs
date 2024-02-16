namespace System.StubHelpers;

internal static class DateMarshaler
{
	internal static double ConvertToNative(DateTime managedDate)
	{
		return managedDate.ToOADate();
	}

	internal static long ConvertToManaged(double nativeDate)
	{
		return DateTime.DoubleDateToTicks(nativeDate);
	}
}
