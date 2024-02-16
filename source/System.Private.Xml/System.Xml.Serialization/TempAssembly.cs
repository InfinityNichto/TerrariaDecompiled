using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Xml.Serialization;

internal sealed class TempAssembly
{
	internal sealed class TempMethod
	{
		internal MethodInfo writeMethod;

		internal MethodInfo readMethod;

		internal string name;

		internal string ns;

		internal bool isSoap;

		internal string methodKey;
	}

	internal sealed class TempMethodDictionary : Dictionary<string, TempMethod>
	{
	}

	private readonly Assembly _assembly;

	private XmlSerializerImplementation _contract;

	private IDictionary _writerMethods;

	private IDictionary _readerMethods;

	private TempMethodDictionary _methods;

	internal static bool UseLegacySerializerGeneration => false;

	internal XmlSerializerImplementation Contract
	{
		[RequiresUnreferencedCode("calls GetTypeFromAssembly")]
		get
		{
			if (_contract == null)
			{
				_contract = (XmlSerializerImplementation)Activator.CreateInstance(GetTypeFromAssembly(_assembly, "XmlSerializerContract"));
			}
			return _contract;
		}
	}

	internal TempAssembly(XmlMapping[] xmlMappings, Assembly assembly, XmlSerializerImplementation contract)
	{
		_assembly = assembly;
		InitAssemblyMethods(xmlMappings);
		_contract = contract;
	}

	[RequiresUnreferencedCode("calls GenerateRefEmitAssembly")]
	internal TempAssembly(XmlMapping[] xmlMappings, Type[] types, string defaultNamespace, string location)
	{
		bool flag = false;
		for (int i = 0; i < xmlMappings.Length; i++)
		{
			xmlMappings[i].CheckShallow();
			if (xmlMappings[i].IsSoap)
			{
				flag = true;
			}
		}
		bool flag2 = false;
		if (!flag)
		{
			_ = UseLegacySerializerGeneration;
			try
			{
				_assembly = GenerateRefEmitAssembly(xmlMappings, types, defaultNamespace);
			}
			catch (CodeGeneratorConversionException)
			{
				flag2 = true;
			}
		}
		else
		{
			flag2 = true;
		}
		if (flag2)
		{
			throw new PlatformNotSupportedException(System.SR.CompilingScriptsNotSupported);
		}
		InitAssemblyMethods(xmlMappings);
	}

	internal void InitAssemblyMethods(XmlMapping[] xmlMappings)
	{
		_methods = new TempMethodDictionary();
		for (int i = 0; i < xmlMappings.Length; i++)
		{
			TempMethod tempMethod = new TempMethod();
			tempMethod.isSoap = xmlMappings[i].IsSoap;
			tempMethod.methodKey = xmlMappings[i].Key;
			if (xmlMappings[i] is XmlTypeMapping xmlTypeMapping)
			{
				tempMethod.name = xmlTypeMapping.ElementName;
				tempMethod.ns = xmlTypeMapping.Namespace;
			}
			_methods.Add(xmlMappings[i].Key, tempMethod);
		}
	}

