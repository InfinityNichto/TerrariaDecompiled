using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.ComponentModel.Design;

internal sealed class RuntimeLicenseContext : LicenseContext
{
	internal Hashtable _savedLicenseKeys;

	[UnconditionalSuppressMessage("SingleFile", "IL3000: Avoid accessing Assembly file path when publishing as a single file", Justification = "Suppressing the warning until gets fixed, see https://github.com/dotnet/runtime/issues/50821")]
	public override string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
	{
		if (_savedLicenseKeys == null || _savedLicenseKeys[type.AssemblyQualifiedName] == null)
		{
			if (_savedLicenseKeys == null)
			{
				_savedLicenseKeys = new Hashtable();
			}
			if (resourceAssembly == null)
			{
				resourceAssembly = Assembly.GetEntryAssembly();
			}
			if (resourceAssembly == null)
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					string location = assembly.Location;
					if (!(location == string.Empty))
					{
						string name = new FileInfo(location).Name;
						Stream stream = assembly.GetManifestResourceStream(name + ".licenses");
						if (stream == null)
						{
							stream = CaseInsensitiveManifestResourceStreamLookup(assembly, name + ".licenses");
						}
						if (stream != null)
						{
							DesigntimeLicenseContextSerializer.Deserialize(stream, name.ToUpperInvariant(), this);
							break;
						}
					}
				}
			}
			else
			{
				string location2 = resourceAssembly.Location;
				if (location2 != string.Empty)
				{
					string fileName = Path.GetFileName(location2);
					string text = fileName + ".licenses";
					Stream manifestResourceStream = resourceAssembly.GetManifestResourceStream(text);
					if (manifestResourceStream == null)
					{
						string text2 = null;
						CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
						string name2 = resourceAssembly.GetName().Name;
						string[] manifestResourceNames = resourceAssembly.GetManifestResourceNames();
						foreach (string text3 in manifestResourceNames)
						{
							if (compareInfo.Compare(text3, text, CompareOptions.IgnoreCase) == 0 || compareInfo.Compare(text3, name2 + ".exe.licenses", CompareOptions.IgnoreCase) == 0 || compareInfo.Compare(text3, name2 + ".dll.licenses", CompareOptions.IgnoreCase) == 0)
							{
								text2 = text3;
								break;
							}
						}
						if (text2 != null)
						{
							manifestResourceStream = resourceAssembly.GetManifestResourceStream(text2);
						}
					}
					if (manifestResourceStream != null)
					{
						DesigntimeLicenseContextSerializer.Deserialize(manifestResourceStream, fileName.ToUpperInvariant(), this);
					}
				}
			}
		}
		return (string)_savedLicenseKeys[type.AssemblyQualifiedName];
	}

	private Stream CaseInsensitiveManifestResourceStreamLookup(Assembly satellite, string name)
	{
		CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;
		string name2 = satellite.GetName().Name;
		string[] manifestResourceNames = satellite.GetManifestResourceNames();
		foreach (string text in manifestResourceNames)
		{
			if (compareInfo.Compare(text, name, CompareOptions.IgnoreCase) == 0 || compareInfo.Compare(text, name2 + ".exe.licenses") == 0 || compareInfo.Compare(text, name2 + ".dll.licenses") == 0)
			{
				name = text;
				break;
			}
		}
		return satellite.GetManifestResourceStream(name);
	}
}
