using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Threading;

namespace System;

public static class Activator
{
	[DebuggerHidden]
	[DebuggerStepThrough]
	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture)
	{
		return CreateInstance(type, bindingAttr, binder, args, culture, null);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args)
	{
		return CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, args, null, null);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, object?[]? args, object?[]? activationAttributes)
	{
		return CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, args, null, activationAttributes);
	}

	[DebuggerHidden]
	[DebuggerStepThrough]
	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
	{
		return CreateInstance(type, nonPublic: false);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName)
	{
		return CreateInstanceFrom(assemblyFile, typeName, ignoreCase: false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null, null);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, object?[]? activationAttributes)
	{
		return CreateInstanceFrom(assemblyFile, typeName, ignoreCase: false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null, activationAttributes);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		Assembly assembly = Assembly.LoadFrom(assemblyFile);
		Type type = assembly.GetType(typeName, throwOnError: true, ignoreCase);
		object obj = CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
		if (obj == null)
		{
			return null;
		}
		return new ObjectHandle(obj);
	}

	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (type is TypeBuilder)
		{
			throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);
		}
		if ((bindingAttr & (BindingFlags)255) == 0)
		{
			bindingAttr |= BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
		}
		if (activationAttributes != null && activationAttributes.Length != 0)
		{
			throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);
		}
		if (type.UnderlyingSystemType is RuntimeType runtimeType)
		{
			return runtimeType.CreateInstanceImpl(bindingAttr, binder, args, culture);
		}
		throw new ArgumentException(SR.Arg_MustBeType, "type");
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstance(string assemblyName, string typeName)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return CreateInstanceInternal(assemblyName, typeName, ignoreCase: false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null, null, ref stackMark);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return CreateInstanceInternal(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, ref stackMark);
	}

	[RequiresUnreferencedCode("Type and its constructor could be removed")]
	public static ObjectHandle? CreateInstance(string assemblyName, string typeName, object?[]? activationAttributes)
	{
		StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
		return CreateInstanceInternal(assemblyName, typeName, ignoreCase: false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null, null, null, activationAttributes, ref stackMark);
	}

	public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type, bool nonPublic)
	{
		return CreateInstance(type, nonPublic, wrapExceptions: true);
	}

	internal static object CreateInstance(Type type, bool nonPublic, bool wrapExceptions)
	{
		if ((object)type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (!(type.UnderlyingSystemType is RuntimeType runtimeType))
		{
			throw new ArgumentException(SR.Arg_MustBeType, "type");
		}
		return runtimeType.CreateInstanceDefaultCtor(!nonPublic, wrapExceptions);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2096:UnrecognizedReflectionPattern", Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
	private static ObjectHandle CreateInstanceInternal(string assemblyString, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, ref StackCrawlMark stackMark)
	{
		Assembly assembly;
		if (assemblyString == null)
		{
			assembly = Assembly.GetExecutingAssembly(ref stackMark);
		}
		else
		{
			AssemblyName assemblyName = new AssemblyName(assemblyString);
			assembly = RuntimeAssembly.InternalLoad(assemblyName, ref stackMark, AssemblyLoadContext.CurrentContextualReflectionContext);
		}
		Type type = assembly.GetType(typeName, throwOnError: true, ignoreCase);
		object obj = CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
		if (obj == null)
		{
			return null;
		}
		return new ObjectHandle(obj);
	}

	[Intrinsic]
	public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
	{
		return (T)((RuntimeType)typeof(T)).CreateInstanceOfT();
	}

	private static T CreateDefaultInstance<T>() where T : struct
	{
		return default(T);
	}
}
