using System.Diagnostics.Tracing;

namespace System.Xml.Serialization;

[EventSource(Name = "System.Xml.Serialzation.XmlSerialization", LocalizationResources = "FxResources.System.Private.Xml.SR")]
internal sealed class XmlSerializationEventSource : EventSource
{
	internal static XmlSerializationEventSource Log = new XmlSerializationEventSource();

	[Event(1, Level = EventLevel.Informational)]
	internal void XmlSerializerExpired(string serializerName, string type)
	{
		WriteEvent(1, serializerName, type);
	}
}
