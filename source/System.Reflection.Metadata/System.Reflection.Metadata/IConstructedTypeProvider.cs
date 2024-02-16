using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public interface IConstructedTypeProvider<TType> : ISZArrayTypeProvider<TType>
{
	TType GetGenericInstantiation(TType genericType, ImmutableArray<TType> typeArguments);

	TType GetArrayType(TType elementType, ArrayShape shape);

	TType GetByReferenceType(TType elementType);

	TType GetPointerType(TType elementType);
}
