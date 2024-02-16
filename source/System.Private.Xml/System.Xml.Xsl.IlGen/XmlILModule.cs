using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl.IlGen;

internal sealed class XmlILModule
{
	private static long s_assemblyId;

	private static readonly ModuleBuilder s_LREModule = CreateLREModule();

	private TypeBuilder _typeBldr;

	private Hashtable _methods;

	private readonly bool _useLRE;

	private readonly bool _emitSymbols;

	public bool EmitSymbols => _emitSymbols;

	private static ModuleBuilder CreateLREModule()
	{
		AssemblyName name = CreateAssemblyName();
		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
		assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Transparent, Array.Empty<object>()));
		return assemblyBuilder.DefineDynamicModule("System.Xml.Xsl.CompiledQuery");
	}

	public XmlILModule(TypeBuilder typeBldr)
	{
		_typeBldr = typeBldr;
		_emitSymbols = false;
		_useLRE = false;
		_methods = new Hashtable();
	}

	public XmlILModule(bool useLRE, bool emitSymbols)
	{
		_useLRE = useLRE;
		_emitSymbols = emitSymbols;
		_methods = new Hashtable();
		if (!useLRE)
		{
			AssemblyName name = CreateAssemblyName();
			AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
			assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Transparent, Array.Empty<object>()));
			if (emitSymbols)
			{
				DebuggableAttribute.DebuggingModes debuggingModes = DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints;
				assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.Debuggable, new object[1] { debuggingModes }));
			}
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("System.Xml.Xsl.CompiledQuery");
			_typeBldr = moduleBuilder.DefineType("System.Xml.Xsl.CompiledQuery.Query", TypeAttributes.Public);
		}
	}

	public MethodInfo DefineMethod(string name, Type returnType, Type[] paramTypes, string[] paramNames, XmlILMethodAttributes xmlAttrs)
	{
		int num = 1;
		string text = name;
		bool flag = (xmlAttrs & XmlILMethodAttributes.Raw) != 0;
		while (_methods[name] != null)
		{
			num++;
			name = text + " (" + num + ")";
		}
		if (!flag)
		{
			Type[] array = new Type[paramTypes.Length + 1];
			array[0] = typeof(XmlQueryRuntime);
			Array.Copy(paramTypes, 0, array, 1, paramTypes.Length);
			paramTypes = array;
		}
		MethodInfo methodInfo;
		if (!_useLRE)
		{
			MethodBuilder methodBuilder = _typeBldr.DefineMethod(name, MethodAttributes.Private | MethodAttributes.Static, returnType, paramTypes);
			if (_emitSymbols && (xmlAttrs & XmlILMethodAttributes.NonUser) != 0)
			{
				methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.StepThrough, Array.Empty<object>()));
				methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(XmlILConstructors.NonUserCode, Array.Empty<object>()));
			}
			if (!flag)
			{
				methodBuilder.DefineParameter(1, ParameterAttributes.None, "{urn:schemas-microsoft-com:xslt-debug}runtime");
			}
			for (int i = 0; i < paramNames.Length; i++)
			{
				if (paramNames[i] != null && paramNames[i].Length != 0)
				{
					methodBuilder.DefineParameter(i + (flag ? 1 : 2), ParameterAttributes.None, paramNames[i]);
				}
			}
			methodInfo = methodBuilder;
		}
		else
		{
			DynamicMethod dynamicMethod = new DynamicMethod(name, returnType, paramTypes, s_LREModule);
			dynamicMethod.InitLocals = true;
			methodInfo = dynamicMethod;
		}
		_methods[name] = methodInfo;
		return methodInfo;
	}

	public static ILGenerator DefineMethodBody(MethodBase methInfo)
	{
		DynamicMethod dynamicMethod = methInfo as DynamicMethod;
		if (dynamicMethod != null)
		{
			return dynamicMethod.GetILGenerator();
		}
		MethodBuilder methodBuilder = methInfo as MethodBuilder;
		if (methodBuilder != null)
		{
			return methodBuilder.GetILGenerator();
		}
		return ((ConstructorBuilder)methInfo).GetILGenerator();
	}

	public MethodInfo FindMethod(string name)
	{
		return (MethodInfo)_methods[name];
	}

	public FieldInfo DefineInitializedData(string name, byte[] data)
	{
		return _typeBldr.DefineInitializedData(name, data, FieldAttributes.Private | FieldAttributes.Static);
	}

	public FieldInfo DefineField(string fieldName, Type type)
	{
		return _typeBldr.DefineField(fieldName, type, FieldAttributes.Private | FieldAttributes.Static);
	}

	public ConstructorInfo DefineTypeInitializer()
	{
		return _typeBldr.DefineTypeInitializer();
	}

	public void BakeMethods()
	{
		if (_useLRE)
		{
			return;
		}
		Type type = _typeBldr.CreateTypeInfo().AsType();
		Hashtable hashtable = new Hashtable(_methods.Count);
		foreach (string key in _methods.Keys)
		{
			hashtable[key] = type.GetMethod(key, BindingFlags.Static | BindingFlags.NonPublic);
		}
		_methods = hashtable;
		_typeBldr = null;
	}

	public Delegate CreateDelegate(string name, Type typDelegate)
	{
		if (!_useLRE)
		{
			return ((MethodInfo)_methods[name]).CreateDelegate(typDelegate);
		}
		return ((DynamicMethod)_methods[name]).CreateDelegate(typDelegate);
	}

	private static AssemblyName CreateAssemblyName()
	{
		Interlocked.Increment(ref s_assemblyId);
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.Name = "System.Xml.Xsl.CompiledQuery." + s_assemblyId;
		return assemblyName;
	}
}
