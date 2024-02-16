using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Reflection;

internal sealed class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
{
	private IntPtr m_fieldHandle;

	private FieldAttributes m_fieldAttributes;

	private string m_name;

	private RuntimeType m_fieldType;

	private INVOCATION_FLAGS m_invocationFlags;

	internal INVOCATION_FLAGS InvocationFlags
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
			{
				return InitializeInvocationFlags();
			}
			return m_invocationFlags;
		}
	}

	RuntimeFieldHandleInternal IRuntimeFieldInfo.Value => new RuntimeFieldHandleInternal(m_fieldHandle);

	public override string Name => m_name ?? (m_name = RuntimeFieldHandle.GetName(this));

	public override int MetadataToken => RuntimeFieldHandle.GetToken(this);

	public override RuntimeFieldHandle FieldHandle => new RuntimeFieldHandle(this);

	public override FieldAttributes Attributes => m_fieldAttributes;

	public override Type FieldType
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return m_fieldType ?? InitializeFieldType();
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private INVOCATION_FLAGS InitializeInvocationFlags()
	{
		Type declaringType = DeclaringType;
		INVOCATION_FLAGS iNVOCATION_FLAGS = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;
		if (declaringType != null && declaringType.ContainsGenericParameters)
		{
			iNVOCATION_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
		}
		if (iNVOCATION_FLAGS == INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN)
		{
			if ((m_fieldAttributes & FieldAttributes.InitOnly) != 0)
			{
				iNVOCATION_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR;
			}
			if ((m_fieldAttributes & FieldAttributes.HasFieldRVA) != 0)
			{
				iNVOCATION_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR;
			}
			Type fieldType = FieldType;
			if (fieldType.IsPointer || fieldType.IsEnum || fieldType.IsPrimitive)
			{
				iNVOCATION_FLAGS |= INVOCATION_FLAGS.INVOCATION_FLAGS_FIELD_SPECIAL_CAST;
			}
		}
		return m_invocationFlags = iNVOCATION_FLAGS | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
	}

	internal RtFieldInfo(RuntimeFieldHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
		: base(reflectedTypeCache, declaringType, bindingFlags)
	{
		m_fieldHandle = handle.Value;
		m_fieldAttributes = RuntimeFieldHandle.GetAttributes(handle);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void CheckConsistency(object target)
	{
		if ((m_fieldAttributes & FieldAttributes.Static) != FieldAttributes.Static && !m_declaringType.IsInstanceOfType(target))
		{
			if (target == null)
			{
				throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
			}
			throw new ArgumentException(SR.Format(SR.Arg_FieldDeclTarget, Name, m_declaringType, target.GetType()));
		}
	}

	internal override bool CacheEquals(object o)
	{
		if (o is RtFieldInfo rtFieldInfo)
		{
			return rtFieldInfo.m_fieldHandle == m_fieldHandle;
		}
		return false;
	}

	internal override RuntimeModule GetRuntimeModule()
	{
		return RuntimeTypeHandle.GetModule(RuntimeFieldHandle.GetApproxDeclaringType(this));
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override object GetValue(object obj)
	{
		INVOCATION_FLAGS invocationFlags = InvocationFlags;
		RuntimeType runtimeType = DeclaringType as RuntimeType;
		if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
		{
			if (runtimeType != null && DeclaringType.ContainsGenericParameters)
			{
				throw new InvalidOperationException(SR.Arg_UnboundGenField);
			}
			throw new FieldAccessException();
		}
		CheckConsistency(obj);
		RuntimeType fieldType = (RuntimeType)FieldType;
		bool domainInitialized = false;
		if (runtimeType == null)
		{
			return RuntimeFieldHandle.GetValue(this, obj, fieldType, null, ref domainInitialized);
		}
		domainInitialized = runtimeType.DomainInitialized;
		object value = RuntimeFieldHandle.GetValue(this, obj, fieldType, runtimeType, ref domainInitialized);
		runtimeType.DomainInitialized = domainInitialized;
		return value;
	}

	public override object GetRawConstantValue()
	{
		throw new InvalidOperationException();
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public unsafe override object GetValueDirect(TypedReference obj)
	{
		if (obj.IsNull)
		{
			throw new ArgumentException(SR.Arg_TypedReference_Null);
		}
		return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType)DeclaringType);
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
	{
		INVOCATION_FLAGS invocationFlags = InvocationFlags;
		RuntimeType runtimeType = DeclaringType as RuntimeType;
		if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
		{
			if (runtimeType != null && runtimeType.ContainsGenericParameters)
			{
				throw new InvalidOperationException(SR.Arg_UnboundGenField);
			}
			throw new FieldAccessException();
		}
		CheckConsistency(obj);
		RuntimeType runtimeType2 = (RuntimeType)FieldType;
		value = runtimeType2.CheckValue(value, binder, culture, invokeAttr);
		bool domainInitialized = false;
		if (runtimeType == null)
		{
			RuntimeFieldHandle.SetValue(this, obj, value, runtimeType2, m_fieldAttributes, null, ref domainInitialized);
			return;
		}
		domainInitialized = runtimeType.DomainInitialized;
		RuntimeFieldHandle.SetValue(this, obj, value, runtimeType2, m_fieldAttributes, runtimeType, ref domainInitialized);
		runtimeType.DomainInitialized = domainInitialized;
	}

	[DebuggerStepThrough]
	[DebuggerHidden]
	public unsafe override void SetValueDirect(TypedReference obj, object value)
	{
		if (obj.IsNull)
		{
			throw new ArgumentException(SR.Arg_TypedReference_Null);
		}
		RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType)DeclaringType);
	}

	internal IntPtr GetFieldHandle()
	{
		return m_fieldHandle;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private RuntimeType InitializeFieldType()
	{
		return m_fieldType = new Signature(this, m_declaringType).FieldType;
	}

	public override Type[] GetRequiredCustomModifiers()
	{
		return new Signature(this, m_declaringType).GetCustomModifiers(1, required: true);
	}

	public override Type[] GetOptionalCustomModifiers()
	{
		return new Signature(this, m_declaringType).GetCustomModifiers(1, required: false);
	}
}
