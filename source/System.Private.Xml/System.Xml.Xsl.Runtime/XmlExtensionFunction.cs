using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlExtensionFunction
{
	private string _namespaceUri;

	private string _name;

	private int _numArgs;

	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
	private Type _objectType;

	private BindingFlags _flags;

	private int _hashCode;

	private MethodInfo _meth;

	private Type[] _argClrTypes;

	private Type _retClrType;

	private XmlQueryType[] _argXmlTypes;

	private XmlQueryType _retXmlType;

	public MethodInfo Method => _meth;

	public Type ClrReturnType => _retClrType;

	public XmlQueryType XmlReturnType => _retXmlType;

	public XmlExtensionFunction()
	{
	}

	public XmlExtensionFunction(string name, string namespaceUri, MethodInfo meth)
	{
		_name = name;
		_namespaceUri = namespaceUri;
		Bind(meth);
	}

	public XmlExtensionFunction(string name, string namespaceUri, int numArgs, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type objectType, BindingFlags flags)
	{
		Init(name, namespaceUri, numArgs, objectType, flags);
	}

	public void Init(string name, string namespaceUri, int numArgs, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type objectType, BindingFlags flags)
	{
		_name = name;
		_namespaceUri = namespaceUri;
		_numArgs = numArgs;
		_objectType = objectType;
		_flags = flags;
		_meth = null;
		_argClrTypes = null;
		_retClrType = null;
		_argXmlTypes = null;
		_retXmlType = null;
		_hashCode = namespaceUri.GetHashCode() ^ name.GetHashCode() ^ ((int)flags << 16) ^ numArgs;
	}

	public Type GetClrArgumentType(int index)
	{
		return _argClrTypes[index];
	}

	public XmlQueryType GetXmlArgumentType(int index)
	{
		return _argXmlTypes[index];
	}

	public bool CanBind()
	{
		MethodInfo[] methods = _objectType.GetMethods(_flags);
		StringComparison comparisonType = (((_flags & BindingFlags.IgnoreCase) != 0) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			if (methodInfo.Name.Equals(_name, comparisonType) && (_numArgs == -1 || methodInfo.GetParameters().Length == _numArgs) && !methodInfo.IsGenericMethodDefinition)
			{
				return true;
			}
		}
		return false;
	}

	public void Bind()
	{
		MethodInfo[] methods = _objectType.GetMethods(_flags);
		MethodInfo methodInfo = null;
		StringComparison comparisonType = (((_flags & BindingFlags.IgnoreCase) != 0) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo2 in array)
		{
			if (methodInfo2.Name.Equals(_name, comparisonType) && (_numArgs == -1 || methodInfo2.GetParameters().Length == _numArgs))
			{
				if (methodInfo != null)
				{
					throw new XslTransformException(System.SR.XmlIl_AmbiguousExtensionMethod, _namespaceUri, _name, _numArgs.ToString(CultureInfo.InvariantCulture));
				}
				methodInfo = methodInfo2;
			}
		}
		if (methodInfo == null)
		{
			methods = _objectType.GetMethods(_flags | BindingFlags.NonPublic);
			MethodInfo[] array2 = methods;
			foreach (MethodInfo methodInfo3 in array2)
			{
				if (methodInfo3.Name.Equals(_name, comparisonType) && methodInfo3.GetParameters().Length == _numArgs)
				{
					throw new XslTransformException(System.SR.XmlIl_NonPublicExtensionMethod, _namespaceUri, _name);
				}
			}
			throw new XslTransformException(System.SR.XmlIl_NoExtensionMethod, _namespaceUri, _name, _numArgs.ToString(CultureInfo.InvariantCulture));
		}
		if (methodInfo.IsGenericMethodDefinition)
		{
			throw new XslTransformException(System.SR.XmlIl_GenericExtensionMethod, _namespaceUri, _name);
		}
		Bind(methodInfo);
	}

	private void Bind(MethodInfo meth)
	{
		ParameterInfo[] parameters = meth.GetParameters();
		_meth = meth;
		_argClrTypes = new Type[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			_argClrTypes[i] = GetClrType(parameters[i].ParameterType);
		}
		_retClrType = GetClrType(_meth.ReturnType);
		_argXmlTypes = new XmlQueryType[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
		{
			_argXmlTypes[i] = InferXmlType(_argClrTypes[i]);
			if (_namespaceUri.Length == 0)
			{
				if ((object)_argXmlTypes[i] == XmlQueryTypeFactory.NodeNotRtf)
				{
					_argXmlTypes[i] = XmlQueryTypeFactory.Node;
				}
				else if ((object)_argXmlTypes[i] == XmlQueryTypeFactory.NodeSDod)
				{
					_argXmlTypes[i] = XmlQueryTypeFactory.NodeS;
				}
			}
			else if ((object)_argXmlTypes[i] == XmlQueryTypeFactory.NodeSDod)
			{
				_argXmlTypes[i] = XmlQueryTypeFactory.NodeNotRtfS;
			}
		}
		_retXmlType = InferXmlType(_retClrType);
	}

	public object Invoke(object extObj, object[] args)
	{
		try
		{
			return _meth.Invoke(extObj, args);
		}
		catch (TargetInvocationException ex)
		{
			throw new XslTransformException(ex.InnerException, System.SR.XmlIl_ExtensionError, _name);
		}
		catch (Exception ex2)
		{
			if (!XmlException.IsCatchableException(ex2))
			{
				throw;
			}
			throw new XslTransformException(ex2, System.SR.XmlIl_ExtensionError, _name);
		}
	}

	public override bool Equals(object other)
	{
		XmlExtensionFunction xmlExtensionFunction = other as XmlExtensionFunction;
		if (_hashCode == xmlExtensionFunction._hashCode && _name == xmlExtensionFunction._name && _namespaceUri == xmlExtensionFunction._namespaceUri && _numArgs == xmlExtensionFunction._numArgs && _objectType == xmlExtensionFunction._objectType)
		{
			return _flags == xmlExtensionFunction._flags;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}

	private Type GetClrType(Type clrType)
	{
		if (clrType.IsEnum)
		{
			return Enum.GetUnderlyingType(clrType);
		}
		if (clrType.IsByRef)
		{
			throw new XslTransformException(System.SR.XmlIl_ByRefType, _namespaceUri, _name);
		}
		return clrType;
	}

	private XmlQueryType InferXmlType(Type clrType)
	{
		return XsltConvert.InferXsltType(clrType);
	}
}
