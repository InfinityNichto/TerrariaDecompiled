using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class GenericNameProvider : IGenericNameProvider
{
	private readonly string _genericTypeName;

	private readonly object[] _genericParams;

	private readonly IList<int> _nestedParamCounts;

	public bool ParametersFromBuiltInNamespaces
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			bool flag = true;
			for (int i = 0; i < GetParameterCount(); i++)
			{
				if (!flag)
				{
					break;
				}
				flag = DataContract.IsBuiltInNamespace(GetStableName(i).Namespace);
			}
			return flag;
		}
	}

	internal GenericNameProvider(Type type)
		: this(DataContract.GetClrTypeFullName(type.GetGenericTypeDefinition()), type.GetGenericArguments())
	{
	}

	internal GenericNameProvider(string genericTypeName, object[] genericParams)
	{
		_genericTypeName = genericTypeName;
		_genericParams = new object[genericParams.Length];
		genericParams.CopyTo(_genericParams, 0);
		DataContract.GetClrNameAndNamespace(genericTypeName, out var localName, out var _);
		_nestedParamCounts = DataContract.GetDataContractNameForGenericName(localName, null);
	}

	public int GetParameterCount()
	{
		return _genericParams.Length;
	}

	public IList<int> GetNestedParameterCounts()
	{
		return _nestedParamCounts;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public string GetParameterName(int paramIndex)
	{
		return GetStableName(paramIndex).Name;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public string GetNamespaces()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < GetParameterCount(); i++)
		{
			stringBuilder.Append(' ').Append(GetStableName(i).Namespace);
		}
		return stringBuilder.ToString();
	}

	public string GetGenericTypeName()
	{
		return _genericTypeName;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private XmlQualifiedName GetStableName(int i)
	{
		object obj = _genericParams[i];
		XmlQualifiedName xmlQualifiedName = obj as XmlQualifiedName;
		if (xmlQualifiedName == null)
		{
			Type type = obj as Type;
			xmlQualifiedName = (XmlQualifiedName)((type != null) ? (_genericParams[i] = DataContract.GetStableName(type)) : (_genericParams[i] = ((DataContract)obj).StableName));
		}
		return xmlQualifiedName;
	}
}
