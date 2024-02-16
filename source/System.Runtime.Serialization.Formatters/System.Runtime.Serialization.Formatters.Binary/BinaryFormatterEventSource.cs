using System.Diagnostics.Tracing;

namespace System.Runtime.Serialization.Formatters.Binary;

[EventSource(Name = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatterEventSource")]
internal sealed class BinaryFormatterEventSource : EventSource
{
	public static class Keywords
	{
		public const EventKeywords Serialization = (EventKeywords)1L;

		public const EventKeywords Deserialization = (EventKeywords)2L;
	}

	public static readonly BinaryFormatterEventSource Log = new BinaryFormatterEventSource();

	private BinaryFormatterEventSource()
	{
	}

	[Event(10, Opcode = EventOpcode.Start, Keywords = (EventKeywords)1L, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive)]
	public void SerializationStart()
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)1L))
		{
			WriteEvent(10);
		}
	}

	[Event(11, Opcode = EventOpcode.Stop, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	public void SerializationStop()
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)1L))
		{
			WriteEvent(11);
		}
	}

	[NonEvent]
	public void SerializingObject(Type type)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)1L))
		{
			SerializingObject(type.AssemblyQualifiedName);
		}
	}

	[Event(12, Keywords = (EventKeywords)1L, Level = EventLevel.Informational)]
	private void SerializingObject(string typeName)
	{
		WriteEvent(12, typeName);
	}

	[Event(20, Opcode = EventOpcode.Start, Keywords = (EventKeywords)2L, Level = EventLevel.Informational, ActivityOptions = EventActivityOptions.Recursive)]
	public void DeserializationStart()
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)2L))
		{
			WriteEvent(20);
		}
	}

	[Event(21, Opcode = EventOpcode.Stop, Keywords = (EventKeywords)2L, Level = EventLevel.Informational)]
	public void DeserializationStop()
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)2L))
		{
			WriteEvent(21);
		}
	}

	[NonEvent]
	public void DeserializingObject(Type type)
	{
		if (IsEnabled(EventLevel.Informational, (EventKeywords)2L))
		{
			DeserializingObject(type.AssemblyQualifiedName);
		}
	}

	[Event(22, Keywords = (EventKeywords)2L, Level = EventLevel.Informational)]
	private void DeserializingObject(string typeName)
	{
		WriteEvent(22, typeName);
	}
}
