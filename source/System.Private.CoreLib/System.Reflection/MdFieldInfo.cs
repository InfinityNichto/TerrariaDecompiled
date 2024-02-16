using System.Diagnostics;
using System.Globalization;

namespace System.Reflection;

internal sealed class MdFieldInfo : RuntimeFieldInfo
{
	private int m_tkField;

	private string m_name;

	private RuntimeType m_fieldType;

	private FieldAttributes m_fieldAttributes;

	public override string Name => m_name ?? (m_name = GetRuntimeModule().MetadataImport.GetName(m_tkField).ToString());

	public override int MetadataToken => m_tkField;

	public override RuntimeFieldHandle FieldHandle
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override FieldAttributes Attributes => m_fieldAttributes;

	public override bool IsSecurityCritical => DeclaringType.IsSecurityCritical;

	public override bool IsSecuritySafeCritical => DeclaringType.IsSecuritySafeCritical;

	public override bool IsSecurityTransparent => DeclaringType.IsSecurityTransparent;

	public unsafe override Type FieldType
	{
		get
		{
			if (m_fieldType == null)
			{
				ConstArray sigOfFieldDef = GetRuntimeModule().MetadataImport.GetSigOfFieldDef(m_tkField);
				m_fieldType = new Signature(sigOfFieldDef.Signature.ToPointer(), sigOfFieldDef.Length, m_declaringType).FieldType;
			}
			return m_fieldType;
		}
	}

	internal MdFieldInfo(int tkField, FieldAttributes fieldAttributes, RuntimeTypeHandle declaringTypeHandle, RuntimeType.RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
		: base(reflectedTypeCache, declaringTypeHandle.GetRuntimeType(), bindingFlags)
	{
		m_tkField = tkField;
		m_name = null;
		m_fieldAttributes = fieldAttributes;
	}

	internal override bool CacheEquals(object o)
	{
		if (o is MdFieldInfo mdFieldInfo && mdFieldInfo.m_tkField == m_tkField)
		{
			return m_declaringType.GetTypeHandleInternal().GetModuleHandle().Equals(mdFieldInfo.m_declaringType.GetTypeHandleInternal().GetModuleHandle());
		}
		return false;
	}

	internal override RuntimeModule GetRuntimeModule()
	{
		return m_declaringType.GetRuntimeModule();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object GetValueDirect(TypedReference obj)
	{
		return GetValue(null);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override void SetValueDirect(TypedReference obj, object value)
	{
		throw new FieldAccessException(SR.Acc_ReadOnly);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object GetValue(object obj)
	{
		return GetValue(raw: false);
	}

	public override object GetRawConstantValue()
	{
		return GetValue(raw: true);
	}

	private object GetValue(bool raw)
	{
		object value = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_tkField, FieldType.GetTypeHandleInternal(), raw);
		if (value == DBNull.Value)
		{
			throw new NotSupportedException(SR.Arg_EnumLitValueNotFound);
		}
		return value;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		throw new FieldAccessException(SR.Acc_ReadOnly);
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return Type.EmptyTypes;
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return Type.EmptyTypes;
	}
}
