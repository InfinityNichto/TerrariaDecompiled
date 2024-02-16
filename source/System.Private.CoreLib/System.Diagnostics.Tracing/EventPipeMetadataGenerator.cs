using System.Reflection;

namespace System.Diagnostics.Tracing;

internal sealed class EventPipeMetadataGenerator
{
	public static EventPipeMetadataGenerator Instance = new EventPipeMetadataGenerator();

	private EventPipeMetadataGenerator()
	{
	}

	public byte[] GenerateEventMetadata(EventSource.EventMetadata eventMetadata)
	{
		ParameterInfo[] parameters = eventMetadata.Parameters;
		EventParameterInfo[] array = new EventParameterInfo[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			EventParameterInfo.GetTypeInfoFromType(parameters[i].ParameterType, out var typeInfo);
			array[i].SetInfo(parameters[i].Name, parameters[i].ParameterType, typeInfo);
		}
		return GenerateMetadata(eventMetadata.Descriptor.EventId, eventMetadata.Name, eventMetadata.Descriptor.Keywords, eventMetadata.Descriptor.Level, eventMetadata.Descriptor.Version, (EventOpcode)eventMetadata.Descriptor.Opcode, array);
	}

	public byte[] GenerateEventMetadata(int eventId, string eventName, EventKeywords keywords, EventLevel level, uint version, EventOpcode opcode, TraceLoggingEventTypes eventTypes)
	{
		TraceLoggingTypeInfo[] typeInfos = eventTypes.typeInfos;
		string[] paramNames = eventTypes.paramNames;
		EventParameterInfo[] array = new EventParameterInfo[typeInfos.Length];
		for (int i = 0; i < typeInfos.Length; i++)
		{
			string name = string.Empty;
			if (paramNames != null)
			{
				name = paramNames[i];
			}
			array[i].SetInfo(name, typeInfos[i].DataType, typeInfos[i]);
		}
		return GenerateMetadata(eventId, eventName, (long)keywords, (uint)level, version, opcode, array);
	}

	internal unsafe byte[] GenerateMetadata(int eventId, string eventName, long keywords, uint level, uint version, EventOpcode opcode, EventParameterInfo[] parameters)
	{
		byte[] array = null;
		bool flag = false;
		try
		{
			uint num = (uint)(24 + (eventName.Length + 1) * 2);
			uint num2 = 0u;
			uint num3 = num;
			if (parameters.Length == 1 && parameters[0].ParameterType == typeof(EmptyStruct))
			{
				parameters = Array.Empty<EventParameterInfo>();
			}
			EventParameterInfo[] array2 = parameters;
			foreach (EventParameterInfo eventParameterInfo in array2)
			{
				if (!eventParameterInfo.GetMetadataLength(out var size))
				{
					flag = true;
					break;
				}
				num += size;
			}
			if (flag)
			{
				num = num3;
				num2 = 4u;
				EventParameterInfo[] array3 = parameters;
				foreach (EventParameterInfo eventParameterInfo2 in array3)
				{
					if (!eventParameterInfo2.GetMetadataLengthV2(out var size2))
					{
						parameters = Array.Empty<EventParameterInfo>();
						num = num3;
						num2 = 0u;
						flag = false;
						break;
					}
					num2 += size2;
				}
			}
			uint num4 = ((opcode != 0) ? 6u : 0u);
			uint num5 = ((num2 != 0) ? (num2 + 5) : 0u);
			uint num6 = num5 + num4;
			uint num7 = num + num6;
			array = new byte[num7];
			fixed (byte* ptr = array)
			{
				uint offset = 0u;
				WriteToBuffer(ptr, num7, ref offset, (uint)eventId);
				fixed (char* src = eventName)
				{
					WriteToBuffer(ptr, num7, ref offset, (byte*)src, (uint)((eventName.Length + 1) * 2));
				}
				WriteToBuffer(ptr, num7, ref offset, keywords);
				WriteToBuffer(ptr, num7, ref offset, version);
				WriteToBuffer(ptr, num7, ref offset, level);
				if (flag)
				{
					WriteToBuffer(ptr, num7, ref offset, 0);
				}
				else
				{
					WriteToBuffer(ptr, num7, ref offset, (uint)parameters.Length);
					EventParameterInfo[] array4 = parameters;
					foreach (EventParameterInfo eventParameterInfo3 in array4)
					{
						if (!eventParameterInfo3.GenerateMetadata(ptr, ref offset, num7))
						{
							return GenerateMetadata(eventId, eventName, keywords, level, version, opcode, Array.Empty<EventParameterInfo>());
						}
					}
				}
				if (opcode != 0)
				{
					WriteToBuffer(ptr, num7, ref offset, 1);
					WriteToBuffer(ptr, num7, ref offset, (byte)1);
					WriteToBuffer(ptr, num7, ref offset, (byte)opcode);
				}
				if (flag)
				{
					WriteToBuffer(ptr, num7, ref offset, num2);
					WriteToBuffer(ptr, num7, ref offset, (byte)2);
					WriteToBuffer(ptr, num7, ref offset, (uint)parameters.Length);
					EventParameterInfo[] array5 = parameters;
					foreach (EventParameterInfo eventParameterInfo4 in array5)
					{
						if (!eventParameterInfo4.GenerateMetadataV2(ptr, ref offset, num7))
						{
							return GenerateMetadata(eventId, eventName, keywords, level, version, opcode, Array.Empty<EventParameterInfo>());
						}
					}
				}
			}
		}
		catch
		{
			array = null;
		}
		return array;
	}

	internal unsafe static void WriteToBuffer(byte* buffer, uint bufferLength, ref uint offset, byte* src, uint srcLength)
	{
		for (int i = 0; i < srcLength; i++)
		{
			(buffer + offset)[i] = src[i];
		}
		offset += srcLength;
	}

	internal unsafe static void WriteToBuffer<T>(byte* buffer, uint bufferLength, ref uint offset, T value) where T : unmanaged
	{
		*(T*)(buffer + offset) = value;
		offset += (uint)sizeof(T);
	}
}
