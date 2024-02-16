namespace System.Diagnostics.Tracing;

internal sealed class ScalarArrayTypeInfo : TraceLoggingTypeInfo
{
	private static ScalarArrayTypeInfo s_boolean;

	private static ScalarArrayTypeInfo s_byte;

	private static ScalarArrayTypeInfo s_sbyte;

	private static ScalarArrayTypeInfo s_char;

	private static ScalarArrayTypeInfo s_int16;

	private static ScalarArrayTypeInfo s_uint16;

	private static ScalarArrayTypeInfo s_int32;

	private static ScalarArrayTypeInfo s_uint32;

	private static ScalarArrayTypeInfo s_int64;

	private static ScalarArrayTypeInfo s_uint64;

	private static ScalarArrayTypeInfo s_intptr;

	private static ScalarArrayTypeInfo s_uintptr;

	private static ScalarArrayTypeInfo s_single;

	private static ScalarArrayTypeInfo s_double;

	private static ScalarArrayTypeInfo s_guid;

	private readonly TraceLoggingDataType nativeFormat;

	private readonly int elementSize;

	private ScalarArrayTypeInfo(Type type, TraceLoggingDataType nativeFormat, int elementSize)
		: base(type)
	{
		this.nativeFormat = nativeFormat;
		this.elementSize = elementSize;
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddArray(name, Statics.FormatScalar(format, nativeFormat));
	}

	public override void WriteData(PropertyValue value)
	{
		TraceLoggingDataCollector.AddArray(value, elementSize);
	}

	public static TraceLoggingTypeInfo Boolean()
	{
		return s_boolean ?? (s_boolean = new ScalarArrayTypeInfo(typeof(bool[]), TraceLoggingDataType.Boolean8, 1));
	}

	public static TraceLoggingTypeInfo Byte()
	{
		return s_byte ?? (s_byte = new ScalarArrayTypeInfo(typeof(byte[]), TraceLoggingDataType.UInt8, 1));
	}

	public static TraceLoggingTypeInfo SByte()
	{
		return s_sbyte ?? (s_sbyte = new ScalarArrayTypeInfo(typeof(sbyte[]), TraceLoggingDataType.Int8, 1));
	}

	public static TraceLoggingTypeInfo Char()
	{
		return s_char ?? (s_char = new ScalarArrayTypeInfo(typeof(char[]), TraceLoggingDataType.Char16, 2));
	}

	public static TraceLoggingTypeInfo Int16()
	{
		return s_int16 ?? (s_int16 = new ScalarArrayTypeInfo(typeof(short[]), TraceLoggingDataType.Int16, 2));
	}

	public static TraceLoggingTypeInfo UInt16()
	{
		return s_uint16 ?? (s_uint16 = new ScalarArrayTypeInfo(typeof(ushort[]), TraceLoggingDataType.UInt16, 2));
	}

	public static TraceLoggingTypeInfo Int32()
	{
		return s_int32 ?? (s_int32 = new ScalarArrayTypeInfo(typeof(int[]), TraceLoggingDataType.Int32, 4));
	}

	public static TraceLoggingTypeInfo UInt32()
	{
		return s_uint32 ?? (s_uint32 = new ScalarArrayTypeInfo(typeof(uint[]), TraceLoggingDataType.UInt32, 4));
	}

	public static TraceLoggingTypeInfo Int64()
	{
		return s_int64 ?? (s_int64 = new ScalarArrayTypeInfo(typeof(long[]), TraceLoggingDataType.Int64, 8));
	}

	public static TraceLoggingTypeInfo UInt64()
	{
		return s_uint64 ?? (s_uint64 = new ScalarArrayTypeInfo(typeof(ulong[]), TraceLoggingDataType.UInt64, 8));
	}

	public static TraceLoggingTypeInfo IntPtr()
	{
		return s_intptr ?? (s_intptr = new ScalarArrayTypeInfo(typeof(IntPtr[]), Statics.IntPtrType, System.IntPtr.Size));
	}

	public static TraceLoggingTypeInfo UIntPtr()
	{
		return s_uintptr ?? (s_uintptr = new ScalarArrayTypeInfo(typeof(UIntPtr[]), Statics.UIntPtrType, System.IntPtr.Size));
	}

	public static TraceLoggingTypeInfo Single()
	{
		return s_single ?? (s_single = new ScalarArrayTypeInfo(typeof(float[]), TraceLoggingDataType.Float, 4));
	}

	public static TraceLoggingTypeInfo Double()
	{
		return s_double ?? (s_double = new ScalarArrayTypeInfo(typeof(double[]), TraceLoggingDataType.Double, 8));
	}

	public unsafe static TraceLoggingTypeInfo Guid()
	{
		return s_guid ?? (s_guid = new ScalarArrayTypeInfo(typeof(Guid[]), TraceLoggingDataType.Guid, sizeof(Guid)));
	}
}
