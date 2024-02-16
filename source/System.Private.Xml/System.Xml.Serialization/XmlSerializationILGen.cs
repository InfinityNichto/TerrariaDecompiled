using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace System.Xml.Serialization;

internal class XmlSerializationILGen
{
	private int _nextMethodNumber;

	private readonly Dictionary<TypeMapping, string> _methodNames = new Dictionary<TypeMapping, string>();

	private readonly Dictionary<string, MethodBuilderInfo> _methodBuilders = new Dictionary<string, MethodBuilderInfo>();

	internal Dictionary<string, Type> CreatedTypes = new Dictionary<string, Type>();

	internal Dictionary<string, MemberInfo> memberInfos = new Dictionary<string, MemberInfo>();

	private readonly ReflectionAwareILGen _raCodeGen;

	private readonly TypeScope[] _scopes;

	private readonly TypeDesc _stringTypeDesc;

	private readonly TypeDesc _qnameTypeDesc;

	private readonly string _className;

	private TypeMapping[] _referencedMethods;

	private int _references;

	private readonly HashSet<TypeMapping> _generatedMethods = new HashSet<TypeMapping>();

	private ModuleBuilder _moduleBuilder;

	private readonly TypeAttributes _typeAttributes;

	protected TypeBuilder typeBuilder;

	protected CodeGenerator ilg;

	private static readonly Dictionary<string, Regex> s_regexs = new Dictionary<string, Regex>();

	internal int NextMethodNumber
	{
		get
		{
			return _nextMethodNumber;
		}
		set
		{
			_nextMethodNumber = value;
		}
	}

	internal ReflectionAwareILGen RaCodeGen => _raCodeGen;

	internal TypeDesc StringTypeDesc => _stringTypeDesc;

	internal TypeDesc QnameTypeDesc => _qnameTypeDesc;

	internal string ClassName => _className;

	internal TypeScope[] Scopes => _scopes;

	internal Dictionary<TypeMapping, string> MethodNames => _methodNames;

	internal HashSet<TypeMapping> GeneratedMethods => _generatedMethods;

	internal ModuleBuilder ModuleBuilder
	{
		get
		{
			return _moduleBuilder;
		}
		set
		{
			_moduleBuilder = value;
		}
	}

	internal TypeAttributes TypeAttributes => _typeAttributes;

	[RequiresUnreferencedCode("Calls GetTypeDesc")]
	internal XmlSerializationILGen(TypeScope[] scopes, string access, string className)
	{
		_scopes = scopes;
		if (scopes.Length != 0)
		{
			_stringTypeDesc = scopes[0].GetTypeDesc(typeof(string));
			_qnameTypeDesc = scopes[0].GetTypeDesc(typeof(XmlQualifiedName));
		}
		_raCodeGen = new ReflectionAwareILGen();
		_className = className;
		_typeAttributes = TypeAttributes.Public;
	}

	internal static Regex NewRegex(string pattern)
	{
		Regex value;
		lock (s_regexs)
		{
			if (!s_regexs.TryGetValue(pattern, out value))
			{
				value = new Regex(pattern);
				s_regexs.Add(pattern, value);
			}
		}
		return value;
	}

