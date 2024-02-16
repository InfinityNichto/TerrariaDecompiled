using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing;

internal static class EventPipePayloadDecoder
{
	internal static object[] DecodePayload(ref EventSource.EventMetadata metadata, ReadOnlySpan<byte> payload)
	{
		ParameterInfo[] parameters = metadata.Parameters;
		object[] array = new object[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			if (payload.Length <= 0)
			{
				break;
			}
			Type parameterType = parameters[i].ParameterType;
			Type type = (parameterType.IsEnum ? Enum.GetUnderlyingType(parameterType) : null);
			if (parameterType == typeof(IntPtr))
			{
				_ = IntPtr.Size;
				array[i] = (IntPtr)BinaryPrimitives.ReadInt64LittleEndian(payload);
				payload = payload.Slice(IntPtr.Size);
			}
			else if (parameterType == typeof(int) || type == typeof(int))
			{
				array[i] = BinaryPrimitives.ReadInt32LittleEndian(payload);
				payload = payload.Slice(4);
			}
			else if (parameterType == typeof(uint) || type == typeof(uint))
			{
				array[i] = BinaryPrimitives.ReadUInt32LittleEndian(payload);
				payload = payload.Slice(4);
			}
			else if (parameterType == typeof(long) || type == typeof(long))
			{
				array[i] = BinaryPrimitives.ReadInt64LittleEndian(payload);
				payload = payload.Slice(8);
			}
			else if (parameterType == typeof(ulong) || type == typeof(ulong))
			{
				array[i] = BinaryPrimitives.ReadUInt64LittleEndian(payload);
				payload = payload.Slice(8);
			}
			else if (parameterType == typeof(byte) || type == typeof(byte))
			{
				array[i] = MemoryMarshal.Read<byte>(payload);
				payload = payload.Slice(1);
			}
			else if (parameterType == typeof(sbyte) || type == typeof(sbyte))
			{
				array[i] = MemoryMarshal.Read<sbyte>(payload);
				payload = payload.Slice(1);
			}
			else if (parameterType == typeof(short) || type == typeof(short))
			{
				array[i] = BinaryPrimitives.ReadInt16LittleEndian(payload);
				payload = payload.Slice(2);
			}
			else if (parameterType == typeof(ushort) || type == typeof(ushort))
			{
				array[i] = BinaryPrimitives.ReadUInt16LittleEndian(payload);
				payload = payload.Slice(2);
			}
			else if (parameterType == typeof(float))
			{
				array[i] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(payload));
				payload = payload.Slice(4);
			}
			else if (parameterType == typeof(double))
			{
				array[i] = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(payload));
				payload = payload.Slice(8);
			}
			else if (parameterType == typeof(bool))
			{
				array[i] = BinaryPrimitives.ReadInt32LittleEndian(payload) == 1;
				payload = payload.Slice(4);
			}
			else if (parameterType == typeof(Guid))
			{
				array[i] = new Guid(payload.Slice(0, 16));
				payload = payload.Slice(16);
			}
			else if (parameterType == typeof(char))
			{
				array[i] = (char)BinaryPrimitives.ReadUInt16LittleEndian(payload);
				payload = payload.Slice(2);
			}
			else
			{
				if (!(parameterType == typeof(string)))
				{
					continue;
				}
				int num = -1;
				for (int j = 1; j < payload.Length; j += 2)
				{
					if (payload[j - 1] == 0 && payload[j] == 0)
					{
						num = j + 1;
						break;
					}
				}
				ReadOnlySpan<char> value;
				if (num < 0)
				{
					value = MemoryMarshal.Cast<byte, char>(payload);
					payload = default(ReadOnlySpan<byte>);
				}
				else
				{
					value = MemoryMarshal.Cast<byte, char>(payload.Slice(0, num - 2));
					payload = payload.Slice(num);
				}
				int num2 = i;
				if (!BitConverter.IsLittleEndian)
				{
				}
				array[num2] = new string(value);
			}
		}
		return array;
	}
}
