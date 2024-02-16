using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Xml.Serialization;

internal sealed class CodeGenerator
{
	internal sealed class WhileState
	{
		public Label StartLabel;

		public Label CondLabel;

		public Label EndLabel;

		public WhileState(CodeGenerator ilg)
		{
			StartLabel = ilg.DefineLabel();
			CondLabel = ilg.DefineLabel();
			EndLabel = ilg.DefineLabel();
		}
	}

	private readonly TypeBuilder _typeBuilder;

	private MethodBuilder _methodBuilder;

	private ILGenerator _ilGen;

	private Dictionary<string, ArgBuilder> _argList;

	private LocalScope _currentScope;

	private Dictionary<(Type, string), Queue<LocalBuilder>> _freeLocals;

	private Stack<object> _blockStack;

	private Label _methodEndLabel;

	internal LocalBuilder retLocal;

	internal Label retLabel;

	private readonly Dictionary<Type, LocalBuilder> _tmpLocals = new Dictionary<Type, LocalBuilder>();

	private static readonly OpCode[] s_branchCodes = new OpCode[6]
	{
		OpCodes.Bge,
		OpCodes.Bne_Un,
		OpCodes.Bgt,
		OpCodes.Ble,
		OpCodes.Beq,
		OpCodes.Blt
	};

	private readonly Stack<Label> _leaveLabels = new Stack<Label>();

	private static readonly OpCode[] s_ldindOpCodes = new OpCode[19]
	{
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Ldind_I1,
		OpCodes.Ldind_I2,
		OpCodes.Ldind_I1,
		OpCodes.Ldind_U1,
		OpCodes.Ldind_I2,
		OpCodes.Ldind_U2,
		OpCodes.Ldind_I4,
		OpCodes.Ldind_U4,
		OpCodes.Ldind_I8,
		OpCodes.Ldind_I8,
		OpCodes.Ldind_R4,
		OpCodes.Ldind_R8,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Ldind_Ref
	};

	private static readonly OpCode[] s_ldelemOpCodes = new OpCode[19]
	{
		OpCodes.Nop,
		OpCodes.Ldelem_Ref,
		OpCodes.Ldelem_Ref,
		OpCodes.Ldelem_I1,
		OpCodes.Ldelem_I2,
		OpCodes.Ldelem_I1,
		OpCodes.Ldelem_U1,
		OpCodes.Ldelem_I2,
		OpCodes.Ldelem_U2,
		OpCodes.Ldelem_I4,
		OpCodes.Ldelem_U4,
		OpCodes.Ldelem_I8,
		OpCodes.Ldelem_I8,
		OpCodes.Ldelem_R4,
		OpCodes.Ldelem_R8,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Ldelem_Ref
	};

	private static readonly OpCode[] s_stelemOpCodes = new OpCode[19]
	{
		OpCodes.Nop,
		OpCodes.Stelem_Ref,
		OpCodes.Stelem_Ref,
		OpCodes.Stelem_I1,
		OpCodes.Stelem_I2,
		OpCodes.Stelem_I1,
		OpCodes.Stelem_I1,
		OpCodes.Stelem_I2,
		OpCodes.Stelem_I2,
		OpCodes.Stelem_I4,
		OpCodes.Stelem_I4,
		OpCodes.Stelem_I8,
		OpCodes.Stelem_I8,
		OpCodes.Stelem_R4,
		OpCodes.Stelem_R8,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Stelem_Ref
	};

	private static readonly OpCode[] s_convOpCodes = new OpCode[19]
	{
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Conv_I1,
		OpCodes.Conv_I2,
		OpCodes.Conv_I1,
		OpCodes.Conv_U1,
		OpCodes.Conv_I2,
		OpCodes.Conv_U2,
		OpCodes.Conv_I4,
		OpCodes.Conv_U4,
		OpCodes.Conv_I8,
		OpCodes.Conv_U8,
		OpCodes.Conv_R4,
		OpCodes.Conv_R8,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop,
		OpCodes.Nop
	};

	private int _initElseIfStack = -1;

	private IfState _elseIfState;

	private int _initIfStack = -1;

	private Stack<WhileState> _whileStack;

	internal MethodBuilder MethodBuilder => _methodBuilder;

