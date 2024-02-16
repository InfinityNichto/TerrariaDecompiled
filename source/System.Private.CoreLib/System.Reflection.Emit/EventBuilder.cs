using System.Runtime.CompilerServices;

namespace System.Reflection.Emit;

public sealed class EventBuilder
{
	private string m_name;

	private int m_evToken;

	private ModuleBuilder m_module;

	private EventAttributes m_attributes;

	private TypeBuilder m_type;

	internal EventBuilder(ModuleBuilder mod, string name, EventAttributes attr, TypeBuilder type, int evToken)
	{
		m_name = name;
		m_module = mod;
		m_attributes = attr;
		m_evToken = evToken;
		m_type = type;
	}

	private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
	{
		if (mdBuilder == null)
		{
			throw new ArgumentNullException("mdBuilder");
		}
		m_type.ThrowIfCreated();
		ModuleBuilder module = m_module;
		TypeBuilder.DefineMethodSemantics(new QCallModule(ref module), m_evToken, semantics, mdBuilder.MetadataToken);
	}

	public void SetAddOnMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
	}

	public void SetRemoveOnMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
	}

	public void SetRaiseMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
	}

	public void AddOtherMethod(MethodBuilder mdBuilder)
	{
		SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
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
		m_type.ThrowIfCreated();
		TypeBuilder.DefineCustomAttribute(m_module, m_evToken, m_module.GetConstructorToken(con), binaryAttribute);
	}

	public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
	{
		if (customBuilder == null)
		{
			throw new ArgumentNullException("customBuilder");
		}
		m_type.ThrowIfCreated();
		customBuilder.CreateCustomAttribute(m_module, m_evToken);
	}
}
