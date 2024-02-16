using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler;

internal static class ILGen
{
	private static readonly MethodInfo s_nullableHasValueGetter = typeof(Nullable<>).GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);

	private static readonly MethodInfo s_nullableValueGetter = typeof(Nullable<>).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);

	private static readonly MethodInfo s_nullableGetValueOrDefault = typeof(Nullable<>).GetMethod("GetValueOrDefault", Type.EmptyTypes);

	internal static void Emit(this ILGenerator il, OpCode opcode, MethodBase methodBase)
	{
		if (methodBase is ConstructorInfo con)
		{
			il.Emit(opcode, con);
		}
		else
		{
			il.Emit(opcode, (MethodInfo)methodBase);
		}
	}

	internal static void EmitLoadArg(this ILGenerator il, int index)
	{
		il.Emit(OpCodes.Ldarg, index);
	}

	internal static void EmitLoadArgAddress(this ILGenerator il, int index)
	{
		il.Emit(OpCodes.Ldarga, index);
	}

	internal static void EmitStoreArg(this ILGenerator il, int index)
	{
		il.Emit(OpCodes.Starg, index);
	}

	internal static void EmitLoadValueIndirect(this ILGenerator il, Type type)
	{
		switch (type.GetTypeCode())
		{
		case TypeCode.SByte:
			il.Emit(OpCodes.Ldind_I1);
			return;
		case TypeCode.Boolean:
		case TypeCode.Byte:
			il.Emit(OpCodes.Ldind_U1);
			return;
		case TypeCode.Int16:
			il.Emit(OpCodes.Ldind_I2);
			return;
		case TypeCode.Char:
		case TypeCode.UInt16:
			il.Emit(OpCodes.Ldind_U2);
			return;
		case TypeCode.Int32:
			il.Emit(OpCodes.Ldind_I4);
			return;
		case TypeCode.UInt32:
			il.Emit(OpCodes.Ldind_U4);
			return;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			il.Emit(OpCodes.Ldind_I8);
			return;
		case TypeCode.Single:
			il.Emit(OpCodes.Ldind_R4);
			return;
		case TypeCode.Double:
			il.Emit(OpCodes.Ldind_R8);
			return;
		}
		if (type.IsValueType)
		{
			il.Emit(OpCodes.Ldobj, type);
		}
		else
		{
			il.Emit(OpCodes.Ldind_Ref);
		}
	}

	internal static void EmitStoreValueIndirect(this ILGenerator il, Type type)
	{
		switch (type.GetTypeCode())
		{
		case TypeCode.Boolean:
		case TypeCode.SByte:
		case TypeCode.Byte:
			il.Emit(OpCodes.Stind_I1);
			return;
		case TypeCode.Char:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			il.Emit(OpCodes.Stind_I2);
			return;
		case TypeCode.Int32:
		case TypeCode.UInt32:
			il.Emit(OpCodes.Stind_I4);
			return;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			il.Emit(OpCodes.Stind_I8);
			return;
		case TypeCode.Single:
			il.Emit(OpCodes.Stind_R4);
			return;
		case TypeCode.Double:
			il.Emit(OpCodes.Stind_R8);
			return;
		}
		if (type.IsValueType)
		{
			il.Emit(OpCodes.Stobj, type);
		}
		else
		{
			il.Emit(OpCodes.Stind_Ref);
		}
	}

	internal static void EmitLoadElement(this ILGenerator il, Type type)
	{
		if (!type.IsValueType)
		{
			il.Emit(OpCodes.Ldelem_Ref);
			return;
		}
		switch (type.GetTypeCode())
		{
		case TypeCode.Boolean:
		case TypeCode.SByte:
			il.Emit(OpCodes.Ldelem_I1);
			break;
		case TypeCode.Byte:
			il.Emit(OpCodes.Ldelem_U1);
			break;
		case TypeCode.Int16:
			il.Emit(OpCodes.Ldelem_I2);
			break;
		case TypeCode.Char:
		case TypeCode.UInt16:
			il.Emit(OpCodes.Ldelem_U2);
			break;
		case TypeCode.Int32:
			il.Emit(OpCodes.Ldelem_I4);
			break;
		case TypeCode.UInt32:
			il.Emit(OpCodes.Ldelem_U4);
			break;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			il.Emit(OpCodes.Ldelem_I8);
			break;
		case TypeCode.Single:
			il.Emit(OpCodes.Ldelem_R4);
			break;
		case TypeCode.Double:
			il.Emit(OpCodes.Ldelem_R8);
			break;
		default:
			il.Emit(OpCodes.Ldelem, type);
			break;
		}
	}

	internal static void EmitStoreElement(this ILGenerator il, Type type)
	{
		switch (type.GetTypeCode())
		{
		case TypeCode.Boolean:
		case TypeCode.SByte:
		case TypeCode.Byte:
			il.Emit(OpCodes.Stelem_I1);
			return;
		case TypeCode.Char:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			il.Emit(OpCodes.Stelem_I2);
			return;
		case TypeCode.Int32:
		case TypeCode.UInt32:
			il.Emit(OpCodes.Stelem_I4);
			return;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			il.Emit(OpCodes.Stelem_I8);
			return;
		case TypeCode.Single:
			il.Emit(OpCodes.Stelem_R4);
			return;
		case TypeCode.Double:
			il.Emit(OpCodes.Stelem_R8);
			return;
		}
		if (type.IsValueType)
		{
			il.Emit(OpCodes.Stelem, type);
		}
		else
		{
			il.Emit(OpCodes.Stelem_Ref);
		}
	}

	internal static void EmitType(this ILGenerator il, Type type)
	{
		il.Emit(OpCodes.Ldtoken, type);
		il.Emit(OpCodes.Call, CachedReflectionInfo.Type_GetTypeFromHandle);
	}

	internal static void EmitFieldAddress(this ILGenerator il, FieldInfo fi)
	{
		il.Emit(fi.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, fi);
	}

	internal static void EmitFieldGet(this ILGenerator il, FieldInfo fi)
	{
		il.Emit(fi.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fi);
	}

	internal static void EmitFieldSet(this ILGenerator il, FieldInfo fi)
	{
		il.Emit(fi.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fi);
	}

	internal static void EmitNew(this ILGenerator il, ConstructorInfo ci)
	{
		il.Emit(OpCodes.Newobj, ci);
	}

	internal static void EmitNull(this ILGenerator il)
	{
		il.Emit(OpCodes.Ldnull);
	}

	internal static void EmitString(this ILGenerator il, string value)
	{
		il.Emit(OpCodes.Ldstr, value);
	}

	internal static void EmitPrimitive(this ILGenerator il, bool value)
	{
		il.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
	}

	internal static void EmitPrimitive(this ILGenerator il, int value)
	{
		il.Emit(OpCodes.Ldc_I4, value);
	}

	private static void EmitPrimitive(this ILGenerator il, uint value)
	{
		il.EmitPrimitive((int)value);
	}

	private static void EmitPrimitive(this ILGenerator il, long value)
	{
		if (int.MinValue <= value && value <= uint.MaxValue)
		{
			il.EmitPrimitive((int)value);
			il.Emit((value > 0) ? OpCodes.Conv_U8 : OpCodes.Conv_I8);
		}
		else
		{
			il.Emit(OpCodes.Ldc_I8, value);
		}
	}

	private static void EmitPrimitive(this ILGenerator il, ulong value)
	{
		il.EmitPrimitive((long)value);
	}

	private static void EmitPrimitive(this ILGenerator il, double value)
	{
		il.Emit(OpCodes.Ldc_R8, value);
	}

	private static void EmitPrimitive(this ILGenerator il, float value)
	{
		il.Emit(OpCodes.Ldc_R4, value);
	}

	internal static bool CanEmitConstant(object value, Type type)
	{
		if (value == null || CanEmitILConstant(type))
		{
			return true;
		}
		if (value is Type t)
		{
			return ShouldLdtoken(t);
		}
		if (value is MethodBase mb)
		{
			return ShouldLdtoken(mb);
		}
		return false;
	}

	private static bool CanEmitILConstant(Type type)
	{
		TypeCode typeCode = type.GetNonNullableType().GetTypeCode();
		if ((uint)(typeCode - 3) <= 12u || typeCode == TypeCode.String)
		{
			return true;
		}
		return false;
	}

	internal static bool TryEmitConstant(this ILGenerator il, object value, Type type, ILocalCache locals)
	{
		if (value == null)
		{
			il.EmitDefault(type, locals);
			return true;
		}
		if (il.TryEmitILConstant(value, type))
		{
			return true;
		}
		if (value is Type type2)
		{
			if (ShouldLdtoken(type2))
			{
				il.EmitType(type2);
				if (type != typeof(Type))
				{
					il.Emit(OpCodes.Castclass, type);
				}
				return true;
			}
			return false;
		}
		if (value is MethodBase methodBase && ShouldLdtoken(methodBase))
		{
			il.Emit(OpCodes.Ldtoken, methodBase);
			Type declaringType = methodBase.DeclaringType;
			if (declaringType != null && declaringType.IsGenericType)
			{
				il.Emit(OpCodes.Ldtoken, declaringType);
				il.Emit(OpCodes.Call, CachedReflectionInfo.MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle);
			}
			else
			{
				il.Emit(OpCodes.Call, CachedReflectionInfo.MethodBase_GetMethodFromHandle_RuntimeMethodHandle);
			}
			if (type != typeof(MethodBase))
			{
				il.Emit(OpCodes.Castclass, type);
			}
			return true;
		}
		return false;
	}

	private static bool ShouldLdtoken(Type t)
	{
		if (!t.IsGenericParameter)
		{
			return t.IsVisible;
		}
		return true;
	}

	internal static bool ShouldLdtoken(MethodBase mb)
	{
		if (mb is DynamicMethod)
		{
			return false;
		}
		Type declaringType = mb.DeclaringType;
		if (!(declaringType == null))
		{
			return ShouldLdtoken(declaringType);
		}
		return true;
	}

	private static bool TryEmitILConstant(this ILGenerator il, object value, Type type)
	{
		if (type.IsNullableType())
		{
			Type nonNullableType = type.GetNonNullableType();
			if (il.TryEmitILConstant(value, nonNullableType))
			{
				il.Emit(OpCodes.Newobj, TypeUtils.GetNullableConstructor(type));
				return true;
			}
			return false;
		}
		switch (type.GetTypeCode())
		{
		case TypeCode.Boolean:
			il.EmitPrimitive((bool)value);
			return true;
		case TypeCode.SByte:
			il.EmitPrimitive((sbyte)value);
			return true;
		case TypeCode.Int16:
			il.EmitPrimitive((short)value);
			return true;
		case TypeCode.Int32:
			il.EmitPrimitive((int)value);
			return true;
		case TypeCode.Int64:
			il.EmitPrimitive((long)value);
			return true;
		case TypeCode.Single:
			il.EmitPrimitive((float)value);
			return true;
		case TypeCode.Double:
			il.EmitPrimitive((double)value);
			return true;
		case TypeCode.Char:
			il.EmitPrimitive((char)value);
			return true;
		case TypeCode.Byte:
			il.EmitPrimitive((byte)value);
			return true;
		case TypeCode.UInt16:
			il.EmitPrimitive((ushort)value);
			return true;
		case TypeCode.UInt32:
			il.EmitPrimitive((uint)value);
			return true;
		case TypeCode.UInt64:
			il.EmitPrimitive((ulong)value);
			return true;
		case TypeCode.Decimal:
			il.EmitDecimal((decimal)value);
			return true;
		case TypeCode.String:
			il.EmitString((string)value);
			return true;
		default:
			return false;
		}
	}

	internal static void EmitConvertToType(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		if (!TypeUtils.AreEquivalent(typeFrom, typeTo))
		{
			bool flag = typeFrom.IsNullableType();
			bool flag2 = typeTo.IsNullableType();
			Type nonNullableType = typeFrom.GetNonNullableType();
			Type nonNullableType2 = typeTo.GetNonNullableType();
			if (typeFrom.IsInterface || typeTo.IsInterface || typeFrom == typeof(object) || typeTo == typeof(object) || typeFrom == typeof(Enum) || typeFrom == typeof(ValueType) || TypeUtils.IsLegalExplicitVariantDelegateConversion(typeFrom, typeTo))
			{
				il.EmitCastToType(typeFrom, typeTo);
			}
			else if (flag || flag2)
			{
				il.EmitNullableConversion(typeFrom, typeTo, isChecked, locals);
			}
			else if ((!typeFrom.IsConvertible() || !typeTo.IsConvertible()) && (nonNullableType.IsAssignableFrom(nonNullableType2) || nonNullableType2.IsAssignableFrom(nonNullableType)))
			{
				il.EmitCastToType(typeFrom, typeTo);
			}
			else if (typeFrom.IsArray && typeTo.IsArray)
			{
				il.EmitCastToType(typeFrom, typeTo);
			}
			else
			{
				il.EmitNumericConversion(typeFrom, typeTo, isChecked);
			}
		}
	}

	private static void EmitCastToType(this ILGenerator il, Type typeFrom, Type typeTo)
	{
		if (typeFrom.IsValueType)
		{
			il.Emit(OpCodes.Box, typeFrom);
			if (typeTo != typeof(object))
			{
				il.Emit(OpCodes.Castclass, typeTo);
			}
		}
		else
		{
			il.Emit(typeTo.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, typeTo);
		}
	}

	private static void EmitNumericConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked)
	{
		TypeCode typeCode = typeTo.GetTypeCode();
		TypeCode typeCode2 = typeFrom.GetTypeCode();
		if (typeCode == typeCode2)
		{
			return;
		}
		bool flag = typeCode2.IsUnsigned();
		OpCode opcode;
		switch (typeCode)
		{
		case TypeCode.Single:
			if (flag)
			{
				il.Emit(OpCodes.Conv_R_Un);
			}
			opcode = OpCodes.Conv_R4;
			break;
		case TypeCode.Double:
			if (flag)
			{
				il.Emit(OpCodes.Conv_R_Un);
			}
			opcode = OpCodes.Conv_R8;
			break;
		case TypeCode.Decimal:
		{
			MethodInfo meth = typeCode2 switch
			{
				TypeCode.Byte => CachedReflectionInfo.Decimal_op_Implicit_Byte, 
				TypeCode.SByte => CachedReflectionInfo.Decimal_op_Implicit_SByte, 
				TypeCode.Int16 => CachedReflectionInfo.Decimal_op_Implicit_Int16, 
				TypeCode.UInt16 => CachedReflectionInfo.Decimal_op_Implicit_UInt16, 
				TypeCode.Int32 => CachedReflectionInfo.Decimal_op_Implicit_Int32, 
				TypeCode.UInt32 => CachedReflectionInfo.Decimal_op_Implicit_UInt32, 
				TypeCode.Int64 => CachedReflectionInfo.Decimal_op_Implicit_Int64, 
				TypeCode.UInt64 => CachedReflectionInfo.Decimal_op_Implicit_UInt64, 
				TypeCode.Char => CachedReflectionInfo.Decimal_op_Implicit_Char, 
				_ => throw ContractUtils.Unreachable, 
			};
			il.Emit(OpCodes.Call, meth);
			return;
		}
		case TypeCode.SByte:
			if (isChecked)
			{
				opcode = (flag ? OpCodes.Conv_Ovf_I1_Un : OpCodes.Conv_Ovf_I1);
				break;
			}
			if (typeCode2 == TypeCode.Byte)
			{
				return;
			}
			opcode = OpCodes.Conv_I1;
			break;
		case TypeCode.Byte:
			if (isChecked)
			{
				opcode = (flag ? OpCodes.Conv_Ovf_U1_Un : OpCodes.Conv_Ovf_U1);
				break;
			}
			if (typeCode2 == TypeCode.SByte)
			{
				return;
			}
			opcode = OpCodes.Conv_U1;
			break;
		case TypeCode.Int16:
			switch (typeCode2)
			{
			case TypeCode.SByte:
			case TypeCode.Byte:
				return;
			case TypeCode.Char:
			case TypeCode.UInt16:
				if (!isChecked)
				{
					return;
				}
				break;
			}
			opcode = ((!isChecked) ? OpCodes.Conv_I2 : (flag ? OpCodes.Conv_Ovf_I2_Un : OpCodes.Conv_Ovf_I2));
			break;
		case TypeCode.Char:
		case TypeCode.UInt16:
			switch (typeCode2)
			{
			case TypeCode.Char:
			case TypeCode.Byte:
			case TypeCode.UInt16:
				return;
			case TypeCode.SByte:
			case TypeCode.Int16:
				if (!isChecked)
				{
					return;
				}
				break;
			}
			opcode = ((!isChecked) ? OpCodes.Conv_U2 : (flag ? OpCodes.Conv_Ovf_U2_Un : OpCodes.Conv_Ovf_U2));
			break;
		case TypeCode.Int32:
			switch (typeCode2)
			{
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
				return;
			case TypeCode.UInt32:
				if (!isChecked)
				{
					return;
				}
				break;
			}
			opcode = ((!isChecked) ? OpCodes.Conv_I4 : (flag ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_I4));
			break;
		case TypeCode.UInt32:
			switch (typeCode2)
			{
			case TypeCode.Char:
			case TypeCode.Byte:
			case TypeCode.UInt16:
				return;
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
				if (!isChecked)
				{
					return;
				}
				break;
			}
			opcode = ((!isChecked) ? OpCodes.Conv_U4 : (flag ? OpCodes.Conv_Ovf_U4_Un : OpCodes.Conv_Ovf_U4));
			break;
		case TypeCode.Int64:
			if (!isChecked && typeCode2 == TypeCode.UInt64)
			{
				return;
			}
			opcode = ((!isChecked) ? (flag ? OpCodes.Conv_U8 : OpCodes.Conv_I8) : (flag ? OpCodes.Conv_Ovf_I8_Un : OpCodes.Conv_Ovf_I8));
			break;
		case TypeCode.UInt64:
			if (!isChecked && typeCode2 == TypeCode.Int64)
			{
				return;
			}
			opcode = ((!isChecked) ? ((flag || typeCode2.IsFloatingPoint()) ? OpCodes.Conv_U8 : OpCodes.Conv_I8) : ((flag || typeCode2.IsFloatingPoint()) ? OpCodes.Conv_Ovf_U8_Un : OpCodes.Conv_Ovf_U8));
			break;
		default:
			throw ContractUtils.Unreachable;
		}
		il.Emit(opcode);
	}

	private static void EmitNullableToNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		LocalBuilder local = locals.GetLocal(typeFrom);
		il.Emit(OpCodes.Stloc, local);
		il.Emit(OpCodes.Ldloca, local);
		il.EmitHasValue(typeFrom);
		Label label = il.DefineLabel();
		il.Emit(OpCodes.Brfalse_S, label);
		il.Emit(OpCodes.Ldloca, local);
		locals.FreeLocal(local);
		il.EmitGetValueOrDefault(typeFrom);
		Type nonNullableType = typeFrom.GetNonNullableType();
		Type nonNullableType2 = typeTo.GetNonNullableType();
		il.EmitConvertToType(nonNullableType, nonNullableType2, isChecked, locals);
		ConstructorInfo nullableConstructor = TypeUtils.GetNullableConstructor(typeTo);
		il.Emit(OpCodes.Newobj, nullableConstructor);
		Label label2 = il.DefineLabel();
		il.Emit(OpCodes.Br_S, label2);
		il.MarkLabel(label);
		LocalBuilder local2 = locals.GetLocal(typeTo);
		il.Emit(OpCodes.Ldloca, local2);
		il.Emit(OpCodes.Initobj, typeTo);
		il.Emit(OpCodes.Ldloc, local2);
		locals.FreeLocal(local2);
		il.MarkLabel(label2);
	}

	private static void EmitNonNullableToNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		Type nonNullableType = typeTo.GetNonNullableType();
		il.EmitConvertToType(typeFrom, nonNullableType, isChecked, locals);
		ConstructorInfo nullableConstructor = TypeUtils.GetNullableConstructor(typeTo);
		il.Emit(OpCodes.Newobj, nullableConstructor);
	}

	private static void EmitNullableToNonNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		if (typeTo.IsValueType)
		{
			il.EmitNullableToNonNullableStructConversion(typeFrom, typeTo, isChecked, locals);
		}
		else
		{
			il.EmitNullableToReferenceConversion(typeFrom);
		}
	}

	private static void EmitNullableToNonNullableStructConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		LocalBuilder local = locals.GetLocal(typeFrom);
		il.Emit(OpCodes.Stloc, local);
		il.Emit(OpCodes.Ldloca, local);
		locals.FreeLocal(local);
		il.EmitGetValue(typeFrom);
		Type nonNullableType = typeFrom.GetNonNullableType();
		il.EmitConvertToType(nonNullableType, typeTo, isChecked, locals);
	}

	private static void EmitNullableToReferenceConversion(this ILGenerator il, Type typeFrom)
	{
		il.Emit(OpCodes.Box, typeFrom);
	}

	private static void EmitNullableConversion(this ILGenerator il, Type typeFrom, Type typeTo, bool isChecked, ILocalCache locals)
	{
		bool flag = typeFrom.IsNullableType();
		bool flag2 = typeTo.IsNullableType();
		if (flag && flag2)
		{
			il.EmitNullableToNullableConversion(typeFrom, typeTo, isChecked, locals);
		}
		else if (flag)
		{
			il.EmitNullableToNonNullableConversion(typeFrom, typeTo, isChecked, locals);
		}
		else
		{
			il.EmitNonNullableToNullableConversion(typeFrom, typeTo, isChecked, locals);
		}
	}

	internal static void EmitHasValue(this ILGenerator il, Type nullableType)
	{
		MethodInfo meth = (MethodInfo)nullableType.GetMemberWithSameMetadataDefinitionAs(s_nullableHasValueGetter);
		il.Emit(OpCodes.Call, meth);
	}

	internal static void EmitGetValue(this ILGenerator il, Type nullableType)
	{
		MethodInfo meth = (MethodInfo)nullableType.GetMemberWithSameMetadataDefinitionAs(s_nullableValueGetter);
		il.Emit(OpCodes.Call, meth);
	}

	internal static void EmitGetValueOrDefault(this ILGenerator il, Type nullableType)
	{
		MethodInfo meth = (MethodInfo)nullableType.GetMemberWithSameMetadataDefinitionAs(s_nullableGetValueOrDefault);
		il.Emit(OpCodes.Call, meth);
	}

	internal static void EmitArray(this ILGenerator il, Type elementType, int count)
	{
		il.EmitPrimitive(count);
		il.Emit(OpCodes.Newarr, elementType);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The Array ctor is dynamically constructed and is not included in IL. It is not subject to trimming.")]
	internal static void EmitArray(this ILGenerator il, Type arrayType)
	{
		if (arrayType.IsSZArray)
		{
			il.Emit(OpCodes.Newarr, arrayType.GetElementType());
			return;
		}
		Type[] array = new Type[arrayType.GetArrayRank()];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = typeof(int);
		}
		ConstructorInfo constructor = arrayType.GetConstructor(array);
		il.EmitNew(constructor);
	}

	private static void EmitDecimal(this ILGenerator il, decimal value)
	{
		Span<int> destination = stackalloc int[4];
		decimal.GetBits(value, destination);
		int num = (destination[3] & 0x7FFFFFFF) >> 16;
		if (num == 0)
		{
			if (-2147483648m <= value)
			{
				if (value <= 2147483647m)
				{
					int num2 = decimal.ToInt32(value);
					switch (num2)
					{
					case -1:
						il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.Decimal_MinusOne);
						break;
					case 0:
						il.EmitDefault(typeof(decimal), null);
						break;
					case 1:
						il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.Decimal_One);
						break;
					default:
						il.EmitPrimitive(num2);
						il.EmitNew(CachedReflectionInfo.Decimal_Ctor_Int32);
						break;
					}
					return;
				}
				if (value <= 4294967295m)
				{
					il.EmitPrimitive(decimal.ToUInt32(value));
					il.EmitNew(CachedReflectionInfo.Decimal_Ctor_UInt32);
					return;
				}
			}
			if (-9223372036854775808m <= value)
			{
				if (value <= 9223372036854775807m)
				{
					il.EmitPrimitive(decimal.ToInt64(value));
					il.EmitNew(CachedReflectionInfo.Decimal_Ctor_Int64);
					return;
				}
				if (value <= 18446744073709551615m)
				{
					il.EmitPrimitive(decimal.ToUInt64(value));
					il.EmitNew(CachedReflectionInfo.Decimal_Ctor_UInt64);
					return;
				}
				if (value == decimal.MaxValue)
				{
					il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.Decimal_MaxValue);
					return;
				}
			}
			else if (value == decimal.MinValue)
			{
				il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.Decimal_MinValue);
				return;
			}
		}
		il.EmitPrimitive(destination[0]);
		il.EmitPrimitive(destination[1]);
		il.EmitPrimitive(destination[2]);
		il.EmitPrimitive((destination[3] & 0x80000000u) != 0);
		il.EmitPrimitive((byte)num);
		il.EmitNew(CachedReflectionInfo.Decimal_Ctor_Int32_Int32_Int32_Bool_Byte);
	}

	internal static void EmitDefault(this ILGenerator il, Type type, ILocalCache locals)
	{
		switch (type.GetTypeCode())
		{
		case TypeCode.DateTime:
			il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.DateTime_MinValue);
			break;
		case TypeCode.Object:
			if (type.IsValueType)
			{
				LocalBuilder local = locals.GetLocal(type);
				il.Emit(OpCodes.Ldloca, local);
				il.Emit(OpCodes.Initobj, type);
				il.Emit(OpCodes.Ldloc, local);
				locals.FreeLocal(local);
				break;
			}
			goto case TypeCode.Empty;
		case TypeCode.Empty:
		case TypeCode.DBNull:
		case TypeCode.String:
			il.Emit(OpCodes.Ldnull);
			break;
		case TypeCode.Boolean:
		case TypeCode.Char:
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
		case TypeCode.Int32:
		case TypeCode.UInt32:
			il.Emit(OpCodes.Ldc_I4_0);
			break;
		case TypeCode.Int64:
		case TypeCode.UInt64:
			il.Emit(OpCodes.Ldc_I4_0);
			il.Emit(OpCodes.Conv_I8);
			break;
		case TypeCode.Single:
			il.Emit(OpCodes.Ldc_R4, 0f);
			break;
		case TypeCode.Double:
			il.Emit(OpCodes.Ldc_R8, 0.0);
			break;
		case TypeCode.Decimal:
			il.Emit(OpCodes.Ldsfld, CachedReflectionInfo.Decimal_Zero);
			break;
		default:
			throw ContractUtils.Unreachable;
		}
	}
}
