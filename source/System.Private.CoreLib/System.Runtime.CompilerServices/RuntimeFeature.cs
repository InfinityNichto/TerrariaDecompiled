using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices;

public static class RuntimeFeature
{
	public const string PortablePdb = "PortablePdb";

	public const string DefaultImplementationsOfInterfaces = "DefaultImplementationsOfInterfaces";

	public const string UnmanagedSignatureCallingConvention = "UnmanagedSignatureCallingConvention";

	public const string CovariantReturnsOfClasses = "CovariantReturnsOfClasses";

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	public const string VirtualStaticsInInterfaces = "VirtualStaticsInInterfaces";

	public static bool IsDynamicCodeSupported => true;

	public static bool IsDynamicCodeCompiled => true;

	public static bool IsSupported(string feature)
	{
		switch (feature)
		{
		case "PortablePdb":
		case "CovariantReturnsOfClasses":
		case "UnmanagedSignatureCallingConvention":
		case "DefaultImplementationsOfInterfaces":
		case "VirtualStaticsInInterfaces":
			return true;
		case "IsDynamicCodeSupported":
			return IsDynamicCodeSupported;
		case "IsDynamicCodeCompiled":
			return IsDynamicCodeCompiled;
		default:
			return false;
		}
	}
}
