using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.XPath;
using System.Xml.Xsl.IlGen;
using System.Xml.Xsl.Qil;
using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl;

internal sealed class XmlILGenerator
{
	private QilExpression _qil;

	private GenerateHelper _helper;

	private XmlILOptimizerVisitor _optVisitor;

	private XmlILVisitor _xmlIlVisitor;

	private XmlILModule _module;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This method will generate the IL methods using RefEmit at runtime, which will then try to call them using methods that are annotated as RequiresUnreferencedCode. In this case, these uses can be suppressed as the trimmer won't be able to trim any IL that gets generated at runtime.")]
	public XmlILCommand Generate(QilExpression query, TypeBuilder typeBldr)
	{
		_qil = query;
		bool useLRE = !_qil.IsDebug && typeBldr == null;
		bool isDebug = _qil.IsDebug;
		_optVisitor = new XmlILOptimizerVisitor(_qil, !_qil.IsDebug);
		_qil = _optVisitor.Optimize();
		if (typeBldr != null)
		{
			_module = new XmlILModule(typeBldr);
		}
		else
		{
			_module = new XmlILModule(useLRE, isDebug);
		}
		_helper = new GenerateHelper(_module, _qil.IsDebug);
		CreateHelperFunctions();
		MethodInfo methExec = _module.DefineMethod("Execute", typeof(void), Type.EmptyTypes, Array.Empty<string>(), XmlILMethodAttributes.NonUser);
		XmlILMethodAttributes xmlAttrs = ((_qil.Root.SourceLine == null) ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None);
		MethodInfo methRoot = _module.DefineMethod("Root", typeof(void), Type.EmptyTypes, Array.Empty<string>(), xmlAttrs);
		foreach (EarlyBoundInfo earlyBoundType in _qil.EarlyBoundTypes)
		{
			_helper.StaticData.DeclareEarlyBound(earlyBoundType.NamespaceUri, earlyBoundType.EarlyBoundType);
		}
		CreateFunctionMetadata(_qil.FunctionList);
		CreateGlobalValueMetadata(_qil.GlobalVariableList);
		CreateGlobalValueMetadata(_qil.GlobalParameterList);
		GenerateExecuteFunction(methExec, methRoot);
		_xmlIlVisitor = new XmlILVisitor();
		_xmlIlVisitor.Visit(_qil, _helper, methRoot);
		XmlQueryStaticData staticData = new XmlQueryStaticData(_qil.DefaultWriterSettings, _qil.WhitespaceRules, _helper.StaticData);
		if (typeBldr != null)
		{
			CreateTypeInitializer(staticData);
			_module.BakeMethods();
			return null;
		}
		_module.BakeMethods();
		ExecuteDelegate delExec = (ExecuteDelegate)_module.CreateDelegate("Execute", typeof(ExecuteDelegate));
		return new XmlILCommand(delExec, staticData);
	}

	private void CreateFunctionMetadata(IList<QilNode> funcList)
	{
		foreach (QilFunction func in funcList)
		{
			Type[] array = new Type[func.Arguments.Count];
			string[] array2 = new string[func.Arguments.Count];
			for (int i = 0; i < func.Arguments.Count; i++)
			{
				QilParameter qilParameter = (QilParameter)func.Arguments[i];
				array[i] = XmlILTypeHelper.GetStorageType(qilParameter.XmlType);
				if (qilParameter.DebugName != null)
				{
					array2[i] = qilParameter.DebugName;
				}
			}
			Type returnType = ((!XmlILConstructInfo.Read(func).PushToWriterLast) ? XmlILTypeHelper.GetStorageType(func.XmlType) : typeof(void));
			XmlILMethodAttributes xmlAttrs = ((func.SourceLine == null) ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None);
			MethodInfo functionBinding = _module.DefineMethod(func.DebugName, returnType, array, array2, xmlAttrs);
			for (int j = 0; j < func.Arguments.Count; j++)
			{
				XmlILAnnotation.Write(func.Arguments[j]).ArgumentPosition = j;
			}
			XmlILAnnotation.Write(func).FunctionBinding = functionBinding;
		}
	}

