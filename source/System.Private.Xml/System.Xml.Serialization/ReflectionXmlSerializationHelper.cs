using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Serialization;

internal static class ReflectionXmlSerializationHelper
{
	[RequiresUnreferencedCode("Reflects over base members")]
	public static MemberInfo GetMember(Type declaringType, string memberName, bool throwOnNotFound)
	{
		MemberInfo[] member = declaringType.GetMember(memberName);
		if (member == null || member.Length == 0)
		{
			bool flag = false;
			Type baseType = declaringType.BaseType;
			while (baseType != null)
			{
				member = baseType.GetMember(memberName);
				if (member != null && member.Length != 0)
				{
					flag = true;
					break;
				}
				baseType = baseType.BaseType;
			}
			if (!flag)
			{
				if (throwOnNotFound)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlInternalErrorDetails, $"Could not find member named {memberName} of type {declaringType}"));
				}
				return null;
			}
			declaringType = baseType;
		}
		MemberInfo result = member[0];
		if (member.Length != 1)
		{
			MemberInfo[] array = member;
			foreach (MemberInfo memberInfo in array)
			{
				if (declaringType == memberInfo.DeclaringType)
				{
					result = memberInfo;
					break;
				}
			}
		}
		return result;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public static MemberInfo GetEffectiveGetInfo(Type declaringType, string memberName)
	{
		MemberInfo member = GetMember(declaringType, memberName, throwOnNotFound: true);
		if (member is PropertyInfo propertyInfo && propertyInfo.GetMethod == null)
		{
			Type baseType = declaringType.BaseType;
			while (baseType != null)
			{
				MemberInfo member2 = GetMember(baseType, memberName, throwOnNotFound: false);
				if (member2 is PropertyInfo propertyInfo2 && propertyInfo2.GetMethod != null && propertyInfo2.PropertyType == propertyInfo.PropertyType)
				{
					return propertyInfo2;
				}
				baseType = baseType.BaseType;
			}
		}
		return member;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public static MemberInfo GetEffectiveSetInfo(Type declaringType, string memberName)
	{
		MemberInfo member = GetMember(declaringType, memberName, throwOnNotFound: true);
		if (member is PropertyInfo propertyInfo && propertyInfo.SetMethod == null)
		{
			Type baseType = declaringType.BaseType;
			while (baseType != null)
			{
				MemberInfo member2 = GetMember(baseType, memberName, throwOnNotFound: false);
				if (member2 is PropertyInfo propertyInfo2 && propertyInfo2.SetMethod != null && propertyInfo2.PropertyType == propertyInfo.PropertyType)
				{
					return propertyInfo2;
				}
				baseType = baseType.BaseType;
			}
		}
		return member;
	}
}
