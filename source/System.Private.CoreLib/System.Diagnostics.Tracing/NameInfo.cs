using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics.Tracing;

internal sealed class NameInfo : ConcurrentSetItem<KeyValuePair<string, EventTags>, NameInfo>
{
	private static int lastIdentity = 184549376;

	internal readonly string name;

	internal readonly EventTags tags;

	internal readonly int identity;

	internal readonly byte[] nameMetadata;

	internal static void ReserveEventIDsBelow(int eventId)
	{
		int num;
		int val;
		do
		{
			num = lastIdentity;
			val = (lastIdentity & -16777216) + eventId;
			val = Math.Max(val, num);
		}
		while (Interlocked.CompareExchange(ref lastIdentity, val, num) != num);
	}

	public NameInfo(string name, EventTags tags, int typeMetadataSize)
	{
		this.name = name;
		this.tags = tags & (EventTags)268435455;
		identity = Interlocked.Increment(ref lastIdentity);
		int pos = 0;
		Statics.EncodeTags((int)this.tags, ref pos, null);
		nameMetadata = Statics.MetadataForString(name, pos, 0, typeMetadataSize);
		pos = 2;
		Statics.EncodeTags((int)this.tags, ref pos, nameMetadata);
	}

	public override int Compare(NameInfo other)
	{
		return Compare(other.name, other.tags);
	}

	public override int Compare(KeyValuePair<string, EventTags> key)
	{
		return Compare(key.Key, key.Value & (EventTags)268435455);
	}

	private int Compare(string otherName, EventTags otherTags)
	{
		int num = StringComparer.Ordinal.Compare(name, otherName);
		if (num == 0 && tags != otherTags)
		{
			num = ((tags >= otherTags) ? 1 : (-1));
		}
		return num;
	}

	public unsafe IntPtr GetOrCreateEventHandle(EventProvider provider, TraceLoggingEventHandleTable eventHandleTable, EventDescriptor descriptor, TraceLoggingEventTypes eventTypes)
	{
		IntPtr intPtr;
		if ((intPtr = eventHandleTable[descriptor.EventId]) == IntPtr.Zero)
		{
			lock (eventHandleTable)
			{
				if ((intPtr = eventHandleTable[descriptor.EventId]) == IntPtr.Zero)
				{
					byte[] array = EventPipeMetadataGenerator.Instance.GenerateEventMetadata(descriptor.EventId, name, (EventKeywords)descriptor.Keywords, (EventLevel)descriptor.Level, descriptor.Version, (EventOpcode)descriptor.Opcode, eventTypes);
					uint metadataLength = ((array != null) ? ((uint)array.Length) : 0u);
					fixed (byte* pMetadata = array)
					{
						intPtr = provider.m_eventProvider.DefineEventHandle((uint)descriptor.EventId, name, descriptor.Keywords, descriptor.Version, descriptor.Level, pMetadata, metadataLength);
					}
					eventHandleTable.SetEventHandle(descriptor.EventId, intPtr);
				}
			}
		}
		return intPtr;
	}
}
