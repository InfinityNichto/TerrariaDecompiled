using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;

namespace System.ComponentModel;

public sealed class LicenseManager
{
	private sealed class CLRLicenseContext : LicenseContext
	{
		private readonly Type _type;

		private string _key;

		public override LicenseUsageMode UsageMode { get; }

		private CLRLicenseContext(Type type, LicenseUsageMode mode)
		{
			UsageMode = mode;
			_type = type;
		}

		public static CLRLicenseContext CreateDesignContext(Type type)
		{
			return new CLRLicenseContext(type, LicenseUsageMode.Designtime);
		}

		public static CLRLicenseContext CreateRuntimeContext(Type type, string key)
		{
			CLRLicenseContext cLRLicenseContext = new CLRLicenseContext(type, LicenseUsageMode.Runtime);
			if (key != null)
			{
				cLRLicenseContext.SetSavedLicenseKey(type, key);
			}
			return cLRLicenseContext;
		}

		public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
		{
			if (type == _type)
			{
				return _key;
			}
			return null;
		}

		public override void SetSavedLicenseKey(Type type, string key)
		{
			if (type == _type)
			{
				_key = key;
			}
		}
	}

	private sealed class LicInfoHelperLicenseContext : LicenseContext
	{
		private readonly Hashtable _savedLicenseKeys = new Hashtable();

		public override LicenseUsageMode UsageMode => LicenseUsageMode.Designtime;

		public bool Contains(string assemblyName)
		{
			return _savedLicenseKeys.Contains(assemblyName);
		}

		public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
		{
			return null;
		}

		public override void SetSavedLicenseKey(Type type, string key)
		{
			_savedLicenseKeys[type.AssemblyQualifiedName] = key;
		}
	}

	private static class LicenseInteropHelper
	{
		public static bool ValidateAndRetrieveLicenseDetails(LicenseContext context, Type type, out License license, out string licenseKey)
		{
			if (context == null)
			{
				context = CurrentContext;
			}
			return ValidateInternalRecursive(context, type, null, allowExceptions: false, out license, out licenseKey);
		}

		public static LicenseContext GetCurrentContextInfo(Type type, out bool isDesignTime, out string key)
		{
			LicenseContext currentContext = CurrentContext;
			isDesignTime = currentContext.UsageMode == LicenseUsageMode.Designtime;
			key = null;
			if (!isDesignTime)
			{
				key = currentContext.GetSavedLicenseKey(type, null);
			}
			return currentContext;
		}
	}

	private static readonly object s_selfLock = new object();

	private static volatile LicenseContext s_context;

	private static object s_contextLockHolder;

	private static volatile Hashtable s_providers;

	private static volatile Hashtable s_providerInstances;

	private static readonly object s_internalSyncObject = new object();

	public static LicenseContext CurrentContext
	{
		get
		{
			if (s_context == null)
			{
				lock (s_internalSyncObject)
				{
					if (s_context == null)
					{
						s_context = new RuntimeLicenseContext();
					}
				}
			}
			return s_context;
		}
		set
		{
			lock (s_internalSyncObject)
			{
				if (s_contextLockHolder != null)
				{
					throw new InvalidOperationException(System.SR.LicMgrContextCannotBeChanged);
				}
				s_context = value;
			}
		}
	}

	public static LicenseUsageMode UsageMode
	{
		get
		{
			if (s_context != null)
			{
				return s_context.UsageMode;
			}
			return LicenseUsageMode.Runtime;
		}
	}

	private static void CacheProvider(Type type, LicenseProvider provider)
	{
		if (s_providers == null)
		{
			Interlocked.CompareExchange(ref s_providers, new Hashtable(), null);
		}
		lock (s_providers)
		{
			s_providers[type] = provider;
		}
		if (provider != null)
		{
			if (s_providerInstances == null)
			{
				Interlocked.CompareExchange(ref s_providerInstances, new Hashtable(), null);
			}
			Type type2 = provider.GetType();
			lock (s_providerInstances)
			{
				s_providerInstances[type2] = provider;
			}
		}
	}

