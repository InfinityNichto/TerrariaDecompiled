using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlExtensionFunctionTable
{
	private readonly Dictionary<XmlExtensionFunction, XmlExtensionFunction> _table;

	private XmlExtensionFunction _funcCached;

	public XmlExtensionFunctionTable()
	{
		_table = new Dictionary<XmlExtensionFunction, XmlExtensionFunction>();
	}

	public XmlExtensionFunction Bind(string name, string namespaceUri, int numArgs, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type objectType, BindingFlags flags)
	{
		if (_funcCached == null)
		{
			_funcCached = new XmlExtensionFunction();
		}
		_funcCached.Init(name, namespaceUri, numArgs, objectType, flags);
		if (!_table.TryGetValue(_funcCached, out var value))
		{
			value = _funcCached;
			_funcCached = null;
			value.Bind();
			_table.Add(value, value);
		}
		return value;
	}
}