	internal MethodBuilder EnsureMethodBuilder(TypeBuilder typeBuilder, string methodName, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
	{
		if (!_methodBuilders.TryGetValue(methodName, out var value))
		{
			MethodBuilder methodBuilder = typeBuilder.DefineMethod(methodName, attributes, returnType, parameterTypes);
			value = new MethodBuilderInfo(methodBuilder, parameterTypes);
			_methodBuilders.Add(methodName, value);
		}
		return value.MethodBuilder;
	}

	internal MethodBuilderInfo GetMethodBuilder(string methodName)
	{
		return _methodBuilders[methodName];
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	internal virtual void GenerateMethod(TypeMapping mapping)
	{
	}

	[RequiresUnreferencedCode("calls GenerateMethod")]
	internal void GenerateReferencedMethods()
	{
		while (_references > 0)
		{
			TypeMapping mapping = _referencedMethods[--_references];
			GenerateMethod(mapping);
		}
	}

	internal string ReferenceMapping(TypeMapping mapping)
	{
		if (!_generatedMethods.Contains(mapping))
		{
			_referencedMethods = EnsureArrayIndex(_referencedMethods, _references);
			_referencedMethods[_references++] = mapping;
		}
		_methodNames.TryGetValue(mapping, out var value);
		return value;
	}

	private TypeMapping[] EnsureArrayIndex(TypeMapping[] a, int index)
	{
		if (a == null)
		{
			return new TypeMapping[32];
		}
		if (index < a.Length)
		{
			return a;
		}
		TypeMapping[] array = new TypeMapping[a.Length + 32];
		Array.Copy(a, array, index);
		return array;
	}

	[return: NotNullIfNotNull("value")]
	internal string GetCSharpString(string value)
	{
		return ReflectionAwareILGen.GetCSharpString(value);
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	internal FieldBuilder GenerateHashtableGetBegin(string privateName, string publicName, TypeBuilder serializerContractTypeBuilder)
	{
		FieldBuilder fieldBuilder = serializerContractTypeBuilder.DefineField(privateName, typeof(Hashtable), FieldAttributes.Private);
		ilg = new CodeGenerator(serializerContractTypeBuilder);
		PropertyBuilder propertyBuilder = serializerContractTypeBuilder.DefineProperty(publicName, PropertyAttributes.None, CallingConventions.HasThis, typeof(Hashtable), null, null, null, null, null);
		ilg.BeginMethod(typeof(Hashtable), "get_" + publicName, Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName);
		propertyBuilder.SetGetMethod(ilg.MethodBuilder);
		ilg.Ldarg(0);
		ilg.LoadMember(fieldBuilder);
		ilg.Load(null);
		ilg.If(Cmp.EqualTo);
		ConstructorInfo constructor = typeof(Hashtable).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		LocalBuilder local = ilg.DeclareLocal(typeof(Hashtable), "_tmp");
		ilg.New(constructor);
		ilg.Stloc(local);
		return fieldBuilder;
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	internal void GenerateHashtableGetEnd(FieldBuilder fieldBuilder)
	{
		ilg.Ldarg(0);
		ilg.LoadMember(fieldBuilder);
		ilg.Load(null);
		ilg.If(Cmp.EqualTo);
		ilg.Ldarg(0);
		ilg.Ldloc(typeof(Hashtable), "_tmp");
		ilg.StoreMember(fieldBuilder);
		ilg.EndIf();
		ilg.EndIf();
		ilg.Ldarg(0);
		ilg.LoadMember(fieldBuilder);
		ilg.GotoMethodEnd();
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls GenerateHashtableGetBegin")]
	internal FieldBuilder GeneratePublicMethods(string privateName, string publicName, string[] methods, XmlMapping[] xmlMappings, TypeBuilder serializerContractTypeBuilder)
	{
		FieldBuilder fieldBuilder = GenerateHashtableGetBegin(privateName, publicName, serializerContractTypeBuilder);
		if (methods != null && methods.Length != 0 && xmlMappings != null && xmlMappings.Length == methods.Length)
		{
			MethodInfo method = typeof(Hashtable).GetMethod("set_Item", new Type[2]
			{
				typeof(object),
				typeof(object)
			});
			for (int i = 0; i < methods.Length; i++)
			{
				if (methods[i] != null)
				{
					ilg.Ldloc(typeof(Hashtable), "_tmp");
					ilg.Ldstr(GetCSharpString(xmlMappings[i].Key));
					ilg.Ldstr(GetCSharpString(methods[i]));
					ilg.Call(method);
				}
			}
		}
		GenerateHashtableGetEnd(fieldBuilder);
		return fieldBuilder;
	}

	internal void GenerateSupportedTypes(Type[] types, TypeBuilder serializerContractTypeBuilder)
	{
		ilg = new CodeGenerator(serializerContractTypeBuilder);
		ilg.BeginMethod(typeof(bool), "CanSerialize", new Type[1] { typeof(Type) }, new string[1] { "type" }, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		HashSet<Type> hashSet = new HashSet<Type>();
		foreach (Type type in types)
		{
			if (!(type == null) && (type.IsPublic || type.IsNestedPublic) && hashSet.Add(type) && !type.IsGenericType && !type.ContainsGenericParameters)
			{
				ilg.Ldarg("type");
				ilg.Ldc(type);
				ilg.If(Cmp.EqualTo);
				ilg.Ldc(boolVar: true);
				ilg.GotoMethodEnd();
				ilg.EndIf();
			}
		}
		ilg.Ldc(boolVar: false);
		ilg.GotoMethodEnd();
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("Uses CreatedTypes Dictionary")]
	internal string GenerateBaseSerializer(string baseSerializer, string readerClass, string writerClass, CodeIdentifiers classes)
	{
		baseSerializer = CodeIdentifier.MakeValid(baseSerializer);
		baseSerializer = classes.AddUnique(baseSerializer, baseSerializer);
		TypeBuilder typeBuilder = CodeGenerator.CreateTypeBuilder(_moduleBuilder, CodeIdentifier.GetCSharpName(baseSerializer), TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, typeof(XmlSerializer), Type.EmptyTypes);
		ConstructorInfo constructor = CreatedTypes[readerClass].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(XmlSerializationReader), "CreateReader", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		ilg.New(constructor);
		ilg.EndMethod();
		ConstructorInfo constructor2 = CreatedTypes[writerClass].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.BeginMethod(typeof(XmlSerializationWriter), "CreateWriter", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		ilg.New(constructor2);
		ilg.EndMethod();
		typeBuilder.DefineDefaultConstructor(MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
		Type type = typeBuilder.CreateTypeInfo().AsType();
		CreatedTypes.Add(type.Name, type);
		return baseSerializer;
	}

	[RequiresUnreferencedCode("uses CreatedTypes dictionary")]
	internal string GenerateTypedSerializer(string readMethod, string writeMethod, XmlMapping mapping, CodeIdentifiers classes, string baseSerializer, string readerClass, string writerClass)
	{
		string text = CodeIdentifier.MakeValid(Accessor.UnescapeName(mapping.Accessor.Mapping.TypeDesc.Name));
		text = classes.AddUnique(text + "Serializer", mapping);
		TypeBuilder typeBuilder = CodeGenerator.CreateTypeBuilder(_moduleBuilder, CodeIdentifier.GetCSharpName(text), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, CreatedTypes[baseSerializer], Type.EmptyTypes);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(bool), "CanDeserialize", new Type[1] { typeof(XmlReader) }, new string[1] { "xmlReader" }, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		if (mapping.Accessor.Any)
		{
			ilg.Ldc(boolVar: true);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
		}
		else
		{
			MethodInfo method = typeof(XmlReader).GetMethod("IsStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(string)
			});
			ilg.Ldarg(ilg.GetArg("xmlReader"));
			ilg.Ldstr(GetCSharpString(mapping.Accessor.Name));
			ilg.Ldstr(GetCSharpString(mapping.Accessor.Namespace));
			ilg.Call(method);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
		}
		ilg.MarkLabel(ilg.ReturnLabel);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
		if (writeMethod != null)
		{
			ilg = new CodeGenerator(typeBuilder);
			ilg.BeginMethod(typeof(void), "Serialize", new Type[2]
			{
				typeof(object),
				typeof(XmlSerializationWriter)
			}, new string[2] { "objectToSerialize", "writer" }, MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
			MethodInfo method2 = CreatedTypes[writerClass].GetMethod(writeMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { (mapping is XmlMembersMapping) ? typeof(object[]) : typeof(object) });
			ilg.Ldarg("writer");
			ilg.Castclass(CreatedTypes[writerClass]);
			ilg.Ldarg("objectToSerialize");
			if (mapping is XmlMembersMapping)
			{
				ilg.ConvertValue(typeof(object), typeof(object[]));
			}
			ilg.Call(method2);
			ilg.EndMethod();
		}
		if (readMethod != null)
		{
			ilg = new CodeGenerator(typeBuilder);
			ilg.BeginMethod(typeof(object), "Deserialize", new Type[1] { typeof(XmlSerializationReader) }, new string[1] { "reader" }, MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
			MethodInfo method3 = CreatedTypes[readerClass].GetMethod(readMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg("reader");
			ilg.Castclass(CreatedTypes[readerClass]);
			ilg.Call(method3);
			ilg.EndMethod();
		}
		typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.HideBySig);
		Type type = typeBuilder.CreateTypeInfo().AsType();
		CreatedTypes.Add(type.Name, type);
		return type.Name;
	}

	[RequiresUnreferencedCode("calls GetConstructor")]
	private FieldBuilder GenerateTypedSerializers(Dictionary<string, string> serializers, TypeBuilder serializerContractTypeBuilder)
	{
		string privateName = "typedSerializers";
		FieldBuilder fieldBuilder = GenerateHashtableGetBegin(privateName, "TypedSerializers", serializerContractTypeBuilder);
		MethodInfo method = typeof(Hashtable).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
		{
			typeof(object),
			typeof(object)
		});
		foreach (string key in serializers.Keys)
		{
			ConstructorInfo constructor = CreatedTypes[serializers[key]].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldloc(typeof(Hashtable), "_tmp");
			ilg.Ldstr(GetCSharpString(key));
			ilg.New(constructor);
			ilg.Call(method);
		}
		GenerateHashtableGetEnd(fieldBuilder);
		return fieldBuilder;
	}

	[RequiresUnreferencedCode("Uses CreatedTypes Dictionary")]
	private void GenerateGetSerializer(Dictionary<string, string> serializers, XmlMapping[] xmlMappings, TypeBuilder serializerContractTypeBuilder)
	{
		ilg = new CodeGenerator(serializerContractTypeBuilder);
		ilg.BeginMethod(typeof(XmlSerializer), "GetSerializer", new Type[1] { typeof(Type) }, new string[1] { "type" }, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		for (int i = 0; i < xmlMappings.Length; i++)
		{
			if (xmlMappings[i] is XmlTypeMapping)
			{
				Type type = xmlMappings[i].Accessor.Mapping.TypeDesc.Type;
				if (!(type == null) && (type.IsPublic || type.IsNestedPublic) && !type.IsGenericType && !type.ContainsGenericParameters)
				{
					ilg.Ldarg("type");
					ilg.Ldc(type);
					ilg.If(Cmp.EqualTo);
					ConstructorInfo constructor = CreatedTypes[serializers[xmlMappings[i].Key]].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.New(constructor);
					ilg.Stloc(ilg.ReturnLocal);
					ilg.Br(ilg.ReturnLabel);
					ilg.EndIf();
				}
			}
		}
		ilg.Load(null);
		ilg.Stloc(ilg.ReturnLocal);
		ilg.Br(ilg.ReturnLabel);
		ilg.MarkLabel(ilg.ReturnLabel);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls GenerateTypedSerializers")]
	internal void GenerateSerializerContract(string className, XmlMapping[] xmlMappings, Type[] types, string readerType, string[] readMethods, string writerType, string[] writerMethods, Dictionary<string, string> serializers)
	{
		TypeBuilder typeBuilder = CodeGenerator.CreateTypeBuilder(_moduleBuilder, "XmlSerializerContract", TypeAttributes.Public | TypeAttributes.BeforeFieldInit, typeof(XmlSerializerImplementation), Type.EmptyTypes);
		ilg = new CodeGenerator(typeBuilder);
		PropertyBuilder propertyBuilder = typeBuilder.DefineProperty("Reader", PropertyAttributes.None, typeof(XmlSerializationReader), null, null, null, null, null);
		ilg.BeginMethod(typeof(XmlSerializationReader), "get_Reader", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName);
		propertyBuilder.SetGetMethod(ilg.MethodBuilder);
		ConstructorInfo constructor = CreatedTypes[readerType].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.New(constructor);
		ilg.EndMethod();
		ilg = new CodeGenerator(typeBuilder);
		propertyBuilder = typeBuilder.DefineProperty("Writer", PropertyAttributes.None, typeof(XmlSerializationWriter), null, null, null, null, null);
		ilg.BeginMethod(typeof(XmlSerializationWriter), "get_Writer", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName);
		propertyBuilder.SetGetMethod(ilg.MethodBuilder);
		constructor = CreatedTypes[writerType].GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.New(constructor);
		ilg.EndMethod();
		FieldBuilder memberInfo = GeneratePublicMethods("readMethods", "ReadMethods", readMethods, xmlMappings, typeBuilder);
		FieldBuilder memberInfo2 = GeneratePublicMethods("writeMethods", "WriteMethods", writerMethods, xmlMappings, typeBuilder);
		FieldBuilder memberInfo3 = GenerateTypedSerializers(serializers, typeBuilder);
		GenerateSupportedTypes(types, typeBuilder);
		GenerateGetSerializer(serializers, xmlMappings, typeBuilder);
		ConstructorInfo constructor2 = typeof(XmlSerializerImplementation).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(void), ".ctor", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
		ilg.Ldarg(0);
		ilg.Load(null);
		ilg.StoreMember(memberInfo);
		ilg.Ldarg(0);
		ilg.Load(null);
		ilg.StoreMember(memberInfo2);
		ilg.Ldarg(0);
		ilg.Load(null);
		ilg.StoreMember(memberInfo3);
		ilg.Ldarg(0);
		ilg.Call(constructor2);
		ilg.EndMethod();
		Type type = typeBuilder.CreateTypeInfo().AsType();
		CreatedTypes.Add(type.Name, type);
	}

	internal static bool IsWildcard(SpecialMapping mapping)
	{
		if (mapping is SerializableMapping)
		{
			return ((SerializableMapping)mapping).IsAny;
		}
		return mapping.TypeDesc.CanBeElementValue;
	}

	[RequiresUnreferencedCode("calls ILGenLoad")]
	internal void ILGenLoad(string source)
	{
		ILGenLoad(source, null);
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	internal void ILGenLoad(string source, Type type)
	{
		if (source.StartsWith("o.@", StringComparison.Ordinal))
		{
			MemberInfo memberInfo = memberInfos[source.Substring(3)];
			ilg.LoadMember(ilg.GetVariable("o"), memberInfo);
			if (type != null)
			{
				Type source2 = ((memberInfo is FieldInfo) ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType);
				ilg.ConvertValue(source2, type);
			}
		}
		else
		{
			SourceInfo sourceInfo = new SourceInfo(source, null, null, null, ilg);
			sourceInfo.Load(type);
		}
	}
}