	internal LocalBuilder ReturnLocal
	{
		get
		{
			if (retLocal == null)
			{
				retLocal = DeclareLocal(_methodBuilder.ReturnType, "_ret");
			}
			return retLocal;
		}
	}

	internal Label ReturnLabel => retLabel;

	internal CodeGenerator(TypeBuilder typeBuilder)
	{
		_typeBuilder = typeBuilder;
	}

	internal static bool IsNullableGenericType(Type type)
	{
		return type.Name == "Nullable`1";
	}

	internal void BeginMethod(Type returnType, string methodName, Type[] argTypes, string[] argNames, MethodAttributes methodAttributes)
	{
		_methodBuilder = _typeBuilder.DefineMethod(methodName, methodAttributes, returnType, argTypes);
		_ilGen = _methodBuilder.GetILGenerator();
		InitILGeneration(argTypes, argNames, (_methodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static);
	}

	internal void BeginMethod(Type returnType, MethodBuilderInfo methodBuilderInfo, Type[] argTypes, string[] argNames, MethodAttributes methodAttributes)
	{
		_methodBuilder = methodBuilderInfo.MethodBuilder;
		_ilGen = _methodBuilder.GetILGenerator();
		InitILGeneration(argTypes, argNames, (_methodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static);
	}

	private void InitILGeneration(Type[] argTypes, string[] argNames, bool isStatic)
	{
		_methodEndLabel = _ilGen.DefineLabel();
		retLabel = _ilGen.DefineLabel();
		_blockStack = new Stack<object>();
		_whileStack = new Stack<WhileState>();
		_currentScope = new LocalScope();
		_freeLocals = new Dictionary<(Type, string), Queue<LocalBuilder>>();
		_argList = new Dictionary<string, ArgBuilder>();
		if (!isStatic)
		{
			_argList.Add("this", new ArgBuilder("this", 0, _typeBuilder.BaseType));
		}
		for (int i = 0; i < argTypes.Length; i++)
		{
			ArgBuilder argBuilder = new ArgBuilder(argNames[i], _argList.Count, argTypes[i]);
			_argList.Add(argBuilder.Name, argBuilder);
			_methodBuilder.DefineParameter(argBuilder.Index, ParameterAttributes.None, argBuilder.Name);
		}
	}

	internal MethodBuilder EndMethod()
	{
		MarkLabel(_methodEndLabel);
		Ret();
		MethodBuilder methodBuilder = null;
		methodBuilder = _methodBuilder;
		_methodBuilder = null;
		_ilGen = null;
		_freeLocals = null;
		_blockStack = null;
		_whileStack = null;
		_argList = null;
		_currentScope = null;
		retLocal = null;
		return methodBuilder;
	}

	internal ArgBuilder GetArg(string name)
	{
		return _argList[name];
	}

	internal LocalBuilder GetLocal(string name)
	{
		return _currentScope[name];
	}

	internal LocalBuilder GetTempLocal(Type type)
	{
		if (!_tmpLocals.TryGetValue(type, out var value))
		{
			value = DeclareLocal(type, "_tmp" + _tmpLocals.Count);
			_tmpLocals.Add(type, value);
		}
		return value;
	}

	internal Type GetVariableType(object var)
	{
		if (var is ArgBuilder)
		{
			return ((ArgBuilder)var).ArgType;
		}
		if (var is LocalBuilder)
		{
			return ((LocalBuilder)var).LocalType;
		}
		return var.GetType();
	}

	internal object GetVariable(string name)
	{
		if (TryGetVariable(name, out var variable))
		{
			return variable;
		}
		return null;
	}

	internal bool TryGetVariable(string name, [NotNullWhen(true)] out object variable)
	{
		if (_currentScope != null && _currentScope.TryGetValue(name, out var value))
		{
			variable = value;
			return true;
		}
		if (_argList != null && _argList.TryGetValue(name, out var value2))
		{
			variable = value2;
			return true;
		}
		if (int.TryParse(name, out var result))
		{
			variable = result;
			return true;
		}
		variable = null;
		return false;
	}

	internal void EnterScope()
	{
		LocalScope currentScope = new LocalScope(_currentScope);
		_currentScope = currentScope;
	}

	internal void ExitScope()
	{
		_currentScope.AddToFreeLocals(_freeLocals);
		_currentScope = _currentScope.parent;
	}

	private bool TryDequeueLocal(Type type, string name, [NotNullWhen(true)] out LocalBuilder local)
	{
		(Type, string) key = (type, name);
		if (_freeLocals.TryGetValue(key, out var value))
		{
			local = value.Dequeue();
			if (value.Count == 0)
			{
				_freeLocals.Remove(key);
			}
			return true;
		}
		local = null;
		return false;
	}

	internal LocalBuilder DeclareLocal(Type type, string name)
	{
		if (!TryDequeueLocal(type, name, out var local))
		{
			local = _ilGen.DeclareLocal(type, pinned: false);
		}
		_currentScope[name] = local;
		return local;
	}

	internal LocalBuilder DeclareOrGetLocal(Type type, string name)
	{
		if (!_currentScope.TryGetValue(name, out var value))
		{
			return DeclareLocal(type, name);
		}
		return value;
	}

	internal object For(LocalBuilder local, object start, object end)
	{
		ForState forState = new ForState(local, DefineLabel(), DefineLabel(), end);
		if (forState.Index != null)
		{
			Load(start);
			Stloc(forState.Index);
			Br(forState.TestLabel);
		}
		MarkLabel(forState.BeginLabel);
		_blockStack.Push(forState);
		return forState;
	}

	internal void EndFor()
	{
		object obj = _blockStack.Pop();
		ForState forState = obj as ForState;
		if (forState.Index != null)
		{
			Ldloc(forState.Index);
			Ldc(1);
			Add();
			Stloc(forState.Index);
			MarkLabel(forState.TestLabel);
			Ldloc(forState.Index);
			Load(forState.End);
			Type variableType = GetVariableType(forState.End);
			if (variableType.IsArray)
			{
				Ldlen();
			}
			else
			{
				MethodInfo method = typeof(ICollection).GetMethod("get_Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				Call(method);
			}
			Blt(forState.BeginLabel);
		}
		else
		{
			Br(forState.BeginLabel);
		}
	}

	internal void If()
	{
		InternalIf(negate: false);
	}

	internal void IfNot()
	{
		InternalIf(negate: true);
	}

	private OpCode GetBranchCode(Cmp cmp)
	{
		return s_branchCodes[(int)cmp];
	}

	internal void If(Cmp cmpOp)
	{
		IfState ifState = new IfState();
		ifState.EndIf = DefineLabel();
		ifState.ElseBegin = DefineLabel();
		_ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
		_blockStack.Push(ifState);
	}

	internal void If(object value1, Cmp cmpOp, object value2)
	{
		Load(value1);
		Load(value2);
		If(cmpOp);
	}

	internal void Else()
	{
		IfState ifState = PopIfState();
		Br(ifState.EndIf);
		MarkLabel(ifState.ElseBegin);
		ifState.ElseBegin = ifState.EndIf;
		_blockStack.Push(ifState);
	}

	internal void EndIf()
	{
		IfState ifState = PopIfState();
		if (!ifState.ElseBegin.Equals(ifState.EndIf))
		{
			MarkLabel(ifState.ElseBegin);
		}
		MarkLabel(ifState.EndIf);
	}

	internal void BeginExceptionBlock()
	{
		_leaveLabels.Push(DefineLabel());
		_ilGen.BeginExceptionBlock();
	}

	internal void BeginCatchBlock(Type exception)
	{
		_ilGen.BeginCatchBlock(exception);
	}

	internal void EndExceptionBlock()
	{
		_ilGen.EndExceptionBlock();
		_ilGen.MarkLabel(_leaveLabels.Pop());
	}

	internal void Leave()
	{
		_ilGen.Emit(OpCodes.Leave, _leaveLabels.Peek());
	}

	internal void Call(MethodInfo methodInfo)
	{
		if (methodInfo.IsVirtual && !methodInfo.DeclaringType.IsValueType)
		{
			_ilGen.Emit(OpCodes.Callvirt, methodInfo);
		}
		else
		{
			_ilGen.Emit(OpCodes.Call, methodInfo);
		}
	}

	internal void Call(ConstructorInfo ctor)
	{
		_ilGen.Emit(OpCodes.Call, ctor);
	}

	internal void New(ConstructorInfo constructorInfo)
	{
		_ilGen.Emit(OpCodes.Newobj, constructorInfo);
	}

	internal void InitObj(Type valueType)
	{
		_ilGen.Emit(OpCodes.Initobj, valueType);
	}

	internal void NewArray(Type elementType, object len)
	{
		Load(len);
		_ilGen.Emit(OpCodes.Newarr, elementType);
	}

	internal void LoadArrayElement(object obj, object arrayIndex)
	{
		Type elementType = GetVariableType(obj).GetElementType();
		Load(obj);
		Load(arrayIndex);
		if (IsStruct(elementType))
		{
			Ldelema(elementType);
			Ldobj(elementType);
		}
		else
		{
			Ldelem(elementType);
		}
	}

	internal void StoreArrayElement(object obj, object arrayIndex, object value)
	{
		Type variableType = GetVariableType(obj);
		if (variableType == typeof(Array))
		{
			Load(obj);
			Call(typeof(Array).GetMethod("SetValue", new Type[2]
			{
				typeof(object),
				typeof(int)
			}));
			return;
		}
		Type elementType = variableType.GetElementType();
		Load(obj);
		Load(arrayIndex);
		if (IsStruct(elementType))
		{
			Ldelema(elementType);
		}
		Load(value);
		ConvertValue(GetVariableType(value), elementType);
		if (IsStruct(elementType))
		{
			Stobj(elementType);
		}
		else
		{
			Stelem(elementType);
		}
	}

	private static bool IsStruct(Type objType)
	{
		if (objType.IsValueType)
		{
			return !objType.IsPrimitive;
		}
		return false;
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	internal Type LoadMember(object obj, MemberInfo memberInfo)
	{
		if (GetVariableType(obj).IsValueType)
		{
			LoadAddress(obj);
		}
		else
		{
			Load(obj);
		}
		return LoadMember(memberInfo);
	}

	[RequiresUnreferencedCode("GetProperty on PropertyInfo type's base type")]
	private static MethodInfo GetPropertyMethodFromBaseType(PropertyInfo propertyInfo, bool isGetter)
	{
		Type baseType = propertyInfo.DeclaringType.BaseType;
		string name = propertyInfo.Name;
		MethodInfo methodInfo = null;
		while (baseType != null)
		{
			PropertyInfo property = baseType.GetProperty(name);
			if (property != null)
			{
				methodInfo = ((!isGetter) ? property.SetMethod : property.GetMethod);
				if (methodInfo != null)
				{
					break;
				}
			}
			baseType = baseType.BaseType;
		}
		return methodInfo;
	}

	[RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
	internal Type LoadMember(MemberInfo memberInfo)
	{
		Type type = null;
		if (memberInfo is FieldInfo)
		{
			FieldInfo fieldInfo = (FieldInfo)memberInfo;
			type = fieldInfo.FieldType;
			if (fieldInfo.IsStatic)
			{
				_ilGen.Emit(OpCodes.Ldsfld, fieldInfo);
			}
			else
			{
				_ilGen.Emit(OpCodes.Ldfld, fieldInfo);
			}
		}
		else
		{
			PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
			type = propertyInfo.PropertyType;
			if (propertyInfo != null)
			{
				MethodInfo methodInfo = propertyInfo.GetMethod;
				if (methodInfo == null)
				{
					methodInfo = GetPropertyMethodFromBaseType(propertyInfo, isGetter: true);
				}
				Call(methodInfo);
			}
		}
		return type;
	}

	[RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
	internal Type LoadMemberAddress(MemberInfo memberInfo)
	{
		Type type = null;
		if (memberInfo is FieldInfo)
		{
			FieldInfo fieldInfo = (FieldInfo)memberInfo;
			type = fieldInfo.FieldType;
			if (fieldInfo.IsStatic)
			{
				_ilGen.Emit(OpCodes.Ldsflda, fieldInfo);
			}
			else
			{
				_ilGen.Emit(OpCodes.Ldflda, fieldInfo);
			}
		}
		else
		{
			PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
			type = propertyInfo.PropertyType;
			if (propertyInfo != null)
			{
				MethodInfo methodInfo = propertyInfo.GetMethod;
				if (methodInfo == null)
				{
					methodInfo = GetPropertyMethodFromBaseType(propertyInfo, isGetter: true);
				}
				Call(methodInfo);
				LocalBuilder tempLocal = GetTempLocal(type);
				Stloc(tempLocal);
				Ldloca(tempLocal);
			}
		}
		return type;
	}

	[RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
	internal void StoreMember(MemberInfo memberInfo)
	{
		if (memberInfo is FieldInfo)
		{
			FieldInfo fieldInfo = (FieldInfo)memberInfo;
			if (fieldInfo.IsStatic)
			{
				_ilGen.Emit(OpCodes.Stsfld, fieldInfo);
			}
			else
			{
				_ilGen.Emit(OpCodes.Stfld, fieldInfo);
			}
			return;
		}
		PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
		if (propertyInfo != null)
		{
			MethodInfo methodInfo = propertyInfo.SetMethod;
			if (methodInfo == null)
			{
				methodInfo = GetPropertyMethodFromBaseType(propertyInfo, isGetter: false);
			}
			Call(methodInfo);
		}
	}

	internal void Load(object obj)
	{
		if (obj == null)
		{
			_ilGen.Emit(OpCodes.Ldnull);
		}
		else if (obj is ArgBuilder)
		{
			Ldarg((ArgBuilder)obj);
		}
		else if (obj is LocalBuilder)
		{
			Ldloc((LocalBuilder)obj);
		}
		else
		{
			Ldc(obj);
		}
	}

	internal void LoadAddress(object obj)
	{
		if (obj is ArgBuilder)
		{
			LdargAddress((ArgBuilder)obj);
		}
		else if (obj is LocalBuilder)
		{
			LdlocAddress((LocalBuilder)obj);
		}
		else
		{
			Load(obj);
		}
	}

	internal void ConvertAddress(Type source, Type target)
	{
		InternalConvert(source, target, isAddress: true);
	}

	internal void ConvertValue(Type source, Type target)
	{
		InternalConvert(source, target, isAddress: false);
	}

	internal void Castclass(Type target)
	{
		_ilGen.Emit(OpCodes.Castclass, target);
	}

	internal void Box(Type type)
	{
		_ilGen.Emit(OpCodes.Box, type);
	}

	internal void Unbox(Type type)
	{
		_ilGen.Emit(OpCodes.Unbox, type);
	}

	private OpCode GetLdindOpCode(TypeCode typeCode)
	{
		return s_ldindOpCodes[(int)typeCode];
	}

	internal void Ldobj(Type type)
	{
		OpCode ldindOpCode = GetLdindOpCode(Type.GetTypeCode(type));
		if (!ldindOpCode.Equals(OpCodes.Nop))
		{
			_ilGen.Emit(ldindOpCode);
		}
		else
		{
			_ilGen.Emit(OpCodes.Ldobj, type);
		}
	}

	internal void Stobj(Type type)
	{
		_ilGen.Emit(OpCodes.Stobj, type);
	}

	internal void Ceq()
	{
		_ilGen.Emit(OpCodes.Ceq);
	}

	internal void Clt()
	{
		_ilGen.Emit(OpCodes.Clt);
	}

	internal void Cne()
	{
		Ceq();
		Ldc(0);
		Ceq();
	}

	internal void Ble(Label label)
	{
		_ilGen.Emit(OpCodes.Ble, label);
	}

	internal void Throw()
	{
		_ilGen.Emit(OpCodes.Throw);
	}

	internal void Ldtoken(Type t)
	{
		_ilGen.Emit(OpCodes.Ldtoken, t);
	}

	internal void Ldc(object o)
	{
		Type type = o.GetType();
		if (o is Type)
		{
			Ldtoken((Type)o);
			Call(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public, new Type[1] { typeof(RuntimeTypeHandle) }));
			return;
		}
		if (type.IsEnum)
		{
			Ldc(Convert.ChangeType(o, Enum.GetUnderlyingType(type), null));
			return;
		}
		switch (Type.GetTypeCode(type))
		{
		case TypeCode.Boolean:
			Ldc((bool)o);
			return;
		case TypeCode.Char:
			throw new NotSupportedException(System.SR.XmlInvalidCharSchemaPrimitive);
		case TypeCode.SByte:
		case TypeCode.Byte:
		case TypeCode.Int16:
		case TypeCode.UInt16:
			Ldc(Convert.ToInt32(o, CultureInfo.InvariantCulture));
			return;
		case TypeCode.Int32:
			Ldc((int)o);
			return;
		case TypeCode.UInt32:
			Ldc((int)(uint)o);
			return;
		case TypeCode.UInt64:
			Ldc((long)(ulong)o);
			return;
		case TypeCode.Int64:
			Ldc((long)o);
			return;
		case TypeCode.Single:
			Ldc((float)o);
			return;
		case TypeCode.Double:
			Ldc((double)o);
			return;
		case TypeCode.String:
			Ldstr((string)o);
			return;
		case TypeCode.Decimal:
		{
			ConstructorInfo constructor2 = typeof(decimal).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[5]
			{
				typeof(int),
				typeof(int),
				typeof(int),
				typeof(bool),
				typeof(byte)
			});
			int[] bits = decimal.GetBits((decimal)o);
			Ldc(bits[0]);
			Ldc(bits[1]);
			Ldc(bits[2]);
			Ldc((bits[3] & 0x80000000u) == 2147483648u);
			Ldc((byte)((bits[3] >> 16) & 0xFF));
			New(constructor2);
			return;
		}
		case TypeCode.DateTime:
		{
			ConstructorInfo constructor = typeof(DateTime).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(long) });
			Ldc(((DateTime)o).Ticks);
			New(constructor);
			return;
		}
		}
		if (type == typeof(TimeSpan))
		{
			ConstructorInfo constructor3 = typeof(TimeSpan).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(long) }, null);
			Ldc(((TimeSpan)o).Ticks);
			New(constructor3);
			return;
		}
		if (type == typeof(DateTimeOffset))
		{
			ConstructorInfo constructor4 = typeof(DateTimeOffset).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[2]
			{
				typeof(long),
				typeof(TimeSpan)
			}, null);
			Ldc(((DateTimeOffset)o).Ticks);
			Ldc(((DateTimeOffset)o).Offset);
			New(constructor4);
			return;
		}
		throw new NotSupportedException(System.SR.Format(System.SR.UnknownConstantType, type.AssemblyQualifiedName));
	}

