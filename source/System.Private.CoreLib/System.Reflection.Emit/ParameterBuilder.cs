using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

public class ParameterBuilder
{
	private readonly string _name;

	private readonly int _position;

	private readonly ParameterAttributes _attributes;

	private MethodBuilder _methodBuilder;

	private int _token;

	public virtual string? Name => _name;

	public virtual int Position => _position;

	public virtual int Attributes => (int)_attributes;

	public bool IsIn => (_attributes & ParameterAttributes.In) != 0;

	public bool IsOut => (_attributes & ParameterAttributes.Out) != 0;

	public bool IsOptional => (_attributes & ParameterAttributes.Optional) != 0;

	public virtual void SetConstant(object? defaultValue)
	{
		TypeBuilder.SetConstantValue(_methodBuilder.GetModuleBuilder(), _token, (_position == 0) ? _methodBuilder.ReturnType : _methodBuilder.m_parameterTypes[_position - 1], defaultValue);
	}

	public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
	{
		if (con == null)
		{
			throw new ArgumentNullException("con");
		}
		if (binaryAttribute == null)
		{
			throw new ArgumentNullException("binaryAttribute");
		}
		TypeBuilder.DefineCustomAttribute(_methodBuilder.GetModuleBuilder(), _token, ((ModuleBuilder)_methodBuilder.GetModule()).GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		customBuilder.CreateCustomAttribute((ModuleBuilder)_methodBuilder.GetModule(), _token);
	}

	internal ParameterBuilder(MethodBuilder methodBuilder, int sequence, ParameterAttributes attributes, string paramName)
	{
		_position = sequence;
		_name = paramName;
		_methodBuilder = methodBuilder;
		_attributes = attributes;
		ModuleBuilder module = _methodBuilder.GetModuleBuilder();
		_token = TypeBuilder.SetParamInfo(new QCallModule(ref module), _methodBuilder.MetadataToken, sequence, attributes, paramName);
	}
}
