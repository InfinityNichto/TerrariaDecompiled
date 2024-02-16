using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Resources;

public class ResourceManager
{
	internal sealed class CultureNameResourceSetPair
	{
		public string lastCultureName;

		public ResourceSet lastResourceSet;
	}

	internal sealed class ResourceManagerMediator
	{
		private readonly ResourceManager _rm;

		internal string ModuleDir => _rm._moduleDir;

		internal Type LocationInfo => _rm._locationInfo;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		internal Type UserResourceSet => _rm._userResourceSet;

		internal string BaseNameField => _rm.BaseNameField;

		internal CultureInfo NeutralResourcesCulture => _rm._neutralResourcesCulture;

		internal bool LookedForSatelliteContractVersion
		{
			get
			{
				return _rm._lookedForSatelliteContractVersion;
			}
			set
			{
				_rm._lookedForSatelliteContractVersion = value;
			}
		}

		internal Version SatelliteContractVersion
		{
			get
			{
				return _rm._satelliteContractVersion;
			}
			set
			{
				_rm._satelliteContractVersion = value;
			}
		}

		internal UltimateResourceFallbackLocation FallbackLoc => _rm.FallbackLocation;

		internal Assembly MainAssembly => _rm.MainAssembly;

		internal string BaseName => _rm.BaseName;

		internal ResourceManagerMediator(ResourceManager rm)
		{
			if (rm == null)
			{
				throw new ArgumentNullException("rm");
			}
			_rm = rm;
		}

		internal string GetResourceFileName(CultureInfo culture)
		{
			return _rm.GetResourceFileName(culture);
		}

		internal static Version ObtainSatelliteContractVersion(Assembly a)
		{
			return GetSatelliteContractVersion(a);
		}
	}

	protected string BaseNameField;

	protected Assembly? MainAssembly;

	private Dictionary<string, ResourceSet> _resourceSets;

	private readonly string _moduleDir;

	private readonly Type _locationInfo;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	private readonly Type _userResourceSet;

	private CultureInfo _neutralResourcesCulture;

	private CultureNameResourceSetPair _lastUsedResourceCache;

	private bool _ignoreCase;

	private bool _useManifest;

	private UltimateResourceFallbackLocation _fallbackLoc;

	private Version _satelliteContractVersion;

	private bool _lookedForSatelliteContractVersion;

	private IResourceGroveler _resourceGroveler;

	public static readonly int MagicNumber = -1091581234;

	public static readonly int HeaderVersionNumber = 1;

	private static readonly Type s_minResourceSet = typeof(ResourceSet);

	public virtual string BaseName => BaseNameField;

	public virtual bool IgnoreCase
	{
		get
		{
			return _ignoreCase;
		}
		set
		{
			_ignoreCase = value;
		}
	}

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	public virtual Type ResourceSetType => _userResourceSet ?? typeof(RuntimeResourceSet);

	protected UltimateResourceFallbackLocation FallbackLocation
	{
		get
		{
			return _fallbackLoc;
		}
		set
		{
			_fallbackLoc = value;
		}
	}

	protected ResourceManager()
	{
		_lastUsedResourceCache = new CultureNameResourceSetPair();
		ResourceManagerMediator mediator = new ResourceManagerMediator(this);
		_resourceGroveler = new ManifestBasedResourceGroveler(mediator);
		BaseNameField = string.Empty;
	}

	private ResourceManager(string baseName, string resourceDir, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type userResourceSet)
	{
		if (baseName == null)
		{
			throw new ArgumentNullException("baseName");
		}
		if (resourceDir == null)
		{
			throw new ArgumentNullException("resourceDir");
		}
		BaseNameField = baseName;
		_moduleDir = resourceDir;
		_userResourceSet = userResourceSet;
		_resourceSets = new Dictionary<string, ResourceSet>();
		_lastUsedResourceCache = new CultureNameResourceSetPair();
		ResourceManagerMediator mediator = new ResourceManagerMediator(this);
		_resourceGroveler = new FileBasedResourceGroveler(mediator);
	}