	[RequiresUnreferencedCode("calls LoadFrom")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "Annotating this as dangerous will make the core of the serializer to be marked as not safe, instead this pattern is only dangerous if using sgen only. See https://github.com/dotnet/runtime/issues/50820")]
	internal static Assembly LoadGeneratedAssembly(Type type, string defaultNamespace, out XmlSerializerImplementation contract)
	{
		Assembly assembly = null;
		contract = null;
		string text = null;
		object[] customAttributes = type.GetCustomAttributes(typeof(XmlSerializerAssemblyAttribute), inherit: false);
		if (customAttributes.Length == 0)
		{
			AssemblyName name = type.Assembly.GetName();
			text = (name.Name = Compiler.GetTempAssemblyName(name, defaultNamespace));
			name.CodeBase = null;
			name.CultureInfo = CultureInfo.InvariantCulture;
			try
			{
				assembly = Assembly.Load(name);
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException)
				{
					throw;
				}
			}
			if ((object)assembly == null)
			{
				assembly = LoadAssemblyByPath(type, text);
			}
			if (assembly == null)
			{
				if (XmlSerializer.Mode == SerializationMode.PreGenOnly)
				{
					throw new Exception(System.SR.Format(System.SR.FailLoadAssemblyUnderPregenMode, text));
				}
				return null;
			}
			if (!IsSerializerVersionMatch(assembly, type, defaultNamespace))
			{
				XmlSerializationEventSource.Log.XmlSerializerExpired(text, type.FullName);
				return null;
			}
		}
		else
		{
			XmlSerializerAssemblyAttribute xmlSerializerAssemblyAttribute = (XmlSerializerAssemblyAttribute)customAttributes[0];
			if (xmlSerializerAssemblyAttribute.AssemblyName != null && xmlSerializerAssemblyAttribute.CodeBase != null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlPregenInvalidXmlSerializerAssemblyAttribute, "AssemblyName", "CodeBase"));
			}
			if (xmlSerializerAssemblyAttribute.AssemblyName != null)
			{
				text = xmlSerializerAssemblyAttribute.AssemblyName;
				assembly = Assembly.Load(text);
			}
			else if (xmlSerializerAssemblyAttribute.CodeBase != null && xmlSerializerAssemblyAttribute.CodeBase.Length > 0)
			{
				text = xmlSerializerAssemblyAttribute.CodeBase;
				assembly = Assembly.LoadFrom(text);
			}
			else
			{
				text = type.Assembly.FullName;
				assembly = type.Assembly;
			}
			if (assembly == null)
			{
				throw new FileNotFoundException(null, text);
			}
		}
		Type typeFromAssembly = GetTypeFromAssembly(assembly, "XmlSerializerContract");
		contract = (XmlSerializerImplementation)Activator.CreateInstance(typeFromAssembly);
		if (contract.CanSerialize(type))
		{
			return assembly;
		}
		return null;
	}

	[RequiresUnreferencedCode("calls LoadFile")]
	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "Annotating this as dangerous will make the core of the serializer to be marked as not safe, instead this pattern is only dangerous if using sgen only. See https://github.com/dotnet/runtime/issues/50820")]
	private static Assembly LoadAssemblyByPath(Type type, string assemblyName)
	{
		Assembly result = null;
		string text = null;
		try
		{
			if (!string.IsNullOrEmpty(type.Assembly.Location))
			{
				text = Path.Combine(Path.GetDirectoryName(type.Assembly.Location), assemblyName + ".dll");
			}
			if ((string.IsNullOrEmpty(text) || !File.Exists(text)) && !string.IsNullOrEmpty(Assembly.GetEntryAssembly()?.Location))
			{
				text = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), assemblyName + ".dll");
			}
			if ((string.IsNullOrEmpty(text) || !File.Exists(text)) && !string.IsNullOrEmpty(AppContext.BaseDirectory))
			{
				text = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), assemblyName + ".dll");
			}
			if (!string.IsNullOrEmpty(text))
			{
				result = Assembly.LoadFile(text);
			}
		}
		catch (Exception ex)
		{
			if (ex is ThreadAbortException || ex is StackOverflowException || ex is OutOfMemoryException)
			{
				throw;
			}
		}
		return result;
	}

	private static bool IsSerializerVersionMatch(Assembly serializer, Type type, string defaultNamespace)
	{
		if (serializer == null)
		{
			return false;
		}
		object[] customAttributes = serializer.GetCustomAttributes(typeof(XmlSerializerVersionAttribute), inherit: false);
		if (customAttributes.Length != 1)
		{
			return false;
		}
		XmlSerializerVersionAttribute xmlSerializerVersionAttribute = (XmlSerializerVersionAttribute)customAttributes[0];
		if (xmlSerializerVersionAttribute.ParentAssemblyId == GenerateAssemblyId(type) && xmlSerializerVersionAttribute.Namespace == defaultNamespace)
		{
			return true;
		}
		return false;
	}

	private static string GenerateAssemblyId(Type type)
	{
		Module[] modules = type.Assembly.GetModules();
		ArrayList arrayList = new ArrayList();
		for (int i = 0; i < modules.Length; i++)
		{
			arrayList.Add(modules[i].ModuleVersionId.ToString());
		}
		arrayList.Sort();
		StringBuilder stringBuilder = new StringBuilder();
		for (int j = 0; j < arrayList.Count; j++)
		{
			stringBuilder.Append(arrayList[j].ToString());
			stringBuilder.Append(',');
		}
		return stringBuilder.ToString();
	}

	[RequiresUnreferencedCode("calls GenerateBegin")]
	internal static bool GenerateSerializerToStream(XmlMapping[] xmlMappings, Type[] types, string defaultNamespace, Assembly assembly, Hashtable assemblies, Stream stream)
	{
		Compiler compiler = new Compiler();
		try
		{
			Hashtable hashtable = new Hashtable();
			foreach (XmlMapping xmlMapping in xmlMappings)
			{
				hashtable[xmlMapping.Scope] = xmlMapping;
			}
			TypeScope[] array = new TypeScope[hashtable.Keys.Count];
			hashtable.Keys.CopyTo(array, 0);
			assemblies.Clear();
			Hashtable types2 = new Hashtable();
			TypeScope[] array2 = array;
			foreach (TypeScope typeScope in array2)
			{
				foreach (Type type3 in typeScope.Types)
				{
					compiler.AddImport(type3, types2);
					Assembly assembly2 = type3.Assembly;
					string fullName = assembly2.FullName;
					if (assemblies[fullName] == null)
					{
						assemblies[fullName] = assembly2;
					}
				}
			}
			for (int k = 0; k < types.Length; k++)
			{
				compiler.AddImport(types[k], types2);
			}
			compiler.AddImport(typeof(object).Assembly);
			compiler.AddImport(typeof(XmlSerializer).Assembly);
			IndentedWriter indentedWriter = new IndentedWriter(compiler.Source, compact: false);
			indentedWriter.WriteLine("[assembly:System.Security.AllowPartiallyTrustedCallers()]");
			indentedWriter.WriteLine("[assembly:System.Security.SecurityTransparent()]");
			indentedWriter.WriteLine("[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]");
			if (assembly != null && types.Length != 0)
			{
				for (int l = 0; l < types.Length; l++)
				{
					Type type2 = types[l];
					if (!(type2 == null) && DynamicAssemblies.IsTypeDynamic(type2))
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.XmlPregenTypeDynamic, types[l].FullName));
					}
				}
				indentedWriter.Write("[assembly:");
				indentedWriter.Write(typeof(XmlSerializerVersionAttribute).FullName);
				indentedWriter.Write("(");
				indentedWriter.Write("ParentAssemblyId=");
				ReflectionAwareCodeGen.WriteQuotedCSharpString(indentedWriter, GenerateAssemblyId(types[0]));
				indentedWriter.Write(", Version=");
				ReflectionAwareCodeGen.WriteQuotedCSharpString(indentedWriter, "1.0.0.0");
				if (defaultNamespace != null)
				{
					indentedWriter.Write(", Namespace=");
					ReflectionAwareCodeGen.WriteQuotedCSharpString(indentedWriter, defaultNamespace);
				}
				indentedWriter.WriteLine(")]");
			}
			CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
			codeIdentifiers.AddUnique("XmlSerializationWriter", "XmlSerializationWriter");
			codeIdentifiers.AddUnique("XmlSerializationReader", "XmlSerializationReader");
			string text = null;
			if (types != null && types.Length == 1 && types[0] != null)
			{
				text = CodeIdentifier.MakeValid(types[0].Name);
				if (types[0].IsArray)
				{
					text += "Array";
				}
			}
			indentedWriter.WriteLine("namespace Microsoft.Xml.Serialization.GeneratedAssembly {");
			indentedWriter.Indent++;
			indentedWriter.WriteLine();
			string text2 = "XmlSerializationWriter" + text;
			text2 = codeIdentifiers.AddUnique(text2, text2);
			XmlSerializationWriterCodeGen xmlSerializationWriterCodeGen = new XmlSerializationWriterCodeGen(indentedWriter, array, "public", text2);
			xmlSerializationWriterCodeGen.GenerateBegin();
			string[] array3 = new string[xmlMappings.Length];
			for (int m = 0; m < xmlMappings.Length; m++)
			{
				array3[m] = xmlSerializationWriterCodeGen.GenerateElement(xmlMappings[m]);
			}
			xmlSerializationWriterCodeGen.GenerateEnd();
			indentedWriter.WriteLine();
			string text3 = "XmlSerializationReader" + text;
			text3 = codeIdentifiers.AddUnique(text3, text3);
			XmlSerializationReaderCodeGen xmlSerializationReaderCodeGen = new XmlSerializationReaderCodeGen(indentedWriter, array, "public", text3);
			xmlSerializationReaderCodeGen.GenerateBegin();
			string[] array4 = new string[xmlMappings.Length];
			for (int n = 0; n < xmlMappings.Length; n++)
			{
				array4[n] = xmlSerializationReaderCodeGen.GenerateElement(xmlMappings[n]);
			}
			xmlSerializationReaderCodeGen.GenerateEnd(array4, xmlMappings, types);
			string baseSerializer = xmlSerializationReaderCodeGen.GenerateBaseSerializer("XmlSerializer1", text3, text2, codeIdentifiers);
			Hashtable hashtable2 = new Hashtable();
			for (int num = 0; num < xmlMappings.Length; num++)
			{
				if (hashtable2[xmlMappings[num].Key] == null)
				{
					hashtable2[xmlMappings[num].Key] = xmlSerializationReaderCodeGen.GenerateTypedSerializer(array4[num], array3[num], xmlMappings[num], codeIdentifiers, baseSerializer, text3, text2);
				}
			}
			xmlSerializationReaderCodeGen.GenerateSerializerContract("XmlSerializerContract", xmlMappings, types, text3, array4, text2, array3, hashtable2);
			indentedWriter.Indent--;
			indentedWriter.WriteLine("}");
			string s = compiler.Source.ToString();
			byte[] bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(s);
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush();
			return true;
		}
		finally
		{
			compiler.Close();
		}
	}

	[RequiresUnreferencedCode("calls GenerateElement")]
	internal static Assembly GenerateRefEmitAssembly(XmlMapping[] xmlMappings, Type[] types, string defaultNamespace)
	{
		Dictionary<TypeScope, XmlMapping> dictionary = new Dictionary<TypeScope, XmlMapping>();
		foreach (XmlMapping xmlMapping in xmlMappings)
		{
			dictionary[xmlMapping.Scope] = xmlMapping;
		}
		TypeScope[] array = new TypeScope[dictionary.Keys.Count];
		dictionary.Keys.CopyTo(array, 0);
		string name = "Microsoft.GeneratedCode";
		AssemblyBuilder assemblyBuilder = CodeGenerator.CreateAssemblyBuilder(name);
		if (types != null && types.Length != 0 && types[0] != null)
		{
			ConstructorInfo constructor = typeof(AssemblyVersionAttribute).GetConstructor(new Type[1] { typeof(string) });
			string text = types[0].Assembly.GetName().Version.ToString();
			assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[1] { text }));
		}
		CodeIdentifiers codeIdentifiers = new CodeIdentifiers();
		codeIdentifiers.AddUnique("XmlSerializationWriter", "XmlSerializationWriter");
		codeIdentifiers.AddUnique("XmlSerializationReader", "XmlSerializationReader");
		string text2 = null;
		if (types != null && types.Length == 1 && types[0] != null)
		{
			text2 = CodeIdentifier.MakeValid(types[0].Name);
			if (types[0].IsArray)
			{
				text2 += "Array";
			}
		}
		ModuleBuilder moduleBuilder = CodeGenerator.CreateModuleBuilder(assemblyBuilder, name);
		string text3 = "XmlSerializationWriter" + text2;
		text3 = codeIdentifiers.AddUnique(text3, text3);
		XmlSerializationWriterILGen xmlSerializationWriterILGen = new XmlSerializationWriterILGen(array, "public", text3);
		xmlSerializationWriterILGen.ModuleBuilder = moduleBuilder;
		xmlSerializationWriterILGen.GenerateBegin();
		string[] array2 = new string[xmlMappings.Length];
		for (int j = 0; j < xmlMappings.Length; j++)
		{
			array2[j] = xmlSerializationWriterILGen.GenerateElement(xmlMappings[j]);
		}
		Type type = xmlSerializationWriterILGen.GenerateEnd();
		string text4 = "XmlSerializationReader" + text2;
		text4 = codeIdentifiers.AddUnique(text4, text4);
		XmlSerializationReaderILGen xmlSerializationReaderILGen = new XmlSerializationReaderILGen(array, "public", text4);
		xmlSerializationReaderILGen.ModuleBuilder = moduleBuilder;
		xmlSerializationReaderILGen.CreatedTypes.Add(type.Name, type);
		xmlSerializationReaderILGen.GenerateBegin();
		string[] array3 = new string[xmlMappings.Length];
		for (int k = 0; k < xmlMappings.Length; k++)
		{
			array3[k] = xmlSerializationReaderILGen.GenerateElement(xmlMappings[k]);
		}
		xmlSerializationReaderILGen.GenerateEnd(array3, xmlMappings, types);
		string baseSerializer = xmlSerializationReaderILGen.GenerateBaseSerializer("XmlSerializer1", text4, text3, codeIdentifiers);
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		for (int l = 0; l < xmlMappings.Length; l++)
		{
			if (!dictionary2.ContainsKey(xmlMappings[l].Key))
			{
				dictionary2[xmlMappings[l].Key] = xmlSerializationReaderILGen.GenerateTypedSerializer(array3[l], array2[l], xmlMappings[l], codeIdentifiers, baseSerializer, text4, text3);
			}
		}
		xmlSerializationReaderILGen.GenerateSerializerContract("XmlSerializerContract", xmlMappings, types, text4, array3, text3, array2, dictionary2);
		return type.Assembly;
	}

	private static MethodInfo GetMethodFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string methodName)
	{
		MethodInfo method = type.GetMethod(methodName);
		if (method != null)
		{
			return method;
		}
		MissingMethodException ex = new MissingMethodException(type.FullName + "::" + methodName);
		throw ex;
	}

	[RequiresUnreferencedCode("calls GetType")]
	internal static Type GetTypeFromAssembly(Assembly assembly, string typeName)
	{
		typeName = "Microsoft.Xml.Serialization.GeneratedAssembly." + typeName;
		Type type = assembly.GetType(typeName);
		if (type == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlMissingType, typeName, assembly.FullName));
		}
		return type;
	}

	internal bool CanRead(XmlMapping mapping, XmlReader xmlReader)
	{
		if (mapping == null)
		{
			return false;
		}
		if (mapping.Accessor.Any)
		{
			return true;
		}
		TempMethod tempMethod = _methods[mapping.Key];
		return xmlReader.IsStartElement(tempMethod.name, tempMethod.ns);
	}

	[return: NotNullIfNotNull("encodingStyle")]
	private string ValidateEncodingStyle(string encodingStyle, string methodKey)
	{
		if (encodingStyle != null && encodingStyle.Length > 0)
		{
			if (!_methods[methodKey].isSoap)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidEncodingNotEncoded1, encodingStyle));
			}
			if (encodingStyle != "http://schemas.xmlsoap.org/soap/encoding/" && encodingStyle != "http://www.w3.org/2003/05/soap-encoding")
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.XmlInvalidEncoding3, encodingStyle, "http://schemas.xmlsoap.org/soap/encoding/", "http://www.w3.org/2003/05/soap-encoding"));
			}
		}
		else if (_methods[methodKey].isSoap)
		{
			encodingStyle = "http://schemas.xmlsoap.org/soap/encoding/";
		}
		return encodingStyle;
	}

	[RequiresUnreferencedCode("calls Contract")]
	internal object InvokeReader(XmlMapping mapping, XmlReader xmlReader, XmlDeserializationEvents events, string encodingStyle)
	{
		XmlSerializationReader xmlSerializationReader = null;
		try
		{
			encodingStyle = ValidateEncodingStyle(encodingStyle, mapping.Key);
			xmlSerializationReader = Contract.Reader;
			xmlSerializationReader.Init(xmlReader, events, encodingStyle, this);
			if (_methods[mapping.Key].readMethod == null)
			{
				if (_readerMethods == null)
				{
					_readerMethods = Contract.ReadMethods;
				}
				string text = (string)_readerMethods[mapping.Key];
				if (text == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlNotSerializable, mapping.Accessor.Name));
				}
				_methods[mapping.Key].readMethod = GetMethodFromType(xmlSerializationReader.GetType(), text);
			}
			return _methods[mapping.Key].readMethod.Invoke(xmlSerializationReader, Array.Empty<object>());
		}
		catch (SecurityException innerException)
		{
			throw new InvalidOperationException(System.SR.XmlNoPartialTrust, innerException);
		}
		finally
		{
			xmlSerializationReader?.Dispose();
		}
	}

	[RequiresUnreferencedCode("calls Contract")]
	internal void InvokeWriter(XmlMapping mapping, XmlWriter xmlWriter, object o, XmlSerializerNamespaces namespaces, string encodingStyle, string id)
	{
		XmlSerializationWriter xmlSerializationWriter = null;
		try
		{
			encodingStyle = ValidateEncodingStyle(encodingStyle, mapping.Key);
			xmlSerializationWriter = Contract.Writer;
			xmlSerializationWriter.Init(xmlWriter, namespaces, encodingStyle, id, this);
			if (_methods[mapping.Key].writeMethod == null)
			{
				if (_writerMethods == null)
				{
					_writerMethods = Contract.WriteMethods;
				}
				string text = (string)_writerMethods[mapping.Key];
				if (text == null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlNotSerializable, mapping.Accessor.Name));
				}
				_methods[mapping.Key].writeMethod = GetMethodFromType(xmlSerializationWriter.GetType(), text);
			}
			_methods[mapping.Key].writeMethod.Invoke(xmlSerializationWriter, new object[1] { o });
		}
		catch (SecurityException innerException)
		{
			throw new InvalidOperationException(System.SR.XmlNoPartialTrust, innerException);
		}
		finally
		{
			xmlSerializationWriter?.Dispose();
		}
	}
}
