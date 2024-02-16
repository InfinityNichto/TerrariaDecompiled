using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class XmlSerializationReaderILGen : XmlSerializationILGen
{
	private sealed class Member
	{
		private readonly string _source;

		private readonly string _arrayName;

		private readonly string _arraySource;

		private readonly string _choiceArrayName;

		private readonly string _choiceSource;

		private readonly string _choiceArraySource;

		private readonly MemberMapping _mapping;

		private readonly bool _isArray;

		private readonly bool _isList;

		private bool _isNullable;

		private int _fixupIndex = -1;

		private string _paramsReadSource;

		private string _checkSpecifiedSource;

		internal MemberMapping Mapping => _mapping;

		internal string Source => _source;

		internal string ArrayName => _arrayName;

		internal string ArraySource => _arraySource;

		internal bool IsList => _isList;

		internal bool IsArrayLike
		{
			get
			{
				if (!_isArray)
				{
					return _isList;
				}
				return true;
			}
		}

		internal bool IsNullable
		{
			get
			{
				return _isNullable;
			}
			set
			{
				_isNullable = value;
			}
		}

		internal int FixupIndex => _fixupIndex;

		internal string ParamsReadSource
		{
			get
			{
				return _paramsReadSource;
			}
			set
			{
				_paramsReadSource = value;
			}
		}

		internal string CheckSpecifiedSource
		{
			get
			{
				return _checkSpecifiedSource;
			}
			set
			{
				_checkSpecifiedSource = value;
			}
		}

		internal string ChoiceSource => _choiceSource;

		internal string ChoiceArrayName => _choiceArrayName;

		internal string ChoiceArraySource => _choiceArraySource;

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arrayName, int i, MemberMapping mapping)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef: false, null)
		{
		}

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arrayName, int i, MemberMapping mapping, string choiceSource)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef: false, choiceSource)
		{
		}

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping)
			: this(outerClass, source, arraySource, arrayName, i, mapping, multiRef: false, null)
		{
		}

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, string choiceSource)
			: this(outerClass, source, arraySource, arrayName, i, mapping, multiRef: false, choiceSource)
		{
		}

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arrayName, int i, MemberMapping mapping, bool multiRef)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef, null)
		{
		}

		internal Member(XmlSerializationReaderILGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, bool multiRef, string choiceSource)
		{
			_source = source;
			_arrayName = arrayName + "_" + i.ToString(CultureInfo.InvariantCulture);
			_choiceArrayName = "choice_" + _arrayName;
			_choiceSource = choiceSource;
			if (mapping.TypeDesc.IsArrayLike)
			{
				if (arraySource != null)
				{
					_arraySource = arraySource;
				}
				else
				{
					_arraySource = outerClass.GetArraySource(mapping.TypeDesc, _arrayName, multiRef);
				}
				_isArray = mapping.TypeDesc.IsArray;
				_isList = !_isArray;
				if (mapping.ChoiceIdentifier != null)
				{
					_choiceArraySource = outerClass.GetArraySource(mapping.TypeDesc, _choiceArrayName, multiRef);
					string choiceArrayName = _choiceArrayName;
					string text = "c" + choiceArrayName;
					string cSharpName = mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName;
					string text2 = "(" + cSharpName + "[])";
					string text3 = choiceArrayName + " = " + text2 + "EnsureArrayIndex(" + choiceArrayName + ", " + text + ", " + outerClass.RaCodeGen.GetStringForTypeof(cSharpName) + ");";
					_choiceArraySource = text3 + outerClass.RaCodeGen.GetStringForArrayMember(choiceArrayName, text + "++", mapping.ChoiceIdentifier.Mapping.TypeDesc);
				}
				else
				{
					_choiceArraySource = _choiceSource;
				}
			}
			else
			{
				_arraySource = ((arraySource == null) ? source : arraySource);
				_choiceArraySource = _choiceSource;
			}
			_mapping = mapping;
		}
	}

	private readonly Dictionary<string, string> _idNames = new Dictionary<string, string>();

	private readonly Dictionary<string, FieldBuilder> _idNameFields = new Dictionary<string, FieldBuilder>();

	private Dictionary<string, EnumMapping> _enums;

	private int _nextIdNumber;

	internal Dictionary<string, EnumMapping> Enums
	{
		get
		{
			if (_enums == null)
			{
				_enums = new Dictionary<string, EnumMapping>();
			}
			return _enums;
		}
	}

	[RequiresUnreferencedCode("Creates XmlSerializationILGen")]
	internal XmlSerializationReaderILGen(TypeScope[] scopes, string access, string className)
		: base(scopes, access, className)
	{
	}

	[RequiresUnreferencedCode("calls WriteReflectionInit")]
	internal void GenerateBegin()
	{
		typeBuilder = CodeGenerator.CreateTypeBuilder(base.ModuleBuilder, base.ClassName, base.TypeAttributes | TypeAttributes.BeforeFieldInit, typeof(XmlSerializationReader), Type.EmptyTypes);
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (TypeMapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping is StructMapping || typeMapping is EnumMapping || typeMapping is NullableMapping)
				{
					base.MethodNames.Add(typeMapping, NextMethodName(typeMapping.TypeDesc.Name));
				}
			}
			base.RaCodeGen.WriteReflectionInit(typeScope);
		}
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	internal override void GenerateMethod(TypeMapping mapping)
	{
		if (base.GeneratedMethods.Add(mapping))
		{
			if (mapping is StructMapping)
			{
				WriteStructMethod((StructMapping)mapping);
			}
			else if (mapping is EnumMapping)
			{
				WriteEnumMethod((EnumMapping)mapping);
			}
			else if (mapping is NullableMapping)
			{
				WriteNullableMethod((NullableMapping)mapping);
			}
		}
	}

	[RequiresUnreferencedCode("calls GenerateReferencedMethods")]
	internal void GenerateEnd(string[] methods, XmlMapping[] xmlMappings, Type[] types)
	{
		GenerateReferencedMethods();
		GenerateInitCallbacksMethod();
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(void), "InitIDs", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("get_NameTable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method3 = typeof(XmlNameTable).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
		foreach (string key in _idNames.Keys)
		{
			ilg.Ldarg(0);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method2);
			ilg.Ldstr(GetCSharpString(key));
			ilg.Call(method3);
			ilg.StoreMember(_idNameFields[key]);
		}
		ilg.EndMethod();
		typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.HideBySig);
		Type type = typeBuilder.CreateTypeInfo().AsType();
		CreatedTypes.Add(type.Name, type);
	}

	[RequiresUnreferencedCode("calls GenerateMembersElement")]
	internal string GenerateElement(XmlMapping xmlMapping)
	{
		if (!xmlMapping.IsReadable)
		{
			return null;
		}
		if (!xmlMapping.GenerateSerializer)
		{
			throw new ArgumentException(System.SR.XmlInternalError, "xmlMapping");
		}
		if (xmlMapping is XmlTypeMapping)
		{
			return GenerateTypeElement((XmlTypeMapping)xmlMapping);
		}
		if (xmlMapping is XmlMembersMapping)
		{
			return GenerateMembersElement((XmlMembersMapping)xmlMapping);
		}
		throw new ArgumentException(System.SR.XmlInternalError, "xmlMapping");
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	private void WriteIsStartTag(string name, string ns)
	{
		WriteID(name);
		WriteID(ns);
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("IsStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
		{
			typeof(string),
			typeof(string)
		});
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Ldarg(0);
		ilg.LoadMember(_idNameFields[name ?? string.Empty]);
		ilg.Ldarg(0);
		ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
		ilg.Call(method2);
		ilg.If();
	}

	[RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
	private void WriteUnknownNode(string func, string node, ElementAccessor e, bool anyIfs)
	{
		if (anyIfs)
		{
			ilg.Else();
		}
		List<Type> list = new List<Type>();
		ilg.Ldarg(0);
		if (node == "null")
		{
			ilg.Load(null);
		}
		else
		{
			object variable = ilg.GetVariable("p");
			ilg.Load(variable);
			ilg.ConvertValue(ilg.GetVariableType(variable), typeof(object));
		}
		list.Add(typeof(object));
		if (e != null)
		{
			string text = ((e.Form == XmlSchemaForm.Qualified) ? e.Namespace : "");
			text += ":";
			text += e.Name;
			ilg.Ldstr(ReflectionAwareILGen.GetCSharpString(text));
			list.Add(typeof(string));
		}
		MethodInfo method = typeof(XmlSerializationReader).GetMethod(func, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, list.ToArray());
		ilg.Call(method);
		if (anyIfs)
		{
			ilg.EndIf();
		}
	}

	private void GenerateInitCallbacksMethod()
	{
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(void), "InitCallbacks", Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls GenerateLiteralMembersElement")]
	private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		return GenerateLiteralMembersElement(xmlMembersMapping);
	}

	private string GetChoiceIdentifierSource(MemberMapping[] mappings, MemberMapping member)
	{
		string result = null;
		if (member.ChoiceIdentifier != null)
		{
			for (int i = 0; i < mappings.Length; i++)
			{
				if (mappings[i].Name == member.ChoiceIdentifier.MemberName)
				{
					result = $"p[{i}]";
					break;
				}
			}
		}
		return result;
	}

	private string GetChoiceIdentifierSource(MemberMapping mapping, string parent, TypeDesc parentTypeDesc)
	{
		if (mapping.ChoiceIdentifier == null)
		{
			return "";
		}
		CodeIdentifier.CheckValidIdentifier(mapping.ChoiceIdentifier.MemberName);
		return base.RaCodeGen.GetStringForMember(parent, mapping.ChoiceIdentifier.MemberName, parentTypeDesc);
	}

	[RequiresUnreferencedCode("calls InitializeValueTypes")]
	private string GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MemberMapping[] members = ((MembersMapping)accessor.Mapping).Members;
		bool hasWrapperElement = ((MembersMapping)accessor.Mapping).HasWrapperElement;
		string text = NextMethodName(accessor.Name);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(object[]), text, Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.HideBySig);
		ilg.Load(null);
		ilg.Stloc(ilg.ReturnLocal);
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("MoveToContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Pop();
		LocalBuilder localBuilder = ilg.DeclareLocal(typeof(object[]), "p");
		ilg.NewArray(typeof(object), members.Length);
		ilg.Stloc(localBuilder);
		InitializeValueTypes("p", members);
		if (hasWrapperElement)
		{
			WriteWhileNotLoopStart();
			WriteIsStartTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
		}
		Member anyText = null;
		Member anyElement = null;
		Member anyAttribute = null;
		List<Member> list = new List<Member>();
		List<Member> list2 = new List<Member>();
		List<Member> list3 = new List<Member>();
		for (int i = 0; i < members.Length; i++)
		{
			MemberMapping memberMapping = members[i];
			string text2 = $"p[{i}]";
			string arraySource = text2;
			if (memberMapping.Xmlns != null)
			{
				arraySource = $"(({memberMapping.TypeDesc.CSharpName}){text2})";
			}
			string choiceIdentifierSource = GetChoiceIdentifierSource(members, memberMapping);
			Member member = new Member(this, text2, arraySource, "a", i, memberMapping, choiceIdentifierSource);
			Member member2 = new Member(this, text2, null, "a", i, memberMapping, choiceIdentifierSource);
			if (!memberMapping.IsSequence)
			{
				member.ParamsReadSource = $"paramsRead[{i}]";
			}
			if (memberMapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
			{
				string text3 = memberMapping.Name + "Specified";
				for (int j = 0; j < members.Length; j++)
				{
					if (members[j].Name == text3)
					{
						member.CheckSpecifiedSource = $"p[{j}]";
						break;
					}
				}
			}
			bool flag = false;
			if (memberMapping.Text != null)
			{
				anyText = member2;
			}
			if (memberMapping.Attribute != null && memberMapping.Attribute.Any)
			{
				anyAttribute = member2;
			}
			if (memberMapping.Attribute != null || memberMapping.Xmlns != null)
			{
				list3.Add(member);
			}
			else if (memberMapping.Text != null)
			{
				list2.Add(member);
			}
			if (!memberMapping.IsSequence)
			{
				for (int k = 0; k < memberMapping.Elements.Length; k++)
				{
					if (memberMapping.Elements[k].Any && memberMapping.Elements[k].Name.Length == 0)
					{
						anyElement = member2;
						if (memberMapping.Attribute == null && memberMapping.Text == null)
						{
							list2.Add(member2);
						}
						flag = true;
						break;
					}
				}
			}
			if (memberMapping.Attribute != null || memberMapping.Text != null || flag)
			{
				list.Add(member2);
				continue;
			}
			if (memberMapping.TypeDesc.IsArrayLike && (memberMapping.Elements.Length != 1 || !(memberMapping.Elements[0].Mapping is ArrayMapping)))
			{
				list.Add(member2);
				list2.Add(member2);
				continue;
			}
			if (memberMapping.TypeDesc.IsArrayLike && !memberMapping.TypeDesc.IsArray)
			{
				member.ParamsReadSource = null;
			}
			list.Add(member);
		}
		Member[] array = list.ToArray();
		Member[] members2 = list2.ToArray();
		if (array.Length != 0 && array[0].Mapping.IsReturnValue)
		{
			MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("set_IsReturnValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(bool) });
			ilg.Ldarg(0);
			ilg.Ldc(boolVar: true);
			ilg.Call(method3);
		}
		WriteParamsRead(members.Length);
		if (list3.Count > 0)
		{
			Member[] members3 = list3.ToArray();
			WriteMemberBegin(members3);
			WriteAttributes(members3, anyAttribute, "UnknownNode", localBuilder);
			WriteMemberEnd(members3);
			MethodInfo method4 = typeof(XmlReader).GetMethod("MoveToElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method4);
			ilg.Pop();
		}
		WriteMemberBegin(members2);
		if (hasWrapperElement)
		{
			MethodInfo method5 = typeof(XmlReader).GetMethod("get_IsEmptyElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method5);
			ilg.If();
			MethodInfo method6 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method6);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method2);
			ilg.Pop();
			ilg.WhileContinue();
			ilg.EndIf();
			MethodInfo method7 = typeof(XmlReader).GetMethod("ReadStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method7);
		}
		if (IsSequence(array))
		{
			ilg.Ldc(0);
			ilg.Stloc(typeof(int), "state");
		}
		WriteWhileNotLoopStart();
		string text4 = "UnknownNode((object)p, " + ExpectedElements(array) + ");";
		WriteMemberElements(array, text4, text4, anyElement, anyText);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Pop();
		WriteWhileLoopEnd();
		WriteMemberEnd(members2);
		if (hasWrapperElement)
		{
			MethodInfo method8 = typeof(XmlSerializationReader).GetMethod("ReadEndElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method8);
			WriteUnknownNode("UnknownNode", "null", accessor, anyIfs: true);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method2);
			ilg.Pop();
			WriteWhileLoopEnd();
		}
		ilg.Ldloc(ilg.GetLocal("p"));
		ilg.EndMethod();
		return text;
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void InitializeValueTypes(string arrayName, MemberMapping[] mappings)
	{
		for (int i = 0; i < mappings.Length; i++)
		{
			if (mappings[i].TypeDesc.IsValueType)
			{
				LocalBuilder local = ilg.GetLocal(arrayName);
				ilg.Ldloc(local);
				ilg.Ldc(i);
				base.RaCodeGen.ILGenForCreateInstance(ilg, mappings[i].TypeDesc.Type, ctorInaccessible: false, cast: false);
				ilg.ConvertValue(mappings[i].TypeDesc.Type, typeof(object));
				ilg.Stelem(local.LocalType.GetElementType());
			}
		}
	}

	[RequiresUnreferencedCode("calls WriteMemberElements")]
	private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
	{
		ElementAccessor accessor = xmlTypeMapping.Accessor;
		TypeMapping mapping = accessor.Mapping;
		string text = NextMethodName(accessor.Name);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(object), text, Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Public | MethodAttributes.HideBySig);
		LocalBuilder localBuilder = ilg.DeclareLocal(typeof(object), "o");
		ilg.Load(null);
		ilg.Stloc(localBuilder);
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.TypeDesc = mapping.TypeDesc;
		memberMapping.Elements = new ElementAccessor[1] { accessor };
		Member[] array = new Member[1]
		{
			new Member(this, "o", "o", "a", 0, memberMapping)
		};
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("MoveToContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Pop();
		string elseString = "UnknownNode(null, " + ExpectedElements(array) + ");";
		WriteMemberElements(array, "throw CreateUnknownNodeException();", elseString, accessor.Any ? array[0] : null, null);
		ilg.Ldloc(localBuilder);
		ilg.Stloc(ilg.ReturnLocal);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
		return text;
	}

	private string NextMethodName(string name)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(5, 2, invariantCulture);
		handler.AppendLiteral("Read");
		handler.AppendFormatted(++base.NextMethodNumber);
		handler.AppendLiteral("_");
		handler.AppendFormatted(CodeIdentifier.MakeValidInternal(name));
		return string.Create(invariantCulture, ref handler);
	}

	private string NextIdName(string name)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(3, 2, invariantCulture);
		handler.AppendLiteral("id");
		handler.AppendFormatted(++_nextIdNumber);
		handler.AppendLiteral("_");
		handler.AppendFormatted(CodeIdentifier.MakeValidInternal(name));
		return string.Create(invariantCulture, ref handler);
	}

	[RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
	private void WritePrimitive(TypeMapping mapping, string source)
	{
		if (mapping is EnumMapping)
		{
			string text = ReferenceMapping(mapping);
			if (text == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingMethodEnum, mapping.TypeDesc.Name));
			}
			MethodBuilder methodInfo = EnsureMethodBuilder(typeBuilder, text, MethodAttributes.Private | MethodAttributes.HideBySig, mapping.TypeDesc.Type, new Type[1] { typeof(string) });
			ilg.Ldarg(0);
			switch (source)
			{
			case "Reader.ReadElementString()":
			case "Reader.ReadString()":
			{
				MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method4 = typeof(XmlReader).GetMethod((source == "Reader.ReadElementString()") ? "ReadElementContentAsString" : "ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method3);
				ilg.Call(method4);
				break;
			}
			case "Reader.Value":
			{
				MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method2 = typeof(XmlReader).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method);
				ilg.Call(method2);
				break;
			}
			case "vals[i]":
			{
				LocalBuilder local = ilg.GetLocal("vals");
				LocalBuilder local2 = ilg.GetLocal("i");
				ilg.LoadArrayElement(local, local2);
				break;
			}
			case "false":
				ilg.Ldc(boolVar: false);
				break;
			default:
				throw Globals.NotSupported("Unexpected: " + source);
			}
			ilg.Call(methodInfo);
			return;
		}
		if (mapping.TypeDesc == base.StringTypeDesc)
		{
			switch (source)
			{
			case "Reader.ReadElementString()":
			case "Reader.ReadString()":
			{
				MethodInfo method7 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method8 = typeof(XmlReader).GetMethod((source == "Reader.ReadElementString()") ? "ReadElementContentAsString" : "ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method7);
				ilg.Call(method8);
				break;
			}
			case "Reader.Value":
			{
				MethodInfo method5 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method6 = typeof(XmlReader).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method5);
				ilg.Call(method6);
				break;
			}
			case "vals[i]":
			{
				LocalBuilder local3 = ilg.GetLocal("vals");
				LocalBuilder local4 = ilg.GetLocal("i");
				ilg.LoadArrayElement(local3, local4);
				break;
			}
			default:
				throw Globals.NotSupported("Unexpected: " + source);
			}
			return;
		}
		if (mapping.TypeDesc.FormatterName == "String")
		{
			if (source == "vals[i]")
			{
				if (mapping.TypeDesc.CollapseWhitespace)
				{
					ilg.Ldarg(0);
				}
				LocalBuilder local5 = ilg.GetLocal("vals");
				LocalBuilder local6 = ilg.GetLocal("i");
				ilg.LoadArrayElement(local5, local6);
				if (mapping.TypeDesc.CollapseWhitespace)
				{
					MethodInfo method9 = typeof(XmlSerializationReader).GetMethod("CollapseWhitespace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null);
					ilg.Call(method9);
				}
				return;
			}
			MethodInfo method10 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method11 = typeof(XmlReader).GetMethod((source == "Reader.Value") ? "get_Value" : "ReadElementContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			if (mapping.TypeDesc.CollapseWhitespace)
			{
				ilg.Ldarg(0);
			}
			ilg.Ldarg(0);
			ilg.Call(method10);
			ilg.Call(method11);
			if (mapping.TypeDesc.CollapseWhitespace)
			{
				MethodInfo method12 = typeof(XmlSerializationReader).GetMethod("CollapseWhitespace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
				ilg.Call(method12);
			}
			return;
		}
		Type type = ((source == "false") ? typeof(bool) : typeof(string));
		MethodInfo method13;
		if (mapping.TypeDesc.HasCustomFormatter)
		{
			BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			if ((mapping.TypeDesc.FormatterName == "ByteArrayBase64" && source == "false") || (mapping.TypeDesc.FormatterName == "ByteArrayHex" && source == "false") || mapping.TypeDesc.FormatterName == "XmlQualifiedName")
			{
				bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				ilg.Ldarg(0);
			}
			method13 = typeof(XmlSerializationReader).GetMethod("To" + mapping.TypeDesc.FormatterName, bindingAttr, new Type[1] { type });
		}
		else
		{
			method13 = typeof(XmlConvert).GetMethod("To" + mapping.TypeDesc.FormatterName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { type });
		}
		switch (source)
		{
		case "Reader.ReadElementString()":
		case "Reader.ReadString()":
		{
			MethodInfo method16 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method17 = typeof(XmlReader).GetMethod((source == "Reader.ReadElementString()") ? "ReadElementContentAsString" : "ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method16);
			ilg.Call(method17);
			break;
		}
		case "Reader.Value":
		{
			MethodInfo method14 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method15 = typeof(XmlReader).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method14);
			ilg.Call(method15);
			break;
		}
		case "vals[i]":
		{
			LocalBuilder local7 = ilg.GetLocal("vals");
			LocalBuilder local8 = ilg.GetLocal("i");
			ilg.LoadArrayElement(local7, local8);
			break;
		}
		default:
			ilg.Ldc(boolVar: false);
			break;
		}
		ilg.Call(method13);
	}

	private string MakeUnique(EnumMapping mapping, string name)
	{
		string text = name;
		if (Enums.TryGetValue(text, out var value))
		{
			if (value == mapping)
			{
				return null;
			}
			int num = 0;
			while (value != null)
			{
				num++;
				text = name + num.ToString(CultureInfo.InvariantCulture);
				Enums.TryGetValue(text, out value);
			}
		}
		Enums.Add(text, mapping);
		return text;
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	private string WriteHashtable(EnumMapping mapping, string typeName, out MethodBuilder get_TableName)
	{
		get_TableName = null;
		CodeIdentifier.CheckValidIdentifier(typeName);
		string text = MakeUnique(mapping, typeName + "Values");
		if (text == null)
		{
			return CodeIdentifier.GetCSharpName(typeName);
		}
		string fieldName = MakeUnique(mapping, "_" + text);
		text = CodeIdentifier.GetCSharpName(text);
		FieldBuilder memberInfo = typeBuilder.DefineField(fieldName, typeof(Hashtable), FieldAttributes.Private);
		PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(text, PropertyAttributes.None, CallingConventions.HasThis, typeof(Hashtable), null, null, null, null, null);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(typeof(Hashtable), "get_" + text, Type.EmptyTypes, Array.Empty<string>(), MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName);
		ilg.Ldarg(0);
		ilg.LoadMember(memberInfo);
		ilg.Load(null);
		ilg.If(Cmp.EqualTo);
		ConstructorInfo constructor = typeof(Hashtable).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		LocalBuilder localBuilder = ilg.DeclareLocal(typeof(Hashtable), "h");
		ilg.New(constructor);
		ilg.Stloc(localBuilder);
		ConstantMapping[] constants = mapping.Constants;
		MethodInfo method = typeof(Hashtable).GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
		{
			typeof(object),
			typeof(object)
		});
		for (int i = 0; i < constants.Length; i++)
		{
			ilg.Ldloc(localBuilder);
			ilg.Ldstr(GetCSharpString(constants[i].XmlName));
			ilg.Ldc(Enum.ToObject(mapping.TypeDesc.Type, constants[i].Value));
			ilg.ConvertValue(mapping.TypeDesc.Type, typeof(long));
			ilg.ConvertValue(typeof(long), typeof(object));
			ilg.Call(method);
		}
		ilg.Ldarg(0);
		ilg.Ldloc(localBuilder);
		ilg.StoreMember(memberInfo);
		ilg.EndIf();
		ilg.Ldarg(0);
		ilg.LoadMember(memberInfo);
		get_TableName = ilg.EndMethod();
		propertyBuilder.SetGetMethod(get_TableName);
		return text;
	}

	[RequiresUnreferencedCode("calls WriteHashtable")]
	private void WriteEnumMethod(EnumMapping mapping)
	{
		MethodBuilder get_TableName = null;
		if (mapping.IsFlags)
		{
			WriteHashtable(mapping, mapping.TypeDesc.Name, out get_TableName);
		}
		base.MethodNames.TryGetValue(mapping, out var value);
		string cSharpName = mapping.TypeDesc.CSharpName;
		List<Type> list = new List<Type>();
		List<string> list2 = new List<string>();
		Type type = mapping.TypeDesc.Type;
		Type underlyingType = Enum.GetUnderlyingType(type);
		list.Add(typeof(string));
		list2.Add("s");
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(type, GetMethodBuilder(value), list.ToArray(), list2.ToArray(), MethodAttributes.Private | MethodAttributes.HideBySig);
		ConstantMapping[] constants = mapping.Constants;
		if (mapping.IsFlags)
		{
			MethodInfo method = typeof(XmlSerializationReader).GetMethod("ToEnum", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
			{
				typeof(string),
				typeof(Hashtable),
				typeof(string)
			});
			ilg.Ldarg("s");
			ilg.Ldarg(0);
			ilg.Call(get_TableName);
			ilg.Ldstr(GetCSharpString(cSharpName));
			ilg.Call(method);
			if (underlyingType != typeof(long))
			{
				ilg.ConvertValue(typeof(long), underlyingType);
			}
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
		}
		else
		{
			List<Label> list3 = new List<Label>();
			List<object> list4 = new List<object>();
			Label label = ilg.DefineLabel();
			Label label2 = ilg.DefineLabel();
			LocalBuilder tempLocal = ilg.GetTempLocal(typeof(string));
			ilg.Ldarg("s");
			ilg.Stloc(tempLocal);
			ilg.Ldloc(tempLocal);
			ilg.Brfalse(label);
			HashSet<string> hashSet = new HashSet<string>();
			foreach (ConstantMapping constantMapping in constants)
			{
				CodeIdentifier.CheckValidIdentifier(constantMapping.Name);
				if (hashSet.Add(constantMapping.XmlName))
				{
					Label label3 = ilg.DefineLabel();
					ilg.Ldloc(tempLocal);
					ilg.Ldstr(GetCSharpString(constantMapping.XmlName));
					MethodInfo method2 = typeof(string).GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
					{
						typeof(string),
						typeof(string)
					});
					ilg.Call(method2);
					ilg.Brtrue(label3);
					list3.Add(label3);
					list4.Add(Enum.ToObject(mapping.TypeDesc.Type, constantMapping.Value));
				}
			}
			ilg.Br(label);
			for (int j = 0; j < list3.Count; j++)
			{
				ilg.MarkLabel(list3[j]);
				ilg.Ldc(list4[j]);
				ilg.Stloc(ilg.ReturnLocal);
				ilg.Br(ilg.ReturnLabel);
			}
			MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("CreateUnknownConstantException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(Type)
			});
			ilg.MarkLabel(label);
			ilg.Ldarg(0);
			ilg.Ldarg("s");
			ilg.Ldc(mapping.TypeDesc.Type);
			ilg.Call(method3);
			ilg.Throw();
			ilg.MarkLabel(label2);
		}
		ilg.MarkLabel(ilg.ReturnLabel);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls WriteQNameEqual")]
	private void WriteDerivedTypes(StructMapping mapping, bool isTypedReturn, string returnTypeName)
	{
		for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
		{
			ilg.InitElseIf();
			WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
			ilg.AndIf();
			string methodName = ReferenceMapping(structMapping);
			List<Type> list = new List<Type>();
			ilg.Ldarg(0);
			if (structMapping.TypeDesc.IsNullable)
			{
				ilg.Ldarg("isNullable");
				list.Add(typeof(bool));
			}
			ilg.Ldc(boolVar: false);
			list.Add(typeof(bool));
			MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder, methodName, MethodAttributes.Private | MethodAttributes.HideBySig, structMapping.TypeDesc.Type, list.ToArray());
			ilg.Call(methodBuilder);
			ilg.ConvertValue(methodBuilder.ReturnType, ilg.ReturnLocal.LocalType);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
			WriteDerivedTypes(structMapping, isTypedReturn, returnTypeName);
		}
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void WriteEnumAndArrayTypes()
	{
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (Mapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping is EnumMapping)
				{
					EnumMapping enumMapping = (EnumMapping)typeMapping;
					ilg.InitElseIf();
					WriteQNameEqual("xsiType", enumMapping.TypeName, enumMapping.Namespace);
					ilg.AndIf();
					MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					MethodInfo method2 = typeof(XmlReader).GetMethod("ReadStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.Ldarg(0);
					ilg.Call(method);
					ilg.Call(method2);
					string methodName = ReferenceMapping(enumMapping);
					LocalBuilder localBuilder = ilg.DeclareOrGetLocal(typeof(object), "e");
					MethodBuilder methodBuilder = EnsureMethodBuilder(typeBuilder, methodName, MethodAttributes.Private | MethodAttributes.HideBySig, enumMapping.TypeDesc.Type, new Type[1] { typeof(string) });
					MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("CollapseWhitespace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
					MethodInfo method4 = typeof(XmlReader).GetMethod("ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.Ldarg(0);
					ilg.Ldarg(0);
					ilg.Ldarg(0);
					ilg.Call(method);
					ilg.Call(method4);
					ilg.Call(method3);
					ilg.Call(methodBuilder);
					ilg.ConvertValue(methodBuilder.ReturnType, localBuilder.LocalType);
					ilg.Stloc(localBuilder);
					MethodInfo method5 = typeof(XmlSerializationReader).GetMethod("ReadEndElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.Ldarg(0);
					ilg.Call(method5);
					ilg.Ldloc(localBuilder);
					ilg.Stloc(ilg.ReturnLocal);
					ilg.Br(ilg.ReturnLabel);
				}
				else
				{
					if (!(typeMapping is ArrayMapping))
					{
						continue;
					}
					ArrayMapping arrayMapping = (ArrayMapping)typeMapping;
					if (arrayMapping.TypeDesc.HasDefaultConstructor)
					{
						ilg.InitElseIf();
						WriteQNameEqual("xsiType", arrayMapping.TypeName, arrayMapping.Namespace);
						ilg.AndIf();
						ilg.EnterScope();
						MemberMapping memberMapping = new MemberMapping();
						memberMapping.TypeDesc = arrayMapping.TypeDesc;
						memberMapping.Elements = arrayMapping.Elements;
						string text = "a";
						string arrayName = "z";
						Member member = new Member(this, text, arrayName, 0, memberMapping);
						TypeDesc typeDesc = arrayMapping.TypeDesc;
						LocalBuilder localBuilder2 = ilg.DeclareLocal(arrayMapping.TypeDesc.Type, text);
						if (arrayMapping.TypeDesc.IsValueType)
						{
							base.RaCodeGen.ILGenForCreateInstance(ilg, typeDesc.Type, ctorInaccessible: false, cast: false);
						}
						else
						{
							ilg.Load(null);
						}
						ilg.Stloc(localBuilder2);
						WriteArray(member.Source, member.ArrayName, arrayMapping, readOnly: false, isNullable: false, -1, 0);
						ilg.Ldloc(localBuilder2);
						ilg.Stloc(ilg.ReturnLocal);
						ilg.Br(ilg.ReturnLabel);
						ilg.ExitScope();
					}
				}
			}
		}
	}

	[RequiresUnreferencedCode("calls WriteElement")]
	private void WriteNullableMethod(NullableMapping nullableMapping)
	{
		base.MethodNames.TryGetValue(nullableMapping, out var value);
		ilg = new CodeGenerator(typeBuilder);
		ilg.BeginMethod(nullableMapping.TypeDesc.Type, GetMethodBuilder(value), new Type[1] { typeof(bool) }, new string[1] { "checkType" }, MethodAttributes.Private | MethodAttributes.HideBySig);
		LocalBuilder localBuilder = ilg.DeclareLocal(nullableMapping.TypeDesc.Type, "o");
		ilg.LoadAddress(localBuilder);
		ilg.InitObj(nullableMapping.TypeDesc.Type);
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("ReadNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.If();
		ilg.Ldloc(localBuilder);
		ilg.Stloc(ilg.ReturnLocal);
		ilg.Br(ilg.ReturnLabel);
		ilg.EndIf();
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Mapping = nullableMapping.BaseMapping;
		elementAccessor.Any = false;
		elementAccessor.IsNullable = nullableMapping.BaseMapping.TypeDesc.IsNullable;
		WriteElement("o", null, null, elementAccessor, null, null, checkForNull: false, readOnly: false, -1, -1);
		ilg.Ldloc(localBuilder);
		ilg.Stloc(ilg.ReturnLocal);
		ilg.Br(ilg.ReturnLabel);
		ilg.MarkLabel(ilg.ReturnLabel);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls WriteLiteralStructMethod")]
	private void WriteStructMethod(StructMapping structMapping)
	{
		WriteLiteralStructMethod(structMapping);
	}

	[RequiresUnreferencedCode("calls WriteEnumAndArrayTypes")]
	private void WriteLiteralStructMethod(StructMapping structMapping)
	{
		base.MethodNames.TryGetValue(structMapping, out var value);
		string cSharpName = structMapping.TypeDesc.CSharpName;
		ilg = new CodeGenerator(typeBuilder);
		List<Type> list = new List<Type>();
		List<string> list2 = new List<string>();
		if (structMapping.TypeDesc.IsNullable)
		{
			list.Add(typeof(bool));
			list2.Add("isNullable");
		}
		list.Add(typeof(bool));
		list2.Add("checkType");
		ilg.BeginMethod(structMapping.TypeDesc.Type, GetMethodBuilder(value), list.ToArray(), list2.ToArray(), MethodAttributes.Private | MethodAttributes.HideBySig);
		LocalBuilder localBuilder = ilg.DeclareLocal(typeof(XmlQualifiedName), "xsiType");
		LocalBuilder localBuilder2 = ilg.DeclareLocal(typeof(bool), "isNull");
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("GetXsiType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("ReadNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		ilg.Ldarg("checkType");
		ilg.Brtrue(label);
		ilg.Load(null);
		ilg.Br_S(label2);
		ilg.MarkLabel(label);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.MarkLabel(label2);
		ilg.Stloc(localBuilder);
		ilg.Ldc(boolVar: false);
		ilg.Stloc(localBuilder2);
		if (structMapping.TypeDesc.IsNullable)
		{
			ilg.Ldarg("isNullable");
			ilg.If();
			ilg.Ldarg(0);
			ilg.Call(method2);
			ilg.Stloc(localBuilder2);
			ilg.EndIf();
		}
		ilg.Ldarg("checkType");
		ilg.If();
		if (structMapping.TypeDesc.IsRoot)
		{
			ilg.Ldloc(localBuilder2);
			ilg.If();
			ilg.Ldloc(localBuilder);
			ilg.Load(null);
			ilg.If(Cmp.NotEqualTo);
			MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("ReadTypedNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { localBuilder.LocalType });
			ilg.Ldarg(0);
			ilg.Ldloc(localBuilder);
			ilg.Call(method3);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
			ilg.Else();
			if (structMapping.TypeDesc.IsValueType)
			{
				throw Globals.NotSupported(System.SR.Arg_NeverValueType);
			}
			ilg.Load(null);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
			ilg.EndIf();
			ilg.EndIf();
		}
		ilg.Ldloc(typeof(XmlQualifiedName), "xsiType");
		ilg.Load(null);
		ilg.Ceq();
		if (!structMapping.TypeDesc.IsRoot)
		{
			label = ilg.DefineLabel();
			label2 = ilg.DefineLabel();
			ilg.Brtrue(label);
			WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
			ilg.Br_S(label2);
			ilg.MarkLabel(label);
			ilg.Ldc(boolVar: true);
			ilg.MarkLabel(label2);
		}
		ilg.If();
		if (structMapping.TypeDesc.IsRoot)
		{
			ConstructorInfo constructor = typeof(XmlQualifiedName).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(string)
			});
			MethodInfo method4 = typeof(XmlSerializationReader).GetMethod("ReadTypedPrimitive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(XmlQualifiedName) });
			ilg.Ldarg(0);
			ilg.Ldstr("anyType");
			ilg.Ldstr("http://www.w3.org/2001/XMLSchema");
			ilg.New(constructor);
			ilg.Call(method4);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
		}
		WriteDerivedTypes(structMapping, !structMapping.TypeDesc.IsRoot, cSharpName);
		if (structMapping.TypeDesc.IsRoot)
		{
			WriteEnumAndArrayTypes();
		}
		ilg.Else();
		if (structMapping.TypeDesc.IsRoot)
		{
			MethodInfo method5 = typeof(XmlSerializationReader).GetMethod("ReadTypedPrimitive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { localBuilder.LocalType });
			ilg.Ldarg(0);
			ilg.Ldloc(localBuilder);
			ilg.Call(method5);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
		}
		else
		{
			MethodInfo method6 = typeof(XmlSerializationReader).GetMethod("CreateUnknownTypeException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(XmlQualifiedName) });
			ilg.Ldarg(0);
			ilg.Ldloc(localBuilder);
			ilg.Call(method6);
			ilg.Throw();
		}
		ilg.EndIf();
		ilg.EndIf();
		if (structMapping.TypeDesc.IsNullable)
		{
			ilg.Ldloc(typeof(bool), "isNull");
			ilg.If();
			ilg.Load(null);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
			ilg.EndIf();
		}
		if (structMapping.TypeDesc.IsAbstract)
		{
			MethodInfo method7 = typeof(XmlSerializationReader).GetMethod("CreateAbstractTypeException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(string)
			});
			ilg.Ldarg(0);
			ilg.Ldstr(GetCSharpString(structMapping.TypeName));
			ilg.Ldstr(GetCSharpString(structMapping.Namespace));
			ilg.Call(method7);
			ilg.Throw();
		}
		else
		{
			if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
			{
				MethodInfo method8 = typeof(XmlSerializationReader).GetMethod("set_DecodeName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(bool) });
				ilg.Ldarg(0);
				ilg.Ldc(boolVar: false);
				ilg.Call(method8);
			}
			WriteCreateMapping(structMapping, "o");
			LocalBuilder local = ilg.GetLocal("o");
			MemberMapping[] settableMembers = TypeScope.GetSettableMembers(structMapping, memberInfos);
			Member member = null;
			Member member2 = null;
			Member member3 = null;
			bool flag = structMapping.HasExplicitSequence();
			List<Member> list3 = new List<Member>(settableMembers.Length);
			List<Member> list4 = new List<Member>(settableMembers.Length);
			List<Member> list5 = new List<Member>(settableMembers.Length);
			for (int i = 0; i < settableMembers.Length; i++)
			{
				MemberMapping memberMapping = settableMembers[i];
				CodeIdentifier.CheckValidIdentifier(memberMapping.Name);
				string stringForMember = base.RaCodeGen.GetStringForMember("o", memberMapping.Name, structMapping.TypeDesc);
				Member member4 = new Member(this, stringForMember, "a", i, memberMapping, GetChoiceIdentifierSource(memberMapping, "o", structMapping.TypeDesc));
				if (!memberMapping.IsSequence)
				{
					member4.ParamsReadSource = $"paramsRead[{i}]";
				}
				member4.IsNullable = memberMapping.TypeDesc.IsNullable;
				if (memberMapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
				{
					member4.CheckSpecifiedSource = base.RaCodeGen.GetStringForMember("o", memberMapping.Name + "Specified", structMapping.TypeDesc);
				}
				if (memberMapping.Text != null)
				{
					member = member4;
				}
				if (memberMapping.Attribute != null && memberMapping.Attribute.Any)
				{
					member3 = member4;
				}
				if (!flag)
				{
					for (int j = 0; j < memberMapping.Elements.Length; j++)
					{
						if (memberMapping.Elements[j].Any && (memberMapping.Elements[j].Name == null || memberMapping.Elements[j].Name.Length == 0))
						{
							member2 = member4;
							break;
						}
					}
				}
				else if (memberMapping.IsParticle && !memberMapping.IsSequence)
				{
					structMapping.FindDeclaringMapping(memberMapping, out var declaringMapping, structMapping.TypeName);
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlSequenceHierarchy, structMapping.TypeDesc.FullName, memberMapping.Name, declaringMapping.TypeDesc.FullName, "Order"));
				}
				if (memberMapping.Attribute == null && memberMapping.Elements.Length == 1 && memberMapping.Elements[0].Mapping is ArrayMapping)
				{
					Member member5 = new Member(this, stringForMember, stringForMember, "a", i, memberMapping, GetChoiceIdentifierSource(memberMapping, "o", structMapping.TypeDesc));
					member5.CheckSpecifiedSource = member4.CheckSpecifiedSource;
					list5.Add(member5);
				}
				else
				{
					list5.Add(member4);
				}
				if (!memberMapping.TypeDesc.IsArrayLike)
				{
					continue;
				}
				list3.Add(member4);
				if (memberMapping.TypeDesc.IsArrayLike && (memberMapping.Elements.Length != 1 || !(memberMapping.Elements[0].Mapping is ArrayMapping)))
				{
					member4.ParamsReadSource = null;
					if (member4 != member && member4 != member2)
					{
						list4.Add(member4);
					}
				}
				else if (!memberMapping.TypeDesc.IsArray)
				{
					member4.ParamsReadSource = null;
				}
			}
			if (member2 != null)
			{
				list4.Add(member2);
			}
			if (member != null && member != member2)
			{
				list4.Add(member);
			}
			Member[] members = list3.ToArray();
			Member[] members2 = list4.ToArray();
			Member[] members3 = list5.ToArray();
			WriteMemberBegin(members);
			WriteParamsRead(settableMembers.Length);
			WriteAttributes(members3, member3, "UnknownNode", local);
			if (member3 != null)
			{
				WriteMemberEnd(members);
			}
			MethodInfo method9 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method10 = typeof(XmlReader).GetMethod("MoveToElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method9);
			ilg.Call(method10);
			ilg.Pop();
			MethodInfo method11 = typeof(XmlReader).GetMethod("get_IsEmptyElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method9);
			ilg.Call(method11);
			ilg.If();
			MethodInfo method12 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method9);
			ilg.Call(method12);
			WriteMemberEnd(members2);
			ilg.Ldloc(local);
			ilg.Stloc(ilg.ReturnLocal);
			ilg.Br(ilg.ReturnLabel);
			ilg.EndIf();
			MethodInfo method13 = typeof(XmlReader).GetMethod("ReadStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method9);
			ilg.Call(method13);
			if (IsSequence(members3))
			{
				ilg.Ldc(0);
				ilg.Stloc(typeof(int), "state");
			}
			WriteWhileNotLoopStart();
			string text = "UnknownNode((object)o, " + ExpectedElements(members3) + ");";
			WriteMemberElements(members3, text, text, member2, member);
			MethodInfo method14 = typeof(XmlReader).GetMethod("MoveToContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method9);
			ilg.Call(method14);
			ilg.Pop();
			WriteWhileLoopEnd();
			WriteMemberEnd(members2);
			MethodInfo method15 = typeof(XmlSerializationReader).GetMethod("ReadEndElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method15);
			ilg.Ldloc(structMapping.TypeDesc.Type, "o");
			ilg.Stloc(ilg.ReturnLocal);
		}
		ilg.MarkLabel(ilg.ReturnLabel);
		ilg.Ldloc(ilg.ReturnLocal);
		ilg.EndMethod();
	}

	[RequiresUnreferencedCode("calls LoadMember")]
	private void WriteQNameEqual(string source, string name, string ns)
	{
		WriteID(name);
		WriteID(ns);
		MethodInfo method = typeof(XmlQualifiedName).GetMethod("get_Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlQualifiedName).GetMethod("get_Namespace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		LocalBuilder local = ilg.GetLocal(source);
		ilg.Ldloc(local);
		ilg.Call(method);
		ilg.Ldarg(0);
		ilg.LoadMember(_idNameFields[name ?? string.Empty]);
		ilg.Bne(label2);
		ilg.Ldloc(local);
		ilg.Call(method2);
		ilg.Ldarg(0);
		ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
		ilg.Ceq();
		ilg.Br_S(label);
		ilg.MarkLabel(label2);
		ilg.Ldc(boolVar: false);
		ilg.MarkLabel(label);
	}

	[RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
	private void WriteXmlNodeEqual(string source, string name, string ns)
	{
		WriteXmlNodeEqual(source, name, ns, doAndIf: true);
	}

	[RequiresUnreferencedCode("XmlSerializationReader methods have RequiresUnreferencedCode")]
	private void WriteXmlNodeEqual(string source, string name, string ns, bool doAndIf)
	{
		bool flag = string.IsNullOrEmpty(name);
		if (!flag)
		{
			WriteID(name);
		}
		WriteID(ns);
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_" + source, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("get_LocalName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method3 = typeof(XmlReader).GetMethod("get_NamespaceURI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		if (!flag)
		{
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method2);
			ilg.Ldarg(0);
			ilg.LoadMember(_idNameFields[name ?? string.Empty]);
			ilg.Bne(label);
		}
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method3);
		ilg.Ldarg(0);
		ilg.LoadMember(_idNameFields[ns ?? string.Empty]);
		ilg.Ceq();
		if (!flag)
		{
			ilg.Br_S(label2);
			ilg.MarkLabel(label);
			ilg.Ldc(boolVar: false);
			ilg.MarkLabel(label2);
		}
		if (doAndIf)
		{
			ilg.AndIf();
		}
	}

	private void WriteID(string name)
	{
		if (name == null)
		{
			name = "";
		}
		if (!_idNames.TryGetValue(name, out var value))
		{
			value = NextIdName(name);
			_idNames.Add(name, value);
			_idNameFields.Add(name, typeBuilder.DefineField(value, typeof(string), FieldAttributes.Private));
		}
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void WriteAttributes(Member[] members, Member anyAttribute, string elseCall, LocalBuilder firstParam)
	{
		int num = 0;
		Member member = null;
		List<AttributeAccessor> list = new List<AttributeAccessor>();
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("MoveToNextAttribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.WhileBegin();
		foreach (Member member2 in members)
		{
			if (member2.Mapping.Xmlns != null)
			{
				member = member2;
			}
			else
			{
				if (member2.Mapping.Ignore)
				{
					continue;
				}
				AttributeAccessor attribute = member2.Mapping.Attribute;
				if (attribute != null && !attribute.Any)
				{
					list.Add(attribute);
					if (num++ > 0)
					{
						ilg.InitElseIf();
					}
					else
					{
						ilg.InitIf();
					}
					if (member2.ParamsReadSource != null)
					{
						ILGenParamsReadSource(member2.ParamsReadSource);
						ilg.Ldc(boolVar: false);
						ilg.AndIf(Cmp.EqualTo);
					}
					if (attribute.IsSpecialXmlNamespace)
					{
						WriteXmlNodeEqual("Reader", attribute.Name, "http://www.w3.org/XML/1998/namespace");
					}
					else
					{
						WriteXmlNodeEqual("Reader", attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : "");
					}
					WriteAttribute(member2);
				}
			}
		}
		if (num > 0)
		{
			ilg.InitElseIf();
		}
		else
		{
			ilg.InitIf();
		}
		if (member != null)
		{
			MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("IsXmlnsAttribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
			MethodInfo method4 = typeof(XmlReader).GetMethod("get_Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method5 = typeof(XmlReader).GetMethod("get_LocalName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method6 = typeof(XmlReader).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method4);
			ilg.Call(method3);
			ilg.Ldc(boolVar: true);
			ilg.AndIf(Cmp.EqualTo);
			ILGenLoad(member.Source);
			ilg.Load(null);
			ilg.If(Cmp.EqualTo);
			WriteSourceBegin(member.Source);
			ConstructorInfo constructor = member.Mapping.TypeDesc.Type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.New(constructor);
			WriteSourceEnd(member.Source, member.Mapping.TypeDesc.Type);
			ilg.EndIf();
			Label label = ilg.DefineLabel();
			Label label2 = ilg.DefineLabel();
			MethodInfo method7 = member.Mapping.TypeDesc.Type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(string)
			});
			MethodInfo method8 = typeof(string).GetMethod("get_Length", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ILGenLoad(member.ArraySource, member.Mapping.TypeDesc.Type);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method4);
			ilg.Call(method8);
			ilg.Ldc(5);
			ilg.Beq(label);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method5);
			ilg.Br(label2);
			ilg.MarkLabel(label);
			ilg.Ldstr(string.Empty);
			ilg.MarkLabel(label2);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method6);
			ilg.Call(method7);
			ilg.Else();
		}
		else
		{
			MethodInfo method9 = typeof(XmlSerializationReader).GetMethod("IsXmlnsAttribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
			MethodInfo method10 = typeof(XmlReader).GetMethod("get_Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method10);
			ilg.Call(method9);
			ilg.Ldc(boolVar: false);
			ilg.AndIf(Cmp.EqualTo);
		}
		if (anyAttribute != null)
		{
			LocalBuilder localBuilder = ilg.DeclareOrGetLocal(typeof(XmlAttribute), "attr");
			MethodInfo method11 = typeof(XmlSerializationReader).GetMethod("get_Document", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method12 = typeof(XmlDocument).GetMethod("ReadNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(XmlReader) });
			ilg.Ldarg(0);
			ilg.Call(method11);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Call(method12);
			ilg.ConvertValue(method12.ReturnType, localBuilder.LocalType);
			ilg.Stloc(localBuilder);
			MethodInfo method13 = typeof(XmlSerializationReader).GetMethod("ParseWsdlArrayType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { localBuilder.LocalType });
			ilg.Ldarg(0);
			ilg.Ldloc(localBuilder);
			ilg.Call(method13);
			WriteAttribute(anyAttribute);
		}
		else
		{
			List<Type> list2 = new List<Type>();
			ilg.Ldarg(0);
			list2.Add(typeof(object));
			ilg.Ldloc(firstParam);
			ilg.ConvertValue(firstParam.LocalType, typeof(object));
			if (list.Count > 0)
			{
				string text = "";
				for (int j = 0; j < list.Count; j++)
				{
					AttributeAccessor attributeAccessor = list[j];
					if (j > 0)
					{
						text += ", ";
					}
					text += (attributeAccessor.IsSpecialXmlNamespace ? "http://www.w3.org/XML/1998/namespace" : (((attributeAccessor.Form == XmlSchemaForm.Qualified) ? attributeAccessor.Namespace : "") + ":" + attributeAccessor.Name));
				}
				list2.Add(typeof(string));
				ilg.Ldstr(text);
			}
			MethodInfo method14 = typeof(XmlSerializationReader).GetMethod(elseCall, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, list2.ToArray());
			ilg.Call(method14);
		}
		ilg.EndIf();
		ilg.WhileBeginCondition();
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.WhileEndCondition();
		ilg.WhileEnd();
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void WriteAttribute(Member member)
	{
		AttributeAccessor attribute = member.Mapping.Attribute;
		if (attribute.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)attribute.Mapping;
			if (specialMapping.TypeDesc.Kind == TypeKind.Attribute)
			{
				WriteSourceBegin(member.ArraySource);
				ilg.Ldloc("attr");
				WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc.Type : member.Mapping.TypeDesc.Type);
			}
			else
			{
				if (!specialMapping.TypeDesc.CanBeAttributeValue)
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				LocalBuilder local = ilg.GetLocal("attr");
				ilg.Ldloc(local);
				if (local.LocalType == typeof(XmlAttribute))
				{
					ilg.Load(null);
					ilg.Cne();
				}
				else
				{
					ilg.IsInst(typeof(XmlAttribute));
				}
				ilg.If();
				WriteSourceBegin(member.ArraySource);
				ilg.Ldloc(local);
				ilg.ConvertValue(local.LocalType, typeof(XmlAttribute));
				WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc.Type : member.Mapping.TypeDesc.Type);
				ilg.EndIf();
			}
		}
		else if (attribute.IsList)
		{
			LocalBuilder localBuilder = ilg.DeclareOrGetLocal(typeof(string), "listValues");
			LocalBuilder localBuilder2 = ilg.DeclareOrGetLocal(typeof(string[]), "vals");
			MethodInfo method = typeof(string).GetMethod("Split", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(char[]) });
			MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method3 = typeof(XmlReader).GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method2);
			ilg.Call(method3);
			ilg.Stloc(localBuilder);
			ilg.Ldloc(localBuilder);
			ilg.Load(null);
			ilg.Call(method);
			ilg.Stloc(localBuilder2);
			LocalBuilder local2 = ilg.DeclareOrGetLocal(typeof(int), "i");
			ilg.For(local2, 0, localBuilder2);
			string arraySource = GetArraySource(member.Mapping.TypeDesc, member.ArrayName);
			WriteSourceBegin(arraySource);
			WritePrimitive(attribute.Mapping, "vals[i]");
			WriteSourceEnd(arraySource, member.Mapping.TypeDesc.ArrayElementTypeDesc.Type);
			ilg.EndFor();
		}
		else
		{
			WriteSourceBegin(member.ArraySource);
			WritePrimitive(attribute.Mapping, attribute.IsList ? "vals[i]" : "Reader.Value");
			WriteSourceEnd(member.ArraySource, member.Mapping.TypeDesc.IsArrayLike ? member.Mapping.TypeDesc.ArrayElementTypeDesc.Type : member.Mapping.TypeDesc.Type);
		}
		if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
		{
			ILGenSet(member.CheckSpecifiedSource, true);
		}
		if (member.ParamsReadSource != null)
		{
			ILGenParamsReadSource(member.ParamsReadSource, value: true);
		}
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void WriteMemberBegin(Member[] members)
	{
		foreach (Member member in members)
		{
			if (!member.IsArrayLike)
			{
				continue;
			}
			string arrayName = member.ArrayName;
			string name = "c" + arrayName;
			TypeDesc typeDesc = member.Mapping.TypeDesc;
			if (member.Mapping.TypeDesc.IsArray)
			{
				WriteArrayLocalDecl(typeDesc.CSharpName, arrayName, "null", typeDesc);
				ilg.Ldc(0);
				ilg.Stloc(typeof(int), name);
				if (member.Mapping.ChoiceIdentifier != null)
				{
					WriteArrayLocalDecl(member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName + "[]", member.ChoiceArrayName, "null", member.Mapping.ChoiceIdentifier.Mapping.TypeDesc);
					ilg.Ldc(0);
					ilg.Stloc(typeof(int), "c" + member.ChoiceArrayName);
				}
				continue;
			}
			if (member.Source[member.Source.Length - 1] == '(' || member.Source[member.Source.Length - 1] == '{')
			{
				WriteCreateInstance(arrayName, typeDesc.CannotNew, typeDesc.Type);
				WriteSourceBegin(member.Source);
				ilg.Ldloc(ilg.GetLocal(arrayName));
				WriteSourceEnd(member.Source, typeDesc.Type);
				continue;
			}
			if (member.IsList && !member.Mapping.ReadOnly && member.Mapping.TypeDesc.IsNullable)
			{
				ILGenLoad(member.Source, typeof(object));
				ilg.Load(null);
				ilg.If(Cmp.EqualTo);
				if (!member.Mapping.TypeDesc.HasDefaultConstructor)
				{
					MethodInfo method = typeof(XmlSerializationReader).GetMethod("CreateReadOnlyCollectionException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
					ilg.Ldarg(0);
					ilg.Ldstr(GetCSharpString(member.Mapping.TypeDesc.CSharpName));
					ilg.Call(method);
					ilg.Throw();
				}
				else
				{
					WriteSourceBegin(member.Source);
					base.RaCodeGen.ILGenForCreateInstance(ilg, member.Mapping.TypeDesc.Type, typeDesc.CannotNew, cast: true);
					WriteSourceEnd(member.Source, member.Mapping.TypeDesc.Type);
				}
				ilg.EndIf();
			}
			WriteLocalDecl(arrayName, new SourceInfo(member.Source, member.Source, member.Mapping.MemberInfo, member.Mapping.TypeDesc.Type, ilg));
		}
	}

	private string ExpectedElements(Member[] members)
	{
		if (IsSequence(members))
		{
			return "null";
		}
		string text = string.Empty;
		bool flag = true;
		foreach (Member member in members)
		{
			if (member.Mapping.Xmlns != null || member.Mapping.Ignore || member.Mapping.IsText || member.Mapping.IsAttribute)
			{
				continue;
			}
			ElementAccessor[] elements = member.Mapping.Elements;
			foreach (ElementAccessor elementAccessor in elements)
			{
				string text2 = ((elementAccessor.Form == XmlSchemaForm.Qualified) ? elementAccessor.Namespace : "");
				if (!elementAccessor.Any || (elementAccessor.Name != null && elementAccessor.Name.Length != 0))
				{
					if (!flag)
					{
						text += ", ";
					}
					text = text + text2 + ":" + elementAccessor.Name;
					flag = false;
				}
			}
		}
		return ReflectionAwareILGen.GetQuotedCSharpString(text);
	}

	[RequiresUnreferencedCode("calls WriteMemberElementsIf")]
	private void WriteMemberElements(Member[] members, string elementElseString, string elseString, Member anyElement, Member anyText)
	{
		if (anyText != null)
		{
			ilg.Load(null);
			ilg.Stloc(typeof(string), "tmp");
		}
		MethodInfo method = typeof(XmlReader).GetMethod("get_NodeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		int intVar = 1;
		ilg.Ldarg(0);
		ilg.Call(method2);
		ilg.Call(method);
		ilg.Ldc(intVar);
		ilg.If(Cmp.EqualTo);
		WriteMemberElementsIf(members, anyElement, elementElseString);
		if (anyText != null)
		{
			WriteMemberText(anyText, elseString);
		}
		ilg.Else();
		ILGenElseString(elseString);
		ilg.EndIf();
	}

	[RequiresUnreferencedCode("calls WriteText")]
	private void WriteMemberText(Member anyText, string elseString)
	{
		ilg.InitElseIf();
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("get_NodeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(XmlNodeType.Text);
		ilg.Ceq();
		ilg.Brtrue(label);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(XmlNodeType.CDATA);
		ilg.Ceq();
		ilg.Brtrue(label);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(XmlNodeType.Whitespace);
		ilg.Ceq();
		ilg.Brtrue(label);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(XmlNodeType.SignificantWhitespace);
		ilg.Ceq();
		ilg.Br(label2);
		ilg.MarkLabel(label);
		ilg.Ldc(boolVar: true);
		ilg.MarkLabel(label2);
		ilg.AndIf();
		if (anyText != null)
		{
			WriteText(anyText);
		}
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void WriteText(Member member)
	{
		TextAccessor text = member.Mapping.Text;
		if (text.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)text.Mapping;
			WriteSourceBeginTyped(member.ArraySource, specialMapping.TypeDesc);
			TypeKind kind = specialMapping.TypeDesc.Kind;
			if (kind == TypeKind.Node)
			{
				MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method2 = typeof(XmlReader).GetMethod("ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("get_Document", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method4 = typeof(XmlDocument).GetMethod("CreateTextNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
				ilg.Ldarg(0);
				ilg.Call(method3);
				ilg.Ldarg(0);
				ilg.Call(method);
				ilg.Call(method2);
				ilg.Call(method4);
				WriteSourceEnd(member.ArraySource, specialMapping.TypeDesc.Type);
				return;
			}
			throw new InvalidOperationException(System.SR.XmlInternalError);
		}
		if (member.IsArrayLike)
		{
			WriteSourceBegin(member.ArraySource);
			if (text.Mapping.TypeDesc.CollapseWhitespace)
			{
				ilg.Ldarg(0);
			}
			MethodInfo method5 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			MethodInfo method6 = typeof(XmlReader).GetMethod("ReadContentAsString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method5);
			ilg.Call(method6);
			if (text.Mapping.TypeDesc.CollapseWhitespace)
			{
				MethodInfo method7 = typeof(XmlSerializationReader).GetMethod("CollapseWhitespace", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
				ilg.Call(method7);
			}
		}
		else if (text.Mapping.TypeDesc == base.StringTypeDesc || text.Mapping.TypeDesc.FormatterName == "String")
		{
			LocalBuilder local = ilg.GetLocal("tmp");
			MethodInfo method8 = typeof(XmlSerializationReader).GetMethod("ReadString", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
			{
				typeof(string),
				typeof(bool)
			});
			ilg.Ldarg(0);
			ilg.Ldloc(local);
			ilg.Ldc(text.Mapping.TypeDesc.CollapseWhitespace);
			ilg.Call(method8);
			ilg.Stloc(local);
			WriteSourceBegin(member.ArraySource);
			ilg.Ldloc(local);
		}
		else
		{
			WriteSourceBegin(member.ArraySource);
			WritePrimitive(text.Mapping, "Reader.ReadString()");
		}
		WriteSourceEnd(member.ArraySource, text.Mapping.TypeDesc.Type);
	}

	[RequiresUnreferencedCode("calls WriteElement")]
	private void WriteMemberElementsElse(Member anyElement, string elementElseString)
	{
		if (anyElement != null)
		{
			ElementAccessor[] elements = anyElement.Mapping.Elements;
			for (int i = 0; i < elements.Length; i++)
			{
				ElementAccessor elementAccessor = elements[i];
				if (elementAccessor.Any && elementAccessor.Name.Length == 0)
				{
					WriteElement(anyElement.ArraySource, anyElement.ArrayName, anyElement.ChoiceArraySource, elementAccessor, anyElement.Mapping.ChoiceIdentifier, (anyElement.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite) ? anyElement.CheckSpecifiedSource : null, checkForNull: false, readOnly: false, -1, i);
					break;
				}
			}
		}
		else
		{
			ILGenElementElseString(elementElseString);
		}
	}

	private bool IsSequence(Member[] members)
	{
		for (int i = 0; i < members.Length; i++)
		{
			if (members[i].Mapping.IsParticle && members[i].Mapping.IsSequence)
			{
				return true;
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("calls WriteElement")]
	private void WriteMemberElementsIf(Member[] members, Member anyElement, string elementElseString)
	{
		int num = 0;
		bool flag = IsSequence(members);
		int num2 = 0;
		foreach (Member member in members)
		{
			if (member.Mapping.Xmlns != null || member.Mapping.Ignore || (flag && (member.Mapping.IsText || member.Mapping.IsAttribute)))
			{
				continue;
			}
			bool flag2 = true;
			ChoiceIdentifierAccessor choiceIdentifier = member.Mapping.ChoiceIdentifier;
			ElementAccessor[] elements = member.Mapping.Elements;
			for (int j = 0; j < elements.Length; j++)
			{
				ElementAccessor elementAccessor = elements[j];
				string ns = ((elementAccessor.Form == XmlSchemaForm.Qualified) ? elementAccessor.Namespace : "");
				if (!flag && elementAccessor.Any && (elementAccessor.Name == null || elementAccessor.Name.Length == 0))
				{
					continue;
				}
				if (!flag2 || (!flag && num > 0))
				{
					ilg.InitElseIf();
				}
				else if (flag)
				{
					if (num2 > 0)
					{
						ilg.InitElseIf();
					}
					else
					{
						ilg.InitIf();
					}
					ilg.Ldloc("state");
					ilg.Ldc(num2);
					ilg.AndIf(Cmp.EqualTo);
					ilg.InitIf();
				}
				else
				{
					ilg.InitIf();
				}
				num++;
				flag2 = false;
				if (member.ParamsReadSource != null)
				{
					ILGenParamsReadSource(member.ParamsReadSource);
					ilg.Ldc(boolVar: false);
					ilg.AndIf(Cmp.EqualTo);
				}
				Label label = ilg.DefineLabel();
				Label label2 = ilg.DefineLabel();
				if (member.Mapping.IsReturnValue)
				{
					MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_IsReturnValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.Ldarg(0);
					ilg.Call(method);
					ilg.Brtrue(label);
				}
				if (flag && elementAccessor.Any && elementAccessor.AnyNamespaces == null)
				{
					ilg.Ldc(boolVar: true);
				}
				else
				{
					WriteXmlNodeEqual("Reader", elementAccessor.Name, ns, doAndIf: false);
				}
				if (member.Mapping.IsReturnValue)
				{
					ilg.Br_S(label2);
					ilg.MarkLabel(label);
					ilg.Ldc(boolVar: true);
					ilg.MarkLabel(label2);
				}
				ilg.AndIf();
				WriteElement(member.ArraySource, member.ArrayName, member.ChoiceArraySource, elementAccessor, choiceIdentifier, (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite) ? member.CheckSpecifiedSource : null, member.IsList && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, member.FixupIndex, j);
				if (member.Mapping.IsReturnValue)
				{
					MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("set_IsReturnValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(bool) });
					ilg.Ldarg(0);
					ilg.Ldc(boolVar: false);
					ilg.Call(method2);
				}
				if (member.ParamsReadSource != null)
				{
					ILGenParamsReadSource(member.ParamsReadSource, value: true);
				}
			}
			if (flag)
			{
				if (member.IsArrayLike)
				{
					ilg.Else();
				}
				else
				{
					ilg.EndIf();
				}
				num2++;
				ilg.Ldc(num2);
				ilg.Stloc(ilg.GetLocal("state"));
				if (member.IsArrayLike)
				{
					ilg.EndIf();
				}
			}
		}
		if (num > 0)
		{
			ilg.Else();
		}
		WriteMemberElementsElse(anyElement, elementElseString);
		if (num > 0)
		{
			ilg.EndIf();
		}
	}

	private string GetArraySource(TypeDesc typeDesc, string arrayName)
	{
		return GetArraySource(typeDesc, arrayName, multiRef: false);
	}

	private string GetArraySource(TypeDesc typeDesc, string arrayName, bool multiRef)
	{
		string text = "c" + arrayName;
		string text2 = "";
		if (multiRef)
		{
			text2 = "soap = (System.Object[])EnsureArrayIndex(soap, " + text + "+2, typeof(System.Object)); ";
		}
		if (typeDesc.IsArray)
		{
			string cSharpName = typeDesc.ArrayElementTypeDesc.CSharpName;
			string text3 = "(" + cSharpName + "[])";
			text2 = text2 + arrayName + " = " + text3 + "EnsureArrayIndex(" + arrayName + ", " + text + ", " + base.RaCodeGen.GetStringForTypeof(cSharpName) + ");";
			string stringForArrayMember = base.RaCodeGen.GetStringForArrayMember(arrayName, text + "++", typeDesc);
			if (multiRef)
			{
				text2 = text2 + " soap[1] = " + arrayName + ";";
				text2 = text2 + " if (ReadReference(out soap[" + text + "+2])) " + stringForArrayMember + " = null; else ";
			}
			return text2 + stringForArrayMember;
		}
		return base.RaCodeGen.GetStringForMethod(arrayName, typeDesc.CSharpName, "Add");
	}

	[RequiresUnreferencedCode("calls WriteMemberEnd")]
	private void WriteMemberEnd(Member[] members)
	{
		WriteMemberEnd(members, soapRefs: false);
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void WriteMemberEnd(Member[] members, bool soapRefs)
	{
		foreach (Member member in members)
		{
			if (!member.IsArrayLike)
			{
				continue;
			}
			TypeDesc typeDesc = member.Mapping.TypeDesc;
			if (typeDesc.IsArray)
			{
				WriteSourceBegin(member.Source);
				string arrayName = member.ArrayName;
				string name = "c" + arrayName;
				MethodInfo method = typeof(XmlSerializationReader).GetMethod("ShrinkArray", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[4]
				{
					typeof(Array),
					typeof(int),
					typeof(Type),
					typeof(bool)
				});
				ilg.Ldarg(0);
				ilg.Ldloc(ilg.GetLocal(arrayName));
				ilg.Ldloc(ilg.GetLocal(name));
				ilg.Ldc(typeDesc.ArrayElementTypeDesc.Type);
				ilg.Ldc(member.IsNullable);
				ilg.Call(method);
				ilg.ConvertValue(method.ReturnType, typeDesc.Type);
				WriteSourceEnd(member.Source, typeDesc.Type);
				if (member.Mapping.ChoiceIdentifier != null)
				{
					WriteSourceBegin(member.ChoiceSource);
					arrayName = member.ChoiceArrayName;
					name = "c" + arrayName;
					ilg.Ldarg(0);
					ilg.Ldloc(ilg.GetLocal(arrayName));
					ilg.Ldloc(ilg.GetLocal(name));
					ilg.Ldc(member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.Type);
					ilg.Ldc(member.IsNullable);
					ilg.Call(method);
					ilg.ConvertValue(method.ReturnType, member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.Type.MakeArrayType());
					WriteSourceEnd(member.ChoiceSource, member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.Type.MakeArrayType());
				}
			}
			else if (typeDesc.IsValueType)
			{
				LocalBuilder local = ilg.GetLocal(member.ArrayName);
				WriteSourceBegin(member.Source);
				ilg.Ldloc(local);
				WriteSourceEnd(member.Source, local.LocalType);
			}
		}
	}

	private void WriteSourceBeginTyped(string source, TypeDesc typeDesc)
	{
		WriteSourceBegin(source);
	}

	private void WriteSourceBegin(string source)
	{
		if (ilg.TryGetVariable(source, out var variable))
		{
			Type variableType = ilg.GetVariableType(variable);
			if (CodeGenerator.IsNullableGenericType(variableType))
			{
				ilg.LoadAddress(variable);
			}
			return;
		}
		if (source.StartsWith("o.@", StringComparison.Ordinal))
		{
			ilg.LdlocAddress(ilg.GetLocal("o"));
			return;
		}
		Regex regex = XmlSerializationILGen.NewRegex("(?<locA1>[^ ]+) = .+EnsureArrayIndex[(](?<locA2>[^,]+), (?<locI1>[^,]+),[^;]+;(?<locA3>[^[]+)[[](?<locI2>[^+]+)[+][+][]]");
		Match match = regex.Match(source);
		if (match.Success)
		{
			LocalBuilder local = ilg.GetLocal(match.Groups["locA1"].Value);
			LocalBuilder local2 = ilg.GetLocal(match.Groups["locI1"].Value);
			Type elementType = local.LocalType.GetElementType();
			MethodInfo method = typeof(XmlSerializationReader).GetMethod("EnsureArrayIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
			{
				typeof(Array),
				typeof(int),
				typeof(Type)
			});
			ilg.Ldarg(0);
			ilg.Ldloc(local);
			ilg.Ldloc(local2);
			ilg.Ldc(elementType);
			ilg.Call(method);
			ilg.Castclass(local.LocalType);
			ilg.Stloc(local);
			ilg.Ldloc(local);
			ilg.Ldloc(local2);
			ilg.Dup();
			ilg.Ldc(1);
			ilg.Add();
			ilg.Stloc(local2);
			if (CodeGenerator.IsNullableGenericType(elementType) || elementType.IsValueType)
			{
				ilg.Ldelema(elementType);
			}
		}
		else if (source.EndsWith(".Add(", StringComparison.Ordinal))
		{
			int length = source.LastIndexOf(".Add(", StringComparison.Ordinal);
			LocalBuilder local3 = ilg.GetLocal(source.Substring(0, length));
			ilg.LdlocAddress(local3);
		}
		else
		{
			regex = XmlSerializationILGen.NewRegex("(?<a>[^[]+)[[](?<ia>.+)[]]");
			match = regex.Match(source);
			if (!match.Success)
			{
				throw Globals.NotSupported("Unexpected: " + source);
			}
			ilg.Load(ilg.GetVariable(match.Groups["a"].Value));
			ilg.Load(ilg.GetVariable(match.Groups["ia"].Value));
		}
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void WriteSourceEnd(string source, Type elementType)
	{
		WriteSourceEnd(source, elementType, elementType);
	}

	[RequiresUnreferencedCode("string-based IL generation")]
	private void WriteSourceEnd(string source, Type elementType, Type stackType)
	{
		if (ilg.TryGetVariable(source, out var variable))
		{
			Type variableType = ilg.GetVariableType(variable);
			if (CodeGenerator.IsNullableGenericType(variableType))
			{
				ilg.Call(variableType.GetConstructor(variableType.GetGenericArguments()));
				return;
			}
			ilg.ConvertValue(stackType, elementType);
			ilg.ConvertValue(elementType, variableType);
			ilg.Stloc((LocalBuilder)variable);
			return;
		}
		if (source.StartsWith("o.@", StringComparison.Ordinal))
		{
			MemberInfo memberInfo = memberInfos[source.Substring(3)];
			ilg.ConvertValue(stackType, (memberInfo is FieldInfo) ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType);
			ilg.StoreMember(memberInfo);
			return;
		}
		Regex regex = XmlSerializationILGen.NewRegex("(?<locA1>[^ ]+) = .+EnsureArrayIndex[(](?<locA2>[^,]+), (?<locI1>[^,]+),[^;]+;(?<locA3>[^[]+)[[](?<locI2>[^+]+)[+][+][]]");
		Match match = regex.Match(source);
		if (match.Success)
		{
			object variable2 = ilg.GetVariable(match.Groups["locA1"].Value);
			Type elementType2 = ilg.GetVariableType(variable2).GetElementType();
			ilg.ConvertValue(elementType, elementType2);
			if (CodeGenerator.IsNullableGenericType(elementType2) || elementType2.IsValueType)
			{
				ilg.Stobj(elementType2);
			}
			else
			{
				ilg.Stelem(elementType2);
			}
		}
		else if (source.EndsWith(".Add(", StringComparison.Ordinal))
		{
			int length = source.LastIndexOf(".Add(", StringComparison.Ordinal);
			LocalBuilder local = ilg.GetLocal(source.Substring(0, length));
			MethodInfo method = local.LocalType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { elementType });
			Type parameterType = method.GetParameters()[0].ParameterType;
			ilg.ConvertValue(stackType, parameterType);
			ilg.Call(method);
			if (method.ReturnType != typeof(void))
			{
				ilg.Pop();
			}
		}
		else
		{
			regex = XmlSerializationILGen.NewRegex("(?<a>[^[]+)[[](?<ia>.+)[]]");
			match = regex.Match(source);
			if (!match.Success)
			{
				throw Globals.NotSupported("Unexpected: " + source);
			}
			Type variableType2 = ilg.GetVariableType(ilg.GetVariable(match.Groups["a"].Value));
			Type elementType3 = variableType2.GetElementType();
			ilg.ConvertValue(stackType, elementType3);
			ilg.Stelem(elementType3);
		}
	}

	[RequiresUnreferencedCode("calls WriteMemberBegin")]
	private void WriteArray(string source, string arrayName, ArrayMapping arrayMapping, bool readOnly, bool isNullable, int fixupIndex, int elementIndex)
	{
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("ReadNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.IfNot();
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = arrayMapping.Elements;
		memberMapping.TypeDesc = arrayMapping.TypeDesc;
		memberMapping.ReadOnly = readOnly;
		if (source.StartsWith("o.@", StringComparison.Ordinal))
		{
			memberMapping.MemberInfo = memberInfos[source.Substring(3)];
		}
		Member member = new Member(this, source, arrayName, elementIndex, memberMapping, multiRef: false);
		member.IsNullable = false;
		Member[] members = new Member[1] { member };
		WriteMemberBegin(members);
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		if (readOnly)
		{
			ilg.Load(ilg.GetVariable(member.ArrayName));
			ilg.Load(null);
			ilg.Beq(label);
		}
		MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method3 = typeof(XmlReader).GetMethod("get_IsEmptyElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method2);
		ilg.Call(method3);
		if (readOnly)
		{
			ilg.Br_S(label2);
			ilg.MarkLabel(label);
			ilg.Ldc(boolVar: true);
			ilg.MarkLabel(label2);
		}
		ilg.If();
		MethodInfo method4 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method2);
		ilg.Call(method4);
		ilg.Else();
		MethodInfo method5 = typeof(XmlReader).GetMethod("ReadStartElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method2);
		ilg.Call(method5);
		WriteWhileNotLoopStart();
		string text = "UnknownNode(null, " + ExpectedElements(members) + ");";
		WriteMemberElements(members, text, text, null, null);
		MethodInfo method6 = typeof(XmlReader).GetMethod("MoveToContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method2);
		ilg.Call(method6);
		ilg.Pop();
		WriteWhileLoopEnd();
		MethodInfo method7 = typeof(XmlSerializationReader).GetMethod("ReadEndElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method7);
		ilg.EndIf();
		WriteMemberEnd(members, soapRefs: false);
		if (isNullable)
		{
			ilg.Else();
			member.IsNullable = true;
			WriteMemberBegin(members);
			WriteMemberEnd(members);
		}
		ilg.EndIf();
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void WriteElement(string source, string arrayName, string choiceSource, ElementAccessor element, ChoiceIdentifierAccessor choice, string checkSpecified, bool checkForNull, bool readOnly, int fixupIndex, int elementIndex)
	{
		if (checkSpecified != null && checkSpecified.Length > 0)
		{
			ILGenSet(checkSpecified, true);
		}
		if (element.Mapping is ArrayMapping)
		{
			WriteArray(source, arrayName, (ArrayMapping)element.Mapping, readOnly, element.IsNullable, fixupIndex, elementIndex);
		}
		else if (element.Mapping is NullableMapping)
		{
			string methodName = ReferenceMapping(element.Mapping);
			WriteSourceBegin(source);
			ilg.Ldarg(0);
			ilg.Ldc(boolVar: true);
			MethodBuilder methodInfo = EnsureMethodBuilder(typeBuilder, methodName, MethodAttributes.Private | MethodAttributes.HideBySig, element.Mapping.TypeDesc.Type, new Type[1] { typeof(bool) });
			ilg.Call(methodInfo);
			WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
		}
		else if (element.Mapping is PrimitiveMapping)
		{
			bool flag = false;
			if (element.IsNullable)
			{
				MethodInfo method = typeof(XmlSerializationReader).GetMethod("ReadNull", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method);
				ilg.If();
				WriteSourceBegin(source);
				if (element.Mapping.TypeDesc.IsValueType)
				{
					throw Globals.NotSupported("No such condition.  PrimitiveMapping && IsNullable = String, XmlQualifiedName and never IsValueType");
				}
				ilg.Load(null);
				WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
				ilg.Else();
				flag = true;
			}
			if (element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc.IsValueType)
			{
				MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method3 = typeof(XmlReader).GetMethod("get_IsEmptyElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method2);
				ilg.Call(method3);
				ilg.If();
				MethodInfo method4 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method2);
				ilg.Call(method4);
				ilg.Else();
				flag = true;
			}
			if (element.Mapping.TypeDesc.Type == typeof(TimeSpan) || element.Mapping.TypeDesc.Type == typeof(DateTimeOffset))
			{
				MethodInfo method5 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method6 = typeof(XmlReader).GetMethod("get_IsEmptyElement", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method5);
				ilg.Call(method6);
				ilg.If();
				WriteSourceBegin(source);
				MethodInfo method7 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldarg(0);
				ilg.Call(method5);
				ilg.Call(method7);
				LocalBuilder tempLocal = ilg.GetTempLocal(element.Mapping.TypeDesc.Type);
				ilg.Ldloca(tempLocal);
				ilg.InitObj(element.Mapping.TypeDesc.Type);
				ilg.Ldloc(tempLocal);
				WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
				ilg.Else();
				WriteSourceBegin(source);
				WritePrimitive(element.Mapping, "Reader.ReadElementString()");
				WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
				ilg.EndIf();
			}
			else
			{
				WriteSourceBegin(source);
				if (element.Mapping.TypeDesc == base.QnameTypeDesc)
				{
					MethodInfo method8 = typeof(XmlSerializationReader).GetMethod("ReadElementQualifiedName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					ilg.Ldarg(0);
					ilg.Call(method8);
				}
				else
				{
					string formatterName = element.Mapping.TypeDesc.FormatterName;
					WritePrimitive(source: (!(formatterName == "ByteArrayBase64") && !(formatterName == "ByteArrayHex")) ? "Reader.ReadElementString()" : "false", mapping: element.Mapping);
				}
				WriteSourceEnd(source, element.Mapping.TypeDesc.Type);
			}
			if (flag)
			{
				ilg.EndIf();
			}
		}
		else if (element.Mapping is StructMapping)
		{
			TypeMapping mapping = element.Mapping;
			string methodName2 = ReferenceMapping(mapping);
			if (checkForNull)
			{
				MethodInfo method9 = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				MethodInfo method10 = typeof(XmlReader).GetMethod("Skip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
				ilg.Ldloc(arrayName);
				ilg.Load(null);
				ilg.If(Cmp.EqualTo);
				ilg.Ldarg(0);
				ilg.Call(method9);
				ilg.Call(method10);
				ilg.Else();
			}
			WriteSourceBegin(source);
			List<Type> list = new List<Type>();
			ilg.Ldarg(0);
			if (mapping.TypeDesc.IsNullable)
			{
				ilg.Load(element.IsNullable);
				list.Add(typeof(bool));
			}
			ilg.Ldc(boolVar: true);
			list.Add(typeof(bool));
			MethodBuilder methodInfo2 = EnsureMethodBuilder(typeBuilder, methodName2, MethodAttributes.Private | MethodAttributes.HideBySig, mapping.TypeDesc.Type, list.ToArray());
			ilg.Call(methodInfo2);
			WriteSourceEnd(source, mapping.TypeDesc.Type);
			if (checkForNull)
			{
				ilg.EndIf();
			}
		}
		else
		{
			if (!(element.Mapping is SpecialMapping))
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			SpecialMapping specialMapping = (SpecialMapping)element.Mapping;
			switch (specialMapping.TypeDesc.Kind)
			{
			case TypeKind.Node:
			{
				bool flag3 = specialMapping.TypeDesc.FullName == typeof(XmlDocument).FullName;
				WriteSourceBeginTyped(source, specialMapping.TypeDesc);
				MethodInfo method13 = typeof(XmlSerializationReader).GetMethod(flag3 ? "ReadXmlDocument" : "ReadXmlNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(bool) });
				ilg.Ldarg(0);
				ilg.Ldc(!element.Any);
				ilg.Call(method13);
				if (specialMapping.TypeDesc != null)
				{
					ilg.Castclass(specialMapping.TypeDesc.Type);
				}
				WriteSourceEnd(source, specialMapping.TypeDesc.Type);
				break;
			}
			case TypeKind.Serializable:
			{
				SerializableMapping serializableMapping = (SerializableMapping)element.Mapping;
				if (serializableMapping.DerivedMappings != null)
				{
					MethodInfo method11 = typeof(XmlSerializationReader).GetMethod("GetXsiType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
					Label label = ilg.DefineLabel();
					Label label2 = ilg.DefineLabel();
					LocalBuilder localBuilder = ilg.DeclareOrGetLocal(typeof(XmlQualifiedName), "tser");
					ilg.Ldarg(0);
					ilg.Call(method11);
					ilg.Stloc(localBuilder);
					ilg.Ldloc(localBuilder);
					ilg.Load(null);
					ilg.Ceq();
					ilg.Brtrue(label);
					WriteQNameEqual("tser", serializableMapping.XsiType.Name, serializableMapping.XsiType.Namespace);
					ilg.Br_S(label2);
					ilg.MarkLabel(label);
					ilg.Ldc(boolVar: true);
					ilg.MarkLabel(label2);
					ilg.If();
				}
				WriteSourceBeginTyped(source, serializableMapping.TypeDesc);
				bool flag2 = !element.Any && XmlSerializationILGen.IsWildcard(serializableMapping);
				MethodInfo method12 = typeof(XmlSerializationReader).GetMethod("ReadSerializable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (!flag2) ? new Type[1] { typeof(IXmlSerializable) } : new Type[2]
				{
					typeof(IXmlSerializable),
					typeof(bool)
				});
				ilg.Ldarg(0);
				base.RaCodeGen.ILGenForCreateInstance(ilg, serializableMapping.TypeDesc.Type, serializableMapping.TypeDesc.CannotNew, cast: false);
				if (serializableMapping.TypeDesc.CannotNew)
				{
					ilg.ConvertValue(typeof(object), typeof(IXmlSerializable));
				}
				if (flag2)
				{
					ilg.Ldc(boolVar: true);
				}
				ilg.Call(method12);
				if (serializableMapping.TypeDesc != null)
				{
					ilg.ConvertValue(typeof(IXmlSerializable), serializableMapping.TypeDesc.Type);
				}
				WriteSourceEnd(source, serializableMapping.TypeDesc.Type);
				if (serializableMapping.DerivedMappings != null)
				{
					WriteDerivedSerializable(serializableMapping, serializableMapping, source, flag2);
					WriteUnknownNode("UnknownNode", "null", null, anyIfs: true);
				}
				break;
			}
			default:
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
		}
		if (choice != null)
		{
			WriteSourceBegin(choiceSource);
			CodeIdentifier.CheckValidIdentifier(choice.MemberIds[elementIndex]);
			base.RaCodeGen.ILGenForEnumMember(ilg, choice.Mapping.TypeDesc.Type, choice.MemberIds[elementIndex]);
			WriteSourceEnd(choiceSource, choice.Mapping.TypeDesc.Type);
		}
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void WriteDerivedSerializable(SerializableMapping head, SerializableMapping mapping, string source, bool isWrappedAny)
	{
		if (mapping == null)
		{
			return;
		}
		for (SerializableMapping serializableMapping = mapping.DerivedMappings; serializableMapping != null; serializableMapping = serializableMapping.NextDerivedMapping)
		{
			Label label = ilg.DefineLabel();
			Label label2 = ilg.DefineLabel();
			LocalBuilder local = ilg.GetLocal("tser");
			ilg.InitElseIf();
			ilg.Ldloc(local);
			ilg.Load(null);
			ilg.Ceq();
			ilg.Brtrue(label);
			WriteQNameEqual("tser", serializableMapping.XsiType.Name, serializableMapping.XsiType.Namespace);
			ilg.Br_S(label2);
			ilg.MarkLabel(label);
			ilg.Ldc(boolVar: true);
			ilg.MarkLabel(label2);
			ilg.AndIf();
			if (serializableMapping.Type != null)
			{
				if (head.Type.IsAssignableFrom(serializableMapping.Type))
				{
					WriteSourceBeginTyped(source, head.TypeDesc);
					MethodInfo method = typeof(XmlSerializationReader).GetMethod("ReadSerializable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (!isWrappedAny) ? new Type[1] { typeof(IXmlSerializable) } : new Type[2]
					{
						typeof(IXmlSerializable),
						typeof(bool)
					});
					ilg.Ldarg(0);
					base.RaCodeGen.ILGenForCreateInstance(ilg, serializableMapping.TypeDesc.Type, serializableMapping.TypeDesc.CannotNew, cast: false);
					if (serializableMapping.TypeDesc.CannotNew)
					{
						ilg.ConvertValue(typeof(object), typeof(IXmlSerializable));
					}
					if (isWrappedAny)
					{
						ilg.Ldc(boolVar: true);
					}
					ilg.Call(method);
					if (head.TypeDesc != null)
					{
						ilg.ConvertValue(typeof(IXmlSerializable), head.TypeDesc.Type);
					}
					WriteSourceEnd(source, head.TypeDesc.Type);
				}
				else
				{
					MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("CreateBadDerivationException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[6]
					{
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string),
						typeof(string)
					});
					ilg.Ldarg(0);
					ilg.Ldstr(GetCSharpString(serializableMapping.XsiType.Name));
					ilg.Ldstr(GetCSharpString(serializableMapping.XsiType.Namespace));
					ilg.Ldstr(GetCSharpString(head.XsiType.Name));
					ilg.Ldstr(GetCSharpString(head.XsiType.Namespace));
					ilg.Ldstr(GetCSharpString(serializableMapping.Type.FullName));
					ilg.Ldstr(GetCSharpString(head.Type.FullName));
					ilg.Call(method2);
					ilg.Throw();
				}
			}
			else
			{
				MethodInfo method3 = typeof(XmlSerializationReader).GetMethod("CreateMissingIXmlSerializableType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[3]
				{
					typeof(string),
					typeof(string),
					typeof(string)
				});
				ilg.Ldarg(0);
				ilg.Ldstr(GetCSharpString(serializableMapping.XsiType.Name));
				ilg.Ldstr(GetCSharpString(serializableMapping.XsiType.Namespace));
				ilg.Ldstr(GetCSharpString(head.Type.FullName));
				ilg.Call(method3);
				ilg.Throw();
			}
			WriteDerivedSerializable(head, serializableMapping, source, isWrappedAny);
		}
	}

	private void WriteWhileNotLoopStart()
	{
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("MoveToContent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Pop();
		ilg.WhileBegin();
	}

	private void WriteWhileLoopEnd()
	{
		ilg.WhileBeginCondition();
		int intVar = 0;
		int intVar2 = 15;
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("get_Reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		MethodInfo method2 = typeof(XmlReader).GetMethod("get_NodeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
		Label label = ilg.DefineLabel();
		Label label2 = ilg.DefineLabel();
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(intVar2);
		ilg.Beq(label);
		ilg.Ldarg(0);
		ilg.Call(method);
		ilg.Call(method2);
		ilg.Ldc(intVar);
		ilg.Cne();
		ilg.Br_S(label2);
		ilg.MarkLabel(label);
		ilg.Ldc(boolVar: false);
		ilg.MarkLabel(label2);
		ilg.WhileEndCondition();
		ilg.WhileEnd();
	}

	private void WriteParamsRead(int length)
	{
		LocalBuilder local = ilg.DeclareLocal(typeof(bool[]), "paramsRead");
		ilg.NewArray(typeof(bool), length);
		ilg.Stloc(local);
	}

	[RequiresUnreferencedCode("calls ILGenForCreateInstance")]
	private void WriteCreateMapping(TypeMapping mapping, string local)
	{
		string cSharpName = mapping.TypeDesc.CSharpName;
		bool cannotNew = mapping.TypeDesc.CannotNew;
		LocalBuilder local2 = ilg.DeclareLocal(mapping.TypeDesc.Type, local);
		if (cannotNew)
		{
			ilg.BeginExceptionBlock();
		}
		base.RaCodeGen.ILGenForCreateInstance(ilg, mapping.TypeDesc.Type, mapping.TypeDesc.CannotNew, cast: true);
		ilg.Stloc(local2);
		if (cannotNew)
		{
			ilg.Leave();
			WriteCatchException(typeof(MissingMethodException));
			MethodInfo method = typeof(XmlSerializationReader).GetMethod("CreateInaccessibleConstructorException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
			ilg.Ldarg(0);
			ilg.Ldstr(GetCSharpString(cSharpName));
			ilg.Call(method);
			ilg.Throw();
			WriteCatchException(typeof(SecurityException));
			MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("CreateCtorHasSecurityException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(string) });
			ilg.Ldarg(0);
			ilg.Ldstr(GetCSharpString(cSharpName));
			ilg.Call(method2);
			ilg.Throw();
			ilg.EndExceptionBlock();
		}
	}

	private void WriteCatchException(Type exceptionType)
	{
		ilg.BeginCatchBlock(exceptionType);
		ilg.Pop();
	}

	[RequiresUnreferencedCode("calls WriteArrayLocalDecl")]
	private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
	{
		base.RaCodeGen.WriteArrayLocalDecl(typeName, variableName, new SourceInfo(initValue, initValue, null, arrayTypeDesc.Type, ilg), arrayTypeDesc);
	}

	[RequiresUnreferencedCode("calls WriteCreateInstance")]
	private void WriteCreateInstance(string source, bool ctorInaccessible, Type type)
	{
		base.RaCodeGen.WriteCreateInstance(source, ctorInaccessible, type, ilg);
	}

	[RequiresUnreferencedCode("calls WriteLocalDecl")]
	private void WriteLocalDecl(string variableName, SourceInfo initValue)
	{
		base.RaCodeGen.WriteLocalDecl(variableName, initValue);
	}

	private void ILGenElseString(string elseString)
	{
		MethodInfo method = typeof(XmlSerializationReader).GetMethod("UnknownNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[1] { typeof(object) });
		MethodInfo method2 = typeof(XmlSerializationReader).GetMethod("UnknownNode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new Type[2]
		{
			typeof(object),
			typeof(string)
		});
		Regex regex = XmlSerializationILGen.NewRegex("UnknownNode[(]null, @[\"](?<qnames>[^\"]*)[\"][)];");
		Match match = regex.Match(elseString);
		if (match.Success)
		{
			ilg.Ldarg(0);
			ilg.Load(null);
			ilg.Ldstr(match.Groups["qnames"].Value);
			ilg.Call(method2);
			return;
		}
		regex = XmlSerializationILGen.NewRegex("UnknownNode[(][(]object[)](?<o>[^,]+), @[\"](?<qnames>[^\"]*)[\"][)];");
		match = regex.Match(elseString);
		if (match.Success)
		{
			ilg.Ldarg(0);
			LocalBuilder local = ilg.GetLocal(match.Groups["o"].Value);
			ilg.Ldloc(local);
			ilg.ConvertValue(local.LocalType, typeof(object));
			ilg.Ldstr(match.Groups["qnames"].Value);
			ilg.Call(method2);
			return;
		}
		regex = XmlSerializationILGen.NewRegex("UnknownNode[(][(]object[)](?<o>[^,]+), null[)];");
		match = regex.Match(elseString);
		if (match.Success)
		{
			ilg.Ldarg(0);
			LocalBuilder local2 = ilg.GetLocal(match.Groups["o"].Value);
			ilg.Ldloc(local2);
			ilg.ConvertValue(local2.LocalType, typeof(object));
			ilg.Load(null);
			ilg.Call(method2);
			return;
		}
		regex = XmlSerializationILGen.NewRegex("UnknownNode[(][(]object[)](?<o>[^)]+)[)];");
		match = regex.Match(elseString);
		if (match.Success)
		{
			ilg.Ldarg(0);
			LocalBuilder local3 = ilg.GetLocal(match.Groups["o"].Value);
			ilg.Ldloc(local3);
			ilg.ConvertValue(local3.LocalType, typeof(object));
			ilg.Call(method);
			return;
		}
		throw Globals.NotSupported("Unexpected: " + elseString);
	}

	private void ILGenParamsReadSource(string paramsReadSource)
	{
		Regex regex = XmlSerializationILGen.NewRegex("paramsRead\\[(?<index>[0-9]+)\\]");
		Match match = regex.Match(paramsReadSource);
		if (match.Success)
		{
			ilg.LoadArrayElement(ilg.GetLocal("paramsRead"), int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture));
			return;
		}
		throw Globals.NotSupported("Unexpected: " + paramsReadSource);
	}

	private void ILGenParamsReadSource(string paramsReadSource, bool value)
	{
		Regex regex = XmlSerializationILGen.NewRegex("paramsRead\\[(?<index>[0-9]+)\\]");
		Match match = regex.Match(paramsReadSource);
		if (match.Success)
		{
			ilg.StoreArrayElement(ilg.GetLocal("paramsRead"), int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture), value);
			return;
		}
		throw Globals.NotSupported("Unexpected: " + paramsReadSource);
	}

	private void ILGenElementElseString(string elementElseString)
	{
		if (elementElseString == "throw CreateUnknownNodeException();")
		{
			MethodInfo method = typeof(XmlSerializationReader).GetMethod("CreateUnknownNodeException", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
			ilg.Ldarg(0);
			ilg.Call(method);
			ilg.Throw();
		}
		else
		{
			if (!elementElseString.StartsWith("UnknownNode(", StringComparison.Ordinal))
			{
				throw Globals.NotSupported("Unexpected: " + elementElseString);
			}
			ILGenElseString(elementElseString);
		}
	}

	[RequiresUnreferencedCode("calls WriteSourceEnd")]
	private void ILGenSet(string source, object value)
	{
		WriteSourceBegin(source);
		ilg.Load(value);
		WriteSourceEnd(source, (value == null) ? typeof(object) : value.GetType());
	}
}