	public ResourceManager(string baseName, Assembly assembly)
	{
		if (baseName == null)
		{
			throw new ArgumentNullException("baseName");
		}
		if (null == assembly)
		{
			throw new ArgumentNullException("assembly");
		}
		if (!assembly.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		MainAssembly = assembly;
		BaseNameField = baseName;
		CommonAssemblyInit();
	}

	public ResourceManager(string baseName, Assembly assembly, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type? usingResourceSet)
	{
		if (baseName == null)
		{
			throw new ArgumentNullException("baseName");
		}
		if (null == assembly)
		{
			throw new ArgumentNullException("assembly");
		}
		if (!assembly.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
		}
		MainAssembly = assembly;
		BaseNameField = baseName;
		if (usingResourceSet != null && usingResourceSet != s_minResourceSet && !usingResourceSet.IsSubclassOf(s_minResourceSet))
		{
			throw new ArgumentException(SR.Arg_ResMgrNotResSet, "usingResourceSet");
		}
		_userResourceSet = usingResourceSet;
		CommonAssemblyInit();
	}

	public ResourceManager(Type resourceSource)
	{
		if (null == resourceSource)
		{
			throw new ArgumentNullException("resourceSource");
		}
		if (!resourceSource.IsRuntimeImplemented())
		{
			throw new ArgumentException(SR.Argument_MustBeRuntimeType);
		}
		_locationInfo = resourceSource;
		MainAssembly = _locationInfo.Assembly;
		BaseNameField = resourceSource.Name;
		CommonAssemblyInit();
	}

	[MemberNotNull("_resourceGroveler")]
	private void CommonAssemblyInit()
	{
		_useManifest = true;
		_resourceSets = new Dictionary<string, ResourceSet>();
		_lastUsedResourceCache = new CultureNameResourceSetPair();
		ResourceManagerMediator mediator = new ResourceManagerMediator(this);
		_resourceGroveler = new ManifestBasedResourceGroveler(mediator);
		_neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(MainAssembly, out _fallbackLoc);
	}

	public virtual void ReleaseAllResources()
	{
		Dictionary<string, ResourceSet> resourceSets = _resourceSets;
		_resourceSets = new Dictionary<string, ResourceSet>();
		_lastUsedResourceCache = new CultureNameResourceSetPair();
		lock (resourceSets)
		{
			foreach (var (_, resourceSet2) in resourceSets)
			{
				resourceSet2.Close();
			}
		}
	}

	public static ResourceManager CreateFileBasedResourceManager(string baseName, string resourceDir, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type? usingResourceSet)
	{
		return new ResourceManager(baseName, resourceDir, usingResourceSet);
	}

	protected virtual string GetResourceFileName(CultureInfo culture)
	{
		if (culture.HasInvariantCultureName)
		{
			return BaseNameField + ".resources";
		}
		CultureInfo.VerifyCultureName(culture.Name, throwException: true);
		return BaseNameField + "." + culture.Name + ".resources";
	}

	internal ResourceSet GetFirstResourceSet(CultureInfo culture)
	{
		if (_neutralResourcesCulture != null && culture.Name == _neutralResourcesCulture.Name)
		{
			culture = CultureInfo.InvariantCulture;
		}
		if (_lastUsedResourceCache != null)
		{
			lock (_lastUsedResourceCache)
			{
				if (culture.Name == _lastUsedResourceCache.lastCultureName)
				{
					return _lastUsedResourceCache.lastResourceSet;
				}
			}
		}
		Dictionary<string, ResourceSet> resourceSets = _resourceSets;
		ResourceSet value = null;
		if (resourceSets != null)
		{
			lock (resourceSets)
			{
				resourceSets.TryGetValue(culture.Name, out value);
			}
		}
		if (value != null)
		{
			if (_lastUsedResourceCache != null)
			{
				lock (_lastUsedResourceCache)
				{
					_lastUsedResourceCache.lastCultureName = culture.Name;
					_lastUsedResourceCache.lastResourceSet = value;
				}
			}
			return value;
		}
		return null;
	}

	public virtual ResourceSet? GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		Dictionary<string, ResourceSet> resourceSets = _resourceSets;
		if (resourceSets != null)
		{
			lock (resourceSets)
			{
				if (resourceSets.TryGetValue(culture.Name, out var value))
				{
					return value;
				}
			}
		}
		if (_useManifest && culture.HasInvariantCultureName)
		{
			string resourceFileName = GetResourceFileName(culture);
			Stream manifestResourceStream = MainAssembly.GetManifestResourceStream(_locationInfo, resourceFileName);
			if (createIfNotExists && manifestResourceStream != null)
			{
				ResourceSet value = ((ManifestBasedResourceGroveler)_resourceGroveler).CreateResourceSet(manifestResourceStream, MainAssembly);
				AddResourceSet(resourceSets, culture.Name, ref value);
				return value;
			}
		}
		return InternalGetResourceSet(culture, createIfNotExists, tryParents);
	}

