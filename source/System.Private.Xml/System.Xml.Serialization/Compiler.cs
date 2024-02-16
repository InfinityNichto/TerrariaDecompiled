using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace System.Xml.Serialization;

internal sealed class Compiler
{
	private readonly StringWriter _writer = new StringWriter(CultureInfo.InvariantCulture);

	internal TextWriter Source => _writer;

	[RequiresUnreferencedCode("Reflects against input Type DeclaringType")]
	internal void AddImport(Type type, Hashtable types)
	{
		if (type == null || TypeScope.IsKnownType(type) || types[type] != null)
		{
			return;
		}
		types[type] = type;
		Type baseType = type.BaseType;
		if (baseType != null)
		{
			AddImport(baseType, types);
		}
		Type declaringType = type.DeclaringType;
		if (declaringType != null)
		{
			AddImport(declaringType, types);
		}
		Type[] interfaces = type.GetInterfaces();
		foreach (Type type2 in interfaces)
		{
			AddImport(type2, types);
		}
		ConstructorInfo[] constructors = type.GetConstructors();
		for (int j = 0; j < constructors.Length; j++)
		{
			ParameterInfo[] parameters = constructors[j].GetParameters();
			for (int k = 0; k < parameters.Length; k++)
			{
				AddImport(parameters[k].ParameterType, types);
			}
		}
		if (type.IsGenericType)
		{
			Type[] genericArguments = type.GetGenericArguments();
			for (int l = 0; l < genericArguments.Length; l++)
			{
				AddImport(genericArguments[l], types);
			}
		}
		Module module = type.Module;
		Assembly assembly = module.Assembly;
		if (DynamicAssemblies.IsTypeDynamic(type))
		{
			DynamicAssemblies.Add(assembly);
			return;
		}
		object[] customAttributes = type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), inherit: false);
		if (customAttributes.Length != 0)
		{
			TypeForwardedFromAttribute typeForwardedFromAttribute = customAttributes[0] as TypeForwardedFromAttribute;
			Assembly.Load(new AssemblyName(typeForwardedFromAttribute.AssemblyFullName));
		}
	}

	internal void AddImport(Assembly assembly)
	{
	}

	internal void Close()
	{
	}

	internal static string GetTempAssemblyName(AssemblyName parent, string ns)
	{
		return parent.Name + ".XmlSerializers" + (string.IsNullOrEmpty(ns) ? "" : $".{GetPersistentHashCode(ns)}");
	}

	private static uint GetPersistentHashCode(string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		byte[] array = SHA512.HashData(bytes);
		return BinaryPrimitives.ReadUInt32BigEndian(array);
	}
}
