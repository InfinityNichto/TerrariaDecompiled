namespace System.Reflection;

internal static class MdConstant
{
	public unsafe static object GetValue(MetadataImport scope, int token, RuntimeTypeHandle fieldTypeHandle, bool raw)
	{
		long value;
		int length;
		CorElementType corElementType;
		string defaultValue = scope.GetDefaultValue(token, out value, out length, out corElementType);
		RuntimeType runtimeType = fieldTypeHandle.GetRuntimeType();
		if (runtimeType.IsEnum && !raw)
		{
			long num = 0L;
			switch (corElementType)
			{
			case CorElementType.ELEMENT_TYPE_VOID:
				return DBNull.Value;
			case CorElementType.ELEMENT_TYPE_CHAR:
				num = *(ushort*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_I1:
				num = *(sbyte*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_U1:
				num = *(byte*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_I2:
				num = *(short*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_U2:
				num = *(ushort*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_I4:
				num = *(int*)(&value);
				break;
			case CorElementType.ELEMENT_TYPE_U4:
				num = (uint)(*(int*)(&value));
				break;
			case CorElementType.ELEMENT_TYPE_I8:
				num = value;
				break;
			case CorElementType.ELEMENT_TYPE_U8:
				num = value;
				break;
			case CorElementType.ELEMENT_TYPE_CLASS:
				return null;
			default:
				throw new FormatException(SR.Arg_BadLiteralFormat);
			}
			return RuntimeType.CreateEnum(runtimeType, num);
		}
		if (runtimeType == typeof(DateTime))
		{
			long num2 = 0L;
			switch (corElementType)
			{
			case CorElementType.ELEMENT_TYPE_VOID:
				return DBNull.Value;
			case CorElementType.ELEMENT_TYPE_I8:
				num2 = value;
				break;
			case CorElementType.ELEMENT_TYPE_U8:
				num2 = value;
				break;
			case CorElementType.ELEMENT_TYPE_CLASS:
				return null;
			default:
				throw new FormatException(SR.Arg_BadLiteralFormat);
			}
			return new DateTime(num2);
		}
		return corElementType switch
		{
			CorElementType.ELEMENT_TYPE_VOID => DBNull.Value, 
			CorElementType.ELEMENT_TYPE_CHAR => *(char*)(&value), 
			CorElementType.ELEMENT_TYPE_I1 => *(sbyte*)(&value), 
			CorElementType.ELEMENT_TYPE_U1 => *(byte*)(&value), 
			CorElementType.ELEMENT_TYPE_I2 => *(short*)(&value), 
			CorElementType.ELEMENT_TYPE_U2 => *(ushort*)(&value), 
			CorElementType.ELEMENT_TYPE_I4 => *(int*)(&value), 
			CorElementType.ELEMENT_TYPE_U4 => *(uint*)(&value), 
			CorElementType.ELEMENT_TYPE_I8 => value, 
			CorElementType.ELEMENT_TYPE_U8 => (ulong)value, 
			CorElementType.ELEMENT_TYPE_BOOLEAN => *(int*)(&value) != 0, 
			CorElementType.ELEMENT_TYPE_R4 => *(float*)(&value), 
			CorElementType.ELEMENT_TYPE_R8 => *(double*)(&value), 
			CorElementType.ELEMENT_TYPE_STRING => defaultValue ?? string.Empty, 
			CorElementType.ELEMENT_TYPE_CLASS => null, 
			_ => throw new FormatException(SR.Arg_BadLiteralFormat), 
		};
	}
}