	protected virtual ResourceSet? InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
	{
		Dictionary<string, ResourceSet> resourceSets = _resourceSets;
		ResourceSet value = null;
		CultureInfo cultureInfo = null;
		lock (resourceSets)
		{
			if (resourceSets.TryGetValue(culture.Name, out value))
			{
				return value;
			}
		}
		ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(culture, _neutralResourcesCulture, tryParents);
		foreach (CultureInfo item in resourceFallbackManager)
		{
			lock (resourceSets)
			{
				if (resourceSets.TryGetValue(item.Name, out value))
				{
					if (culture != item)
					{
						cultureInfo = item;
					}
					break;
				}
			}
			value = _resourceGroveler.GrovelForResourceSet(item, resourceSets, tryParents, createIfNotExists);
			if (value != null)
			{
				cultureInfo = item;
				break;
			}
		}
		if (value != null && cultureInfo != null)
		{
			foreach (CultureInfo item2 in resourceFallbackManager)
			{
				AddResourceSet(resourceSets, item2.Name, ref value);
				if (item2 == cultureInfo)
				{
					break;
				}
			}
		}
		return value;
	}

	private static void AddResourceSet(Dictionary<string, ResourceSet> localResourceSets, string cultureName, ref ResourceSet rs)
	{
		lock (localResourceSets)
		{
			if (localResourceSets.TryGetValue(cultureName, out var value))
			{
				if (value != rs)
				{
					if (!localResourceSets.ContainsValue(rs))
					{
						rs.Dispose();
					}
					rs = value;
				}
			}
			else
			{
				localResourceSets.Add(cultureName, rs);
			}
		}
	}

	protected static Version? GetSatelliteContractVersion(Assembly a)
	{
		if (a == null)
		{
			throw new ArgumentNullException("a", SR.ArgumentNull_Assembly);
		}
		string text = a.GetCustomAttribute<SatelliteContractVersionAttribute>()?.Version;
		if (text == null)
		{
			return null;
		}
		if (!Version.TryParse(text, out Version result))
		{
			throw new ArgumentException(SR.Format(SR.Arg_InvalidSatelliteContract_Asm_Ver, a, text));
		}
		return result;
	}

	protected static CultureInfo GetNeutralResourcesLanguage(Assembly a)
	{
		UltimateResourceFallbackLocation fallbackLocation;
		return ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(a, out fallbackLocation);
	}

	internal static bool IsDefaultType(string asmTypeName, string typeName)
	{
		int num = asmTypeName.IndexOf(',');
		if (((num == -1) ? asmTypeName.Length : num) != typeName.Length)
		{
			return false;
		}
		if (string.Compare(asmTypeName, 0, typeName, 0, typeName.Length, StringComparison.Ordinal) != 0)
		{
			return false;
		}
		if (num == -1)
		{
			return true;
		}
		while (char.IsWhiteSpace(asmTypeName[++num]))
		{
		}
		AssemblyName assemblyName = new AssemblyName(asmTypeName.Substring(num));
		return string.Equals(assemblyName.Name, "mscorlib", StringComparison.OrdinalIgnoreCase);
	}

