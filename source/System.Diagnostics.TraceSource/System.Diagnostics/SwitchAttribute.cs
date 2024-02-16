using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Diagnostics;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
public sealed class SwitchAttribute : Attribute
{
	private Type _type;

	private string _name;

	public string SwitchName
	{
		get
		{
			return _name;
		}
		[MemberNotNull("_name")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullEmptyArgument, "value"), "value");
			}
			_name = value;
		}
	}

	public Type SwitchType
	{
		get
		{
			return _type;
		}
		[MemberNotNull("_type")]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_type = value;
		}
	}

	public string? SwitchDescription { get; set; }

	public SwitchAttribute(string switchName, Type switchType)
	{
		SwitchName = switchName;
		SwitchType = switchType;
	}

	[RequiresUnreferencedCode("Types may be trimmed from the assembly.")]
	public static SwitchAttribute[] GetAll(Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		List<object> list = new List<object>();
		object[] customAttributes = assembly.GetCustomAttributes(typeof(SwitchAttribute), inherit: false);
		list.AddRange(customAttributes);
		Type[] types = assembly.GetTypes();
		foreach (Type type in types)
		{
			GetAllRecursive(type, list);
		}
		SwitchAttribute[] array = new SwitchAttribute[list.Count];
		object[] array2 = array;
		list.CopyTo(array2, 0);
		return array;
	}

	private static void GetAllRecursive([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, List<object> switchAttribs)
	{
		GetAllRecursive((MemberInfo)type, switchAttribs);
		MemberInfo[] members = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		MemberInfo[] array = members;
		foreach (MemberInfo memberInfo in array)
		{
			if (!(memberInfo is Type))
			{
				GetAllRecursive(memberInfo, switchAttribs);
			}
		}
	}

	private static void GetAllRecursive(MemberInfo member, List<object> switchAttribs)
	{
		object[] customAttributes = member.GetCustomAttributes(typeof(SwitchAttribute), inherit: false);
		switchAttribs.AddRange(customAttributes);
	}
}
