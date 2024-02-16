namespace System.Reflection.Metadata;

public interface ICustomAttributeTypeProvider<TType> : ISimpleTypeProvider<TType>, ISZArrayTypeProvider<TType>
{
	TType GetSystemType();

	bool IsSystemType(TType type);

	TType GetTypeFromSerializedName(string name);

	PrimitiveTypeCode GetUnderlyingEnumType(TType type);
}