	internal void Ldc(bool boolVar)
	{
		if (boolVar)
		{
			_ilGen.Emit(OpCodes.Ldc_I4_1);
		}
		else
		{
			_ilGen.Emit(OpCodes.Ldc_I4_0);
		}
	}

	internal void Ldc(int intVar)
	{
		_ilGen.Emit(OpCodes.Ldc_I4, intVar);
	}

	internal void Ldc(long l)
	{
		_ilGen.Emit(OpCodes.Ldc_I8, l);
	}

	internal void Ldc(float f)
	{
		_ilGen.Emit(OpCodes.Ldc_R4, f);
	}

	internal void Ldc(double d)
	{
		_ilGen.Emit(OpCodes.Ldc_R8, d);
	}

	internal void Ldstr(string strVar)
	{
		if (strVar == null)
		{
			_ilGen.Emit(OpCodes.Ldnull);
		}
		else
		{
			_ilGen.Emit(OpCodes.Ldstr, strVar);
		}
	}

	internal void LdlocAddress(LocalBuilder localBuilder)
	{
		if (localBuilder.LocalType.IsValueType)
		{
			Ldloca(localBuilder);
		}
		else
		{
			Ldloc(localBuilder);
		}
	}

	internal void Ldloc(LocalBuilder localBuilder)
	{
		_ilGen.Emit(OpCodes.Ldloc, localBuilder);
	}