	private void CreateGlobalValueMetadata(IList<QilNode> globalList)
	{
		foreach (QilReference global in globalList)
		{
			Type storageType = XmlILTypeHelper.GetStorageType(global.XmlType);
			XmlILMethodAttributes xmlAttrs = ((global.SourceLine == null) ? XmlILMethodAttributes.NonUser : XmlILMethodAttributes.None);
			MethodInfo functionBinding = _module.DefineMethod(global.DebugName.ToString(), storageType, Type.EmptyTypes, Array.Empty<string>(), xmlAttrs);
			XmlILAnnotation.Write(global).FunctionBinding = functionBinding;
		}
	}

	private MethodInfo GenerateExecuteFunction(MethodInfo methExec, MethodInfo methRoot)
	{
		_helper.MethodBegin(methExec, null, initWriters: false);
		EvaluateGlobalValues(_qil.GlobalVariableList);
		EvaluateGlobalValues(_qil.GlobalParameterList);
		_helper.LoadQueryRuntime();
		_helper.Call(methRoot);
		_helper.MethodEnd();
		return methExec;
	}

	private void CreateHelperFunctions()
	{
		MethodInfo methInfo = _module.DefineMethod("SyncToNavigator", typeof(XPathNavigator), new Type[2]
		{
			typeof(XPathNavigator),
			typeof(XPathNavigator)
		}, new string[2], (XmlILMethodAttributes)3);
		_helper.MethodBegin(methInfo, null, initWriters: false);
		Label label = _helper.DefineLabel();
		_helper.Emit(OpCodes.Ldarg_0);
		_helper.Emit(OpCodes.Brfalse, label);
		_helper.Emit(OpCodes.Ldarg_0);
		_helper.Emit(OpCodes.Ldarg_1);
		_helper.Call(XmlILMethods.NavMoveTo);
		_helper.Emit(OpCodes.Brfalse, label);
		_helper.Emit(OpCodes.Ldarg_0);
		_helper.Emit(OpCodes.Ret);
		_helper.MarkLabel(label);
		_helper.Emit(OpCodes.Ldarg_1);
		_helper.Call(XmlILMethods.NavClone);
		_helper.MethodEnd();
	}

	private void EvaluateGlobalValues(IList<QilNode> iterList)
	{
		foreach (QilIterator iter in iterList)
		{
			if (_qil.IsDebug || OptimizerPatterns.Read(iter).MatchesPattern(OptimizerPatternName.MaybeSideEffects))
			{
				MethodInfo functionBinding = XmlILAnnotation.Write(iter).FunctionBinding;
				_helper.LoadQueryRuntime();
				_helper.Call(functionBinding);
				_helper.Emit(OpCodes.Pop);
			}
		}
	}

	public void CreateTypeInitializer(XmlQueryStaticData staticData)
	{
		staticData.GetObjectData(out var data, out var ebTypes);
		FieldInfo fldInfo = _module.DefineInitializedData("__staticData", data);
		FieldInfo fldInfo2 = _module.DefineField("staticData", typeof(object));
		FieldInfo fldInfo3 = _module.DefineField("ebTypes", typeof(Type[]));
		ConstructorInfo methInfo = _module.DefineTypeInitializer();
		_helper.MethodBegin(methInfo, null, initWriters: false);
		_helper.LoadInteger(data.Length);
		_helper.Emit(OpCodes.Newarr, typeof(byte));
		_helper.Emit(OpCodes.Dup);
		_helper.Emit(OpCodes.Ldtoken, fldInfo);
		_helper.Call(XmlILMethods.InitializeArray);
		_helper.Emit(OpCodes.Stsfld, fldInfo2);
		if (ebTypes != null)
		{
			LocalBuilder locBldr = _helper.DeclareLocal("$$$types", typeof(Type[]));
			_helper.LoadInteger(ebTypes.Length);
			_helper.Emit(OpCodes.Newarr, typeof(Type));
			_helper.Emit(OpCodes.Stloc, locBldr);
			for (int i = 0; i < ebTypes.Length; i++)
			{
				_helper.Emit(OpCodes.Ldloc, locBldr);
				_helper.LoadInteger(i);
				_helper.LoadType(ebTypes[i]);
				_helper.Emit(OpCodes.Stelem_Ref);
			}
			_helper.Emit(OpCodes.Ldloc, locBldr);
			_helper.Emit(OpCodes.Stsfld, fldInfo3);
		}
		_helper.MethodEnd();
	}
}
