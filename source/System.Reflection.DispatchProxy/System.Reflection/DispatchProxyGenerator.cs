using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Reflection;

internal static class DispatchProxyGenerator
{
	private sealed class GeneratedTypeInfo
	{
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		public Type GeneratedType { get; }

		public MethodInfo[] MethodInfos { get; }

		public GeneratedTypeInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type generatedType, MethodInfo[] methodInfos)
		{
			GeneratedType = generatedType;
			MethodInfos = methodInfos;
		}
	}

	private sealed class ProxyAssembly
	{
		private readonly AssemblyBuilder _ab;

		private readonly ModuleBuilder _mb;

		private int _typeId;

		private readonly HashSet<string> _ignoresAccessAssemblyNames = new HashSet<string>();

		private ConstructorInfo _ignoresAccessChecksToAttributeConstructor;

		internal ConstructorInfo IgnoresAccessChecksAttributeConstructor
		{
			get
			{
				if (_ignoresAccessChecksToAttributeConstructor == null)
				{
					_ignoresAccessChecksToAttributeConstructor = IgnoreAccessChecksToAttributeBuilder.AddToModule(_mb);
				}
				return _ignoresAccessChecksToAttributeConstructor;
			}
		}

		public ProxyAssembly()
		{
			_ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProxyBuilder"), AssemblyBuilderAccess.Run);
			_mb = _ab.DefineDynamicModule("testmod");
		}

		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "Only the parameterless ctor is referenced on proxyBaseType. Other members can be trimmed if unused.")]
		public ProxyBuilder CreateProxy(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type proxyBaseType)
		{
			int num = Interlocked.Increment(ref _typeId);
			TypeBuilder tb = _mb.DefineType(name + "_" + num, TypeAttributes.Public, proxyBaseType);
			return new ProxyBuilder(this, tb, proxyBaseType);
		}

		internal void GenerateInstanceOfIgnoresAccessChecksToAttribute(string assemblyName)
		{
			ConstructorInfo ignoresAccessChecksAttributeConstructor = IgnoresAccessChecksAttributeConstructor;
			CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(ignoresAccessChecksAttributeConstructor, new object[1] { assemblyName });
			_ab.SetCustomAttribute(customAttribute);
		}

		internal void EnsureTypeIsVisible(Type type)
		{
			if (!type.IsVisible)
			{
				string name = type.Assembly.GetName().Name;
				if (!_ignoresAccessAssemblyNames.Contains(name))
				{
					GenerateInstanceOfIgnoresAccessChecksToAttribute(name);
					_ignoresAccessAssemblyNames.Add(name);
				}
			}
		}
	}

	private sealed class ProxyBuilder
	{
		private sealed class ParametersArray
		{
			private readonly ILGenerator _il;

			private readonly Type[] _paramTypes;

			internal ParametersArray(ILGenerator il, Type[] paramTypes)
			{
				_il = il;
				_paramTypes = paramTypes;
			}

			internal void Get(int i)
			{
				_il.Emit(OpCodes.Ldarg, i + 1);
			}

			internal void BeginSet(int i)
			{
				_il.Emit(OpCodes.Ldarg, i + 1);
			}

			internal void EndSet(int i, Type stackType)
			{
				Type elementType = _paramTypes[i].GetElementType();
				Convert(_il, stackType, elementType, isAddress: false);
				Stind(_il, elementType);
			}
		}

		private sealed class GenericArray<T>
		{
			private readonly ILGenerator _il;

			private readonly LocalBuilder _lb;

			internal GenericArray(ILGenerator il, int len)
			{
				_il = il;
				_lb = il.DeclareLocal(typeof(T[]));
				il.Emit(OpCodes.Ldc_I4, len);
				il.Emit(OpCodes.Newarr, typeof(T));
				il.Emit(OpCodes.Stloc, _lb);
			}

			internal void Load()
			{
				_il.Emit(OpCodes.Ldloc, _lb);
			}

			internal void Get(int i)
			{
				_il.Emit(OpCodes.Ldloc, _lb);
				_il.Emit(OpCodes.Ldc_I4, i);
				_il.Emit(OpCodes.Ldelem_Ref);
			}

			internal void BeginSet(int i)
			{
				_il.Emit(OpCodes.Ldloc, _lb);
				_il.Emit(OpCodes.Ldc_I4, i);
			}

			internal void EndSet(Type stackType)
			{
				Convert(_il, stackType, typeof(T), isAddress: false);
				_il.Emit(OpCodes.Stelem_Ref);
			}
		}

		private sealed class PropertyAccessorInfo
		{
			[CompilerGenerated]
			private readonly MethodInfo _003CInterfaceSetMethod_003Ek__BackingField;

			public MethodInfo InterfaceGetMethod { get; }

			public MethodBuilder GetMethodBuilder { get; set; }

			public MethodBuilder SetMethodBuilder { get; set; }

			public PropertyAccessorInfo(MethodInfo interfaceGetMethod, MethodInfo interfaceSetMethod)
			{
				InterfaceGetMethod = interfaceGetMethod;
				_003CInterfaceSetMethod_003Ek__BackingField = interfaceSetMethod;
			}
		}

		private sealed class EventAccessorInfo
		{
			[CompilerGenerated]
			private readonly MethodInfo _003CInterfaceRaiseMethod_003Ek__BackingField;

			public MethodInfo InterfaceAddMethod { get; }

			public MethodInfo InterfaceRemoveMethod { get; }

			public MethodBuilder AddMethodBuilder { get; set; }

			public MethodBuilder RemoveMethodBuilder { get; set; }

			public MethodBuilder RaiseMethodBuilder { get; set; }

			public EventAccessorInfo(MethodInfo interfaceAddMethod, MethodInfo interfaceRemoveMethod, MethodInfo interfaceRaiseMethod)
			{
				InterfaceAddMethod = interfaceAddMethod;
				InterfaceRemoveMethod = interfaceRemoveMethod;
				_003CInterfaceRaiseMethod_003Ek__BackingField = interfaceRaiseMethod;
			}
		}

		private readonly ProxyAssembly _assembly;

		private readonly TypeBuilder _tb;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private readonly Type _proxyBaseType;

		private readonly List<FieldBuilder> _fields;

		private readonly List<MethodInfo> _methodInfos;

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

		private static readonly OpCode[] s_stindOpCodes = new OpCode[19]
		{
			OpCodes.Nop,
			OpCodes.Nop,
			OpCodes.Nop,
			OpCodes.Stind_I1,
			OpCodes.Stind_I2,
			OpCodes.Stind_I1,
			OpCodes.Stind_I1,
			OpCodes.Stind_I2,
			OpCodes.Stind_I2,
			OpCodes.Stind_I4,
			OpCodes.Stind_I4,
			OpCodes.Stind_I8,
			OpCodes.Stind_I8,
			OpCodes.Stind_R4,
			OpCodes.Stind_R8,
			OpCodes.Nop,
			OpCodes.Nop,
			OpCodes.Nop,
			OpCodes.Stind_Ref
		};

		internal ProxyBuilder(ProxyAssembly assembly, TypeBuilder tb, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type proxyBaseType)
		{
			_assembly = assembly;
			_tb = tb;
			_proxyBaseType = proxyBaseType;
			_fields = new List<FieldBuilder>();
			_fields.Add(tb.DefineField("_methodInfos", typeof(MethodInfo[]), FieldAttributes.Private));
			_methodInfos = new List<MethodInfo>();
			_assembly.EnsureTypeIsVisible(proxyBaseType);
		}

		private void Complete()
		{
			Type[] array = new Type[_fields.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _fields[i].FieldType;
			}
			ConstructorBuilder constructorBuilder = _tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, array);
			ILGenerator iLGenerator = constructorBuilder.GetILGenerator();
			ConstructorInfo constructor = _proxyBaseType.GetConstructor(Type.EmptyTypes);
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Call, constructor);
			for (int j = 0; j < array.Length; j++)
			{
				iLGenerator.Emit(OpCodes.Ldarg_0);
				iLGenerator.Emit(OpCodes.Ldarg, j + 1);
				iLGenerator.Emit(OpCodes.Stfld, _fields[j]);
			}
			iLGenerator.Emit(OpCodes.Ret);
		}

		internal GeneratedTypeInfo CreateType()
		{
			Complete();
			return new GeneratedTypeInfo(_tb.CreateType(), _methodInfos.ToArray());
		}

		internal void AddInterfaceImpl([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type iface)
		{
			_assembly.EnsureTypeIsVisible(iface);
			_tb.AddInterfaceImplementation(iface);
			Dictionary<MethodInfo, PropertyAccessorInfo> dictionary = new Dictionary<MethodInfo, PropertyAccessorInfo>();
			foreach (PropertyInfo runtimeProperty in iface.GetRuntimeProperties())
			{
				PropertyAccessorInfo value = new PropertyAccessorInfo(runtimeProperty.GetMethod, runtimeProperty.SetMethod);
				if (runtimeProperty.GetMethod != null)
				{
					dictionary[runtimeProperty.GetMethod] = value;
				}
				if (runtimeProperty.SetMethod != null)
				{
					dictionary[runtimeProperty.SetMethod] = value;
				}
			}
			Dictionary<MethodInfo, EventAccessorInfo> dictionary2 = new Dictionary<MethodInfo, EventAccessorInfo>();
			foreach (EventInfo runtimeEvent in iface.GetRuntimeEvents())
			{
				EventAccessorInfo value2 = new EventAccessorInfo(runtimeEvent.AddMethod, runtimeEvent.RemoveMethod, runtimeEvent.RaiseMethod);
				if (runtimeEvent.AddMethod != null)
				{
					dictionary2[runtimeEvent.AddMethod] = value2;
				}
				if (runtimeEvent.RemoveMethod != null)
				{
					dictionary2[runtimeEvent.RemoveMethod] = value2;
				}
				if (runtimeEvent.RaiseMethod != null)
				{
					dictionary2[runtimeEvent.RaiseMethod] = value2;
				}
			}
			foreach (MethodInfo runtimeMethod in iface.GetRuntimeMethods())
			{
				if (!runtimeMethod.IsVirtual || runtimeMethod.IsFinal)
				{
					continue;
				}
				int count = _methodInfos.Count;
				_methodInfos.Add(runtimeMethod);
				MethodBuilder methodBuilder = AddMethodImpl(runtimeMethod, count);
				if (dictionary.TryGetValue(runtimeMethod, out var value3))
				{
					if (runtimeMethod.Equals(value3.InterfaceGetMethod))
					{
						value3.GetMethodBuilder = methodBuilder;
					}
					else
					{
						value3.SetMethodBuilder = methodBuilder;
					}
				}
				if (dictionary2.TryGetValue(runtimeMethod, out var value4))
				{
					if (runtimeMethod.Equals(value4.InterfaceAddMethod))
					{
						value4.AddMethodBuilder = methodBuilder;
					}
					else if (runtimeMethod.Equals(value4.InterfaceRemoveMethod))
					{
						value4.RemoveMethodBuilder = methodBuilder;
					}
					else
					{
						value4.RaiseMethodBuilder = methodBuilder;
					}
				}
			}
			foreach (PropertyInfo runtimeProperty2 in iface.GetRuntimeProperties())
			{
				PropertyAccessorInfo propertyAccessorInfo = dictionary[runtimeProperty2.GetMethod ?? runtimeProperty2.SetMethod];
				if (!(propertyAccessorInfo.GetMethodBuilder == null) || !(propertyAccessorInfo.SetMethodBuilder == null))
				{
					PropertyBuilder propertyBuilder = _tb.DefineProperty(runtimeProperty2.Name, runtimeProperty2.Attributes, runtimeProperty2.PropertyType, (from p in runtimeProperty2.GetIndexParameters()
						select p.ParameterType).ToArray());
					if (propertyAccessorInfo.GetMethodBuilder != null)
					{
						propertyBuilder.SetGetMethod(propertyAccessorInfo.GetMethodBuilder);
					}
					if (propertyAccessorInfo.SetMethodBuilder != null)
					{
						propertyBuilder.SetSetMethod(propertyAccessorInfo.SetMethodBuilder);
					}
				}
			}
			foreach (EventInfo runtimeEvent2 in iface.GetRuntimeEvents())
			{
				EventAccessorInfo eventAccessorInfo = dictionary2[runtimeEvent2.AddMethod ?? runtimeEvent2.RemoveMethod];
				if (!(eventAccessorInfo.AddMethodBuilder == null) || !(eventAccessorInfo.RemoveMethodBuilder == null) || !(eventAccessorInfo.RaiseMethodBuilder == null))
				{
					EventBuilder eventBuilder = _tb.DefineEvent(runtimeEvent2.Name, runtimeEvent2.Attributes, runtimeEvent2.EventHandlerType);
					if (eventAccessorInfo.AddMethodBuilder != null)
					{
						eventBuilder.SetAddOnMethod(eventAccessorInfo.AddMethodBuilder);
					}
					if (eventAccessorInfo.RemoveMethodBuilder != null)
					{
						eventBuilder.SetRemoveOnMethod(eventAccessorInfo.RemoveMethodBuilder);
					}
					if (eventAccessorInfo.RaiseMethodBuilder != null)
					{
						eventBuilder.SetRaiseMethod(eventAccessorInfo.RaiseMethodBuilder);
					}
				}
			}
		}

		private MethodBuilder AddMethodImpl(MethodInfo mi, int methodInfoIndex)
		{
			ParameterInfo[] parameters = mi.GetParameters();
			Type[] array = new Type[parameters.Length];
			Type[][] array2 = new Type[array.Length][];
			for (int i = 0; i < parameters.Length; i++)
			{
				array[i] = parameters[i].ParameterType;
				array2[i] = parameters[i].GetRequiredCustomModifiers();
			}
			MethodBuilder methodBuilder = _tb.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, mi.ReturnType, null, null, array, array2, null);
			if (mi.ContainsGenericParameters)
			{
				Type[] genericArguments = mi.GetGenericArguments();
				string[] array3 = new string[genericArguments.Length];
				for (int j = 0; j < genericArguments.Length; j++)
				{
					array3[j] = genericArguments[j].Name;
				}
				GenericTypeParameterBuilder[] array4 = methodBuilder.DefineGenericParameters(array3);
				for (int k = 0; k < array4.Length; k++)
				{
					array4[k].SetGenericParameterAttributes(genericArguments[k].GenericParameterAttributes);
				}
			}
			ILGenerator iLGenerator = methodBuilder.GetILGenerator();
			ParametersArray parametersArray = new ParametersArray(iLGenerator, array);
			iLGenerator.Emit(OpCodes.Nop);
			GenericArray<object> genericArray = new GenericArray<object>(iLGenerator, parameters.Length);
			for (int l = 0; l < parameters.Length; l++)
			{
				if (!parameters[l].IsOut || !parameters[l].ParameterType.IsByRef || parameters[l].IsIn)
				{
					genericArray.BeginSet(l);
					parametersArray.Get(l);
					genericArray.EndSet(parameters[l].ParameterType);
				}
			}
			LocalBuilder local = iLGenerator.DeclareLocal(typeof(MethodInfo));
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldfld, _fields[0]);
			iLGenerator.Emit(OpCodes.Ldc_I4, methodInfoIndex);
			iLGenerator.Emit(OpCodes.Ldelem_Ref);
			iLGenerator.Emit(OpCodes.Stloc, local);
			if (mi.ContainsGenericParameters)
			{
				iLGenerator.Emit(OpCodes.Ldloc, local);
				Type[] genericArguments2 = mi.GetGenericArguments();
				GenericArray<Type> genericArray2 = new GenericArray<Type>(iLGenerator, genericArguments2.Length);
				for (int m = 0; m < genericArguments2.Length; m++)
				{
					genericArray2.BeginSet(m);
					iLGenerator.Emit(OpCodes.Ldtoken, genericArguments2[m]);
					iLGenerator.Emit(OpCodes.Call, s_getTypeFromHandleMethod);
					genericArray2.EndSet(typeof(Type));
				}
				genericArray2.Load();
				iLGenerator.Emit(OpCodes.Callvirt, s_makeGenericMethodMethod);
				iLGenerator.Emit(OpCodes.Stloc, local);
			}
			LocalBuilder localBuilder = ((mi.ReturnType != typeof(void)) ? iLGenerator.DeclareLocal(typeof(object)) : null);
			iLGenerator.Emit(OpCodes.Ldarg_0);
			iLGenerator.Emit(OpCodes.Ldloc, local);
			genericArray.Load();
			iLGenerator.Emit(OpCodes.Callvirt, s_dispatchProxyInvokeMethod);
			if (localBuilder != null)
			{
				iLGenerator.Emit(OpCodes.Stloc, localBuilder);
			}
			else
			{
				iLGenerator.Emit(OpCodes.Pop);
			}
			for (int n = 0; n < parameters.Length; n++)
			{
				if (parameters[n].ParameterType.IsByRef)
				{
					parametersArray.BeginSet(n);
					genericArray.Get(n);
					parametersArray.EndSet(n, typeof(object));
				}
			}
			if (localBuilder != null)
			{
				iLGenerator.Emit(OpCodes.Ldloc, localBuilder);
				Convert(iLGenerator, typeof(object), mi.ReturnType, isAddress: false);
			}
			iLGenerator.Emit(OpCodes.Ret);
			_tb.DefineMethodOverride(methodBuilder, mi);
			return methodBuilder;
		}

		private static int GetTypeCode(Type type)
		{
			if (type == null)
			{
				return 0;
			}
			if (type == typeof(bool))
			{
				return 3;
			}
			if (type == typeof(char))
			{
				return 4;
			}
			if (type == typeof(sbyte))
			{
				return 5;
			}
			if (type == typeof(byte))
			{
				return 6;
			}
			if (type == typeof(short))
			{
				return 7;
			}
			if (type == typeof(ushort))
			{
				return 8;
			}
			if (type == typeof(int))
			{
				return 9;
			}
			if (type == typeof(uint))
			{
				return 10;
			}
			if (type == typeof(long))
			{
				return 11;
			}
			if (type == typeof(ulong))
			{
				return 12;
			}
			if (type == typeof(float))
			{
				return 13;
			}
			if (type == typeof(double))
			{
				return 14;
			}
			if (type == typeof(decimal))
			{
				return 15;
			}
			if (type == typeof(DateTime))
			{
				return 16;
			}
			if (type == typeof(string))
			{
				return 18;
			}
			if (type.IsEnum)
			{
				return GetTypeCode(Enum.GetUnderlyingType(type));
			}
			return 1;
		}

		private static void Convert(ILGenerator il, Type source, Type target, bool isAddress)
		{
			if (target == source)
			{
				return;
			}
			if (source.IsByRef)
			{
				Type elementType = source.GetElementType();
				Ldind(il, elementType);
				Convert(il, elementType, target, isAddress);
			}
			else if (target.IsValueType)
			{
				if (source.IsValueType)
				{
					OpCode opcode = s_convOpCodes[GetTypeCode(target)];
					il.Emit(opcode);
					return;
				}
				il.Emit(OpCodes.Unbox, target);
				if (!isAddress)
				{
					Ldind(il, target);
				}
			}
			else if (target.IsAssignableFrom(source))
			{
				if (source.IsValueType || source.IsGenericParameter)
				{
					if (isAddress)
					{
						Ldind(il, source);
					}
					il.Emit(OpCodes.Box, source);
				}
			}
			else if (target.IsGenericParameter)
			{
				il.Emit(OpCodes.Unbox_Any, target);
			}
			else
			{
				il.Emit(OpCodes.Castclass, target);
			}
		}

		private static void Ldind(ILGenerator il, Type type)
		{
			OpCode opcode = s_ldindOpCodes[GetTypeCode(type)];
			if (!opcode.Equals(OpCodes.Nop))
			{
				il.Emit(opcode);
			}
			else
			{
				il.Emit(OpCodes.Ldobj, type);
			}
		}

		private static void Stind(ILGenerator il, Type type)
		{
			OpCode opcode = s_stindOpCodes[GetTypeCode(type)];
			if (!opcode.Equals(OpCodes.Nop))
			{
				il.Emit(opcode);
			}
			else
			{
				il.Emit(OpCodes.Stobj, type);
			}
		}
	}

	private static readonly Dictionary<Type, Dictionary<Type, GeneratedTypeInfo>> s_baseTypeAndInterfaceToGeneratedProxyType = new Dictionary<Type, Dictionary<Type, GeneratedTypeInfo>>();

	private static readonly ProxyAssembly s_proxyAssembly = new ProxyAssembly();

	private static readonly MethodInfo s_dispatchProxyInvokeMethod = typeof(DispatchProxy).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly MethodInfo s_getTypeFromHandleMethod = typeof(Type).GetRuntimeMethod("GetTypeFromHandle", new Type[1] { typeof(RuntimeTypeHandle) });

	private static readonly MethodInfo s_makeGenericMethodMethod = GetGenericMethodMethodInfo();

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "MakeGenericMethod is safe here because the user code invoking the generic method will reference the GenericTypes being used, which will guarantee the requirements of the generic method.")]
	private static MethodInfo GetGenericMethodMethodInfo()
	{
		return typeof(MethodInfo).GetMethod("MakeGenericMethod", new Type[1] { typeof(Type[]) });
	}

	internal static object CreateProxyInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		GeneratedTypeInfo proxyType = GetProxyType(baseType, interfaceType);
		return Activator.CreateInstance(proxyType.GeneratedType, new object[1] { proxyType.MethodInfos });
	}

	private static GeneratedTypeInfo GetProxyType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		lock (s_baseTypeAndInterfaceToGeneratedProxyType)
		{
			if (!s_baseTypeAndInterfaceToGeneratedProxyType.TryGetValue(baseType, out var value))
			{
				value = new Dictionary<Type, GeneratedTypeInfo>();
				s_baseTypeAndInterfaceToGeneratedProxyType[baseType] = value;
			}
			if (!value.TryGetValue(interfaceType, out var value2))
			{
				value2 = (value[interfaceType] = GenerateProxyType(baseType, interfaceType));
			}
			return value2;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062:UnrecognizedReflectionPattern", Justification = "interfaceType is annotated as preserve All members, so any Types returned from GetInterfaces should be preserved as well once https://github.com/mono/linker/issues/1731 is fixed.")]
	private static GeneratedTypeInfo GenerateProxyType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
	{
		if (!interfaceType.IsInterface)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InterfaceType_Must_Be_Interface, interfaceType.FullName), "T");
		}
		if (baseType.IsSealed)
		{
			throw new ArgumentException(System.SR.Format(System.SR.BaseType_Cannot_Be_Sealed, baseType.FullName), "TProxy");
		}
		if (baseType.IsAbstract)
		{
			throw new ArgumentException(System.SR.Format(System.SR.BaseType_Cannot_Be_Abstract, baseType.FullName), "TProxy");
		}
		if (baseType.GetConstructor(Type.EmptyTypes) == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.BaseType_Must_Have_Default_Ctor, baseType.FullName), "TProxy");
		}
		ProxyBuilder proxyBuilder = s_proxyAssembly.CreateProxy("generatedProxy", baseType);
		Type[] interfaces = interfaceType.GetInterfaces();
		foreach (Type iface in interfaces)
		{
			proxyBuilder.AddInterfaceImpl(iface);
		}
		proxyBuilder.AddInterfaceImpl(interfaceType);
		return proxyBuilder.CreateType();
	}
}
