using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Resources;

internal sealed class FileBasedResourceGroveler : IResourceGroveler
{
	private readonly ResourceManager.ResourceManagerMediator _mediator;

	public FileBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
	{
		_mediator = mediator;
	}

	public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists)
	{
		ResourceSet result = null;
		string resourceFileName = _mediator.GetResourceFileName(culture);
		string text = FindResourceFile(culture, resourceFileName);
		if (text == null)
		{
			if (tryParents && culture.HasInvariantCultureName)
			{
				string value = ((_mediator.LocationInfo == null) ? "<null>" : _mediator.LocationInfo.FullName);
				throw new MissingManifestResourceException($"{SR.MissingManifestResource_NoNeutralDisk}{"\r\n"}baseName: {_mediator.BaseNameField}  locationInfo: {value}  fileName: {_mediator.GetResourceFileName(culture)}");
			}
		}
		else
		{
			result = CreateResourceSet(text);
		}
		return result;
	}

	private string FindResourceFile(CultureInfo culture, string fileName)
	{
		if (_mediator.ModuleDir != null)
		{
			string text = Path.Combine(_mediator.ModuleDir, fileName);
			if (File.Exists(text))
			{
				return text;
			}
		}
		if (File.Exists(fileName))
		{
			return fileName;
		}
		return null;
	}

	private ResourceSet CreateResourceSet(string file)
	{
		if (_mediator.UserResourceSet == null)
		{
			return new RuntimeResourceSet(file);
		}
		object[] args = new object[1] { file };
		try
		{
			return (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
		}
		catch (MissingMethodException innerException)
		{
			throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResMgrBadResSet_Type, _mediator.UserResourceSet.AssemblyQualifiedName), innerException);
		}
	}
}