	[UnsupportedOSPlatform("browser")]
	public static object? CreateWithContext([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, LicenseContext creationContext)
	{
		return CreateWithContext(type, creationContext, Array.Empty<object>());
	}

	[UnsupportedOSPlatform("browser")]
	public static object? CreateWithContext([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, LicenseContext creationContext, object[] args)
	{
		object obj = null;
		lock (s_internalSyncObject)
		{
			LicenseContext currentContext = CurrentContext;
			try
			{
				CurrentContext = creationContext;
				LockContext(s_selfLock);
				try
				{
					return Activator.CreateInstance(type, args);
				}
				catch (TargetInvocationException ex)
				{
					throw ex.InnerException;
				}
			}
			finally
			{
				UnlockContext(s_selfLock);
				CurrentContext = currentContext;
			}
		}
	}

	private static bool GetCachedNoLicenseProvider(Type type)
	{
		if (s_providers != null)
		{
			return s_providers.ContainsKey(type);
		}
		return false;
	}

	private static LicenseProvider GetCachedProvider(Type type)
	{
		return (LicenseProvider)(s_providers?[type]);
	}

	private static LicenseProvider GetCachedProviderInstance(Type providerType)
	{
		return (LicenseProvider)(s_providerInstances?[providerType]);
	}

	public static bool IsLicensed(Type type)
	{
		License license;
		bool result = ValidateInternal(type, null, allowExceptions: false, out license);
		if (license != null)
		{
			license.Dispose();
			license = null;
		}
		return result;
	}

	public static bool IsValid(Type type)
	{
		License license;
		bool result = ValidateInternal(type, null, allowExceptions: false, out license);
		if (license != null)
		{
			license.Dispose();
			license = null;
		}
		return result;
	}

	public static bool IsValid(Type type, object? instance, out License? license)
	{
		return ValidateInternal(type, instance, allowExceptions: false, out license);
	}

	public static void LockContext(object contextUser)
	{
		lock (s_internalSyncObject)
		{
			if (s_contextLockHolder != null)
			{
				throw new InvalidOperationException(System.SR.LicMgrAlreadyLocked);
			}
			s_contextLockHolder = contextUser;
		}
	}

	public static void UnlockContext(object contextUser)
	{
		lock (s_internalSyncObject)
		{
			if (s_contextLockHolder != contextUser)
			{
				throw new ArgumentException(System.SR.LicMgrDifferentUser);
			}
			s_contextLockHolder = null;
		}
	}

	private static bool ValidateInternal(Type type, object instance, bool allowExceptions, out License license)
	{
		string licenseKey;
		return ValidateInternalRecursive(CurrentContext, type, instance, allowExceptions, out license, out licenseKey);
	}

	private static bool ValidateInternalRecursive(LicenseContext context, Type type, object instance, bool allowExceptions, out License license, out string licenseKey)
	{
		LicenseProvider licenseProvider = GetCachedProvider(type);
		if (licenseProvider == null && !GetCachedNoLicenseProvider(type))
		{
			LicenseProviderAttribute licenseProviderAttribute = (LicenseProviderAttribute)Attribute.GetCustomAttribute(type, typeof(LicenseProviderAttribute), inherit: false);
			if (licenseProviderAttribute != null)
			{
				Type licenseProvider2 = licenseProviderAttribute.LicenseProvider;
				licenseProvider = GetCachedProviderInstance(licenseProvider2) ?? ((LicenseProvider)Activator.CreateInstance(licenseProvider2));
			}
			CacheProvider(type, licenseProvider);
		}
		license = null;
		bool flag = true;
		licenseKey = null;
		if (licenseProvider != null)
		{
			license = licenseProvider.GetLicense(context, type, instance, allowExceptions);
			if (license == null)
			{
				flag = false;
			}
			else
			{
				licenseKey = license.LicenseKey;
			}
		}
		if (flag && instance == null)
		{
			Type baseType = type.BaseType;
			if (baseType != typeof(object) && baseType != null)
			{
				if (license != null)
				{
					license.Dispose();
					license = null;
				}
				flag = ValidateInternalRecursive(context, baseType, null, allowExceptions, out license, out var _);
				if (license != null)
				{
					license.Dispose();
					license = null;
				}
			}
		}
		return flag;
	}

	public static void Validate(Type type)
	{
		if (!ValidateInternal(type, null, allowExceptions: true, out var license))
		{
			throw new LicenseException(type);
		}
		if (license != null)
		{
			license.Dispose();
			license = null;
		}
	}

	public static License? Validate(Type type, object? instance)
	{
		if (!ValidateInternal(type, instance, allowExceptions: true, out var license))
		{
			throw new LicenseException(type, instance);
		}
		return license;
	}
}
