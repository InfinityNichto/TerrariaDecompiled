using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

namespace Internal.Runtime.InteropServices;

[SupportedOSPlatform("windows")]
public static class ComActivator
{
	[ComVisible(true)]
	private sealed class BasicClassFactory : IClassFactory
	{
		private readonly Guid _classId;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		private readonly Type _classType;

		public BasicClassFactory(Guid clsid, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type classType)
		{
			_classId = clsid;
			_classType = classType;
		}

		public static Type GetValidatedInterfaceType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type classType, ref Guid riid, object outer)
		{
			if (riid == Marshal.IID_IUnknown)
			{
				return typeof(object);
			}
			if (outer != null)
			{
				throw new COMException(string.Empty, -2147221232);
			}
			Type[] interfaces = classType.GetInterfaces();
			foreach (Type type in interfaces)
			{
				if (type.GUID == riid)
				{
					return type;
				}
			}
			throw new InvalidCastException();
		}

		public static IntPtr GetObjectAsInterface(object obj, Type interfaceType)
		{
			if (interfaceType == typeof(object))
			{
				return Marshal.GetIUnknownForObject(obj);
			}
			IntPtr comInterfaceForObject = Marshal.GetComInterfaceForObject(obj, interfaceType, CustomQueryInterfaceMode.Ignore);
			if (comInterfaceForObject == IntPtr.Zero)
			{
				throw new InvalidCastException();
			}
			return comInterfaceForObject;
		}

		public static object CreateAggregatedObject(object pUnkOuter, object comObject)
		{
			IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(pUnkOuter);
			try
			{
				IntPtr pUnk = Marshal.CreateAggregatedObject(iUnknownForObject, comObject);
				return Marshal.GetObjectForIUnknown(pUnk);
			}
			finally
			{
				Marshal.Release(iUnknownForObject);
			}
		}

		[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
		public void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid riid, out IntPtr ppvObject)
		{
			Type validatedInterfaceType = GetValidatedInterfaceType(_classType, ref riid, pUnkOuter);
			object obj = Activator.CreateInstance(_classType);
			if (pUnkOuter != null)
			{
				obj = CreateAggregatedObject(pUnkOuter, obj);
			}
			ppvObject = GetObjectAsInterface(obj, validatedInterfaceType);
		}

