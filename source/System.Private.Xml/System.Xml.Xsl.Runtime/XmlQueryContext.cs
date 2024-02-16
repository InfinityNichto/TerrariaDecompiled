using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class XmlQueryContext
{
	private readonly XmlQueryRuntime _runtime;

	private readonly XPathNavigator _defaultDataSource;

	private readonly XmlResolver _dataSources;

	private readonly Hashtable _dataSourceCache;

	private readonly XsltArgumentList _argList;

	private XmlExtensionFunctionTable _extFuncsLate;

	private readonly WhitespaceRuleLookup _wsRules;

	private readonly QueryReaderSettings _readerSettings;

	public XmlNameTable QueryNameTable => _readerSettings.NameTable;

	public XmlNameTable DefaultNameTable
	{
		get
		{
			if (_defaultDataSource == null)
			{
				return null;
			}
			return _defaultDataSource.NameTable;
		}
	}

	public XPathNavigator DefaultDataSource
	{
		get
		{
			if (_defaultDataSource == null)
			{
				throw new XslTransformException(System.SR.XmlIl_NoDefaultDocument, string.Empty);
			}
			return _defaultDataSource;
		}
	}

	internal XmlQueryContext(XmlQueryRuntime runtime, object defaultDataSource, XmlResolver dataSources, XsltArgumentList argList, WhitespaceRuleLookup wsRules)
	{
		_runtime = runtime;
		_dataSources = dataSources;
		_dataSourceCache = new Hashtable();
		_argList = argList;
		_wsRules = wsRules;
		if (defaultDataSource is XmlReader)
		{
			_readerSettings = new QueryReaderSettings((XmlReader)defaultDataSource);
		}
		else
		{
			_readerSettings = new QueryReaderSettings(new NameTable());
		}
		if (defaultDataSource is string)
		{
			_defaultDataSource = GetDataSource(defaultDataSource as string, null);
			if (_defaultDataSource == null)
			{
				throw new XslTransformException(System.SR.XmlIl_UnknownDocument, defaultDataSource as string);
			}
		}
		else if (defaultDataSource != null)
		{
			_defaultDataSource = ConstructDocument(defaultDataSource, null, null);
		}
	}

	public XPathNavigator GetDataSource(string uriRelative, string uriBase)
	{
		XPathNavigator xPathNavigator = null;
		try
		{
			Uri baseUri = ((uriBase != null) ? _dataSources.ResolveUri(null, uriBase) : null);
			Uri uri = _dataSources.ResolveUri(baseUri, uriRelative);
			if (uri != null)
			{
				xPathNavigator = _dataSourceCache[uri] as XPathNavigator;
			}
			if (xPathNavigator == null)
			{
				object entity = _dataSources.GetEntity(uri, null, null);
				if (entity != null)
				{
					xPathNavigator = ConstructDocument(entity, uriRelative, uri);
					_dataSourceCache.Add(uri, xPathNavigator);
				}
			}
		}
		catch (XslTransformException)
		{
			throw;
		}
		catch (Exception ex2)
		{
			if (!XmlException.IsCatchableException(ex2))
			{
				throw;
			}
			throw new XslTransformException(ex2, System.SR.XmlIl_DocumentLoadError, uriRelative);
		}
		return xPathNavigator;
	}

	private XPathNavigator ConstructDocument(object dataSource, string uriRelative, Uri uriResolved)
	{
		if (dataSource is Stream stream)
		{
			XmlReader xmlReader = _readerSettings.CreateReader(stream, (uriResolved != null) ? uriResolved.ToString() : null);
			try
			{
				return new XPathDocument(WhitespaceRuleReader.CreateReader(xmlReader, _wsRules), XmlSpace.Preserve).CreateNavigator();
			}
			finally
			{
				xmlReader.Close();
			}
		}
		if (dataSource is XmlReader)
		{
			return new XPathDocument(WhitespaceRuleReader.CreateReader(dataSource as XmlReader, _wsRules), XmlSpace.Preserve).CreateNavigator();
		}
		if (dataSource is IXPathNavigable)
		{
			if (_wsRules != null)
			{
				throw new XslTransformException(System.SR.XmlIl_CantStripNav, string.Empty);
			}
			return (dataSource as IXPathNavigable).CreateNavigator();
		}
		throw new XslTransformException(System.SR.XmlIl_CantResolveEntity, uriRelative, dataSource.GetType().ToString());
	}

	public object GetParameter(string localName, string namespaceUri)
	{
		if (_argList == null)
		{
			return null;
		}
		return _argList.GetParam(localName, namespaceUri);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "In order for this code path to be hit, a previous call to XsltArgumentList.AddExtensionObject is required. That method is already annotated as unsafe and throwing a warning, so we can suppress here.")]
	public object GetLateBoundObject(string namespaceUri)
	{
		if (_argList == null)
		{
			return null;
		}
		return _argList.GetExtensionObject(namespaceUri);
	}

	[RequiresUnreferencedCode("The extension function referenced will be called from the stylesheet which cannot be statically analyzed.")]
	public bool LateBoundFunctionExists(string name, string namespaceUri)
	{
		if (_argList == null)
		{
			return false;
		}
		object extensionObject = _argList.GetExtensionObject(namespaceUri);
		if (extensionObject == null)
		{
			return false;
		}
		return new XmlExtensionFunction(name, namespaceUri, -1, extensionObject.GetType(), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public).CanBind();
	}

	[RequiresUnreferencedCode("The extension function referenced will be called from the stylesheet which cannot be statically analyzed.")]
	public IList<XPathItem> InvokeXsltLateBoundFunction(string name, string namespaceUri, IList<XPathItem>[] args)
	{
		object obj = ((_argList != null) ? _argList.GetExtensionObject(namespaceUri) : null);
		if (obj == null)
		{
			throw new XslTransformException(System.SR.XmlIl_UnknownExtObj, namespaceUri);
		}
		if (_extFuncsLate == null)
		{
			_extFuncsLate = new XmlExtensionFunctionTable();
		}
		XmlExtensionFunction xmlExtensionFunction = _extFuncsLate.Bind(name, namespaceUri, args.Length, obj.GetType(), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		object[] array = new object[args.Length];
		for (int i = 0; i < args.Length; i++)
		{
			XmlQueryType xmlArgumentType = xmlExtensionFunction.GetXmlArgumentType(i);
			switch (xmlArgumentType.TypeCode)
			{
			case XmlTypeCode.Boolean:
				array[i] = XsltConvert.ToBoolean(args[i]);
				break;
			case XmlTypeCode.Double:
				array[i] = XsltConvert.ToDouble(args[i]);
				break;
			case XmlTypeCode.String:
				array[i] = XsltConvert.ToString(args[i]);
				break;
			case XmlTypeCode.Node:
				if (xmlArgumentType.IsSingleton)
				{
					array[i] = XsltConvert.ToNode(args[i]);
				}
				else
				{
					array[i] = XsltConvert.ToNodeSet(args[i]);
				}
				break;
			case XmlTypeCode.Item:
				array[i] = args[i];
				break;
			}
			Type clrArgumentType = xmlExtensionFunction.GetClrArgumentType(i);
			if (xmlArgumentType.TypeCode == XmlTypeCode.Item || !clrArgumentType.IsAssignableFrom(array[i].GetType()))
			{
				array[i] = _runtime.ChangeTypeXsltArgument(xmlArgumentType, array[i], clrArgumentType);
			}
		}
		object obj2 = xmlExtensionFunction.Invoke(obj, array);
		if (obj2 == null && xmlExtensionFunction.ClrReturnType == XsltConvert.VoidType)
		{
			return XmlQueryNodeSequence.Empty;
		}
		return (IList<XPathItem>)_runtime.ChangeTypeXsltResult(XmlQueryTypeFactory.ItemS, obj2);
	}

	public void OnXsltMessageEncountered(string message)
	{
		((_argList != null) ? _argList.xsltMessageEncountered : null)?.Invoke(this, new XmlILQueryEventArgs(message));
	}
}
