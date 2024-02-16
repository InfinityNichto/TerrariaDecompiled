using System.Collections.Generic;

namespace System.Reflection;

internal abstract class RuntimeFieldInfo : FieldInfo
{
	private BindingFlags m_bindingFlags;

	protected RuntimeType.RuntimeTypeCache m_reflectedTypeCache;

	protected RuntimeType m_declaringType;

	internal BindingFlags BindingFlags => m_bindingFlags;

	private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

	public override MemberTypes MemberType => MemberTypes.Field;

	public override Type ReflectedType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return ReflectedTypeInternal;
			}
			return null;
		}
	}

	public override Type DeclaringType
	{
		get
		{
			if (!m_reflectedTypeCache.IsGlobal)
			{
				return m_declaringType;
			}
			return null;
		}
	}

	public override Module Module => GetRuntimeModule();

	public override bool IsCollectible => m_declaringType.IsCollectible;

	protected RuntimeFieldInfo(RuntimeType.RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
	{
		m_bindingFlags = bindingFlags;
		m_declaringType = declaringType;
		m_reflectedTypeCache = reflectedTypeCache;
	}

	internal RuntimeType GetDeclaringTypeInternal()
	{
		return m_declaringType;
	}

	internal RuntimeType GetRuntimeType()
	{
		return m_declaringType;
	}

	internal abstract RuntimeModule GetRuntimeModule();

	public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		return HasSameMetadataDefinitionAsCore<RuntimeFieldInfo>(other);
	}

	public override string ToString()
	{
		return FieldType.FormatTypeName() + " " + Name;
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
}