		public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
		{
		}
	}

	[ComVisible(true)]
	private sealed class LicenseClassFactory : IClassFactory2, IClassFactory
	{
		private readonly LicenseInteropProxy _licenseProxy = new LicenseInteropProxy();

		private readonly Guid _classId;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
		private readonly Type _classType;

		public LicenseClassFactory(Guid clsid, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type classType)
		{
			_classId = clsid;
			_classType = classType;
		}

		[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
		public void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid riid, out IntPtr ppvObject)
		{
			CreateInstanceInner(pUnkOuter, ref riid, null, isDesignTime: true, out ppvObject);
		}

		public void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock)
		{
		}

		public void GetLicInfo(ref LICINFO licInfo)
		{
			_licenseProxy.GetLicInfo(_classType, out var runtimeKeyAvail, out var licVerified);
			licInfo.cbLicInfo = 12;
			licInfo.fRuntimeKeyAvail = runtimeKeyAvail;
			licInfo.fLicVerified = licVerified;
		}

		public void RequestLicKey(int dwReserved, [MarshalAs(UnmanagedType.BStr)] out string pBstrKey)
		{
			pBstrKey = _licenseProxy.RequestLicKey(_classType);
		}

		public void CreateInstanceLic([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, [MarshalAs(UnmanagedType.Interface)] object pUnkReserved, ref Guid riid, [MarshalAs(UnmanagedType.BStr)] string bstrKey, out IntPtr ppvObject)
		{
			CreateInstanceInner(pUnkOuter, ref riid, bstrKey, isDesignTime: false, out ppvObject);
		}

		private void CreateInstanceInner(object pUnkOuter, ref Guid riid, string key, bool isDesignTime, out IntPtr ppvObject)
		{
			Type validatedInterfaceType = BasicClassFactory.GetValidatedInterfaceType(_classType, ref riid, pUnkOuter);
			object obj = _licenseProxy.AllocateAndValidateLicense(_classType, key, isDesignTime);
			if (pUnkOuter != null)
			{
				obj = BasicClassFactory.CreateAggregatedObject(pUnkOuter, obj);
			}
			ppvObject = BasicClassFactory.GetObjectAsInterface(obj, validatedInterfaceType);
		}
	}

	private static readonly Dictionary<string, AssemblyLoadContext> s_assemblyLoadContexts = new Dictionary<string, AssemblyLoadContext>(StringComparer.InvariantCultureIgnoreCase);

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	public static object GetClassFactoryForType(ComActivationContext cxt)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		if (cxt.InterfaceId != typeof(IClassFactory).GUID && cxt.InterfaceId != typeof(IClassFactory2).GUID)
		{
			throw new NotSupportedException();
		}
		if (!Path.IsPathRooted(cxt.AssemblyPath))
		{
			throw new ArgumentException(null, "cxt");
		}
		Type type = FindClassType(cxt.ClassId, cxt.AssemblyPath, cxt.AssemblyName, cxt.TypeName);
		if (LicenseInteropProxy.HasLicense(type))
		{
			return new LicenseClassFactory(cxt.ClassId, type);
		}
		return new BasicClassFactory(cxt.ClassId, type);
	}

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	public static void ClassRegistrationScenarioForType(ComActivationContext cxt, bool register)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		string text = (register ? "ComRegisterFunctionAttribute" : "ComUnregisterFunctionAttribute");
		Type type = Type.GetType("System.Runtime.InteropServices." + text + ", System.Runtime.InteropServices", throwOnError: false);
		if (type == null)
		{
			return;
		}
		if (!Path.IsPathRooted(cxt.AssemblyPath))
		{
			throw new ArgumentException(null, "cxt");
		}
		Type type2 = FindClassType(cxt.ClassId, cxt.AssemblyPath, cxt.AssemblyName, cxt.TypeName);
		Type type3 = type2;
		bool flag = false;
		while (type3 != null && !flag)
		{
			MethodInfo[] methods = type3.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (methodInfo.GetCustomAttributes(type, inherit: true).Length != 0)
				{
					if (!methodInfo.IsStatic)
					{
						string resourceFormat = (register ? SR.InvalidOperation_NonStaticComRegFunction : SR.InvalidOperation_NonStaticComUnRegFunction);
						throw new InvalidOperationException(SR.Format(resourceFormat));
					}
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (methodInfo.ReturnType != typeof(void) || parameters == null || parameters.Length != 1 || (parameters[0].ParameterType != typeof(string) && parameters[0].ParameterType != typeof(Type)))
					{
						string resourceFormat2 = (register ? SR.InvalidOperation_InvalidComRegFunctionSig : SR.InvalidOperation_InvalidComUnRegFunctionSig);
						throw new InvalidOperationException(SR.Format(resourceFormat2));
					}
					if (flag)
					{
						string resourceFormat3 = (register ? SR.InvalidOperation_MultipleComRegFunctions : SR.InvalidOperation_MultipleComUnRegFunctions);
						throw new InvalidOperationException(SR.Format(resourceFormat3));
					}
					object[] array2 = new object[1];
					if (parameters[0].ParameterType == typeof(string))
					{
						array2[0] = $"HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\CLSID\\{cxt.ClassId:B}";
					}
					else
					{
						array2[0] = type2;
					}
					methodInfo.Invoke(null, array2);
					flag = true;
				}
			}
			type3 = type3.BaseType;
		}
	}

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[CLSCompliant(false)]
	[UnmanagedCallersOnly]
	public unsafe static int GetClassFactoryForTypeInternal(ComActivationContextInternal* pCxtInt)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ref ComActivationContextInternal reference = ref *pCxtInt;
		if (IsLoggingEnabled())
		{
		}
		try
		{
			ComActivationContext cxt = ComActivationContext.Create(ref reference);
			object classFactoryForType = GetClassFactoryForType(cxt);
			IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(classFactoryForType);
			Marshal.WriteIntPtr(reference.ClassFactoryDest, iUnknownForObject);
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[CLSCompliant(false)]
	[UnmanagedCallersOnly]
	public unsafe static int RegisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ref ComActivationContextInternal reference = ref *pCxtInt;
		if (IsLoggingEnabled())
		{
		}
		if (reference.InterfaceId != Guid.Empty || reference.ClassFactoryDest != IntPtr.Zero)
		{
			throw new ArgumentException(null, "pCxtInt");
		}
		try
		{
			ComActivationContext cxt = ComActivationContext.Create(ref reference);
			ClassRegistrationScenarioForType(cxt, register: true);
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	[CLSCompliant(false)]
	[UnmanagedCallersOnly]
	public unsafe static int UnregisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
	{
		if (!Marshal.IsBuiltInComSupported)
		{
			throw new NotSupportedException(SR.NotSupported_COM);
		}
		ref ComActivationContextInternal reference = ref *pCxtInt;
		if (IsLoggingEnabled())
		{
		}
		if (reference.InterfaceId != Guid.Empty || reference.ClassFactoryDest != IntPtr.Zero)
		{
			throw new ArgumentException(null, "pCxtInt");
		}
		try
		{
			ComActivationContext cxt = ComActivationContext.Create(ref reference);
			ClassRegistrationScenarioForType(cxt, register: false);
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
		return 0;
	}

	private static bool IsLoggingEnabled()
	{
		return false;
	}

	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	private static Type FindClassType(Guid clsid, string assemblyPath, string assemblyName, string typeName)
	{
		try
		{
			AssemblyLoadContext aLC = GetALC(assemblyPath);
			AssemblyName assemblyName2 = new AssemblyName(assemblyName);
			Assembly assembly = aLC.LoadFromAssemblyName(assemblyName2);
			Type type = assembly.GetType(typeName);
			if (type != null)
			{
				return type;
			}
		}
		catch (Exception)
		{
			if (!IsLoggingEnabled())
			{
			}
		}
		throw new COMException(string.Empty, -2147221231);
	}

	[RequiresUnreferencedCode("The trimmer might remove types which are needed by the assemblies loaded in this method.")]
	private static AssemblyLoadContext GetALC(string assemblyPath)
	{
		AssemblyLoadContext value;
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
}
