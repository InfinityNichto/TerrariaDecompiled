using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices;

public static class ComponentActivator
{
	public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);

	private static readonly Dictionary<string, IsolatedComponentLoadContext> s_assemblyLoadContexts = new Dictionary<string, IsolatedComponentLoadContext>(StringComparer.InvariantCulture);

	private static readonly Dictionary<IntPtr, Delegate> s_delegates = new Dictionary<IntPtr, Delegate>();

	private static bool IsSupported { get; } = InitializeIsSupported();


	private static bool InitializeIsSupported()
	{
		if (!AppContext.TryGetSwitch("System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting", out var isEnabled))
		{
			return true;
		}
		return isEnabled;
	}

	private static string MarshalToString(IntPtr arg, string argName)
	{
		string text = Marshal.PtrToStringAuto(arg);
		if (text == null)
		{
			throw new ArgumentNullException(argName);
		}
		return text;
	}

	[RequiresUnreferencedCode("Native hosting is not trim compatible and this warning will be seen if trimming is enabled.", Url = "https://aka.ms/dotnet-illink/nativehost")]
	[UnmanagedCallersOnly]
	public unsafe static int LoadAssemblyAndGetFunctionPointer(IntPtr assemblyPathNative, IntPtr typeNameNative, IntPtr methodNameNative, IntPtr delegateTypeNative, IntPtr reserved, IntPtr functionHandle)
	{
		if (!IsSupported)
		{
			return -2147450713;
		}
		try
		{
			string assemblyPath = MarshalToString(assemblyPathNative, "assemblyPathNative");
			string typeName = MarshalToString(typeNameNative, "typeNameNative");
			string methodName = MarshalToString(methodNameNative, "methodNameNative");
			if (reserved != IntPtr.Zero)
			{
				throw new ArgumentOutOfRangeException("reserved");
			}
			if (functionHandle == IntPtr.Zero)
			{
				throw new ArgumentNullException("functionHandle");
			}
			AssemblyLoadContext isolatedComponentLoadContext = GetIsolatedComponentLoadContext(assemblyPath);
			*(IntPtr*)(void*)functionHandle = InternalGetFunctionPointer(isolatedComponentLoadContext, typeName, methodName, delegateTypeNative);
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	[UnmanagedCallersOnly]
	public unsafe static int GetFunctionPointer(IntPtr typeNameNative, IntPtr methodNameNative, IntPtr delegateTypeNative, IntPtr loadContext, IntPtr reserved, IntPtr functionHandle)
	{
		if (!IsSupported)
		{
			return -2147450713;
		}
		try
		{
			string typeName = MarshalToString(typeNameNative, "typeNameNative");
			string methodName = MarshalToString(methodNameNative, "methodNameNative");
			if (loadContext != IntPtr.Zero)
			{
				throw new ArgumentOutOfRangeException("loadContext");
			}
			if (reserved != IntPtr.Zero)
			{
				throw new ArgumentOutOfRangeException("reserved");
			}
			if (functionHandle == IntPtr.Zero)
			{
				throw new ArgumentNullException("functionHandle");
			}
			*(IntPtr*)(void*)functionHandle = InternalGetFunctionPointer(AssemblyLoadContext.Default, typeName, methodName, delegateTypeNative);
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	[RequiresUnreferencedCode("Native hosting is not trim compatible and this warning will be seen if trimming is enabled.", Url = "https://aka.ms/dotnet-illink/nativehost")]
	private static IsolatedComponentLoadContext GetIsolatedComponentLoadContext(string assemblyPath)
	{
		IsolatedComponentLoadContext value;
		lock (s_assemblyLoadContexts)
		{
			if (!s_assemblyLoadContexts.TryGetValue(assemblyPath, out value))
			{
				value = new IsolatedComponentLoadContext(assemblyPath);
				s_assemblyLoadContexts.Add(assemblyPath, value);
			}
		}
		return value;
	}

	[RequiresUnreferencedCode("Native hosting is not trim compatible and this warning will be seen if trimming is enabled.", Url = "https://aka.ms/dotnet-illink/nativehost")]
	private static IntPtr InternalGetFunctionPointer(AssemblyLoadContext alc, string typeName, string methodName, IntPtr delegateTypeNative)
	{
		Func<AssemblyName, Assembly> assemblyResolver = (AssemblyName name) => alc.LoadFromAssemblyName(name);
		Type type;
		if (delegateTypeNative == IntPtr.Zero)
		{
			type = typeof(ComponentEntryPoint);
		}
		else if (delegateTypeNative == (IntPtr)(-1))
		{
			type = null;
		}
		else
		{
			string typeName2 = MarshalToString(delegateTypeNative, "delegateTypeNative");
			type = Type.GetType(typeName2, assemblyResolver, null, throwOnError: true);
		}
		Type type2 = Type.GetType(typeName, assemblyResolver, null, throwOnError: true);
		IntPtr intPtr;
		if (type == null)
		{
			BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo method = type2.GetMethod(methodName, bindingAttr);
			if (method == null)
			{
				throw new MissingMethodException(typeName, methodName);
			}
			if (method.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() == null)
			{
				throw new InvalidOperationException(SR.InvalidOperation_FunctionMissingUnmanagedCallersOnly);
			}
			intPtr = method.MethodHandle.GetFunctionPointer();
		}
		else
		{
			Delegate @delegate = Delegate.CreateDelegate(type, type2, methodName);
			intPtr = Marshal.GetFunctionPointerForDelegate(@delegate);
			lock (s_delegates)
			{
				s_delegates[intPtr] = @delegate;
			}
		}
		return intPtr;
	}
}
