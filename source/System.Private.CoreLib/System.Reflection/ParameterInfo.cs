using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Reflection;

public class ParameterInfo : ICustomAttributeProvider, IObjectReference
{
	protected ParameterAttributes AttrsImpl;

	protected Type? ClassImpl;

	protected object? DefaultValueImpl;

	protected MemberInfo MemberImpl;

	protected string? NameImpl;

	protected int PositionImpl;

	public virtual ParameterAttributes Attributes => AttrsImpl;

	public virtual MemberInfo Member => MemberImpl;

	public virtual string? Name => NameImpl;

	public virtual Type ParameterType => ClassImpl;

	public virtual int Position => PositionImpl;

	public bool IsIn => (Attributes & ParameterAttributes.In) != 0;

	public bool IsLcid => (Attributes & ParameterAttributes.Lcid) != 0;

	public bool IsOptional => (Attributes & ParameterAttributes.Optional) != 0;

	public bool IsOut => (Attributes & ParameterAttributes.Out) != 0;

	public bool IsRetval => (Attributes & ParameterAttributes.Retval) != 0;

	public virtual object? DefaultValue
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual object? RawDefaultValue
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual bool HasDefaultValue
	{
		get
		{
			throw NotImplemented.ByDesign;
		}
	}

	public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

	public virtual int MetadataToken => 134217728;

	protected ParameterInfo()
	{
	}

	public virtual bool IsDefined(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		return false;
	}

	public virtual IList<CustomAttributeData> GetCustomAttributesData()
	{
		throw NotImplemented.ByDesign;
	}

	public virtual object[] GetCustomAttributes(bool inherit)
	{
		return Array.Empty<object>();
	}

	public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		return Array.Empty<object>();
	}

	public virtual Type[] GetOptionalCustomModifiers()
	{
		return Type.EmptyTypes;
	}

	public virtual Type[] GetRequiredCustomModifiers()
	{
		return Type.EmptyTypes;
	}

	public object GetRealObject(StreamingContext context)
	{
		if (MemberImpl == null)
		{
			throw new SerializationException(SR.Serialization_InsufficientState);
		}
		switch (MemberImpl.MemberType)
		{
		case MemberTypes.Constructor:
		case MemberTypes.Method:
		{
			if (PositionImpl == -1)
			{
				if (MemberImpl.MemberType == MemberTypes.Method)
				{
					return ((MethodInfo)MemberImpl).ReturnParameter;
				}
				throw new SerializationException(SR.Serialization_BadParameterInfo);
			}
			ParameterInfo[] indexParameters = ((MethodBase)MemberImpl).GetParametersNoCopy();
			if (indexParameters != null && PositionImpl < indexParameters.Length)
			{
				return indexParameters[PositionImpl];
			}
			throw new SerializationException(SR.Serialization_BadParameterInfo);
		}
		case MemberTypes.Property:
		{
			ParameterInfo[] indexParameters = ((PropertyInfo)MemberImpl).GetIndexParameters();
			if (indexParameters != null && PositionImpl > -1 && PositionImpl < indexParameters.Length)
			{
				return indexParameters[PositionImpl];
			}
			throw new SerializationException(SR.Serialization_BadParameterInfo);
		}
		default:
			throw new SerializationException(SR.Serialization_NoParameterInfo);
		}
	}

	public override string ToString()
	{
		string text = ParameterType.FormatTypeName();
		string name = Name;
		if (name != null)
		{
			return text + " " + name;
		}
		return text;
	}
}
