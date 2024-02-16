using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

public interface ICastable
{
	bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, [NotNullWhen(true)] out Exception? castError);

	RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType);
}
