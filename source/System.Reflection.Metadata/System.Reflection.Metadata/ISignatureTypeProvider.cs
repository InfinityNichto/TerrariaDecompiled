namespace System.Reflection.Metadata;

public interface ISignatureTypeProvider<TType, TGenericContext> : ISimpleTypeProvider<TType>, IConstructedTypeProvider<TType>, ISZArrayTypeProvider<TType>
{
	TType GetFunctionPointerType(MethodSignature<TType> signature);

	TType GetGenericMethodParameter(TGenericContext genericContext, int index);

	TType GetGenericTypeParameter(TGenericContext genericContext, int index);

	TType GetModifiedType(TType modifier, TType unmodifiedType, bool isRequired);

	TType GetPinnedType(TType elementType);

	TType GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind);
}
