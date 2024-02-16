using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal static class CachedReflectionInfo
{
	private static ConstructorInfo s_Nullable_Boolean_Ctor;

	private static ConstructorInfo s_Decimal_Ctor_Int32;

	private static ConstructorInfo s_Decimal_Ctor_UInt32;

	private static ConstructorInfo s_Decimal_Ctor_Int64;

	private static ConstructorInfo s_Decimal_Ctor_UInt64;

	private static ConstructorInfo s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte;

	private static FieldInfo s_Decimal_One;

	private static FieldInfo s_Decimal_MinusOne;

	private static FieldInfo s_Decimal_MinValue;

	private static FieldInfo s_Decimal_MaxValue;

	private static FieldInfo s_Decimal_Zero;

	private static FieldInfo s_DateTime_MinValue;

	private static MethodInfo s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle;

	private static MethodInfo s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle;

	private static MethodInfo s_MethodInfo_CreateDelegate_Type_Object;

	private static MethodInfo s_String_op_Equality_String_String;

	private static MethodInfo s_String_Equals_String_String;

	private static MethodInfo s_DictionaryOfStringInt32_Add_String_Int32;

	private static ConstructorInfo s_DictionaryOfStringInt32_Ctor_Int32;

	private static MethodInfo s_Type_GetTypeFromHandle;

	private static MethodInfo s_Object_GetType;

	private static MethodInfo s_Decimal_op_Implicit_Byte;

	private static MethodInfo s_Decimal_op_Implicit_SByte;

	private static MethodInfo s_Decimal_op_Implicit_Int16;

	private static MethodInfo s_Decimal_op_Implicit_UInt16;

	private static MethodInfo s_Decimal_op_Implicit_Int32;

	private static MethodInfo s_Decimal_op_Implicit_UInt32;

	private static MethodInfo s_Decimal_op_Implicit_Int64;

	private static MethodInfo s_Decimal_op_Implicit_UInt64;

	private static MethodInfo s_Decimal_op_Implicit_Char;

	private static MethodInfo s_Math_Pow_Double_Double;

	private static ConstructorInfo s_Closure_ObjectArray_ObjectArray;

	private static FieldInfo s_Closure_Constants;

	private static FieldInfo s_Closure_Locals;

	private static MethodInfo s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array;

	private static MethodInfo s_RuntimeOps_CreateRuntimeVariables;

	private static MethodInfo s_RuntimeOps_MergeRuntimeVariables;

	private static MethodInfo s_RuntimeOps_Quote;

	private static MethodInfo s_String_Format_String_ObjectArray;

	private static ConstructorInfo s_InvalidCastException_Ctor_String;

	private static MethodInfo s_CallSiteOps_SetNotMatched;

	private static MethodInfo s_CallSiteOps_CreateMatchmaker;

	private static MethodInfo s_CallSiteOps_GetMatch;

	private static MethodInfo s_CallSiteOps_ClearMatch;

	private static MethodInfo s_CallSiteOps_UpdateRules;

	private static MethodInfo s_CallSiteOps_GetRules;

	private static MethodInfo s_CallSiteOps_GetRuleCache;

	private static MethodInfo s_CallSiteOps_GetCachedRules;

	private static MethodInfo s_CallSiteOps_AddRule;

	private static MethodInfo s_CallSiteOps_MoveRule;

	private static MethodInfo s_CallSiteOps_Bind;

	private static MethodInfo s_DynamicObject_TryGetMember;

	private static MethodInfo s_DynamicObject_TrySetMember;

	private static MethodInfo s_DynamicObject_TryDeleteMember;

	private static MethodInfo s_DynamicObject_TryGetIndex;

	private static MethodInfo s_DynamicObject_TrySetIndex;

	private static MethodInfo s_DynamicObject_TryDeleteIndex;

	private static MethodInfo s_DynamicObject_TryConvert;

	private static MethodInfo s_DynamicObject_TryInvoke;

	private static MethodInfo s_DynamicObject_TryInvokeMember;

	private static MethodInfo s_DynamicObject_TryBinaryOperation;

	private static MethodInfo s_DynamicObject_TryUnaryOperation;

	private static MethodInfo s_DynamicObject_TryCreateInstance;

	public static ConstructorInfo Nullable_Boolean_Ctor => s_Nullable_Boolean_Ctor ?? (s_Nullable_Boolean_Ctor = typeof(bool?).GetConstructor(new Type[1] { typeof(bool) }));

	public static ConstructorInfo Decimal_Ctor_Int32 => s_Decimal_Ctor_Int32 ?? (s_Decimal_Ctor_Int32 = typeof(decimal).GetConstructor(new Type[1] { typeof(int) }));

	public static ConstructorInfo Decimal_Ctor_UInt32 => s_Decimal_Ctor_UInt32 ?? (s_Decimal_Ctor_UInt32 = typeof(decimal).GetConstructor(new Type[1] { typeof(uint) }));

	public static ConstructorInfo Decimal_Ctor_Int64 => s_Decimal_Ctor_Int64 ?? (s_Decimal_Ctor_Int64 = typeof(decimal).GetConstructor(new Type[1] { typeof(long) }));

	public static ConstructorInfo Decimal_Ctor_UInt64 => s_Decimal_Ctor_UInt64 ?? (s_Decimal_Ctor_UInt64 = typeof(decimal).GetConstructor(new Type[1] { typeof(ulong) }));

	public static ConstructorInfo Decimal_Ctor_Int32_Int32_Int32_Bool_Byte => s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte ?? (s_Decimal_Ctor_Int32_Int32_Int32_Bool_Byte = typeof(decimal).GetConstructor(new Type[5]
	{
		typeof(int),
		typeof(int),
		typeof(int),
		typeof(bool),
		typeof(byte)
	}));

	public static FieldInfo Decimal_One => s_Decimal_One ?? (s_Decimal_One = typeof(decimal).GetField("One"));

	public static FieldInfo Decimal_MinusOne => s_Decimal_MinusOne ?? (s_Decimal_MinusOne = typeof(decimal).GetField("MinusOne"));

	public static FieldInfo Decimal_MinValue => s_Decimal_MinValue ?? (s_Decimal_MinValue = typeof(decimal).GetField("MinValue"));

	public static FieldInfo Decimal_MaxValue => s_Decimal_MaxValue ?? (s_Decimal_MaxValue = typeof(decimal).GetField("MaxValue"));

	public static FieldInfo Decimal_Zero => s_Decimal_Zero ?? (s_Decimal_Zero = typeof(decimal).GetField("Zero"));

	public static FieldInfo DateTime_MinValue => s_DateTime_MinValue ?? (s_DateTime_MinValue = typeof(DateTime).GetField("MinValue"));

	public static MethodInfo MethodBase_GetMethodFromHandle_RuntimeMethodHandle => s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle ?? (s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[1] { typeof(RuntimeMethodHandle) }));

	public static MethodInfo MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle => s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle ?? (s_MethodBase_GetMethodFromHandle_RuntimeMethodHandle_RuntimeTypeHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new Type[2]
	{
		typeof(RuntimeMethodHandle),
		typeof(RuntimeTypeHandle)
	}));

	public static MethodInfo MethodInfo_CreateDelegate_Type_Object => s_MethodInfo_CreateDelegate_Type_Object ?? (s_MethodInfo_CreateDelegate_Type_Object = typeof(MethodInfo).GetMethod("CreateDelegate", new Type[2]
	{
		typeof(Type),
		typeof(object)
	}));

	public static MethodInfo String_op_Equality_String_String => s_String_op_Equality_String_String ?? (s_String_op_Equality_String_String = typeof(string).GetMethod("op_Equality", new Type[2]
	{
		typeof(string),
		typeof(string)
	}));

	public static MethodInfo String_Equals_String_String => s_String_Equals_String_String ?? (s_String_Equals_String_String = typeof(string).GetMethod("Equals", new Type[2]
	{
		typeof(string),
		typeof(string)
	}));

	public static MethodInfo DictionaryOfStringInt32_Add_String_Int32 => s_DictionaryOfStringInt32_Add_String_Int32 ?? (s_DictionaryOfStringInt32_Add_String_Int32 = typeof(Dictionary<string, int>).GetMethod("Add", new Type[2]
	{
		typeof(string),
		typeof(int)
	}));

	public static ConstructorInfo DictionaryOfStringInt32_Ctor_Int32 => s_DictionaryOfStringInt32_Ctor_Int32 ?? (s_DictionaryOfStringInt32_Ctor_Int32 = typeof(Dictionary<string, int>).GetConstructor(new Type[1] { typeof(int) }));

	public static MethodInfo Type_GetTypeFromHandle => s_Type_GetTypeFromHandle ?? (s_Type_GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle"));

	public static MethodInfo Object_GetType => s_Object_GetType ?? (s_Object_GetType = typeof(object).GetMethod("GetType"));

	public static MethodInfo Decimal_op_Implicit_Byte => s_Decimal_op_Implicit_Byte ?? (s_Decimal_op_Implicit_Byte = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(byte) }));

	public static MethodInfo Decimal_op_Implicit_SByte => s_Decimal_op_Implicit_SByte ?? (s_Decimal_op_Implicit_SByte = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(sbyte) }));

	public static MethodInfo Decimal_op_Implicit_Int16 => s_Decimal_op_Implicit_Int16 ?? (s_Decimal_op_Implicit_Int16 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(short) }));

	public static MethodInfo Decimal_op_Implicit_UInt16 => s_Decimal_op_Implicit_UInt16 ?? (s_Decimal_op_Implicit_UInt16 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(ushort) }));

	public static MethodInfo Decimal_op_Implicit_Int32 => s_Decimal_op_Implicit_Int32 ?? (s_Decimal_op_Implicit_Int32 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(int) }));

	public static MethodInfo Decimal_op_Implicit_UInt32 => s_Decimal_op_Implicit_UInt32 ?? (s_Decimal_op_Implicit_UInt32 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(uint) }));

	public static MethodInfo Decimal_op_Implicit_Int64 => s_Decimal_op_Implicit_Int64 ?? (s_Decimal_op_Implicit_Int64 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(long) }));

	public static MethodInfo Decimal_op_Implicit_UInt64 => s_Decimal_op_Implicit_UInt64 ?? (s_Decimal_op_Implicit_UInt64 = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(ulong) }));

	public static MethodInfo Decimal_op_Implicit_Char => s_Decimal_op_Implicit_Char ?? (s_Decimal_op_Implicit_Char = typeof(decimal).GetMethod("op_Implicit", new Type[1] { typeof(char) }));

	public static MethodInfo Math_Pow_Double_Double => s_Math_Pow_Double_Double ?? (s_Math_Pow_Double_Double = typeof(Math).GetMethod("Pow", new Type[2]
	{
		typeof(double),
		typeof(double)
	}));

	public static ConstructorInfo Closure_ObjectArray_ObjectArray => s_Closure_ObjectArray_ObjectArray ?? (s_Closure_ObjectArray_ObjectArray = typeof(Closure).GetConstructor(new Type[2]
	{
		typeof(object[]),
		typeof(object[])
	}));

	public static FieldInfo Closure_Constants => s_Closure_Constants ?? (s_Closure_Constants = typeof(Closure).GetField("Constants"));

	public static FieldInfo Closure_Locals => s_Closure_Locals ?? (s_Closure_Locals = typeof(Closure).GetField("Locals"));

	public static MethodInfo RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array => s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array ?? (s_RuntimeOps_CreateRuntimeVariables_ObjectArray_Int64Array = typeof(RuntimeOps).GetMethod("CreateRuntimeVariables", new Type[2]
	{
		typeof(object[]),
		typeof(long[])
	}));

	public static MethodInfo RuntimeOps_CreateRuntimeVariables => s_RuntimeOps_CreateRuntimeVariables ?? (s_RuntimeOps_CreateRuntimeVariables = typeof(RuntimeOps).GetMethod("CreateRuntimeVariables", Type.EmptyTypes));

	public static MethodInfo RuntimeOps_MergeRuntimeVariables => s_RuntimeOps_MergeRuntimeVariables ?? (s_RuntimeOps_MergeRuntimeVariables = typeof(RuntimeOps).GetMethod("MergeRuntimeVariables"));

	public static MethodInfo RuntimeOps_Quote => s_RuntimeOps_Quote ?? (s_RuntimeOps_Quote = typeof(RuntimeOps).GetMethod("Quote"));

	public static MethodInfo String_Format_String_ObjectArray => s_String_Format_String_ObjectArray ?? (s_String_Format_String_ObjectArray = typeof(string).GetMethod("Format", new Type[2]
	{
		typeof(string),
		typeof(object[])
	}));

	public static ConstructorInfo InvalidCastException_Ctor_String => s_InvalidCastException_Ctor_String ?? (s_InvalidCastException_Ctor_String = typeof(InvalidCastException).GetConstructor(new Type[1] { typeof(string) }));

	public static MethodInfo CallSiteOps_SetNotMatched => s_CallSiteOps_SetNotMatched ?? (s_CallSiteOps_SetNotMatched = typeof(CallSiteOps).GetMethod("SetNotMatched"));

	public static MethodInfo CallSiteOps_CreateMatchmaker => s_CallSiteOps_CreateMatchmaker ?? (s_CallSiteOps_CreateMatchmaker = typeof(CallSiteOps).GetMethod("CreateMatchmaker"));

	public static MethodInfo CallSiteOps_GetMatch => s_CallSiteOps_GetMatch ?? (s_CallSiteOps_GetMatch = typeof(CallSiteOps).GetMethod("GetMatch"));

	public static MethodInfo CallSiteOps_ClearMatch => s_CallSiteOps_ClearMatch ?? (s_CallSiteOps_ClearMatch = typeof(CallSiteOps).GetMethod("ClearMatch"));

	public static MethodInfo CallSiteOps_UpdateRules => s_CallSiteOps_UpdateRules ?? (s_CallSiteOps_UpdateRules = typeof(CallSiteOps).GetMethod("UpdateRules"));

	public static MethodInfo CallSiteOps_GetRules => s_CallSiteOps_GetRules ?? (s_CallSiteOps_GetRules = typeof(CallSiteOps).GetMethod("GetRules"));

	public static MethodInfo CallSiteOps_GetRuleCache => s_CallSiteOps_GetRuleCache ?? (s_CallSiteOps_GetRuleCache = typeof(CallSiteOps).GetMethod("GetRuleCache"));

	public static MethodInfo CallSiteOps_GetCachedRules => s_CallSiteOps_GetCachedRules ?? (s_CallSiteOps_GetCachedRules = typeof(CallSiteOps).GetMethod("GetCachedRules"));

	public static MethodInfo CallSiteOps_AddRule => s_CallSiteOps_AddRule ?? (s_CallSiteOps_AddRule = typeof(CallSiteOps).GetMethod("AddRule"));

	public static MethodInfo CallSiteOps_MoveRule => s_CallSiteOps_MoveRule ?? (s_CallSiteOps_MoveRule = typeof(CallSiteOps).GetMethod("MoveRule"));

	public static MethodInfo CallSiteOps_Bind => s_CallSiteOps_Bind ?? (s_CallSiteOps_Bind = typeof(CallSiteOps).GetMethod("Bind"));

	public static MethodInfo DynamicObject_TryGetMember => s_DynamicObject_TryGetMember ?? (s_DynamicObject_TryGetMember = typeof(DynamicObject).GetMethod("TryGetMember"));

	public static MethodInfo DynamicObject_TrySetMember => s_DynamicObject_TrySetMember ?? (s_DynamicObject_TrySetMember = typeof(DynamicObject).GetMethod("TrySetMember"));

	public static MethodInfo DynamicObject_TryDeleteMember => s_DynamicObject_TryDeleteMember ?? (s_DynamicObject_TryDeleteMember = typeof(DynamicObject).GetMethod("TryDeleteMember"));

	public static MethodInfo DynamicObject_TryGetIndex => s_DynamicObject_TryGetIndex ?? (s_DynamicObject_TryGetIndex = typeof(DynamicObject).GetMethod("TryGetIndex"));

	public static MethodInfo DynamicObject_TrySetIndex => s_DynamicObject_TrySetIndex ?? (s_DynamicObject_TrySetIndex = typeof(DynamicObject).GetMethod("TrySetIndex"));

	public static MethodInfo DynamicObject_TryDeleteIndex => s_DynamicObject_TryDeleteIndex ?? (s_DynamicObject_TryDeleteIndex = typeof(DynamicObject).GetMethod("TryDeleteIndex"));

	public static MethodInfo DynamicObject_TryConvert => s_DynamicObject_TryConvert ?? (s_DynamicObject_TryConvert = typeof(DynamicObject).GetMethod("TryConvert"));

	public static MethodInfo DynamicObject_TryInvoke => s_DynamicObject_TryInvoke ?? (s_DynamicObject_TryInvoke = typeof(DynamicObject).GetMethod("TryInvoke"));

	public static MethodInfo DynamicObject_TryInvokeMember => s_DynamicObject_TryInvokeMember ?? (s_DynamicObject_TryInvokeMember = typeof(DynamicObject).GetMethod("TryInvokeMember"));

	public static MethodInfo DynamicObject_TryBinaryOperation => s_DynamicObject_TryBinaryOperation ?? (s_DynamicObject_TryBinaryOperation = typeof(DynamicObject).GetMethod("TryBinaryOperation"));

	public static MethodInfo DynamicObject_TryUnaryOperation => s_DynamicObject_TryUnaryOperation ?? (s_DynamicObject_TryUnaryOperation = typeof(DynamicObject).GetMethod("TryUnaryOperation"));

	public static MethodInfo DynamicObject_TryCreateInstance => s_DynamicObject_TryCreateInstance ?? (s_DynamicObject_TryCreateInstance = typeof(DynamicObject).GetMethod("TryCreateInstance"));
}
