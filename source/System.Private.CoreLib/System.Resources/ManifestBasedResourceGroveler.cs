using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Resources;

internal sealed class ManifestBasedResourceGroveler : IResourceGroveler
{
	private readonly ResourceManager.ResourceManagerMediator _mediator;

	private static Assembly InternalGetSatelliteAssembly(Assembly mainAssembly, CultureInfo culture, Version version)
	{
		return ((RuntimeAssembly)mainAssembly).InternalGetSatelliteAssembly(culture, version, throwOnFileNotFound: false);
	}

	public ManifestBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
	{
		_mediator = mediator;
	}

	public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists)
	{
		ResourceSet value = null;
		Stream stream = null;
		CultureInfo cultureInfo = UltimateFallbackFixup(culture);
		Assembly assembly;
		if (cultureInfo.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
		{
			assembly = _mediator.MainAssembly;
		}
		else
		{
			assembly = GetSatelliteAssembly(cultureInfo);
			if (assembly == null && culture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite)
			{
				HandleSatelliteMissing();
			}
		}
		string resourceFileName = _mediator.GetResourceFileName(cultureInfo);
		if (assembly != null)
		{
			lock (localResourceSets)
			{
				localResourceSets.TryGetValue(culture.Name, out value);
			}
			stream = GetManifestResourceStream(assembly, resourceFileName);
		}
		if (createIfNotExists && stream != null && value == null)
		{
			value = CreateResourceSet(stream, assembly);
		}
		else if (stream == null && tryParents && culture.HasInvariantCultureName)
		{
			HandleResourceStreamMissing(resourceFileName);
		}
		return value;
	}

	private CultureInfo UltimateFallbackFixup(CultureInfo lookForCulture)
	{
		CultureInfo result = lookForCulture;
		if (lookForCulture.Name == _mediator.NeutralResourcesCulture.Name && _mediator.FallbackLoc == UltimateResourceFallbackLocation.MainAssembly)
		{
			result = CultureInfo.InvariantCulture;
		}
		else if (lookForCulture.HasInvariantCultureName && _mediator.FallbackLoc == UltimateResourceFallbackLocation.Satellite)
		{
			result = _mediator.NeutralResourcesCulture;
		}
		return result;
	}

	internal static CultureInfo GetNeutralResourcesLanguage(Assembly a, out UltimateResourceFallbackLocation fallbackLocation)
	{
		NeutralResourcesLanguageAttribute customAttribute = a.GetCustomAttribute<NeutralResourcesLanguageAttribute>();
		if (customAttribute == null || (GlobalizationMode.Invariant && GlobalizationMode.PredefinedCulturesOnly))
		{
			fallbackLocation = UltimateResourceFallbackLocation.MainAssembly;
			return CultureInfo.InvariantCulture;
		}
		fallbackLocation = customAttribute.Location;
		if (fallbackLocation < UltimateResourceFallbackLocation.MainAssembly || fallbackLocation > UltimateResourceFallbackLocation.Satellite)
		{
			throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, fallbackLocation));
		}
		try
		{
			return CultureInfo.GetCultureInfo(customAttribute.CultureName);
		}
		catch (ArgumentException innerException)
		{
			if (a == typeof(object).Assembly)
			{
				return CultureInfo.InvariantCulture;
			}
			throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_Asm_Culture, a, customAttribute.CultureName), innerException);
		}
	}

	internal ResourceSet CreateResourceSet(Stream store, Assembly assembly)
	{
		if (store.CanSeek && store.Length > 4)
		{
			long position = store.Position;
			BinaryReader binaryReader = new BinaryReader(store);
			int num = binaryReader.ReadInt32();
			if (num == ResourceManager.MagicNumber)
			{
				int num2 = binaryReader.ReadInt32();
				string text = null;
				string text2 = null;
				if (num2 == ResourceManager.HeaderVersionNumber)
				{
					binaryReader.ReadInt32();
					text = binaryReader.ReadString();
					text2 = binaryReader.ReadString();
				}
				else
				{
					if (num2 <= ResourceManager.HeaderVersionNumber)
					{
						throw new NotSupportedException(SR.Format(SR.NotSupported_ObsoleteResourcesFile, _mediator.MainAssembly.GetName().Name));
					}
					int num3 = binaryReader.ReadInt32();
					long offset = binaryReader.BaseStream.Position + num3;
					text = binaryReader.ReadString();
					text2 = binaryReader.ReadString();
					binaryReader.BaseStream.Seek(offset, SeekOrigin.Begin);
				}
				store.Position = position;
				if (CanUseDefaultResourceClasses(text, text2))
				{
					return new RuntimeResourceSet(store, permitDeserialization: true);
				}
				if (ResourceReader.AllowCustomResourceTypes)
				{
					return InternalGetResourceSetFromSerializedData(store, text, text2, _mediator);
				}
				throw new NotSupportedException(SR.ResourceManager_ReflectionNotAllowed);
			}
			store.Position = position;
		}
		if (_mediator.UserResourceSet == null)
		{
			return new RuntimeResourceSet(store, permitDeserialization: true);
		}
		object[] args = new object[2] { store, assembly };
		try
		{
			try
			{
				return (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
			}
			catch (MissingMethodException)
			{
			}
			return (ResourceSet)Activator.CreateInstance(args: new object[1] { store }, type: _mediator.UserResourceSet);
		}
		catch (MissingMethodException innerException)
		{
			throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResMgrBadResSet_Type, _mediator.UserResourceSet.AssemblyQualifiedName), innerException);
		}
	}

	[RequiresUnreferencedCode("The CustomResourceTypesSupport feature switch has been enabled for this app which is being trimmed. Custom readers as well as custom objects on the resources file are not observable by the trimmer and so required assemblies, types and members may be removed.")]
	private static ResourceSet InternalGetResourceSetFromSerializedData(Stream store, string readerTypeName, string resSetTypeName, ResourceManager.ResourceManagerMediator mediator)
	{
		IResourceReader resourceReader;
		if (ResourceManager.IsDefaultType(readerTypeName, "System.Resources.ResourceReader"))
		{
			resourceReader = new ResourceReader(store, new Dictionary<string, ResourceLocator>(FastResourceComparer.Default), permitDeserialization: true);
		}
		else
		{
			Type type = Type.GetType(readerTypeName, throwOnError: true);
			resourceReader = (IResourceReader)Activator.CreateInstance(type, store);
		}
		object[] args = new object[1] { resourceReader };
		Type type2 = mediator.UserResourceSet;
		if (type2 == null)
		{
			type2 = Type.GetType(resSetTypeName, throwOnError: true, ignoreCase: false);
		}
		return (ResourceSet)Activator.CreateInstance(type2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, args, null, null);
	}

	private Stream GetManifestResourceStream(Assembly satellite, string fileName)
	{
		return satellite.GetManifestResourceStream(_mediator.LocationInfo, fileName) ?? CaseInsensitiveManifestResourceStreamLookup(satellite, fileName);
	}

	private Stream CaseInsensitiveManifestResourceStreamLookup(Assembly satellite, string name)
	{
		string text = _mediator.LocationInfo?.Namespace;
		char ptr = Type.Delimiter;
		string text2 = ((text != null && name != null) ? string.Concat(text, new ReadOnlySpan<char>(ref ptr, 1), name) : (text + name));
		string text3 = null;
		string[] manifestResourceNames = satellite.GetManifestResourceNames();
		foreach (string text4 in manifestResourceNames)
		{
			if (string.Equals(text4, text2, StringComparison.InvariantCultureIgnoreCase))
			{
				if (text3 != null)
				{
					throw new MissingManifestResourceException(SR.Format(SR.MissingManifestResource_MultipleBlobs, text2, satellite.ToString()));
				}
				text3 = text4;
			}
		}
		if (text3 == null)
		{
			return null;
		}
		return satellite.GetManifestResourceStream(text3);
	}

	private Assembly GetSatelliteAssembly(CultureInfo lookForCulture)
	{
		if (!_mediator.LookedForSatelliteContractVersion)
		{
			_mediator.SatelliteContractVersion = ResourceManager.ResourceManagerMediator.ObtainSatelliteContractVersion(_mediator.MainAssembly);
			_mediator.LookedForSatelliteContractVersion = true;
		}
		Assembly result = null;
		try
		{
			result = InternalGetSatelliteAssembly(_mediator.MainAssembly, lookForCulture, _mediator.SatelliteContractVersion);
		}
		catch (FileLoadException)
		{
		}
		catch (BadImageFormatException)
		{
		}
		return result;
	}

	private bool CanUseDefaultResourceClasses(string readerTypeName, string resSetTypeName)
	{
		if (_mediator.UserResourceSet != null)
		{
			return false;
		}
		if (readerTypeName != null && !ResourceManager.IsDefaultType(readerTypeName, "System.Resources.ResourceReader"))
		{
			return false;
		}
		if (resSetTypeName != null && !ResourceManager.IsDefaultType(resSetTypeName, "System.Resources.RuntimeResourceSet"))
		{
			return false;
		}
		return true;
	}

	private void HandleSatelliteMissing()
	{
		AssemblyName name = _mediator.MainAssembly.GetName();
		string p = AssemblyNameFormatter.ComputeDisplayName(name.Name + ".resources.dll", _mediator.SatelliteContractVersion, null, name.GetPublicKeyToken());
		string text = _mediator.NeutralResourcesCulture.Name;
		if (text.Length == 0)
		{
			text = "<invariant>";
		}
		throw new MissingSatelliteAssemblyException(SR.Format(SR.MissingSatelliteAssembly_Culture_Name, _mediator.NeutralResourcesCulture, p), text);
	}

	private static string GetManifestResourceNamesList(Assembly assembly)
	{
		try
		{
			string[] manifestResourceNames = assembly.GetManifestResourceNames();
			int num = manifestResourceNames.Length;
			string text = "\"";
			if (num > 10)
			{
				num = 10;
				text = "\", ...";
			}
			return "\"" + string.Join("\", \"", manifestResourceNames, 0, num) + text;
		}
		catch
		{
			return "\"\"";
		}
	}

	private void HandleResourceStreamMissing(string fileName)
	{
		if (_mediator.MainAssembly == typeof(object).Assembly && _mediator.BaseName.Equals("System.Private.CoreLib"))
		{
			Environment.FailFast("System.Private.CoreLib.resources couldn't be found!  Large parts of the BCL won't work!");
		}
		string text = string.Empty;
		if (_mediator.LocationInfo != null && _mediator.LocationInfo.Namespace != null)
		{
			text = _mediator.LocationInfo.Namespace + Type.Delimiter;
		}
		text += fileName;
		throw new MissingManifestResourceException(SR.Format(SR.MissingManifestResource_NoNeutralAsm, text, _mediator.MainAssembly.GetName().Name, GetManifestResourceNamesList(_mediator.MainAssembly)));
	}
}
