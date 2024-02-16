using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace System.Xml.Serialization;

internal sealed class ReflectionAwareCodeGen
{
	private Hashtable _reflectionVariables;

	private int _nextReflectionVariableNumber;

	private readonly IndentedWriter _writer;

	internal ReflectionAwareCodeGen(IndentedWriter writer)
	{
		_writer = writer;
	}

	[RequiresUnreferencedCode("calls GetTypeDesc")]
	internal void WriteReflectionInit(TypeScope scope)
	{
		foreach (Type type in scope.Types)
		{
			TypeDesc typeDesc = scope.GetTypeDesc(type);
			if (typeDesc.UseReflection)
			{
				WriteTypeInfo(scope, typeDesc, type);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	private string WriteTypeInfo(TypeScope scope, TypeDesc typeDesc, Type type)
	{
		InitTheFirstTime();
		string cSharpName = typeDesc.CSharpName;
		string text = (string)_reflectionVariables[cSharpName];
		if (text != null)
		{
			return text;
		}
		if (type.IsArray)
		{
			text = GenerateVariableName("array", typeDesc.CSharpName);
			TypeDesc arrayElementTypeDesc = typeDesc.ArrayElementTypeDesc;
			if (arrayElementTypeDesc.UseReflection)
			{
				string text2 = WriteTypeInfo(scope, arrayElementTypeDesc, scope.GetTypeFromTypeDesc(arrayElementTypeDesc));
				_writer.WriteLine("static " + typeof(Type).FullName + " " + text + " = " + text2 + ".MakeArrayType();");
			}
			else
			{
				string text3 = WriteAssemblyInfo(type);
				_writer.Write("static " + typeof(Type).FullName + " " + text + " = " + text3 + ".GetType(");
				WriteQuotedCSharpString(type.FullName);
				_writer.WriteLine(");");
			}
		}
		else
		{
			text = GenerateVariableName("type", typeDesc.CSharpName);
			Type underlyingType = Nullable.GetUnderlyingType(type);
			if (underlyingType != null)
			{
				string text4 = WriteTypeInfo(scope, scope.GetTypeDesc(underlyingType), underlyingType);
				_writer.WriteLine("static " + typeof(Type).FullName + " " + text + " = typeof(System.Nullable<>).MakeGenericType(new " + typeof(Type).FullName + "[] {" + text4 + "});");
			}
			else
			{
				string text5 = WriteAssemblyInfo(type);
				_writer.Write("static " + typeof(Type).FullName + " " + text + " = " + text5 + ".GetType(");
				WriteQuotedCSharpString(type.FullName);
				_writer.WriteLine(");");
			}
		}
		_reflectionVariables.Add(cSharpName, text);
		TypeMapping typeMappingFromTypeDesc = scope.GetTypeMappingFromTypeDesc(typeDesc);
		if (typeMappingFromTypeDesc != null)
		{
			WriteMappingInfo(typeMappingFromTypeDesc, text, type);
		}
		if (typeDesc.IsCollection || typeDesc.IsEnumerable)
		{
			TypeDesc arrayElementTypeDesc2 = typeDesc.ArrayElementTypeDesc;
			if (arrayElementTypeDesc2.UseReflection)
			{
				WriteTypeInfo(scope, arrayElementTypeDesc2, scope.GetTypeFromTypeDesc(arrayElementTypeDesc2));
			}
			WriteCollectionInfo(text, typeDesc, type);
		}
		return text;
	}

	[MemberNotNull("_reflectionVariables")]
	private void InitTheFirstTime()
	{
		if (_reflectionVariables == null)
		{
			_reflectionVariables = new Hashtable();
			_writer.Write(string.Format(CultureInfo.InvariantCulture, "\r\n    sealed class XSFieldInfo {{\r\n       {3} fieldInfo;\r\n        public XSFieldInfo({2} t, {1} memberName){{\r\n            fieldInfo = t.GetField(memberName);\r\n        }}\r\n        public {0} this[{0} o] {{\r\n            get {{\r\n                return fieldInfo.GetValue(o);\r\n            }}\r\n            set {{\r\n                fieldInfo.SetValue(o, value);\r\n            }}\r\n        }}\r\n\r\n    }}\r\n    sealed class XSPropInfo {{\r\n        {4} propInfo;\r\n        public XSPropInfo({2} t, {1} memberName){{\r\n            propInfo = t.GetProperty(memberName);\r\n        }}\r\n        public {0} this[{0} o] {{\r\n            get {{\r\n                return propInfo.GetValue(o, null);\r\n            }}\r\n            set {{\r\n                propInfo.SetValue(o, value, null);\r\n            }}\r\n        }}\r\n    }}\r\n    sealed class XSArrayInfo {{\r\n        {4} propInfo;\r\n        public XSArrayInfo({4} propInfo){{\r\n            this.propInfo = propInfo;\r\n        }}\r\n        public {0} this[{0} a, int i] {{\r\n            get {{\r\n                return propInfo.GetValue(a, new {0}[]{{i}});\r\n            }}\r\n            set {{\r\n                propInfo.SetValue(a, value, new {0}[]{{i}});\r\n            }}\r\n        }}\r\n    }}\r\n", "object", "string", typeof(Type).FullName, typeof(FieldInfo).FullName, typeof(PropertyInfo).FullName));
			WriteDefaultIndexerInit(typeof(IList), typeof(Array).FullName, collectionUseReflection: false, elementUseReflection: false);
		}
	}

	private void WriteMappingInfo(TypeMapping mapping, string typeVariable, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type)
	{
		string cSharpName = mapping.TypeDesc.CSharpName;
		if (mapping is StructMapping)
		{
			StructMapping structMapping = mapping as StructMapping;
			for (int i = 0; i < structMapping.Members.Length; i++)
			{
				MemberMapping memberMapping = structMapping.Members[i];
				string text = WriteMemberInfo(type, cSharpName, typeVariable, memberMapping.Name);
				if (memberMapping.CheckShouldPersist)
				{
					string memberName = "ShouldSerialize" + memberMapping.Name;
					text = WriteMethodInfo(cSharpName, typeVariable, memberName, false);
				}
				if (memberMapping.CheckSpecified != 0)
				{
					string memberName2 = memberMapping.Name + "Specified";
					text = WriteMemberInfo(type, cSharpName, typeVariable, memberName2);
				}
				if (memberMapping.ChoiceIdentifier != null)
				{
					string memberName3 = memberMapping.ChoiceIdentifier.MemberName;
					text = WriteMemberInfo(type, cSharpName, typeVariable, memberName3);
				}
			}
		}
		else if (mapping is EnumMapping)
		{
			FieldInfo[] fields = type.GetFields();
			for (int j = 0; j < fields.Length; j++)
			{
				WriteMemberInfo(type, cSharpName, typeVariable, fields[j].Name);
			}
		}
	}

	private void WriteCollectionInfo(string typeVariable, TypeDesc typeDesc, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type)
	{
		string cSharpName = CodeIdentifier.GetCSharpName(type);
		string cSharpName2 = typeDesc.ArrayElementTypeDesc.CSharpName;
		bool useReflection = typeDesc.ArrayElementTypeDesc.UseReflection;
		if (typeDesc.IsCollection)
		{
			WriteDefaultIndexerInit(type, cSharpName, typeDesc.UseReflection, useReflection);
		}
		else if (typeDesc.IsEnumerable)
		{
			if (typeDesc.IsGenericInterface)
			{
				WriteMethodInfo(cSharpName, typeVariable, "System.Collections.Generic.IEnumerable*", true);
			}
			else if (!typeDesc.IsPrivateImplementation)
			{
				WriteMethodInfo(cSharpName, typeVariable, "GetEnumerator", true);
			}
		}
		WriteMethodInfo(cSharpName, typeVariable, "Add", false, GetStringForTypeof(cSharpName2, useReflection));
	}

	private string WriteAssemblyInfo(Type type)
	{
		string fullName = type.Assembly.FullName;
		string text = (string)_reflectionVariables[fullName];
		if (text == null)
		{
			int num = fullName.IndexOf(',');
			string fullName2 = ((num > -1) ? fullName.Substring(0, num) : fullName);
			text = GenerateVariableName("assembly", fullName2);
			_writer.Write("static " + typeof(Assembly).FullName + " " + text + " = ResolveDynamicAssembly(");
			WriteQuotedCSharpString(DynamicAssemblies.GetName(type.Assembly));
			_writer.WriteLine(");");
			_reflectionVariables.Add(fullName, text);
		}
		return text;
	}

	private string WriteMemberInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type, string escapedName, string typeVariable, string memberName)
	{
		MemberInfo[] member = type.GetMember(memberName);
		for (int i = 0; i < member.Length; i++)
		{
			if (member[i] is PropertyInfo)
			{
				string text = GenerateVariableName("prop", memberName);
				_writer.Write("static XSPropInfo " + text + " = new XSPropInfo(" + typeVariable + ", ");
				WriteQuotedCSharpString(memberName);
				_writer.WriteLine(");");
				_reflectionVariables.Add(memberName + ":" + escapedName, text);
				return text;
			}
			if (member[i] is FieldInfo)
			{
				string text2 = GenerateVariableName("field", memberName);
				_writer.Write("static XSFieldInfo " + text2 + " = new XSFieldInfo(" + typeVariable + ", ");
				WriteQuotedCSharpString(memberName);
				_writer.WriteLine(");");
				_reflectionVariables.Add(memberName + ":" + escapedName, text2);
				return text2;
			}
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlSerializerUnsupportedType, member[0]));
	}

	private string WriteMethodInfo(string escapedName, string typeVariable, string memberName, bool isNonPublic, params string[] paramTypes)
	{
		string text = GenerateVariableName("method", memberName);
		_writer.Write("static " + typeof(MethodInfo).FullName + " " + text + " = " + typeVariable + ".GetMethod(");
		WriteQuotedCSharpString(memberName);
		_writer.Write(", ");
		string fullName = typeof(BindingFlags).FullName;
		_writer.Write(fullName);
		_writer.Write(".Public | ");
		_writer.Write(fullName);
		_writer.Write(".Instance | ");
		_writer.Write(fullName);
		_writer.Write(".Static");
		if (isNonPublic)
		{
			_writer.Write(" | ");
			_writer.Write(fullName);
			_writer.Write(".NonPublic");
		}
		_writer.Write(", null, ");
		_writer.Write("new " + typeof(Type).FullName + "[] { ");
		for (int i = 0; i < paramTypes.Length; i++)
		{
			_writer.Write(paramTypes[i]);
			if (i < paramTypes.Length - 1)
			{
				_writer.Write(", ");
			}
		}
		_writer.WriteLine("}, null);");
		_reflectionVariables.Add(memberName + ":" + escapedName, text);
		return text;
	}

	private string WriteDefaultIndexerInit([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents)] Type type, string escapedName, bool collectionUseReflection, bool elementUseReflection)
	{
		string text = GenerateVariableName("item", escapedName);
		PropertyInfo defaultIndexer = TypeScope.GetDefaultIndexer(type, null);
		_writer.Write("static XSArrayInfo ");
		_writer.Write(text);
		_writer.Write("= new XSArrayInfo(");
		_writer.Write(GetStringForTypeof(CodeIdentifier.GetCSharpName(type), collectionUseReflection));
		_writer.Write(".GetProperty(");
		WriteQuotedCSharpString(defaultIndexer.Name);
		_writer.Write(",");
		_writer.Write(GetStringForTypeof(CodeIdentifier.GetCSharpName(defaultIndexer.PropertyType), elementUseReflection));
		_writer.Write(",new ");
		_writer.Write(typeof(Type[]).FullName);
		_writer.WriteLine("{typeof(int)}));");
		_reflectionVariables.Add("0:" + escapedName, text);
		return text;
	}

