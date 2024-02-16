using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices;

internal static class ICastableHelpers
{
	internal static bool IsInstanceOfInterface(ICastable castable, RuntimeType type, [NotNullWhen(true)] out Exception castError)
	{
		return castable.IsInstanceOfInterface(new RuntimeTypeHandle(type), out castError);
	}

	internal static RuntimeType GetImplType(ICastable castable, RuntimeType interfaceType)
	{
		return castable.GetImplType(new RuntimeTypeHandle(interfaceType)).GetRuntimeType();
	}
}
