using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices;

internal sealed class LicenseInteropProxy
{
	private static readonly Type s_licenseAttrType = Type.GetType("System.ComponentModel.LicenseProviderAttribute, System.ComponentModel.TypeConverter", throwOnError: false);

	private static readonly Type s_licenseExceptionType = Type.GetType("System.ComponentModel.LicenseException, System.ComponentModel.TypeConverter", throwOnError: false);

	private readonly MethodInfo _createWithContext;

	private readonly MethodInfo _validateTypeAndReturnDetails;

	private readonly MethodInfo _getCurrentContextInfo;

	private readonly MethodInfo _createDesignContext;

	private readonly MethodInfo _createRuntimeContext;

	private readonly MethodInfo _setSavedLicenseKey;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
	private readonly Type _licInfoHelper;

	private readonly MethodInfo _licInfoHelperContains;

	private object _licContext;

	private Type _targetRcwType;

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers", Justification = "The type parameter to LicenseManager.CreateWithContext method has PublicConstructors annotation. We only invoke this methodfrom AllocateAndValidateLicense which annotates the value passed in with the same annotation.")]
	public LicenseInteropProxy()
	{
		Type type = Type.GetType("System.ComponentModel.LicenseManager, System.ComponentModel.TypeConverter", throwOnError: true);
		Type type2 = Type.GetType("System.ComponentModel.LicenseContext, System.ComponentModel.TypeConverter", throwOnError: true);
		_setSavedLicenseKey = type2.GetMethod("SetSavedLicenseKey", BindingFlags.Instance | BindingFlags.Public);
		_createWithContext = type.GetMethod("CreateWithContext", new Type[2]
		{
			typeof(Type),
			type2
		});
		Type nestedType = type.GetNestedType("LicenseInteropHelper", BindingFlags.NonPublic);
		_validateTypeAndReturnDetails = nestedType.GetMethod("ValidateAndRetrieveLicenseDetails", BindingFlags.Static | BindingFlags.Public);
		_getCurrentContextInfo = nestedType.GetMethod("GetCurrentContextInfo", BindingFlags.Static | BindingFlags.Public);
		Type nestedType2 = type.GetNestedType("CLRLicenseContext", BindingFlags.NonPublic);
		_createDesignContext = nestedType2.GetMethod("CreateDesignContext", BindingFlags.Static | BindingFlags.Public);
		_createRuntimeContext = nestedType2.GetMethod("CreateRuntimeContext", BindingFlags.Static | BindingFlags.Public);
		_licInfoHelper = type.GetNestedType("LicInfoHelperLicenseContext", BindingFlags.NonPublic);
		_licInfoHelperContains = _licInfoHelper.GetMethod("Contains", BindingFlags.Instance | BindingFlags.Public);
	}

	public static object Create()
	{
		return new LicenseInteropProxy();
	}

	public static bool HasLicense(Type type)
	{
		if (s_licenseAttrType == null)
		{
			return false;
		}
		return type.IsDefined(s_licenseAttrType, inherit: true);
	}

	public void GetLicInfo(Type type, out bool runtimeKeyAvail, out bool licVerified)
	{
		runtimeKeyAvail = false;
		licVerified = false;
		object obj = Activator.CreateInstance(_licInfoHelper);
		object[] array = new object[4] { obj, type, null, null };
		if ((bool)_validateTypeAndReturnDetails.Invoke(null, BindingFlags.DoNotWrapExceptions, null, array, null))
		{
			IDisposable disposable = (IDisposable)array[2];
			if (disposable != null)
			{
				disposable.Dispose();
				licVerified = true;
			}
			array = new object[1] { type.AssemblyQualifiedName };
			runtimeKeyAvail = (bool)_licInfoHelperContains.Invoke(obj, BindingFlags.DoNotWrapExceptions, null, array, null);
		}
	}

	public string RequestLicKey(Type type)
	{
		object[] array = new object[4] { null, type, null, null };
		if (!(bool)_validateTypeAndReturnDetails.Invoke(null, BindingFlags.DoNotWrapExceptions, null, array, null))
		{
			throw new COMException();
		}
		((IDisposable)array[2])?.Dispose();
		string text = (string)array[3];
		if (text == null)
		{
			throw new COMException();
		}
		return text;
	}

	public object AllocateAndValidateLicense([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, string key, bool isDesignTime)
	{
		object obj;
		if (isDesignTime)
		{
			object[] parameters = new object[1] { type };
			obj = _createDesignContext.Invoke(null, BindingFlags.DoNotWrapExceptions, null, parameters, null);
		}
		else
		{
			object[] parameters = new object[2] { type, key };
			obj = _createRuntimeContext.Invoke(null, BindingFlags.DoNotWrapExceptions, null, parameters, null);
		}
		try
		{
			object[] parameters = new object[2] { type, obj };
			return _createWithContext.Invoke(null, BindingFlags.DoNotWrapExceptions, null, parameters, null);
		}
		catch (Exception ex) when (ex.GetType() == s_licenseExceptionType)
		{
			throw new COMException(ex.Message, -2147221230);
		}
	}

	public void GetCurrentContextInfo(RuntimeTypeHandle rth, out bool isDesignTime, out IntPtr bstrKey)
	{
		Type typeFromHandle = Type.GetTypeFromHandle(rth);
		object[] array = new object[3] { typeFromHandle, null, null };
		_licContext = _getCurrentContextInfo.Invoke(null, BindingFlags.DoNotWrapExceptions, null, array, null);
		_targetRcwType = typeFromHandle;
		isDesignTime = (bool)array[1];
		bstrKey = Marshal.StringToBSTR((string)array[2]);
	}

	public void SaveKeyInCurrentContext(IntPtr bstrKey)
	{
		if (!(bstrKey == IntPtr.Zero))
		{
			string text = Marshal.PtrToStringBSTR(bstrKey);
			object[] parameters = new object[2] { _targetRcwType, text };
			_setSavedLicenseKey.Invoke(_licContext, BindingFlags.DoNotWrapExceptions, null, parameters, null);
		}
	}
}
