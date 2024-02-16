using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics;

internal static class GraphicsHelpers
{
	public static void ThrowExceptionFromResult(uint result)
	{
		if (result == 0)
		{
			return;
		}
		throw GetExceptionFromResult(result);
	}

	[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
	[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
	[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
	public static Exception GetExceptionFromResult(uint result)
	{
		if (result == 0)
		{
			return null;
		}
		return result switch
		{
			2289436711u => new InvalidOperationException(FrameworkResources.DriverError), 
			2289436776u => new DeviceLostException(), 
			2289436777u => new DeviceNotResetException(), 
			2289436784u => new NotSupportedException(), 
			2289435004u => new OutOfMemoryException(), 
			2150814720u => new InvalidOperationException(FrameworkResources.DirectRenderingWrongMode), 
			_ => Helpers.GetExceptionFromResult(result), 
		};
	}

	internal static Blend AdjustAlphaBlend(Blend blend)
	{
		return blend switch
		{
			Blend.SourceColor => Blend.SourceAlpha, 
			Blend.InverseSourceColor => Blend.InverseSourceAlpha, 
			Blend.DestinationColor => Blend.DestinationAlpha, 
			Blend.InverseDestinationColor => Blend.InverseDestinationAlpha, 
			_ => blend, 
		};
	}

	internal static bool IsSeparateBlend(Blend colorBlend, Blend alphaBlend)
	{
		return AdjustAlphaBlend(colorBlend) != AdjustAlphaBlend(alphaBlend);
	}
}