	internal void Ldloc(string name)
	{
		LocalBuilder localBuilder = _currentScope[name];
		Ldloc(localBuilder);
	}

	internal void Stloc(Type type, string name)
	{
		LocalBuilder value = null;
		if (!_currentScope.TryGetValue(name, out value))
		{
			value = DeclareLocal(type, name);
		}
		Stloc(value);
	}

	internal void Stloc(LocalBuilder local)
	{
		_ilGen.Emit(OpCodes.Stloc, local);
	}

	internal void Ldloc(Type type, string name)
	{
		LocalBuilder localBuilder = _currentScope[name];
		Ldloc(localBuilder);
	}

	internal void Ldloca(LocalBuilder localBuilder)
	{
		_ilGen.Emit(OpCodes.Ldloca, localBuilder);
	}

	internal void LdargAddress(ArgBuilder argBuilder)
	{
		if (argBuilder.ArgType.IsValueType)
		{
			Ldarga(argBuilder);
		}
		else
		{
			Ldarg(argBuilder);
		}
	}

	internal void Ldarg(string arg)
	{
		Ldarg(GetArg(arg));
	}

	internal void Ldarg(ArgBuilder arg)
	{
		Ldarg(arg.Index);
	}

	internal void Ldarg(int slot)
	{
		_ilGen.Emit(OpCodes.Ldarg, slot);
	}

