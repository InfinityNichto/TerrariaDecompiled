using System.Resources;
using FxResources.System.Runtime.Serialization.Formatters;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_HTCapacityOverflow => GetResourceString("Arg_HTCapacityOverflow");

	internal static string Serialization_NonSerType => GetResourceString("Serialization_NonSerType");

	internal static string Argument_DataLengthDifferent => GetResourceString("Argument_DataLengthDifferent");

	internal static string ArgumentNull_NullMember => GetResourceString("ArgumentNull_NullMember");

	internal static string Serialization_UnknownMemberInfo => GetResourceString("Serialization_UnknownMemberInfo");

	internal static string Serialization_NoID => GetResourceString("Serialization_NoID");

	internal static string Serialization_TooManyElements => GetResourceString("Serialization_TooManyElements");

	internal static string Argument_InvalidFieldInfo => GetResourceString("Argument_InvalidFieldInfo");

	internal static string Serialization_NeverSeen => GetResourceString("Serialization_NeverSeen");

	internal static string Serialization_IORIncomplete => GetResourceString("Serialization_IORIncomplete");

	internal static string Serialization_ObjectNotSupplied => GetResourceString("Serialization_ObjectNotSupplied");

	internal static string Serialization_NotCyclicallyReferenceableSurrogate => GetResourceString("Serialization_NotCyclicallyReferenceableSurrogate");

	internal static string Serialization_TooManyReferences => GetResourceString("Serialization_TooManyReferences");

	internal static string Serialization_MissingObject => GetResourceString("Serialization_MissingObject");

	internal static string Serialization_InvalidFixupDiscovered => GetResourceString("Serialization_InvalidFixupDiscovered");

	internal static string Serialization_TypeLoadFailure => GetResourceString("Serialization_TypeLoadFailure");

	internal static string Serialization_ValueTypeFixup => GetResourceString("Serialization_ValueTypeFixup");

	internal static string Serialization_PartialValueTypeFixup => GetResourceString("Serialization_PartialValueTypeFixup");

	internal static string Serialization_UnableToFixup => GetResourceString("Serialization_UnableToFixup");

	internal static string ArgumentOutOfRange_ObjectID => GetResourceString("ArgumentOutOfRange_ObjectID");

	internal static string Serialization_RegisterTwice => GetResourceString("Serialization_RegisterTwice");

	internal static string Serialization_NotISer => GetResourceString("Serialization_NotISer");

	internal static string Serialization_ConstructorNotFound => GetResourceString("Serialization_ConstructorNotFound");

	internal static string Serialization_IncorrectNumberOfFixups => GetResourceString("Serialization_IncorrectNumberOfFixups");

	internal static string Serialization_InvalidFixupType => GetResourceString("Serialization_InvalidFixupType");

	internal static string Serialization_IdTooSmall => GetResourceString("Serialization_IdTooSmall");

	internal static string Serialization_ParentChildIdentical => GetResourceString("Serialization_ParentChildIdentical");

	internal static string Serialization_InvalidType => GetResourceString("Serialization_InvalidType");

	internal static string Argument_MustSupplyParent => GetResourceString("Argument_MustSupplyParent");

	internal static string Argument_MemberAndArray => GetResourceString("Argument_MemberAndArray");

	internal static string Serialization_CorruptedStream => GetResourceString("Serialization_CorruptedStream");

	internal static string Serialization_Stream => GetResourceString("Serialization_Stream");

	internal static string Serialization_BinaryHeader => GetResourceString("Serialization_BinaryHeader");

	internal static string Serialization_TypeExpected => GetResourceString("Serialization_TypeExpected");

	internal static string Serialization_StreamEnd => GetResourceString("Serialization_StreamEnd");

	internal static string Serialization_CrossAppDomainError => GetResourceString("Serialization_CrossAppDomainError");

	internal static string Serialization_Map => GetResourceString("Serialization_Map");

	internal static string Serialization_Assembly => GetResourceString("Serialization_Assembly");

	internal static string Serialization_ObjectTypeEnum => GetResourceString("Serialization_ObjectTypeEnum");

	internal static string Serialization_AssemblyId => GetResourceString("Serialization_AssemblyId");

	internal static string Serialization_ArrayType => GetResourceString("Serialization_ArrayType");

	internal static string Serialization_TypeCode => GetResourceString("Serialization_TypeCode");

	internal static string Serialization_TypeWrite => GetResourceString("Serialization_TypeWrite");

	internal static string Serialization_TypeRead => GetResourceString("Serialization_TypeRead");

	internal static string Serialization_AssemblyNotFound => GetResourceString("Serialization_AssemblyNotFound");

	internal static string Serialization_InvalidFormat => GetResourceString("Serialization_InvalidFormat");

	internal static string Serialization_TopObject => GetResourceString("Serialization_TopObject");

	internal static string Serialization_XMLElement => GetResourceString("Serialization_XMLElement");

	internal static string Serialization_TopObjectInstantiate => GetResourceString("Serialization_TopObjectInstantiate");

	internal static string Serialization_ArrayTypeObject => GetResourceString("Serialization_ArrayTypeObject");

	internal static string Serialization_TypeMissing => GetResourceString("Serialization_TypeMissing");

	internal static string Serialization_ObjNoID => GetResourceString("Serialization_ObjNoID");

	internal static string Serialization_SerMemberInfo => GetResourceString("Serialization_SerMemberInfo");

	internal static string Argument_MustSupplyContainer => GetResourceString("Argument_MustSupplyContainer");

	internal static string Serialization_ParseError => GetResourceString("Serialization_ParseError");

	internal static string Serialization_ISerializableMemberInfo => GetResourceString("Serialization_ISerializableMemberInfo");

	internal static string Serialization_MemberInfo => GetResourceString("Serialization_MemberInfo");

	internal static string Serialization_ISerializableTypes => GetResourceString("Serialization_ISerializableTypes");

	internal static string Serialization_MissingMember => GetResourceString("Serialization_MissingMember");

	internal static string Serialization_NoMemberInfo => GetResourceString("Serialization_NoMemberInfo");

	internal static string Serialization_SurrogateCycleInArgument => GetResourceString("Serialization_SurrogateCycleInArgument");

	internal static string Serialization_SurrogateCycle => GetResourceString("Serialization_SurrogateCycle");

	internal static string IO_EOF_ReadBeyondEOF => GetResourceString("IO_EOF_ReadBeyondEOF");

	internal static string BinaryFormatter_SerializationDisallowed => GetResourceString("BinaryFormatter_SerializationDisallowed");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}
}