	public virtual string? GetString(string name)
	{
		return GetString(name, null);
	}

	public virtual string? GetString(string name, CultureInfo? culture)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (culture == null)
		{
			culture = CultureInfo.CurrentUICulture;
		}
		ResourceSet resourceSet = GetFirstResourceSet(culture);
		if (resourceSet != null)
		{
			string @string = resourceSet.GetString(name, _ignoreCase);
			if (@string != null)
			{
				return @string;
			}
		}
		ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(culture, _neutralResourcesCulture, useParents: true);
		foreach (CultureInfo item in resourceFallbackManager)
		{
			ResourceSet resourceSet2 = InternalGetResourceSet(item, createIfNotExists: true, tryParents: true);
			if (resourceSet2 == null)
			{
				break;
			}
			if (resourceSet2 == resourceSet)
			{
				continue;
			}
			string string2 = resourceSet2.GetString(name, _ignoreCase);
			if (string2 != null)
			{
				if (_lastUsedResourceCache != null)
				{
					lock (_lastUsedResourceCache)
					{
						_lastUsedResourceCache.lastCultureName = item.Name;
						_lastUsedResourceCache.lastResourceSet = resourceSet2;
					}
				}
				return string2;
			}
			resourceSet = resourceSet2;
		}
		return null;
	}

	public virtual object? GetObject(string name)
	{
		return GetObject(name, null, wrapUnmanagedMemStream: true);
	}

	public virtual object? GetObject(string name, CultureInfo? culture)
	{
		return GetObject(name, culture, wrapUnmanagedMemStream: true);
	}

	private object GetObject(string name, CultureInfo culture, bool wrapUnmanagedMemStream)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (culture == null)
		{
			culture = CultureInfo.CurrentUICulture;
		}
		ResourceSet resourceSet = GetFirstResourceSet(culture);
		if (resourceSet != null)
		{
			object @object = resourceSet.GetObject(name, _ignoreCase);
			if (@object != null)
			{
				UnmanagedMemoryStream unmanagedMemoryStream = @object as UnmanagedMemoryStream;
				if (unmanagedMemoryStream != null && wrapUnmanagedMemStream)
				{
					return new UnmanagedMemoryStreamWrapper(unmanagedMemoryStream);
				}
				return @object;
			}
		}
		ResourceFallbackManager resourceFallbackManager = new ResourceFallbackManager(culture, _neutralResourcesCulture, useParents: true);
		foreach (CultureInfo item in resourceFallbackManager)
		{
			ResourceSet resourceSet2 = InternalGetResourceSet(item, createIfNotExists: true, tryParents: true);
			if (resourceSet2 == null)
			{
				break;
			}
			if (resourceSet2 == resourceSet)
			{
				continue;
			}
			object object2 = resourceSet2.GetObject(name, _ignoreCase);
			if (object2 != null)
			{
				if (_lastUsedResourceCache != null)
				{
					lock (_lastUsedResourceCache)
					{
						_lastUsedResourceCache.lastCultureName = item.Name;
						_lastUsedResourceCache.lastResourceSet = resourceSet2;
					}
				}
				UnmanagedMemoryStream unmanagedMemoryStream2 = object2 as UnmanagedMemoryStream;
				if (unmanagedMemoryStream2 != null && wrapUnmanagedMemStream)
				{
					return new UnmanagedMemoryStreamWrapper(unmanagedMemoryStream2);
				}
				return object2;
			}
			resourceSet = resourceSet2;
		}
		return null;
	}

	public UnmanagedMemoryStream? GetStream(string name)
	{
		return GetStream(name, null);
	}

	public UnmanagedMemoryStream? GetStream(string name, CultureInfo? culture)
	{
		object @object = GetObject(name, culture, wrapUnmanagedMemStream: false);
		UnmanagedMemoryStream unmanagedMemoryStream = @object as UnmanagedMemoryStream;
		if (unmanagedMemoryStream == null && @object != null)
		{
			throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotStream_Name, name));
		}
		return unmanagedMemoryStream;
	}
}
