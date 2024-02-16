using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Diagnostics.Tracing;

internal static class SimpleEventTypes<T>
{
	private static TraceLoggingEventTypes instance;

	public static TraceLoggingEventTypes Instance
	{
		[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
		get
		{
			return instance ?? (instance = InitInstance());
		}
	}

	[RequiresUnreferencedCode("EventSource WriteEvent will serialize the whole object graph. Trimmer will not safely handle this case because properties may be trimmed. This can be suppressed if the object is a primitive type")]
	private static TraceLoggingEventTypes InitInstance()
	{
		TraceLoggingTypeInfo traceLoggingTypeInfo = TraceLoggingTypeInfo.GetInstance(typeof(T), null);
		TraceLoggingEventTypes value = new TraceLoggingEventTypes(traceLoggingTypeInfo.Name, traceLoggingTypeInfo.Tags, traceLoggingTypeInfo);
		Interlocked.CompareExchange(ref instance, value, null);
		return instance;
	}
}