	internal void Ldarga(ArgBuilder argBuilder)
	{
		Ldarga(argBuilder.Index);
	}

	internal void Ldarga(int slot)
	{
		_ilGen.Emit(OpCodes.Ldarga, slot);
	}

	internal void Ldlen()
	{
		_ilGen.Emit(OpCodes.Ldlen);
		_ilGen.Emit(OpCodes.Conv_I4);
	}

	private OpCode GetLdelemOpCode(TypeCode typeCode)
	{
		return s_ldelemOpCodes[(int)typeCode];
	}

	internal void Ldelem(Type arrayElementType)
	{
		if (arrayElementType.IsEnum)
		{
			Ldelem(Enum.GetUnderlyingType(arrayElementType));
			return;
		}
		OpCode ldelemOpCode = GetLdelemOpCode(Type.GetTypeCode(arrayElementType));
		if (ldelemOpCode.Equals(OpCodes.Nop))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ArrayTypeIsNotSupported, arrayElementType.AssemblyQualifiedName));
		}
		_ilGen.Emit(ldelemOpCode);
	}

	internal void Ldelema(Type arrayElementType)
	{
		OpCode ldelema = OpCodes.Ldelema;
		_ilGen.Emit(ldelema, arrayElementType);
	}

	private OpCode GetStelemOpCode(TypeCode typeCode)
	{
		return s_stelemOpCodes[(int)typeCode];
	}

	internal void Stelem(Type arrayElementType)
	{
		if (arrayElementType.IsEnum)
		{
			Stelem(Enum.GetUnderlyingType(arrayElementType));
			return;
		}
		OpCode stelemOpCode = GetStelemOpCode(Type.GetTypeCode(arrayElementType));
		if (stelemOpCode.Equals(OpCodes.Nop))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.ArrayTypeIsNotSupported, arrayElementType.AssemblyQualifiedName));
		}
		_ilGen.Emit(stelemOpCode);
	}

	internal Label DefineLabel()
	{
		return _ilGen.DefineLabel();
	}

	internal void MarkLabel(Label label)
	{
		_ilGen.MarkLabel(label);
	}

	internal void Nop()
	{
		_ilGen.Emit(OpCodes.Nop);
	}

	internal void Add()
	{
		_ilGen.Emit(OpCodes.Add);
	}

	internal void Ret()
	{
		_ilGen.Emit(OpCodes.Ret);
	}

	internal void Br(Label label)
	{
		_ilGen.Emit(OpCodes.Br, label);
	}

	internal void Br_S(Label label)
	{
		_ilGen.Emit(OpCodes.Br_S, label);
	}

	internal void Blt(Label label)
	{
		_ilGen.Emit(OpCodes.Blt, label);
	}

	internal void Brfalse(Label label)
	{
		_ilGen.Emit(OpCodes.Brfalse, label);
	}

	internal void Brtrue(Label label)
	{
		_ilGen.Emit(OpCodes.Brtrue, label);
	}

	internal void Pop()
	{
		_ilGen.Emit(OpCodes.Pop);
	}

	internal void Dup()
	{
		_ilGen.Emit(OpCodes.Dup);
	}

	private void InternalIf(bool negate)
	{
		IfState ifState = new IfState();
		ifState.EndIf = DefineLabel();
		ifState.ElseBegin = DefineLabel();
		if (negate)
		{
			Brtrue(ifState.ElseBegin);
		}
		else
		{
			Brfalse(ifState.ElseBegin);
		}
		_blockStack.Push(ifState);
	}

	private OpCode GetConvOpCode(TypeCode typeCode)
	{
		return s_convOpCodes[(int)typeCode];
	}

	private void InternalConvert(Type source, Type target, bool isAddress)
	{
		if (target == source)
		{
			return;
		}
		if (target.IsValueType)
		{
			if (source.IsValueType)
			{
				OpCode convOpCode = GetConvOpCode(Type.GetTypeCode(target));
				if (convOpCode.Equals(OpCodes.Nop))
				{
					throw new CodeGeneratorConversionException(source, target, isAddress, "NoConversionPossibleTo");
				}
				_ilGen.Emit(convOpCode);
				return;
			}
			if (!source.IsAssignableFrom(target))
			{
				throw new CodeGeneratorConversionException(source, target, isAddress, "IsNotAssignableFrom");
			}
			Unbox(target);
			if (!isAddress)
			{
				Ldobj(target);
			}
		}
		else if (target.IsAssignableFrom(source))
		{
			if (source.IsValueType)
			{
				if (isAddress)
				{
					Ldobj(source);
				}
				Box(source);
			}
		}
		else if (source.IsAssignableFrom(target))
		{
			Castclass(target);
		}
		else
		{
			if (!target.IsInterface && !source.IsInterface)
			{
				throw new CodeGeneratorConversionException(source, target, isAddress, "IsNotAssignableFrom");
			}
			Castclass(target);
		}
	}

	private IfState PopIfState()
	{
		object obj = _blockStack.Pop();
		return obj as IfState;
	}

	internal static AssemblyBuilder CreateAssemblyBuilder(string name)
	{
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.Name = name;
		assemblyName.Version = new Version(1, 0, 0, 0);
		return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
	}

	internal static ModuleBuilder CreateModuleBuilder(AssemblyBuilder assemblyBuilder, string name)
	{
		return assemblyBuilder.DefineDynamicModule(name);
	}

	internal static TypeBuilder CreateTypeBuilder(ModuleBuilder moduleBuilder, string name, TypeAttributes attributes, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces)
	{
		return moduleBuilder.DefineType("Microsoft.Xml.Serialization.GeneratedAssembly." + name, attributes, parent, interfaces);
	}

	internal void InitElseIf()
	{
		_elseIfState = (IfState)_blockStack.Pop();
		_initElseIfStack = _blockStack.Count;
		Br(_elseIfState.EndIf);
		MarkLabel(_elseIfState.ElseBegin);
	}

	internal void InitIf()
	{
		_initIfStack = _blockStack.Count;
	}

	internal void AndIf(Cmp cmpOp)
	{
		if (_initIfStack == _blockStack.Count)
		{
			_initIfStack = -1;
			If(cmpOp);
		}
		else if (_initElseIfStack == _blockStack.Count)
		{
			_initElseIfStack = -1;
			_elseIfState.ElseBegin = DefineLabel();
			_ilGen.Emit(GetBranchCode(cmpOp), _elseIfState.ElseBegin);
			_blockStack.Push(_elseIfState);
		}
		else
		{
			IfState ifState = (IfState)_blockStack.Peek();
			_ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
		}
	}

	internal void AndIf()
	{
		if (_initIfStack == _blockStack.Count)
		{
			_initIfStack = -1;
			If();
		}
		else if (_initElseIfStack == _blockStack.Count)
		{
			_initElseIfStack = -1;
			_elseIfState.ElseBegin = DefineLabel();
			Brfalse(_elseIfState.ElseBegin);
			_blockStack.Push(_elseIfState);
		}
		else
		{
			IfState ifState = (IfState)_blockStack.Peek();
			Brfalse(ifState.ElseBegin);
		}
	}

	internal void IsInst(Type type)
	{
		_ilGen.Emit(OpCodes.Isinst, type);
	}

	internal void Beq(Label label)
	{
		_ilGen.Emit(OpCodes.Beq, label);
	}

	internal void Bne(Label label)
	{
		_ilGen.Emit(OpCodes.Bne_Un, label);
	}

	internal void GotoMethodEnd()
	{
		Br(_methodEndLabel);
	}

	internal void WhileBegin()
	{
		WhileState whileState = new WhileState(this);
		Ldc(boolVar: true);
		Brtrue(whileState.CondLabel);
		MarkLabel(whileState.StartLabel);
		_whileStack.Push(whileState);
	}

	internal void WhileEnd()
	{
		WhileState whileState = _whileStack.Pop();
		MarkLabel(whileState.EndLabel);
	}

	internal void WhileContinue()
	{
		WhileState whileState = _whileStack.Peek();
		Br(whileState.CondLabel);
	}

	internal void WhileBeginCondition()
	{
		WhileState whileState = _whileStack.Peek();
		Nop();
		MarkLabel(whileState.CondLabel);
	}

	internal void WhileEndCondition()
	{
		WhileState whileState = _whileStack.Peek();
		Brtrue(whileState.StartLabel);
	}
}
