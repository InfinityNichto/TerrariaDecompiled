using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl;

public class XsltArgumentList
{
	private readonly Hashtable _parameters = new Hashtable();

	private readonly Hashtable _extensions = new Hashtable();

	internal XsltMessageEncounteredEventHandler xsltMessageEncountered;

	public event XsltMessageEncounteredEventHandler XsltMessageEncountered
	{
		add
		{
			xsltMessageEncountered = (XsltMessageEncounteredEventHandler)Delegate.Combine(xsltMessageEncountered, value);
		}
		remove
		{
			xsltMessageEncountered = (XsltMessageEncounteredEventHandler)Delegate.Remove(xsltMessageEncountered, value);
		}
	}

	public object? GetParam(string name, string namespaceUri)
	{
		return _parameters[new XmlQualifiedName(name, namespaceUri)];
	}

	[RequiresUnreferencedCode("The stylesheet may have calls to methods of the extension object passed in which cannot be statically analyzed by the trimmer. Ensure all methods that may be called are preserved.")]
	public object? GetExtensionObject(string namespaceUri)
	{
		return _extensions[namespaceUri];
	}

	public void AddParam(string name, string namespaceUri, object parameter)
	{
		CheckArgumentNull(name, "name");
		CheckArgumentNull(namespaceUri, "namespaceUri");
		CheckArgumentNull(parameter, "parameter");
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(name, namespaceUri);
		xmlQualifiedName.Verify();
		_parameters.Add(xmlQualifiedName, parameter);
	}

	[RequiresUnreferencedCode("The stylesheet may have calls to methods of the extension object passed in which cannot be statically analyzed by the trimmer. Ensure all methods that may be called are preserved.")]
	public void AddExtensionObject(string namespaceUri, object extension)
	{
		CheckArgumentNull(namespaceUri, "namespaceUri");
		CheckArgumentNull(extension, "extension");
		_extensions.Add(namespaceUri, extension);
	}

	public object? RemoveParam(string name, string namespaceUri)
	{
		XmlQualifiedName key = new XmlQualifiedName(name, namespaceUri);
		object result = _parameters[key];
		_parameters.Remove(key);
		return result;
	}

	public object? RemoveExtensionObject(string namespaceUri)
	{
		object result = _extensions[namespaceUri];
		_extensions.Remove(namespaceUri);
		return result;
	}

	public void Clear()
	{
		_parameters.Clear();
		_extensions.Clear();
		xsltMessageEncountered = null;
	}

	private static void CheckArgumentNull(object param, string paramName)
	{
		if (param == null)
		{
			throw new ArgumentNullException(paramName);
		}
	}
}
