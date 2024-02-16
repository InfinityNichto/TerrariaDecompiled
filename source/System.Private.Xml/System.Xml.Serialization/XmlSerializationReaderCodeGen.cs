using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class XmlSerializationReaderCodeGen : XmlSerializationCodeGen
{
	private sealed class CreateCollectionInfo
	{
		private readonly string _name;

		private readonly TypeDesc _td;

		internal string Name => _name;

		internal TypeDesc TypeDesc => _td;

		internal CreateCollectionInfo(string name, TypeDesc td)
		{
			_name = name;
			_td = td;
		}
	}

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

		private bool _multiRef;

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

		internal bool MultiRef
		{
			get
			{
				return _multiRef;
			}
			set
			{
				_multiRef = value;
			}
		}

		internal int FixupIndex
		{
			get
			{
				return _fixupIndex;
			}
			set
			{
				_fixupIndex = value;
			}
		}

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

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef: false, null)
		{
		}

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping, string choiceSource)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef: false, choiceSource)
		{
		}

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping)
			: this(outerClass, source, arraySource, arrayName, i, mapping, multiRef: false, null)
		{
		}

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, string choiceSource)
			: this(outerClass, source, arraySource, arrayName, i, mapping, multiRef: false, choiceSource)
		{
		}

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arrayName, int i, MemberMapping mapping, bool multiRef)
			: this(outerClass, source, null, arrayName, i, mapping, multiRef, null)
		{
		}

		internal Member(XmlSerializationReaderCodeGen outerClass, string source, string arraySource, string arrayName, int i, MemberMapping mapping, bool multiRef, string choiceSource)
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
					bool useReflection = mapping.ChoiceIdentifier.Mapping.TypeDesc.UseReflection;
					string cSharpName = mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName;
					string text2 = (useReflection ? "" : ("(" + cSharpName + "[])"));
					string text3 = choiceArrayName + " = " + text2 + "EnsureArrayIndex(" + choiceArrayName + ", " + text + ", " + outerClass.RaCodeGen.GetStringForTypeof(cSharpName, useReflection) + ");";
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

	private readonly Hashtable _idNames = new Hashtable();

	private Hashtable _enums;

	private readonly Hashtable _createMethods = new Hashtable();

	private int _nextCreateMethodNumber;

	private int _nextIdNumber;

	internal Hashtable Enums
	{
		get
		{
			if (_enums == null)
			{
				_enums = new Hashtable();
			}
			return _enums;
		}
	}

	[RequiresUnreferencedCode("creates XmlSerializationCodeGen")]
	internal XmlSerializationReaderCodeGen(IndentedWriter writer, TypeScope[] scopes, string access, string className)
		: base(writer, scopes, access, className)
	{
	}

	[RequiresUnreferencedCode("calls WriteReflectionInit")]
	internal void GenerateBegin()
	{
		base.Writer.Write(base.Access);
		base.Writer.Write(" class ");
		base.Writer.Write(base.ClassName);
		base.Writer.Write(" : ");
		base.Writer.Write(typeof(XmlSerializationReader).FullName);
		base.Writer.WriteLine(" {");
		base.Writer.Indent++;
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (TypeMapping typeMapping3 in typeScope.TypeMappings)
			{
				if (typeMapping3 is StructMapping || typeMapping3 is EnumMapping || typeMapping3 is NullableMapping)
				{
					base.MethodNames.Add(typeMapping3, NextMethodName(typeMapping3.TypeDesc.Name));
				}
			}
			base.RaCodeGen.WriteReflectionInit(typeScope);
		}
		TypeScope[] scopes2 = base.Scopes;
		foreach (TypeScope typeScope2 in scopes2)
		{
			foreach (TypeMapping typeMapping4 in typeScope2.TypeMappings)
			{
				if (typeMapping4.IsSoap)
				{
					if (typeMapping4 is StructMapping)
					{
						WriteStructMethod((StructMapping)typeMapping4);
					}
					else if (typeMapping4 is EnumMapping)
					{
						WriteEnumMethod((EnumMapping)typeMapping4);
					}
					else if (typeMapping4 is NullableMapping)
					{
						WriteNullableMethod((NullableMapping)typeMapping4);
					}
				}
			}
		}
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	internal override void GenerateMethod(TypeMapping mapping)
	{
		if (!base.GeneratedMethods.Contains(mapping))
		{
			base.GeneratedMethods[mapping] = mapping;
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
		foreach (CreateCollectionInfo value in _createMethods.Values)
		{
			WriteCreateCollectionMethod(value);
		}
		base.Writer.WriteLine();
		foreach (string value2 in _idNames.Values)
		{
			base.Writer.Write("string ");
			base.Writer.Write(value2);
			base.Writer.WriteLine(";");
		}
		base.Writer.WriteLine();
		base.Writer.WriteLine("protected override void InitIDs() {");
		base.Writer.Indent++;
		foreach (string key in _idNames.Keys)
		{
			string s2 = (string)_idNames[key];
			base.Writer.Write(s2);
			base.Writer.Write(" = Reader.NameTable.Add(");
			WriteQuotedCSharpString(key);
			base.Writer.WriteLine(");");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

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

	private void WriteIsStartTag(string name, string ns)
	{
		base.Writer.Write("if (Reader.IsStartElement(");
		WriteID(name);
		base.Writer.Write(", ");
		WriteID(ns);
		base.Writer.WriteLine(")) {");
		base.Writer.Indent++;
	}

	private void WriteUnknownNode(string func, string node, ElementAccessor e, bool anyIfs)
	{
		if (anyIfs)
		{
			base.Writer.WriteLine("else {");
			base.Writer.Indent++;
		}
		base.Writer.Write(func);
		base.Writer.Write("(");
		base.Writer.Write(node);
		if (e != null)
		{
			base.Writer.Write(", ");
			string text = ((e.Form == XmlSchemaForm.Qualified) ? e.Namespace : "");
			text += ":";
			text += e.Name;
			ReflectionAwareCodeGen.WriteQuotedCSharpString(base.Writer, text);
		}
		base.Writer.WriteLine(");");
		if (anyIfs)
		{
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void GenerateInitCallbacksMethod()
	{
		base.Writer.WriteLine();
		base.Writer.WriteLine("protected override void InitCallbacks() {");
		base.Writer.Indent++;
		string text = NextMethodName("Array");
		bool flag = false;
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (TypeMapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping.IsSoap && (typeMapping is StructMapping || typeMapping is EnumMapping || typeMapping is ArrayMapping || typeMapping is NullableMapping) && !typeMapping.TypeDesc.IsRoot)
				{
					string s;
					if (typeMapping is ArrayMapping)
					{
						s = text;
						flag = true;
					}
					else
					{
						s = (string)base.MethodNames[typeMapping];
					}
					base.Writer.Write("AddReadCallback(");
					WriteID(typeMapping.TypeName);
					base.Writer.Write(", ");
					WriteID(typeMapping.Namespace);
					base.Writer.Write(", ");
					base.Writer.Write(base.RaCodeGen.GetStringForTypeof(typeMapping.TypeDesc.CSharpName, typeMapping.TypeDesc.UseReflection));
					base.Writer.Write(", new ");
					base.Writer.Write(typeof(XmlSerializationReadCallback).FullName);
					base.Writer.Write("(this.");
					base.Writer.Write(s);
					base.Writer.WriteLine("));");
				}
			}
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (flag)
		{
			base.Writer.WriteLine();
			base.Writer.Write("object ");
			base.Writer.Write(text);
			base.Writer.WriteLine("() {");
			base.Writer.Indent++;
			base.Writer.WriteLine("// dummy array method");
			base.Writer.WriteLine("UnknownNode(null);");
			base.Writer.WriteLine("return null;");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		if (xmlMembersMapping.Accessor.IsSoap)
		{
			return GenerateEncodedMembersElement(xmlMembersMapping);
		}
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

	private string GenerateLiteralMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MemberMapping[] members = ((MembersMapping)accessor.Mapping).Members;
		bool hasWrapperElement = ((MembersMapping)accessor.Mapping).HasWrapperElement;
		string text = NextMethodName(accessor.Name);
		base.Writer.WriteLine();
		base.Writer.Write("public object[] ");
		base.Writer.Write(text);
		base.Writer.WriteLine("() {");
		base.Writer.Indent++;
		base.Writer.WriteLine("Reader.MoveToContent();");
		base.Writer.Write("object[] p = new object[");
		base.Writer.Write(members.Length.ToString(CultureInfo.InvariantCulture));
		base.Writer.WriteLine("];");
		InitializeValueTypes("p", members);
		if (hasWrapperElement)
		{
			WriteWhileNotLoopStart();
			base.Writer.Indent++;
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
			base.Writer.WriteLine("IsReturnValue = true;");
		}
		WriteParamsRead(members.Length);
		if (list3.Count > 0)
		{
			Member[] members3 = list3.ToArray();
			WriteMemberBegin(members3);
			WriteAttributes(members3, anyAttribute, "UnknownNode", "(object)p");
			WriteMemberEnd(members3);
			base.Writer.WriteLine("Reader.MoveToElement();");
		}
		WriteMemberBegin(members2);
		if (hasWrapperElement)
		{
			base.Writer.WriteLine("if (Reader.IsEmptyElement) { Reader.Skip(); Reader.MoveToContent(); continue; }");
			base.Writer.WriteLine("Reader.ReadStartElement();");
		}
		if (IsSequence(array))
		{
			base.Writer.WriteLine("int state = 0;");
		}
		WriteWhileNotLoopStart();
		base.Writer.Indent++;
		string text4 = "UnknownNode((object)p, " + ExpectedElements(array) + ");";
		WriteMemberElements(array, text4, text4, anyElement, anyText, null);
		base.Writer.WriteLine("Reader.MoveToContent();");
		WriteWhileLoopEnd();
		WriteMemberEnd(members2);
		if (hasWrapperElement)
		{
			base.Writer.WriteLine("ReadEndElement();");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			WriteUnknownNode("UnknownNode", "null", accessor, anyIfs: true);
			base.Writer.WriteLine("Reader.MoveToContent();");
			WriteWhileLoopEnd();
		}
		base.Writer.WriteLine("return p;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		return text;
	}

	private void InitializeValueTypes(string arrayName, MemberMapping[] mappings)
	{
		for (int i = 0; i < mappings.Length; i++)
		{
			if (mappings[i].TypeDesc.IsValueType)
			{
				base.Writer.Write(arrayName);
				base.Writer.Write("[");
				base.Writer.Write(i.ToString(CultureInfo.InvariantCulture));
				base.Writer.Write("] = ");
				if (mappings[i].TypeDesc.IsOptionalValue && mappings[i].TypeDesc.BaseTypeDesc.UseReflection)
				{
					base.Writer.Write("null");
				}
				else
				{
					base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(mappings[i].TypeDesc.CSharpName, mappings[i].TypeDesc.UseReflection, ctorInaccessible: false, cast: false));
				}
				base.Writer.WriteLine(";");
			}
		}
	}

	private string GenerateEncodedMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MembersMapping membersMapping = (MembersMapping)accessor.Mapping;
		MemberMapping[] members = membersMapping.Members;
		bool hasWrapperElement = membersMapping.HasWrapperElement;
		bool writeAccessors = membersMapping.WriteAccessors;
		string text = NextMethodName(accessor.Name);
		base.Writer.WriteLine();
		base.Writer.Write("public object[] ");
		base.Writer.Write(text);
		base.Writer.WriteLine("() {");
		base.Writer.Indent++;
		base.Writer.WriteLine("Reader.MoveToContent();");
		base.Writer.Write("object[] p = new object[");
		base.Writer.Write(members.Length.ToString(CultureInfo.InvariantCulture));
		base.Writer.WriteLine("];");
		InitializeValueTypes("p", members);
		if (hasWrapperElement)
		{
			WriteReadNonRoots();
			if (membersMapping.ValidateRpcWrapperElement)
			{
				base.Writer.Write("if (!");
				WriteXmlNodeEqual("Reader", accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
				base.Writer.WriteLine(") throw CreateUnknownNodeException();");
			}
			base.Writer.WriteLine("bool isEmptyWrapper = Reader.IsEmptyElement;");
			base.Writer.WriteLine("Reader.ReadStartElement();");
		}
		Member[] array = new Member[members.Length];
		for (int i = 0; i < members.Length; i++)
		{
			MemberMapping memberMapping = members[i];
			string text2 = $"p[{i}]";
			string arraySource = text2;
			if (memberMapping.Xmlns != null)
			{
				arraySource = $"(({memberMapping.TypeDesc.CSharpName}){text2})";
			}
			Member member = new Member(this, text2, arraySource, "a", i, memberMapping);
			if (!memberMapping.IsSequence)
			{
				member.ParamsReadSource = $"paramsRead[{i}]";
			}
			array[i] = member;
			if (memberMapping.CheckSpecified != SpecifiedAccessor.ReadWrite)
			{
				continue;
			}
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
		string fixupMethodName = "fixup_" + text;
		bool flag = WriteMemberFixupBegin(array, fixupMethodName, "p");
		if (array.Length != 0 && array[0].Mapping.IsReturnValue)
		{
			base.Writer.WriteLine("IsReturnValue = true;");
		}
		string text4 = ((!hasWrapperElement && !writeAccessors) ? "hrefList" : null);
		if (text4 != null)
		{
			WriteInitCheckTypeHrefList(text4);
		}
		WriteParamsRead(members.Length);
		WriteWhileNotLoopStart();
		base.Writer.Indent++;
		string elementElseString = ((text4 == null) ? "UnknownNode((object)p);" : "if (Reader.GetAttribute(\"id\", null) != null) { ReadReferencedElement(); } else { UnknownNode((object)p); }");
		WriteMemberElements(array, elementElseString, "UnknownNode((object)p);", null, null, text4);
		base.Writer.WriteLine("Reader.MoveToContent();");
		WriteWhileLoopEnd();
		if (hasWrapperElement)
		{
			base.Writer.WriteLine("if (!isEmptyWrapper) ReadEndElement();");
		}
		if (text4 != null)
		{
			WriteHandleHrefList(array, text4);
		}
		base.Writer.WriteLine("ReadReferencedElements();");
		base.Writer.WriteLine("return p;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (flag)
		{
			WriteFixupMethod(fixupMethodName, array, "object[]", useReflection: false, typed: false, "p");
		}
		return text;
	}

	private void WriteCreateCollection(TypeDesc td, string source)
	{
		bool useReflection = td.UseReflection;
		string text = ((td.ArrayElementTypeDesc == null) ? "object" : td.ArrayElementTypeDesc.CSharpName) + "[]";
		bool flag = td.ArrayElementTypeDesc != null && td.ArrayElementTypeDesc.UseReflection;
		if (flag)
		{
			text = typeof(Array).FullName;
		}
		base.Writer.Write(text);
		base.Writer.Write(" ");
		base.Writer.Write("ci =");
		base.Writer.Write("(" + text + ")");
		base.Writer.Write(source);
		base.Writer.WriteLine(";");
		base.Writer.WriteLine("for (int i = 0; i < ci.Length; i++) {");
		base.Writer.Indent++;
		base.Writer.Write(base.RaCodeGen.GetStringForMethod("c", td.CSharpName, "Add", useReflection));
		if (!flag)
		{
			base.Writer.Write("ci[i]");
		}
		else
		{
			base.Writer.Write(base.RaCodeGen.GetReflectionVariable(typeof(Array).FullName, "0") + "[ci , i]");
		}
		if (useReflection)
		{
			base.Writer.WriteLine("}");
		}
		base.Writer.WriteLine(");");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
	{
		ElementAccessor accessor = xmlTypeMapping.Accessor;
		TypeMapping mapping = accessor.Mapping;
		string text = NextMethodName(accessor.Name);
		base.Writer.WriteLine();
		base.Writer.Write("public object ");
		base.Writer.Write(text);
		base.Writer.WriteLine("() {");
		base.Writer.Indent++;
		base.Writer.WriteLine("object o = null;");
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.TypeDesc = mapping.TypeDesc;
		memberMapping.Elements = new ElementAccessor[1] { accessor };
		Member[] array = new Member[1]
		{
			new Member(this, "o", "o", "a", 0, memberMapping)
		};
		base.Writer.WriteLine("Reader.MoveToContent();");
		string elseString = "UnknownNode(null, " + ExpectedElements(array) + ");";
		WriteMemberElements(array, "throw CreateUnknownNodeException();", elseString, accessor.Any ? array[0] : null, null, null);
		if (accessor.IsSoap)
		{
			base.Writer.WriteLine("Referenced(o);");
			base.Writer.WriteLine("ReadReferencedElements();");
		}
		base.Writer.WriteLine("return (object)o;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
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

	private void WritePrimitive(TypeMapping mapping, string source)
	{
		if (mapping is EnumMapping)
		{
			string text = ReferenceMapping(mapping);
			if (text == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingMethodEnum, mapping.TypeDesc.Name));
			}
			if (mapping.IsSoap)
			{
				base.Writer.Write("(");
				base.Writer.Write(mapping.TypeDesc.CSharpName);
				base.Writer.Write(")");
			}
			base.Writer.Write(text);
			base.Writer.Write("(");
			if (!mapping.IsSoap)
			{
				base.Writer.Write(source);
			}
			base.Writer.Write(")");
		}
		else if (mapping.TypeDesc == base.StringTypeDesc)
		{
			base.Writer.Write(source);
		}
		else if (mapping.TypeDesc.FormatterName == "String")
		{
			if (mapping.TypeDesc.CollapseWhitespace)
			{
				base.Writer.Write("CollapseWhitespace(");
				base.Writer.Write(source);
				base.Writer.Write(")");
			}
			else
			{
				base.Writer.Write(source);
			}
		}
		else
		{
			if (!mapping.TypeDesc.HasCustomFormatter)
			{
				base.Writer.Write(typeof(XmlConvert).FullName);
				base.Writer.Write(".");
			}
			base.Writer.Write("To");
			base.Writer.Write(mapping.TypeDesc.FormatterName);
			base.Writer.Write("(");
			base.Writer.Write(source);
			base.Writer.Write(")");
		}
	}

	private string MakeUnique(EnumMapping mapping, string name)
	{
		string text = name;
		object obj = Enums[text];
		if (obj != null)
		{
			if (obj == mapping)
			{
				return null;
			}
			int num = 0;
			while (obj != null)
			{
				num++;
				text = name + num.ToString(CultureInfo.InvariantCulture);
				obj = Enums[text];
			}
		}
		Enums.Add(text, mapping);
		return text;
	}

	private string WriteHashtable(EnumMapping mapping, string typeName)
	{
		CodeIdentifier.CheckValidIdentifier(typeName);
		string text = MakeUnique(mapping, typeName + "Values");
		if (text == null)
		{
			return CodeIdentifier.GetCSharpName(typeName);
		}
		string s = MakeUnique(mapping, "_" + text);
		text = CodeIdentifier.GetCSharpName(text);
		base.Writer.WriteLine();
		base.Writer.Write(typeof(Hashtable).FullName);
		base.Writer.Write(" ");
		base.Writer.Write(s);
		base.Writer.WriteLine(";");
		base.Writer.WriteLine();
		base.Writer.Write("internal ");
		base.Writer.Write(typeof(Hashtable).FullName);
		base.Writer.Write(" ");
		base.Writer.Write(text);
		base.Writer.WriteLine(" {");
		base.Writer.Indent++;
		base.Writer.WriteLine("get {");
		base.Writer.Indent++;
		base.Writer.Write("if ((object)");
		base.Writer.Write(s);
		base.Writer.WriteLine(" == null) {");
		base.Writer.Indent++;
		base.Writer.Write(typeof(Hashtable).FullName);
		base.Writer.Write(" h = new ");
		base.Writer.Write(typeof(Hashtable).FullName);
		base.Writer.WriteLine("();");
		ConstantMapping[] constants = mapping.Constants;
		for (int i = 0; i < constants.Length; i++)
		{
			base.Writer.Write("h.Add(");
			WriteQuotedCSharpString(constants[i].XmlName);
			if (!mapping.TypeDesc.UseReflection)
			{
				base.Writer.Write(", (long)");
				base.Writer.Write(mapping.TypeDesc.CSharpName);
				base.Writer.Write(".@");
				CodeIdentifier.CheckValidIdentifier(constants[i].Name);
				base.Writer.Write(constants[i].Name);
			}
			else
			{
				base.Writer.Write(", ");
				IndentedWriter writer = base.Writer;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(1, 1, invariantCulture);
				handler.AppendFormatted(constants[i].Value);
				handler.AppendLiteral("L");
				writer.Write(string.Create(invariantCulture, ref handler));
			}
			base.Writer.WriteLine(");");
		}
		base.Writer.Write(s);
		base.Writer.WriteLine(" = h;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Write("return ");
		base.Writer.Write(s);
		base.Writer.WriteLine(";");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		return text;
	}

	private void WriteEnumMethod(EnumMapping mapping)
	{
		string s = null;
		if (mapping.IsFlags)
		{
			s = WriteHashtable(mapping, mapping.TypeDesc.Name);
		}
		string s2 = (string)base.MethodNames[mapping];
		base.Writer.WriteLine();
		bool useReflection = mapping.TypeDesc.UseReflection;
		string cSharpName = mapping.TypeDesc.CSharpName;
		if (mapping.IsSoap)
		{
			base.Writer.Write("object");
			base.Writer.Write(" ");
			base.Writer.Write(s2);
			base.Writer.WriteLine("() {");
			base.Writer.Indent++;
			base.Writer.WriteLine("string s = Reader.ReadElementString();");
		}
		else
		{
			base.Writer.Write(useReflection ? "object" : cSharpName);
			base.Writer.Write(" ");
			base.Writer.Write(s2);
			base.Writer.WriteLine("(string s) {");
			base.Writer.Indent++;
		}
		ConstantMapping[] constants = mapping.Constants;
		if (mapping.IsFlags)
		{
			if (useReflection)
			{
				base.Writer.Write("return ");
				base.Writer.Write(typeof(Enum).FullName);
				base.Writer.Write(".ToObject(");
				base.Writer.Write(base.RaCodeGen.GetStringForTypeof(cSharpName, useReflection));
				base.Writer.Write(", ToEnum(s, ");
				base.Writer.Write(s);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(cSharpName);
				base.Writer.WriteLine("));");
			}
			else
			{
				base.Writer.Write("return (");
				base.Writer.Write(cSharpName);
				base.Writer.Write(")ToEnum(s, ");
				base.Writer.Write(s);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(cSharpName);
				base.Writer.WriteLine(");");
			}
		}
		else
		{
			base.Writer.WriteLine("switch (s) {");
			base.Writer.Indent++;
			Hashtable hashtable = new Hashtable();
			foreach (ConstantMapping constantMapping in constants)
			{
				CodeIdentifier.CheckValidIdentifier(constantMapping.Name);
				if (hashtable[constantMapping.XmlName] == null)
				{
					base.Writer.Write("case ");
					WriteQuotedCSharpString(constantMapping.XmlName);
					base.Writer.Write(": return ");
					base.Writer.Write(base.RaCodeGen.GetStringForEnumMember(cSharpName, constantMapping.Name, useReflection));
					base.Writer.WriteLine(";");
					hashtable[constantMapping.XmlName] = constantMapping.XmlName;
				}
			}
			base.Writer.Write("default: throw CreateUnknownConstantException(s, ");
			base.Writer.Write(base.RaCodeGen.GetStringForTypeof(cSharpName, useReflection));
			base.Writer.WriteLine(");");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteDerivedTypes(StructMapping mapping, bool isTypedReturn, string returnTypeName)
	{
		for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
		{
			base.Writer.Write("if (");
			WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
			base.Writer.WriteLine(")");
			base.Writer.Indent++;
			string s = ReferenceMapping(structMapping);
			base.Writer.Write("return ");
			if (structMapping.TypeDesc.UseReflection && isTypedReturn)
			{
				base.Writer.Write("(" + returnTypeName + ")");
			}
			base.Writer.Write(s);
			base.Writer.Write("(");
			if (structMapping.TypeDesc.IsNullable)
			{
				base.Writer.Write("isNullable, ");
			}
			base.Writer.WriteLine("false);");
			base.Writer.Indent--;
			WriteDerivedTypes(structMapping, isTypedReturn, returnTypeName);
		}
	}

	private void WriteEnumAndArrayTypes()
	{
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (Mapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping.IsSoap)
				{
					continue;
				}
				if (typeMapping is EnumMapping)
				{
					EnumMapping enumMapping = (EnumMapping)typeMapping;
					base.Writer.Write("if (");
					WriteQNameEqual("xsiType", enumMapping.TypeName, enumMapping.Namespace);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
					base.Writer.WriteLine("Reader.ReadStartElement();");
					string s = ReferenceMapping(enumMapping);
					base.Writer.Write("object e = ");
					base.Writer.Write(s);
					base.Writer.WriteLine("(CollapseWhitespace(Reader.ReadString()));");
					base.Writer.WriteLine("ReadEndElement();");
					base.Writer.WriteLine("return e;");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				else
				{
					if (!(typeMapping is ArrayMapping))
					{
						continue;
					}
					ArrayMapping arrayMapping = (ArrayMapping)typeMapping;
					if (!arrayMapping.TypeDesc.HasDefaultConstructor)
					{
						continue;
					}
					base.Writer.Write("if (");
					WriteQNameEqual("xsiType", arrayMapping.TypeName, arrayMapping.Namespace);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
					MemberMapping memberMapping = new MemberMapping();
					memberMapping.TypeDesc = arrayMapping.TypeDesc;
					memberMapping.Elements = arrayMapping.Elements;
					Member member = new Member(this, "a", "z", 0, memberMapping);
					TypeDesc typeDesc = arrayMapping.TypeDesc;
					string cSharpName = arrayMapping.TypeDesc.CSharpName;
					if (typeDesc.UseReflection)
					{
						if (typeDesc.IsArray)
						{
							base.Writer.Write(typeof(Array).FullName);
						}
						else
						{
							base.Writer.Write("object");
						}
					}
					else
					{
						base.Writer.Write(cSharpName);
					}
					base.Writer.Write(" a = ");
					if (arrayMapping.TypeDesc.IsValueType)
					{
						base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(cSharpName, typeDesc.UseReflection, ctorInaccessible: false, cast: false));
						base.Writer.WriteLine(";");
					}
					else
					{
						base.Writer.WriteLine("null;");
					}
					WriteArray(member.Source, member.ArrayName, arrayMapping, readOnly: false, isNullable: false, -1);
					base.Writer.WriteLine("return a;");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
		}
	}

	private void WriteNullableMethod(NullableMapping nullableMapping)
	{
		string s = (string)base.MethodNames[nullableMapping];
		bool useReflection = nullableMapping.BaseMapping.TypeDesc.UseReflection;
		string s2 = (useReflection ? "object" : nullableMapping.TypeDesc.CSharpName);
		base.Writer.WriteLine();
		base.Writer.Write(s2);
		base.Writer.Write(" ");
		base.Writer.Write(s);
		base.Writer.WriteLine("(bool checkType) {");
		base.Writer.Indent++;
		base.Writer.Write(s2);
		base.Writer.Write(" o = ");
		if (useReflection)
		{
			base.Writer.Write("null");
		}
		else
		{
			base.Writer.Write("default(");
			base.Writer.Write(s2);
			base.Writer.Write(")");
		}
		base.Writer.WriteLine(";");
		base.Writer.WriteLine("if (ReadNull())");
		base.Writer.Indent++;
		base.Writer.WriteLine("return o;");
		base.Writer.Indent--;
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor.Mapping = nullableMapping.BaseMapping;
		elementAccessor.Any = false;
		elementAccessor.IsNullable = nullableMapping.BaseMapping.TypeDesc.IsNullable;
		WriteElement("o", null, null, elementAccessor, null, null, checkForNull: false, readOnly: false, -1, -1);
		base.Writer.WriteLine("return o;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteStructMethod(StructMapping structMapping)
	{
		if (structMapping.IsSoap)
		{
			WriteEncodedStructMethod(structMapping);
		}
		else
		{
			WriteLiteralStructMethod(structMapping);
		}
	}

	private void WriteLiteralStructMethod(StructMapping structMapping)
	{
		string s = (string)base.MethodNames[structMapping];
		bool useReflection = structMapping.TypeDesc.UseReflection;
		string text = (useReflection ? "object" : structMapping.TypeDesc.CSharpName);
		base.Writer.WriteLine();
		base.Writer.Write(text);
		base.Writer.Write(" ");
		base.Writer.Write(s);
		base.Writer.Write("(");
		if (structMapping.TypeDesc.IsNullable)
		{
			base.Writer.Write("bool isNullable, ");
		}
		base.Writer.WriteLine("bool checkType) {");
		base.Writer.Indent++;
		base.Writer.Write(typeof(XmlQualifiedName).FullName);
		base.Writer.WriteLine(" xsiType = checkType ? GetXsiType() : null;");
		base.Writer.WriteLine("bool isNull = false;");
		if (structMapping.TypeDesc.IsNullable)
		{
			base.Writer.WriteLine("if (isNullable) isNull = ReadNull();");
		}
		base.Writer.WriteLine("if (checkType) {");
		if (structMapping.TypeDesc.IsRoot)
		{
			base.Writer.Indent++;
			base.Writer.WriteLine("if (isNull) {");
			base.Writer.Indent++;
			base.Writer.WriteLine("if (xsiType != null) return (" + text + ")ReadTypedNull(xsiType);");
			base.Writer.Write("else return ");
			if (structMapping.TypeDesc.IsValueType)
			{
				base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(structMapping.TypeDesc.CSharpName, useReflection, ctorInaccessible: false, cast: false));
				base.Writer.WriteLine(";");
			}
			else
			{
				base.Writer.WriteLine("null;");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		base.Writer.Write("if (xsiType == null");
		if (!structMapping.TypeDesc.IsRoot)
		{
			base.Writer.Write(" || ");
			WriteQNameEqual("xsiType", structMapping.TypeName, structMapping.Namespace);
		}
		base.Writer.WriteLine(") {");
		if (structMapping.TypeDesc.IsRoot)
		{
			base.Writer.Indent++;
			base.Writer.WriteLine("return ReadTypedPrimitive(new System.Xml.XmlQualifiedName(\"anyType\", \"http://www.w3.org/2001/XMLSchema\"));");
			base.Writer.Indent--;
		}
		base.Writer.WriteLine("}");
		base.Writer.WriteLine("else {");
		base.Writer.Indent++;
		WriteDerivedTypes(structMapping, !useReflection && !structMapping.TypeDesc.IsRoot, text);
		if (structMapping.TypeDesc.IsRoot)
		{
			WriteEnumAndArrayTypes();
		}
		if (structMapping.TypeDesc.IsRoot)
		{
			base.Writer.Write("return ReadTypedPrimitive((");
		}
		else
		{
			base.Writer.Write("throw CreateUnknownTypeException((");
		}
		base.Writer.Write(typeof(XmlQualifiedName).FullName);
		base.Writer.WriteLine(")xsiType);");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (structMapping.TypeDesc.IsRoot)
		{
			base.Writer.Indent--;
		}
		base.Writer.WriteLine("}");
		if (structMapping.TypeDesc.IsNullable)
		{
			base.Writer.WriteLine("if (isNull) return null;");
		}
		if (structMapping.TypeDesc.IsAbstract)
		{
			base.Writer.Write("throw CreateAbstractTypeException(");
			WriteQuotedCSharpString(structMapping.TypeName);
			base.Writer.Write(", ");
			WriteQuotedCSharpString(structMapping.Namespace);
			base.Writer.WriteLine(");");
		}
		else
		{
			if (structMapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(structMapping.TypeDesc.Type))
			{
				base.Writer.WriteLine("DecodeName = false;");
			}
			WriteCreateMapping(structMapping, "o");
			MemberMapping[] settableMembers = TypeScope.GetSettableMembers(structMapping);
			Member member = null;
			Member member2 = null;
			Member member3 = null;
			bool flag = structMapping.HasExplicitSequence();
			List<Member> list = new List<Member>(settableMembers.Length);
			List<Member> list2 = new List<Member>(settableMembers.Length);
			List<Member> list3 = new List<Member>(settableMembers.Length);
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
					list3.Add(member5);
				}
				else
				{
					list3.Add(member4);
				}
				if (!memberMapping.TypeDesc.IsArrayLike)
				{
					continue;
				}
				list.Add(member4);
				if (memberMapping.TypeDesc.IsArrayLike && (memberMapping.Elements.Length != 1 || !(memberMapping.Elements[0].Mapping is ArrayMapping)))
				{
					member4.ParamsReadSource = null;
					if (member4 != member && member4 != member2)
					{
						list2.Add(member4);
					}
				}
				else if (!memberMapping.TypeDesc.IsArray)
				{
					member4.ParamsReadSource = null;
				}
			}
			if (member2 != null)
			{
				list2.Add(member2);
			}
			if (member != null && member != member2)
			{
				list2.Add(member);
			}
			Member[] members = list.ToArray();
			Member[] members2 = list2.ToArray();
			Member[] members3 = list3.ToArray();
			WriteMemberBegin(members);
			WriteParamsRead(settableMembers.Length);
			WriteAttributes(members3, member3, "UnknownNode", "(object)o");
			if (member3 != null)
			{
				WriteMemberEnd(members);
			}
			base.Writer.WriteLine("Reader.MoveToElement();");
			base.Writer.WriteLine("if (Reader.IsEmptyElement) {");
			base.Writer.Indent++;
			base.Writer.WriteLine("Reader.Skip();");
			WriteMemberEnd(members2);
			base.Writer.WriteLine("return o;");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			base.Writer.WriteLine("Reader.ReadStartElement();");
			if (IsSequence(members3))
			{
				base.Writer.WriteLine("int state = 0;");
			}
			WriteWhileNotLoopStart();
			base.Writer.Indent++;
			string text2 = "UnknownNode((object)o, " + ExpectedElements(members3) + ");";
			WriteMemberElements(members3, text2, text2, member2, member, null);
			base.Writer.WriteLine("Reader.MoveToContent();");
			WriteWhileLoopEnd();
			WriteMemberEnd(members2);
			base.Writer.WriteLine("ReadEndElement();");
			base.Writer.WriteLine("return o;");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteEncodedStructMethod(StructMapping structMapping)
	{
		if (structMapping.TypeDesc.IsRoot)
		{
			return;
		}
		string text = (string)base.MethodNames[structMapping];
		base.Writer.WriteLine();
		base.Writer.Write("object");
		base.Writer.Write(" ");
		base.Writer.Write(text);
		base.Writer.Write("(");
		base.Writer.WriteLine(") {");
		base.Writer.Indent++;
		Member[] array;
		bool flag;
		string fixupMethodName;
		if (structMapping.TypeDesc.IsAbstract)
		{
			base.Writer.Write("throw CreateAbstractTypeException(");
			WriteQuotedCSharpString(structMapping.TypeName);
			base.Writer.Write(", ");
			WriteQuotedCSharpString(structMapping.Namespace);
			base.Writer.WriteLine(");");
			array = Array.Empty<Member>();
			flag = false;
			fixupMethodName = null;
		}
		else
		{
			WriteCreateMapping(structMapping, "o");
			MemberMapping[] settableMembers = TypeScope.GetSettableMembers(structMapping);
			array = new Member[settableMembers.Length];
			for (int i = 0; i < settableMembers.Length; i++)
			{
				MemberMapping memberMapping = settableMembers[i];
				CodeIdentifier.CheckValidIdentifier(memberMapping.Name);
				string stringForMember = base.RaCodeGen.GetStringForMember("o", memberMapping.Name, structMapping.TypeDesc);
				Member member = new Member(this, stringForMember, stringForMember, "a", i, memberMapping, GetChoiceIdentifierSource(memberMapping, "o", structMapping.TypeDesc));
				if (memberMapping.CheckSpecified == SpecifiedAccessor.ReadWrite)
				{
					member.CheckSpecifiedSource = base.RaCodeGen.GetStringForMember("o", memberMapping.Name + "Specified", structMapping.TypeDesc);
				}
				if (!memberMapping.IsSequence)
				{
					member.ParamsReadSource = $"paramsRead[{i}]";
				}
				array[i] = member;
			}
			fixupMethodName = "fixup_" + text;
			flag = WriteMemberFixupBegin(array, fixupMethodName, "o");
			WriteParamsRead(settableMembers.Length);
			WriteAttributes(array, null, "UnknownNode", "(object)o");
			base.Writer.WriteLine("Reader.MoveToElement();");
			base.Writer.WriteLine("if (Reader.IsEmptyElement) { Reader.Skip(); return o; }");
			base.Writer.WriteLine("Reader.ReadStartElement();");
			WriteWhileNotLoopStart();
			base.Writer.Indent++;
			WriteMemberElements(array, "UnknownNode((object)o);", "UnknownNode((object)o);", null, null, null);
			base.Writer.WriteLine("Reader.MoveToContent();");
			WriteWhileLoopEnd();
			base.Writer.WriteLine("ReadEndElement();");
			base.Writer.WriteLine("return o;");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (flag)
		{
			WriteFixupMethod(fixupMethodName, array, structMapping.TypeDesc.CSharpName, structMapping.TypeDesc.UseReflection, typed: true, "o");
		}
	}

	private void WriteFixupMethod(string fixupMethodName, Member[] members, string typeName, bool useReflection, bool typed, string source)
	{
		base.Writer.WriteLine();
		base.Writer.Write("void ");
		base.Writer.Write(fixupMethodName);
		base.Writer.WriteLine("(object objFixup) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("Fixup fixup = (Fixup)objFixup;");
		WriteLocalDecl(typeName, source, "fixup.Source", useReflection);
		base.Writer.WriteLine("string[] ids = fixup.Ids;");
		foreach (Member member in members)
		{
			if (!member.MultiRef)
			{
				continue;
			}
			string text = member.FixupIndex.ToString(CultureInfo.InvariantCulture);
			base.Writer.Write("if (ids[");
			base.Writer.Write(text);
			base.Writer.WriteLine("] != null) {");
			base.Writer.Indent++;
			string arraySource = member.ArraySource;
			string text2 = "GetTarget(ids[" + text + "])";
			TypeDesc typeDesc = member.Mapping.TypeDesc;
			if (typeDesc.IsCollection || typeDesc.IsEnumerable)
			{
				WriteAddCollectionFixup(typeDesc, member.Mapping.ReadOnly, arraySource, text2);
			}
			else
			{
				if (typed)
				{
					base.Writer.WriteLine("try {");
					base.Writer.Indent++;
					WriteSourceBeginTyped(arraySource, member.Mapping.TypeDesc);
				}
				else
				{
					WriteSourceBegin(arraySource);
				}
				base.Writer.Write(text2);
				WriteSourceEnd(arraySource);
				base.Writer.WriteLine(";");
				if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
				{
					base.Writer.Write(member.CheckSpecifiedSource);
					base.Writer.WriteLine(" = true;");
				}
				if (typed)
				{
					WriteCatchCastException(member.Mapping.TypeDesc, text2, "ids[" + text + "]");
				}
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteAddCollectionFixup(TypeDesc typeDesc, bool readOnly, string memberSource, string targetSource)
	{
		base.Writer.WriteLine("// get array of the collection items");
		CreateCollectionInfo createCollectionInfo = (CreateCollectionInfo)_createMethods[typeDesc];
		if (createCollectionInfo == null)
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(7, 2, invariantCulture);
			handler.AppendLiteral("create");
			handler.AppendFormatted(++_nextCreateMethodNumber);
			handler.AppendLiteral("_");
			handler.AppendFormatted(typeDesc.Name);
			string name = string.Create(invariantCulture, ref handler);
			createCollectionInfo = new CreateCollectionInfo(name, typeDesc);
			_createMethods.Add(typeDesc, createCollectionInfo);
		}
		base.Writer.Write("if ((object)(");
		base.Writer.Write(memberSource);
		base.Writer.WriteLine(") == null) {");
		base.Writer.Indent++;
		if (readOnly)
		{
			base.Writer.Write("throw CreateReadOnlyCollectionException(");
			WriteQuotedCSharpString(typeDesc.CSharpName);
			base.Writer.WriteLine(");");
		}
		else
		{
			base.Writer.Write(memberSource);
			base.Writer.Write(" = ");
			base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(typeDesc.CSharpName, typeDesc.UseReflection, typeDesc.CannotNew, cast: true));
			base.Writer.WriteLine(";");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Write("CollectionFixup collectionFixup = new CollectionFixup(");
		base.Writer.Write(memberSource);
		base.Writer.Write(", ");
		base.Writer.Write("new ");
		base.Writer.Write(typeof(XmlSerializationCollectionFixupCallback).FullName);
		base.Writer.Write("(this.");
		base.Writer.Write(createCollectionInfo.Name);
		base.Writer.Write("), ");
		base.Writer.Write(targetSource);
		base.Writer.WriteLine(");");
		base.Writer.WriteLine("AddFixup(collectionFixup);");
	}

	private void WriteCreateCollectionMethod(CreateCollectionInfo c)
	{
		base.Writer.Write("void ");
		base.Writer.Write(c.Name);
		base.Writer.WriteLine("(object collection, object collectionItems) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("if (collectionItems == null) return;");
		base.Writer.WriteLine("if (collection == null) return;");
		TypeDesc typeDesc = c.TypeDesc;
		bool useReflection = typeDesc.UseReflection;
		string cSharpName = typeDesc.CSharpName;
		WriteLocalDecl(cSharpName, "c", "collection", useReflection);
		WriteCreateCollection(typeDesc, "collectionItems");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteQNameEqual(string source, string name, string ns)
	{
		base.Writer.Write("((object) ((");
		base.Writer.Write(typeof(XmlQualifiedName).FullName);
		base.Writer.Write(")");
		base.Writer.Write(source);
		base.Writer.Write(").Name == (object)");
		WriteID(name);
		base.Writer.Write(" && (object) ((");
		base.Writer.Write(typeof(XmlQualifiedName).FullName);
		base.Writer.Write(")");
		base.Writer.Write(source);
		base.Writer.Write(").Namespace == (object)");
		WriteID(ns);
		base.Writer.Write(")");
	}

	private void WriteXmlNodeEqual(string source, string name, string ns)
	{
		base.Writer.Write("(");
		if (name != null && name.Length > 0)
		{
			base.Writer.Write("(object) ");
			base.Writer.Write(source);
			base.Writer.Write(".LocalName == (object)");
			WriteID(name);
			base.Writer.Write(" && ");
		}
		base.Writer.Write("(object) ");
		base.Writer.Write(source);
		base.Writer.Write(".NamespaceURI == (object)");
		WriteID(ns);
		base.Writer.Write(")");
	}

	private void WriteID(string name)
	{
		if (name == null)
		{
			name = "";
		}
		string text = (string)_idNames[name];
		if (text == null)
		{
			text = NextIdName(name);
			_idNames.Add(name, text);
		}
		base.Writer.Write(text);
	}

	private void WriteAttributes(Member[] members, Member anyAttribute, string elseCall, string firstParam)
	{
		int num = 0;
		Member member = null;
		ArrayList arrayList = new ArrayList();
		base.Writer.WriteLine("while (Reader.MoveToNextAttribute()) {");
		base.Writer.Indent++;
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
					arrayList.Add(attribute);
					if (num++ > 0)
					{
						base.Writer.Write("else ");
					}
					base.Writer.Write("if (");
					if (member2.ParamsReadSource != null)
					{
						base.Writer.Write("!");
						base.Writer.Write(member2.ParamsReadSource);
						base.Writer.Write(" && ");
					}
					if (attribute.IsSpecialXmlNamespace)
					{
						WriteXmlNodeEqual("Reader", attribute.Name, "http://www.w3.org/XML/1998/namespace");
					}
					else
					{
						WriteXmlNodeEqual("Reader", attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : "");
					}
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
					WriteAttribute(member2);
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
		}
		if (num > 0)
		{
			base.Writer.Write("else ");
		}
		if (member != null)
		{
			base.Writer.WriteLine("if (IsXmlnsAttribute(Reader.Name)) {");
			base.Writer.Indent++;
			base.Writer.Write("if (");
			base.Writer.Write(member.Source);
			base.Writer.Write(" == null) ");
			base.Writer.Write(member.Source);
			base.Writer.Write(" = new ");
			base.Writer.Write(member.Mapping.TypeDesc.CSharpName);
			base.Writer.WriteLine("();");
			base.Writer.Write("((" + member.Mapping.TypeDesc.CSharpName + ")" + member.ArraySource + ")");
			base.Writer.WriteLine(".Add(Reader.Name.Length == 5 ? \"\" : Reader.LocalName, Reader.Value);");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			base.Writer.WriteLine("else {");
			base.Writer.Indent++;
		}
		else
		{
			base.Writer.WriteLine("if (!IsXmlnsAttribute(Reader.Name)) {");
			base.Writer.Indent++;
		}
		if (anyAttribute != null)
		{
			base.Writer.Write(typeof(XmlAttribute).FullName);
			base.Writer.Write(" attr = ");
			base.Writer.Write("(");
			base.Writer.Write(typeof(XmlAttribute).FullName);
			base.Writer.WriteLine(") Document.ReadNode(Reader);");
			base.Writer.WriteLine("ParseWsdlArrayType(attr);");
			WriteAttribute(anyAttribute);
		}
		else
		{
			base.Writer.Write(elseCall);
			base.Writer.Write("(");
			base.Writer.Write(firstParam);
			if (arrayList.Count > 0)
			{
				base.Writer.Write(", ");
				string text = "";
				for (int j = 0; j < arrayList.Count; j++)
				{
					AttributeAccessor attributeAccessor = (AttributeAccessor)arrayList[j];
					if (j > 0)
					{
						text += ", ";
					}
					text += (attributeAccessor.IsSpecialXmlNamespace ? "http://www.w3.org/XML/1998/namespace" : (((attributeAccessor.Form == XmlSchemaForm.Qualified) ? attributeAccessor.Namespace : "") + ":" + attributeAccessor.Name));
				}
				WriteQuotedCSharpString(text);
			}
			base.Writer.WriteLine(");");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteAttribute(Member member)
	{
		AttributeAccessor attribute = member.Mapping.Attribute;
		if (attribute.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)attribute.Mapping;
			if (specialMapping.TypeDesc.Kind == TypeKind.Attribute)
			{
				WriteSourceBegin(member.ArraySource);
				base.Writer.Write("attr");
				WriteSourceEnd(member.ArraySource);
				base.Writer.WriteLine(";");
			}
			else
			{
				if (!specialMapping.TypeDesc.CanBeAttributeValue)
				{
					throw new InvalidOperationException(System.SR.XmlInternalError);
				}
				base.Writer.Write("if (attr is ");
				base.Writer.Write(typeof(XmlAttribute).FullName);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				WriteSourceBegin(member.ArraySource);
				base.Writer.Write("(");
				base.Writer.Write(typeof(XmlAttribute).FullName);
				base.Writer.Write(")attr");
				WriteSourceEnd(member.ArraySource);
				base.Writer.WriteLine(";");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
		}
		else if (attribute.IsList)
		{
			base.Writer.WriteLine("string listValues = Reader.Value;");
			base.Writer.WriteLine("string[] vals = listValues.Split(null);");
			base.Writer.WriteLine("for (int i = 0; i < vals.Length; i++) {");
			base.Writer.Indent++;
			string arraySource = GetArraySource(member.Mapping.TypeDesc, member.ArrayName);
			WriteSourceBegin(arraySource);
			WritePrimitive(attribute.Mapping, "vals[i]");
			WriteSourceEnd(arraySource);
			base.Writer.WriteLine(";");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		else
		{
			WriteSourceBegin(member.ArraySource);
			WritePrimitive(attribute.Mapping, attribute.IsList ? "vals[i]" : "Reader.Value");
			WriteSourceEnd(member.ArraySource);
			base.Writer.WriteLine(";");
		}
		if (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite && member.CheckSpecifiedSource != null && member.CheckSpecifiedSource.Length > 0)
		{
			base.Writer.Write(member.CheckSpecifiedSource);
			base.Writer.WriteLine(" = true;");
		}
		if (member.ParamsReadSource != null)
		{
			base.Writer.Write(member.ParamsReadSource);
			base.Writer.WriteLine(" = true;");
		}
	}

	private bool WriteMemberFixupBegin(Member[] members, string fixupMethodName, string source)
	{
		int num = 0;
		foreach (Member member in members)
		{
			if (member.Mapping.Elements.Length != 0)
			{
				TypeMapping mapping = member.Mapping.Elements[0].Mapping;
				if (mapping is StructMapping || mapping is ArrayMapping || mapping is PrimitiveMapping || mapping is NullableMapping)
				{
					member.MultiRef = true;
					member.FixupIndex = num++;
				}
			}
		}
		if (num > 0)
		{
			base.Writer.Write("Fixup fixup = new Fixup(");
			base.Writer.Write(source);
			base.Writer.Write(", ");
			base.Writer.Write("new ");
			base.Writer.Write(typeof(XmlSerializationFixupCallback).FullName);
			base.Writer.Write("(this.");
			base.Writer.Write(fixupMethodName);
			base.Writer.Write("), ");
			base.Writer.Write(num.ToString(CultureInfo.InvariantCulture));
			base.Writer.WriteLine(");");
			base.Writer.WriteLine("AddFixup(fixup);");
			return true;
		}
		return false;
	}

	private void WriteMemberBegin(Member[] members)
	{
		foreach (Member member in members)
		{
			if (!member.IsArrayLike)
			{
				continue;
			}
			string arrayName = member.ArrayName;
			string s = "c" + arrayName;
			TypeDesc typeDesc = member.Mapping.TypeDesc;
			string cSharpName = typeDesc.CSharpName;
			if (member.Mapping.TypeDesc.IsArray)
			{
				WriteArrayLocalDecl(typeDesc.CSharpName, arrayName, "null", typeDesc);
				base.Writer.Write("int ");
				base.Writer.Write(s);
				base.Writer.WriteLine(" = 0;");
				if (member.Mapping.ChoiceIdentifier != null)
				{
					WriteArrayLocalDecl(member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName + "[]", member.ChoiceArrayName, "null", member.Mapping.ChoiceIdentifier.Mapping.TypeDesc);
					base.Writer.Write("int c");
					base.Writer.Write(member.ChoiceArrayName);
					base.Writer.WriteLine(" = 0;");
				}
				continue;
			}
			bool useReflection = typeDesc.UseReflection;
			if (member.Source[member.Source.Length - 1] == '(' || member.Source[member.Source.Length - 1] == '{')
			{
				WriteCreateInstance(cSharpName, arrayName, useReflection, typeDesc.CannotNew);
				base.Writer.Write(member.Source);
				base.Writer.Write(arrayName);
				if (member.Source[member.Source.Length - 1] == '{')
				{
					base.Writer.WriteLine("});");
				}
				else
				{
					base.Writer.WriteLine(");");
				}
				continue;
			}
			if (member.IsList && !member.Mapping.ReadOnly && member.Mapping.TypeDesc.IsNullable)
			{
				base.Writer.Write("if ((object)(");
				base.Writer.Write(member.Source);
				base.Writer.Write(") == null) ");
				if (!member.Mapping.TypeDesc.HasDefaultConstructor)
				{
					base.Writer.Write("throw CreateReadOnlyCollectionException(");
					WriteQuotedCSharpString(member.Mapping.TypeDesc.CSharpName);
					base.Writer.WriteLine(");");
				}
				else
				{
					base.Writer.Write(member.Source);
					base.Writer.Write(" = ");
					base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(cSharpName, useReflection, typeDesc.CannotNew, cast: true));
					base.Writer.WriteLine(";");
				}
			}
			WriteLocalDecl(cSharpName, arrayName, member.Source, useReflection);
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
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		ReflectionAwareCodeGen.WriteQuotedCSharpString(new IndentedWriter(stringWriter, compact: true), text);
		return stringWriter.ToString();
	}

	private void WriteMemberElements(Member[] members, string elementElseString, string elseString, Member anyElement, Member anyText, string checkTypeHrefsSource)
	{
		bool flag = checkTypeHrefsSource != null && checkTypeHrefsSource.Length > 0;
		if (anyText != null)
		{
			base.Writer.WriteLine("string tmp = null;");
		}
		base.Writer.Write("if (Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".Element) {");
		base.Writer.Indent++;
		if (flag)
		{
			WriteIfNotSoapRoot(elementElseString + " continue;");
			WriteMemberElementsCheckType(checkTypeHrefsSource);
		}
		else
		{
			WriteMemberElementsIf(members, anyElement, elementElseString, null);
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (anyText != null)
		{
			WriteMemberText(anyText, elseString);
		}
		base.Writer.WriteLine("else {");
		base.Writer.Indent++;
		base.Writer.WriteLine(elseString);
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteMemberText(Member anyText, string elseString)
	{
		base.Writer.Write("else if (Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".Text || ");
		base.Writer.Write("Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".CDATA || ");
		base.Writer.Write("Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".Whitespace || ");
		base.Writer.Write("Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".SignificantWhitespace) {");
		base.Writer.Indent++;
		if (anyText != null)
		{
			WriteText(anyText);
		}
		else
		{
			base.Writer.Write(elseString);
			base.Writer.WriteLine(";");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteText(Member member)
	{
		TextAccessor text = member.Mapping.Text;
		if (text.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)text.Mapping;
			WriteSourceBeginTyped(member.ArraySource, specialMapping.TypeDesc);
			TypeKind kind = specialMapping.TypeDesc.Kind;
			if (kind != TypeKind.Node)
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			base.Writer.Write("Document.CreateTextNode(Reader.ReadString())");
			WriteSourceEnd(member.ArraySource);
		}
		else
		{
			if (member.IsArrayLike)
			{
				WriteSourceBegin(member.ArraySource);
				if (text.Mapping.TypeDesc.CollapseWhitespace)
				{
					base.Writer.Write("CollapseWhitespace(Reader.ReadString())");
				}
				else
				{
					base.Writer.Write("Reader.ReadString()");
				}
			}
			else if (text.Mapping.TypeDesc == base.StringTypeDesc || text.Mapping.TypeDesc.FormatterName == "String")
			{
				base.Writer.Write("tmp = ReadString(tmp, ");
				if (text.Mapping.TypeDesc.CollapseWhitespace)
				{
					base.Writer.WriteLine("true);");
				}
				else
				{
					base.Writer.WriteLine("false);");
				}
				WriteSourceBegin(member.ArraySource);
				base.Writer.Write("tmp");
			}
			else
			{
				WriteSourceBegin(member.ArraySource);
				WritePrimitive(text.Mapping, "Reader.ReadString()");
			}
			WriteSourceEnd(member.ArraySource);
		}
		base.Writer.WriteLine(";");
	}

	private void WriteMemberElementsCheckType(string checkTypeHrefsSource)
	{
		base.Writer.WriteLine("string refElemId = null;");
		base.Writer.WriteLine("object refElem = ReadReferencingElement(null, null, true, out refElemId);");
		base.Writer.WriteLine("if (refElemId != null) {");
		base.Writer.Indent++;
		base.Writer.Write(checkTypeHrefsSource);
		base.Writer.WriteLine(".Add(refElemId);");
		base.Writer.Write(checkTypeHrefsSource);
		base.Writer.WriteLine("IsObject.Add(false);");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.WriteLine("else if (refElem != null) {");
		base.Writer.Indent++;
		base.Writer.Write(checkTypeHrefsSource);
		base.Writer.WriteLine(".Add(refElem);");
		base.Writer.Write(checkTypeHrefsSource);
		base.Writer.WriteLine("IsObject.Add(true);");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

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
			base.Writer.WriteLine(elementElseString);
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

	private void WriteMemberElementsIf(Member[] members, Member anyElement, string elementElseString, string checkTypeSource)
	{
		bool flag = checkTypeSource != null && checkTypeSource.Length > 0;
		int num = 0;
		bool flag2 = IsSequence(members);
		if (flag2)
		{
			base.Writer.WriteLine("switch (state) {");
		}
		int num2 = 0;
		foreach (Member member in members)
		{
			if (member.Mapping.Xmlns != null || member.Mapping.Ignore || (flag2 && (member.Mapping.IsText || member.Mapping.IsAttribute)))
			{
				continue;
			}
			bool flag3 = true;
			ChoiceIdentifierAccessor choiceIdentifier = member.Mapping.ChoiceIdentifier;
			ElementAccessor[] elements = member.Mapping.Elements;
			for (int j = 0; j < elements.Length; j++)
			{
				ElementAccessor elementAccessor = elements[j];
				string ns = ((elementAccessor.Form == XmlSchemaForm.Qualified) ? elementAccessor.Namespace : "");
				if (!flag2 && elementAccessor.Any && (elementAccessor.Name == null || elementAccessor.Name.Length == 0))
				{
					continue;
				}
				if (!flag2)
				{
					if (flag3 && num == 0)
					{
						base.Writer.WriteLine("do {");
						base.Writer.Indent++;
					}
				}
				else if (!flag3 || (!flag2 && num > 0))
				{
					base.Writer.Write("else ");
				}
				else if (flag2)
				{
					base.Writer.Write("case ");
					base.Writer.Write(num2.ToString(CultureInfo.InvariantCulture));
					base.Writer.WriteLine(":");
					base.Writer.Indent++;
				}
				num++;
				flag3 = false;
				base.Writer.Write("if (");
				if (member.ParamsReadSource != null)
				{
					base.Writer.Write("!");
					base.Writer.Write(member.ParamsReadSource);
					base.Writer.Write(" && ");
				}
				if (flag)
				{
					if (elementAccessor.Mapping is NullableMapping)
					{
						TypeDesc typeDesc = ((NullableMapping)elementAccessor.Mapping).BaseMapping.TypeDesc;
						base.Writer.Write(base.RaCodeGen.GetStringForTypeof(typeDesc.CSharpName, typeDesc.UseReflection));
					}
					else
					{
						base.Writer.Write(base.RaCodeGen.GetStringForTypeof(elementAccessor.Mapping.TypeDesc.CSharpName, elementAccessor.Mapping.TypeDesc.UseReflection));
					}
					base.Writer.Write(".IsAssignableFrom(");
					base.Writer.Write(checkTypeSource);
					base.Writer.Write("Type)");
				}
				else
				{
					if (member.Mapping.IsReturnValue)
					{
						base.Writer.Write("(IsReturnValue || ");
					}
					if (flag2 && elementAccessor.Any && elementAccessor.AnyNamespaces == null)
					{
						base.Writer.Write("true");
					}
					else
					{
						WriteXmlNodeEqual("Reader", elementAccessor.Name, ns);
					}
					if (member.Mapping.IsReturnValue)
					{
						base.Writer.Write(")");
					}
				}
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				if (flag)
				{
					if (elementAccessor.Mapping.TypeDesc.IsValueType || elementAccessor.Mapping is NullableMapping)
					{
						base.Writer.Write("if (");
						base.Writer.Write(checkTypeSource);
						base.Writer.WriteLine(" != null) {");
						base.Writer.Indent++;
					}
					if (elementAccessor.Mapping is NullableMapping)
					{
						WriteSourceBegin(member.ArraySource);
						TypeDesc typeDesc2 = ((NullableMapping)elementAccessor.Mapping).BaseMapping.TypeDesc;
						base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(elementAccessor.Mapping.TypeDesc.CSharpName, elementAccessor.Mapping.TypeDesc.UseReflection, ctorInaccessible: false, cast: true, "(" + typeDesc2.CSharpName + ")" + checkTypeSource));
					}
					else
					{
						WriteSourceBeginTyped(member.ArraySource, elementAccessor.Mapping.TypeDesc);
						base.Writer.Write(checkTypeSource);
					}
					WriteSourceEnd(member.ArraySource);
					base.Writer.WriteLine(";");
					if (elementAccessor.Mapping.TypeDesc.IsValueType)
					{
						base.Writer.Indent--;
						base.Writer.WriteLine("}");
					}
					if (member.FixupIndex >= 0)
					{
						base.Writer.Write("fixup.Ids[");
						base.Writer.Write(member.FixupIndex.ToString(CultureInfo.InvariantCulture));
						base.Writer.Write("] = ");
						base.Writer.Write(checkTypeSource);
						base.Writer.WriteLine("Id;");
					}
				}
				else
				{
					WriteElement(member.ArraySource, member.ArrayName, member.ChoiceArraySource, elementAccessor, choiceIdentifier, (member.Mapping.CheckSpecified == SpecifiedAccessor.ReadWrite) ? member.CheckSpecifiedSource : null, member.IsList && member.Mapping.TypeDesc.IsNullable, member.Mapping.ReadOnly, member.FixupIndex, j);
				}
				if (member.Mapping.IsReturnValue)
				{
					base.Writer.WriteLine("IsReturnValue = false;");
				}
				if (member.ParamsReadSource != null)
				{
					base.Writer.Write(member.ParamsReadSource);
					base.Writer.WriteLine(" = true;");
				}
				if (!flag2)
				{
					base.Writer.WriteLine("break;");
				}
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			if (flag2)
			{
				if (member.IsArrayLike)
				{
					base.Writer.WriteLine("else {");
					base.Writer.Indent++;
				}
				num2++;
				base.Writer.Write("state = ");
				base.Writer.Write(num2.ToString(CultureInfo.InvariantCulture));
				base.Writer.WriteLine(";");
				if (member.IsArrayLike)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				base.Writer.WriteLine("break;");
				base.Writer.Indent--;
			}
		}
		if (num > 0 && flag2)
		{
			base.Writer.WriteLine("default:");
			base.Writer.Indent++;
		}
		WriteMemberElementsElse(anyElement, elementElseString);
		if (num > 0)
		{
			if (flag2)
			{
				base.Writer.WriteLine("break;");
			}
			base.Writer.Indent--;
			if (!flag2)
			{
				base.Writer.WriteLine("} while (false);");
			}
			else
			{
				base.Writer.WriteLine("}");
			}
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
		bool useReflection = typeDesc.UseReflection;
		if (typeDesc.IsArray)
		{
			string cSharpName = typeDesc.ArrayElementTypeDesc.CSharpName;
			bool useReflection2 = typeDesc.ArrayElementTypeDesc.UseReflection;
			string text3 = (useReflection ? "" : ("(" + cSharpName + "[])"));
			text2 = text2 + arrayName + " = " + text3 + "EnsureArrayIndex(" + arrayName + ", " + text + ", " + base.RaCodeGen.GetStringForTypeof(cSharpName, useReflection2) + ");";
			string stringForArrayMember = base.RaCodeGen.GetStringForArrayMember(arrayName, text + "++", typeDesc);
			if (multiRef)
			{
				text2 = text2 + " soap[1] = " + arrayName + ";";
				text2 = text2 + " if (ReadReference(out soap[" + text + "+2])) " + stringForArrayMember + " = null; else ";
			}
			return text2 + stringForArrayMember;
		}
		return base.RaCodeGen.GetStringForMethod(arrayName, typeDesc.CSharpName, "Add", useReflection);
	}

	private void WriteMemberEnd(Member[] members)
	{
		WriteMemberEnd(members, soapRefs: false);
	}

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
				if (soapRefs)
				{
					base.Writer.Write(" soap[1] = ");
				}
				string arrayName = member.ArrayName;
				string s = "c" + arrayName;
				bool useReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
				string cSharpName = typeDesc.ArrayElementTypeDesc.CSharpName;
				if (!useReflection)
				{
					base.Writer.Write("(" + cSharpName + "[])");
				}
				base.Writer.Write("ShrinkArray(");
				base.Writer.Write(arrayName);
				base.Writer.Write(", ");
				base.Writer.Write(s);
				base.Writer.Write(", ");
				base.Writer.Write(base.RaCodeGen.GetStringForTypeof(cSharpName, useReflection));
				base.Writer.Write(", ");
				WriteBooleanValue(member.IsNullable);
				base.Writer.Write(")");
				WriteSourceEnd(member.Source);
				base.Writer.WriteLine(";");
				if (member.Mapping.ChoiceIdentifier != null)
				{
					WriteSourceBegin(member.ChoiceSource);
					arrayName = member.ChoiceArrayName;
					s = "c" + arrayName;
					bool useReflection2 = member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.UseReflection;
					string cSharpName2 = member.Mapping.ChoiceIdentifier.Mapping.TypeDesc.CSharpName;
					if (!useReflection2)
					{
						base.Writer.Write("(" + cSharpName2 + "[])");
					}
					base.Writer.Write("ShrinkArray(");
					base.Writer.Write(arrayName);
					base.Writer.Write(", ");
					base.Writer.Write(s);
					base.Writer.Write(", ");
					base.Writer.Write(base.RaCodeGen.GetStringForTypeof(cSharpName2, useReflection2));
					base.Writer.Write(", ");
					WriteBooleanValue(member.IsNullable);
					base.Writer.Write(")");
					WriteSourceEnd(member.ChoiceSource);
					base.Writer.WriteLine(";");
				}
			}
			else if (typeDesc.IsValueType)
			{
				base.Writer.Write(member.Source);
				base.Writer.Write(" = ");
				base.Writer.Write(member.ArrayName);
				base.Writer.WriteLine(";");
			}
		}
	}

	private void WriteSourceBeginTyped(string source, TypeDesc typeDesc)
	{
		WriteSourceBegin(source);
		if (typeDesc != null && !typeDesc.UseReflection)
		{
			base.Writer.Write("(");
			base.Writer.Write(typeDesc.CSharpName);
			base.Writer.Write(")");
		}
	}

	private void WriteSourceBegin(string source)
	{
		base.Writer.Write(source);
		if (source[source.Length - 1] != '(' && source[source.Length - 1] != '{')
		{
			base.Writer.Write(" = ");
		}
	}

	private void WriteSourceEnd(string source)
	{
		if (source[source.Length - 1] == '(')
		{
			base.Writer.Write(")");
		}
		else if (source[source.Length - 1] == '{')
		{
			base.Writer.Write("})");
		}
	}

	private void WriteArray(string source, string arrayName, ArrayMapping arrayMapping, bool readOnly, bool isNullable, int fixupIndex)
	{
		if (arrayMapping.IsSoap)
		{
			base.Writer.Write("object rre = ");
			base.Writer.Write((fixupIndex >= 0) ? "ReadReferencingElement" : "ReadReferencedElement");
			base.Writer.Write("(");
			WriteID(arrayMapping.TypeName);
			base.Writer.Write(", ");
			WriteID(arrayMapping.Namespace);
			if (fixupIndex >= 0)
			{
				base.Writer.Write(", ");
				base.Writer.Write("out fixup.Ids[");
				base.Writer.Write(fixupIndex.ToString(CultureInfo.InvariantCulture));
				base.Writer.Write("]");
			}
			base.Writer.WriteLine(");");
			TypeDesc typeDesc = arrayMapping.TypeDesc;
			if (typeDesc.IsEnumerable || typeDesc.IsCollection)
			{
				base.Writer.WriteLine("if (rre != null) {");
				base.Writer.Indent++;
				WriteAddCollectionFixup(typeDesc, readOnly, source, "rre");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			else
			{
				base.Writer.WriteLine("try {");
				base.Writer.Indent++;
				WriteSourceBeginTyped(source, arrayMapping.TypeDesc);
				base.Writer.Write("rre");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				WriteCatchCastException(arrayMapping.TypeDesc, "rre", null);
			}
			return;
		}
		base.Writer.WriteLine("if (!ReadNull()) {");
		base.Writer.Indent++;
		MemberMapping memberMapping = new MemberMapping();
		memberMapping.Elements = arrayMapping.Elements;
		memberMapping.TypeDesc = arrayMapping.TypeDesc;
		memberMapping.ReadOnly = readOnly;
		Member member = new Member(this, source, arrayName, 0, memberMapping, multiRef: false);
		member.IsNullable = false;
		Member[] members = new Member[1] { member };
		WriteMemberBegin(members);
		if (readOnly)
		{
			base.Writer.Write("if (((object)(");
			base.Writer.Write(member.ArrayName);
			base.Writer.Write(") == null) || ");
		}
		else
		{
			base.Writer.Write("if (");
		}
		base.Writer.WriteLine("(Reader.IsEmptyElement)) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("Reader.Skip();");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.WriteLine("else {");
		base.Writer.Indent++;
		base.Writer.WriteLine("Reader.ReadStartElement();");
		WriteWhileNotLoopStart();
		base.Writer.Indent++;
		string text = "UnknownNode(null, " + ExpectedElements(members) + ");";
		WriteMemberElements(members, text, text, null, null, null);
		base.Writer.WriteLine("Reader.MoveToContent();");
		WriteWhileLoopEnd();
		base.Writer.Indent--;
		base.Writer.WriteLine("ReadEndElement();");
		base.Writer.WriteLine("}");
		WriteMemberEnd(members, soapRefs: false);
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (isNullable)
		{
			base.Writer.WriteLine("else {");
			base.Writer.Indent++;
			member.IsNullable = true;
			WriteMemberBegin(members);
			WriteMemberEnd(members);
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void WriteElement(string source, string arrayName, string choiceSource, ElementAccessor element, ChoiceIdentifierAccessor choice, string checkSpecified, bool checkForNull, bool readOnly, int fixupIndex, int elementIndex)
	{
		if (checkSpecified != null && checkSpecified.Length > 0)
		{
			base.Writer.Write(checkSpecified);
			base.Writer.WriteLine(" = true;");
		}
		if (element.Mapping is ArrayMapping)
		{
			WriteArray(source, arrayName, (ArrayMapping)element.Mapping, readOnly, element.IsNullable, fixupIndex);
		}
		else if (element.Mapping is NullableMapping)
		{
			string s = ReferenceMapping(element.Mapping);
			WriteSourceBegin(source);
			base.Writer.Write(s);
			base.Writer.Write("(true)");
			WriteSourceEnd(source);
			base.Writer.WriteLine(";");
		}
		else if (!element.Mapping.IsSoap && element.Mapping is PrimitiveMapping)
		{
			if (element.IsNullable)
			{
				base.Writer.WriteLine("if (ReadNull()) {");
				base.Writer.Indent++;
				WriteSourceBegin(source);
				if (element.Mapping.TypeDesc.IsValueType)
				{
					base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(element.Mapping.TypeDesc.CSharpName, element.Mapping.TypeDesc.UseReflection, ctorInaccessible: false, cast: false));
				}
				else
				{
					base.Writer.Write("null");
				}
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				base.Writer.Write("else ");
			}
			if (element.Default != null && element.Default != DBNull.Value && element.Mapping.TypeDesc.IsValueType)
			{
				base.Writer.WriteLine("if (Reader.IsEmptyElement) {");
				base.Writer.Indent++;
				base.Writer.WriteLine("Reader.Skip();");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				base.Writer.WriteLine("else {");
			}
			else
			{
				base.Writer.WriteLine("{");
			}
			base.Writer.Indent++;
			if (element.Mapping.TypeDesc.Type == typeof(TimeSpan) || element.Mapping.TypeDesc.Type == typeof(DateTimeOffset))
			{
				base.Writer.WriteLine("if (Reader.IsEmptyElement) {");
				base.Writer.Indent++;
				base.Writer.WriteLine("Reader.Skip();");
				WriteSourceBegin(source);
				if (element.Mapping.TypeDesc.Type == typeof(TimeSpan))
				{
					base.Writer.Write("default(System.TimeSpan)");
				}
				else if (element.Mapping.TypeDesc.Type == typeof(DateTimeOffset))
				{
					base.Writer.Write("default(System.DateTimeOffset)");
				}
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				base.Writer.WriteLine("else {");
				base.Writer.Indent++;
				WriteSourceBegin(source);
				WritePrimitive(element.Mapping, "Reader.ReadElementString()");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			else
			{
				WriteSourceBegin(source);
				if (element.Mapping.TypeDesc == base.QnameTypeDesc)
				{
					base.Writer.Write("ReadElementQualifiedName()");
				}
				else
				{
					string formatterName = element.Mapping.TypeDesc.FormatterName;
					WritePrimitive(source: (!(formatterName == "ByteArrayBase64") && !(formatterName == "ByteArrayHex")) ? "Reader.ReadElementString()" : "false", mapping: element.Mapping);
				}
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		else if (element.Mapping is StructMapping || (element.Mapping.IsSoap && element.Mapping is PrimitiveMapping))
		{
			TypeMapping mapping = element.Mapping;
			if (mapping.IsSoap)
			{
				base.Writer.Write("object rre = ");
				base.Writer.Write((fixupIndex >= 0) ? "ReadReferencingElement" : "ReadReferencedElement");
				base.Writer.Write("(");
				WriteID(mapping.TypeName);
				base.Writer.Write(", ");
				WriteID(mapping.Namespace);
				if (fixupIndex >= 0)
				{
					base.Writer.Write(", out fixup.Ids[");
					base.Writer.Write(fixupIndex.ToString(CultureInfo.InvariantCulture));
					base.Writer.Write("]");
				}
				base.Writer.Write(")");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				if (mapping.TypeDesc.IsValueType)
				{
					base.Writer.WriteLine("if (rre != null) {");
					base.Writer.Indent++;
				}
				base.Writer.WriteLine("try {");
				base.Writer.Indent++;
				WriteSourceBeginTyped(source, mapping.TypeDesc);
				base.Writer.Write("rre");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				WriteCatchCastException(mapping.TypeDesc, "rre", null);
				base.Writer.Write("Referenced(");
				base.Writer.Write(source);
				base.Writer.WriteLine(");");
				if (mapping.TypeDesc.IsValueType)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
			else
			{
				string s2 = ReferenceMapping(mapping);
				if (checkForNull)
				{
					base.Writer.Write("if ((object)(");
					base.Writer.Write(arrayName);
					base.Writer.Write(") == null) Reader.Skip(); else ");
				}
				WriteSourceBegin(source);
				base.Writer.Write(s2);
				base.Writer.Write("(");
				if (mapping.TypeDesc.IsNullable)
				{
					WriteBooleanValue(element.IsNullable);
					base.Writer.Write(", ");
				}
				base.Writer.Write("true");
				base.Writer.Write(")");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
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
				bool flag2 = specialMapping.TypeDesc.FullName == typeof(XmlDocument).FullName;
				WriteSourceBeginTyped(source, specialMapping.TypeDesc);
				base.Writer.Write(flag2 ? "ReadXmlDocument(" : "ReadXmlNode(");
				base.Writer.Write(element.Any ? "false" : "true");
				base.Writer.Write(")");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				break;
			}
			case TypeKind.Serializable:
			{
				SerializableMapping serializableMapping = (SerializableMapping)element.Mapping;
				if (serializableMapping.DerivedMappings != null)
				{
					base.Writer.Write(typeof(XmlQualifiedName).FullName);
					base.Writer.WriteLine(" tser = GetXsiType();");
					base.Writer.Write("if (tser == null");
					base.Writer.Write(" || ");
					WriteQNameEqual("tser", serializableMapping.XsiType.Name, serializableMapping.XsiType.Namespace);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				WriteSourceBeginTyped(source, serializableMapping.TypeDesc);
				base.Writer.Write("ReadSerializable(( ");
				base.Writer.Write(typeof(IXmlSerializable).FullName);
				base.Writer.Write(")");
				base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(serializableMapping.TypeDesc.CSharpName, serializableMapping.TypeDesc.UseReflection, serializableMapping.TypeDesc.CannotNew, cast: false));
				bool flag = !element.Any && XmlSerializationCodeGen.IsWildcard(serializableMapping);
				if (flag)
				{
					base.Writer.WriteLine(", true");
				}
				base.Writer.Write(")");
				WriteSourceEnd(source);
				base.Writer.WriteLine(";");
				if (serializableMapping.DerivedMappings != null)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
					WriteDerivedSerializable(serializableMapping, serializableMapping, source, flag);
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
			string cSharpName = choice.Mapping.TypeDesc.CSharpName;
			base.Writer.Write(choiceSource);
			base.Writer.Write(" = ");
			CodeIdentifier.CheckValidIdentifier(choice.MemberIds[elementIndex]);
			base.Writer.Write(base.RaCodeGen.GetStringForEnumMember(cSharpName, choice.MemberIds[elementIndex], choice.Mapping.TypeDesc.UseReflection));
			base.Writer.WriteLine(";");
		}
	}

	private void WriteDerivedSerializable(SerializableMapping head, SerializableMapping mapping, string source, bool isWrappedAny)
	{
		if (mapping == null)
		{
			return;
		}
		for (SerializableMapping serializableMapping = mapping.DerivedMappings; serializableMapping != null; serializableMapping = serializableMapping.NextDerivedMapping)
		{
			base.Writer.Write("else if (tser == null");
			base.Writer.Write(" || ");
			WriteQNameEqual("tser", serializableMapping.XsiType.Name, serializableMapping.XsiType.Namespace);
			base.Writer.WriteLine(") {");
			base.Writer.Indent++;
			if (serializableMapping.Type != null)
			{
				if (head.Type.IsAssignableFrom(serializableMapping.Type))
				{
					WriteSourceBeginTyped(source, head.TypeDesc);
					base.Writer.Write("ReadSerializable(( ");
					base.Writer.Write(typeof(IXmlSerializable).FullName);
					base.Writer.Write(")");
					base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(serializableMapping.TypeDesc.CSharpName, serializableMapping.TypeDesc.UseReflection, serializableMapping.TypeDesc.CannotNew, cast: false));
					if (isWrappedAny)
					{
						base.Writer.WriteLine(", true");
					}
					base.Writer.Write(")");
					WriteSourceEnd(source);
					base.Writer.WriteLine(";");
				}
				else
				{
					base.Writer.Write("throw CreateBadDerivationException(");
					WriteQuotedCSharpString(serializableMapping.XsiType.Name);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(serializableMapping.XsiType.Namespace);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(head.XsiType.Name);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(head.XsiType.Namespace);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(serializableMapping.Type.FullName);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(head.Type.FullName);
					base.Writer.WriteLine(");");
				}
			}
			else
			{
				base.Writer.WriteLine("// missing real mapping for " + serializableMapping.XsiType);
				base.Writer.Write("throw CreateMissingIXmlSerializableType(");
				WriteQuotedCSharpString(serializableMapping.XsiType.Name);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(serializableMapping.XsiType.Namespace);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(head.Type.FullName);
				base.Writer.WriteLine(");");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			WriteDerivedSerializable(head, serializableMapping, source, isWrappedAny);
		}
	}

	private void WriteWhileNotLoopStart()
	{
		base.Writer.WriteLine("Reader.MoveToContent();");
		base.Writer.Write("while (Reader.NodeType != ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.Write(".EndElement && Reader.NodeType != ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".None) {");
	}

	private void WriteWhileLoopEnd()
	{
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteParamsRead(int length)
	{
		base.Writer.Write("bool[] paramsRead = new bool[");
		base.Writer.Write(length.ToString(CultureInfo.InvariantCulture));
		base.Writer.WriteLine("];");
	}

	private void WriteReadNonRoots()
	{
		base.Writer.WriteLine("Reader.MoveToContent();");
		base.Writer.Write("while (Reader.NodeType == ");
		base.Writer.Write(typeof(XmlNodeType).FullName);
		base.Writer.WriteLine(".Element) {");
		base.Writer.Indent++;
		base.Writer.Write("string root = Reader.GetAttribute(\"root\", \"");
		base.Writer.Write("http://schemas.xmlsoap.org/soap/encoding/");
		base.Writer.WriteLine("\");");
		base.Writer.Write("if (root == null || ");
		base.Writer.Write(typeof(XmlConvert).FullName);
		base.Writer.WriteLine(".ToBoolean(root)) break;");
		base.Writer.WriteLine("ReadReferencedElement();");
		base.Writer.WriteLine("Reader.MoveToContent();");
		WriteWhileLoopEnd();
	}

	private void WriteBooleanValue(bool value)
	{
		base.Writer.Write(value ? "true" : "false");
	}

	private void WriteInitCheckTypeHrefList(string source)
	{
		base.Writer.Write(typeof(ArrayList).FullName);
		base.Writer.Write(" ");
		base.Writer.Write(source);
		base.Writer.Write(" = new ");
		base.Writer.Write(typeof(ArrayList).FullName);
		base.Writer.WriteLine("();");
		base.Writer.Write(typeof(ArrayList).FullName);
		base.Writer.Write(" ");
		base.Writer.Write(source);
		base.Writer.Write("IsObject = new ");
		base.Writer.Write(typeof(ArrayList).FullName);
		base.Writer.WriteLine("();");
	}

	private void WriteHandleHrefList(Member[] members, string listSource)
	{
		base.Writer.WriteLine("int isObjectIndex = 0;");
		base.Writer.Write("foreach (object obj in ");
		base.Writer.Write(listSource);
		base.Writer.WriteLine(") {");
		base.Writer.Indent++;
		base.Writer.WriteLine("bool isReferenced = true;");
		base.Writer.Write("bool isObject = (bool)");
		base.Writer.Write(listSource);
		base.Writer.WriteLine("IsObject[isObjectIndex++];");
		base.Writer.WriteLine("object refObj = isObject ? obj : GetTarget((string)obj);");
		base.Writer.WriteLine("if (refObj == null) continue;");
		base.Writer.Write(typeof(Type).FullName);
		base.Writer.WriteLine(" refObjType = refObj.GetType();");
		base.Writer.WriteLine("string refObjId = null;");
		WriteMemberElementsIf(members, null, "isReferenced = false;", "refObj");
		base.Writer.WriteLine("if (isObject && isReferenced) Referenced(refObj); // need to mark this obj as ref'd since we didn't do GetTarget");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteIfNotSoapRoot(string source)
	{
		base.Writer.Write("if (Reader.GetAttribute(\"root\", \"");
		base.Writer.Write("http://schemas.xmlsoap.org/soap/encoding/");
		base.Writer.WriteLine("\") == \"0\") {");
		base.Writer.Indent++;
		base.Writer.WriteLine(source);
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteCreateMapping(TypeMapping mapping, string local)
	{
		string cSharpName = mapping.TypeDesc.CSharpName;
		bool useReflection = mapping.TypeDesc.UseReflection;
		bool cannotNew = mapping.TypeDesc.CannotNew;
		base.Writer.Write(useReflection ? "object" : cSharpName);
		base.Writer.Write(" ");
		base.Writer.Write(local);
		base.Writer.WriteLine(";");
		if (cannotNew)
		{
			base.Writer.WriteLine("try {");
			base.Writer.Indent++;
		}
		base.Writer.Write(local);
		base.Writer.Write(" = ");
		base.Writer.Write(base.RaCodeGen.GetStringForCreateInstance(cSharpName, useReflection, mapping.TypeDesc.CannotNew, cast: true));
		base.Writer.WriteLine(";");
		if (cannotNew)
		{
			WriteCatchException(typeof(MissingMethodException));
			base.Writer.Indent++;
			base.Writer.Write("throw CreateInaccessibleConstructorException(");
			WriteQuotedCSharpString(cSharpName);
			base.Writer.WriteLine(");");
			WriteCatchException(typeof(SecurityException));
			base.Writer.Indent++;
			base.Writer.Write("throw CreateCtorHasSecurityException(");
			WriteQuotedCSharpString(cSharpName);
			base.Writer.WriteLine(");");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void WriteCatchException(Type exceptionType)
	{
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Write("catch (");
		base.Writer.Write(exceptionType.FullName);
		base.Writer.WriteLine(") {");
	}

	private void WriteCatchCastException(TypeDesc typeDesc, string source, string id)
	{
		WriteCatchException(typeof(InvalidCastException));
		base.Writer.Indent++;
		base.Writer.Write("throw CreateInvalidCastException(");
		base.Writer.Write(base.RaCodeGen.GetStringForTypeof(typeDesc.CSharpName, typeDesc.UseReflection));
		base.Writer.Write(", ");
		base.Writer.Write(source);
		if (id == null)
		{
			base.Writer.WriteLine(", null);");
		}
		else
		{
			base.Writer.Write(", (string)");
			base.Writer.Write(id);
			base.Writer.WriteLine(");");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
	{
		base.RaCodeGen.WriteArrayLocalDecl(typeName, variableName, initValue, arrayTypeDesc);
	}

	private void WriteCreateInstance(string escapedName, string source, bool useReflection, bool ctorInaccessible)
	{
		base.RaCodeGen.WriteCreateInstance(escapedName, source, useReflection, ctorInaccessible);
	}

	private void WriteLocalDecl(string typeFullName, string variableName, string initValue, bool useReflection)
	{
		base.RaCodeGen.WriteLocalDecl(typeFullName, variableName, initValue, useReflection);
	}
}
