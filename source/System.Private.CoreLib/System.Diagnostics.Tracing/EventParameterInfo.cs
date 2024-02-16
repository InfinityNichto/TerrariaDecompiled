namespace System.Diagnostics.Tracing;

internal struct EventParameterInfo
{
	internal string ParameterName;

	internal Type ParameterType;

	internal TraceLoggingTypeInfo TypeInfo;

	internal void SetInfo(string name, Type type, TraceLoggingTypeInfo typeInfo = null)
	{
		ParameterName = name;
		ParameterType = type;
		TypeInfo = typeInfo;
	}

	internal unsafe bool GenerateMetadata(byte* pMetadataBlob, ref uint offset, uint blobSize)
	{
		TypeCode typeCodeExtended = GetTypeCodeExtended(ParameterType);
		if (typeCodeExtended == TypeCode.Object)
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 1u);
			if (!(TypeInfo is InvokeTypeInfo { properties: var properties }))
			{
				return false;
			}
			if (properties != null)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis property in array)
				{
					if (!GenerateMetadataForProperty(property, pMetadataBlob, ref offset, blobSize))
					{
						return false;
					}
				}
			}
			else
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 0u);
			}
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, '\0');
		}
		else
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCodeExtended);
			fixed (char* src = ParameterName)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)src, (uint)((ParameterName.Length + 1) * 2));
			}
		}
		return true;
	}

	private unsafe static bool GenerateMetadataForProperty(PropertyAnalysis property, byte* pMetadataBlob, ref uint offset, uint blobSize)
	{
		if (property.typeInfo is InvokeTypeInfo invokeTypeInfo)
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 1u);
			PropertyAnalysis[] properties = invokeTypeInfo.properties;
			if (properties != null)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis property2 in array)
				{
					if (!GenerateMetadataForProperty(property2, pMetadataBlob, ref offset, blobSize))
					{
						return false;
					}
				}
			}
			else
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 0u);
			}
			fixed (char* src = property.name)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)src, (uint)((property.name.Length + 1) * 2));
			}
		}
		else
		{
			TypeCode typeCodeExtended = GetTypeCodeExtended(property.typeInfo.DataType);
			if (typeCodeExtended == TypeCode.Object)
			{
				return false;
			}
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCodeExtended);
			fixed (char* src2 = property.name)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)src2, (uint)((property.name.Length + 1) * 2));
			}
		}
		return true;
	}

	internal unsafe bool GenerateMetadataV2(byte* pMetadataBlob, ref uint offset, uint blobSize)
	{
		if (TypeInfo == null)
		{
			return false;
		}
		return GenerateMetadataForNamedTypeV2(ParameterName, TypeInfo, pMetadataBlob, ref offset, blobSize);
	}

	private unsafe static bool GenerateMetadataForNamedTypeV2(string name, TraceLoggingTypeInfo typeInfo, byte* pMetadataBlob, ref uint offset, uint blobSize)
	{
		if (!GetMetadataLengthForNamedTypeV2(name, typeInfo, out var size))
		{
			return false;
		}
		EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, size);
		fixed (char* src = name)
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (byte*)src, (uint)((name.Length + 1) * 2));
		}
		return GenerateMetadataForTypeV2(typeInfo, pMetadataBlob, ref offset, blobSize);
	}

	private unsafe static bool GenerateMetadataForTypeV2(TraceLoggingTypeInfo typeInfo, byte* pMetadataBlob, ref uint offset, uint blobSize)
	{
		if (typeInfo is InvokeTypeInfo invokeTypeInfo)
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 1u);
			PropertyAnalysis[] properties = invokeTypeInfo.properties;
			if (properties != null)
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)properties.Length);
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis propertyAnalysis in array)
				{
					if (!GenerateMetadataForNamedTypeV2(propertyAnalysis.name, propertyAnalysis.typeInfo, pMetadataBlob, ref offset, blobSize))
					{
						return false;
					}
				}
			}
			else
			{
				EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 0u);
			}
		}
		else if (typeInfo is EnumerableTypeInfo enumerableTypeInfo)
		{
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 19);
			GenerateMetadataForTypeV2(enumerableTypeInfo.ElementInfo, pMetadataBlob, ref offset, blobSize);
		}
		else if (typeInfo is ScalarArrayTypeInfo scalarArrayTypeInfo)
		{
			if (!scalarArrayTypeInfo.DataType.HasElementType)
			{
				return false;
			}
			if (!GetTypeInfoFromType(scalarArrayTypeInfo.DataType.GetElementType(), out var typeInfo2))
			{
				return false;
			}
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, 19);
			GenerateMetadataForTypeV2(typeInfo2, pMetadataBlob, ref offset, blobSize);
		}
		else
		{
			TypeCode typeCodeExtended = GetTypeCodeExtended(typeInfo.DataType);
			if (typeCodeExtended == TypeCode.Object)
			{
				return false;
			}
			EventPipeMetadataGenerator.WriteToBuffer(pMetadataBlob, blobSize, ref offset, (uint)typeCodeExtended);
		}
		return true;
	}

	internal static bool GetTypeInfoFromType(Type type, out TraceLoggingTypeInfo typeInfo)
	{
		if (type == typeof(bool))
		{
			typeInfo = ScalarTypeInfo.Boolean();
			return true;
		}
		if (type == typeof(byte))
		{
			typeInfo = ScalarTypeInfo.Byte();
			return true;
		}
		if (type == typeof(sbyte))
		{
			typeInfo = ScalarTypeInfo.SByte();
			return true;
		}
		if (type == typeof(char))
		{
			typeInfo = ScalarTypeInfo.Char();
			return true;
		}
		if (type == typeof(short))
		{
			typeInfo = ScalarTypeInfo.Int16();
			return true;
		}
		if (type == typeof(ushort))
		{
			typeInfo = ScalarTypeInfo.UInt16();
			return true;
		}
		if (type == typeof(int))
		{
			typeInfo = ScalarTypeInfo.Int32();
			return true;
		}
		if (type == typeof(uint))
		{
			typeInfo = ScalarTypeInfo.UInt32();
			return true;
		}
		if (type == typeof(long))
		{
			typeInfo = ScalarTypeInfo.Int64();
			return true;
		}
		if (type == typeof(ulong))
		{
			typeInfo = ScalarTypeInfo.UInt64();
			return true;
		}
		if (type == typeof(IntPtr))
		{
			typeInfo = ScalarTypeInfo.IntPtr();
			return true;
		}
		if (type == typeof(UIntPtr))
		{
			typeInfo = ScalarTypeInfo.UIntPtr();
			return true;
		}
		if (type == typeof(float))
		{
			typeInfo = ScalarTypeInfo.Single();
			return true;
		}
		if (type == typeof(double))
		{
			typeInfo = ScalarTypeInfo.Double();
			return true;
		}
		if (type == typeof(Guid))
		{
			typeInfo = ScalarTypeInfo.Guid();
			return true;
		}
		typeInfo = null;
		return false;
	}

	internal bool GetMetadataLength(out uint size)
	{
		size = 0u;
		TypeCode typeCodeExtended = GetTypeCodeExtended(ParameterType);
		if (typeCodeExtended == TypeCode.Object)
		{
			if (!(TypeInfo is InvokeTypeInfo invokeTypeInfo))
			{
				return false;
			}
			size += 8u;
			PropertyAnalysis[] properties = invokeTypeInfo.properties;
			if (properties != null)
			{
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis property in array)
				{
					size += GetMetadataLengthForProperty(property);
				}
			}
			size += 2u;
		}
		else
		{
			size += (uint)(4 + (ParameterName.Length + 1) * 2);
		}
		return true;
	}

	private static uint GetMetadataLengthForProperty(PropertyAnalysis property)
	{
		uint num = 0u;
		if (property.typeInfo is InvokeTypeInfo invokeTypeInfo)
		{
			num += 8;
			PropertyAnalysis[] properties = invokeTypeInfo.properties;
			if (properties != null)
			{
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis property2 in array)
				{
					num += GetMetadataLengthForProperty(property2);
				}
			}
			return num + (uint)((property.name.Length + 1) * 2);
		}
		return num + (uint)(4 + (property.name.Length + 1) * 2);
	}

	private static TypeCode GetTypeCodeExtended(Type parameterType)
	{
		if (parameterType == typeof(Guid))
		{
			return (TypeCode)17;
		}
		if (parameterType == typeof(IntPtr))
		{
			_ = IntPtr.Size;
			return TypeCode.Int64;
		}
		if (parameterType == typeof(UIntPtr))
		{
			_ = UIntPtr.Size;
			return TypeCode.UInt64;
		}
		return Type.GetTypeCode(parameterType);
	}

	internal bool GetMetadataLengthV2(out uint size)
	{
		return GetMetadataLengthForNamedTypeV2(ParameterName, TypeInfo, out size);
	}

	private static bool GetMetadataLengthForTypeV2(TraceLoggingTypeInfo typeInfo, out uint size)
	{
		size = 0u;
		if (typeInfo == null)
		{
			return false;
		}
		if (typeInfo is InvokeTypeInfo invokeTypeInfo)
		{
			size += 8u;
			PropertyAnalysis[] properties = invokeTypeInfo.properties;
			if (properties != null)
			{
				PropertyAnalysis[] array = properties;
				foreach (PropertyAnalysis propertyAnalysis in array)
				{
					if (!GetMetadataLengthForNamedTypeV2(propertyAnalysis.name, propertyAnalysis.typeInfo, out var size2))
					{
						return false;
					}
					size += size2;
				}
			}
		}
		else if (typeInfo is EnumerableTypeInfo enumerableTypeInfo)
		{
			size += 4u;
			if (!GetMetadataLengthForTypeV2(enumerableTypeInfo.ElementInfo, out var size3))
			{
				return false;
			}
			size += size3;
		}
		else if (typeInfo is ScalarArrayTypeInfo scalarArrayTypeInfo)
		{
			if (!scalarArrayTypeInfo.DataType.HasElementType || !GetTypeInfoFromType(scalarArrayTypeInfo.DataType.GetElementType(), out var typeInfo2))
			{
				return false;
			}
			size += 4u;
			if (!GetMetadataLengthForTypeV2(typeInfo2, out var size4))
			{
				return false;
			}
			size += size4;
		}
		else
		{
			size += 4u;
		}
		return true;
	}

	private static bool GetMetadataLengthForNamedTypeV2(string name, TraceLoggingTypeInfo typeInfo, out uint size)
	{
		size = (uint)(4 + (name.Length + 1) * 2);
		if (!GetMetadataLengthForTypeV2(typeInfo, out var size2))
		{
			return false;
		}
		size += size2;
		return true;
	}
}
