namespace System.Diagnostics.Tracing;

internal sealed class ScalarTypeInfo : TraceLoggingTypeInfo
{
	private static ScalarTypeInfo s_boolean;

	private static ScalarTypeInfo s_byte;

	private static ScalarTypeInfo s_sbyte;

	private static ScalarTypeInfo s_char;

	private static ScalarTypeInfo s_int16;

	private static ScalarTypeInfo s_uint16;

	private static ScalarTypeInfo s_int32;

	private static ScalarTypeInfo s_uint32;

	private static ScalarTypeInfo s_int64;

	private static ScalarTypeInfo s_uint64;

	private static ScalarTypeInfo s_intptr;

	private static ScalarTypeInfo s_uintptr;

	private static ScalarTypeInfo s_single;

	private static ScalarTypeInfo s_double;

	private static ScalarTypeInfo s_guid;

	private readonly TraceLoggingDataType nativeFormat;

	private ScalarTypeInfo(Type type, TraceLoggingDataType nativeFormat)
		: base(type)
	{
		this.nativeFormat = nativeFormat;
	}

	public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
	{
		collector.AddScalar(name, Statics.FormatScalar(format, nativeFormat));
	}

	public override void WriteData(PropertyValue value)
	{
		TraceLoggingDataCollector.AddScalar(value);
	}

	public static TraceLoggingTypeInfo Boolean()
	{
		return s_boolean ?? (s_boolean = new ScalarTypeInfo(typeof(bool), TraceLoggingDataType.Boolean8));
	}

	public static TraceLoggingTypeInfo Byte()
	{
		return s_byte ?? (s_byte = new ScalarTypeInfo(typeof(byte), TraceLoggingDataType.UInt8));
	}

	public static TraceLoggingTypeInfo SByte()
	{
		return s_sbyte ?? (s_sbyte = new ScalarTypeInfo(typeof(sbyte), TraceLoggingDataType.Int8));
	}

	public static TraceLoggingTypeInfo Char()
	{
		return s_char ?? (s_char = new ScalarTypeInfo(typeof(char), TraceLoggingDataType.Char16));
	}

	public static TraceLoggingTypeInfo Int16()
	{
		return s_int16 ?? (s_int16 = new ScalarTypeInfo(typeof(short), TraceLoggingDataType.Int16));
	}

	public static TraceLoggingTypeInfo UInt16()
	{
		return s_uint16 ?? (s_uint16 = new ScalarTypeInfo(typeof(ushort), TraceLoggingDataType.UInt16));
	}

	public static TraceLoggingTypeInfo Int32()
	{
		return s_int32 ?? (s_int32 = new ScalarTypeInfo(typeof(int), TraceLoggingDataType.Int32));
	}

	public static TraceLoggingTypeInfo UInt32()
	{
		return s_uint32 ?? (s_uint32 = new ScalarTypeInfo(typeof(uint), TraceLoggingDataType.UInt32));
	}

	public static TraceLoggingTypeInfo Int64()
	{
		return s_int64 ?? (s_int64 = new ScalarTypeInfo(typeof(long), TraceLoggingDataType.Int64));
	}

	public static TraceLoggingTypeInfo UInt64()
	{
		return s_uint64 ?? (s_uint64 = new ScalarTypeInfo(typeof(ulong), TraceLoggingDataType.UInt64));
	}

	public static TraceLoggingTypeInfo IntPtr()
	{
		return s_intptr ?? (s_intptr = new ScalarTypeInfo(typeof(IntPtr), Statics.IntPtrType));
	}

	public static TraceLoggingTypeInfo UIntPtr()
	{
		return s_uintptr ?? (s_uintptr = new ScalarTypeInfo(typeof(UIntPtr), Statics.UIntPtrType));
	}

	public static TraceLoggingTypeInfo Single()
	{
		return s_single ?? (s_single = new ScalarTypeInfo(typeof(float), TraceLoggingDataType.Float));
	}

	public static TraceLoggingTypeInfo Double()
	{
		return s_double ?? (s_double = new ScalarTypeInfo(typeof(double), TraceLoggingDataType.Double));
	}

	public static TraceLoggingTypeInfo Guid()
	{
		return s_guid ?? (s_guid = new ScalarTypeInfo(typeof(Guid), TraceLoggingDataType.Guid));
	}
}