	private string GenerateVariableName(string prefix, string fullName)
	{
		_nextReflectionVariableNumber++;
		return prefix + _nextReflectionVariableNumber + "_" + CodeIdentifier.MakeValidInternal(fullName.Replace('.', '_'));
	}

	internal string GetReflectionVariable(string typeFullName, string memberName)
	{
		string key = ((memberName != null) ? (memberName + ":" + typeFullName) : typeFullName);
		return (string)_reflectionVariables[key];
	}

	internal string GetStringForMethodInvoke(string obj, string escapedTypeName, string methodName, bool useReflection, params string[] args)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (useReflection)
		{
			stringBuilder.Append(GetReflectionVariable(escapedTypeName, methodName));
			stringBuilder.Append(".Invoke(");
			stringBuilder.Append(obj);
			stringBuilder.Append(", new object[] {");
		}
		else
		{
			stringBuilder.Append(obj);
			stringBuilder.Append(".@");
			stringBuilder.Append(methodName);
			stringBuilder.Append('(');
		}
		for (int i = 0; i < args.Length; i++)
		{
			if (i != 0)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(args[i]);
		}
		if (useReflection)
		{
			stringBuilder.Append("})");
		}
		else
		{
			stringBuilder.Append(')');
		}
		return stringBuilder.ToString();
	}

	internal string GetStringForEnumCompare(EnumMapping mapping, string memberName, bool useReflection)
	{
		if (!useReflection)
		{
			CodeIdentifier.CheckValidIdentifier(memberName);
			return mapping.TypeDesc.CSharpName + ".@" + memberName;
		}
		string stringForEnumMember = GetStringForEnumMember(mapping.TypeDesc.CSharpName, memberName, useReflection);
		return GetStringForEnumLongValue(stringForEnumMember, useReflection);
	}

	internal string GetStringForEnumLongValue(string variable, bool useReflection)
	{
		if (useReflection)
		{
			return typeof(Convert).FullName + ".ToInt64(" + variable + ")";
		}
		return "((" + typeof(long).FullName + ")" + variable + ")";
	}

	internal string GetStringForTypeof(string typeFullName, bool useReflection)
	{
		if (useReflection)
		{
			return GetReflectionVariable(typeFullName, null);
		}
		return "typeof(" + typeFullName + ")";
	}

	internal string GetStringForMember(string obj, string memberName, TypeDesc typeDesc)
	{
		if (!typeDesc.UseReflection)
		{
			return obj + ".@" + memberName;
		}
		while (typeDesc != null)
		{
			string cSharpName = typeDesc.CSharpName;
			string reflectionVariable = GetReflectionVariable(cSharpName, memberName);
			if (reflectionVariable != null)
			{
				return reflectionVariable + "[" + obj + "]";
			}
			typeDesc = typeDesc.BaseTypeDesc;
			if (typeDesc != null && !typeDesc.UseReflection)
			{
				return "((" + typeDesc.CSharpName + ")" + obj + ").@" + memberName;
			}
		}
		return "[" + obj + "]";
	}

	internal string GetStringForEnumMember(string typeFullName, string memberName, bool useReflection)
	{
		if (!useReflection)
		{
			return typeFullName + ".@" + memberName;
		}
		string reflectionVariable = GetReflectionVariable(typeFullName, memberName);
		return reflectionVariable + "[null]";
	}

	internal string GetStringForArrayMember(string arrayName, string subscript, TypeDesc arrayTypeDesc)
	{
		if (!arrayTypeDesc.UseReflection)
		{
			return arrayName + "[" + subscript + "]";
		}
		string typeFullName = (arrayTypeDesc.IsCollection ? arrayTypeDesc.CSharpName : typeof(Array).FullName);
		string reflectionVariable = GetReflectionVariable(typeFullName, "0");
		return reflectionVariable + "[" + arrayName + ", " + subscript + "]";
	}

	internal string GetStringForMethod(string obj, string typeFullName, string memberName, bool useReflection)
	{
		if (!useReflection)
		{
			return obj + "." + memberName + "(";
		}
		string reflectionVariable = GetReflectionVariable(typeFullName, memberName);
		return reflectionVariable + ".Invoke(" + obj + ", new object[]{";
	}

	internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast)
	{
		return GetStringForCreateInstance(escapedTypeName, useReflection, ctorInaccessible, cast, string.Empty);
	}

	internal string GetStringForCreateInstance(string escapedTypeName, bool useReflection, bool ctorInaccessible, bool cast, string arg)
	{
		if (!useReflection && !ctorInaccessible)
		{
			return "new " + escapedTypeName + "(" + arg + ")";
		}
		return GetStringForCreateInstance(GetStringForTypeof(escapedTypeName, useReflection), (cast && !useReflection) ? escapedTypeName : null, ctorInaccessible, arg);
	}

	internal string GetStringForCreateInstance(string type, string cast, bool nonPublic, string arg)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (cast != null && cast.Length > 0)
		{
			stringBuilder.Append('(');
			stringBuilder.Append(cast);
			stringBuilder.Append(')');
		}
		stringBuilder.Append(typeof(Activator).FullName);
		stringBuilder.Append(".CreateInstance(");
		stringBuilder.Append(type);
		stringBuilder.Append(", ");
		string fullName = typeof(BindingFlags).FullName;
		stringBuilder.Append(fullName);
		stringBuilder.Append(".Instance | ");
		stringBuilder.Append(fullName);
		stringBuilder.Append(".Public | ");
		stringBuilder.Append(fullName);
		stringBuilder.Append(".CreateInstance");
		if (nonPublic)
		{
			stringBuilder.Append(" | ");
			stringBuilder.Append(fullName);
			stringBuilder.Append(".NonPublic");
		}
		if (arg == null || arg.Length == 0)
		{
			stringBuilder.Append(", null, new object[0], null)");
		}
		else
		{
			stringBuilder.Append(", null, new object[] { ");
			stringBuilder.Append(arg);
			stringBuilder.Append(" }, null)");
		}
		return stringBuilder.ToString();
	}

	internal void WriteLocalDecl(string typeFullName, string variableName, string initValue, bool useReflection)
	{
		if (useReflection)
		{
			typeFullName = "object";
		}
		_writer.Write(typeFullName);
		_writer.Write(" ");
		_writer.Write(variableName);
		if (initValue != null)
		{
			_writer.Write(" = ");
			if (!useReflection && initValue != "null")
			{
				_writer.Write("(" + typeFullName + ")");
			}
			_writer.Write(initValue);
		}
		_writer.WriteLine(";");
	}

	internal void WriteCreateInstance(string escapedName, string source, bool useReflection, bool ctorInaccessible)
	{
		_writer.Write(useReflection ? "object" : escapedName);
		_writer.Write(" ");
		_writer.Write(source);
		_writer.Write(" = ");
		_writer.Write(GetStringForCreateInstance(escapedName, useReflection, ctorInaccessible, !useReflection && ctorInaccessible));
		_writer.WriteLine(";");
	}

	internal void WriteInstanceOf(string source, string escapedTypeName, bool useReflection)
	{
		if (!useReflection)
		{
			_writer.Write(source);
			_writer.Write(" is ");
			_writer.Write(escapedTypeName);
		}
		else
		{
			_writer.Write(GetReflectionVariable(escapedTypeName, null));
			_writer.Write(".IsAssignableFrom(");
			_writer.Write(source);
			_writer.Write(".GetType())");
		}
	}

	internal void WriteArrayLocalDecl(string typeName, string variableName, string initValue, TypeDesc arrayTypeDesc)
	{
		if (arrayTypeDesc.UseReflection)
		{
			typeName = (arrayTypeDesc.IsEnumerable ? typeof(IEnumerable).FullName : ((!arrayTypeDesc.IsCollection) ? typeof(Array).FullName : typeof(ICollection).FullName));
		}
		_writer.Write(typeName);
		_writer.Write(" ");
		_writer.Write(variableName);
		if (initValue != null)
		{
			_writer.Write(" = ");
			if (initValue != "null")
			{
				_writer.Write("(" + typeName + ")");
			}
			_writer.Write(initValue);
		}
		_writer.WriteLine(";");
	}

	internal void WriteEnumCase(string fullTypeName, ConstantMapping c, bool useReflection)
	{
		_writer.Write("case ");
		if (useReflection)
		{
			_writer.Write(c.Value.ToString(CultureInfo.InvariantCulture));
		}
		else
		{
			_writer.Write(fullTypeName);
			_writer.Write(".@");
			CodeIdentifier.CheckValidIdentifier(c.Name);
			_writer.Write(c.Name);
		}
		_writer.Write(": ");
	}

	internal void WriteTypeCompare(string variable, string escapedTypeName, bool useReflection)
	{
		_writer.Write(variable);
		_writer.Write(" == ");
		_writer.Write(GetStringForTypeof(escapedTypeName, useReflection));
	}

	internal void WriteArrayTypeCompare(string variable, string escapedTypeName, string elementTypeName, bool useReflection)
	{
		if (!useReflection)
		{
			_writer.Write(variable);
			_writer.Write(" == typeof(");
			_writer.Write(escapedTypeName);
			_writer.Write(")");
		}
		else
		{
			_writer.Write(variable);
			_writer.Write(".IsArray ");
			_writer.Write(" && ");
			WriteTypeCompare(variable + ".GetElementType()", elementTypeName, useReflection);
		}
	}

	internal static void WriteQuotedCSharpString(IndentedWriter writer, string value)
	{
		if (value == null)
		{
			writer.Write("null");
			return;
		}
		writer.Write("@\"");
		foreach (char c in value)
		{
			if (c < ' ')
			{
				switch (c)
				{
				case '\r':
					writer.Write("\\r");
					continue;
				case '\n':
					writer.Write("\\n");
					continue;
				case '\t':
					writer.Write("\\t");
					continue;
				}
				byte b = (byte)c;
				writer.Write("\\x");
				writer.Write(System.HexConverter.ToCharUpper(b >> 4));
				writer.Write(System.HexConverter.ToCharUpper(b));
			}
			else if (c == '"')
			{
				writer.Write("\"\"");
			}
			else
			{
				writer.Write(c);
			}
		}
		writer.Write("\"");
	}

	internal void WriteQuotedCSharpString(string value)
	{
		WriteQuotedCSharpString(_writer, value);
	}
}
