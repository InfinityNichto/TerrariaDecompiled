namespace System.Diagnostics.Tracing;

internal static class TraceLoggingDataCollector
{
	public static int BeginBufferedArray()
	{
		return DataCollector.ThreadInstance.BeginBufferedArray();
	}

	public static void EndBufferedArray(int bookmark, int count)
	{
		DataCollector.ThreadInstance.EndBufferedArray(bookmark, count);
	}

	public unsafe static void AddScalar(PropertyValue value)
	{
		PropertyValue.Scalar scalarValue = value.ScalarValue;
		DataCollector.ThreadInstance.AddScalar(&scalarValue, value.ScalarLength);
	}

	public unsafe static void AddScalar(long value)
	{
		DataCollector.ThreadInstance.AddScalar(&value, 8);
	}

	public unsafe static void AddScalar(double value)
	{
		DataCollector.ThreadInstance.AddScalar(&value, 8);
	}

	public unsafe static void AddScalar(bool value)
	{
		DataCollector.ThreadInstance.AddScalar(&value, 1);
	}

	public static void AddNullTerminatedString(string value)
	{
		DataCollector.ThreadInstance.AddNullTerminatedString(value);
	}

	public static void AddArray(PropertyValue value, int elementSize)
	{
		Array array = (Array)value.ReferenceValue;
		DataCollector.ThreadInstance.AddArray(array, array?.Length ?? 0, elementSize);
	}
}
