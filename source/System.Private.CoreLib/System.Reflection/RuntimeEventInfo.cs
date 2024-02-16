using System.Collections.Generic;

namespace System.Reflection;

internal sealed class RuntimeEventInfo : EventInfo
{
	private int m_token;

	private EventAttributes m_flags;

	private string m_name;

	private unsafe void* m_utf8name;

	private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	private RuntimeMethodInfo m_addMethod;

	private RuntimeMethodInfo m_removeMethod;

	private RuntimeMethodInfo m_raiseMethod;

	private MethodInfo[] m_otherMethod;

	private RuntimeType m_declaringType;

	private BindingFlags m_bindingFlags;

	internal BindingFlags BindingFlags => m_bindingFlags;

	public override MemberTypes MemberType => MemberTypes.Event;

	public unsafe override string Name => m_name ?? (m_name = new MdUtf8String(m_utf8name).ToString());

	public override Type DeclaringType => m_declaringType;

	public override Type ReflectedType => ReflectedTypeInternal;

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	public override int MetadataToken => m_token;

	public override Module Module => GetRuntimeModule();

	public override EventAttributes Attributes => m_flags;

	internal unsafe RuntimeEventInfo(int tkEvent, RuntimeType declaredType, RuntimeType.RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
	{
		MetadataImport metadataImport = declaredType.GetRuntimeModule().MetadataImport;
		m_token = tkEvent;
		m_reflectedTypeCache = reflectedTypeCache;
		m_declaringType = declaredType;
		RuntimeType runtimeType = reflectedTypeCache.GetRuntimeType();
		metadataImport.GetEventProps(tkEvent, out m_utf8name, out m_flags);
		Associates.AssignAssociates(metadataImport, tkEvent, declaredType, runtimeType, out m_addMethod, out m_removeMethod, out m_raiseMethod, out var _, out var _, out m_otherMethod, out isPrivate, out m_bindingFlags);
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RuntimeEventInfo runtimeEventInfo && runtimeEventInfo.m_token == m_token)
		{
			return RuntimeTypeHandle.GetModule(m_declaringType).Equals(RuntimeTypeHandle.GetModule(runtimeEventInfo.m_declaringType));
		}
		return false;
	}

	public override string ToString()
	{
		if (m_addMethod == null || m_addMethod.GetParametersNoCopy().Length == 0)
		{
			throw new InvalidOperationException(SR.InvalidOperation_NoPublicAddMethod);
		}
		return m_addMethod.GetParametersNoCopy()[0].ParameterType.FormatTypeName() + " " + Name;
	}

	public override object[] GetCustomAttributes(bool inherit)
	{
		return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
	}

	public override object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.GetCustomAttributes(this, runtimeType);
	}

	public override bool IsDefined(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		RuntimeType runtimeType = attributeType.UnderlyingSystemType as RuntimeType;
		if (runtimeType == null)
		{
			throw new ArgumentException(SR.Arg_MustBeType, "attributeType");
		}
		return CustomAttribute.IsDefined(this, runtimeType);
	}

	public override IList<CustomAttributeData> GetCustomAttributesData()
	{
		return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
	}

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeEventInfo>(other);
	}

	internal RuntimeModule GetRuntimeModule()
	{
		return m_declaringType.GetRuntimeModule();
	}

	public override MethodInfo[] GetOtherMethods(bool nonPublic)
	{
		List<MethodInfo> list = new List<MethodInfo>();
		if (m_otherMethod == null)
		{
			return Array.Empty<MethodInfo>();
		}
		for (int i = 0; i < m_otherMethod.Length; i++)
		{
			if (Associates.IncludeAccessor(m_otherMethod[i], nonPublic))
			{
				list.Add(m_otherMethod[i]);
			}
		}
		return list.ToArray();
	}

	public override MethodInfo GetAddMethod(bool nonPublic)
	{
		if (!Associates.IncludeAccessor(m_addMethod, nonPublic))
		{
			return null;
		}
		return m_addMethod;
	}

	public override MethodInfo GetRemoveMethod(bool nonPublic)
	{
		if (!Associates.IncludeAccessor(m_removeMethod, nonPublic))
		{
			return null;
		}
		return m_removeMethod;
	}

	public override MethodInfo GetRaiseMethod(bool nonPublic)
	{
		if (!Associates.IncludeAccessor(m_raiseMethod, nonPublic))
		{
			return null;
		}
		return m_raiseMethod;
	}
}
