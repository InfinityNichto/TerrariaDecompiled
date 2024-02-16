using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization;

internal sealed class XmlSerializationWriterCodeGen : XmlSerializationCodeGen
{
	[RequiresUnreferencedCode("creates XmlSerializationCodeGen")]
	internal XmlSerializationWriterCodeGen(IndentedWriter writer, TypeScope[] scopes, string access, string className)
		: base(writer, scopes, access, className)
	{
	}

	[RequiresUnreferencedCode("calls WriteStructMethod")]
	internal void GenerateBegin()
	{
		base.Writer.Write(base.Access);
		base.Writer.Write(" class ");
		base.Writer.Write(base.ClassName);
		base.Writer.Write(" : ");
		base.Writer.Write(typeof(XmlSerializationWriter).FullName);
		base.Writer.WriteLine(" {");
		base.Writer.Indent++;
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (TypeMapping typeMapping3 in typeScope.TypeMappings)
			{
				if (typeMapping3 is StructMapping || typeMapping3 is EnumMapping)
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
		}
	}

	[RequiresUnreferencedCode("calls GenerateReferencedMethods")]
	internal void GenerateEnd()
	{
		GenerateReferencedMethods();
		GenerateInitCallbacksMethod();
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	[RequiresUnreferencedCode("calls GenerateMembersElement")]
	internal string GenerateElement(XmlMapping xmlMapping)
	{
		if (!xmlMapping.IsWriteable)
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

	private void GenerateInitCallbacksMethod()
	{
		base.Writer.WriteLine();
		base.Writer.WriteLine("protected override void InitCallbacks() {");
		base.Writer.Indent++;
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (TypeMapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping.IsSoap && (typeMapping is StructMapping || typeMapping is EnumMapping) && !typeMapping.TypeDesc.IsRoot)
				{
					string s = (string)base.MethodNames[typeMapping];
					base.Writer.Write("AddWriteCallback(");
					base.Writer.Write(base.RaCodeGen.GetStringForTypeof(typeMapping.TypeDesc.CSharpName, typeMapping.TypeDesc.UseReflection));
					base.Writer.Write(", ");
					WriteQuotedCSharpString(typeMapping.TypeName);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(typeMapping.Namespace);
					base.Writer.Write(", new ");
					base.Writer.Write(typeof(XmlSerializationWriteCallback).FullName);
					base.Writer.Write("(this.");
					base.Writer.Write(s);
					base.Writer.WriteLine("));");
				}
			}
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	[RequiresUnreferencedCode("calls WriteCheckDefault")]
	private void WriteQualifiedNameElement(string name, string ns, object defaultValue, string source, bool nullable, bool IsSoap, TypeMapping mapping)
	{
		bool flag = defaultValue != null && defaultValue != DBNull.Value;
		if (flag)
		{
			WriteCheckDefault(mapping, source, defaultValue, nullable);
			base.Writer.WriteLine(" {");
			base.Writer.Indent++;
		}
		string text = (IsSoap ? "Encoded" : "Literal");
		base.Writer.Write(nullable ? ("WriteNullableQualifiedName" + text) : "WriteElementQualifiedName");
		base.Writer.Write("(");
		WriteQuotedCSharpString(name);
		if (ns != null)
		{
			base.Writer.Write(", ");
			WriteQuotedCSharpString(ns);
		}
		base.Writer.Write(", ");
		base.Writer.Write(source);
		if (IsSoap)
		{
			base.Writer.Write(", new System.Xml.XmlQualifiedName(");
			WriteQuotedCSharpString(mapping.TypeName);
			base.Writer.Write(", ");
			WriteQuotedCSharpString(mapping.Namespace);
			base.Writer.Write(")");
		}
		base.Writer.WriteLine(");");
		if (flag)
		{
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void WriteEnumValue(EnumMapping mapping, string source)
	{
		string s = ReferenceMapping(mapping);
		base.Writer.Write(s);
		base.Writer.Write("(");
		base.Writer.Write(source);
		base.Writer.Write(")");
	}

	private void WritePrimitiveValue(TypeDesc typeDesc, string source, bool isElement)
	{
		if (typeDesc == base.StringTypeDesc || typeDesc.FormatterName == "String")
		{
			base.Writer.Write(source);
		}
		else if (!typeDesc.HasCustomFormatter)
		{
			base.Writer.Write(typeof(XmlConvert).FullName);
			base.Writer.Write(".ToString((");
			base.Writer.Write(typeDesc.CSharpName);
			base.Writer.Write(")");
			base.Writer.Write(source);
			base.Writer.Write(")");
		}
		else
		{
			base.Writer.Write("From");
			base.Writer.Write(typeDesc.FormatterName);
			base.Writer.Write("(");
			base.Writer.Write(source);
			base.Writer.Write(")");
		}
	}

	[RequiresUnreferencedCode("calls WriteCheckDefault")]
	private void WritePrimitive(string method, string name, string ns, object defaultValue, string source, TypeMapping mapping, bool writeXsiType, bool isElement, bool isNullable)
	{
		TypeDesc typeDesc = mapping.TypeDesc;
		bool flag = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc.HasDefaultSupport;
		if (flag)
		{
			if (mapping is EnumMapping)
			{
				base.Writer.Write("if (");
				if (mapping.TypeDesc.UseReflection)
				{
					base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue(source, mapping.TypeDesc.UseReflection));
				}
				else
				{
					base.Writer.Write(source);
				}
				base.Writer.Write(" != ");
				if (((EnumMapping)mapping).IsFlags)
				{
					base.Writer.Write("(");
					string[] array = ((string)defaultValue).Split((char[]?)null);
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i] != null && array[i].Length != 0)
						{
							if (i > 0)
							{
								base.Writer.WriteLine(" | ");
							}
							base.Writer.Write(base.RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, array[i], mapping.TypeDesc.UseReflection));
						}
					}
					base.Writer.Write(")");
				}
				else
				{
					base.Writer.Write(base.RaCodeGen.GetStringForEnumCompare((EnumMapping)mapping, (string)defaultValue, mapping.TypeDesc.UseReflection));
				}
				base.Writer.Write(")");
			}
			else
			{
				WriteCheckDefault(mapping, source, defaultValue, isNullable);
			}
			base.Writer.WriteLine(" {");
			base.Writer.Indent++;
		}
		base.Writer.Write(method);
		base.Writer.Write("(");
		WriteQuotedCSharpString(name);
		if (ns != null)
		{
			base.Writer.Write(", ");
			WriteQuotedCSharpString(ns);
		}
		base.Writer.Write(", ");
		if (mapping is EnumMapping)
		{
			WriteEnumValue((EnumMapping)mapping, source);
		}
		else
		{
			WritePrimitiveValue(typeDesc, source, isElement);
		}
		if (writeXsiType)
		{
			base.Writer.Write(", new System.Xml.XmlQualifiedName(");
			WriteQuotedCSharpString(mapping.TypeName);
			base.Writer.Write(", ");
			WriteQuotedCSharpString(mapping.Namespace);
			base.Writer.Write(")");
		}
		base.Writer.WriteLine(");");
		if (flag)
		{
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void WriteTag(string methodName, string name, string ns)
	{
		base.Writer.Write(methodName);
		base.Writer.Write("(");
		WriteQuotedCSharpString(name);
		base.Writer.Write(", ");
		if (ns == null)
		{
			base.Writer.Write("null");
		}
		else
		{
			WriteQuotedCSharpString(ns);
		}
		base.Writer.WriteLine(");");
	}

	private void WriteTag(string methodName, string name, string ns, bool writePrefixed)
	{
		base.Writer.Write(methodName);
		base.Writer.Write("(");
		WriteQuotedCSharpString(name);
		base.Writer.Write(", ");
		if (ns == null)
		{
			base.Writer.Write("null");
		}
		else
		{
			WriteQuotedCSharpString(ns);
		}
		base.Writer.Write(", null, ");
		if (writePrefixed)
		{
			base.Writer.Write("true");
		}
		else
		{
			base.Writer.Write("false");
		}
		base.Writer.WriteLine(");");
	}

	private void WriteStartElement(string name, string ns, bool writePrefixed)
	{
		WriteTag("WriteStartElement", name, ns, writePrefixed);
	}

	private void WriteEndElement()
	{
		base.Writer.WriteLine("WriteEndElement();");
	}

	private void WriteEndElement(string source)
	{
		base.Writer.Write("WriteEndElement(");
		base.Writer.Write(source);
		base.Writer.WriteLine(");");
	}

	private void WriteEncodedNullTag(string name, string ns)
	{
		WriteTag("WriteNullTagEncoded", name, ns);
	}

	private void WriteLiteralNullTag(string name, string ns)
	{
		WriteTag("WriteNullTagLiteral", name, ns);
	}

	private void WriteEmptyTag(string name, string ns)
	{
		WriteTag("WriteEmptyTag", name, ns);
	}

	[RequiresUnreferencedCode("calls WriteMember")]
	private string GenerateMembersElement(XmlMembersMapping xmlMembersMapping)
	{
		ElementAccessor accessor = xmlMembersMapping.Accessor;
		MembersMapping membersMapping = (MembersMapping)accessor.Mapping;
		bool hasWrapperElement = membersMapping.HasWrapperElement;
		bool writeAccessors = membersMapping.WriteAccessors;
		bool flag = xmlMembersMapping.IsSoap && writeAccessors;
		string text = NextMethodName(accessor.Name);
		base.Writer.WriteLine();
		base.Writer.Write("public void ");
		base.Writer.Write(text);
		base.Writer.WriteLine("(object[] p) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("WriteStartDocument();");
		if (!membersMapping.IsSoap)
		{
			base.Writer.WriteLine("TopLevelElement();");
		}
		base.Writer.WriteLine("int pLength = p.Length;");
		if (hasWrapperElement)
		{
			WriteStartElement(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "", membersMapping.IsSoap);
			int num = FindXmlnsIndex(membersMapping.Members);
			if (num >= 0)
			{
				string source = $"(({typeof(XmlSerializerNamespaces).FullName})p[{num}])";
				base.Writer.Write("if (pLength > ");
				base.Writer.Write(num.ToString(CultureInfo.InvariantCulture));
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				WriteNamespaces(source);
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			for (int i = 0; i < membersMapping.Members.Length; i++)
			{
				MemberMapping memberMapping = membersMapping.Members[i];
				if (memberMapping.Attribute == null || memberMapping.Ignore)
				{
					continue;
				}
				string text2 = i.ToString(CultureInfo.InvariantCulture);
				string source2 = "p[" + text2 + "]";
				string text3 = null;
				int num2 = 0;
				if (memberMapping.CheckSpecified != 0)
				{
					string text4 = memberMapping.Name + "Specified";
					for (int j = 0; j < membersMapping.Members.Length; j++)
					{
						if (membersMapping.Members[j].Name == text4)
						{
							text3 = $"((bool) p[{j}])";
							num2 = j;
							break;
						}
					}
				}
				base.Writer.Write("if (pLength > ");
				base.Writer.Write(text2);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				if (text3 != null)
				{
					base.Writer.Write("if (pLength <= ");
					base.Writer.Write(num2.ToString(CultureInfo.InvariantCulture));
					base.Writer.Write(" || ");
					base.Writer.Write(text3);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				WriteMember(source2, memberMapping.Attribute, memberMapping.TypeDesc, "p");
				if (text3 != null)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
		}
		for (int k = 0; k < membersMapping.Members.Length; k++)
		{
			MemberMapping memberMapping2 = membersMapping.Members[k];
			if (memberMapping2.Xmlns != null || memberMapping2.Ignore)
			{
				continue;
			}
			string text5 = null;
			int num3 = 0;
			if (memberMapping2.CheckSpecified != 0)
			{
				string text6 = memberMapping2.Name + "Specified";
				for (int l = 0; l < membersMapping.Members.Length; l++)
				{
					if (membersMapping.Members[l].Name == text6)
					{
						text5 = $"((bool) p[{l}])";
						num3 = l;
						break;
					}
				}
			}
			string text7 = k.ToString(CultureInfo.InvariantCulture);
			base.Writer.Write("if (pLength > ");
			base.Writer.Write(text7);
			base.Writer.WriteLine(") {");
			base.Writer.Indent++;
			if (text5 != null)
			{
				base.Writer.Write("if (pLength <= ");
				base.Writer.Write(num3.ToString(CultureInfo.InvariantCulture));
				base.Writer.Write(" || ");
				base.Writer.Write(text5);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
			}
			string source3 = "p[" + text7 + "]";
			string choiceSource = null;
			if (memberMapping2.ChoiceIdentifier != null)
			{
				for (int m = 0; m < membersMapping.Members.Length; m++)
				{
					if (membersMapping.Members[m].Name == memberMapping2.ChoiceIdentifier.MemberName)
					{
						choiceSource = ((!memberMapping2.ChoiceIdentifier.Mapping.TypeDesc.UseReflection) ? $"(({membersMapping.Members[m].TypeDesc.CSharpName})p[{m}])" : $"p[{m}]");
						break;
					}
				}
			}
			if (flag && memberMapping2.IsReturnValue && memberMapping2.Elements.Length != 0)
			{
				base.Writer.Write("WriteRpcResult(");
				WriteQuotedCSharpString(memberMapping2.Elements[0].Name);
				base.Writer.Write(", ");
				WriteQuotedCSharpString("");
				base.Writer.WriteLine(");");
			}
			WriteMember(source3, choiceSource, memberMapping2.ElementsSortedByDerivation, memberMapping2.Text, memberMapping2.ChoiceIdentifier, memberMapping2.TypeDesc, writeAccessors || hasWrapperElement);
			if (text5 != null)
			{
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		if (hasWrapperElement)
		{
			WriteEndElement();
		}
		if (accessor.IsSoap)
		{
			if (!hasWrapperElement && !writeAccessors)
			{
				base.Writer.Write("if (pLength > ");
				base.Writer.Write(membersMapping.Members.Length.ToString(CultureInfo.InvariantCulture));
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				WriteExtraMembers(membersMapping.Members.Length.ToString(CultureInfo.InvariantCulture), "pLength");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.WriteLine("WriteReferencedElements();");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		return text;
	}

	[RequiresUnreferencedCode("calls WriteMember")]
	private string GenerateTypeElement(XmlTypeMapping xmlTypeMapping)
	{
		ElementAccessor accessor = xmlTypeMapping.Accessor;
		TypeMapping mapping = accessor.Mapping;
		string text = NextMethodName(accessor.Name);
		base.Writer.WriteLine();
		base.Writer.Write("public void ");
		base.Writer.Write(text);
		base.Writer.WriteLine("(object o) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("WriteStartDocument();");
		base.Writer.WriteLine("if (o == null) {");
		base.Writer.Indent++;
		if (accessor.IsNullable)
		{
			if (mapping.IsSoap)
			{
				WriteEncodedNullTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
			}
			else
			{
				WriteLiteralNullTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
			}
		}
		else
		{
			WriteEmptyTag(accessor.Name, (accessor.Form == XmlSchemaForm.Qualified) ? accessor.Namespace : "");
		}
		base.Writer.WriteLine("return;");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		if (!mapping.IsSoap && !mapping.TypeDesc.IsValueType && !mapping.TypeDesc.Type.IsPrimitive)
		{
			base.Writer.WriteLine("TopLevelElement();");
		}
		WriteMember("o", null, new ElementAccessor[1] { accessor }, null, null, mapping.TypeDesc, !accessor.IsSoap);
		if (mapping.IsSoap)
		{
			base.Writer.WriteLine("WriteReferencedElements();");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		return text;
	}

	private string NextMethodName(string name)
	{
		return "Write" + (++base.NextMethodNumber).ToString(null, NumberFormatInfo.InvariantInfo) + "_" + CodeIdentifier.MakeValidInternal(name);
	}

	private void WriteEnumMethod(EnumMapping mapping)
	{
		string s = (string)base.MethodNames[mapping];
		base.Writer.WriteLine();
		string cSharpName = mapping.TypeDesc.CSharpName;
		if (mapping.IsSoap)
		{
			base.Writer.Write("void ");
			base.Writer.Write(s);
			base.Writer.WriteLine("(object e) {");
			WriteLocalDecl(cSharpName, "v", "e", mapping.TypeDesc.UseReflection);
		}
		else
		{
			base.Writer.Write("string ");
			base.Writer.Write(s);
			base.Writer.Write("(");
			base.Writer.Write(mapping.TypeDesc.UseReflection ? "object" : cSharpName);
			base.Writer.WriteLine(" v) {");
		}
		base.Writer.Indent++;
		base.Writer.WriteLine("string s = null;");
		ConstantMapping[] constants = mapping.Constants;
		if (constants.Length != 0)
		{
			Hashtable hashtable = new Hashtable();
			if (mapping.TypeDesc.UseReflection)
			{
				base.Writer.WriteLine("switch (" + base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection) + " ){");
			}
			else
			{
				base.Writer.WriteLine("switch (v) {");
			}
			base.Writer.Indent++;
			foreach (ConstantMapping constantMapping in constants)
			{
				if (hashtable[constantMapping.Value] == null)
				{
					WriteEnumCase(cSharpName, constantMapping, mapping.TypeDesc.UseReflection);
					base.Writer.Write("s = ");
					WriteQuotedCSharpString(constantMapping.XmlName);
					base.Writer.WriteLine("; break;");
					hashtable.Add(constantMapping.Value, constantMapping.Value);
				}
			}
			if (mapping.IsFlags)
			{
				base.Writer.Write("default: s = FromEnum(");
				base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
				base.Writer.Write(", new string[] {");
				base.Writer.Indent++;
				for (int j = 0; j < constants.Length; j++)
				{
					ConstantMapping constantMapping2 = constants[j];
					if (j > 0)
					{
						base.Writer.WriteLine(", ");
					}
					WriteQuotedCSharpString(constantMapping2.XmlName);
				}
				base.Writer.Write("}, new ");
				base.Writer.Write(typeof(long).FullName);
				base.Writer.Write("[] {");
				for (int k = 0; k < constants.Length; k++)
				{
					ConstantMapping constantMapping3 = constants[k];
					if (k > 0)
					{
						base.Writer.WriteLine(", ");
					}
					base.Writer.Write("(long)");
					if (mapping.TypeDesc.UseReflection)
					{
						base.Writer.Write(constantMapping3.Value.ToString(CultureInfo.InvariantCulture));
						continue;
					}
					base.Writer.Write(cSharpName);
					base.Writer.Write(".@");
					CodeIdentifier.CheckValidIdentifier(constantMapping3.Name);
					base.Writer.Write(constantMapping3.Name);
				}
				base.Writer.Indent--;
				base.Writer.Write("}, ");
				WriteQuotedCSharpString(mapping.TypeDesc.FullName);
				base.Writer.WriteLine("); break;");
			}
			else
			{
				base.Writer.Write("default: throw CreateInvalidEnumValueException(");
				base.Writer.Write(base.RaCodeGen.GetStringForEnumLongValue("v", mapping.TypeDesc.UseReflection));
				base.Writer.Write(".ToString(System.Globalization.CultureInfo.InvariantCulture), ");
				WriteQuotedCSharpString(mapping.TypeDesc.FullName);
				base.Writer.WriteLine(");");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		if (mapping.IsSoap)
		{
			base.Writer.Write("WriteXsiType(");
			WriteQuotedCSharpString(mapping.TypeName);
			base.Writer.Write(", ");
			WriteQuotedCSharpString(mapping.Namespace);
			base.Writer.WriteLine(");");
			base.Writer.WriteLine("Writer.WriteString(s);");
		}
		else
		{
			base.Writer.WriteLine("return s;");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteDerivedTypes(StructMapping mapping)
	{
		for (StructMapping structMapping = mapping.DerivedMappings; structMapping != null; structMapping = structMapping.NextDerivedMapping)
		{
			string cSharpName = structMapping.TypeDesc.CSharpName;
			base.Writer.Write("if (");
			WriteTypeCompare("t", cSharpName, structMapping.TypeDesc.UseReflection);
			base.Writer.WriteLine(") {");
			base.Writer.Indent++;
			string s = ReferenceMapping(structMapping);
			base.Writer.Write(s);
			base.Writer.Write("(n, ns,");
			if (!structMapping.TypeDesc.UseReflection)
			{
				base.Writer.Write("(" + cSharpName + ")");
			}
			base.Writer.Write("o");
			if (structMapping.TypeDesc.IsNullable)
			{
				base.Writer.Write(", isNullable");
			}
			base.Writer.Write(", true");
			base.Writer.WriteLine(");");
			base.Writer.WriteLine("return;");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			WriteDerivedTypes(structMapping);
		}
	}

	[RequiresUnreferencedCode("calls WriteMember")]
	private void WriteEnumAndArrayTypes()
	{
		TypeScope[] scopes = base.Scopes;
		foreach (TypeScope typeScope in scopes)
		{
			foreach (Mapping typeMapping in typeScope.TypeMappings)
			{
				if (typeMapping is EnumMapping && !typeMapping.IsSoap)
				{
					EnumMapping enumMapping = (EnumMapping)typeMapping;
					string cSharpName = enumMapping.TypeDesc.CSharpName;
					base.Writer.Write("if (");
					WriteTypeCompare("t", cSharpName, enumMapping.TypeDesc.UseReflection);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
					string s = ReferenceMapping(enumMapping);
					base.Writer.WriteLine("Writer.WriteStartElement(n, ns);");
					base.Writer.Write("WriteXsiType(");
					WriteQuotedCSharpString(enumMapping.TypeName);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(enumMapping.Namespace);
					base.Writer.WriteLine(");");
					base.Writer.Write("Writer.WriteString(");
					base.Writer.Write(s);
					base.Writer.Write("(");
					if (!enumMapping.TypeDesc.UseReflection)
					{
						base.Writer.Write("(" + cSharpName + ")");
					}
					base.Writer.WriteLine("o));");
					base.Writer.WriteLine("Writer.WriteEndElement();");
					base.Writer.WriteLine("return;");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				else if (typeMapping is ArrayMapping && !typeMapping.IsSoap && typeMapping is ArrayMapping arrayMapping && !typeMapping.IsSoap)
				{
					string cSharpName2 = arrayMapping.TypeDesc.CSharpName;
					base.Writer.Write("if (");
					if (arrayMapping.TypeDesc.IsArray)
					{
						WriteArrayTypeCompare("t", cSharpName2, arrayMapping.TypeDesc.ArrayElementTypeDesc.CSharpName, arrayMapping.TypeDesc.UseReflection);
					}
					else
					{
						WriteTypeCompare("t", cSharpName2, arrayMapping.TypeDesc.UseReflection);
					}
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
					base.Writer.WriteLine("Writer.WriteStartElement(n, ns);");
					base.Writer.Write("WriteXsiType(");
					WriteQuotedCSharpString(arrayMapping.TypeName);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(arrayMapping.Namespace);
					base.Writer.WriteLine(");");
					WriteMember("o", null, arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, writeAccessors: true);
					base.Writer.WriteLine("Writer.WriteEndElement();");
					base.Writer.WriteLine("return;");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
		}
	}

	[RequiresUnreferencedCode("calls WriteMember")]
	private void WriteStructMethod(StructMapping mapping)
	{
		if (mapping.IsSoap && mapping.TypeDesc.IsRoot)
		{
			return;
		}
		string s = (string)base.MethodNames[mapping];
		base.Writer.WriteLine();
		base.Writer.Write("void ");
		base.Writer.Write(s);
		string cSharpName = mapping.TypeDesc.CSharpName;
		if (mapping.IsSoap)
		{
			base.Writer.WriteLine("(object s) {");
			base.Writer.Indent++;
			WriteLocalDecl(cSharpName, "o", "s", mapping.TypeDesc.UseReflection);
		}
		else
		{
			base.Writer.Write("(string n, string ns, ");
			base.Writer.Write(mapping.TypeDesc.UseReflection ? "object" : cSharpName);
			base.Writer.Write(" o");
			if (mapping.TypeDesc.IsNullable)
			{
				base.Writer.Write(", bool isNullable");
			}
			base.Writer.WriteLine(", bool needType) {");
			base.Writer.Indent++;
			if (mapping.TypeDesc.IsNullable)
			{
				base.Writer.WriteLine("if ((object)o == null) {");
				base.Writer.Indent++;
				base.Writer.WriteLine("if (isNullable) WriteNullTagLiteral(n, ns);");
				base.Writer.WriteLine("return;");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.WriteLine("if (!needType) {");
			base.Writer.Indent++;
			base.Writer.Write(typeof(Type).FullName);
			base.Writer.WriteLine(" t = o.GetType();");
			base.Writer.Write("if (");
			WriteTypeCompare("t", cSharpName, mapping.TypeDesc.UseReflection);
			base.Writer.WriteLine(") {");
			base.Writer.WriteLine("}");
			base.Writer.WriteLine("else {");
			base.Writer.Indent++;
			WriteDerivedTypes(mapping);
			if (mapping.TypeDesc.IsRoot)
			{
				WriteEnumAndArrayTypes();
			}
			if (mapping.TypeDesc.IsRoot)
			{
				base.Writer.WriteLine("WriteTypedPrimitive(n, ns, o, true);");
				base.Writer.WriteLine("return;");
			}
			else
			{
				base.Writer.WriteLine("throw CreateUnknownTypeException(o);");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		if (!mapping.TypeDesc.IsAbstract)
		{
			if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
			{
				base.Writer.WriteLine("EscapeName = false;");
			}
			string text = null;
			MemberMapping[] allMembers = TypeScope.GetAllMembers(mapping);
			int num = FindXmlnsIndex(allMembers);
			if (num >= 0)
			{
				MemberMapping memberMapping = allMembers[num];
				CodeIdentifier.CheckValidIdentifier(memberMapping.Name);
				text = base.RaCodeGen.GetStringForMember("o", memberMapping.Name, mapping.TypeDesc);
				if (mapping.TypeDesc.UseReflection)
				{
					text = "((" + memberMapping.TypeDesc.CSharpName + ")" + text + ")";
				}
			}
			if (!mapping.IsSoap)
			{
				base.Writer.Write("WriteStartElement(n, ns, o, false, ");
				if (text == null)
				{
					base.Writer.Write("null");
				}
				else
				{
					base.Writer.Write(text);
				}
				base.Writer.WriteLine(");");
				if (!mapping.TypeDesc.IsRoot)
				{
					base.Writer.Write("if (needType) WriteXsiType(");
					WriteQuotedCSharpString(mapping.TypeName);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(mapping.Namespace);
					base.Writer.WriteLine(");");
				}
			}
			else if (text != null)
			{
				WriteNamespaces(text);
			}
			foreach (MemberMapping memberMapping2 in allMembers)
			{
				if (memberMapping2.Attribute == null)
				{
					continue;
				}
				CodeIdentifier.CheckValidIdentifier(memberMapping2.Name);
				if (memberMapping2.CheckShouldPersist)
				{
					base.Writer.Write("if (");
					string text2 = base.RaCodeGen.GetStringForMethodInvoke("o", cSharpName, "ShouldSerialize" + memberMapping2.Name, mapping.TypeDesc.UseReflection);
					if (mapping.TypeDesc.UseReflection)
					{
						text2 = "((" + typeof(bool).FullName + ")" + text2 + ")";
					}
					base.Writer.Write(text2);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				if (memberMapping2.CheckSpecified != 0)
				{
					base.Writer.Write("if (");
					string text3 = base.RaCodeGen.GetStringForMember("o", memberMapping2.Name + "Specified", mapping.TypeDesc);
					if (mapping.TypeDesc.UseReflection)
					{
						text3 = "((" + typeof(bool).FullName + ")" + text3 + ")";
					}
					base.Writer.Write(text3);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				WriteMember(base.RaCodeGen.GetStringForMember("o", memberMapping2.Name, mapping.TypeDesc), memberMapping2.Attribute, memberMapping2.TypeDesc, "o");
				if (memberMapping2.CheckSpecified != 0)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				if (memberMapping2.CheckShouldPersist)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
			foreach (MemberMapping memberMapping3 in allMembers)
			{
				if (memberMapping3.Xmlns != null)
				{
					continue;
				}
				CodeIdentifier.CheckValidIdentifier(memberMapping3.Name);
				bool flag = memberMapping3.CheckShouldPersist && (memberMapping3.Elements.Length != 0 || memberMapping3.Text != null);
				if (flag)
				{
					base.Writer.Write("if (");
					string text4 = base.RaCodeGen.GetStringForMethodInvoke("o", cSharpName, "ShouldSerialize" + memberMapping3.Name, mapping.TypeDesc.UseReflection);
					if (mapping.TypeDesc.UseReflection)
					{
						text4 = "((" + typeof(bool).FullName + ")" + text4 + ")";
					}
					base.Writer.Write(text4);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				if (memberMapping3.CheckSpecified != 0)
				{
					base.Writer.Write("if (");
					string text5 = base.RaCodeGen.GetStringForMember("o", memberMapping3.Name + "Specified", mapping.TypeDesc);
					if (mapping.TypeDesc.UseReflection)
					{
						text5 = "((" + typeof(bool).FullName + ")" + text5 + ")";
					}
					base.Writer.Write(text5);
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				string choiceSource = null;
				if (memberMapping3.ChoiceIdentifier != null)
				{
					CodeIdentifier.CheckValidIdentifier(memberMapping3.ChoiceIdentifier.MemberName);
					choiceSource = base.RaCodeGen.GetStringForMember("o", memberMapping3.ChoiceIdentifier.MemberName, mapping.TypeDesc);
				}
				WriteMember(base.RaCodeGen.GetStringForMember("o", memberMapping3.Name, mapping.TypeDesc), choiceSource, memberMapping3.ElementsSortedByDerivation, memberMapping3.Text, memberMapping3.ChoiceIdentifier, memberMapping3.TypeDesc, writeAccessors: true);
				if (memberMapping3.CheckSpecified != 0)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				if (flag)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
			if (!mapping.IsSoap)
			{
				WriteEndElement("o");
			}
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private bool CanOptimizeWriteListSequence(TypeDesc listElementTypeDesc)
	{
		if (listElementTypeDesc != null)
		{
			return listElementTypeDesc != base.QnameTypeDesc;
		}
		return false;
	}

	[RequiresUnreferencedCode("calls WriteAttribute")]
	private void WriteMember(string source, AttributeAccessor attribute, TypeDesc memberTypeDesc, string parent)
	{
		if (memberTypeDesc.IsAbstract)
		{
			return;
		}
		if (memberTypeDesc.IsArrayLike)
		{
			base.Writer.WriteLine("{");
			base.Writer.Indent++;
			string cSharpName = memberTypeDesc.CSharpName;
			WriteArrayLocalDecl(cSharpName, "a", source, memberTypeDesc);
			if (memberTypeDesc.IsNullable)
			{
				base.Writer.WriteLine("if (a != null) {");
				base.Writer.Indent++;
			}
			if (attribute.IsList)
			{
				if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
				{
					base.Writer.Write("Writer.WriteStartAttribute(null, ");
					WriteQuotedCSharpString(attribute.Name);
					base.Writer.Write(", ");
					string text = ((attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty);
					if (text != null)
					{
						WriteQuotedCSharpString(text);
					}
					else
					{
						base.Writer.Write("null");
					}
					base.Writer.WriteLine(");");
				}
				else
				{
					base.Writer.Write(typeof(StringBuilder).FullName);
					base.Writer.Write(" sb = new ");
					base.Writer.Write(typeof(StringBuilder).FullName);
					base.Writer.WriteLine("();");
				}
			}
			TypeDesc arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
			if (memberTypeDesc.IsEnumerable)
			{
				base.Writer.Write(" e = ");
				base.Writer.Write(typeof(IEnumerator).FullName);
				if (memberTypeDesc.IsPrivateImplementation)
				{
					base.Writer.Write("((");
					base.Writer.Write(typeof(IEnumerable).FullName);
					base.Writer.WriteLine(").GetEnumerator();");
				}
				else if (memberTypeDesc.IsGenericInterface)
				{
					if (memberTypeDesc.UseReflection)
					{
						base.Writer.Write("(");
						base.Writer.Write(typeof(IEnumerator).FullName);
						base.Writer.Write(")");
						base.Writer.Write(base.RaCodeGen.GetReflectionVariable(memberTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
						base.Writer.WriteLine(".Invoke(a, new object[0]);");
					}
					else
					{
						base.Writer.Write("((System.Collections.Generic.IEnumerable<");
						base.Writer.Write(arrayElementTypeDesc.CSharpName);
						base.Writer.WriteLine(">)a).GetEnumerator();");
					}
				}
				else
				{
					if (memberTypeDesc.UseReflection)
					{
						base.Writer.Write("(");
						base.Writer.Write(typeof(IEnumerator).FullName);
						base.Writer.Write(")");
					}
					base.Writer.Write(base.RaCodeGen.GetStringForMethodInvoke("a", memberTypeDesc.CSharpName, "GetEnumerator", memberTypeDesc.UseReflection));
					base.Writer.WriteLine(";");
				}
				base.Writer.WriteLine("if (e != null)");
				base.Writer.WriteLine("while (e.MoveNext()) {");
				base.Writer.Indent++;
				string cSharpName2 = arrayElementTypeDesc.CSharpName;
				WriteLocalDecl(cSharpName2, "ai", "e.Current", arrayElementTypeDesc.UseReflection);
			}
			else
			{
				base.Writer.Write("for (int i = 0; i < ");
				if (memberTypeDesc.IsArray)
				{
					base.Writer.WriteLine("a.Length; i++) {");
				}
				else
				{
					base.Writer.Write("((");
					base.Writer.Write(typeof(ICollection).FullName);
					base.Writer.WriteLine(")a).Count; i++) {");
				}
				base.Writer.Indent++;
				string cSharpName3 = arrayElementTypeDesc.CSharpName;
				WriteLocalDecl(cSharpName3, "ai", base.RaCodeGen.GetStringForArrayMember("a", "i", memberTypeDesc), arrayElementTypeDesc.UseReflection);
			}
			if (attribute.IsList)
			{
				if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
				{
					base.Writer.WriteLine("if (i != 0) Writer.WriteString(\" \");");
					base.Writer.Write("WriteValue(");
				}
				else
				{
					base.Writer.WriteLine("if (i != 0) sb.Append(\" \");");
					base.Writer.Write("sb.Append(");
				}
				if (attribute.Mapping is EnumMapping)
				{
					WriteEnumValue((EnumMapping)attribute.Mapping, "ai");
				}
				else
				{
					WritePrimitiveValue(arrayElementTypeDesc, "ai", isElement: true);
				}
				base.Writer.WriteLine(");");
			}
			else
			{
				WriteAttribute("ai", attribute, parent);
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			if (attribute.IsList)
			{
				if (CanOptimizeWriteListSequence(memberTypeDesc.ArrayElementTypeDesc))
				{
					base.Writer.WriteLine("Writer.WriteEndAttribute();");
				}
				else
				{
					base.Writer.WriteLine("if (sb.Length != 0) {");
					base.Writer.Indent++;
					base.Writer.Write("WriteAttribute(");
					WriteQuotedCSharpString(attribute.Name);
					base.Writer.Write(", ");
					string text2 = ((attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : string.Empty);
					if (text2 != null)
					{
						WriteQuotedCSharpString(text2);
						base.Writer.Write(", ");
					}
					base.Writer.WriteLine("sb.ToString());");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
			}
			if (memberTypeDesc.IsNullable)
			{
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		else
		{
			WriteAttribute(source, attribute, parent);
		}
	}

	[RequiresUnreferencedCode("calls WritePrimitive")]
	private void WriteAttribute(string source, AttributeAccessor attribute, string parent)
	{
		if (attribute.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)attribute.Mapping;
			if (specialMapping.TypeDesc.Kind == TypeKind.Attribute || specialMapping.TypeDesc.CanBeAttributeValue)
			{
				base.Writer.Write("WriteXmlAttribute(");
				base.Writer.Write(source);
				base.Writer.Write(", ");
				base.Writer.Write(parent);
				base.Writer.WriteLine(");");
				return;
			}
			throw new InvalidOperationException(System.SR.XmlInternalError);
		}
		TypeDesc typeDesc = attribute.Mapping.TypeDesc;
		if (!typeDesc.UseReflection)
		{
			source = "((" + typeDesc.CSharpName + ")" + source + ")";
		}
		WritePrimitive("WriteAttribute", attribute.Name, (attribute.Form == XmlSchemaForm.Qualified) ? attribute.Namespace : "", attribute.Default, source, attribute.Mapping, writeXsiType: false, isElement: false, isNullable: false);
	}

	[RequiresUnreferencedCode("calls WriteElements")]
	private void WriteMember(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc memberTypeDesc, bool writeAccessors)
	{
		if (memberTypeDesc.IsArrayLike && (elements.Length != 1 || !(elements[0].Mapping is ArrayMapping)))
		{
			WriteArray(source, choiceSource, elements, text, choice, memberTypeDesc);
		}
		else
		{
			WriteElements(source, choiceSource, elements, text, choice, "a", writeAccessors, memberTypeDesc.IsNullable);
		}
	}

	[RequiresUnreferencedCode("calls WriteArrayItems")]
	private void WriteArray(string source, string choiceSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc)
	{
		if (elements.Length != 0 || text != null)
		{
			base.Writer.WriteLine("{");
			base.Writer.Indent++;
			string cSharpName = arrayTypeDesc.CSharpName;
			WriteArrayLocalDecl(cSharpName, "a", source, arrayTypeDesc);
			if (arrayTypeDesc.IsNullable)
			{
				base.Writer.WriteLine("if (a != null) {");
				base.Writer.Indent++;
			}
			if (choice != null)
			{
				string cSharpName2 = choice.Mapping.TypeDesc.CSharpName;
				WriteArrayLocalDecl(cSharpName2 + "[]", "c", choiceSource, choice.Mapping.TypeDesc);
				base.Writer.WriteLine("if (c == null || c.Length < a.Length) {");
				base.Writer.Indent++;
				base.Writer.Write("throw CreateInvalidChoiceIdentifierValueException(");
				WriteQuotedCSharpString(choice.Mapping.TypeDesc.FullName);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(choice.MemberName);
				base.Writer.Write(");");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			WriteArrayItems(elements, text, choice, arrayTypeDesc, "a", "c");
			if (arrayTypeDesc.IsNullable)
			{
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	[RequiresUnreferencedCode("calls WriteElements")]
	private void WriteArrayItems(ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, TypeDesc arrayTypeDesc, string arrayName, string choiceName)
	{
		TypeDesc arrayElementTypeDesc = arrayTypeDesc.ArrayElementTypeDesc;
		if (arrayTypeDesc.IsEnumerable)
		{
			base.Writer.Write(typeof(IEnumerator).FullName);
			base.Writer.Write(" e = ");
			if (arrayTypeDesc.IsPrivateImplementation)
			{
				base.Writer.Write("((");
				base.Writer.Write(typeof(IEnumerable).FullName);
				base.Writer.Write(")");
				base.Writer.Write(arrayName);
				base.Writer.WriteLine(").GetEnumerator();");
			}
			else if (arrayTypeDesc.IsGenericInterface)
			{
				if (arrayTypeDesc.UseReflection)
				{
					base.Writer.Write("(");
					base.Writer.Write(typeof(IEnumerator).FullName);
					base.Writer.Write(")");
					base.Writer.Write(base.RaCodeGen.GetReflectionVariable(arrayTypeDesc.CSharpName, "System.Collections.Generic.IEnumerable*"));
					base.Writer.Write(".Invoke(");
					base.Writer.Write(arrayName);
					base.Writer.WriteLine(", new object[0]);");
				}
				else
				{
					base.Writer.Write("((System.Collections.Generic.IEnumerable<");
					base.Writer.Write(arrayElementTypeDesc.CSharpName);
					base.Writer.Write(">)");
					base.Writer.Write(arrayName);
					base.Writer.WriteLine(").GetEnumerator();");
				}
			}
			else
			{
				if (arrayTypeDesc.UseReflection)
				{
					base.Writer.Write("(");
					base.Writer.Write(typeof(IEnumerator).FullName);
					base.Writer.Write(")");
				}
				base.Writer.Write(base.RaCodeGen.GetStringForMethodInvoke(arrayName, arrayTypeDesc.CSharpName, "GetEnumerator", arrayTypeDesc.UseReflection));
				base.Writer.WriteLine(";");
			}
			base.Writer.WriteLine("if (e != null)");
			base.Writer.WriteLine("while (e.MoveNext()) {");
			base.Writer.Indent++;
			string cSharpName = arrayElementTypeDesc.CSharpName;
			WriteLocalDecl(cSharpName, arrayName + "i", "e.Current", arrayElementTypeDesc.UseReflection);
			WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", writeAccessors: true, isNullable: true);
		}
		else
		{
			base.Writer.Write("for (int i");
			base.Writer.Write(arrayName);
			base.Writer.Write(" = 0; i");
			base.Writer.Write(arrayName);
			base.Writer.Write(" < ");
			if (arrayTypeDesc.IsArray)
			{
				base.Writer.Write(arrayName);
				base.Writer.Write(".Length");
			}
			else
			{
				base.Writer.Write("((");
				base.Writer.Write(typeof(ICollection).FullName);
				base.Writer.Write(")");
				base.Writer.Write(arrayName);
				base.Writer.Write(").Count");
			}
			base.Writer.Write("; i");
			base.Writer.Write(arrayName);
			base.Writer.WriteLine("++) {");
			base.Writer.Indent++;
			int num = elements.Length + ((text != null) ? 1 : 0);
			if (num > 1)
			{
				string cSharpName2 = arrayElementTypeDesc.CSharpName;
				WriteLocalDecl(cSharpName2, arrayName + "i", base.RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), arrayElementTypeDesc.UseReflection);
				if (choice != null)
				{
					string cSharpName3 = choice.Mapping.TypeDesc.CSharpName;
					WriteLocalDecl(cSharpName3, choiceName + "i", base.RaCodeGen.GetStringForArrayMember(choiceName, "i" + arrayName, choice.Mapping.TypeDesc), choice.Mapping.TypeDesc.UseReflection);
				}
				WriteElements(arrayName + "i", choiceName + "i", elements, text, choice, arrayName + "a", writeAccessors: true, arrayElementTypeDesc.IsNullable);
			}
			else
			{
				WriteElements(base.RaCodeGen.GetStringForArrayMember(arrayName, "i" + arrayName, arrayTypeDesc), elements, text, choice, arrayName + "a", writeAccessors: true, arrayElementTypeDesc.IsNullable);
			}
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	[RequiresUnreferencedCode("calls WriteElements")]
	private void WriteElements(string source, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
	{
		WriteElements(source, null, elements, text, choice, arrayName, writeAccessors, isNullable);
	}

	[RequiresUnreferencedCode("calls WriteElement")]
	private void WriteElements(string source, string enumSource, ElementAccessor[] elements, TextAccessor text, ChoiceIdentifierAccessor choice, string arrayName, bool writeAccessors, bool isNullable)
	{
		if (elements.Length == 0 && text == null)
		{
			return;
		}
		if (elements.Length == 1 && text == null)
		{
			TypeDesc typeDesc = (elements[0].IsUnbounded ? elements[0].Mapping.TypeDesc.CreateArrayTypeDesc() : elements[0].Mapping.TypeDesc);
			if (!elements[0].Any && !elements[0].Mapping.TypeDesc.UseReflection && !elements[0].Mapping.TypeDesc.IsOptionalValue)
			{
				source = "((" + typeDesc.CSharpName + ")" + source + ")";
			}
			WriteElement(source, elements[0], arrayName, writeAccessors);
			return;
		}
		if (isNullable && choice == null)
		{
			base.Writer.Write("if ((object)(");
			base.Writer.Write(source);
			base.Writer.Write(") != null)");
		}
		base.Writer.WriteLine("{");
		base.Writer.Indent++;
		int num = 0;
		ArrayList arrayList = new ArrayList();
		ElementAccessor elementAccessor = null;
		bool flag = false;
		string text2 = choice?.Mapping.TypeDesc.FullName;
		foreach (ElementAccessor elementAccessor2 in elements)
		{
			if (elementAccessor2.Any)
			{
				num++;
				if (elementAccessor2.Name != null && elementAccessor2.Name.Length > 0)
				{
					arrayList.Add(elementAccessor2);
				}
				else if (elementAccessor == null)
				{
					elementAccessor = elementAccessor2;
				}
			}
			else if (choice != null)
			{
				bool useReflection = elementAccessor2.Mapping.TypeDesc.UseReflection;
				string cSharpName = elementAccessor2.Mapping.TypeDesc.CSharpName;
				bool useReflection2 = choice.Mapping.TypeDesc.UseReflection;
				string text3 = (useReflection2 ? "" : (text2 + ".@")) + FindChoiceEnumValue(elementAccessor2, (EnumMapping)choice.Mapping, useReflection2);
				if (flag)
				{
					base.Writer.Write("else ");
				}
				else
				{
					flag = true;
				}
				base.Writer.Write("if (");
				base.Writer.Write(useReflection2 ? base.RaCodeGen.GetStringForEnumLongValue(enumSource, useReflection2) : enumSource);
				base.Writer.Write(" == ");
				base.Writer.Write(text3);
				if (isNullable && !elementAccessor2.IsNullable)
				{
					base.Writer.Write(" && ((object)(");
					base.Writer.Write(source);
					base.Writer.Write(") != null)");
				}
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				WriteChoiceTypeCheck(source, cSharpName, useReflection, choice, text3, elementAccessor2.Mapping.TypeDesc);
				string text4 = source;
				if (!useReflection)
				{
					text4 = "((" + cSharpName + ")" + source + ")";
				}
				WriteElement(elementAccessor2.Any ? source : text4, elementAccessor2, arrayName, writeAccessors);
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			else
			{
				bool useReflection3 = elementAccessor2.Mapping.TypeDesc.UseReflection;
				TypeDesc typeDesc2 = (elementAccessor2.IsUnbounded ? elementAccessor2.Mapping.TypeDesc.CreateArrayTypeDesc() : elementAccessor2.Mapping.TypeDesc);
				string cSharpName2 = typeDesc2.CSharpName;
				if (flag)
				{
					base.Writer.Write("else ");
				}
				else
				{
					flag = true;
				}
				base.Writer.Write("if (");
				WriteInstanceOf(source, cSharpName2, useReflection3);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				string text5 = source;
				if (!useReflection3)
				{
					text5 = "((" + cSharpName2 + ")" + source + ")";
				}
				WriteElement(elementAccessor2.Any ? source : text5, elementAccessor2, arrayName, writeAccessors);
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
		}
		if (num > 0)
		{
			if (elements.Length - num > 0)
			{
				base.Writer.Write("else ");
			}
			string fullName = typeof(XmlElement).FullName;
			base.Writer.Write("if (");
			base.Writer.Write(source);
			base.Writer.Write(" is ");
			base.Writer.Write(fullName);
			base.Writer.WriteLine(") {");
			base.Writer.Indent++;
			base.Writer.Write(fullName);
			base.Writer.Write(" elem = (");
			base.Writer.Write(fullName);
			base.Writer.Write(")");
			base.Writer.Write(source);
			base.Writer.WriteLine(";");
			int num2 = 0;
			foreach (ElementAccessor item in arrayList)
			{
				if (num2++ > 0)
				{
					base.Writer.Write("else ");
				}
				string text6 = null;
				if (choice != null)
				{
					bool useReflection4 = choice.Mapping.TypeDesc.UseReflection;
					text6 = (useReflection4 ? "" : (text2 + ".@")) + FindChoiceEnumValue(item, (EnumMapping)choice.Mapping, useReflection4);
					base.Writer.Write("if (");
					base.Writer.Write(useReflection4 ? base.RaCodeGen.GetStringForEnumLongValue(enumSource, useReflection4) : enumSource);
					base.Writer.Write(" == ");
					base.Writer.Write(text6);
					if (isNullable && !item.IsNullable)
					{
						base.Writer.Write(" && ((object)(");
						base.Writer.Write(source);
						base.Writer.Write(") != null)");
					}
					base.Writer.WriteLine(") {");
					base.Writer.Indent++;
				}
				base.Writer.Write("if (elem.Name == ");
				WriteQuotedCSharpString(item.Name);
				base.Writer.Write(" && elem.NamespaceURI == ");
				WriteQuotedCSharpString(item.Namespace);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				WriteElement("elem", item, arrayName, writeAccessors);
				if (choice != null)
				{
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
					base.Writer.WriteLine("else {");
					base.Writer.Indent++;
					base.Writer.WriteLine("// throw Value '{0}' of the choice identifier '{1}' does not match element '{2}' from namespace '{3}'.");
					base.Writer.Write("throw CreateChoiceIdentifierValueException(");
					WriteQuotedCSharpString(text6);
					base.Writer.Write(", ");
					WriteQuotedCSharpString(choice.MemberName);
					base.Writer.WriteLine(", elem.Name, elem.NamespaceURI);");
					base.Writer.Indent--;
					base.Writer.WriteLine("}");
				}
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			if (num2 > 0)
			{
				base.Writer.WriteLine("else {");
				base.Writer.Indent++;
			}
			if (elementAccessor != null)
			{
				WriteElement("elem", elementAccessor, arrayName, writeAccessors);
			}
			else
			{
				base.Writer.WriteLine("throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);");
			}
			if (num2 > 0)
			{
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		if (text != null)
		{
			bool useReflection5 = text.Mapping.TypeDesc.UseReflection;
			string cSharpName3 = text.Mapping.TypeDesc.CSharpName;
			if (elements.Length != 0)
			{
				base.Writer.Write("else ");
				base.Writer.Write("if (");
				WriteInstanceOf(source, cSharpName3, useReflection5);
				base.Writer.WriteLine(") {");
				base.Writer.Indent++;
				string source2 = source;
				if (!useReflection5)
				{
					source2 = "((" + cSharpName3 + ")" + source + ")";
				}
				WriteText(source2, text);
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
			else
			{
				string source3 = source;
				if (!useReflection5)
				{
					source3 = "((" + cSharpName3 + ")" + source + ")";
				}
				WriteText(source3, text);
			}
		}
		if (elements.Length != 0)
		{
			base.Writer.Write("else ");
			if (isNullable)
			{
				base.Writer.Write(" if ((object)(");
				base.Writer.Write(source);
				base.Writer.Write(") != null)");
			}
			base.Writer.WriteLine("{");
			base.Writer.Indent++;
			base.Writer.Write("throw CreateUnknownTypeException(");
			base.Writer.Write(source);
			base.Writer.WriteLine(");");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteText(string source, TextAccessor text)
	{
		if (text.Mapping is PrimitiveMapping)
		{
			PrimitiveMapping primitiveMapping = (PrimitiveMapping)text.Mapping;
			base.Writer.Write("WriteValue(");
			if (text.Mapping is EnumMapping)
			{
				WriteEnumValue((EnumMapping)text.Mapping, source);
			}
			else
			{
				WritePrimitiveValue(primitiveMapping.TypeDesc, source, isElement: false);
			}
			base.Writer.WriteLine(");");
		}
		else if (text.Mapping is SpecialMapping)
		{
			SpecialMapping specialMapping = (SpecialMapping)text.Mapping;
			TypeKind kind = specialMapping.TypeDesc.Kind;
			if (kind != TypeKind.Node)
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			base.Writer.Write(source);
			base.Writer.WriteLine(".WriteTo(Writer);");
		}
	}

	[RequiresUnreferencedCode("calls WritePrimitive")]
	private void WriteElement(string source, ElementAccessor element, string arrayName, bool writeAccessor)
	{
		string text = (writeAccessor ? element.Name : element.Mapping.TypeName);
		string text2 = ((element.Any && element.Name.Length == 0) ? null : ((element.Form != XmlSchemaForm.Qualified) ? "" : (writeAccessor ? element.Namespace : element.Mapping.Namespace)));
		if (element.Mapping is NullableMapping)
		{
			base.Writer.Write("if (");
			base.Writer.Write(source);
			base.Writer.WriteLine(" != null) {");
			base.Writer.Indent++;
			string cSharpName = element.Mapping.TypeDesc.BaseTypeDesc.CSharpName;
			string text3 = source;
			if (!element.Mapping.TypeDesc.BaseTypeDesc.UseReflection)
			{
				text3 = "((" + cSharpName + ")" + source + ")";
			}
			ElementAccessor elementAccessor = element.Clone();
			elementAccessor.Mapping = ((NullableMapping)element.Mapping).BaseMapping;
			WriteElement(elementAccessor.Any ? source : text3, elementAccessor, arrayName, writeAccessor);
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			if (element.IsNullable)
			{
				base.Writer.WriteLine("else {");
				base.Writer.Indent++;
				WriteLiteralNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified) ? element.Namespace : "");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
			}
		}
		else if (element.Mapping is ArrayMapping)
		{
			ArrayMapping arrayMapping = (ArrayMapping)element.Mapping;
			if (arrayMapping.IsSoap)
			{
				base.Writer.Write("WritePotentiallyReferencingElement(");
				WriteQuotedCSharpString(text);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(text2);
				base.Writer.Write(", ");
				base.Writer.Write(source);
				if (!writeAccessor)
				{
					base.Writer.Write(", ");
					base.Writer.Write(base.RaCodeGen.GetStringForTypeof(arrayMapping.TypeDesc.CSharpName, arrayMapping.TypeDesc.UseReflection));
					base.Writer.Write(", true, ");
				}
				else
				{
					base.Writer.Write(", null, false, ");
				}
				WriteValue(element.IsNullable);
				base.Writer.WriteLine(");");
				return;
			}
			if (element.IsUnbounded)
			{
				TypeDesc typeDesc = arrayMapping.TypeDesc.CreateArrayTypeDesc();
				string cSharpName2 = typeDesc.CSharpName;
				string text4 = "el" + arrayName;
				string text5 = "c" + text4;
				base.Writer.WriteLine("{");
				base.Writer.Indent++;
				WriteArrayLocalDecl(cSharpName2, text4, source, arrayMapping.TypeDesc);
				if (element.IsNullable)
				{
					WriteNullCheckBegin(text4, element);
				}
				else
				{
					if (arrayMapping.TypeDesc.IsNullable)
					{
						base.Writer.Write("if (");
						base.Writer.Write(text4);
						base.Writer.Write(" != null)");
					}
					base.Writer.WriteLine("{");
					base.Writer.Indent++;
				}
				base.Writer.Write("for (int ");
				base.Writer.Write(text5);
				base.Writer.Write(" = 0; ");
				base.Writer.Write(text5);
				base.Writer.Write(" < ");
				if (typeDesc.IsArray)
				{
					base.Writer.Write(text4);
					base.Writer.Write(".Length");
				}
				else
				{
					base.Writer.Write("((");
					base.Writer.Write(typeof(ICollection).FullName);
					base.Writer.Write(")");
					base.Writer.Write(text4);
					base.Writer.Write(").Count");
				}
				base.Writer.Write("; ");
				base.Writer.Write(text5);
				base.Writer.WriteLine("++) {");
				base.Writer.Indent++;
				element.IsUnbounded = false;
				WriteElement(text4 + "[" + text5 + "]", element, arrayName, writeAccessor);
				element.IsUnbounded = true;
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				base.Writer.Indent--;
				base.Writer.WriteLine("}");
				return;
			}
			string cSharpName3 = arrayMapping.TypeDesc.CSharpName;
			base.Writer.WriteLine("{");
			base.Writer.Indent++;
			WriteArrayLocalDecl(cSharpName3, arrayName, source, arrayMapping.TypeDesc);
			if (element.IsNullable)
			{
				WriteNullCheckBegin(arrayName, element);
			}
			else
			{
				if (arrayMapping.TypeDesc.IsNullable)
				{
					base.Writer.Write("if (");
					base.Writer.Write(arrayName);
					base.Writer.Write(" != null)");
				}
				base.Writer.WriteLine("{");
				base.Writer.Indent++;
			}
			WriteStartElement(text, text2, writePrefixed: false);
			WriteArrayItems(arrayMapping.ElementsSortedByDerivation, null, null, arrayMapping.TypeDesc, arrayName, null);
			WriteEndElement();
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
		else if (element.Mapping is EnumMapping)
		{
			if (element.Mapping.IsSoap)
			{
				string s = (string)base.MethodNames[element.Mapping];
				base.Writer.Write("Writer.WriteStartElement(");
				WriteQuotedCSharpString(text);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(text2);
				base.Writer.WriteLine(");");
				base.Writer.Write(s);
				base.Writer.Write("(");
				base.Writer.Write(source);
				base.Writer.WriteLine(");");
				WriteEndElement();
			}
			else
			{
				WritePrimitive("WriteElementString", text, text2, element.Default, source, element.Mapping, writeXsiType: false, isElement: true, element.IsNullable);
			}
		}
		else if (element.Mapping is PrimitiveMapping)
		{
			PrimitiveMapping primitiveMapping = (PrimitiveMapping)element.Mapping;
			if (primitiveMapping.TypeDesc == base.QnameTypeDesc)
			{
				WriteQualifiedNameElement(text, text2, element.Default, source, element.IsNullable, primitiveMapping.IsSoap, primitiveMapping);
				return;
			}
			string text6 = (primitiveMapping.IsSoap ? "Encoded" : "Literal");
			string text7 = (primitiveMapping.TypeDesc.XmlEncodingNotRequired ? "Raw" : "");
			WritePrimitive(element.IsNullable ? ("WriteNullableString" + text6 + text7) : ("WriteElementString" + text7), text, text2, element.Default, source, primitiveMapping, primitiveMapping.IsSoap, isElement: true, element.IsNullable);
		}
		else if (element.Mapping is StructMapping)
		{
			StructMapping structMapping = (StructMapping)element.Mapping;
			if (structMapping.IsSoap)
			{
				base.Writer.Write("WritePotentiallyReferencingElement(");
				WriteQuotedCSharpString(text);
				base.Writer.Write(", ");
				WriteQuotedCSharpString(text2);
				base.Writer.Write(", ");
				base.Writer.Write(source);
				if (!writeAccessor)
				{
					base.Writer.Write(", ");
					base.Writer.Write(base.RaCodeGen.GetStringForTypeof(structMapping.TypeDesc.CSharpName, structMapping.TypeDesc.UseReflection));
					base.Writer.Write(", true, ");
				}
				else
				{
					base.Writer.Write(", null, false, ");
				}
				WriteValue(element.IsNullable);
			}
			else
			{
				string s2 = ReferenceMapping(structMapping);
				base.Writer.Write(s2);
				base.Writer.Write("(");
				WriteQuotedCSharpString(text);
				base.Writer.Write(", ");
				if (text2 == null)
				{
					base.Writer.Write("null");
				}
				else
				{
					WriteQuotedCSharpString(text2);
				}
				base.Writer.Write(", ");
				base.Writer.Write(source);
				if (structMapping.TypeDesc.IsNullable)
				{
					base.Writer.Write(", ");
					WriteValue(element.IsNullable);
				}
				base.Writer.Write(", false");
			}
			base.Writer.WriteLine(");");
		}
		else
		{
			if (!(element.Mapping is SpecialMapping))
			{
				throw new InvalidOperationException(System.SR.XmlInternalError);
			}
			if (element.Mapping is SerializableMapping)
			{
				WriteElementCall("WriteSerializable", typeof(IXmlSerializable), source, text, text2, element.IsNullable, !element.Any);
				return;
			}
			base.Writer.Write("if ((");
			base.Writer.Write(source);
			base.Writer.Write(") is ");
			base.Writer.Write(typeof(XmlNode).FullName);
			base.Writer.Write(" || ");
			base.Writer.Write(source);
			base.Writer.Write(" == null");
			base.Writer.WriteLine(") {");
			base.Writer.Indent++;
			WriteElementCall("WriteElementLiteral", typeof(XmlNode), source, text, text2, element.IsNullable, element.Any);
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
			base.Writer.WriteLine("else {");
			base.Writer.Indent++;
			base.Writer.Write("throw CreateInvalidAnyTypeException(");
			base.Writer.Write(source);
			base.Writer.WriteLine(");");
			base.Writer.Indent--;
			base.Writer.WriteLine("}");
		}
	}

	private void WriteElementCall(string func, Type cast, string source, string name, string ns, bool isNullable, bool isAny)
	{
		base.Writer.Write(func);
		base.Writer.Write("((");
		base.Writer.Write(cast.FullName);
		base.Writer.Write(")");
		base.Writer.Write(source);
		base.Writer.Write(", ");
		WriteQuotedCSharpString(name);
		base.Writer.Write(", ");
		WriteQuotedCSharpString(ns);
		base.Writer.Write(", ");
		WriteValue(isNullable);
		base.Writer.Write(", ");
		WriteValue(isAny);
		base.Writer.WriteLine(");");
	}

	[RequiresUnreferencedCode("calls GetType")]
	private void WriteCheckDefault(TypeMapping mapping, string source, object value, bool isNullable)
	{
		base.Writer.Write("if (");
		if (value is string && ((string)value).Length == 0)
		{
			base.Writer.Write("(");
			base.Writer.Write(source);
			if (isNullable)
			{
				base.Writer.Write(" == null) || (");
			}
			else
			{
				base.Writer.Write(" != null) && (");
			}
			base.Writer.Write(source);
			base.Writer.Write(".Length != 0)");
		}
		else if (value is double || value is float)
		{
			base.Writer.Write("!");
			base.Writer.Write(source);
			base.Writer.Write(".Equals(");
			Type type = Type.GetType(mapping.TypeDesc.Type.FullName);
			WriteValue((type != null) ? Convert.ChangeType(value, type) : value);
			base.Writer.Write(")");
		}
		else
		{
			base.Writer.Write(source);
			base.Writer.Write(" != ");
			Type type2 = Type.GetType(mapping.TypeDesc.Type.FullName);
			WriteValue((type2 != null) ? Convert.ChangeType(value, type2) : value);
		}
		base.Writer.Write(")");
	}

	private void WriteChoiceTypeCheck(string source, string fullTypeName, bool useReflection, ChoiceIdentifierAccessor choice, string enumName, TypeDesc typeDesc)
	{
		base.Writer.Write("if (((object)");
		base.Writer.Write(source);
		base.Writer.Write(") != null && !(");
		WriteInstanceOf(source, fullTypeName, useReflection);
		base.Writer.Write(")) throw CreateMismatchChoiceException(");
		WriteQuotedCSharpString(typeDesc.FullName);
		base.Writer.Write(", ");
		WriteQuotedCSharpString(choice.MemberName);
		base.Writer.Write(", ");
		WriteQuotedCSharpString(enumName);
		base.Writer.WriteLine(");");
	}

	private void WriteNullCheckBegin(string source, ElementAccessor element)
	{
		base.Writer.Write("if ((object)(");
		base.Writer.Write(source);
		base.Writer.WriteLine(") == null) {");
		base.Writer.Indent++;
		WriteLiteralNullTag(element.Name, (element.Form == XmlSchemaForm.Qualified) ? element.Namespace : "");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.WriteLine("else {");
		base.Writer.Indent++;
	}

	private void WriteValue(object value)
	{
		if (value == null)
		{
			base.Writer.Write("null");
			return;
		}
		Type type = value.GetType();
		if (type == typeof(string))
		{
			string value2 = (string)value;
			WriteQuotedCSharpString(value2);
		}
		else if (type == typeof(char))
		{
			base.Writer.Write('\'');
			char c = (char)value;
			if (c == '\'')
			{
				base.Writer.Write("'");
			}
			else
			{
				base.Writer.Write(c);
			}
			base.Writer.Write('\'');
		}
		else if (type == typeof(int))
		{
			base.Writer.Write(((int)value).ToString(null, NumberFormatInfo.InvariantInfo));
		}
		else if (type == typeof(double))
		{
			if (double.IsNaN((double)value))
			{
				base.Writer.Write("System.Double.NaN");
			}
			else if (double.IsPositiveInfinity((double)value))
			{
				base.Writer.Write("System.Double.PositiveInfinity");
			}
			else if (double.IsNegativeInfinity((double)value))
			{
				base.Writer.Write("System.Double.NegativeInfinity");
			}
			else
			{
				base.Writer.Write(((double)value).ToString("R", NumberFormatInfo.InvariantInfo));
			}
		}
		else if (type == typeof(bool))
		{
			base.Writer.Write(((bool)value) ? "true" : "false");
		}
		else if (type == typeof(short) || type == typeof(long) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) || type == typeof(byte) || type == typeof(sbyte))
		{
			base.Writer.Write("(");
			base.Writer.Write(type.FullName);
			base.Writer.Write(")");
			base.Writer.Write("(");
			base.Writer.Write(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
			base.Writer.Write(")");
		}
		else if (type == typeof(float))
		{
			if (float.IsNaN((float)value))
			{
				base.Writer.Write("System.Single.NaN");
				return;
			}
			if (float.IsPositiveInfinity((float)value))
			{
				base.Writer.Write("System.Single.PositiveInfinity");
				return;
			}
			if (float.IsNegativeInfinity((float)value))
			{
				base.Writer.Write("System.Single.NegativeInfinity");
				return;
			}
			base.Writer.Write(((float)value).ToString("R", NumberFormatInfo.InvariantInfo));
			base.Writer.Write("f");
		}
		else if (type == typeof(decimal))
		{
			base.Writer.Write(((decimal)value).ToString(null, NumberFormatInfo.InvariantInfo));
			base.Writer.Write("m");
		}
		else if (type == typeof(DateTime))
		{
			base.Writer.Write(" new ");
			base.Writer.Write(type.FullName);
			base.Writer.Write("(");
			base.Writer.Write(((DateTime)value).Ticks.ToString(CultureInfo.InvariantCulture));
			base.Writer.Write(")");
		}
		else if (type == typeof(DateTimeOffset))
		{
			base.Writer.Write(" new ");
			base.Writer.Write(type.FullName);
			base.Writer.Write("(");
			base.Writer.Write(((DateTimeOffset)value).Ticks.ToString(CultureInfo.InvariantCulture));
			base.Writer.Write(", new ");
			base.Writer.Write(((DateTimeOffset)value).Offset.GetType().FullName);
			base.Writer.Write("(");
			base.Writer.Write(((DateTimeOffset)value).Offset.Ticks.ToString(CultureInfo.InvariantCulture));
			base.Writer.Write("))");
		}
		else if (type == typeof(TimeSpan))
		{
			base.Writer.Write(" new ");
			base.Writer.Write(type.FullName);
			base.Writer.Write("(");
			base.Writer.Write(((TimeSpan)value).Ticks.ToString(CultureInfo.InvariantCulture));
			base.Writer.Write(")");
		}
		else
		{
			if (!type.IsEnum)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlUnsupportedDefaultType, type.FullName));
			}
			base.Writer.Write(((int)value).ToString(null, NumberFormatInfo.InvariantInfo));
		}
	}

	private void WriteNamespaces(string source)
	{
		base.Writer.Write("WriteNamespaceDeclarations(");
		base.Writer.Write(source);
		base.Writer.WriteLine(");");
	}

	private int FindXmlnsIndex(MemberMapping[] members)
	{
		for (int i = 0; i < members.Length; i++)
		{
			if (members[i].Xmlns != null)
			{
				return i;
			}
		}
		return -1;
	}

	private void WriteExtraMembers(string loopStartSource, string loopEndSource)
	{
		base.Writer.Write("for (int i = ");
		base.Writer.Write(loopStartSource);
		base.Writer.Write("; i < ");
		base.Writer.Write(loopEndSource);
		base.Writer.WriteLine("; i++) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("if (p[i] != null) {");
		base.Writer.Indent++;
		base.Writer.WriteLine("WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
		base.Writer.Indent--;
		base.Writer.WriteLine("}");
	}

	private void WriteLocalDecl(string typeName, string variableName, string initValue, bool useReflection)
	{
		base.RaCodeGen.WriteLocalDecl(typeName, variableName, initValue, useReflection);
	}

	private void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
	{
		base.RaCodeGen.WriteArrayLocalDecl(typeName, variableName, initValue, arrayTypeDesc);
	}

	private void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
	{
		base.RaCodeGen.WriteTypeCompare(variable, escapedTypeName, useReflection);
	}

	private void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
	{
		base.RaCodeGen.WriteInstanceOf(source, escapedTypeName, useReflection);
	}

	private void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
	{
		base.RaCodeGen.WriteArrayTypeCompare(variable, escapedTypeName, elementTypeName, useReflection);
	}

	private void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
	{
		base.RaCodeGen.WriteEnumCase(fullTypeName, c, useReflection);
	}

	private string FindChoiceEnumValue(ElementAccessor element, EnumMapping choiceMapping, bool useReflection)
	{
		string text = null;
		for (int i = 0; i < choiceMapping.Constants.Length; i++)
		{
			string xmlName = choiceMapping.Constants[i].XmlName;
			if (element.Any && element.Name.Length == 0)
			{
				if (xmlName == "##any:")
				{
					text = ((!useReflection) ? choiceMapping.Constants[i].Name : choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture));
					break;
				}
				continue;
			}
			int num = xmlName.LastIndexOf(':');
			ReadOnlySpan<char> span = ((num < 0) ? ((ReadOnlySpan<char>)choiceMapping.Namespace) : xmlName.AsSpan(0, num));
			ReadOnlySpan<char> span2 = ((num < 0) ? ((ReadOnlySpan<char>)xmlName) : xmlName.AsSpan(num + 1));
			if (span2.SequenceEqual(element.Name) && ((element.Form == XmlSchemaForm.Unqualified && span.IsEmpty) || span.SequenceEqual(element.Namespace)))
			{
				text = ((!useReflection) ? choiceMapping.Constants[i].Name : choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture));
				break;
			}
		}
		if (text == null || text.Length == 0)
		{
			if (element.Any && element.Name.Length == 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceMissingAnyValue, choiceMapping.TypeDesc.FullName));
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlChoiceMissingValue, choiceMapping.TypeDesc.FullName, element.Namespace + ":" + element.Name, element.Name, element.Namespace));
		}
		if (!useReflection)
		{
			CodeIdentifier.CheckValidIdentifier(text);
		}
		return text;
	}
}
